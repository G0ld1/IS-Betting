# BetStrike — Parte 1 (Integração REST + SQL Server)


## Estrutura

- `src/Federation.Results.Api` — API REST da Federação (jogos e resultados)
- `src/BetStrike.Betting.Api` — API REST de gestão de apostas
- `src/Federation.DataGenerator` — aplicação de geração de calendário e simulação em tempo fictício
- `frontend/` — site front end para operações e monitorização
- `database/Apostas` — esquema e stored procedures da BD `Apostas`
- `database/Pagamentos` — esquema e stored procedures da BD `Pagamentos`
- `database/Integration/Triggers` — trigger de integração entre apostas e pagamentos

## Princípios aplicados

- Toda a camada de dados usa **Stored Procedures** .
- Validações críticas em **BD** e reforçadas na **API**.
- Fluxo assíncrono para simulação de 9 jogos em paralelo .
- Contratos REST explícitos para sincronização entre plataformas.



## Guia de execução detalhado

Ver [docs/EXECUCAO_PARTE1.md](docs/EXECUCAO_PARTE1.md) para:
- ordem exata de execução dos scripts SQL;
- arranque das APIs com portas fixas;
- teste fim-a-fim com requests HTTP.

### Criação Rápida de Bases de Dados e Arranque de APIs

Para deixar a aplicação pronta para execução:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\preparar_entrega_professor.ps1
```

Depois, abrir o frontend em `http://localhost:8080`.

## Front end

O site está em [frontend/index.html](frontend/index.html) e comunica com as APIs em tempo real.

### Configuração necessária:

**As seguintes APIs devem estar a rodar:**
- `http://localhost:5001` — API da Federação (jogos e resultados)
- `http://localhost:5002` — API de Apostas (gestão de apostas)

**O frontend é servido na porta `8080`:**

```powershell
Set-Location frontend
python -m http.server 8080
```

Depois abre `http://localhost:8080` no browser.

O frontend acede aos dados dos jogos e das apostas através das APIs em 5001 e 5002.
