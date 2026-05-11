USE Apostas;
GO

IF OBJECT_ID('dbo.Evento_Middleware', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Evento_Middleware
    (
        EventId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Evento_Middleware PRIMARY KEY,
        EventSequence BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT UQ_Evento_Middleware_Sequence UNIQUE,
        EventType NVARCHAR(120) NOT NULL,
        SourceSystem NVARCHAR(80) NOT NULL,
        AggregateType NVARCHAR(80) NOT NULL,
        AggregateKey NVARCHAR(120) NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        CriadoEmUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Evento_Middleware_Criado DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Dashboard_Resumo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dashboard_Resumo
    (
        Id BIT NOT NULL CONSTRAINT PK_Dashboard_Resumo PRIMARY KEY CONSTRAINT CK_Dashboard_Resumo_Id CHECK (Id = 1),
        JogosAtivos INT NOT NULL,
        ApostasPendentes INT NOT NULL,
        ApostasGanhas INT NOT NULL,
        VolumeTotalApostado DECIMAL(18,2) NOT NULL,
        MargemPlataforma DECIMAL(18,2) NOT NULL,
        UtilizadoresAtivos INT NOT NULL,
        ApostasPorMinuto DECIMAL(10,2) NOT NULL,
        MediaMovel15MinVolume DECIMAL(18,2) NOT NULL,
        MaxVolumeHora DECIMAL(18,2) NOT NULL,
        MinVolumeHora DECIMAL(18,2) NOT NULL,
        AtualizadoUtc DATETIME2(0) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Dashboard_Competicao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dashboard_Competicao
    (
        Competicao NVARCHAR(100) NOT NULL CONSTRAINT PK_Dashboard_Competicao PRIMARY KEY,
        MediaGolosPorJogo DECIMAL(10,2) NULL,
        VolumeTotalApostado DECIMAL(18,2) NOT NULL,
        TaxaResultado1 DECIMAL(6,4) NULL,
        TaxaResultadoX DECIMAL(6,4) NULL,
        TaxaResultado2 DECIMAL(6,4) NULL,
        AtualizadoUtc DATETIME2(0) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Dashboard_JanelaTemporal', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dashboard_JanelaTemporal
    (
        JanelaInicioUtc DATETIME2(0) NOT NULL CONSTRAINT PK_Dashboard_JanelaTemporal PRIMARY KEY,
        Apostas INT NOT NULL,
        VolumeTotal DECIMAL(18,2) NOT NULL,
        MediaAposta DECIMAL(18,2) NOT NULL,
        MaxAposta DECIMAL(18,2) NOT NULL,
        MinAposta DECIMAL(18,2) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Dashboard_ExposicaoJogo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dashboard_ExposicaoJogo
    (
        JogoId INT NOT NULL CONSTRAINT PK_Dashboard_ExposicaoJogo PRIMARY KEY,
        CodigoJogo VARCHAR(20) NOT NULL,
        Competicao NVARCHAR(100) NOT NULL,
        ApostasPendentes INT NOT NULL,
        VolumeTotal DECIMAL(18,2) NOT NULL,
        ExposicaoLiquida DECIMAL(18,2) NOT NULL,
        AtualizadoUtc DATETIME2(0) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Dashboard_TipoAposta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Dashboard_TipoAposta
    (
        TipoAposta CHAR(1) NOT NULL CONSTRAINT PK_Dashboard_TipoAposta PRIMARY KEY,
        TotalApostas INT NOT NULL,
        ApostasGanhas INT NOT NULL,
        ApostasPerdidas INT NOT NULL,
        TaxaVitoria DECIMAL(6,4) NOT NULL,
        VolumeTotal DECIMAL(18,2) NOT NULL,
        AtualizadoUtc DATETIME2(0) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Alerta_Middleware', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Alerta_Middleware
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Alerta_Middleware PRIMARY KEY,
        Severidade NVARCHAR(20) NOT NULL,
        Mensagem NVARCHAR(400) NOT NULL,
        Competicao NVARCHAR(100) NULL,
        SourceEventId UNIQUEIDENTIFIER NULL,
        CriadoEmUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Alerta_Middleware_Criado DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Alerta_Middleware_SourceEventId' AND object_id = OBJECT_ID('dbo.Alerta_Middleware'))
BEGIN
    CREATE UNIQUE INDEX UX_Alerta_Middleware_SourceEventId
        ON dbo.Alerta_Middleware(SourceEventId)
        WHERE SourceEventId IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.Stream_ReplayCheckpoint', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stream_ReplayCheckpoint
    (
        ReplayName NVARCHAR(80) NOT NULL CONSTRAINT PK_Stream_ReplayCheckpoint PRIMARY KEY,
        LastEventSequence BIGINT NOT NULL,
        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Stream_ReplayCheckpoint_Updated DEFAULT(SYSUTCDATETIME())
    );
END
GO