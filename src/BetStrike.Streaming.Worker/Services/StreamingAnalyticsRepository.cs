using System.Data;
using BetStrike.Middleware.Contracts;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BetStrike.Streaming.Worker.Services;

public sealed class StreamingAnalyticsRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("AnalyticsDb")
        ?? configuration.GetConnectionString("ApostasDb")
        ?? throw new InvalidOperationException("Connection string AnalyticsDb/ApostasDb nao configurada.");

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

    public async Task RegisterAlertAsync(AlertRaisedEvent alert, CancellationToken ct)
    {
        using var db = Open();

        await db.ExecuteAsync(new CommandDefinition(
            "dbo.sp_Middleware_RegistarAlerta",
            new
            {
                Severity = alert.Severity,
                Message = alert.Message,
                Competition = alert.Competition,
                SourceEventId = alert.SourceEventId,
                alert.OccurredAtUtc
            },
            commandType: CommandType.StoredProcedure,
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
                COALESCE(SUM(a.ValorApostado), 0) AS VolumeTotalApostado
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

public sealed class JogoAlertContext
{
    public int JogoId { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
    public string Competicao { get; set; } = string.Empty;
    public decimal VolumeTotalApostado { get; set; }
}
