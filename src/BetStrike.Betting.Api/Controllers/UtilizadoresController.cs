using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Betting.Api.Controllers;

[ApiController]
[Route("api/utilizadores")]
public sealed class UtilizadoresController(IBettingService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<int>> Criar([FromBody] CriarUtilizadorRequest request, CancellationToken ct)
    {
        try
        {
            var id = await service.CriarUtilizadorAsync(request, ct);
            return CreatedAtAction(nameof(Criar), new { utilizadorId = id }, id);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            return Conflict("Email já existe. Use um email diferente.");
        }
    }
}
