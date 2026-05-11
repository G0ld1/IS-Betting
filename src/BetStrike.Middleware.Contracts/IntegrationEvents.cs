namespace BetStrike.Middleware.Contracts;

public static class MiddlewareTopology
{
    public const string ExchangeName = "betstrike.events";
    public const string StreamQueue = "betstrike.analytics.stream";
    public const string StreamDeadLetterQueue = "betstrike.analytics.stream.dlq";
    public const string HighPriorityAnalyticsQueue = "betstrike.analytics.high";
    public const string HighPriorityAnalyticsDeadLetterQueue = "betstrike.analytics.high.dlq";
    public const string LowPriorityAnalyticsQueue = "betstrike.analytics.low";
    public const string LowPriorityAnalyticsDeadLetterQueue = "betstrike.analytics.low.dlq";
}

public interface IPlatformEvent
{
    Guid EventId { get; }
    string SourceSystem { get; }
    string AggregateType { get; }
    string AggregateKey { get; }
    DateTime OccurredAtUtc { get; }
}

public abstract record PlatformEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc) : IPlatformEvent;

public sealed record GameCatalogPublishedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string CodigoJogo,
    DateOnly DataJogo,
    TimeOnly HoraInicio,
    string EquipaCasa,
    string EquipaFora,
    int Estado,
    string? Competicao) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record GameCatalogUpdatedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string CodigoJogo,
    int Estado,
    int GolosCasa,
    int GolosFora) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record GameCatalogDeletedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string CodigoJogo) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record BetRegisteredEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    int ApostaId,
    int JogoId,
    int UtilizadorId,
    string TipoAposta,
    decimal ValorApostado,
    decimal OddMomento,
    int Estado) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record BetStatusChangedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    int ApostaId,
    int JogoId,
    int UtilizadorId,
    int EstadoAnterior,
    int EstadoNovo,
    decimal ValorApostado,
    decimal OddMomento) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record UserCreatedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    int UtilizadorId,
    string Nome,
    string Email) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record ResultRecordedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    int JogoId,
    int GolosCasa,
    int GolosFora,
    bool Desconhecido) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record HighPriorityDashboardRefreshRequestedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string Reason,
    Guid? SourceEventId) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record LowPriorityDashboardRefreshRequestedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string Reason,
    Guid? SourceEventId) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);

public sealed record AlertRaisedEvent(
    Guid EventId,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    DateTime OccurredAtUtc,
    string Severity,
    string Message,
    Guid? SourceEventId,
    string? Competition) : PlatformEvent(EventId, SourceSystem, AggregateType, AggregateKey, OccurredAtUtc);