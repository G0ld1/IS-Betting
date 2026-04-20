namespace BetStrike.Betting.Api.Domain;

public sealed class Jogo
{
    public int Id { get; set; }
    public required string CodigoJogo { get; set; }
    public DateOnly DataJogo { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public required string EquipaCasa { get; set; }
    public required string EquipaFora { get; set; }
    public required string Competicao { get; set; }
    public int Estado { get; set; }
}

public sealed class Resultado
{
    public int Id { get; set; }
    public int JogoId { get; set; }
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public DateTime UltimaAtualizacaoUtc { get; set; }
}

public sealed class Aposta
{
    public int Id { get; set; }
    public int JogoId { get; set; }
    public int UtilizadorId { get; set; }
    public string TipoAposta { get; set; } = "1";
    public decimal ValorApostado { get; set; }
    public decimal OddMomento { get; set; }
    public int Estado { get; set; }
    public DateTime DataHoraUtc { get; set; }
}

public sealed class Utilizador
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}

public sealed class EstatisticasJogo
{
    public string CodigoJogo { get; set; } = string.Empty;
    public decimal TotalApostado { get; set; }
    public int ApostasCasa { get; set; }
    public int ApostasEmpate { get; set; }
    public int ApostasFora { get; set; }
    public int Pendentes { get; set; }
    public int Ganhas { get; set; }
    public int Perdidas { get; set; }
    public int Anuladas { get; set; }
    public decimal MargemPlataforma { get; set; }
}
