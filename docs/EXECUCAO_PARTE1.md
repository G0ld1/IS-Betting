# Execução — Parte 1

## 1) Preparar SQL Server

Opção recomendada: abrir e executar [database/00_Execucao_Ordem.sql](database/00_Execucao_Ordem.sql) com SQLCMD Mode ativo no SSMS.

Se executar manualmente, usar esta ordem:
1. [database/Resultados/Tables/01_tables.sql](database/Resultados/Tables/01_tables.sql)
2. [database/Pagamentos/Tables/01_tables.sql](database/Pagamentos/Tables/01_tables.sql)
3. [database/Apostas/Tables/01_tables.sql](database/Apostas/Tables/01_tables.sql)
4. [database/Resultados/StoredProcedures/02_stored_procedures.sql](database/Resultados/StoredProcedures/02_stored_procedures.sql)
5. [database/Pagamentos/StoredProcedures/02_stored_procedures.sql](database/Pagamentos/StoredProcedures/02_stored_procedures.sql)
6. [database/Apostas/StoredProcedures/02_stored_procedures.sql](database/Apostas/StoredProcedures/02_stored_procedures.sql)
7. [database/Integration/Triggers/01_trigger_aposta_status.sql](database/Integration/Triggers/01_trigger_aposta_status.sql)

### VS Code (extensão SQL Server)

Se estiveres a usar VS Code, executa os scripts **um a um** (o ficheiro com `:r` pode não funcionar fora do SSMS):

1. Abre o ficheiro SQL.
2. Confirma a ligação no canto inferior direito (instância correta).
3. Clica em `Run Query`.
4. Repete para os 7 ficheiros na ordem acima.
5. No painel de bases, faz `Refresh`.

Validação rápida (executar numa query):

```sql
SELECT @@SERVERNAME AS ServerName, DB_NAME() AS CurrentDb;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM Apostas.INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM Pagamentos.INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM ResultadosFutebol.INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
```

## 2) Confirmar connection strings

- Resultados API: [src/Federation.Results.Api/appsettings.json](src/Federation.Results.Api/appsettings.json)
- Apostas API: [src/BetStrike.Betting.Api/appsettings.json](src/BetStrike.Betting.Api/appsettings.json)

## 3) Arrancar APIs

Com as `launchSettings` já fixadas:
- Resultados API: `http://localhost:5001`
- Apostas API: `http://localhost:5002`

## 4) Teste fim-a-fim

Usar [tests/e2e_betstrike.http](tests/e2e_betstrike.http) por esta sequência:
1. Criar utilizador (saldo inicial 50€ via `sp_Pagamentos_CriarSaldoInicial`).
2. Inserir jogo na API de Apostas.
3. Registar aposta pendente.
4. Finalizar jogo + resultado.
5. Verificar aposta resolvida (`Estado=2` ganha ou `Estado=3` perdida).
6. Verificar crédito em `Pagamentos.dbo.Transacao` (`PG`) e saldo atualizado em `Pagamentos.dbo.Saldo_Utilizador`.

## 5) Simulação automática (opcional)

Executar `Federation.DataGenerator` com variável de ambiente:
- `RESULTS_API_BASE=http://localhost:5001`

A app publica calendário e depois simula 9 jogos em paralelo com atualização de 10 em 10 segundos.
