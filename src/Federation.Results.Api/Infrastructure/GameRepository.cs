using System.Data;
using Dapper;
using Federation.Results.Api.Application;
using Federation.Results.Api.Domain;
using Microsoft.Data.SqlClient;

namespace Federation.Results.Api.Infrastructure;

public sealed class GameRepository(IConfiguration configuration) : IGameRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("ResultsDb")
        ?? throw new InvalidOperationException("Connection string ResultsDb não configurada.");

    private IDbConnection Open() => new SqlConnection(_connectionString);

    public async Task<int> InsertAsync(CreateGameRequest request, CancellationToken ct)
    {
        using var db = Open();
        var p = new DynamicParameters();
        p.Add("@CodigoJogo", request.CodigoJogo);
        p.Add("@DataJogo", request.DataJogo.ToDateTime(TimeOnly.MinValue), DbType.Date);
        p.Add("@HoraInicio", request.HoraInicio.ToTimeSpan(), DbType.Time);
        p.Add("@EquipaCasa", request.EquipaCasa);
        p.Add("@EquipaFora", request.EquipaFora);
        p.Add("@Estado", request.Estado);

        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "sp_Resultados_InserirJogo",
            p,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(UpdateGameRequest request, CancellationToken ct)
    {
        using var db = Open();

        var existe = await db.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(
            "sp_Resultados_ObterJogoPorCodigo",
            new { CodigoJogo = request.CodigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        if (existe is null)
            return false;

        var p = new DynamicParameters();
        p.Add("@CodigoJogo", request.CodigoJogo);
        p.Add("@Estado", request.Estado);
        p.Add("@GolosCasa", request.GolosCasa);
        p.Add("@GolosFora", request.GolosFora);

        await db.ExecuteAsync(new CommandDefinition(
            "sp_Resultados_AtualizarJogo",
            p,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return true;
    }

    public async Task<Game?> GetByCodeAsync(string codigoJogo, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync<Game>(new CommandDefinition(
            "sp_Resultados_ObterJogoPorCodigo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Game>> ListAsync(GameQuery query, CancellationToken ct)
    {
        using var db = Open();

        var p = new DynamicParameters();
        p.Add("@DataJogo", query.Data?.ToDateTime(TimeOnly.MinValue), DbType.Date);
        p.Add("@Estado", query.Estado);

        var rows = await db.QueryAsync<Game>(new CommandDefinition(
            "sp_Resultados_ListarJogos",
            p,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<bool> DeleteAsync(string codigoJogo, CancellationToken ct)
    {
        using var db = Open();

        var game = await db.QueryFirstOrDefaultAsync<Game>(new CommandDefinition(
            "sp_Resultados_ObterJogoPorCodigo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        if (game is null || game.Estado != GameStatus.Agendado)
            return false;

        await db.ExecuteAsync(new CommandDefinition(
            "sp_Resultados_RemoverJogo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return true;
    }
}
