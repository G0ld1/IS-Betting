/*
  Executar em SSMS com SQLCMD Mode ativo.
  Ordem obrigatória para cumprir dependências entre bases e objetos.
*/

:r .\Resultados\Tables\01_tables.sql
:r .\Pagamentos\Tables\01_tables.sql
:r .\Apostas\Tables\01_tables.sql

:r .\Resultados\StoredProcedures\02_stored_procedures.sql
:r .\Pagamentos\StoredProcedures\02_stored_procedures.sql
:r .\Apostas\StoredProcedures\02_stored_procedures.sql

:r .\Integration\Triggers\01_trigger_aposta_status.sql
