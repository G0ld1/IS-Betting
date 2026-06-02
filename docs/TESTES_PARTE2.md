# Testes Parte 2 - Carga, Falhas e Reprocessamento

## Arranque

```powershell
docker compose up --build
```

Servicos relevantes:
- Frontend: `http://localhost:8080`
- Betting API: `http://localhost:5002`
- Results API: `http://localhost:5001`
- RabbitMQ Management: `http://localhost:15672`
- Kafka UI: `http://localhost:8081`

## Teste de carga

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\teste_carga_parte2.ps1 -Apostas 100
```

O teste cria um utilizador, cria um jogo e regista varias apostas. Algumas apostas ultrapassam o limiar de alerta para demonstrar tratamento automatico de anomalias.

Evidencias esperadas:
- `GET http://localhost:5002/api/analytics/dashboard` mostra aumento de volume, apostas por minuto e alertas.
- `GET http://localhost:5002/api/analytics/events` lista eventos arquivados em `Evento_Middleware`.
- Kafka UI mostra mensagens nas topics `bets-events` e `game-events`.
- RabbitMQ mostra as filas de analytics, prioridade e DLQ criadas pelo worker MassTransit.

## Teste de falha temporaria

1. Pare o consumer Kafka:

```powershell
docker compose stop streaming-worker
```

2. Gere carga enquanto o consumer esta em baixo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\teste_carga_parte2.ps1 -Apostas 50
```

3. Volte a iniciar o consumer:

```powershell
docker compose start streaming-worker
```

Evidencia esperada: o worker Kafka retoma a partir dos offsets do consumer group e processa eventos pendentes sem bloquear as APIs.

## Teste de reprocessamento

O RabbitMQ worker mantem checkpoint em `Apostas.dbo.Stream_ReplayCheckpoint` e o stream persistido em `Apostas.dbo.Evento_Middleware`.

Para forcar replay controlado:

```sql
USE Apostas;
UPDATE dbo.Stream_ReplayCheckpoint
SET LastEventSequence = 0,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE ReplayName = 'middleware.worker';
```

Depois reinicie:

```powershell
docker compose restart middleware-worker
```

Evidencia esperada: o worker relê eventos arquivados, recalcula o dashboard e evita alertas duplicados atraves de `SourceEventId`.
