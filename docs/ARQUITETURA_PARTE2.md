# BetStrike - Arquitetura Parte 2

## 1. Visao geral

A Parte 2 acrescenta duas camadas de middleware a solucao base:

- **RabbitMQ + MassTransit** para filas de mensagens, trabalho em background, retries, prioridades e dead-letter queues.
- **Apache Kafka** para streaming de eventos near real time, consumo por multiplos subsistemas e replay por offsets.

O objetivo e separar o caminho transacional das APIs do caminho analitico e assincrono.

## 2. Arquitetura logica

```text
Betting API  ─┬─ publica eventos em RabbitMQ ──> Middleware Worker ──> SQL Analytics / Alertas
              └─ publica eventos em Kafka    ──> Streaming Worker   ──> SQL Analytics / Alertas

Results API  ─┬─ publica eventos em RabbitMQ ──> Middleware Worker
              └─ publica eventos em Kafka    ──> Streaming Worker

Frontend ─────── consulta Betting API ────────> /api/analytics/dashboard
```

Componentes:

| Componente | Funcao |
|-----------|--------|
| `BetStrike.Betting.Api` | API operacional de jogos, apostas, utilizadores, resultados e analytics. |
| `Federation.Results.Api` | API externa/federada de jogos e resultados. |
| `BetStrike.Middleware.Worker` | Consumidor RabbitMQ para processamento assincrono, prioridades, alertas, dashboard e replay persistido. |
| `BetStrike.Streaming.Worker` | Consumidor Kafka para eventos near real time e atualizacao analitica independente. |
| SQL Server | Base operacional e tabelas analiticas `Evento_Middleware`, `Stream_ReplayCheckpoint`, `Dashboard_*` e `Alerta_Middleware`. |
| Frontend | Dashboard operacional em tempo quase real por polling da API. |

## 3. RabbitMQ - camada de filas

Tecnologia escolhida: **RabbitMQ**, com **MassTransit**.

Filas usadas:

| Fila | Objetivo |
|------|----------|
| `betstrike.analytics.stream` | Processamento assincrono de eventos de plataforma. |
| `betstrike.analytics.stream.dlq` | Dead-letter queue para eventos que falham apos retries/redelivery. |
| `betstrike.analytics.high` | Refresh de dashboard de alta prioridade. |
| `betstrike.analytics.high.dlq` | DLQ da fila de alta prioridade. |
| `betstrike.analytics.low` | Refresh de dashboard de baixa prioridade. |
| `betstrike.analytics.low.dlq` | DLQ da fila de baixa prioridade. |

Caracteristicas implementadas:

- retries configurados por fila;
- delayed redelivery;
- prefetch diferenciado por prioridade;
- in-memory outbox;
- dead-letter queues;
- consumidores desacoplados dos produtores.

## 4. Kafka - camada de streaming

Tecnologia escolhida: **Apache Kafka**.

Topics usadas:

| Topic | Eventos |
|-------|---------|
| `bets-events` | `BetRegisteredEvent`, `BetStatusChangedEvent`. |
| `game-events` | `GameCatalogPublishedEvent`, `GameCatalogUpdatedEvent`, `GameCatalogDeletedEvent`, `ResultRecordedEvent`. |
| `analytics-events` | Eventos analiticos futuros ou genericos. |

Caracteristicas implementadas:

- producers nas APIs;
- consumer dedicado em `BetStrike.Streaming.Worker`;
- consumer group `betstrike-streaming-analytics`;
- offsets geridos pelo Kafka;
- processamento idempotente no SQL atraves de `EventId`;
- replay operacional por reposicionamento de offsets/consumer group.

## 5. Dashboard, alertas e analytics

Endpoints principais:

| Endpoint | Objetivo |
|----------|----------|
| `GET /api/analytics/dashboard` | Snapshot analitico para o frontend. |
| `GET /api/analytics/alerts` | Alertas automaticos recentes. |
| `GET /api/analytics/events` | Eventos arquivados no stream persistido. |
| `GET /api/analytics/stream` | Estado do stream persistido e checkpoint de replay. |

Metricas calculadas:

- numero de jogos ativos;
- apostas pendentes e ganhas;
- volume total apostado;
- margem da plataforma;
- utilizadores ativos;
- apostas por minuto;
- media movel de volume em janelas de 15 minutos;
- maximo/minimo por hora;
- volume por competicao;
- exposicao financeira por jogo;
- taxa de vitoria por tipo de aposta.

Alertas implementados:

- aposta individual acima de limiar;
- exposicao elevada num jogo;
- resultado anomalo por muitos golos ou diferenca elevada.

## 6. Fluxos principais

### Registo de aposta

```text
1. Cliente regista aposta na Betting API.
2. Betting API persiste a aposta no SQL Server.
3. Betting API publica `BetRegisteredEvent` em RabbitMQ.
4. Betting API publica o mesmo evento em Kafka topic `bets-events`.
5. Middleware Worker e Streaming Worker processam de forma independente.
6. Dashboard e alertas sao atualizados sem bloquear a resposta da API.
```

### Atualizacao de jogo/resultado

```text
1. Betting API ou Results API atualiza jogo/resultado.
2. API publica evento de jogo em RabbitMQ e Kafka.
3. Workers atualizam o arquivo de eventos e recalculam analytics.
4. Frontend obtem o novo estado em `/api/analytics/dashboard`.
```

### Reprocessamento

```text
1. Eventos RabbitMQ processados sao arquivados em `Evento_Middleware`.
2. O checkpoint em `Stream_ReplayCheckpoint` permite replay controlado do worker RabbitMQ.
3. No Kafka, o consumer group permite retomar ou repetir consumo por offsets.
4. O SQL evita duplicados por `EventId` e `SourceEventId`.
```

## 7. Requisitos nao funcionais

| Requisito | Implementacao |
|-----------|---------------|
| Baixo acoplamento | APIs publicam eventos sem conhecer consumidores. |
| Recuperacao apos falhas | Retries, delayed redelivery, DLQ e offsets Kafka. |
| Reprocessamento | Checkpoint SQL e consumer groups Kafka. |
| Picos de carga | Kafka para alto volume e RabbitMQ com prefetch/prioridades. |
| Near real time | Eventos processados por workers em background. |
| Separacao transacional/analitica | APIs persistem operacoes; workers atualizam analytics. |
| Multiplos consumidores | RabbitMQ consumers e Kafka consumer groups independentes. |

## 8. Execucao containerizada

```powershell
docker compose up --build
```

Servicos:

| Servico | Porta |
|---------|-------|
| Frontend | `http://localhost:8080` |
| Betting API | `http://localhost:5002` |
| Results API | `http://localhost:5001` |
| RabbitMQ Management | `http://localhost:15672` |
| Kafka | `localhost:9092` |
| Kafka UI | `http://localhost:8081` |
| SQL Server | `localhost:1433` |

## 9. Evidencia de testes

Ver [TESTES_PARTE2.md](TESTES_PARTE2.md).

Teste rapido:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\teste_carga_parte2.ps1 -Apostas 100
```

Este script gera eventos, cria carga, ativa alertas e permite confirmar o processamento no dashboard, no Kafka UI e no RabbitMQ Management.
