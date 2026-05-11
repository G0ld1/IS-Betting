using System.Data;
using System.Text.Json;
using BetStrike.Middleware.Contracts;
using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;

namespace BetStrike.Middleware.Worker;

public sealed class AnalyticsRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("AnalyticsDb")
        ?? configuration.GetConnectionString("ApostasDb")
        ?? throw new InvalidOperationException("Connection string AnalyticsDb/ApostasDb não configurada.");

    private IDbConnection Open() => new SqlConnection(_connectionString);

    public async Task RegisterEventAsync(IPlatformEvent platformEvent, string payloadJson, CancellationToken ct)
    {
        using var db = Open();

        await db.ExecuteAsync(new CommandDefinition(
            "dbo.sp_Middleware_RegistarEvento",
            new
            {
                platformEvent.EventId,
                platformEvent.SourceSystem,
                platformEvent.AggregateType,
                platformEvent.AggregateKey,
                platformEvent.OccurredAtUtc,
                EventType = platformEvent.GetType().Name,
                PayloadJson = payloadJson
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task RegisterAlertAsync(AlertRaisedEvent alert, Guid? sourceEventId, CancellationToken ct)
    {
        using var db = Open();

        await db.ExecuteAsync(new CommandDefinition(
            "dbo.sp_Middleware_RegistarAlerta",
            new
            {
                Severity = alert.Severity,
                Message = alert.Message,
                Competition = alert.Competition,
                SourceEventId = sourceEventId,
                alert.OccurredAtUtc
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

        public async Task<IReadOnlyList<ArchivedPlatformEvent>> ListArchivedEventsAsync(CancellationToken ct)
                => await ListArchivedEventsAsync(0, ct);

        public async Task<IReadOnlyList<ArchivedPlatformEvent>> ListArchivedEventsAsync(long afterSequence, CancellationToken ct)
    {
        using var db = Open();

        var rows = await db.QueryAsync<ArchivedPlatformEvent>(new CommandDefinition(
            @"SELECT EventSequence, EventId, EventType, SourceSystem, AggregateType, AggregateKey, PayloadJson, CriadoEmUtc
              FROM dbo.Evento_Middleware
                            WHERE EventSequence > @AfterSequence
              ORDER BY EventSequence",
                        new { AfterSequence = afterSequence },
            cancellationToken: ct));

        return rows.ToList();
    }

        public async Task<long> GetReplayCheckpointAsync(string replayName, CancellationToken ct)
        {
                using var db = Open();

                return await db.QueryFirstOrDefaultAsync<long>(new CommandDefinition(
                        @"SELECT COALESCE(LastEventSequence, 0)
                            FROM dbo.Stream_ReplayCheckpoint
                            WHERE ReplayName = @ReplayName",
                        new { ReplayName = replayName },
                        cancellationToken: ct));
        }

        public async Task UpdateReplayCheckpointAsync(string replayName, long lastEventSequence, CancellationToken ct)
        {
                using var db = Open();

                await db.ExecuteAsync(new CommandDefinition(
                        @"MERGE dbo.Stream_ReplayCheckpoint AS target
                            USING (SELECT @ReplayName AS ReplayName, @LastEventSequence AS LastEventSequence) AS source
                                ON target.ReplayName = source.ReplayName
                            WHEN MATCHED THEN
                                UPDATE SET LastEventSequence = source.LastEventSequence, UpdatedAtUtc = SYSUTCDATETIME()
                            WHEN NOT MATCHED THEN
                                INSERT (ReplayName, LastEventSequence, UpdatedAtUtc)
                                VALUES (source.ReplayName, source.LastEventSequence, SYSUTCDATETIME());",
                        new { ReplayName = replayName, LastEventSequence = lastEventSequence },
                        cancellationToken: ct));
        }

    public async Task<JogoAlertContext?> ObterContextoJogoAsync(int jogoId, CancellationToken ct)
    {
        using var db = Open();

        return await db.QueryFirstOrDefaultAsync<JogoAlertContext>(new CommandDefinition(
            @"SELECT TOP 1
                j.Id AS JogoId,
                j.CodigoJogo,
                j.Competicao,
                COALESCE(SUM(a.ValorApostado), 0) AS VolumeTotalApostado,
                COALESCE(SUM(CASE WHEN a.Estado = 1 THEN 1 ELSE 0 END), 0) AS ApostasPendentes
              FROM dbo.Jogo j
              LEFT JOIN dbo.Aposta a ON a.JogoId = j.Id
              WHERE j.Id = @JogoId
              GROUP BY j.Id, j.CodigoJogo, j.Competicao",
            new { JogoId = jogoId },
            cancellationToken: ct));
    }

    public async Task RebuildDashboardAsync(CancellationToken ct)
    {
        using var db = Open();

        await db.ExecuteAsync(new CommandDefinition(
            "dbo.sp_Middleware_RecalcularDashboard",
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }
}

public sealed record ArchivedPlatformEvent(
    long EventSequence,
    Guid EventId,
    string EventType,
    string SourceSystem,
    string AggregateType,
    string AggregateKey,
    string PayloadJson,
    DateTime CriadoEmUtc);

public sealed class JogoAlertContext
{
    public int JogoId { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
    public string Competicao { get; set; } = string.Empty;
    public decimal VolumeTotalApostado { get; set; }
    public int ApostasPendentes { get; set; }
}
