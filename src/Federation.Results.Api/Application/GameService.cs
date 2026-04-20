using Federation.Results.Api.Domain;

namespace Federation.Results.Api.Application;

public sealed class GameService(IGameRepository repository) : IGameService
{
    public Task<int> CreateAsync(CreateGameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoJogo) || !request.CodigoJogo.StartsWith("FUT-"))
            throw new ArgumentException("Codigo_Jogo inválido.");

        if (request.EquipaCasa.Equals(request.EquipaFora, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Uma equipa não pode jogar contra si própria.");

        return repository.InsertAsync(request, ct);
    }

    public Task<bool> UpdateAsync(UpdateGameRequest request, CancellationToken ct)
        => repository.UpdateAsync(request, ct);

    public Task<Game?> GetAsync(string codigoJogo, CancellationToken ct)
        => repository.GetByCodeAsync(codigoJogo, ct);

    public Task<IReadOnlyList<Game>> ListAsync(GameQuery query, CancellationToken ct)
        => repository.ListAsync(query, ct);

    public Task<bool> DeleteAsync(string codigoJogo, CancellationToken ct)
        => repository.DeleteAsync(codigoJogo, ct);
}
