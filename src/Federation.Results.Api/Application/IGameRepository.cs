using Federation.Results.Api.Domain;

namespace Federation.Results.Api.Application;

public interface IGameRepository
{
    Task<int> InsertAsync(CreateGameRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(UpdateGameRequest request, CancellationToken ct);
    Task<Game?> GetByCodeAsync(string codigoJogo, CancellationToken ct);
    Task<IReadOnlyList<Game>> ListAsync(GameQuery query, CancellationToken ct);
    Task<bool> DeleteAsync(string codigoJogo, CancellationToken ct);
}
