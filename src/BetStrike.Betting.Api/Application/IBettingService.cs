using BetStrike.Betting.Api.Domain;

namespace BetStrike.Betting.Api.Application;

public interface IBettingService
{
    Task<int> InserirJogoAsync(InserirJogoRequest request, CancellationToken ct);
    Task<bool> AtualizarJogoAsync(AtualizarJogoRequest request, CancellationToken ct);
    Task<IReadOnlyList<Jogo>> ListarJogosAsync(FiltroJogos filtro, CancellationToken ct);
    Task<Jogo?> ObterJogoAsync(string codigoJogo, CancellationToken ct);
    Task<bool> RemoverJogoAsync(string codigoJogo, CancellationToken ct);

    Task<int> RegistarApostaAsync(RegistarApostaRequest request, CancellationToken ct);
    Task<bool> CancelarApostaAsync(CancelarApostaRequest request, CancellationToken ct);
    Task<Aposta?> ObterApostaAsync(int apostaId, CancellationToken ct);
    Task<IReadOnlyList<Aposta>> ListarApostasAsync(FiltroApostas filtro, CancellationToken ct);

    Task<int> CriarUtilizadorAsync(CriarUtilizadorRequest request, CancellationToken ct);

    Task<bool> InserirResultadoAsync(InserirResultadoRequest request, CancellationToken ct);
    Task<Resultado?> ObterResultadoAsync(int jogoId, CancellationToken ct);

    Task<EstatisticasJogo?> EstatisticasJogoAsync(string codigoJogo, CancellationToken ct);
    Task<object?> EstatisticasCompeticaoAsync(string competicao, CancellationToken ct);
}
