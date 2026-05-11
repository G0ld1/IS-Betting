using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(IBettingService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSnapshot>> Dashboard(CancellationToken ct)
        => Ok(await service.ObterDashboardAsync(ct));

    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<DashboardAlerta>>> Alertas([FromQuery] int? limite, CancellationToken ct)
        => Ok(await service.ListarAlertasAsync(Math.Clamp(limite ?? 12, 1, 100), ct));

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<DashboardEvento>>> Eventos([FromQuery] int? limite, CancellationToken ct)
        => Ok(await service.ListarEventosAsync(Math.Clamp(limite ?? 20, 1, 100), ct));
}