using System.Text.Json;
using BetStrike.Middleware.Contracts;
using MassTransit;

namespace BetStrike.Middleware.Worker;

public sealed class PlatformEventConsumer(
    PlatformEventProcessor processor,
    ILogger<PlatformEventConsumer> logger) :
    IConsumer<GameCatalogPublishedEvent>,
    IConsumer<GameCatalogUpdatedEvent>,
    IConsumer<BetRegisteredEvent>,
    IConsumer<BetStatusChangedEvent>,
    IConsumer<UserCreatedEvent>,
    IConsumer<ResultRecordedEvent>
{
    public Task Consume(ConsumeContext<GameCatalogPublishedEvent> context)
        => HandleAsync(context.Message, context, "game published");

    public Task Consume(ConsumeContext<GameCatalogUpdatedEvent> context)
        => HandleAsync(context.Message, context, "game updated");

    public Task Consume(ConsumeContext<BetRegisteredEvent> context)
        => HandleAsync(context.Message, context, "bet registered");

    public Task Consume(ConsumeContext<BetStatusChangedEvent> context)
        => HandleAsync(context.Message, context, "bet status changed");

    public Task Consume(ConsumeContext<UserCreatedEvent> context)
        => HandleAsync(context.Message, context, "user created");

    public Task Consume(ConsumeContext<ResultRecordedEvent> context)
        => HandleAsync(context.Message, context, "result recorded");

    private async Task HandleAsync<T>(T message, ConsumeContext<T> context, string label)
        where T : class, IPlatformEvent
    {
        try
        {
            await processor.ProcessLiveAsync(message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process {Label} event {EventId}", label, message.EventId);
            throw;
        }
    }
}

public sealed class AlertRaisedEventConsumer(AnalyticsRepository repository, ILogger<AlertRaisedEventConsumer> logger) : IConsumer<AlertRaisedEvent>
{
    public async Task Consume(ConsumeContext<AlertRaisedEvent> context)
    {
        try
        {
            await repository.RegisterAlertAsync(context.Message, context.Message.SourceEventId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist alert {EventId}", context.Message.EventId);
            throw;
        }
    }
}

public sealed class DashboardRefreshConsumer(AnalyticsRepository repository, ILogger<DashboardRefreshConsumer> logger) :
    IConsumer<HighPriorityDashboardRefreshRequestedEvent>,
    IConsumer<LowPriorityDashboardRefreshRequestedEvent>
{
    public Task Consume(ConsumeContext<HighPriorityDashboardRefreshRequestedEvent> context)
        => RefreshAsync("high", context);

    public Task Consume(ConsumeContext<LowPriorityDashboardRefreshRequestedEvent> context)
        => RefreshAsync("low", context);

    private async Task RefreshAsync(string priority, ConsumeContext context)
    {
        try
        {
            await repository.RebuildDashboardAsync(context.CancellationToken);
            logger.LogInformation("Dashboard recalculated from {Priority} priority queue.", priority);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dashboard refresh failed from {Priority} priority queue.", priority);
            throw;
        }
    }
}

public sealed class PlatformEventProcessor(
    AnalyticsRepository repository,
    IPublishEndpoint publishEndpoint,
    ILogger<PlatformEventProcessor> logger)
{
    public async Task ProcessLiveAsync<T>(T message, CancellationToken ct)
        where T : class, IPlatformEvent
    {
        await repository.RegisterEventAsync(message, JsonSerializer.Serialize(message), ct);

        foreach (var alert in await BuildAlertsAsync(message, ct))
        {
            await publishEndpoint.Publish(alert, ct);
        }

        await PublishRefreshAsync(message, ct);
    }

    public async Task ReplayAsync(ArchivedPlatformEvent archivedEvent, CancellationToken ct)
    {
        var message = Deserialize(archivedEvent.EventType, archivedEvent.PayloadJson);
        if (message is null)
        {
            logger.LogWarning("Unable to replay archived event {EventSequence} ({EventType}).", archivedEvent.EventSequence, archivedEvent.EventType);
            return;
        }

        foreach (var alert in await BuildAlertsAsync(message, ct))
        {
            await publishEndpoint.Publish(alert, ct);
        }
    }

    private async Task PublishRefreshAsync(IPlatformEvent message, CancellationToken ct)
    {
        var highPriority = message switch
        {
            GameCatalogUpdatedEvent updated => updated.Estado is 2 or 3 or 4 or 5,
            BetRegisteredEvent => true,
            BetStatusChangedEvent => true,
            ResultRecordedEvent => true,
            _ => false
        };

        if (highPriority)
        {
            await publishEndpoint.Publish(
                new HighPriorityDashboardRefreshRequestedEvent(Guid.NewGuid(), "middleware.worker", "dashboard", "global", DateTime.UtcNow, "event stream update", message.EventId),
                ct);
        }
        else
        {
            await publishEndpoint.Publish(
                new LowPriorityDashboardRefreshRequestedEvent(Guid.NewGuid(), "middleware.worker", "dashboard", "global", DateTime.UtcNow, "event stream update", message.EventId),
                ct);
        }
    }

    private async Task<List<AlertRaisedEvent>> BuildAlertsAsync(IPlatformEvent message, CancellationToken ct)
    {
        var alerts = new List<AlertRaisedEvent>();

        switch (message)
        {
            case BetRegisteredEvent bet:
            {
                var contexto = await repository.ObterContextoJogoAsync(bet.JogoId, ct);
                if (contexto is null)
                {
                    break;
                }

                if (bet.ValorApostado >= 250m || contexto.VolumeTotalApostado >= 1500m)
                {
                    alerts.Add(new AlertRaisedEvent(
                        Guid.NewGuid(),
                        "middleware.worker",
                        "alert",
                        contexto.CodigoJogo,
                        DateTime.UtcNow,
                        "Alta",
                        $"Exposição elevada no jogo {contexto.CodigoJogo}: {contexto.VolumeTotalApostado:N2} EUR.",
                        bet.EventId,
                        contexto.Competicao));
                }
                else if (bet.ValorApostado >= 100m)
                {
                    alerts.Add(new AlertRaisedEvent(
                        Guid.NewGuid(),
                        "middleware.worker",
                        "alert",
                        contexto.CodigoJogo,
                        DateTime.UtcNow,
                        "Média",
                        $"Aposta individual acima do limiar no jogo {contexto.CodigoJogo}: {bet.ValorApostado:N2} EUR.",
                        bet.EventId,
                        contexto.Competicao));
                }

                break;
            }
            case ResultRecordedEvent result:
            {
                var totalGolos = result.GolosCasa + result.GolosFora;
                var diferenca = Math.Abs(result.GolosCasa - result.GolosFora);

                if (totalGolos >= 6 || diferenca >= 4)
                {
                    alerts.Add(new AlertRaisedEvent(
                        Guid.NewGuid(),
                        "middleware.worker",
                        "alert",
                        result.AggregateKey,
                        DateTime.UtcNow,
                        diferenca >= 4 ? "Alta" : "Média",
                        $"Resultado anómalo no jogo {result.JogoId}: {result.GolosCasa}-{result.GolosFora}.",
                        result.EventId,
                        null));
                }

                break;
            }
            case GameCatalogUpdatedEvent game when game.Estado is 4 or 5:
            {
                alerts.Add(new AlertRaisedEvent(
                    Guid.NewGuid(),
                    "middleware.worker",
                    "alert",
                    game.AggregateKey,
                    DateTime.UtcNow,
                    "Baixa",
                    $"Jogo {game.CodigoJogo} alterado para o estado {game.Estado}.",
                    game.EventId,
                    null));
                break;
            }
        }

        return alerts;
    }

    private static IPlatformEvent? Deserialize(string eventType, string payloadJson)
        => eventType switch
        {
            nameof(GameCatalogPublishedEvent) => JsonSerializer.Deserialize<GameCatalogPublishedEvent>(payloadJson),
            nameof(GameCatalogUpdatedEvent) => JsonSerializer.Deserialize<GameCatalogUpdatedEvent>(payloadJson),
            nameof(GameCatalogDeletedEvent) => JsonSerializer.Deserialize<GameCatalogDeletedEvent>(payloadJson),
            nameof(BetRegisteredEvent) => JsonSerializer.Deserialize<BetRegisteredEvent>(payloadJson),
            nameof(BetStatusChangedEvent) => JsonSerializer.Deserialize<BetStatusChangedEvent>(payloadJson),
            nameof(UserCreatedEvent) => JsonSerializer.Deserialize<UserCreatedEvent>(payloadJson),
            nameof(ResultRecordedEvent) => JsonSerializer.Deserialize<ResultRecordedEvent>(payloadJson),
            _ => null
        };
}

public sealed class HistoricalReplayService(AnalyticsRepository repository, PlatformEventProcessor processor, ILogger<HistoricalReplayService> logger) : IHostedService
{
    private const string ReplayName = "middleware.worker";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var checkpoint = await repository.GetReplayCheckpointAsync(ReplayName, cancellationToken);
        var archivedEvents = await repository.ListArchivedEventsAsync(checkpoint, cancellationToken);
        if (archivedEvents.Count == 0)
        {
            logger.LogInformation("No archived events to replay.");
            return;
        }

        foreach (var archivedEvent in archivedEvents)
        {
            await processor.ReplayAsync(archivedEvent, cancellationToken);
        }

        await repository.UpdateReplayCheckpointAsync(ReplayName, archivedEvents[^1].EventSequence, cancellationToken);
        await repository.RebuildDashboardAsync(cancellationToken);
        logger.LogInformation("Historical replay completed for {Count} events.", archivedEvents.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}