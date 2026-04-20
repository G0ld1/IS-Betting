IF DB_ID('Apostas') IS NULL
    CREATE DATABASE Apostas;
GO

USE Apostas;
GO

IF OBJECT_ID('dbo.Jogo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Jogo
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CodigoJogo VARCHAR(20) NOT NULL UNIQUE,
        DataJogo DATE NOT NULL,
        HoraInicio TIME(0) NOT NULL,
        EquipaCasa NVARCHAR(100) NOT NULL,
        EquipaFora NVARCHAR(100) NOT NULL,
        Competicao NVARCHAR(100) NOT NULL,
        Estado INT NOT NULL,
        CONSTRAINT CK_Apostas_JogoEstado CHECK (Estado BETWEEN 1 AND 5)
    );
END
GO

IF OBJECT_ID('dbo.Resultado', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resultado
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        JogoId INT NOT NULL UNIQUE,
        GolosCasa INT NOT NULL,
        GolosFora INT NOT NULL,
        Desconhecido BIT NOT NULL CONSTRAINT DF_Resultado_Desconhecido DEFAULT(0),
        UltimaAtualizacaoUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Resultado_Ultima DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Resultado_Jogo FOREIGN KEY (JogoId) REFERENCES dbo.Jogo(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.Utilizador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Utilizador
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome NVARCHAR(120) NOT NULL,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        CriadoEmUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Utilizador_Criado DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Aposta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Aposta
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        JogoId INT NOT NULL,
        UtilizadorId INT NOT NULL,
        TipoAposta CHAR(1) NOT NULL,
        ValorApostado DECIMAL(12,2) NOT NULL,
        OddMomento DECIMAL(8,2) NOT NULL,
        Estado INT NOT NULL CONSTRAINT DF_Aposta_Estado DEFAULT(1),
        DataHoraUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Aposta_Data DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT FK_Aposta_Jogo FOREIGN KEY (JogoId) REFERENCES dbo.Jogo(Id),
        CONSTRAINT FK_Aposta_Utilizador FOREIGN KEY (UtilizadorId) REFERENCES dbo.Utilizador(Id),
        CONSTRAINT CK_Aposta_Tipo CHECK (TipoAposta IN ('1','X','2')),
        CONSTRAINT CK_Aposta_Valor CHECK (ValorApostado > 0),
        CONSTRAINT CK_Aposta_Odd CHECK (OddMomento > 1.0),
        CONSTRAINT CK_Aposta_Estado CHECK (Estado BETWEEN 1 AND 4)
    );
END
GO
