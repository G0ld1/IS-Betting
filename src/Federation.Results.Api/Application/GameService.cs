using Federation.Results.Api.Domain;
using BetStrike.Middleware.Contracts;
using MassTransit;

namespace Federation.Results.Api.Application;

public sealed class GameService(IGameRepository repository, IPublishEndpoint publishEndpoint, ILogger<GameService> logger) : IGameService
{
    public async Task<int> CreateAsync(CreateGameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoJogo) || !request.CodigoJogo.StartsWith("FUT-"))
            throw new ArgumentException("Codigo_Jogo inválido.");

        if (request.EquipaCasa.Equals(request.EquipaFora, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Uma equipa não pode jogar contra si própria.");

        var id = await repository.InsertAsync(request, ct);
        await PublishSafelyAsync(new GameCatalogPublishedEvent(Guid.NewGuid(), "Federation.Results.Api", "game", request.CodigoJogo, DateTime.UtcNow, request.CodigoJogo, request.DataJogo, request.HoraInicio, request.EquipaCasa, request.EquipaFora, request.Estado, null), ct);
        return id;
    }

    public async Task<bool> UpdateAsync(UpdateGameRequest request, CancellationToken ct)
    {
        var ok = await repository.UpdateAsync(request, ct);
        if (ok)
        {
            await PublishSafelyAsync(new GameCatalogUpdatedEvent(Guid.NewGuid(), "Federation.Results.Api", "game", request.CodigoJogo, DateTime.UtcNow, request.CodigoJogo, request.Estado, request.GolosCasa, request.GolosFora), ct);
        }

        return ok;
    }

    public Task<Game?> GetAsync(string codigoJogo, CancellationToken ct)
        => repository.GetByCodeAsync(codigoJogo, ct);

    public Task<IReadOnlyList<Game>> ListAsync(GameQuery query, CancellationToken ct)
        => repository.ListAsync(query, ct);

    public async Task<bool> DeleteAsync(string codigoJogo, CancellationToken ct)
    {
        var ok = await repository.DeleteAsync(codigoJogo, ct);
        if (ok)
        {
            await PublishSafelyAsync(new GameCatalogDeletedEvent(Guid.NewGuid(), "Federation.Results.Api", "game", codigoJogo, DateTime.UtcNow, codigoJogo), ct);
        }

        return ok;
    }

    private async Task PublishSafelyAsync<T>(T message, CancellationToken ct)
        where T : class
    {
        try
        {
            await publishEndpoint.Publish(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao publicar evento {EventType} no broker.", typeof(T).Name);
        }
    }
}
