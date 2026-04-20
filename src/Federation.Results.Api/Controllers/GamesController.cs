using Federation.Results.Api.Application;
using Federation.Results.Api.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace Federation.Results.Api.Controllers;

[ApiController]
[Route("api/jogos")]
public sealed class GamesController(IGameService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateGameRequest request, CancellationToken ct)
    {
        try
        {
            var id = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetByCode), new { codigoJogo = request.CodigoJogo }, id);
        }
        catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 2627 || ex.Number == 2601)
        {
            return Conflict("Codigo_Jogo já existe.");
        }
    }

    [HttpPut("{codigoJogo}")]
    public async Task<IActionResult> Update(string codigoJogo, [FromBody] UpdateGameRequest request, CancellationToken ct)
    {
        if (!codigoJogo.Equals(request.CodigoJogo, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Código do URL é diferente do payload.");

        return await service.UpdateAsync(request, ct) ? NoContent() : NotFound();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Game>>> List([FromQuery] DateOnly? data, [FromQuery] int? estado, CancellationToken ct)
    {
        var dataOut = await service.ListAsync(new GameQuery(data, estado), ct);
        return Ok(dataOut);
    }

    [HttpGet("{codigoJogo}")]
    public async Task<ActionResult<Game>> GetByCode(string codigoJogo, CancellationToken ct)
    {
        var game = await service.GetAsync(codigoJogo, ct);
        return game is null ? NotFound() : Ok(game);
    }

    [HttpDelete("{codigoJogo}")]
    public async Task<IActionResult> Delete(string codigoJogo, CancellationToken ct)
    {
        return await service.DeleteAsync(codigoJogo, ct) ? NoContent() : NotFound();
    }
}
