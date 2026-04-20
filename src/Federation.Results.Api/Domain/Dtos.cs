namespace Federation.Results.Api.Domain;

public sealed record CreateGameRequest(
    string CodigoJogo,
    DateOnly DataJogo,
    TimeOnly HoraInicio,
    string EquipaCasa,
    string EquipaFora,
    int Estado = 1);

public sealed record UpdateGameRequest(
    string CodigoJogo,
    int Estado,
    int GolosCasa,
    int GolosFora);

public sealed record GameQuery(DateOnly? Data, int? Estado);
