using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/resultados")]
public sealed class ResultadosController(IBettingService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Inserir([FromBody] InserirResultadoRequest request, CancellationToken ct)
    {
        return await service.InserirResultadoAsync(request, ct) ? NoContent() : BadRequest("Resultado inválido.");
    }

    [HttpGet("jogo/{jogoId:int}")]
    public async Task<ActionResult<Resultado>> Obter(int jogoId, CancellationToken ct)
    {
        var row = await service.ObterResultadoAsync(jogoId, ct);
        return row is null ? NotFound() : Ok(row);
    }
}
