IF DB_ID('Pagamentos') IS NULL
    CREATE DATABASE Pagamentos;
GO

USE Pagamentos;
GO

IF OBJECT_ID('dbo.Saldo_Utilizador', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Saldo_Utilizador
    (
        UtilizadorId INT NOT NULL PRIMARY KEY,
        SaldoAtual DECIMAL(12,2) NOT NULL,
        UltimaAtualizacaoUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Saldo_Ultima DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT CK_Saldo_NaoNegativo CHECK (SaldoAtual >= 0)
    );
END
GO

IF OBJECT_ID('dbo.Transacao', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transacao
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ApostaId INT NULL,
        UtilizadorId INT NOT NULL,
        Tipo CHAR(2) NOT NULL,
        Valor DECIMAL(12,2) NOT NULL,
        DataHoraUtc DATETIME2(0) NOT NULL CONSTRAINT DF_Transacao_Data DEFAULT(SYSUTCDATETIME()),
        Estado NVARCHAR(20) NOT NULL,
        CONSTRAINT CK_Transacao_Tipo CHECK (Tipo IN ('AP','PG','RE','DE','LV')),
        CONSTRAINT CK_Transacao_Estado CHECK (Estado IN ('Pendente','Processada','Falhada','Reembolsada'))
    );
END
GO
