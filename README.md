# BetStrike — Parte 1 (Integração REST + SQL Server)

Este repositório já inclui também a base da Parte 2, com middleware assíncrono, replay de eventos, dashboard e alertas.


## Estrutura

- `src/Federation.Results.Api` — API REST da Federação (jogos e resultados)
- `src/BetStrike.Betting.Api` — API REST de gestão de apostas
- `src/Federation.DataGenerator` — aplicação de geração de calendário e simulação em tempo fictício
- `frontend/` — site front end para operações e monitorização
- `database/Apostas` — esquema e stored procedures da BD `Apostas`
- `database/Pagamentos` — esquema e stored procedures da BD `Pagamentos`
- `database/Integration/Triggers` — trigger de integração entre apostas e pagamentos

## Arranque completo com Docker

Para subir a infraestrutura toda com um único comando:

```powershell
docker compose up --build
```

O compose inclui:
- SQL Server com bootstrap automático dos scripts em `database/`;
- RabbitMQ com management UI;
- Kafka com Kafka UI em `http://localhost:8081`;
- `BetStrike.Betting.Api`;
- `Federation.Results.Api`;
- `BetStrike.Middleware.Worker`;
- `BetStrike.Streaming.Worker`;
- `frontend/` servido em `http://localhost:8080`.

O replay é feito a partir do stream persistido em `Apostas.dbo.Evento_Middleware`. O worker grava os eventos, reprocessa historicamente com checkpoint e republica alertas idempotentes via `AlertRaisedEvent`, para que o comportamento se aproxime de uma stream com replay e não apenas de uma fila transitória.

## Princípios aplicados

- Toda a camada de dados usa **Stored Procedures** .
- Validações críticas em **BD** e reforçadas na **API**.
- Fluxo assíncrono para simulação de 9 jogos em paralelo .
- Contratos REST explícitos para sincronização entre plataformas.



## Guia de execução detalhado

Ver [docs/EXECUCAO_PARTE1.md](docs/EXECUCAO_PARTE1.md) para:
- ordem exata de execução dos scripts SQL;
- arranque das APIs com portas fixas;
- teste fim-a-fim com requests HTTP.

### Criação Rápida de Bases de Dados e Arranque de APIs

Para deixar a aplicação pronta para execução:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\preparar_entrega_professor.ps1
```

Depois, abrir o frontend em `http://localhost:8080`.

## Front end

O site está em [frontend/index.html](frontend/index.html) e comunica com as APIs em tempo real.

### Configuração necessária:

**As seguintes APIs devem estar a rodar:**
- `http://localhost:5001` — API da Federação (jogos e resultados)
- `http://localhost:5002` — API de Apostas (gestão de apostas)

**O frontend é servido na porta `8080`:**

```powershell
Set-Location frontend
python -m http.server 8080
```

Depois abre `http://localhost:8080` no browser.

O frontend acede aos dados dos jogos e das apostas através das APIs em 5001 e 5002.

## Parte 2 — Middleware, Streaming e Processamento Assíncrono

### Arquitetura lógica

- Produtores de eventos: `BetStrike.Betting.Api` e `Federation.Results.Api`.
- Camada de filas: `BetStrike.Middleware.Worker` com MassTransit sobre RabbitMQ, retries com intervalos, prioridades e DLQ.
- Camada de streaming: Apache Kafka com topics `bets-events`, `game-events` e `analytics-events`, produzidas pelas APIs e consumidas por `BetStrike.Streaming.Worker`.
- Persistência do stream: `Apostas.dbo.Evento_Middleware` e `Apostas.dbo.Stream_ReplayCheckpoint`.
- Camada analítica: tabelas `Dashboard_*` em SQL Server, recalculadas pelo worker.
- Camada de visualização: endpoint `GET /api/analytics/dashboard` e frontend em tempo quase real.
- Estado do stream: endpoint `GET /api/analytics/stream` com replay checkpoint e sequência do stream.

### O que a solução já cobre

- propagação de eventos quase em tempo real entre serviços;
- desacoplamento entre produtor e consumidor via RabbitMQ e Kafka;
- múltiplos consumidores sobre o mesmo fluxo de eventos;
- processamento assíncrono para alertas e refresh do dashboard;
- retries com intervalos e dead-letter queues;
- replay histórico com checkpoint;
- métricas agregadas para dashboard e alertas automáticos.

### Justificação das tecnologias

- RabbitMQ: escolhido para a camada de filas por ser simples de operar, fácil de containerizar e suportar retries, prioridades e DLQ.
- Kafka: escolhido para streaming near real time por suportar topics persistentes, consumer groups independentes, replay por offsets e elevada taxa de eventos.
- MassTransit: reduz boilerplate no publish/consume e facilita topologias, retries e consumidores múltiplos.
- SQL Server: usado como base operacional e analítica porque o projeto já estava centrado em stored procedures e consistência transacional.
- Worker dedicado: separa o caminho crítico de apostas do caminho analítico e de alertas.

### Requisitos cobertos e pontos a notar

- Filas de mensagens: coberto com RabbitMQ, retries, prioridade e DLQ.
- Streaming de eventos: coberto com Kafka, topics por domínio, producer nas APIs e worker consumer independente.
- Observabilidade básica: coberta com `/health/live` e `/health/ready` nas APIs.
- Dashboards/alertas/analytics: cobertos com snapshot analítico, alertas por evento e frontend.
- Resiliência e reprocessamento: cobertos com checkpoint e replay.
- Testes de carga, falhas e reprocessamento: documentados em [docs/TESTES_PARTE2.md](docs/TESTES_PARTE2.md) e suportados por `scripts/teste_carga_parte2.ps1`.

### Entregáveis da Parte 2

- arquitetura lógica descrita acima;
- justificação das tecnologias nesta secção;
- descrição dos fluxos principais e assíncronos;
- execução em Docker via `docker compose up --build`;
- evidência operacional via dashboard e endpoints de analytics.
