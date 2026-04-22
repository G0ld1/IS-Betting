param(
    [string]$ResultsApiBase = "http://localhost:5001",
    [string]$BettingApiBase = "http://localhost:5002"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $root "logs\demo"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$resultsLogOut = Join-Path $logsDir "results-api.out.log"
$resultsLogErr = Join-Path $logsDir "results-api.err.log"
$bettingLogOut = Join-Path $logsDir "betting-api.out.log"
$bettingLogErr = Join-Path $logsDir "betting-api.err.log"

function Wait-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSec = 40
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 3 | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 700
        }
    }

    throw "Timeout ao aguardar API em $Url"
}

function Run-Sql {
    param(
        [Parameter(Mandatory = $true)][string]$Query,
        [string]$Database = "master"
    )

    sqlcmd -S "(localdb)\MSSQLLocalDB" -d $Database -Q $Query
}

function Pause-Step {
    param([Parameter(Mandatory = $true)][string]$Message)

    Write-Host ""
    Read-Host "$Message (carrega Enter para continuar)" | Out-Null
}

$resultsProc = $null
$bettingProc = $null


    Pause-Step -Message "Iniciar demo"

    Write-Host "[1/5] Arrancar Results API ($ResultsApiBase)..."
    $resultsProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory (Join-Path $root "src\Federation.Results.Api") -RedirectStandardOutput $resultsLogOut -RedirectStandardError $resultsLogErr -PassThru

    Write-Host "[2/5] Arrancar Betting API ($BettingApiBase)..."
    $bettingProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory (Join-Path $root "src\BetStrike.Betting.Api") -RedirectStandardOutput $bettingLogOut -RedirectStandardError $bettingLogErr -PassThru

    Write-Host "[3/5] Esperar APIs ficarem prontas..."
    Wait-Api -Url "$ResultsApiBase/api/jogos"
    Wait-Api -Url "$BettingApiBase/api/apostas/jogos"

    Pause-Step -Message "APIs prontas"

    Write-Host "[4/5] Correr simulador (publica e atualiza nas duas APIs)..."
    $env:RESULTS_API_BASE = $ResultsApiBase
    $env:BETTING_API_BASE = $BettingApiBase
    Push-Location (Join-Path $root "src\Federation.DataGenerator")
    try {
        dotnet run
    }
    finally {
        Pop-Location
    }

    Pause-Step -Message "Simulação concluída"

    Write-Host "[5/5] Queries de prova"

    Write-Host ""
    Write-Host "--- PROVA 1: Ultimos 9 jogos em Resultados ---"
    Run-Sql -Database "ResultadosFutebol" -Query @"
SELECT TOP 9 CodigoJogo, Estado
FROM dbo.Jogo
ORDER BY Id DESC;
"@

    Write-Host ""
    Write-Host "--- PROVA 2: Mesmos jogos em Apostas ---"
    Run-Sql -Database "Apostas" -Query @"
SELECT TOP 9 CodigoJogo, Estado
FROM dbo.Jogo
ORDER BY Id DESC;
"@

    Write-Host ""
    Write-Host "--- PROVA 3: Comparacao estado Resultados vs Apostas (ultimos 9 codigos) ---"
    Run-Sql -Database "master" -Query @"
;WITH Ultimos AS
(
    SELECT TOP 9 CodigoJogo
    FROM ResultadosFutebol.dbo.Jogo
    ORDER BY Id DESC
)
SELECT
    u.CodigoJogo,
    r.Estado AS EstadoResultados,
    a.Estado AS EstadoApostas
FROM Ultimos u
LEFT JOIN ResultadosFutebol.dbo.Jogo r ON r.CodigoJogo = u.CodigoJogo
LEFT JOIN Apostas.dbo.Jogo a ON a.CodigoJogo = u.CodigoJogo
ORDER BY u.CodigoJogo DESC;
"@

    Write-Host ""
    Write-Host "--- PROVA 4: Resultado das ultimas apostas ---"
    Run-Sql -Database "Apostas" -Query @"
SELECT TOP 10
    a.Id AS ApostaId,
    j.CodigoJogo,
    a.UtilizadorId,
    a.TipoAposta,
    a.ValorApostado,
    a.OddMomento,
    a.Estado AS EstadoAposta,
    CASE a.Estado
        WHEN 1 THEN 'Pendente'
        WHEN 2 THEN 'Ganha'
        WHEN 3 THEN 'Perdida'
        WHEN 4 THEN 'Anulada'
        ELSE 'Desconhecido'
    END AS EstadoDescricao,
    CAST(a.ValorApostado * a.OddMomento AS DECIMAL(12,2)) AS PremioPotencial,
    r.GolosCasa,
    r.GolosFora,
    a.DataHoraUtc
FROM dbo.Aposta a
INNER JOIN dbo.Jogo j ON j.Id = a.JogoId
LEFT JOIN dbo.Resultado r ON r.JogoId = j.Id
ORDER BY a.Id DESC;
"@

    Write-Host ""
    Write-Host "--- PROVA 5: Movimentos em Pagamentos (ultimos 15) ---"
    Run-Sql -Database "Pagamentos" -Query @"
SELECT TOP 15
    Id,
    ApostaId,
    UtilizadorId,
    Tipo,
    Valor,
    Estado,
    DataHoraUtc
FROM dbo.Transacao
ORDER BY Id DESC;
"@

    Pause-Step -Message "Provas apresentadas"

    Write-Host ""
    Write-Host "Demo concluida com sucesso."


    if ($resultsProc -and -not $resultsProc.HasExited) {
        Stop-Process -Id $resultsProc.Id -Force
    }

    if ($bettingProc -and -not $bettingProc.HasExited) {
        Stop-Process -Id $bettingProc.Id -Force
    }


