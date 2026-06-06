USE Apostas;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_RegistarEvento
    @EventId UNIQUEIDENTIFIER,
    @SourceSystem NVARCHAR(80),
    @AggregateType NVARCHAR(80),
    @AggregateKey NVARCHAR(120),
    @EventType NVARCHAR(120),
    @PayloadJson NVARCHAR(MAX),
    @OccurredAtUtc DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Evento_Middleware WHERE EventId = @EventId)
        RETURN;

    INSERT INTO dbo.Evento_Middleware (EventId, EventType, SourceSystem, AggregateType, AggregateKey, PayloadJson, CriadoEmUtc)
    VALUES (@EventId, @EventType, @SourceSystem, @AggregateType, @AggregateKey, @PayloadJson, @OccurredAtUtc);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_RegistarAlerta
    @Severity NVARCHAR(20),
    @Message NVARCHAR(400),
    @Competition NVARCHAR(100) = NULL,
    @SourceEventId UNIQUEIDENTIFIER = NULL,
    @OccurredAtUtc DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    IF @SourceEventId IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Alerta_Middleware WHERE SourceEventId = @SourceEventId)
        RETURN;

    INSERT INTO dbo.Alerta_Middleware (Severidade, Mensagem, Competicao, SourceEventId, CriadoEmUtc)
    VALUES (@Severity, @Message, @Competition, @SourceEventId, @OccurredAtUtc);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_RecalcularDashboard
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock
        @Resource = 'BetStrike.Dashboard.Rebuild',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = 30000;

    IF @LockResult < 0
        THROW 51000, 'Nao foi possivel obter o lock para recalcular o dashboard.', 1;

    DECLARE @AgoraUtc DATETIME2(0) = SYSUTCDATETIME();
    DECLARE @Corte24h DATETIME2(0) = DATEADD(DAY, -1, @AgoraUtc);
    DECLARE @Corte1h DATETIME2(0) = DATEADD(HOUR, -1, @AgoraUtc);

    DECLARE @JogosAtivos INT = (
        SELECT COUNT(*)
        FROM dbo.Jogo
        WHERE Estado IN (1, 2)
    );

    DECLARE @ApostasPendentes INT = (
        SELECT COUNT(*)
        FROM dbo.Aposta
        WHERE Estado = 1
    );

    DECLARE @ApostasGanhas INT = (
        SELECT COUNT(*)
        FROM dbo.Aposta
        WHERE Estado = 2
    );

    DECLARE @VolumeTotalApostado DECIMAL(18,2) = (
        SELECT COALESCE(SUM(ValorApostado), 0)
        FROM dbo.Aposta
    );

    DECLARE @MargemPlataforma DECIMAL(18,2) = (
        SELECT COALESCE(SUM(CASE WHEN Estado = 3 THEN ValorApostado ELSE 0 END), 0)
             - COALESCE(SUM(CASE WHEN Estado = 2 THEN ValorApostado * OddMomento ELSE 0 END), 0)
        FROM dbo.Aposta
    );

    DECLARE @UtilizadoresAtivos INT = (
        SELECT COUNT(DISTINCT UtilizadorId)
        FROM dbo.Aposta
        WHERE DataHoraUtc >= @Corte24h
    );

    DECLARE @ApostasPorMinuto DECIMAL(10,2) = (
        SELECT CAST(COUNT(*) / 60.0 AS DECIMAL(10,2))
        FROM dbo.Aposta
        WHERE DataHoraUtc >= @Corte1h
    );

    DECLARE @MediaMovel15MinVolume DECIMAL(18,2) = (
        SELECT COALESCE(AVG(CAST(VolumeTotal AS DECIMAL(18,2))), 0)
        FROM (
            SELECT TOP (4)
                DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, DataHoraUtc) / 15) * 15, 0) AS JanelaInicioUtc,
                SUM(ValorApostado) AS VolumeTotal
            FROM dbo.Aposta
            WHERE DataHoraUtc >= @Corte24h
            GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, DataHoraUtc) / 15) * 15, 0)
            ORDER BY JanelaInicioUtc DESC
        ) AS ultimas_janelas
    );

    DECLARE @MaxVolumeHora DECIMAL(18,2) = 0;
    DECLARE @MinVolumeHora DECIMAL(18,2) = 0;

    ;WITH JanelasHora AS
    (
        SELECT
            DATEADD(HOUR, DATEDIFF(HOUR, 0, DataHoraUtc), 0) AS JanelaInicioUtc,
            SUM(ValorApostado) AS VolumeTotal
        FROM dbo.Aposta
        WHERE DataHoraUtc >= @Corte24h
        GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, DataHoraUtc), 0)
    )
    SELECT
        @MaxVolumeHora = COALESCE(MAX(VolumeTotal), 0),
        @MinVolumeHora = COALESCE(MIN(VolumeTotal), 0)
    FROM JanelasHora;

    MERGE dbo.Dashboard_Resumo AS target
    USING (
        SELECT CAST(1 AS BIT) AS Id,
               @JogosAtivos AS JogosAtivos,
               @ApostasPendentes AS ApostasPendentes,
               @ApostasGanhas AS ApostasGanhas,
               @VolumeTotalApostado AS VolumeTotalApostado,
               @MargemPlataforma AS MargemPlataforma,
               @UtilizadoresAtivos AS UtilizadoresAtivos,
               @ApostasPorMinuto AS ApostasPorMinuto,
               @MediaMovel15MinVolume AS MediaMovel15MinVolume,
               @MaxVolumeHora AS MaxVolumeHora,
               @MinVolumeHora AS MinVolumeHora,
               SYSUTCDATETIME() AS AtualizadoUtc
    ) AS source
    ON target.Id = source.Id
    WHEN MATCHED THEN
        UPDATE SET
            JogosAtivos = source.JogosAtivos,
            ApostasPendentes = source.ApostasPendentes,
            ApostasGanhas = source.ApostasGanhas,
            VolumeTotalApostado = source.VolumeTotalApostado,
            MargemPlataforma = source.MargemPlataforma,
            UtilizadoresAtivos = source.UtilizadoresAtivos,
            ApostasPorMinuto = source.ApostasPorMinuto,
            MediaMovel15MinVolume = source.MediaMovel15MinVolume,
            MaxVolumeHora = source.MaxVolumeHora,
            MinVolumeHora = source.MinVolumeHora,
            AtualizadoUtc = source.AtualizadoUtc
    WHEN NOT MATCHED THEN
        INSERT (Id, JogosAtivos, ApostasPendentes, ApostasGanhas, VolumeTotalApostado, MargemPlataforma, UtilizadoresAtivos, ApostasPorMinuto, MediaMovel15MinVolume, MaxVolumeHora, MinVolumeHora, AtualizadoUtc)
        VALUES (source.Id, source.JogosAtivos, source.ApostasPendentes, source.ApostasGanhas, source.VolumeTotalApostado, source.MargemPlataforma, source.UtilizadoresAtivos, source.ApostasPorMinuto, source.MediaMovel15MinVolume, source.MaxVolumeHora, source.MinVolumeHora, source.AtualizadoUtc);

    DELETE FROM dbo.Dashboard_Competicao;
    DELETE FROM dbo.Dashboard_JanelaTemporal;
    DELETE FROM dbo.Dashboard_ExposicaoJogo;
    DELETE FROM dbo.Dashboard_TipoAposta;

    ;WITH BaseCompeticao AS
    (
        SELECT
            j.Competicao,
            j.Id AS JogoId,
            ISNULL(r.GolosCasa, 0) AS GolosCasa,
            ISNULL(r.GolosFora, 0) AS GolosFora
        FROM dbo.Jogo j
        LEFT JOIN dbo.Resultado r ON r.JogoId = j.Id
    )
    INSERT INTO dbo.Dashboard_Competicao
    (
        Competicao,
        MediaGolosPorJogo,
        VolumeTotalApostado,
        TaxaResultado1,
        TaxaResultadoX,
        TaxaResultado2,
        AtualizadoUtc
    )
    SELECT
        b.Competicao,
        AVG(CAST(b.GolosCasa + b.GolosFora AS DECIMAL(10,2))) AS MediaGolosPorJogo,
        COALESCE((
            SELECT SUM(a.ValorApostado)
            FROM dbo.Aposta a
            INNER JOIN dbo.Jogo j2 ON j2.Id = a.JogoId
            WHERE j2.Competicao = b.Competicao
        ), 0) AS VolumeTotalApostado,
        AVG(CASE WHEN b.GolosCasa > b.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultado1,
        AVG(CASE WHEN b.GolosCasa = b.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultadoX,
        AVG(CASE WHEN b.GolosCasa < b.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultado2,
        SYSUTCDATETIME() AS AtualizadoUtc
    FROM BaseCompeticao b
    GROUP BY b.Competicao;

    ;WITH Janelas15 AS
    (
        SELECT
            DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, DataHoraUtc) / 15) * 15, 0) AS JanelaInicioUtc,
            COUNT(*) AS Apostas,
            SUM(ValorApostado) AS VolumeTotal,
            AVG(ValorApostado) AS MediaAposta,
            MAX(ValorApostado) AS MaxAposta,
            MIN(ValorApostado) AS MinAposta
        FROM dbo.Aposta
        WHERE DataHoraUtc >= @Corte24h
        GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, DataHoraUtc) / 15) * 15, 0)
    )
    INSERT INTO dbo.Dashboard_JanelaTemporal (JanelaInicioUtc, Apostas, VolumeTotal, MediaAposta, MaxAposta, MinAposta)
    SELECT JanelaInicioUtc, Apostas, COALESCE(VolumeTotal, 0), COALESCE(MediaAposta, 0), COALESCE(MaxAposta, 0), COALESCE(MinAposta, 0)
    FROM Janelas15;

    ;WITH ExposicaoJogo AS
    (
        SELECT
            j.Id AS JogoId,
            j.CodigoJogo,
            j.Competicao,
            COUNT(CASE WHEN a.Estado = 1 THEN 1 END) AS ApostasPendentes,
            COALESCE(SUM(a.ValorApostado), 0) AS VolumeTotal,
            COALESCE(SUM(a.ValorApostado), 0) - COALESCE(SUM(CASE WHEN a.Estado = 2 THEN a.ValorApostado * a.OddMomento ELSE 0 END), 0) AS ExposicaoLiquida
        FROM dbo.Jogo j
        LEFT JOIN dbo.Aposta a ON a.JogoId = j.Id
        GROUP BY j.Id, j.CodigoJogo, j.Competicao
    )
    INSERT INTO dbo.Dashboard_ExposicaoJogo (JogoId, CodigoJogo, Competicao, ApostasPendentes, VolumeTotal, ExposicaoLiquida, AtualizadoUtc)
    SELECT JogoId, CodigoJogo, Competicao, ApostasPendentes, VolumeTotal, ExposicaoLiquida, @AgoraUtc
    FROM ExposicaoJogo;

    ;WITH TiposAposta AS
    (
        SELECT
            TipoAposta,
            COUNT(*) AS TotalApostas,
            SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS ApostasGanhas,
            SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS ApostasPerdidas,
            COALESCE(SUM(ValorApostado), 0) AS VolumeTotal
        FROM dbo.Aposta
        GROUP BY TipoAposta
    )
    INSERT INTO dbo.Dashboard_TipoAposta (TipoAposta, TotalApostas, ApostasGanhas, ApostasPerdidas, TaxaVitoria, VolumeTotal, AtualizadoUtc)
    SELECT
        TipoAposta,
        TotalApostas,
        ApostasGanhas,
        ApostasPerdidas,
        CASE WHEN TotalApostas = 0 THEN 0 ELSE CAST(ApostasGanhas AS DECIMAL(6,4)) / NULLIF(CAST(TotalApostas AS DECIMAL(6,4)), 0) END,
        VolumeTotal,
        @AgoraUtc
    FROM TiposAposta;

    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_ConsultarDashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        AtualizadoUtc,
        JogosAtivos,
        ApostasPendentes,
        ApostasGanhas,
        VolumeTotalApostado,
        MargemPlataforma,
        UtilizadoresAtivos,
        ApostasPorMinuto,
        MediaMovel15MinVolume,
        MaxVolumeHora,
        MinVolumeHora
    FROM dbo.Dashboard_Resumo
    ORDER BY AtualizadoUtc DESC;

    SELECT
        Competicao,
        MediaGolosPorJogo,
        VolumeTotalApostado,
        TaxaResultado1,
        TaxaResultadoX,
        TaxaResultado2,
        AtualizadoUtc
    FROM dbo.Dashboard_Competicao
    ORDER BY VolumeTotalApostado DESC, Competicao;

    SELECT TOP (8)
        JanelaInicioUtc,
        Apostas,
        VolumeTotal,
        MediaAposta,
        MaxAposta,
        MinAposta
    FROM dbo.Dashboard_JanelaTemporal
    ORDER BY JanelaInicioUtc DESC;

    SELECT TOP (10)
        JogoId,
        CodigoJogo,
        Competicao,
        ApostasPendentes,
        VolumeTotal,
        ExposicaoLiquida,
        AtualizadoUtc
    FROM dbo.Dashboard_ExposicaoJogo
    ORDER BY ExposicaoLiquida DESC, VolumeTotal DESC;

    SELECT TOP (4)
        TipoAposta,
        TotalApostas,
        ApostasGanhas,
        ApostasPerdidas,
        TaxaVitoria,
        VolumeTotal,
        AtualizadoUtc
    FROM dbo.Dashboard_TipoAposta
    ORDER BY VolumeTotal DESC, TipoAposta;

    SELECT TOP (6)
        Id,
        Severidade,
        Mensagem,
        Competicao,
        SourceEventId,
        CriadoEmUtc
    FROM dbo.Alerta_Middleware
    ORDER BY CriadoEmUtc DESC, Id DESC;

    SELECT TOP (8)
        EventId,
        EventType,
        SourceSystem,
        AggregateType,
        AggregateKey,
        PayloadJson,
        CriadoEmUtc
    FROM dbo.Evento_Middleware
    ORDER BY CriadoEmUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_ConsultarAlertas
    @Limite INT = 12
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limite)
        Id,
        Severidade,
        Mensagem,
        Competicao,
        SourceEventId,
        CriadoEmUtc
    FROM dbo.Alerta_Middleware
    ORDER BY CriadoEmUtc DESC, Id DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Middleware_ConsultarEventos
    @Limite INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limite)
        EventSequence,
        EventId,
        EventType,
        SourceSystem,
        AggregateType,
        AggregateKey,
        PayloadJson,
        CriadoEmUtc
    FROM dbo.Evento_Middleware
    ORDER BY EventSequence DESC;
END
GO
