using BetStrike.Betting.Api.Domain;

namespace BetStrike.Betting.Api.Application;

public sealed class BettingService(IBettingRepository repository) : IBettingService
{
    public Task<int> InserirJogoAsync(InserirJogoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoJogo) || !request.CodigoJogo.StartsWith("FUT-"))
            throw new ArgumentException("Código de jogo inválido.");

        return repository.InserirJogoAsync(request, ct);
    }

    public Task<bool> AtualizarJogoAsync(AtualizarJogoRequest request, CancellationToken ct)
        => repository.AtualizarJogoResultadoAsync(request, ct);

    public Task<IReadOnlyList<Jogo>> ListarJogosAsync(FiltroJogos filtro, CancellationToken ct)
        => repository.ListarJogosAsync(filtro, ct);

    public Task<Jogo?> ObterJogoAsync(string codigoJogo, CancellationToken ct)
        => repository.ObterJogoAsync(codigoJogo, ct);

    public Task<bool> RemoverJogoAsync(string codigoJogo, CancellationToken ct)
        => repository.RemoverJogoAsync(codigoJogo, ct);

    public Task<int> RegistarApostaAsync(RegistarApostaRequest request, CancellationToken ct)
    {
        if (request.ValorApostado <= 0)
            throw new ArgumentException("Valor apostado tem de ser superior a zero.");

        if (request.OddMomento <= 1.0m)
            throw new ArgumentException("Odd tem de ser maior que 1.0.");

        if (request.TipoAposta is not ("1" or "X" or "2"))
            throw new ArgumentException("Tipo de aposta inválido. Use 1, X ou 2.");

        return repository.RegistarApostaAsync(request, ct);
    }

    public Task<bool> CancelarApostaAsync(CancelarApostaRequest request, CancellationToken ct)
        => repository.CancelarApostaAsync(request, ct);

    public Task<Aposta?> ObterApostaAsync(int apostaId, CancellationToken ct)
        => repository.ObterApostaAsync(apostaId, ct);

    public Task<IReadOnlyList<Aposta>> ListarApostasAsync(FiltroApostas filtro, CancellationToken ct)
        => repository.ListarApostasAsync(filtro, ct);

    public Task<int> CriarUtilizadorAsync(CriarUtilizadorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("Email inválido.");

        return repository.CriarUtilizadorAsync(request, ct);
    }

    public Task<bool> InserirResultadoAsync(InserirResultadoRequest request, CancellationToken ct)
        => repository.InserirResultadoAsync(request, ct);

    public Task<Resultado?> ObterResultadoAsync(int jogoId, CancellationToken ct)
        => repository.ObterResultadoAsync(jogoId, ct);

    public Task<EstatisticasJogo?> EstatisticasJogoAsync(string codigoJogo, CancellationToken ct)
        => repository.EstatisticasJogoAsync(codigoJogo, ct);

    public Task<object?> EstatisticasCompeticaoAsync(string competicao, CancellationToken ct)
        => repository.EstatisticasCompeticaoAsync(competicao, ct);
}
