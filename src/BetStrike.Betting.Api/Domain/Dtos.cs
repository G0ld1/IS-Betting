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

public sealed class DashboardResumo
{
    public DateTime AtualizadoUtc { get; set; }
    public int JogosAtivos { get; set; }
    public int ApostasPendentes { get; set; }
    public int ApostasGanhas { get; set; }
    public decimal VolumeTotalApostado { get; set; }
    public decimal MargemPlataforma { get; set; }
    public int UtilizadoresAtivos { get; set; }
    public decimal ApostasPorMinuto { get; set; }
    public decimal MediaMovel15MinVolume { get; set; }
    public decimal MaxVolumeHora { get; set; }
    public decimal MinVolumeHora { get; set; }
}

public sealed class DashboardCompeticao
{
    public string Competicao { get; set; } = string.Empty;
    public decimal MediaGolosPorJogo { get; set; }
    public decimal VolumeTotalApostado { get; set; }
    public decimal TaxaResultado1 { get; set; }
    public decimal TaxaResultadoX { get; set; }
    public decimal TaxaResultado2 { get; set; }
    public DateTime AtualizadoUtc { get; set; }
}

public sealed class DashboardJanelaTemporal
{
    public DateTime JanelaInicioUtc { get; set; }
    public int Apostas { get; set; }
    public decimal VolumeTotal { get; set; }
    public decimal MediaAposta { get; set; }
    public decimal MaxAposta { get; set; }
    public decimal MinAposta { get; set; }
}

public sealed class DashboardExposicaoJogo
{
    public int JogoId { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
    public string Competicao { get; set; } = string.Empty;
    public int ApostasPendentes { get; set; }
    public decimal VolumeTotal { get; set; }
    public decimal ExposicaoLiquida { get; set; }
    public DateTime AtualizadoUtc { get; set; }
}

public sealed class DashboardTipoAposta
{
    public string TipoAposta { get; set; } = string.Empty;
    public int TotalApostas { get; set; }
    public int ApostasGanhas { get; set; }
    public int ApostasPerdidas { get; set; }
    public decimal TaxaVitoria { get; set; }
    public decimal VolumeTotal { get; set; }
    public DateTime AtualizadoUtc { get; set; }
}

public sealed class DashboardAlerta
{
    public int Id { get; set; }
    public string Severidade { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? Competicao { get; set; }
    public Guid? SourceEventId { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}

public sealed class DashboardEvento
{
    public long EventSequence { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CriadoEmUtc { get; set; }
}

public sealed class DashboardSnapshot
{
    public DashboardResumo Resumo { get; set; } = new();
    public IReadOnlyList<DashboardCompeticao> Competicoes { get; set; } = Array.Empty<DashboardCompeticao>();
    public IReadOnlyList<DashboardJanelaTemporal> JanelasTemporais { get; set; } = Array.Empty<DashboardJanelaTemporal>();
    public IReadOnlyList<DashboardExposicaoJogo> ExposicoesJogos { get; set; } = Array.Empty<DashboardExposicaoJogo>();
    public IReadOnlyList<DashboardTipoAposta> TiposAposta { get; set; } = Array.Empty<DashboardTipoAposta>();
    public IReadOnlyList<DashboardAlerta> Alertas { get; set; } = Array.Empty<DashboardAlerta>();
    public IReadOnlyList<DashboardEvento> EventosRecentes { get; set; } = Array.Empty<DashboardEvento>();
}
