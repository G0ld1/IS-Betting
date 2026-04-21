using System.Net.Http.Json;
using System.Text.RegularExpressions;

var equipas = new[]
{
    "Benfica", "Porto", "Sporting", "Braga", "Vitória SC", "Boavista", "Famalicão", "Rio Ave", "Casa Pia",
    "Estoril", "Arouca", "Gil Vicente", "Nacional", "Farense", "Moreirense", "AVS", "Santa Clara", "Estrela"
};

var ano = DateTime.UtcNow.Year;
var resultsApiBase = Environment.GetEnvironmentVariable("RESULTS_API_BASE") ?? "http://localhost:5001";
var bettingApiBase = Environment.GetEnvironmentVariable("BETTING_API_BASE") ?? "http://localhost:5002";

using var resultsHttp = new HttpClient { BaseAddress = new Uri(resultsApiBase) };
using var bettingHttp = new HttpClient { BaseAddress = new Uri(bettingApiBase) };

Console.WriteLine($"Resultados API: {resultsApiBase}");
Console.WriteLine($"Apostas API: {bettingApiBase}");

var jornada = await ObterProximaJornadaAsync(resultsHttp, ano);
Console.WriteLine($"Jornada selecionada: {jornada:00}");

var jogos = GerarCalendario(equipas, jornada, ano);

Console.WriteLine("[FASE 1] Publicar calendário...");
foreach (var jogo in jogos)
{
    var responseResults = await resultsHttp.PostAsJsonAsync("/api/jogos", new
    {
        jogo.CodigoJogo,
        jogo.DataJogo,
        jogo.HoraInicio,
        jogo.EquipaCasa,
        jogo.EquipaFora,
        Estado = 1
    });

    var responseBetting = await bettingHttp.PostAsJsonAsync("/api/apostas/jogos", new
    {
        jogo.CodigoJogo,
        jogo.DataJogo,
        jogo.HoraInicio,
        jogo.EquipaCasa,
        jogo.EquipaFora,
        Competicao = "Primeira Liga",
        Estado = 1
    });

    if (responseResults.StatusCode == System.Net.HttpStatusCode.Conflict && responseBetting.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
        Console.WriteLine($"Jogo já existe nas duas APIs, a ignorar criação: {jogo.CodigoJogo}");
        continue;
    }

    if (responseResults.StatusCode != System.Net.HttpStatusCode.Conflict)
        responseResults.EnsureSuccessStatusCode();

    if (responseBetting.StatusCode != System.Net.HttpStatusCode.Conflict)
        responseBetting.EnsureSuccessStatusCode();

    Console.WriteLine($"Jogo criado: {jogo.CodigoJogo} ({jogo.EquipaCasa} vs {jogo.EquipaFora})");
}

Console.WriteLine("[FASE 1.5] Criar utilizadores e registar apostas automáticas...");
var utilizadoresDemo = await CriarUtilizadoresDemoAsync(bettingHttp, 8);
await RegistarApostasDemoAsync(jogos, bettingHttp, utilizadoresDemo, 2);

Console.WriteLine("[FASE 2] Simular jogos em paralelo...");
await Task.WhenAll(jogos.Select(j => SimularJogoAsync(j, resultsHttp, bettingHttp)));

Console.WriteLine("Simulação concluída.");

