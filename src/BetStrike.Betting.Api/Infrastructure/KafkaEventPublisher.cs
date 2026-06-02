using System.Text.Json;
using System.Text.Json.Nodes;
using BetStrike.Middleware.Contracts;
using Confluent.Kafka;

namespace BetStrike.Betting.Api.Infrastructure;

public interface IKafkaEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct)
        where T : class, IPlatformEvent;
}

public sealed class KafkaEventPublisher : IKafkaEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IProducer<Null, string>? _producer;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _producer = CreateProducer(configuration, logger);
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct)
        where T : class, IPlatformEvent
    {
        if (_producer is null)
        {
            return;
        }

        var topic = ResolveTopic(message);
        var payload = BuildPayload(message);

        await _producer.ProduceAsync(topic, new Message<Null, string> { Value = payload }, ct);
    }

    public void Dispose() => _producer?.Dispose();

    private static IProducer<Null, string>? CreateProducer(IConfiguration configuration, ILogger logger)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            logger.LogInformation("Kafka producer disabled because Kafka:BootstrapServers is not configured.");
            return null;
        }

        return new ProducerBuilder<Null, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
            EnableIdempotence = false,
            MessageTimeoutMs = 5000
        }).Build();
    }

    private static string ResolveTopic(IPlatformEvent message)
        => message switch
        {
            BetRegisteredEvent or BetStatusChangedEvent => "bets-events",
            GameCatalogPublishedEvent or GameCatalogUpdatedEvent or GameCatalogDeletedEvent or ResultRecordedEvent => "game-events",
            _ => "analytics-events"
        };

    private static string BuildPayload<T>(T message)
        where T : class, IPlatformEvent
    {
        var node = JsonSerializer.SerializeToNode(message, JsonOptions)?.AsObject() ?? new JsonObject();
        node["eventType"] = message.GetType().Name;
        return node.ToJsonString(JsonOptions);
    }
}
