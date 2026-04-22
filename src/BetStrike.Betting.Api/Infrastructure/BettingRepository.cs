using System.Data;
using BetStrike.Betting.Api.Application;
using BetStrike.Betting.Api.Domain;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BetStrike.Betting.Api.Infrastructure;

public sealed class BettingRepository(IConfiguration configuration) : IBettingRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("ApostasDb")
        ?? throw new InvalidOperationException("Connection string ApostasDb não configurada.");

    private IDbConnection Open() => new SqlConnection(_connectionString);

    public async Task<int> InserirJogoAsync(InserirJogoRequest request, CancellationToken ct)
    {
        using var db = Open();
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "sp_Apostas_InserirJogo",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<bool> AtualizarJogoResultadoAsync(AtualizarJogoRequest request, CancellationToken ct)
    {
        using var db = Open();

        var existe = await db.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(
            "sp_Apostas_ObterJogo",
            new { CodigoJogo = request.CodigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        if (existe is null)
            return false;

        await db.ExecuteAsync(new CommandDefinition(
            "sp_Apostas_AtualizarEstadoResultadoJogo",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return true;
    }

    public async Task<IReadOnlyList<Jogo>> ListarJogosAsync(FiltroJogos filtro, CancellationToken ct)
    {
        using var db = Open();
        var rows = await db.QueryAsync<Jogo>(new CommandDefinition(
            "sp_Apostas_ConsultarJogos",
            new { DataJogo = filtro.Data, filtro.Estado, filtro.Competicao },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<Jogo?> ObterJogoAsync(string codigoJogo, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync<Jogo>(new CommandDefinition(
            "sp_Apostas_ObterJogo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<bool> RemoverJogoAsync(string codigoJogo, CancellationToken ct)
    {
        using var db = Open();
        var affected = await db.ExecuteAsync(new CommandDefinition(
            "sp_Apostas_RemoverJogo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return affected > 0;
    }

    public async Task<int> RegistarApostaAsync(RegistarApostaRequest request, CancellationToken ct)
    {
        using var db = Open();
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "sp_Apostas_InserirAposta",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<bool> CancelarApostaAsync(CancelarApostaRequest request, CancellationToken ct)
    {
        using var db = Open();
        var affected = await db.ExecuteAsync(new CommandDefinition(
            "sp_Apostas_CancelarAposta",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return affected > 0;
    }

    public async Task<Aposta?> ObterApostaAsync(int apostaId, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync<Aposta>(new CommandDefinition(
            "sp_Apostas_ObterAposta",
            new { ApostaId = apostaId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Aposta>> ListarApostasAsync(FiltroApostas filtro, CancellationToken ct)
    {
        using var db = Open();
        var rows = await db.QueryAsync<Aposta>(new CommandDefinition(
            "sp_Apostas_ConsultarApostas",
            filtro,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<int> CriarUtilizadorAsync(CriarUtilizadorRequest request, CancellationToken ct)
    {
        using var db = Open();
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "sp_Apostas_CriarUtilizador",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UtilizadorComSaldo>> ListarUtilizadoresAsync(CancellationToken ct)
    {
        using var db = Open();
        
        const string sql = @"
            SELECT 
                u.Id,
                u.Nome,
                u.Email,
                0 AS SaldoDisponivel,
                COALESCE(SUM(a.ValorApostado), 0) AS SaldoGastoTotal,
                u.CriadoEmUtc
            FROM dbo.Utilizador u
            LEFT JOIN dbo.Aposta a ON u.Id = a.UtilizadorId
            GROUP BY u.Id, u.Nome, u.Email, u.CriadoEmUtc
            ORDER BY u.Id DESC";

        var rows = await db.QueryAsync<UtilizadorComSaldo>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<bool> InserirResultadoAsync(InserirResultadoRequest request, CancellationToken ct)
    {
        using var db = Open();
        var affected = await db.ExecuteAsync(new CommandDefinition(
            "sp_Apostas_InserirResultado",
            request,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        return affected > 0;
    }

    public async Task<Resultado?> ObterResultadoAsync(int jogoId, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync<Resultado>(new CommandDefinition(
            "sp_Apostas_ObterResultado",
            new { JogoId = jogoId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<EstatisticasJogo?> EstatisticasJogoAsync(string codigoJogo, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync<EstatisticasJogo>(new CommandDefinition(
            "sp_Apostas_EstatisticasPorJogo",
            new { CodigoJogo = codigoJogo },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }

    public async Task<object?> EstatisticasCompeticaoAsync(string competicao, CancellationToken ct)
    {
        using var db = Open();
        return await db.QueryFirstOrDefaultAsync(new CommandDefinition(
            "sp_Apostas_EstatisticasPorCompeticao",
            new { Competicao = competicao },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
    }
}
