namespace BetStrike.Betting.Api.Domain;

public enum GameStatus { Agendado = 1, EmCurso = 2, Finalizado = 3, Cancelado = 4, Adiado = 5 }
public enum BetStatus { Pendente = 1, Ganha = 2, Perdida = 3, Anulada = 4 }
public enum BetType { Casa = 1, Empate = 2, Fora = 3 }
