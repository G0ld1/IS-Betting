using BetStrike.Betting.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/estatisticas")]
public sealed class EstatisticasController(IBettingService service) : ControllerBase
{
    [HttpGet("jogo/{codigoJogo}")]
    public async Task<IActionResult> PorJogo(string codigoJogo, CancellationToken ct)
    {
        var row = await service.EstatisticasJogoAsync(codigoJogo, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpGet("competicao/{competicao}")]
    public async Task<IActionResult> PorCompeticao(string competicao, CancellationToken ct)
    {
        var row = await service.EstatisticasCompeticaoAsync(competicao, ct);
        return row is null ? NotFound() : Ok(row);
    }
}
