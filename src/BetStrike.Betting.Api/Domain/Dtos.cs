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
