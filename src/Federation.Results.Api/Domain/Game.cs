namespace Federation.Results.Api.Domain;

public sealed class Game
{
    public int Id { get; set; }
    public required string CodigoJogo { get; set; }
    public DateOnly DataJogo { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public required string EquipaCasa { get; set; }
    public required string EquipaFora { get; set; }
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public GameStatus Estado { get; set; } = GameStatus.Agendado;
}
