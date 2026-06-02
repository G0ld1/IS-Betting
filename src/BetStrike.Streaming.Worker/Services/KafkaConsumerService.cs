using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using BetStrike.Middleware.Contracts;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BetStrike.Streaming.Worker.Services;

public sealed class KafkaConsumerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly StreamingAnalyticsRepository _repository;
    private readonly StreamingAlertService _alertService;
    private IConsumer<Ignore, string>? _consumer;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IConfiguration configuration,
        StreamingAnalyticsRepository repository,
        StreamingAlertService alertService)
    {
        _logger = logger;
        _configuration = configuration;
        _repository = repository;
        _alertService = alertService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var topics = _configuration.GetSection("Kafka:Topics").Get<string[]>()
            ?? new[] { "bets-events", "game-events", "analytics-events" };
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        await EnsureTopicsAsync(bootstrapServers, topics, stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = _configuration["Kafka:GroupId"] ?? "betstrike-streaming-analytics",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 15000
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                _logger.LogInformation("Kafka partitions assigned: {Partitions}", string.Join(", ", partitions)))
            .Build();

        _consumer.Subscribe(topics);
        _logger.LogInformation("Kafka streaming worker subscribed to {Topics}", string.Join(", ", topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<Ignore, string>? result;
                try
                {
                    result = _consumer.Consume(TimeSpan.FromMilliseconds(250));
                }
                catch (ConsumeException ex) when (ex.Error.Code is ErrorCode.UnknownTopicOrPart or ErrorCode.LeaderNotAvailable)
                {
                    _logger.LogWarning("Kafka topic metadata not ready yet: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                await ProcessMessageAsync(result, stoppingToken);
                _consumer.StoreOffset(result);
                _consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka streaming worker stopped.");
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task EnsureTopicsAsync(string bootstrapServers, string[] topics, CancellationToken ct)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
                topics.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }),
                new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(15) });

            _logger.LogInformation("Kafka topics ensured: {Topics}", string.Join(", ", topics));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka topics already exist: {Topics}", string.Join(", ", topics));
        }
        catch (KafkaException ex)
        {
            _logger.LogWarning(ex, "Unable to create Kafka topics now. The worker will retry through metadata refresh.");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<Ignore, string> result, CancellationToken ct)
    {
        try
        {
            var platformEvent = DeserializePlatformEvent(result.Message.Value);
            if (platformEvent is null)
            {
                _logger.LogWarning("Ignored Kafka message from {Topic} without known EventType.", result.Topic);
                return;
            }

            await _repository.RegisterEventAsync(platformEvent, result.Message.Value, ct);
            await _alertService.RegisterAlertsAsync(platformEvent, ct);
            await _repository.RebuildDashboardAsync(ct);

            _logger.LogInformation(
                "Processed Kafka event {EventType} from {Topic} offset {Offset}.",
                platformEvent.GetType().Name,
                result.Topic,
                result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Kafka message from {Topic} offset {Offset}.", result.Topic, result.Offset.Value);
            throw;
        }
    }

    private static IPlatformEvent? DeserializePlatformEvent(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        if (!document.RootElement.TryGetProperty("eventType", out var eventTypeElement) &&
            !document.RootElement.TryGetProperty("EventType", out eventTypeElement))
        {
            return null;
        }

        var eventType = eventTypeElement.GetString();
        return eventType switch
        {
            nameof(GameCatalogPublishedEvent) => JsonSerializer.Deserialize<GameCatalogPublishedEvent>(payloadJson, JsonOptions),
            nameof(GameCatalogUpdatedEvent) => JsonSerializer.Deserialize<GameCatalogUpdatedEvent>(payloadJson, JsonOptions),
            nameof(GameCatalogDeletedEvent) => JsonSerializer.Deserialize<GameCatalogDeletedEvent>(payloadJson, JsonOptions),
            nameof(BetRegisteredEvent) => JsonSerializer.Deserialize<BetRegisteredEvent>(payloadJson, JsonOptions),
            nameof(BetStatusChangedEvent) => JsonSerializer.Deserialize<BetStatusChangedEvent>(payloadJson, JsonOptions),
            nameof(UserCreatedEvent) => JsonSerializer.Deserialize<UserCreatedEvent>(payloadJson, JsonOptions),
            nameof(ResultRecordedEvent) => JsonSerializer.Deserialize<ResultRecordedEvent>(payloadJson, JsonOptions),
            _ => null
        };
    }
}
