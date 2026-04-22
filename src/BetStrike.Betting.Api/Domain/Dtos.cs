namespace BetStrike.Betting.Api.Domain;

public sealed record InserirJogoRequest(
    string CodigoJogo,
    DateOnly DataJogo,
    TimeOnly HoraInicio,
    string EquipaCasa,
    string EquipaFora,
    string Competicao,
    int Estado);

public sealed record AtualizarJogoRequest(string CodigoJogo, int Estado, int? GolosCasa, int? GolosFora);

public sealed record RegistarApostaRequest(int JogoId, int UtilizadorId, string TipoAposta, decimal ValorApostado, decimal OddMomento);

public sealed record CancelarApostaRequest(int ApostaId, int UtilizadorId);

public sealed record CriarUtilizadorRequest(string Nome, string Email);

public sealed record InserirResultadoRequest(int JogoId, int GolosCasa, int GolosFora);

public sealed record FiltroJogos(DateOnly? Data, int? Estado, string? Competicao);

public sealed record FiltroApostas(int? UtilizadorId, int? JogoId, int? Estado, DateTime? InicioUtc, DateTime? FimUtc);

public sealed class UtilizadorComSaldo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal SaldoDisponivel { get; set; }
    public decimal SaldoGastoTotal { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}
