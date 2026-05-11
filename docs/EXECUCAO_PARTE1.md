# Execução — Parte 1

## Criação Rápida de base de Dados 

Para criação de bases de dados e arranque das APIs, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\preparar_entrega_professor.ps1
```

Se preferires a infraestrutura containerizada completa, usa:

```powershell
docker compose up --build
```

Isto sobe automaticamente SQL Server, RabbitMQ, as duas APIs, o worker e o frontend.

O script faz automaticamente:
- criação/atualização de tabelas, SPs e trigger;
- limpeza de dados antigos;
- arranque das APIs (5001 e 5002);
- arranque do worker de middleware para consumo do stream/fila;
- injeção de dados de demo (jogos, utilizadores, apostas e resultados).

Antes de correr a Parte 2, garante também que existe um broker RabbitMQ acessível em `localhost:5672`.
Um arranque local rápido com Docker é:

```powershell
docker run -d --name betstrike-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

No modelo atual, o middleware deixa de depender só de uma recalculação periódica: os eventos são arquivados em `Apostas.dbo.Evento_Middleware`, o replay usa `Stream_ReplayCheckpoint` e os alertas são gerados por evento através de `AlertRaisedEvent`.

No fim, basta servir o frontend:

```powershell
Set-Location frontend
python -m http.server 8080
```

Abrir `http://localhost:8080`.

## 1) Preparar SQL Server (manualmente)



Executar manualmente, com esta ordem:
1. [database/Resultados/Tables/01_tables.sql](database/Resultados/Tables/01_tables.sql)
2. [database/Pagamentos/Tables/01_tables.sql](database/Pagamentos/Tables/01_tables.sql)
3. [database/Apostas/Tables/01_tables.sql](database/Apostas/Tables/01_tables.sql)
4. [database/Resultados/StoredProcedures/02_stored_procedures.sql](database/Resultados/StoredProcedures/02_stored_procedures.sql)
5. [database/Pagamentos/StoredProcedures/02_stored_procedures.sql](database/Pagamentos/StoredProcedures/02_stored_procedures.sql)
6. [database/Apostas/StoredProcedures/02_stored_procedures.sql](database/Apostas/StoredProcedures/02_stored_procedures.sql)
7. [database/Integration/Triggers/01_trigger_aposta_status.sql](database/Integration/Triggers/01_trigger_aposta_status.sql)

### VS Code (extensão SQL Server)

Se estiveres a usar VS Code, executa os scripts **um a um**:

1. Abre o ficheiro SQL.
2. Confirma a ligação no canto inferior direito (instância correta).
3. Clica em `Run Query`.
4. Repete para os 7 ficheiros na ordem acima.
5. No painel de bases, faz `Refresh`.


## 2) Confirmar connection strings

- Resultados API: [src/Federation.Results.Api/appsettings.json](src/Federation.Results.Api/appsettings.json)
- Apostas API: [src/BetStrike.Betting.Api/appsettings.json](src/BetStrike.Betting.Api/appsettings.json)
- Worker de middleware: [src/BetStrike.Middleware.Worker/appsettings.json](src/BetStrike.Middleware.Worker/appsettings.json)

## 3) Arrancar APIs

Com as `launchSettings` já fixadas:
- Resultados API: `http://localhost:5001`
- Apostas API: `http://localhost:5002`
- Worker de middleware: consola em background, sem endpoint HTTP

## 4) Teste fim-a-fim

Usar [tests/e2e_betstrike.http](tests/e2e_betstrike.http) por esta sequência:
1. Criar utilizador (saldo inicial 50€ via `sp_Pagamentos_CriarSaldoInicial`).
2. Inserir jogo na API de Apostas.
3. Registar aposta pendente.
4. Finalizar jogo + resultado.
5. Verificar aposta resolvida (`Estado=2` ganha ou `Estado=3` perdida).
6. Verificar crédito em `Pagamentos.dbo.Transacao` (`PG`) e saldo atualizado em `Pagamentos.dbo.Saldo_Utilizador`.
7. Abrir `http://localhost:5002/api/analytics/dashboard` para ver o snapshot live da Parte 2.


### Demo rápida 

Se quiseres correr uma jornada simulada:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\demo_rapida.ps1
```
