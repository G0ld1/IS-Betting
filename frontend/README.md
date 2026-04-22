# BetStrike Control Room

Site front end estático para acompanhar a plataforma BetStrike.

## O que mostra

- Jogos da API de Apostas (`http://localhost:5002`)
- Jogos da Federação (`http://localhost:5001`)
- Apostas recentes
- Criação de utilizadores, jogos, apostas e resultados
- Estatísticas por jogo e por competição

## Como abrir

Serve os ficheiros `frontend/` com qualquer servidor estático e abre o `index.html`.

Exemplo com PowerShell:

```powershell
Set-Location frontend
python -m http.server 8080
```

Depois abre `http://localhost:8080`.
