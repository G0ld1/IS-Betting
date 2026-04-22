param(
    [string]$SqlServer = "(localdb)\MSSQLLocalDB",
    [string]$ResultsApiBase = "http://localhost:5001",
    [string]$BettingApiBase = "http://localhost:5002"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $root "logs\professor"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$resultsLogOut = Join-Path $logsDir "results-api.out.log"
$resultsLogErr = Join-Path $logsDir "results-api.err.log"
$bettingLogOut = Join-Path $logsDir "betting-api.out.log"
$bettingLogErr = Join-Path $logsDir "betting-api.err.log"

$resultsProc = $null
$bettingProc = $null

function Assert-Tool {
    param([Parameter(Mandatory = $true)][string]$ToolName)

    if (-not (Get-Command $ToolName -ErrorAction SilentlyContinue)) {
        throw "Ferramenta obrigatória não encontrada: $ToolName"
    }
}

function Wait-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSec = 45
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

function Run-SqlScript {
    param([Parameter(Mandatory = $true)][string]$ScriptPath)

    if (-not (Test-Path $ScriptPath)) {
        throw "Script SQL não encontrado: $ScriptPath"
    }

    Write-Host "A executar SQL: $ScriptPath"
    sqlcmd -S $SqlServer -d master -b -i $ScriptPath
}

try {
    Assert-Tool -ToolName "dotnet"
    Assert-Tool -ToolName "sqlcmd"

    Write-Host "[1/5] Criar/atualizar schema, SPs e trigger..."
    Run-SqlScript -ScriptPath (Join-Path $root "database\00_Execucao_Ordem.sql")

    Write-Host "[2/5] Limpar dados antigos..."
    Run-SqlScript -ScriptPath (Join-Path $root "database\99_Limpeza_Dados.sql")

    Write-Host "[3/5] Arrancar APIs..."
    $resultsProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory (Join-Path $root "src\Federation.Results.Api") -RedirectStandardOutput $resultsLogOut -RedirectStandardError $resultsLogErr -PassThru
    $bettingProc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory (Join-Path $root "src\BetStrike.Betting.Api") -RedirectStandardOutput $bettingLogOut -RedirectStandardError $bettingLogErr -PassThru

    Write-Host "[4/5] Aguardar APIs prontas..."
    Wait-Api -Url "$ResultsApiBase/api/jogos"
    Wait-Api -Url "$BettingApiBase/api/apostas/jogos"

    Write-Host "[5/5] Injetar dados de demo (jogos + utilizadores + apostas + resultados)..."
    $env:RESULTS_API_BASE = $ResultsApiBase
    $env:BETTING_API_BASE = $BettingApiBase

    Push-Location (Join-Path $root "src\Federation.DataGenerator")
    try {
        dotnet run
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "Preparação concluída."
    Write-Host "- API Federação: $ResultsApiBase"
    Write-Host "- API Apostas:   $BettingApiBase"
    Write-Host "- Frontend:      http://localhost:8080 (servir pasta frontend)"
    Write-Host ""
    Write-Host "As APIs foram iniciadas em background e mantêm-se ativas."
    Write-Host "Logs em: $logsDir"
}
catch {
    Write-Error $_

    if ($resultsProc -and -not $resultsProc.HasExited) {
        Stop-Process -Id $resultsProc.Id -Force
    }

    if ($bettingProc -and -not $bettingProc.HasExited) {
        Stop-Process -Id $bettingProc.Id -Force
    }

    throw
}
