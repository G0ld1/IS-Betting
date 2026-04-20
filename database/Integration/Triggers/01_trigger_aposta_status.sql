USE Apostas;
GO

CREATE OR ALTER TRIGGER dbo.trg_Aposta_IntegracaoPagamentos
ON dbo.Aposta
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Mudancas AS
    (
        SELECT
            i.Id AS ApostaId,
            i.UtilizadorId,
            i.Estado AS NovoEstado,
            i.ValorApostado,
            i.OddMomento
        FROM inserted i
        INNER JOIN deleted d ON d.Id = i.Id
        WHERE i.Estado <> d.Estado
          AND i.Estado IN (2,4)
    )
    SELECT * INTO #Mudancas FROM Mudancas;

    DECLARE @ApostaId INT, @UtilizadorId INT, @NovoEstado INT, @Valor DECIMAL(12,2), @Tipo CHAR(2);

    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            ApostaId,
            UtilizadorId,
            NovoEstado,
            CASE WHEN NovoEstado = 2 THEN ValorApostado * OddMomento ELSE ValorApostado END AS Valor
        FROM #Mudancas;

    OPEN c;
    FETCH NEXT FROM c INTO @ApostaId, @UtilizadorId, @NovoEstado, @Valor;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Tipo = CASE WHEN @NovoEstado = 2 THEN 'PG' ELSE 'RE' END;

        EXEC Pagamentos.dbo.sp_Pagamentos_CreditarPremio
            @ApostaId = @ApostaId,
            @UtilizadorId = @UtilizadorId,
            @Valor = @Valor,
            @Tipo = @Tipo;

        FETCH NEXT FROM c INTO @ApostaId, @UtilizadorId, @NovoEstado, @Valor;
    END

    CLOSE c;
    DEALLOCATE c;

    DROP TABLE #Mudancas;
END
GO
