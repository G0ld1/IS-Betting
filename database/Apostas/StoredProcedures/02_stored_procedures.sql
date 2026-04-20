USE Apostas;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_InserirJogo
    @CodigoJogo VARCHAR(20),
    @DataJogo DATE,
    @HoraInicio TIME(0),
    @EquipaCasa NVARCHAR(100),
    @EquipaFora NVARCHAR(100),
    @Competicao NVARCHAR(100),
    @Estado INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @CodigoJogo NOT LIKE 'FUT-[1-2][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
        THROW 51000, 'Formato de código inválido.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Jogo WHERE CodigoJogo = @CodigoJogo)
        THROW 51001, 'Jogo duplicado.', 1;

    INSERT INTO dbo.Jogo (CodigoJogo, DataJogo, HoraInicio, EquipaCasa, EquipaFora, Competicao, Estado)
    VALUES (@CodigoJogo, @DataJogo, @HoraInicio, @EquipaCasa, @EquipaFora, @Competicao, @Estado);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_AtualizarEstadoResultadoJogo
    @CodigoJogo VARCHAR(20),
    @Estado INT,
    @GolosCasa INT = NULL,
    @GolosFora INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JogoId INT, @EstadoAtual INT;
    SELECT @JogoId = Id, @EstadoAtual = Estado FROM dbo.Jogo WHERE CodigoJogo = @CodigoJogo;

    IF @JogoId IS NULL
        THROW 51002, 'Jogo não existe.', 1;

    IF (@EstadoAtual = 3 AND @Estado <> 3) OR (@EstadoAtual IN (4,5) AND @Estado <> @EstadoAtual)
        THROW 51003, 'Transição inválida.', 1;

    UPDATE dbo.Jogo SET Estado = @Estado WHERE Id = @JogoId;

    IF @Estado = 3
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Resultado WHERE JogoId = @JogoId)
        BEGIN
            INSERT INTO dbo.Resultado (JogoId, GolosCasa, GolosFora, Desconhecido)
            VALUES (@JogoId, ISNULL(@GolosCasa,0), ISNULL(@GolosFora,0), CASE WHEN @GolosCasa IS NULL OR @GolosFora IS NULL THEN 1 ELSE 0 END);
        END
        ELSE IF @GolosCasa IS NOT NULL AND @GolosFora IS NOT NULL
        BEGIN
            UPDATE dbo.Resultado
            SET GolosCasa = @GolosCasa,
                GolosFora = @GolosFora,
                Desconhecido = 0,
                UltimaAtualizacaoUtc = SYSUTCDATETIME()
            WHERE JogoId = @JogoId;
        END

        EXEC dbo.sp_Apostas_ResolverApostas @JogoId;
    END

    IF @Estado IN (4,5)
    BEGIN
        UPDATE dbo.Aposta
        SET Estado = 4
        WHERE JogoId = @JogoId
          AND Estado = 1;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_InserirResultado
    @JogoId INT,
    @GolosCasa INT,
    @GolosFora INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Jogo WHERE Id = @JogoId AND Estado = 3)
        THROW 51004, 'Resultado só pode ser inserido com jogo Finalizado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Resultado WHERE JogoId = @JogoId)
        THROW 51005, 'Jogo já possui resultado.', 1;

    INSERT INTO dbo.Resultado (JogoId, GolosCasa, GolosFora, Desconhecido)
    VALUES (@JogoId, @GolosCasa, @GolosFora, 0);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_InserirAposta
    @JogoId INT,
    @UtilizadorId INT,
    @TipoAposta CHAR(1),
    @ValorApostado DECIMAL(12,2),
    @OddMomento DECIMAL(8,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EstadoJogo INT;
    SELECT @EstadoJogo = Estado FROM dbo.Jogo WHERE Id = @JogoId;

    IF @EstadoJogo IS NULL
        THROW 51006, 'Jogo não existe.', 1;

    IF @EstadoJogo IN (3,4,5)
        THROW 51007, 'Não é permitido apostar neste estado de jogo.', 1;

    IF @TipoAposta NOT IN ('1','X','2')
        THROW 51008, 'Tipo de aposta inválido.', 1;

    IF @ValorApostado <= 0 OR @OddMomento <= 1.0
        THROW 51009, 'Valor/Odd inválido.', 1;

    BEGIN TRAN;

    DECLARE @ApostaId INT;

    INSERT INTO dbo.Aposta (JogoId, UtilizadorId, TipoAposta, ValorApostado, OddMomento, Estado)
    VALUES (@JogoId, @UtilizadorId, @TipoAposta, @ValorApostado, @OddMomento, 1);

    SET @ApostaId = CAST(SCOPE_IDENTITY() AS INT);

    EXEC Pagamentos.dbo.sp_Pagamentos_DebitarAposta @ApostaId, @UtilizadorId, @ValorApostado;

    COMMIT TRAN;

    SELECT @ApostaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_CancelarAposta
    @ApostaId INT,
    @UtilizadorId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE a
    SET a.Estado = 4
    FROM dbo.Aposta a
    INNER JOIN dbo.Jogo j ON j.Id = a.JogoId
    WHERE a.Id = @ApostaId
      AND a.UtilizadorId = @UtilizadorId
      AND a.Estado = 1
      AND j.Estado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ResolverApostas
    @JogoId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GolosCasa INT, @GolosFora INT;
    SELECT @GolosCasa = GolosCasa, @GolosFora = GolosFora
    FROM dbo.Resultado
    WHERE JogoId = @JogoId;

    IF @GolosCasa IS NULL OR @GolosFora IS NULL
        THROW 51010, 'Resultado inexistente para resolver apostas.', 1;

    UPDATE dbo.Aposta
    SET Estado = CASE
        WHEN TipoAposta = '1' AND @GolosCasa > @GolosFora THEN 2
        WHEN TipoAposta = 'X' AND @GolosCasa = @GolosFora THEN 2
        WHEN TipoAposta = '2' AND @GolosFora > @GolosCasa THEN 2
        ELSE 3
    END
    WHERE JogoId = @JogoId
      AND Estado = 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_CriarUtilizador
    @Nome NVARCHAR(120),
    @Email NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    INSERT INTO dbo.Utilizador (Nome, Email)
    VALUES (@Nome, @Email);

    DECLARE @UtilizadorId INT = CAST(SCOPE_IDENTITY() AS INT);

    EXEC Pagamentos.dbo.sp_Pagamentos_CriarSaldoInicial @UtilizadorId, 50.00;

    COMMIT TRAN;

    SELECT @UtilizadorId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ConsultarJogos
    @DataJogo DATE = NULL,
    @Estado INT = NULL,
    @Competicao NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT j.*
    FROM dbo.Jogo j
    WHERE (@DataJogo IS NULL OR j.DataJogo = @DataJogo)
      AND (@Estado IS NULL OR j.Estado = @Estado)
      AND (@Competicao IS NULL OR j.Competicao = @Competicao)
    ORDER BY j.DataJogo, j.HoraInicio;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ObterJogo
    @CodigoJogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 j.*,
           SUM(CASE WHEN a.Estado = 1 THEN 1 ELSE 0 END) AS ApostasPendentes,
           ISNULL(SUM(a.ValorApostado),0) AS VolumeTotalApostado
    FROM dbo.Jogo j
    LEFT JOIN dbo.Aposta a ON a.JogoId = j.Id
    WHERE j.CodigoJogo = @CodigoJogo
    GROUP BY j.Id, j.CodigoJogo, j.DataJogo, j.HoraInicio, j.EquipaCasa, j.EquipaFora, j.Competicao, j.Estado;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_RemoverJogo
    @CodigoJogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE j
    FROM dbo.Jogo j
    WHERE j.CodigoJogo = @CodigoJogo
      AND j.Estado = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.Aposta a WHERE a.JogoId = j.Id);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ConsultarApostas
    @UtilizadorId INT = NULL,
    @JogoId INT = NULL,
    @Estado INT = NULL,
    @InicioUtc DATETIME2(0) = NULL,
    @FimUtc DATETIME2(0) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Aposta
    WHERE (@UtilizadorId IS NULL OR UtilizadorId = @UtilizadorId)
      AND (@JogoId IS NULL OR JogoId = @JogoId)
      AND (@Estado IS NULL OR Estado = @Estado)
      AND (@InicioUtc IS NULL OR DataHoraUtc >= @InicioUtc)
      AND (@FimUtc IS NULL OR DataHoraUtc <= @FimUtc)
    ORDER BY DataHoraUtc DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ObterAposta
    @ApostaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.Aposta WHERE Id = @ApostaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_ObterResultado
    @JogoId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM dbo.Resultado WHERE JogoId = @JogoId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_EstatisticasPorJogo
    @CodigoJogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.CodigoJogo,
        ISNULL(SUM(a.ValorApostado),0) AS TotalApostado,
        SUM(CASE WHEN a.TipoAposta = '1' THEN 1 ELSE 0 END) AS ApostasCasa,
        SUM(CASE WHEN a.TipoAposta = 'X' THEN 1 ELSE 0 END) AS ApostasEmpate,
        SUM(CASE WHEN a.TipoAposta = '2' THEN 1 ELSE 0 END) AS ApostasFora,
        SUM(CASE WHEN a.Estado = 1 THEN 1 ELSE 0 END) AS Pendentes,
        SUM(CASE WHEN a.Estado = 2 THEN 1 ELSE 0 END) AS Ganhas,
        SUM(CASE WHEN a.Estado = 3 THEN 1 ELSE 0 END) AS Perdidas,
        SUM(CASE WHEN a.Estado = 4 THEN 1 ELSE 0 END) AS Anuladas,
        ISNULL(SUM(CASE WHEN a.Estado = 3 THEN a.ValorApostado ELSE 0 END),0)
         - ISNULL(SUM(CASE WHEN a.Estado = 2 THEN a.ValorApostado * a.OddMomento ELSE 0 END),0) AS MargemPlataforma
    FROM dbo.Jogo j
    LEFT JOIN dbo.Aposta a ON a.JogoId = j.Id
    WHERE j.CodigoJogo = @CodigoJogo
    GROUP BY j.CodigoJogo;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Apostas_EstatisticasPorCompeticao
    @Competicao NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        @Competicao AS Competicao,
        AVG(CAST(r.GolosCasa + r.GolosFora AS DECIMAL(10,2))) AS MediaGolosPorJogo,
        SUM(a.ValorApostado) AS VolumeTotalApostado,
        AVG(CASE WHEN r.GolosCasa > r.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultado1,
        AVG(CASE WHEN r.GolosCasa = r.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultadoX,
        AVG(CASE WHEN r.GolosCasa < r.GolosFora THEN 1.0 ELSE 0 END) AS TaxaResultado2
    FROM dbo.Jogo j
    LEFT JOIN dbo.Resultado r ON r.JogoId = j.Id
    LEFT JOIN dbo.Aposta a ON a.JogoId = j.Id
    WHERE j.Competicao = @Competicao;
END
GO
