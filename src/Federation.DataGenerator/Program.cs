using System.Net.Http.Json;
using System.Text.RegularExpressions;

var equipas = new[]
{
    "Benfica", "Porto", "Sporting", "Braga", "Vitória SC", "Boavista", "Famalicão", "Rio Ave", "Casa Pia",
    "Estoril", "Arouca", "Gil Vicente", "Nacional", "Farense", "Moreirense", "AVS", "Santa Clara", "Estrela"
};

var ano = DateTime.UtcNow.Year;
var apiBase = Environment.GetEnvironmentVariable("RESULTS_API_BASE") ?? "http://localhost:5001";

using var http = new HttpClient { BaseAddress = new Uri(apiBase) };

Console.WriteLine($"Resultados API: {apiBase}");

var jornada = await ObterProximaJornadaAsync(http, ano);
Console.WriteLine($"Jornada selecionada: {jornada:00}");

var jogos = GerarCalendario(equipas, jornada, ano);

Console.WriteLine("[FASE 1] Publicar calendário...");
foreach (var jogo in jogos)
{
    var response = await http.PostAsJsonAsync("/api/jogos", new
    {
        jogo.CodigoJogo,
        jogo.DataJogo,
        jogo.HoraInicio,
        jogo.EquipaCasa,
        jogo.EquipaFora,
        Estado = 1
    });

    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
        Console.WriteLine($"Jogo já existe, a ignorar: {jogo.CodigoJogo}");
        continue;
    }

    response.EnsureSuccessStatusCode();
    Console.WriteLine($"Jogo criado: {jogo.CodigoJogo} ({jogo.EquipaCasa} vs {jogo.EquipaFora})");
}

Console.WriteLine("[FASE 2] Simular jogos em paralelo...");
await Task.WhenAll(jogos.Select(j => SimularJogoAsync(j, http)));

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

static async Task SimularJogoAsync(JogoSimulado jogo, HttpClient http)
{
    var rnd = new Random(Guid.NewGuid().GetHashCode());

    jogo.Estado = 2;
    await PublicarAtualizacao(jogo, http);

    for (var minuto = 10; minuto <= 90; minuto += 10)
    {
        if (rnd.NextDouble() < 0.15) jogo.GolosCasa++;
        if (rnd.NextDouble() < 0.13) jogo.GolosFora++;

        await PublicarAtualizacao(jogo, http);
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    jogo.Estado = 3;
    await PublicarAtualizacao(jogo, http);
}

static async Task PublicarAtualizacao(JogoSimulado jogo, HttpClient http)
{
    var response = await http.PutAsJsonAsync($"/api/jogos/{jogo.CodigoJogo}", new
    {
        jogo.CodigoJogo,
        jogo.Estado,
        jogo.GolosCasa,
        jogo.GolosFora
    });

    response.EnsureSuccessStatusCode();
    Console.WriteLine($"[{jogo.CodigoJogo}] Estado={jogo.Estado} {jogo.GolosCasa}-{jogo.GolosFora}");
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
