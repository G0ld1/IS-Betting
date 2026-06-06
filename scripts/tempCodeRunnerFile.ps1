param(
    [string]$BettingApi = "http://localhost:5002",
    [int]$Apostas = 75
)

$ErrorActionPreference = "Stop"

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null
    )

    $params = @{
        Method = $Method
        Uri = $Uri
        Headers = @{ Accept = "application/json" }
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json"
        $params.Body = ($Body | ConvertTo-Json -Depth 8)
    }

    try {
        Invoke-RestMethod @params
    }
    catch {
        Write-Host ""
        Write-Host "Pedido falhou:" -ForegroundColor Red
        Write-Host "  $Method $Uri"
        if ($null -ne $Body) {
            Write-Host "  Body: $($params.Body)"
        }

        $response = $_.Exception.Response
        if ($null -ne $response) {
            Write-Host "  HTTP: $([int]$response.StatusCode) $($response.StatusDescription)"
            $stream = $response.GetResponseStream()
            if ($null -ne $stream) {
                $reader = New-Object System.IO.StreamReader($stream)
                Write-Host ($reader.ReadToEnd())
            }
        }
        else {
            Write-Host $_.Exception.Message
        }

        throw
    }
}

$run = Get-Date -Format "yyyyMMddHHmmss"
$codigoJogo = "FUT-2026-$(Get-Random -Minimum 1000 -Maximum 9999)"

Write-Host "Criar utilizadores de teste..."
$utilizadores = New-Object System.Collections.Generic.List[int]
$utilizadoresNecessarios = [Math]::Max(1, $Apostas)

1..$utilizadoresNecessarios | ForEach-Object {
    $utilizadorId = Invoke-Json POST "$BettingApi/api/utilizadores" @{
        nome = "Load Test $run $_"
        email = "load.$run.$_@betstrike.local"
    }

    $utilizadores.Add([int]$utilizadorId)
}

Write-Host "Criar jogo de teste $codigoJogo..."
$jogoId = Invoke-Json POST "$BettingApi/api/apostas/jogos" @{
    codigoJogo = $codigoJogo
    dataJogo = "2026-06-29"
    horaInicio = "18:00:00"
    equipaCasa = "Benfica"
    equipaFora = "Porto"
    competicao = "Teste CargaS"
    estado = 2
}

Write-Host "Registar $Apostas apostas para gerar carga no stream..."
1..$Apostas | ForEach-Object {
    $valor = if ($_ % 10 -eq 0) { 45 } else { 10 + ($_ % 25) }
    $tipo = @("1", "X", "2")[($_ % 3)]
    $utilizadorId = $utilizadores[$_ - 1]

    Invoke-Json POST "$BettingApi/api/apostas" @{
        jogoId = [int]$jogoId
        utilizadorId = [int]$utilizadorId
        tipoAposta = $tipo
        valorApostado = [decimal]$valor
        oddMomento = 1.8
    } | Out-Null
}

Write-Host "Aguardar processamento assincrono..."
Start-Sleep -Seconds 8

$dashboard = Invoke-Json GET "$BettingApi/api/analytics/dashboard"
$stream = Invoke-Json GET "$BettingApi/api/analytics/stream"

Write-Host ""
Write-Host "Resumo analytics:"
Write-Host "  Volume total apostado: $($dashboard.resumo.volumeTotalApostado)"
Write-Host "  Apostas por minuto:    $($dashboard.resumo.apostasPorMinuto)"
Write-Host "  Alertas recentes:      $($dashboard.alertas.Count)"
Write-Host ""
Write-Host "Estado do stream:"
Write-Host "  Broker:                $($stream.broker)"
Write-Host "  Ultima sequencia:      $($stream.lastEventSequence)"
Write-Host "  Replay checkpoint:     $($stream.replayCheckpoint)"
Write-Host "  Eventos pendentes:     $($stream.pendingEvents)"
Write-Host ""
Write-Host "Para testar falha/reprocessamento: pare o betstrike-streaming-worker, execute este script, volte a iniciar o worker e confirme que o dashboard recupera os eventos em falta."
