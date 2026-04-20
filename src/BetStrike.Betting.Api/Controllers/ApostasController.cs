using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/apostas")]
public sealed class ApostasController(IBettingService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Registar([FromBody] RegistarApostaRequest request, CancellationToken ct)
    {
        var id = await service.RegistarApostaAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { apostaId = id }, id);
    }

    [HttpDelete("{apostaId}")]
    public async Task<IActionResult> Cancelar(int apostaId, [FromQuery] int utilizadorId, CancellationToken ct)
    {
        var ok = await service.CancelarApostaAsync(new CancelarApostaRequest(apostaId, utilizadorId), ct);
        return ok ? NoContent() : BadRequest("Aposta não pode ser cancelada.");
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Aposta>>> Listar(
        [FromQuery] int? utilizadorId,
        [FromQuery] int? jogoId,
        [FromQuery] int? estado,
        [FromQuery] DateTime? inicioUtc,
        [FromQuery] DateTime? fimUtc,
        CancellationToken ct)
    {
        var rows = await service.ListarApostasAsync(new FiltroApostas(utilizadorId, jogoId, estado, inicioUtc, fimUtc), ct);
        return Ok(rows);
    }

    [HttpGet("{apostaId}")]
    public async Task<ActionResult<object>> Obter(int apostaId, CancellationToken ct)
    {
        var row = await service.ObterApostaAsync(apostaId, ct);
        if (row is null) return NotFound();

        var premio = row.ValorApostado * row.OddMomento;
        return Ok(new
        {
            Aposta = row,
            PremioPotencial = premio
        });
    }
}
