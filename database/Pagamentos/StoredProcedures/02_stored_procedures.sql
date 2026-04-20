USE Pagamentos;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pagamentos_CriarSaldoInicial
    @UtilizadorId INT,
    @SaldoInicial DECIMAL(12,2) = 50.00
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Saldo_Utilizador WHERE UtilizadorId = @UtilizadorId)
        THROW 52000, 'Saldo já existe para utilizador.', 1;

    INSERT INTO dbo.Saldo_Utilizador (UtilizadorId, SaldoAtual)
    VALUES (@UtilizadorId, @SaldoInicial);

    INSERT INTO dbo.Transacao (ApostaId, UtilizadorId, Tipo, Valor, Estado)
    VALUES (NULL, @UtilizadorId, 'DE', @SaldoInicial, 'Processada');
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pagamentos_DebitarAposta
    @ApostaId INT,
    @UtilizadorId INT,
    @Valor DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @SaldoAtual DECIMAL(12,2);
    SELECT @SaldoAtual = SaldoAtual
    FROM dbo.Saldo_Utilizador WITH (UPDLOCK, ROWLOCK)
    WHERE UtilizadorId = @UtilizadorId;

    IF @SaldoAtual IS NULL
        THROW 52001, 'Utilizador sem saldo inicial.', 1;

    IF @SaldoAtual < @Valor
        THROW 52002, 'Saldo insuficiente.', 1;

    UPDATE dbo.Saldo_Utilizador
    SET SaldoAtual = SaldoAtual - @Valor,
        UltimaAtualizacaoUtc = SYSUTCDATETIME()
    WHERE UtilizadorId = @UtilizadorId;

    INSERT INTO dbo.Transacao (ApostaId, UtilizadorId, Tipo, Valor, Estado)
    VALUES (@ApostaId, @UtilizadorId, 'AP', @Valor, 'Processada');

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pagamentos_CreditarPremio
    @ApostaId INT,
    @UtilizadorId INT,
    @Valor DECIMAL(12,2),
    @Tipo CHAR(2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Tipo NOT IN ('PG','RE')
        THROW 52003, 'Tipo de crédito inválido.', 1;

    BEGIN TRAN;

    UPDATE dbo.Saldo_Utilizador
    SET SaldoAtual = SaldoAtual + @Valor,
        UltimaAtualizacaoUtc = SYSUTCDATETIME()
    WHERE UtilizadorId = @UtilizadorId;

    INSERT INTO dbo.Transacao (ApostaId, UtilizadorId, Tipo, Valor, Estado)
    VALUES (@ApostaId, @UtilizadorId, @Tipo, @Valor, 'Processada');

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Pagamentos_DepositoFicticio
    @UtilizadorId INT,
    @Valor DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Valor <= 0
        THROW 52004, 'Valor inválido.', 1;

    UPDATE dbo.Saldo_Utilizador
    SET SaldoAtual = SaldoAtual + @Valor,
        UltimaAtualizacaoUtc = SYSUTCDATETIME()
    WHERE UtilizadorId = @UtilizadorId;

    INSERT INTO dbo.Transacao (ApostaId, UtilizadorId, Tipo, Valor, Estado)
    VALUES (NULL, @UtilizadorId, 'DE', @Valor, 'Processada');
END
GO