static async Task<int> ObterProximaJornadaAsync(HttpClient http, int ano)
{
    var jogos = await http.GetFromJsonAsync<List<JogoExistente>>("/api/jogos") ?? new();

    var regex = new Regex(@"^FUT-(\d{4})-(\d{2})(\d{2})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    var maxJornada = 0;

    foreach (var j in jogos)
    {
        if (string.IsNullOrWhiteSpace(j.CodigoJogo)) continue;

        var m = regex.Match(j.CodigoJogo);
        if (!m.Success) continue;

        if (!int.TryParse(m.Groups[1].Value, out var anoCodigo) || anoCodigo != ano) continue;
        if (!int.TryParse(m.Groups[2].Value, out var jornadaCodigo)) continue;

        if (jornadaCodigo > maxJornada) maxJornada = jornadaCodigo;
    }

    var proxima = maxJornada + 1;
    return proxima <= 99 ? proxima : 1;
}

static List<JogoSimulado> GerarCalendario(string[] equipas, int jornada, int ano)
{
    var rnd = new Random();
    var baralhadas = equipas.OrderBy(_ => rnd.Next()).ToArray();

    var dataBase = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    var horaBase = new TimeOnly(18, 0);

    var lista = new List<JogoSimulado>();
    for (int i = 0; i < 18; i += 2)
    {
        var numeroJogo = (i / 2) + 1;
        lista.Add(new JogoSimulado
        {
            CodigoJogo = $"FUT-{ano}-{jornada:00}{numeroJogo:00}",
            DataJogo = dataBase,
            HoraInicio = horaBase.AddMinutes(numeroJogo * 20),
            EquipaCasa = baralhadas[i],
            EquipaFora = baralhadas[i + 1],
            GolosCasa = 0,
            GolosFora = 0,
            Estado = 1
        });
    }

    return lista;
}

static async Task SimularJogoAsync(JogoSimulado jogo, HttpClient resultsHttp, HttpClient bettingHttp)
{
    var rnd = new Random(Guid.NewGuid().GetHashCode());

    jogo.Estado = 2;
    await PublicarAtualizacao(jogo, resultsHttp, bettingHttp);

    for (var minuto = 10; minuto <= 90; minuto += 10)
    {
        if (rnd.NextDouble() < 0.15) jogo.GolosCasa++;
        if (rnd.NextDouble() < 0.13) jogo.GolosFora++;

        await PublicarAtualizacao(jogo, resultsHttp, bettingHttp);
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    jogo.Estado = 3;
    await PublicarAtualizacao(jogo, resultsHttp, bettingHttp);
}

static async Task PublicarAtualizacao(JogoSimulado jogo, HttpClient resultsHttp, HttpClient bettingHttp)
{
    var resultsUpdateTask = resultsHttp.PutAsJsonAsync($"/api/jogos/{jogo.CodigoJogo}", new
    {
        jogo.CodigoJogo,
        jogo.Estado,
        jogo.GolosCasa,
        jogo.GolosFora
    });

    var bettingUpdateTask = bettingHttp.PutAsJsonAsync($"/api/apostas/jogos/{jogo.CodigoJogo}", new
    {
        jogo.CodigoJogo,
        jogo.Estado,
        jogo.GolosCasa,
        jogo.GolosFora
    });

    await Task.WhenAll(resultsUpdateTask, bettingUpdateTask);

    var responseResults = await resultsUpdateTask;
    var responseBetting = await bettingUpdateTask;

    responseResults.EnsureSuccessStatusCode();
    responseBetting.EnsureSuccessStatusCode();
    Console.WriteLine($"[{jogo.CodigoJogo}] Estado={jogo.Estado} {jogo.GolosCasa}-{jogo.GolosFora}");
}

static async Task<List<int>> CriarUtilizadoresDemoAsync(HttpClient bettingHttp, int quantidade)
{
    var utilizadores = new List<int>(quantidade);

    for (var i = 1; i <= quantidade; i++)
    {
        var email = $"demo.auto.{DateTime.UtcNow:yyyyMMddHHmmss}.{Guid.NewGuid():N}@betstrike.local";
        var response = await bettingHttp.PostAsJsonAsync("/api/utilizadores", new
        {
            Nome = $"Demo Auto {i}",
            Email = email
        });

        response.EnsureSuccessStatusCode();
        var utilizadorId = await response.Content.ReadFromJsonAsync<int>();

        if (utilizadorId <= 0)
            throw new InvalidOperationException("Não foi possível criar utilizador demo.");

        utilizadores.Add(utilizadorId);
        Console.WriteLine($"Utilizador criado: {utilizadorId} ({email})");
    }

    return utilizadores;
}

static async Task RegistarApostasDemoAsync(IReadOnlyList<JogoSimulado> jogos, HttpClient bettingHttp, IReadOnlyList<int> utilizadores, int apostasPorJogo)
{
    if (utilizadores.Count == 0)
        throw new InvalidOperationException("Sem utilizadores demo para registar apostas.");

    var rnd = new Random();
    var tipos = new[] { "1", "X", "2" };
    var saldoDisponivel = utilizadores.ToDictionary(u => u, _ => 50.00m);
    var totalCriadas = 0;
    var totalFalhadas = 0;

    foreach (var jogo in jogos)
    {
        var detalheJogo = await bettingHttp.GetFromJsonAsync<JogoApostasDto>($"/api/apostas/jogos/{jogo.CodigoJogo}")
            ?? throw new InvalidOperationException($"Jogo não encontrado na API de Apostas: {jogo.CodigoJogo}");

        for (var i = 0; i < apostasPorJogo; i++)
        {
            var apostaRegistada = false;

            for (var tentativa = 1; tentativa <= 5 && !apostaRegistada; tentativa++)
            {
                var valorApostado = decimal.Round(2m + (decimal)rnd.NextDouble() * 8m, 2);
                var elegiveis = saldoDisponivel
                    .Where(x => x.Value >= valorApostado)
                    .Select(x => x.Key)
                    .ToList();

                if (elegiveis.Count == 0)
                {
                    Console.WriteLine($"Sem utilizadores com saldo para apostar no jogo {jogo.CodigoJogo}. A avançar.");
                    break;
                }

                var utilizadorId = elegiveis[rnd.Next(elegiveis.Count)];
                var tipo = tipos[rnd.Next(tipos.Length)];
                var odd = decimal.Round(1.20m + (decimal)rnd.NextDouble() * 2.30m, 2);

                var response = await bettingHttp.PostAsJsonAsync("/api/apostas", new
                {
                    JogoId = detalheJogo.Id,
                    UtilizadorId = utilizadorId,
                    TipoAposta = tipo,
                    ValorApostado = valorApostado,
                    OddMomento = odd
                });

                if (response.IsSuccessStatusCode)
                {
                    var apostaId = await response.Content.ReadFromJsonAsync<int>();
                    saldoDisponivel[utilizadorId] -= valorApostado;
                    totalCriadas++;
                    apostaRegistada = true;

                    Console.WriteLine($"Aposta criada: {apostaId} Jogo={jogo.CodigoJogo} Utilizador={utilizadorId} Tipo={tipo} Valor={valorApostado} Odd={odd} SaldoRestante={saldoDisponivel[utilizadorId]:0.00}");
                    continue;
                }

                var erro = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Falha ao criar aposta (tentativa {tentativa}) Jogo={jogo.CodigoJogo} HTTP={(int)response.StatusCode}: {erro}");

                if (erro.Contains("Saldo insuficiente", StringComparison.OrdinalIgnoreCase))
                    saldoDisponivel[utilizadorId] = 0;
            }

            if (!apostaRegistada)
                totalFalhadas++;
        }
    }

    Console.WriteLine($"Resumo apostas automáticas: criadas={totalCriadas}, falhadas={totalFalhadas}");
}

file sealed class JogoSimulado
{
    public required string CodigoJogo { get; set; }
    public DateOnly DataJogo { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public required string EquipaCasa { get; set; }
    public required string EquipaFora { get; set; }
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public int Estado { get; set; }
}

file sealed class JogoExistente
{
    public string CodigoJogo { get; set; } = string.Empty;
}

file sealed class JogoApostasDto
{
    public int Id { get; set; }
    public string CodigoJogo { get; set; } = string.Empty;
}
