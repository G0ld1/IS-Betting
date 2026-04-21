/*
  Limpeza de dados para preparar demonstração.
  Mantém schema, stored procedures e triggers.
  Remove apenas registos e faz reset de identities.
*/

SET NOCOUNT ON;

PRINT '=== Limpeza ResultadosFutebol ===';
USE ResultadosFutebol;
GO

DELETE FROM dbo.Jogo;
DBCC CHECKIDENT ('dbo.Jogo', RESEED, 0);
GO

PRINT '=== Limpeza Apostas ===';
USE Apostas;
GO

DELETE FROM dbo.Resultado;
DELETE FROM dbo.Aposta;
DELETE FROM dbo.Jogo;
DELETE FROM dbo.Utilizador;

DBCC CHECKIDENT ('dbo.Resultado', RESEED, 0);
DBCC CHECKIDENT ('dbo.Aposta', RESEED, 0);
DBCC CHECKIDENT ('dbo.Jogo', RESEED, 0);
DBCC CHECKIDENT ('dbo.Utilizador', RESEED, 0);
GO

PRINT '=== Limpeza Pagamentos ===';
USE Pagamentos;
GO

DELETE FROM dbo.Transacao;
DELETE FROM dbo.Saldo_Utilizador;

DBCC CHECKIDENT ('dbo.Transacao', RESEED, 0);
GO

PRINT '=== Validação final ===';
SELECT 'ResultadosFutebol.dbo.Jogo' AS Tabela, COUNT(*) AS Total FROM ResultadosFutebol.dbo.Jogo
UNION ALL
SELECT 'Apostas.dbo.Jogo', COUNT(*) FROM Apostas.dbo.Jogo
UNION ALL
SELECT 'Apostas.dbo.Aposta', COUNT(*) FROM Apostas.dbo.Aposta
UNION ALL
SELECT 'Apostas.dbo.Resultado', COUNT(*) FROM Apostas.dbo.Resultado
UNION ALL
SELECT 'Apostas.dbo.Utilizador', COUNT(*) FROM Apostas.dbo.Utilizador
UNION ALL
SELECT 'Pagamentos.dbo.Saldo_Utilizador', COUNT(*) FROM Pagamentos.dbo.Saldo_Utilizador
UNION ALL
SELECT 'Pagamentos.dbo.Transacao', COUNT(*) FROM Pagamentos.dbo.Transacao;
GO
