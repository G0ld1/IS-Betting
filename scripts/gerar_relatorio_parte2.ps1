param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\docs\Relatorio_Parte2_BetStrike.docx")
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Escape-Xml([string]$Text) {
    return [System.Security.SecurityElement]::Escape($Text)
}

function Run([string]$Text, [switch]$Bold, [switch]$Italic, [int]$Size = 22) {
    $properties = "<w:sz w:val=`"$Size`"/><w:szCs w:val=`"$Size`"/>"
    if ($Bold) { $properties += "<w:b/>" }
    if ($Italic) { $properties += "<w:i/>" }
    return "<w:r><w:rPr>$properties</w:rPr><w:t xml:space=`"preserve`">$(Escape-Xml $Text)</w:t></w:r>"
}

function Paragraph(
    [string]$Text,
    [string]$Style = "Normal",
    [string]$Align = "both",
    [switch]$Bold,
    [switch]$Italic,
    [switch]$Bullet,
    [switch]$KeepNext
) {
    $pPr = "<w:pStyle w:val=`"$Style`"/><w:jc w:val=`"$Align`"/>"
    if ($Bullet) { $pPr += "<w:numPr><w:ilvl w:val=`"0`"/><w:numId w:val=`"1`"/></w:numPr>" }
    if ($KeepNext) { $pPr += "<w:keepNext/>" }
    return "<w:p><w:pPr>$pPr</w:pPr>$(Run $Text -Bold:$Bold -Italic:$Italic)</w:p>"
}

function Code-Paragraph([string]$Text) {
    $escaped = Escape-Xml $Text
    return "<w:p><w:pPr><w:pStyle w:val=`"Code`"/><w:jc w:val=`"left`"/></w:pPr><w:r><w:rPr><w:rFonts w:ascii=`"Consolas`" w:hAnsi=`"Consolas`"/><w:sz w:val=`"18`"/></w:rPr><w:t xml:space=`"preserve`">$escaped</w:t></w:r></w:p>"
}

function Diagram-Cell([string]$Text, [string]$Fill, [int]$Width, [string]$Color = "FFFFFF", [switch]$Bold) {
    $boldXml = if ($Bold) { "<w:b/>" } else { "" }
    return "<w:tc><w:tcPr><w:tcW w:w=`"$Width`" w:type=`"dxa`"/><w:shd w:fill=`"$Fill`"/><w:tcMar><w:top w:w=`"160`" w:type=`"dxa`"/><w:left w:w=`"100`" w:type=`"dxa`"/><w:bottom w:w=`"160`" w:type=`"dxa`"/><w:right w:w=`"100`" w:type=`"dxa`"/></w:tcMar><w:vAlign w:val=`"center`"/></w:tcPr><w:p><w:pPr><w:jc w:val=`"center`"/></w:pPr><w:r><w:rPr>$boldXml<w:color w:val=`"$Color`"/><w:sz w:val=`"19`"/></w:rPr><w:t>$(Escape-Xml $Text)</w:t></w:r></w:p></w:tc>"
}

function Architecture-Diagram {
    $arrow = "<w:tc><w:tcPr><w:tcW w:w=`"450`" w:type=`"dxa`"/><w:vAlign w:val=`"center`"/></w:tcPr><w:p><w:pPr><w:jc w:val=`"center`"/></w:pPr><w:r><w:rPr><w:b/><w:color w:val=`"2F75B5`"/><w:sz w:val=`"28`"/></w:rPr><w:t>→</w:t></w:r></w:p></w:tc>"
    $leftArrow = "<w:tc><w:tcPr><w:tcW w:w=`"450`" w:type=`"dxa`"/><w:vAlign w:val=`"center`"/></w:tcPr><w:p><w:pPr><w:jc w:val=`"center`"/></w:pPr><w:r><w:rPr><w:b/><w:color w:val=`"2F75B5`"/><w:sz w:val=`"28`"/></w:rPr><w:t>←</w:t></w:r></w:p></w:tc>"

    $xml = "<w:tbl><w:tblPr><w:tblW w:w=`"0`" w:type=`"auto`"/><w:tblCellSpacing w:w=`"70`" w:type=`"dxa`"/></w:tblPr><w:tblGrid>"
    foreach ($width in @(1750, 450, 1850, 450, 1850, 450, 2500)) { $xml += "<w:gridCol w:w=`"$width`"/>" }
    $xml += "</w:tblGrid>"

    $xml += "<w:tr>"
    $xml += Diagram-Cell "Betting API e Results API" "1F4E78" 1750 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "RabbitMQ + MassTransit" "C55A11" 1850 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "Middleware Worker" "548235" 1850 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "Arquivo de eventos, analytics, alertas e replay" "5B9BD5" 2500 -Bold
    $xml += "</w:tr>"

    $xml += "<w:tr>"
    $xml += Diagram-Cell "Betting API e Results API" "1F4E78" 1750 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "Apache Kafka" "7030A0" 1850 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "Streaming Worker" "548235" 1850 -Bold
    $xml += $arrow
    $xml += Diagram-Cell "Analytics e alertas em tempo quase real" "5B9BD5" 2500 -Bold
    $xml += "</w:tr>"

    $xml += "<w:tr>"
    $xml += Diagram-Cell "Frontend" "4472C4" 1750 -Bold
    $xml += $leftArrow
    $xml += Diagram-Cell "GET /api/analytics/dashboard" "D9EAF7" 1850 "1F1F1F" -Bold
    $xml += $leftArrow
    $xml += Diagram-Cell "Betting API" "1F4E78" 1850 -Bold
    $xml += "<w:tc><w:tcPr><w:tcW w:w=`"450`" w:type=`"dxa`"/></w:tcPr><w:p/></w:tc>"
    $xml += Diagram-Cell "SQL Server: dados operacionais e snapshots analíticos" "7F6000" 2500 -Bold
    $xml += "</w:tr></w:tbl>"

    return $xml
}

function Page-Break {
    return "<w:p><w:r><w:br w:type=`"page`"/></w:r></w:p>"
}

function Table([string[]]$Headers, [object[][]]$Rows, [int[]]$Widths) {
    $xml = "<w:tbl><w:tblPr><w:tblStyle w:val=`"ReportTable`"/><w:tblW w:w=`"0`" w:type=`"auto`"/></w:tblPr><w:tblGrid>"
    foreach ($width in $Widths) { $xml += "<w:gridCol w:w=`"$width`"/>" }
    $xml += "</w:tblGrid><w:tr>"
    for ($i = 0; $i -lt $Headers.Count; $i++) {
        $xml += "<w:tc><w:tcPr><w:tcW w:w=`"$($Widths[$i])`" w:type=`"dxa`"/><w:shd w:fill=`"1F4E78`"/></w:tcPr><w:p><w:r><w:rPr><w:b/><w:color w:val=`"FFFFFF`"/></w:rPr><w:t>$(Escape-Xml $Headers[$i])</w:t></w:r></w:p></w:tc>"
    }
    $xml += "</w:tr>"
    foreach ($row in $Rows) {
        $xml += "<w:tr>"
        for ($i = 0; $i -lt $Headers.Count; $i++) {
            $xml += "<w:tc><w:tcPr><w:tcW w:w=`"$($Widths[$i])`" w:type=`"dxa`"/></w:tcPr><w:p><w:pPr><w:jc w:val=`"left`"/></w:pPr><w:r><w:t>$(Escape-Xml ([string]$row[$i]))</w:t></w:r></w:p></w:tc>"
        }
        $xml += "</w:tr>"
    }
    return $xml + "</w:tbl>"
}

$body = New-Object System.Text.StringBuilder

[void]$body.Append("<w:p><w:pPr><w:spacing w:before=`"1400`"/><w:jc w:val=`"center`"/></w:pPr>$(Run "INTEGRAÇÃO DE SISTEMAS" -Bold -Size 30)</w:p>")
[void]$body.Append("<w:p><w:pPr><w:spacing w:before=`"1200`"/><w:jc w:val=`"center`"/></w:pPr>$(Run "Relatório Técnico - Parte 2" -Bold -Size 38)</w:p>")
[void]$body.Append("<w:p><w:pPr><w:spacing w:before=`"300`"/><w:jc w:val=`"center`"/></w:pPr>$(Run "Middleware Assíncrono, Streaming e Execução Containerizada" -Size 26)</w:p>")
[void]$body.Append("<w:p><w:pPr><w:spacing w:before=`"1000`"/><w:jc w:val=`"center`"/></w:pPr>$(Run "Projeto BetStrike" -Bold -Size 32)</w:p>")
[void]$body.Append("<w:p><w:pPr><w:spacing w:before=`"1400`"/><w:jc w:val=`"center`"/></w:pPr>$(Run "Autor: Filipe ____________________" -Size 22)</w:p>")
[void]$body.Append("<w:p><w:pPr><w:jc w:val=`"center`"/></w:pPr>$(Run "Data: 6 de junho de 2026" -Size 22)</w:p>")
[void]$body.Append((Page-Break))

[void]$body.Append((Paragraph "Resumo" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "Este relatório apresenta a evolução da plataforma BetStrike para uma arquitetura orientada a eventos. A solução combina dois tipos de middleware com responsabilidades distintas: RabbitMQ, através do MassTransit, para filas de mensagens e execução fiável de tarefas assíncronas; e Apache Kafka para streaming persistente de eventos, consumo independente e retoma por offsets. A solução inclui ainda APIs REST em .NET 8, SQL Server, workers dedicados, dashboard analítico, alertas automáticos e execução integral em containers Docker."))
[void]$body.Append((Paragraph "A separação entre o caminho transacional e o processamento assíncrono reduz o acoplamento entre componentes, evita que cálculos analíticos bloqueiem as operações principais e permite recuperar de falhas temporárias. Os mecanismos implementados incluem retries com intervalos, dead-letter queues, prioridades, idempotência, checkpoints e replay histórico."))

[void]$body.Append((Paragraph "1. Objetivos e âmbito" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "O objetivo da Parte 2 foi acrescentar capacidades de integração assíncrona à solução base da BetStrike. A plataforma operacional gere jogos, utilizadores, apostas e resultados através de APIs REST e SQL Server. Sobre essa base foram adicionadas duas camadas de middleware, responsáveis pela distribuição de eventos, atualização de indicadores, geração de alertas e recuperação após falhas."))
[void]$body.Append((Paragraph "Os principais requisitos abrangidos são:" "Normal" -KeepNext))
foreach ($item in @(
    "propagação de eventos em tempo quase real entre serviços;",
    "desacoplamento entre produtores e consumidores;",
    "processamento assíncrono de analytics e alertas;",
    "resiliência através de retries, DLQ, offsets e replay;",
    "evidência de carga, falhas e reprocessamento;",
    "execução completa num ambiente containerizado."
)) { [void]$body.Append((Paragraph $item -Bullet)) }

[void]$body.Append((Paragraph "2. Arquitetura lógica da solução" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "A arquitetura segue um modelo orientado a eventos. As APIs persistem primeiro as operações de negócio em SQL Server e publicam eventos de integração. Os workers consomem esses eventos de forma independente, arquivam o stream, atualizam o dashboard e geram alertas, sem prolongar o tempo de resposta das APIs."))
[void]$body.Append((Architecture-Diagram))
[void]$body.Append((Paragraph "Figura 1 - Fluxos principais da arquitetura orientada a eventos." "Normal" "center" -Italic))

$componentRows = @(
    @("BetStrike.Betting.Api", "API operacional para jogos, utilizadores, apostas, resultados e consulta de analytics."),
    @("Federation.Results.Api", "API federada que disponibiliza e atualiza jogos e resultados."),
    @("RabbitMQ + MassTransit", "Camada de filas para entrega de trabalho, retries com intervalos, prioridades e DLQ."),
    @("BetStrike.Middleware.Worker", "Consome filas RabbitMQ, arquiva eventos, publica alertas, atualiza dashboard e executa replay."),
    @("Apache Kafka", "Camada de streaming persistente organizada por topics e offsets."),
    @("BetStrike.Streaming.Worker", "Consome o stream Kafka, processa eventos, alertas e analytics de forma independente."),
    @("SQL Server", "Mantém dados operacionais, arquivo idempotente de eventos, checkpoint, alertas e tabelas de dashboard."),
    @("Frontend", "Apresenta operações e métricas em tempo quase real através das APIs.")
)
[void]$body.Append((Table @("Componente", "Função") $componentRows @(2800, 6500)))

[void]$body.Append((Paragraph "2.1. Os dois tipos de middleware" "Heading2" -KeepNext))
$middlewareRows = @(
    @("Middleware de mensagens / filas", "RabbitMQ com MassTransit", "Distribuir tarefas assíncronas que devem ser processadas por consumidores específicos, com controlo de retries, prioridade e mensagens não processáveis."),
    @("Middleware de streaming de eventos", "Apache Kafka", "Manter um fluxo persistente e ordenado por partição, permitindo vários consumidores, retoma e replay através de offsets.")
)
[void]$body.Append((Table @("Tipo", "Tecnologia", "Função principal") $middlewareRows @(2450, 2350, 4500)))
[void]$body.Append((Paragraph "A utilização simultânea não é redundante: a fila representa trabalho a entregar e concluir; o stream representa um histórico de acontecimentos que pode ser lido por diferentes subsistemas, em momentos diferentes."))

[void]$body.Append((Paragraph "3. Middleware de filas: RabbitMQ e MassTransit" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "RabbitMQ é usado como message broker para o processamento assíncrono controlado. O MassTransit fornece a abstração .NET para publicação e consumo, configuração de endpoints e políticas de resiliência. O Middleware Worker possui consumidores para eventos da plataforma, pedidos de atualização de dashboard e alertas."))
$queueRows = @(
    @("betstrike.analytics.stream", "Processamento dos eventos da plataforma.", "Prefetch 16; retries após 2 s, 5 s e 15 s."),
    @("betstrike.analytics.high", "Atualizações urgentes do dashboard.", "Prefetch 4; retries entre 3 s e 1 min."),
    @("betstrike.analytics.low", "Atualizações menos urgentes do dashboard.", "Prefetch 2; retries entre 5 s e 2 min."),
    @("*.dlq", "Conservar mensagens que continuam a falhar.", "Dead-letter routing configurado em cada fila principal.")
)
[void]$body.Append((Table @("Fila", "Finalidade", "Política") $queueRows @(2750, 3200, 3350)))
[void]$body.Append((Paragraph "A prioridade é concretizada através de filas separadas e valores de prefetch diferentes. Eventos como registo de aposta, alteração de estado e resultado originam refresh de alta prioridade; eventos menos críticos seguem para a fila de baixa prioridade. O in-memory outbox reduz o risco de publicação duplicada durante o processamento de uma mensagem."))

[void]$body.Append((Paragraph "4. Middleware de streaming: Apache Kafka" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "Kafka é utilizado para difundir eventos de domínio em tempo quase real. As duas APIs funcionam como producers e o Streaming Worker subscreve os topics através do consumer group betstrike-streaming-analytics. O consumidor desativa o auto-commit e só guarda e confirma o offset após o processamento bem-sucedido do evento."))
$topicRows = @(
    @("bets-events", "BetRegisteredEvent e BetStatusChangedEvent."),
    @("game-events", "Publicação, atualização e remoção de jogos, bem como resultados."),
    @("analytics-events", "Eventos analíticos ou genéricos futuros.")
)
[void]$body.Append((Table @("Topic", "Conteúdo") $topicRows @(2700, 6600)))
[void]$body.Append((Paragraph "O worker cria os topics quando necessário, utiliza AutoOffsetReset=Earliest e confirma offsets manualmente. Caso o worker pare, os producers continuam a publicar e, após o reinício, o consumer group retoma a partir do último offset confirmado."))

[void]$body.Append((Paragraph "5. Justificação das tecnologias escolhidas" "Heading1" -KeepNext))
$techRows = @(
    @("RabbitMQ", "Adequado a filas de trabalho, encaminhamento e entrega controlada. Suporta retries, filas com prioridades distintas e DLQ. É simples de operar e possui interface de gestão."),
    @("MassTransit", "Reduz código repetitivo de integração com RabbitMQ e centraliza consumidores, topologia, retries e outbox no ecossistema .NET."),
    @("Apache Kafka", "Adequado a streaming com elevada taxa de eventos, retenção persistente, consumer groups independentes e replay por offsets."),
    @("SQL Server", "Mantém consistência com a base tecnológica existente e permite reutilizar stored procedures para dados operacionais, analytics, idempotência e checkpoints."),
    @("Workers dedicados", "Retiram analytics e alertas do caminho crítico das APIs e permitem escalar ou reiniciar consumidores independentemente.")
)
[void]$body.Append((Table @("Tecnologia", "Fundamentação") $techRows @(2500, 6800)))
[void]$body.Append((Paragraph "A escolha conjunta permite explorar os pontos fortes de cada broker. RabbitMQ oferece semântica clara de tarefa e políticas de entrega; Kafka oferece retenção e leitura repetida de um fluxo de acontecimentos. Esta combinação suporta tanto processamento operacional fiável como integração analítica extensível."))

[void]$body.Append((Paragraph "6. Fluxos principais e assíncronos" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "6.1. Registo de uma aposta" "Heading2" -KeepNext))
foreach ($item in @(
    "O cliente envia o pedido de registo para a Betting API.",
    "A API valida e persiste a aposta no SQL Server.",
    "A API publica BetRegisteredEvent em RabbitMQ e no topic Kafka bets-events.",
    "O Middleware Worker arquiva o evento, avalia alertas e publica um refresh de alta prioridade.",
    "O Streaming Worker processa o mesmo acontecimento de forma independente e confirma o offset após sucesso.",
    "As stored procedures recalculam as métricas e o frontend consulta o snapshot atualizado."
)) { [void]$body.Append((Paragraph $item -Bullet)) }

[void]$body.Append((Paragraph "6.2. Atualização de jogo ou resultado" "Heading2" -KeepNext))
foreach ($item in @(
    "A Betting API ou a Results API altera o jogo/resultado e publica um evento de domínio.",
    "RabbitMQ distribui o evento aos consumidores responsáveis por arquivo, alertas e refresh.",
    "Kafka adiciona o evento ao stream game-events para consumo independente.",
    "Resultados anómalos, com seis ou mais golos ou diferença igual/superior a quatro, originam alertas.",
    "O frontend obtém o novo estado através de GET /api/analytics/dashboard."
)) { [void]$body.Append((Paragraph $item -Bullet)) }

[void]$body.Append((Paragraph "6.3. Fluxo de falha e reprocessamento" "Heading2" -KeepNext))
foreach ($item in @(
    "Em RabbitMQ, uma exceção ativa retries com intervalos progressivos; após esgotar as tentativas, a mensagem é encaminhada para a DLQ.",
    "Os eventos processados são arquivados em Evento_Middleware, cuja chave EventId impede duplicados.",
    "O HistoricalReplayService lê eventos posteriores ao checkpoint Stream_ReplayCheckpoint e reconstrói alertas/dashboard.",
    "Os alertas usam SourceEventId com índice único, evitando duplicação durante replay.",
    "Em Kafka, a confirmação manual do offset ocorre apenas após arquivo, alertas e recálculo do dashboard; após falha, o consumo retoma do último offset confirmado."
)) { [void]$body.Append((Paragraph $item -Bullet)) }

[void]$body.Append((Paragraph "7. Persistência, analytics e idempotência" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "A base Apostas contém estruturas específicas para suportar o middleware. Evento_Middleware funciona como arquivo persistido dos eventos; Stream_ReplayCheckpoint guarda a última sequência reprocessada; Alerta_Middleware guarda alertas; e as tabelas Dashboard_* mantêm snapshots analíticos."))
$dataRows = @(
    @("Evento_Middleware", "Arquivo de eventos com EventId único e EventSequence incremental."),
    @("Stream_ReplayCheckpoint", "Checkpoint por processo de replay."),
    @("Alerta_Middleware", "Alertas com índice único por SourceEventId."),
    @("Dashboard_Resumo", "Indicadores globais: volume, margem, utilizadores ativos e apostas/minuto."),
    @("Dashboard_Competicao", "Volume e resultados agregados por competição."),
    @("Dashboard_JanelaTemporal", "Métricas em janelas de 15 minutos."),
    @("Dashboard_ExposicaoJogo", "Exposição financeira por jogo."),
    @("Dashboard_TipoAposta", "Volume e taxa de vitória por tipo de aposta.")
)
[void]$body.Append((Table @("Estrutura", "Responsabilidade") $dataRows @(3000, 6300)))

[void]$body.Append((Paragraph "8. Evidência de testes: carga, falhas e reprocessamento" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "O repositório inclui o script scripts/teste_carga_parte2.ps1 e o guião docs/TESTES_PARTE2.md. O script cria utilizadores, cria um jogo, regista um número configurável de apostas, aguarda o processamento assíncrono e consulta o dashboard e o estado do stream. Alguns valores são selecionados para aumentar o volume agregado e demonstrar a geração automática de alertas."))
[void]$body.Append((Paragraph "8.1. Teste de carga" "Heading2" -KeepNext))
[void]$body.Append((Code-Paragraph "powershell -ExecutionPolicy Bypass -File .\scripts\teste_carga_parte2.ps1 -Apostas 100"))
[void]$body.Append((Paragraph "Evidências a observar: aumento de VolumeTotalApostado e ApostasPorMinuto; crescimento da sequência de Evento_Middleware; mensagens nos topics Kafka; filas RabbitMQ criadas; e alertas quando a exposição acumulada ultrapassa o limiar definido."))

[void]$body.Append((Paragraph "8.2. Teste de falha temporária do consumidor Kafka" "Heading2" -KeepNext))
[void]$body.Append((Code-Paragraph "docker compose stop streaming-worker`npowershell -ExecutionPolicy Bypass -File .\scripts\teste_carga_parte2.ps1 -Apostas 50`ndocker compose start streaming-worker"))
[void]$body.Append((Paragraph "Durante a paragem, as APIs continuam operacionais e Kafka conserva os eventos. Após o arranque, o Streaming Worker retoma a partir do último offset confirmado e processa o backlog. A confirmação manual do offset após sucesso sustenta esta recuperação."))

[void]$body.Append((Paragraph "8.3. Teste de replay controlado" "Heading2" -KeepNext))
[void]$body.Append((Code-Paragraph "USE Apostas;`nUPDATE dbo.Stream_ReplayCheckpoint`nSET LastEventSequence = 0, UpdatedAtUtc = SYSUTCDATETIME()`nWHERE ReplayName = 'middleware.worker';`n`ndocker compose restart middleware-worker"))
[void]$body.Append((Paragraph "No reinício, o HistoricalReplayService relê os eventos arquivados, atualiza o checkpoint e recalcula o dashboard. EventId e SourceEventId asseguram idempotência, evitando eventos e alertas duplicados."))

[void]$body.Append((Paragraph "8.4. Validação realizada em 6 de junho de 2026" "Heading2" -KeepNext))
$evidenceRows = @(
    @("Compilação da solução", "dotnet build BetStrike.Parte1.sln --no-restore", "Sucesso: 0 erros e 3 avisos de API obsoleta relativos ao UseInMemoryOutbox."),
    @("Validação do Compose", "docker compose config --services", "Sucesso: foram reconhecidos 10 serviços."),
    @("Execução dos containers", "docker compose ps -a", "Não executada: o Docker Engine não estava ativo no ambiente durante a elaboração do relatório."),
    @("Carga/falha/replay", "Guião e scripts existentes", "Procedimentos prontos; resultados operacionais devem ser capturados com Docker Desktop ativo.")
)
[void]$body.Append((Table @("Validação", "Comando/Origem", "Resultado") $evidenceRows @(2100, 3100, 4100)))
[void]$body.Append((Paragraph "Para a entrega final, recomenda-se anexar capturas do resumo produzido pelo script, da Kafka UI, do RabbitMQ Management e do endpoint /api/analytics/stream após executar os três cenários com o Docker Engine ativo." -Italic))

[void]$body.Append((Paragraph "9. Execução da solução em ambiente containerizado" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "Toda a solução está definida em docker-compose.yml. O arranque constrói as aplicações .NET e o frontend, inicia os brokers e o SQL Server, executa os scripts de inicialização da base de dados e liga os serviços através da rede interna do Compose."))
[void]$body.Append((Code-Paragraph "docker compose up --build"))
$serviceRows = @(
    @("sqlserver", "SQL Server 2022 e volume persistente", "localhost:1433"),
    @("db-init", "Bootstrap dos scripts SQL", "Execução única"),
    @("rabbitmq", "Broker e interface de gestão", "5672 / http://localhost:15672"),
    @("kafka", "Broker Kafka em modo KRaft", "localhost:9092"),
    @("kafka-ui", "Inspeção de topics, mensagens e consumer groups", "http://localhost:8081"),
    @("betting-api", "API BetStrike", "http://localhost:5002"),
    @("results-api", "API da Federação", "http://localhost:5001"),
    @("middleware-worker", "Consumidor RabbitMQ e replay", "Serviço interno"),
    @("streaming-worker", "Consumidor Kafka", "Serviço interno"),
    @("frontend", "Interface web", "http://localhost:8080")
)
[void]$body.Append((Table @("Serviço", "Responsabilidade", "Acesso") $serviceRows @(2450, 4300, 2550)))
[void]$body.Append((Paragraph "A persistência do SQL Server é garantida pelo volume sqlserver-data. As connection strings e endereços dos brokers são injetados por variáveis de ambiente, usando os nomes dos serviços como DNS interno."))

[void]$body.Append((Paragraph "10. Avaliação crítica" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "A solução cumpre o objetivo de separar as operações transacionais do processamento analítico e demonstra mecanismos relevantes de resiliência. A utilização de contratos partilhados torna explícitos os eventos e simplifica a integração entre produtores e consumidores. A idempotência no SQL reduz o impacto de entregas repetidas e o replay permite reconstruir estado derivado."))
[void]$body.Append((Paragraph "Como melhorias futuras, seria adequado substituir o in-memory outbox por um outbox transacional persistente, aumentar o número de partições e réplicas Kafka em ambientes reais, adicionar autenticação aos brokers, introduzir métricas centralizadas e automatizar os cenários de falha numa pipeline de integração contínua. Os três avisos atuais de compilação também indicam que a chamada UseInMemoryOutbox deve ser atualizada para a sobrecarga recomendada pelo MassTransit."))

[void]$body.Append((Paragraph "11. Conclusão" "Heading1" -KeepNext))
[void]$body.Append((Paragraph "A Parte 2 da BetStrike implementa uma arquitetura orientada a eventos com dois tipos de middleware claramente diferenciados. RabbitMQ/MassTransit assegura filas de trabalho resilientes, prioridades e DLQ; Kafka assegura streaming persistente, múltiplos consumidores e retoma por offsets. Os workers processam analytics e alertas fora do caminho crítico, enquanto SQL Server fornece persistência, idempotência e replay. A definição Docker Compose reúne os componentes num ambiente reproduzível e os guiões de teste permitem demonstrar carga, tolerância a falhas e reprocessamento."))

[void]$body.Append((Paragraph "Referências do projeto" "Heading1" -KeepNext))
foreach ($item in @(
    "README.md",
    "docs/ARQUITETURA_PARTE2.md",
    "docs/TESTES_PARTE2.md",
    "docker-compose.yml",
    "scripts/teste_carga_parte2.ps1",
    "src/BetStrike.Middleware.Worker",
    "src/BetStrike.Streaming.Worker",
    "src/BetStrike.Middleware.Contracts",
    "database/Apostas/Tables/02_middleware_tables.sql",
    "database/Apostas/StoredProcedures/03_middleware_stored_procedures.sql"
)) { [void]$body.Append((Paragraph $item -Bullet)) }

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    $body
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134" w:header="708" w:footer="708"/>
      <w:footerReference w:type="default" r:id="rId1" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"/>
    </w:sectPr>
  </w:body>
</w:document>
"@

$stylesXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Aptos" w:hAnsi="Aptos"/><w:sz w:val="22"/></w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:spacing w:after="120" w:line="276" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults>
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:pPr><w:spacing w:after="120" w:line="276" w:lineRule="auto"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:pPr><w:keepNext/><w:spacing w:before="360" w:after="160"/><w:outlineLvl w:val="0"/></w:pPr><w:rPr><w:b/><w:color w:val="1F4E78"/><w:sz w:val="30"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:pPr><w:keepNext/><w:spacing w:before="240" w:after="120"/><w:outlineLvl w:val="1"/></w:pPr><w:rPr><w:b/><w:color w:val="2F75B5"/><w:sz w:val="25"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Code"><w:name w:val="Code"/><w:basedOn w:val="Normal"/><w:pPr><w:shd w:fill="F2F2F2"/><w:spacing w:before="80" w:after="160"/><w:ind w:left="180" w:right="180"/></w:pPr><w:rPr><w:rFonts w:ascii="Consolas" w:hAnsi="Consolas"/><w:sz w:val="18"/></w:rPr></w:style>
  <w:style w:type="table" w:styleId="ReportTable"><w:name w:val="Report Table"/><w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4" w:color="BFBFBF"/><w:left w:val="single" w:sz="4" w:color="BFBFBF"/><w:bottom w:val="single" w:sz="4" w:color="BFBFBF"/><w:right w:val="single" w:sz="4" w:color="BFBFBF"/><w:insideH w:val="single" w:sz="4" w:color="D9EAF7"/><w:insideV w:val="single" w:sz="4" w:color="D9EAF7"/></w:tblBorders><w:tblCellMar><w:top w:w="90" w:type="dxa"/><w:left w:w="90" w:type="dxa"/><w:bottom w:w="90" w:type="dxa"/><w:right w:w="90" w:type="dxa"/></w:tblCellMar></w:tblPr></w:style>
</w:styles>
"@

$numberingXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:numbering xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:abstractNum w:abstractNumId="0"><w:multiLevelType w:val="singleLevel"/><w:lvl w:ilvl="0"><w:numFmt w:val="bullet"/><w:lvlText w:val="•"/><w:lvlJc w:val="left"/><w:pPr><w:tabs><w:tab w:val="num" w:pos="720"/></w:tabs><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl></w:abstractNum>
  <w:num w:numId="1"><w:abstractNumId w:val="0"/></w:num>
</w:numbering>
"@

$footerXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:ftr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:rPr><w:color w:val="777777"/><w:sz w:val="18"/></w:rPr><w:t>BetStrike - Relatório Técnico Parte 2 | </w:t></w:r><w:fldSimple w:instr="PAGE"><w:r><w:rPr><w:color w:val="777777"/><w:sz w:val="18"/></w:rPr><w:t>1</w:t></w:r></w:fldSimple></w:p>
</w:ftr>
"@

$contentTypes = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/numbering.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"/>
  <Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>
"@

$rootRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>
"@

$documentRels = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering" Target="numbering.xml"/>
</Relationships>
"@

$coreXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>Relatório Técnico Parte 2 - BetStrike</dc:title>
  <dc:subject>Middleware assíncrono, streaming e containers</dc:subject>
  <dc:creator>Filipe</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">2026-06-06T00:00:00Z</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">2026-06-06T00:00:00Z</dcterms:modified>
</cp:coreProperties>
"@

$appXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Microsoft Office Word</Application>
</Properties>
"@

$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $outputFullPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
if (Test-Path $outputFullPath) { Remove-Item -LiteralPath $outputFullPath -Force }

$fileStream = [System.IO.File]::Open($outputFullPath, [System.IO.FileMode]::CreateNew)
$archive = New-Object System.IO.Compression.ZipArchive($fileStream, [System.IO.Compression.ZipArchiveMode]::Create)

try {
    $entries = @{
        "[Content_Types].xml" = $contentTypes
        "_rels/.rels" = $rootRels
        "word/document.xml" = $documentXml
        "word/styles.xml" = $stylesXml
        "word/numbering.xml" = $numberingXml
        "word/footer1.xml" = $footerXml
        "word/_rels/document.xml.rels" = $documentRels
        "docProps/core.xml" = $coreXml
        "docProps/app.xml" = $appXml
    }

    foreach ($entryName in $entries.Keys) {
        $entry = $archive.CreateEntry($entryName)
        $writer = New-Object System.IO.StreamWriter($entry.Open(), (New-Object System.Text.UTF8Encoding($false)))
        try { $writer.Write($entries[$entryName]) } finally { $writer.Dispose() }
    }
}
finally {
    $archive.Dispose()
    $fileStream.Dispose()
}

Write-Host "Relatório Word gerado em: $outputFullPath"
