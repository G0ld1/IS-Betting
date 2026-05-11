using BetStrike.Betting.Api.Domain;
using BetStrike.Middleware.Contracts;
using MassTransit;

namespace BetStrike.Betting.Api.Application;

public sealed class BettingService(IBettingRepository repository, IPublishEndpoint publishEndpoint, ILogger<BettingService> logger) : IBettingService
{
    public async Task<int> InserirJogoAsync(InserirJogoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoJogo) || !request.CodigoJogo.StartsWith("FUT-"))
            throw new ArgumentException("Código de jogo inválido.");

        var id = await repository.InserirJogoAsync(request, ct);
        await PublishSafelyAsync(new GameCatalogPublishedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "game", request.CodigoJogo, DateTime.UtcNow, request.CodigoJogo, request.DataJogo, request.HoraInicio, request.EquipaCasa, request.EquipaFora, request.Estado, request.Competicao), ct);
        return id;
    }

    public async Task<bool> AtualizarJogoAsync(AtualizarJogoRequest request, CancellationToken ct)
    {
        var ok = await repository.AtualizarJogoResultadoAsync(request, ct);
        if (ok)
        {
            await PublishSafelyAsync(new GameCatalogUpdatedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "game", request.CodigoJogo, DateTime.UtcNow, request.CodigoJogo, request.Estado, request.GolosCasa ?? 0, request.GolosFora ?? 0), ct);
        }

        return ok;
    }

    public Task<IReadOnlyList<Jogo>> ListarJogosAsync(FiltroJogos filtro, CancellationToken ct)
        => repository.ListarJogosAsync(filtro, ct);

    public Task<Jogo?> ObterJogoAsync(string codigoJogo, CancellationToken ct)
        => repository.ObterJogoAsync(codigoJogo, ct);

    public async Task<bool> RemoverJogoAsync(string codigoJogo, CancellationToken ct)
    {
        var ok = await repository.RemoverJogoAsync(codigoJogo, ct);
        if (ok)
        {
            await PublishSafelyAsync(new GameCatalogDeletedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "game", codigoJogo, DateTime.UtcNow, codigoJogo), ct);
        }

        return ok;
    }

    public async Task<int> RegistarApostaAsync(RegistarApostaRequest request, CancellationToken ct)
    {
        if (request.ValorApostado <= 0)
            throw new ArgumentException("Valor apostado tem de ser superior a zero.");

        if (request.OddMomento <= 1.0m)
            throw new ArgumentException("Odd tem de ser maior que 1.0.");

        if (request.TipoAposta is not ("1" or "X" or "2"))
            throw new ArgumentException("Tipo de aposta inválido. Use 1, X ou 2.");

        var apostaId = await repository.RegistarApostaAsync(request, ct);
        await PublishSafelyAsync(new BetRegisteredEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "bet", apostaId.ToString(), DateTime.UtcNow, apostaId, request.JogoId, request.UtilizadorId, request.TipoAposta, request.ValorApostado, request.OddMomento, 1), ct);
        return apostaId;
    }

    public async Task<bool> CancelarApostaAsync(CancelarApostaRequest request, CancellationToken ct)
    {
        var aposta = await repository.ObterApostaAsync(request.ApostaId, ct);
        var ok = await repository.CancelarApostaAsync(request, ct);

        if (ok && aposta is not null)
        {
            await PublishSafelyAsync(new BetStatusChangedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "bet", request.ApostaId.ToString(), DateTime.UtcNow, aposta.Id, aposta.JogoId, aposta.UtilizadorId, aposta.Estado, 4, aposta.ValorApostado, aposta.OddMomento), ct);
        }

        return ok;
    }

    public Task<Aposta?> ObterApostaAsync(int apostaId, CancellationToken ct)
        => repository.ObterApostaAsync(apostaId, ct);

    public Task<IReadOnlyList<Aposta>> ListarApostasAsync(FiltroApostas filtro, CancellationToken ct)
        => repository.ListarApostasAsync(filtro, ct);

    public async Task<int> CriarUtilizadorAsync(CriarUtilizadorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("Email inválido.");

        var utilizadorId = await repository.CriarUtilizadorAsync(request, ct);
        await PublishSafelyAsync(new UserCreatedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "user", utilizadorId.ToString(), DateTime.UtcNow, utilizadorId, request.Nome, request.Email), ct);
        return utilizadorId;
    }

    public Task<IReadOnlyList<UtilizadorComSaldo>> ListarUtilizadoresAsync(CancellationToken ct)
        => repository.ListarUtilizadoresAsync(ct);

    public async Task<bool> InserirResultadoAsync(InserirResultadoRequest request, CancellationToken ct)
    {
        var ok = await repository.InserirResultadoAsync(request, ct);
        if (ok)
        {
            await PublishSafelyAsync(new ResultRecordedEvent(Guid.NewGuid(), "BetStrike.Betting.Api", "result", request.JogoId.ToString(), DateTime.UtcNow, request.JogoId, request.GolosCasa, request.GolosFora, false), ct);
        }

        return ok;
    }

    public Task<Resultado?> ObterResultadoAsync(int jogoId, CancellationToken ct)
        => repository.ObterResultadoAsync(jogoId, ct);

    public Task<EstatisticasJogo?> EstatisticasJogoAsync(string codigoJogo, CancellationToken ct)
        => repository.EstatisticasJogoAsync(codigoJogo, ct);

    public Task<object?> EstatisticasCompeticaoAsync(string competicao, CancellationToken ct)
        => repository.EstatisticasCompeticaoAsync(competicao, ct);

    public Task<DashboardSnapshot> ObterDashboardAsync(CancellationToken ct)
        => repository.ObterDashboardAsync(ct);

    public Task<IReadOnlyList<DashboardAlerta>> ListarAlertasAsync(int limite, CancellationToken ct)
        => repository.ListarAlertasAsync(limite, ct);

    public Task<IReadOnlyList<DashboardEvento>> ListarEventosAsync(int limite, CancellationToken ct)
        => repository.ListarEventosAsync(limite, ct);

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
