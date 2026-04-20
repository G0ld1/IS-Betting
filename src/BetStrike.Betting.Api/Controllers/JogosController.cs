using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/apostas/jogos")]
public sealed class JogosController(IBettingService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Inserir([FromBody] InserirJogoRequest request, CancellationToken ct)
    {
        var id = await service.InserirJogoAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { codigoJogo = request.CodigoJogo }, id);
    }

    [HttpPut("{codigoJogo}")]
    public async Task<IActionResult> Atualizar(string codigoJogo, [FromBody] AtualizarJogoRequest request, CancellationToken ct)
    {
        if (!codigoJogo.Equals(request.CodigoJogo, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Código do jogo no URL e no payload têm de coincidir.");

        return await service.AtualizarJogoAsync(request, ct) ? NoContent() : NotFound();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Jogo>>> Listar([FromQuery] DateOnly? data, [FromQuery] int? estado, [FromQuery] string? competicao, CancellationToken ct)
    {
        var rows = await service.ListarJogosAsync(new FiltroJogos(data, estado, competicao), ct);
        return Ok(rows);
    }

    [HttpGet("{codigoJogo}")]
    public async Task<ActionResult<Jogo>> Obter(string codigoJogo, CancellationToken ct)
    {
        var row = await service.ObterJogoAsync(codigoJogo, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpDelete("{codigoJogo}")]
    public async Task<IActionResult> Remover(string codigoJogo, CancellationToken ct)
    {
        return await service.RemoverJogoAsync(codigoJogo, ct) ? NoContent() : NotFound();
    }
}
