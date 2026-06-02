using BetStrike.Middleware.Contracts;
using Microsoft.Extensions.Logging;

namespace BetStrike.Streaming.Worker.Services;

public sealed class StreamingAlertService(
    StreamingAnalyticsRepository repository,
    ILogger<StreamingAlertService> logger)
{
    public async Task RegisterAlertsAsync(IPlatformEvent platformEvent, CancellationToken ct)
    {
        foreach (var alert in await BuildAlertsAsync(platformEvent, ct))
        {
            await repository.RegisterAlertAsync(alert, ct);
            logger.LogWarning("Streaming alert {Severity}: {Message}", alert.Severity, alert.Message);
        }
    }

    private async Task<IReadOnlyList<AlertRaisedEvent>> BuildAlertsAsync(IPlatformEvent platformEvent, CancellationToken ct)
    {
        var alerts = new List<AlertRaisedEvent>();

        switch (platformEvent)
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
                        "BetStrike.Streaming.Worker",
                        "alert",
                        contexto.CodigoJogo,
                        DateTime.UtcNow,
                        "Alta",
                        $"Exposicao elevada no jogo {contexto.CodigoJogo}: {contexto.VolumeTotalApostado:N2} EUR.",
                        bet.EventId,
                        contexto.Competicao));
                }
                else if (bet.ValorApostado >= 100m)
                {
                    alerts.Add(new AlertRaisedEvent(
                        Guid.NewGuid(),
                        "BetStrike.Streaming.Worker",
                        "alert",
                        contexto.CodigoJogo,
                        DateTime.UtcNow,
                        "Media",
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
                        "BetStrike.Streaming.Worker",
                        "alert",
                        result.AggregateKey,
                        DateTime.UtcNow,
                        diferenca >= 4 ? "Alta" : "Media",
                        $"Resultado anomalo no jogo {result.JogoId}: {result.GolosCasa}-{result.GolosFora}.",
                        result.EventId,
                        null));
                }

                break;
            }
        }

        return alerts;
    }
}
