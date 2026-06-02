const DEFAULT_BETTING_BASE = "http://localhost:5002";
const DEFAULT_RESULTS_BASE = "http://localhost:5001";
const STORAGE_KEY = "betstrike.frontend.settings";

const gameStatusLabels = {
  1: "Agendado",
  2: "Em curso",
  3: "Finalizado",
  4: "Cancelado",
  5: "Adiado",
};

const betStatusLabels = {
  1: "Pendente",
  2: "Ganha",
  3: "Perdida",
  4: "Anulada",
};

const betTypeLabels = {
  1: "Casa",
  X: "Empate",
  2: "Fora",
};

const state = {
  bettingGames: [],
  resultsGames: [],
  bets: [],
  users: [],
  statsGame: null,
  statsCompetition: null,
  dashboard: null,
  streamStatus: null,
  lastCreatedUserId: null,
  settings: loadSettings(),
  filter: "all",
};

const elements = {};

function loadSettings() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return {
        bettingBase: DEFAULT_BETTING_BASE,
        resultsBase: DEFAULT_RESULTS_BASE,
      };
    }

    const parsed = JSON.parse(raw);
    return {
      bettingBase: parsed.bettingBase || DEFAULT_BETTING_BASE,
      resultsBase: parsed.resultsBase || DEFAULT_RESULTS_BASE,
    };
  } catch {
    return {
      bettingBase: DEFAULT_BETTING_BASE,
      resultsBase: DEFAULT_RESULTS_BASE,
    };
  }
}

function saveSettings() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state.settings));
}

function normalizeBaseUrl(value, fallback) {
  const trimmed = (value || "").trim();
  return trimmed || fallback;
}

function formatDate(value) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat("pt-PT", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(date);
}

function formatDateOnly(value) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat("pt-PT", { dateStyle: "short" }).format(date);
}

function formatCurrency(value) {
  return new Intl.NumberFormat("pt-PT", {
    style: "currency",
    currency: "EUR",
    maximumFractionDigits: 2,
  }).format(Number(value || 0));
}

function formatNumber(value) {
  return new Intl.NumberFormat("pt-PT", { maximumFractionDigits: 2 }).format(Number(value || 0));
}

function normalizeDateInput(value) {
  const text = String(value || "").trim();
  if (!text) return "";

  const isoMatch = text.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (isoMatch) {
    return text;
  }

  const parsed = new Date(text);
  if (Number.isNaN(parsed.getTime())) {
    return text;
  }

  const year = parsed.getFullYear();
  const month = String(parsed.getMonth() + 1).padStart(2, "0");
  const day = String(parsed.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function normalizeTimeInput(value) {
  const text = String(value || "").trim();
  if (!text) return "";

  const trimmed = text.replace(/\s+/g, "");
  const isoMatch = trimmed.match(/^(\d{2}):(\d{2})(?::(\d{2}))?$/);
  if (isoMatch) {
    const hours = isoMatch[1];
    const minutes = isoMatch[2];
    const seconds = isoMatch[3] || "00";
    return `${hours}:${minutes}:${seconds}`;
  }

  const amPmMatch = trimmed.match(/^(\d{1,2}):(\d{2})(AM|PM)$/i);
  if (amPmMatch) {
    let hours = Number(amPmMatch[1]);
    const minutes = amPmMatch[2];
    const suffix = amPmMatch[3].toUpperCase();

    if (suffix === "PM" && hours < 12) hours += 12;
    if (suffix === "AM" && hours === 12) hours = 0;

    return `${String(hours).padStart(2, "0")}:${minutes}:00`;
  }

  return text;
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function pickValue(source, ...keys) {
  for (const key of keys) {
    const value = source?.[key];
    if (value !== undefined && value !== null) {
      return value;
    }
  }

  return undefined;
}

function normalizeBettingGame(game) {
  return {
    Id: pickValue(game, "Id", "id"),
    CodigoJogo: pickValue(game, "CodigoJogo", "codigoJogo"),
    Competicao: pickValue(game, "Competicao", "competicao"),
    EquipaCasa: pickValue(game, "EquipaCasa", "equipaCasa"),
    EquipaFora: pickValue(game, "EquipaFora", "equipaFora"),
    DataJogo: pickValue(game, "DataJogo", "dataJogo"),
    HoraInicio: pickValue(game, "HoraInicio", "horaInicio"),
    Estado: pickValue(game, "Estado", "estado"),
  };
}

function normalizeFederationGame(game) {
  return {
    Id: pickValue(game, "Id", "id"),
    CodigoJogo: pickValue(game, "CodigoJogo", "codigoJogo"),
    EquipaCasa: pickValue(game, "EquipaCasa", "equipaCasa"),
    EquipaFora: pickValue(game, "EquipaFora", "equipaFora"),
    DataJogo: pickValue(game, "DataJogo", "dataJogo"),
    HoraInicio: pickValue(game, "HoraInicio", "horaInicio"),
    GolosCasa: pickValue(game, "GolosCasa", "golosCasa"),
    GolosFora: pickValue(game, "GolosFora", "golosFora"),
    Estado: pickValue(game, "Estado", "estado"),
  };
}

function normalizeBet(bet) {
  return {
    Id: pickValue(bet, "Id", "id"),
    JogoId: pickValue(bet, "JogoId", "jogoId"),
    UtilizadorId: pickValue(bet, "UtilizadorId", "utilizadorId"),
    TipoAposta: pickValue(bet, "TipoAposta", "tipoAposta"),
    ValorApostado: pickValue(bet, "ValorApostado", "valorApostado"),
    OddMomento: pickValue(bet, "OddMomento", "oddMomento"),
    Estado: pickValue(bet, "Estado", "estado"),
    DataHoraUtc: pickValue(bet, "DataHoraUtc", "dataHoraUtc"),
  };
}

function normalizeUser(user) {
  return {
    Id: pickValue(user, "Id", "id"),
    Nome: pickValue(user, "Nome", "nome"),
    Email: pickValue(user, "Email", "email"),
    SaldoDisponivel: pickValue(user, "SaldoDisponivel", "saldoDisponivel"),
    SaldoGastoTotal: pickValue(user, "SaldoGastoTotal", "saldoGastoTotal"),
    CriadoEmUtc: pickValue(user, "CriadoEmUtc", "criadoEmUtc"),
  };
}

function normalizeGameStats(stats) {
  return {
    CodigoJogo: pickValue(stats, "CodigoJogo", "codigoJogo"),
    TotalApostado: pickValue(stats, "TotalApostado", "totalApostado"),
    ApostasCasa: pickValue(stats, "ApostasCasa", "apostasCasa"),
    ApostasEmpate: pickValue(stats, "ApostasEmpate", "apostasEmpate"),
    ApostasFora: pickValue(stats, "ApostasFora", "apostasFora"),
    Pendentes: pickValue(stats, "Pendentes", "pendentes"),
    MargemPlataforma: pickValue(stats, "MargemPlataforma", "margemPlataforma"),
  };
}

function normalizeDashboardSnapshot(snapshot) {
  const resumo = pickValue(snapshot, "Resumo", "resumo");
  const competicoes = pickValue(snapshot, "Competicoes", "competicoes");
  const janelasTemporais = pickValue(snapshot, "JanelasTemporais", "janelasTemporais");
  const exposicoesJogos = pickValue(snapshot, "ExposicoesJogos", "exposicoesJogos");
  const tiposAposta = pickValue(snapshot, "TiposAposta", "tiposAposta");
  const alertas = pickValue(snapshot, "Alertas", "alertas");
  const eventosRecentes = pickValue(snapshot, "EventosRecentes", "eventosRecentes");

  return {
    Resumo: resumo
      ? {
          AtualizadoUtc: pickValue(resumo, "AtualizadoUtc", "atualizadoUtc"),
          JogosAtivos: pickValue(resumo, "JogosAtivos", "jogosAtivos"),
          ApostasPendentes: pickValue(resumo, "ApostasPendentes", "apostasPendentes"),
          ApostasGanhas: pickValue(resumo, "ApostasGanhas", "apostasGanhas"),
          VolumeTotalApostado: pickValue(resumo, "VolumeTotalApostado", "volumeTotalApostado"),
          MargemPlataforma: pickValue(resumo, "MargemPlataforma", "margemPlataforma"),
          UtilizadoresAtivos: pickValue(resumo, "UtilizadoresAtivos", "utilizadoresAtivos"),
          ApostasPorMinuto: pickValue(resumo, "ApostasPorMinuto", "apostasPorMinuto"),
          MediaMovel15MinVolume: pickValue(resumo, "MediaMovel15MinVolume", "mediaMovel15MinVolume"),
          MaxVolumeHora: pickValue(resumo, "MaxVolumeHora", "maxVolumeHora"),
          MinVolumeHora: pickValue(resumo, "MinVolumeHora", "minVolumeHora"),
        }
      : null,
    Competicoes: Array.isArray(competicoes)
      ? competicoes.map((item) => ({
          Competicao: pickValue(item, "Competicao", "competicao"),
          MediaGolosPorJogo: pickValue(item, "MediaGolosPorJogo", "mediaGolosPorJogo"),
          VolumeTotalApostado: pickValue(item, "VolumeTotalApostado", "volumeTotalApostado"),
          TaxaResultado1: pickValue(item, "TaxaResultado1", "taxaResultado1"),
          TaxaResultadoX: pickValue(item, "TaxaResultadoX", "taxaResultadoX"),
          TaxaResultado2: pickValue(item, "TaxaResultado2", "taxaResultado2"),
          AtualizadoUtc: pickValue(item, "AtualizadoUtc", "atualizadoUtc"),
        }))
      : [],
    JanelasTemporais: Array.isArray(janelasTemporais)
      ? janelasTemporais.map((item) => ({
          JanelaInicioUtc: pickValue(item, "JanelaInicioUtc", "janelaInicioUtc"),
          Apostas: pickValue(item, "Apostas", "apostas"),
          VolumeTotal: pickValue(item, "VolumeTotal", "volumeTotal"),
          MediaAposta: pickValue(item, "MediaAposta", "mediaAposta"),
          MaxAposta: pickValue(item, "MaxAposta", "maxAposta"),
          MinAposta: pickValue(item, "MinAposta", "minAposta"),
        }))
      : [],
    ExposicoesJogos: Array.isArray(exposicoesJogos)
      ? exposicoesJogos.map((item) => ({
          JogoId: pickValue(item, "JogoId", "jogoId"),
          CodigoJogo: pickValue(item, "CodigoJogo", "codigoJogo"),
          Competicao: pickValue(item, "Competicao", "competicao"),
          ApostasPendentes: pickValue(item, "ApostasPendentes", "apostasPendentes"),
          VolumeTotal: pickValue(item, "VolumeTotal", "volumeTotal"),
          ExposicaoLiquida: pickValue(item, "ExposicaoLiquida", "exposicaoLiquida"),
          AtualizadoUtc: pickValue(item, "AtualizadoUtc", "atualizadoUtc"),
        }))
      : [],
    TiposAposta: Array.isArray(tiposAposta)
      ? tiposAposta.map((item) => ({
          TipoAposta: pickValue(item, "TipoAposta", "tipoAposta"),
          TotalApostas: pickValue(item, "TotalApostas", "totalApostas"),
          ApostasGanhas: pickValue(item, "ApostasGanhas", "apostasGanhas"),
          ApostasPerdidas: pickValue(item, "ApostasPerdidas", "apostasPerdidas"),
          TaxaVitoria: pickValue(item, "TaxaVitoria", "taxaVitoria"),
          VolumeTotal: pickValue(item, "VolumeTotal", "volumeTotal"),
          AtualizadoUtc: pickValue(item, "AtualizadoUtc", "atualizadoUtc"),
        }))
      : [],
    Alertas: Array.isArray(alertas)
      ? alertas.map((item) => ({
          Id: pickValue(item, "Id", "id"),
          Severidade: pickValue(item, "Severidade", "severidade"),
          Mensagem: pickValue(item, "Mensagem", "mensagem"),
          Competicao: pickValue(item, "Competicao", "competicao"),
          SourceEventId: pickValue(item, "SourceEventId", "sourceEventId"),
          CriadoEmUtc: pickValue(item, "CriadoEmUtc", "criadoEmUtc"),
        }))
      : [],
    EventosRecentes: Array.isArray(eventosRecentes)
      ? eventosRecentes.map((item) => ({
          EventSequence: pickValue(item, "EventSequence", "eventSequence"),
          EventId: pickValue(item, "EventId", "eventId"),
          EventType: pickValue(item, "EventType", "eventType"),
          SourceSystem: pickValue(item, "SourceSystem", "sourceSystem"),
          AggregateType: pickValue(item, "AggregateType", "aggregateType"),
          AggregateKey: pickValue(item, "AggregateKey", "aggregateKey"),
          PayloadJson: pickValue(item, "PayloadJson", "payloadJson"),
          CriadoEmUtc: pickValue(item, "CriadoEmUtc", "criadoEmUtc"),
        }))
      : [],
  };
}

function normalizeStreamStatus(snapshot) {
  if (!snapshot) {
    return null;
  }

  return {
    Broker: pickValue(snapshot, "Broker", "broker"),
    Exchange: pickValue(snapshot, "Exchange", "exchange"),
    StreamQueue: pickValue(snapshot, "StreamQueue", "streamQueue"),
    LastEventSequence: pickValue(snapshot, "LastEventSequence", "lastEventSequence"),
    ReplayCheckpoint: pickValue(snapshot, "ReplayCheckpoint", "replayCheckpoint"),
    PendingEvents: pickValue(snapshot, "PendingEvents", "pendingEvents"),
    AtualizadoUtc: pickValue(snapshot, "AtualizadoUtc", "atualizadoUtc"),
  };
}

function promptText(message, defaultValue = "") {
  const value = window.prompt(message, defaultValue);
  if (value === null) {
    return null;
  }

  return value.trim();
}

function promptInteger(message, defaultValue, allowEmpty = false) {
  const value = promptText(message, String(defaultValue ?? ""));
  if (value === null) {
    return null;
  }

  if (value === "") {
    return allowEmpty ? undefined : null;
  }

  const parsed = Number(value);
  return Number.isInteger(parsed) ? parsed : null;
}

function promptDecimal(message, defaultValue, allowEmpty = false) {
  const value = promptText(message, String(defaultValue ?? ""));
  if (value === null) {
    return null;
  }

  if (value === "") {
    return allowEmpty ? undefined : null;
  }

  const parsed = Number(value.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : null;
}

async function refreshAfterMutation(title, detail) {
  appendLog("success", title, detail);
  await loadData();
}

async function deleteBettingGame(codigoJogo) {
  if (!window.confirm(`Remover o jogo ${codigoJogo}?`)) return;

  await requestJson(state.settings.bettingBase, `/api/apostas/jogos/${encodeURIComponent(codigoJogo)}`, {
    method: "DELETE",
  });

  await refreshAfterMutation("Jogo de apostas removido", { codigoJogo });
}

async function editBettingGame(codigoJogo) {
  const current = state.bettingGames.find((game) => game.CodigoJogo === codigoJogo);
  const estado = promptInteger("Estado do jogo (1=Agendado, 2=Em curso, 3=Finalizado, 4=Cancelado, 5=Adiado)", current?.Estado ?? 1);
  if (estado === null) return;

  const golosCasa = promptInteger("Golos casa (deixa vazio para null)", current?.GolosCasa ?? "", true);
  if (golosCasa === null) return;

  const golosFora = promptInteger("Golos fora (deixa vazio para null)", current?.GolosFora ?? "", true);
  if (golosFora === null) return;

  const payload = {
    codigoJogo,
    estado,
    golosCasa,
    golosFora,
  };

  await requestJson(state.settings.bettingBase, `/api/apostas/jogos/${encodeURIComponent(codigoJogo)}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });

  await refreshAfterMutation("Jogo de apostas atualizado", payload);
}

async function deleteFederationGame(codigoJogo) {
  if (!window.confirm(`Remover o jogo ${codigoJogo} da Federação?`)) return;

  await requestJson(state.settings.resultsBase, `/api/jogos/${encodeURIComponent(codigoJogo)}`, {
    method: "DELETE",
  });

  await refreshAfterMutation("Jogo da Federação removido", { codigoJogo });
}

async function editFederationGame(codigoJogo) {
  const current = state.resultsGames.find((game) => game.CodigoJogo === codigoJogo);
  const estado = promptInteger("Estado do jogo (1=Agendado, 2=Em curso, 3=Finalizado, 4=Cancelado, 5=Adiado)", current?.Estado ?? 1);
  if (estado === null) return;

  const golosCasa = promptInteger("Golos casa", current?.GolosCasa ?? 0);
  if (golosCasa === null) return;

  const golosFora = promptInteger("Golos fora", current?.GolosFora ?? 0);
  if (golosFora === null) return;

  const payload = {
    codigoJogo,
    estado,
    golosCasa,
    golosFora,
  };

  await requestJson(state.settings.resultsBase, `/api/jogos/${encodeURIComponent(codigoJogo)}`, {
    method: "PUT",
    body: JSON.stringify(payload),
  });

  await refreshAfterMutation("Jogo da Federação atualizado", payload);
}

async function cancelBet(apostaId, utilizadorId) {
  if (!window.confirm(`Cancelar a aposta #${apostaId}?`)) return;

  await requestJson(state.settings.bettingBase, `/api/apostas/${apostaId}?utilizadorId=${encodeURIComponent(utilizadorId)}`, {
    method: "DELETE",
  });

  await refreshAfterMutation("Aposta cancelada", { apostaId, utilizadorId });
}

function statusClassForGame(status) {
  return `state-${status}`;
}

function statusClassForBet(status) {
  return `state-${status}`;
}

async function requestJson(baseUrl, path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    headers: {
      Accept: "application/json",
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(options.headers || {}),
    },
    ...options,
  });

  const text = await response.text();
  const contentType = response.headers.get("content-type") || "";
  const payload = text && contentType.includes("application/json") ? JSON.parse(text) : text;

  if (!response.ok) {
    const error = new Error(
      typeof payload === "string" && payload
        ? payload
        : `Pedido falhou com o estado HTTP ${response.status}`
    );
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  if (response.status === 204 || !text) {
    return null;
  }

  return payload;
}

async function loadData() {
  setConnectionState("loading");
  setSectionMessage("lastUpdated", "A carregar dados das APIs...");

  const bettingBase = state.settings.bettingBase;
  const resultsBase = state.settings.resultsBase;
  updateBaseLabels();

  const tasks = [
    requestJson(bettingBase, "/api/apostas/jogos").catch((error) => ({ __error: error, source: "bettingGames" })),
    requestJson(resultsBase, "/api/jogos").catch((error) => ({ __error: error, source: "resultsGames" })),
    requestJson(bettingBase, "/api/apostas").catch((error) => ({ __error: error, source: "bets" })),
    requestJson(bettingBase, "/api/utilizadores").catch((error) => ({ __error: error, source: "users" })),
    requestJson(bettingBase, "/api/analytics/dashboard").catch((error) => ({ __error: error, source: "dashboard" })),
    requestJson(bettingBase, "/api/analytics/stream").catch((error) => ({ __error: error, source: "stream" })),
  ];

  const [bettingGames, resultsGames, bets, users, dashboard, stream] = await Promise.all(tasks);

  state.bettingGames = Array.isArray(bettingGames) ? bettingGames.map(normalizeBettingGame) : [];
  state.resultsGames = Array.isArray(resultsGames) ? resultsGames.map(normalizeFederationGame) : [];
  state.bets = Array.isArray(bets) ? bets.map(normalizeBet) : [];
  state.users = Array.isArray(users) ? users.map(normalizeUser) : [];
  state.dashboard = dashboard && !dashboard.__error ? normalizeDashboardSnapshot(dashboard) : null;
  state.streamStatus = stream && !stream.__error ? normalizeStreamStatus(stream) : null;

  const errors = [bettingGames, resultsGames, bets, users, dashboard, stream].filter((item) => item && item.__error);
  if (errors.length > 0) {
    setConnectionState("partial");
    appendLog("warning", "Algumas consultas falharam", errors[0].__error?.message || "Erro inesperado.");
  } else {
    setConnectionState("ok");
  }

  renderAll();
  setSectionMessage("lastUpdated", `Atualizado em ${new Intl.DateTimeFormat("pt-PT", { dateStyle: "short", timeStyle: "medium" }).format(new Date())}`);
}

function updateBaseLabels() {
  elements.bettingBaseLabel.textContent = state.settings.bettingBase;
  elements.resultsBaseLabel.textContent = state.settings.resultsBase;
  elements.swaggerBettingLink.href = `${state.settings.bettingBase}/swagger`;
  elements.swaggerResultsLink.href = `${state.settings.resultsBase}/swagger`;
}

function setConnectionState(mode) {
  const map = {
    ok: { text: "online", className: "status-dot ok" },
    loading: { text: "a sincronizar", className: "status-dot warn" },
    partial: { text: "parcial", className: "status-dot warn" },
    error: { text: "offline", className: "status-dot bad" },
  };

  const current = map[mode] || map.ok;
  elements.bettingStatusDot.className = current.className;
  elements.bettingStatusDot.textContent = current.text;
  elements.resultsStatusDot.className = current.className;
  elements.resultsStatusDot.textContent = current.text;
}

function setSectionMessage(id, message) {
  elements[id].textContent = message;
}

function renderAll() {
  renderSummary();
  renderDashboardPanel();
  renderMiddlewarePanel();
  renderGames();
  renderBets();
  renderUsers();
  renderDynamicFormOptions();
  renderStatsPanel();
}

function renderDynamicFormOptions() {
  const bettingOptions = state.bettingGames
    .slice()
    .sort((a, b) => Number(a.Id) - Number(b.Id))
    .map(
      (game) =>
        `<option value="${Number(game.Id)}">#${Number(game.Id)} - ${escapeHtml(game.CodigoJogo)} (${escapeHtml(game.EquipaCasa)} vs ${escapeHtml(game.EquipaFora)})</option>`
    )
    .join("");

  const placeholder = '<option value="">Seleciona um jogo da API de Apostas</option>';
  elements.registerBetGameId.innerHTML = `${placeholder}${bettingOptions}`;
  elements.insertResultGameId.innerHTML = `${placeholder}${bettingOptions}`;

  elements.lastCreatedUserHint.textContent = state.lastCreatedUserId
    ? `Último utilizador criado: #${state.lastCreatedUserId}`
    : "Cria um utilizador para obter o ID.";
}

function renderSummary() {
  const activeGames = [...state.bettingGames, ...state.resultsGames].filter((game) => Number(game.Estado) === 1 || Number(game.Estado) === 2).length;
  const pendingBets = state.bets.filter((bet) => Number(bet.Estado) === 1).length;
  const wonBets = state.bets.filter((bet) => Number(bet.Estado) === 2).length;
  const totalStake = state.bets.reduce((sum, bet) => sum + Number(bet.ValorApostado || 0), 0);

  elements.summaryBettingGames.textContent = String(state.bettingGames.length);
  elements.summaryResultsGames.textContent = String(state.resultsGames.length);
  elements.summaryBets.textContent = String(state.bets.length);
  elements.summaryStake.textContent = formatCurrency(totalStake);

  elements.metricActiveGames.textContent = String(activeGames);
  elements.metricPendingBets.textContent = String(pendingBets);
  elements.metricWonBets.textContent = String(wonBets);

  const firstCompetition = state.bettingGames.find((game) => game.Competicao)?.Competicao;
  elements.metricCompetition.textContent = firstCompetition || "—";
  elements.metricCompetitionDetail.textContent = firstCompetition
    ? `Última competição carregada: ${firstCompetition}`
    : "Consulta estatística por competição";
}

function filteredBettingGames() {
  return state.bettingGames.filter((game) => {
    if (state.filter === "active") return Number(game.Estado) === 2;
    if (state.filter === "finished") return Number(game.Estado) === 3;
    return true;
  });
}

function filteredResultsGames() {
  return state.resultsGames.filter((game) => {
    if (state.filter === "active") return Number(game.Estado) === 2;
    if (state.filter === "finished") return Number(game.Estado) === 3;
    return true;
  });
}

function renderGames() {
  const bettingGames = filteredBettingGames();
  const resultsGames = filteredResultsGames();

  elements.bettingGamesCount.textContent = `${bettingGames.length} registos`;
  elements.resultsGamesCount.textContent = `${resultsGames.length} registos`;

  elements.bettingGamesBody.innerHTML = bettingGames.length
    ? bettingGames
        .map(
          (game) => `
            <tr>
              <td>${escapeHtml(game.CodigoJogo)}</td>
              <td>${escapeHtml(game.Competicao || "—")}</td>
              <td>${escapeHtml(game.EquipaCasa)} vs ${escapeHtml(game.EquipaFora)}</td>
              <td>${formatDateOnly(game.DataJogo)} ${escapeHtml(game.HoraInicio || "")}</td>
              <td><span class="badge ${statusClassForGame(Number(game.Estado))}">${escapeHtml(gameStatusLabels[Number(game.Estado)] || game.Estado)}</span></td>
              <td>
                <div class="row-actions">
                  <button type="button" class="chip" data-action="edit-betting-game" data-code="${encodeURIComponent(game.CodigoJogo || "")}">Editar</button>
                  <button type="button" class="chip danger" data-action="delete-betting-game" data-code="${encodeURIComponent(game.CodigoJogo || "")}">Remover</button>
                </div>
              </td>
            </tr>
          `
        )
        .join("")
    : '<tr><td colspan="6" class="empty-state">Sem jogos na API de apostas para este filtro.</td></tr>';

  elements.resultsGamesBody.innerHTML = resultsGames.length
    ? resultsGames
        .map(
          (game) => `
            <tr>
              <td>${escapeHtml(game.CodigoJogo)}</td>
              <td>${escapeHtml(game.EquipaCasa)} vs ${escapeHtml(game.EquipaFora)}</td>
              <td>${formatDateOnly(game.DataJogo)} ${escapeHtml(game.HoraInicio || "")}</td>
              <td>${Number(game.GolosCasa)} - ${Number(game.GolosFora)}</td>
              <td><span class="badge ${statusClassForGame(Number(game.Estado))}">${escapeHtml(gameStatusLabels[Number(game.Estado)] || game.Estado)}</span></td>
              <td>
                <div class="row-actions">
                  <button type="button" class="chip" data-action="edit-federation-game" data-code="${encodeURIComponent(game.CodigoJogo || "")}">Editar</button>
                  <button type="button" class="chip danger" data-action="delete-federation-game" data-code="${encodeURIComponent(game.CodigoJogo || "")}">Remover</button>
                </div>
              </td>
            </tr>
          `
        )
        .join("")
    : '<tr><td colspan="6" class="empty-state">Sem jogos na API da federação para este filtro.</td></tr>';
}

function renderBets() {
  elements.betsCount.textContent = `${state.bets.length} registos`;
  elements.betsBody.innerHTML = state.bets.length
    ? state.bets
        .slice()
        .sort((left, right) => new Date(right.DataHoraUtc).getTime() - new Date(left.DataHoraUtc).getTime())
        .map(
          (bet) => `
            <tr>
              <td>${bet.Id}</td>
              <td>${bet.JogoId}</td>
              <td>${bet.UtilizadorId}</td>
              <td>${escapeHtml(betTypeLabels[bet.TipoAposta] || bet.TipoAposta)}</td>
              <td>${formatCurrency(bet.ValorApostado)}</td>
              <td>${formatNumber(bet.OddMomento)}</td>
              <td><span class="badge ${statusClassForBet(Number(bet.Estado))}">${escapeHtml(betStatusLabels[Number(bet.Estado)] || bet.Estado)}</span></td>
              <td>${formatDate(bet.DataHoraUtc)}</td>
              <td>
                <div class="row-actions">
                  <button type="button" class="chip danger" data-action="cancel-bet" data-id="${bet.Id}" data-user="${bet.UtilizadorId}">Cancelar</button>
                </div>
              </td>
            </tr>
          `
        )
        .join("")
    : '<tr><td colspan="9" class="empty-state">Ainda não há apostas carregadas.</td></tr>';
}

function renderUsers() {
  elements.usersCount.textContent = `${state.users.length} registos`;
  elements.usersBody.innerHTML = state.users.length
    ? state.users
        .slice()
        .sort((left, right) => Number(left.Id) - Number(right.Id))
        .map(
          (user) => `
            <tr>
              <td>${user.Id}</td>
              <td>${escapeHtml(user.Nome || "—")}</td>
              <td>${escapeHtml(user.Email || "—")}</td>
              <td>${formatCurrency(user.SaldoDisponivel)}</td>
              <td>${formatCurrency(user.SaldoGastoTotal)}</td>
              <td>${formatDate(user.CriadoEmUtc)}</td>
            </tr>
          `
        )
        .join("")
    : '<tr><td colspan="6" class="empty-state">Sem utilizadores carregados.</td></tr>';
}

function renderStatsPanel() {
  if (state.statsGame) {
    elements.statsGameCode.textContent = state.statsGame.CodigoJogo || "—";
    elements.statsGameState.textContent = "carregado";
    elements.statsGameState.className = "status-dot ok";

    const row = state.statsGame;
    elements.gameStatsGrid.innerHTML = `
      <div><span>Total apostado</span><strong>${formatCurrency(row.TotalApostado)}</strong></div>
      <div><span>Casa</span><strong>${Number(row.ApostasCasa)}</strong></div>
      <div><span>Empate</span><strong>${Number(row.ApostasEmpate)}</strong></div>
      <div><span>Fora</span><strong>${Number(row.ApostasFora)}</strong></div>
      <div><span>Pendentes</span><strong>${Number(row.Pendentes)}</strong></div>
      <div><span>Margem</span><strong>${formatNumber(row.MargemPlataforma)}</strong></div>
    `;
  } else {
    elements.statsGameCode.textContent = "—";
    elements.statsGameState.textContent = "aguarda pesquisa";
    elements.statsGameState.className = "status-dot";
    elements.gameStatsGrid.innerHTML = `
      <div><span>Total apostado</span><strong>—</strong></div>
      <div><span>Casa</span><strong>—</strong></div>
      <div><span>Empate</span><strong>—</strong></div>
      <div><span>Fora</span><strong>—</strong></div>
      <div><span>Pendentes</span><strong>—</strong></div>
      <div><span>Margem</span><strong>—</strong></div>
    `;
  }

  if (state.statsCompetition !== null && state.statsCompetition !== undefined) {
    elements.statsCompetitionState.textContent = "carregado";
    elements.statsCompetitionState.className = "status-dot ok";
    elements.statsCompetitionCode.textContent = state.statsCompetitionCode || "—";
    elements.competitionStatsOutput.textContent = JSON.stringify(state.statsCompetition, null, 2);
  } else {
    elements.statsCompetitionState.textContent = "aguarda pesquisa";
    elements.statsCompetitionState.className = "status-dot";
    elements.statsCompetitionCode.textContent = "—";
    elements.competitionStatsOutput.textContent = "{}";
  }
}

function renderDashboardPanel() {
  if (!state.dashboard || !state.dashboard.Resumo) {
    elements.dashboardState.textContent = "a aguardar stream";
    elements.dashboardState.className = "status-dot";
    elements.dashboardUpdatedAt.textContent = "sem dados de analytics";
    elements.dashboardVolume.textContent = "—";
    elements.dashboardVolumeTile.textContent = "—";
    elements.dashboardActiveGames.textContent = "—";
    elements.dashboardPendingBets.textContent = "—";
    elements.dashboardWinRate.textContent = "—";
    elements.dashboardCompetitions.innerHTML = '<div class="feed-item"><strong>Sem snapshot</strong><small>O worker ainda não publicou métricas.</small></div>';
    elements.dashboardWindows.innerHTML = '<div class="feed-item"><strong>Sem janelas</strong><small>O stream ainda não gerou janelas temporais.</small></div>';
    elements.dashboardExposures.innerHTML = '<div class="feed-item"><strong>Sem exposição</strong><small>Os jogos aparecem aqui com o respetivo risco agregado.</small></div>';
    elements.dashboardBetTypes.innerHTML = '<div class="feed-item"><strong>Sem tipo de aposta</strong><small>Os rácios por tipo aparecem após o primeiro snapshot.</small></div>';
    elements.dashboardAlerts.innerHTML = '<div class="feed-item"><strong>Sem alertas</strong><small>Os alertas aparecerão aqui quando o worker detetar uma anomalia.</small></div>';
    elements.dashboardEvents.innerHTML = '<div class="feed-item"><strong>Sem eventos</strong><small>Os últimos eventos do stream surgirão aqui.</small></div>';
    return;
  }

  const resumo = state.dashboard.Resumo;
  elements.dashboardState.textContent = "online";
  elements.dashboardState.className = "status-dot ok";
  elements.dashboardUpdatedAt.textContent = `Atualizado em ${formatDate(resumo.AtualizadoUtc)}`;
  const volumeText = formatCurrency(resumo.VolumeTotalApostado);
  elements.dashboardVolume.textContent = volumeText;
  elements.dashboardVolumeTile.textContent = volumeText;
  elements.dashboardActiveGames.textContent = String(resumo.JogosAtivos ?? 0);
  elements.dashboardPendingBets.textContent = String(resumo.ApostasPendentes ?? 0);
  if (elements.dashboardBettingsPerMinute) {
    elements.dashboardBettingsPerMinute.textContent = formatNumber(resumo.ApostasPorMinuto ?? 0);
  }
  if (elements.dashboardMovingAverage) {
    elements.dashboardMovingAverage.textContent = formatCurrency(resumo.MediaMovel15MinVolume ?? 0);
  }
  if (elements.dashboardPeakHour) {
    elements.dashboardPeakHour.textContent = formatCurrency(resumo.MaxVolumeHora ?? 0);
  }
  if (elements.dashboardLowHour) {
    elements.dashboardLowHour.textContent = formatCurrency(resumo.MinVolumeHora ?? 0);
  }
  const winRate = Number(resumo.ApostasGanhas || 0) + Number(resumo.ApostasPendentes || 0) > 0
    ? ((Number(resumo.ApostasGanhas || 0) / (Number(resumo.ApostasGanhas || 0) + Number(resumo.ApostasPendentes || 0))) * 100).toFixed(1)
    : "0.0";
  elements.dashboardWinRate.textContent = `${winRate}%`;

  elements.dashboardCompetitions.innerHTML = state.dashboard.Competicoes.length
    ? state.dashboard.Competicoes
        .slice(0, 5)
        .map(
          (competition) => `
            <div class="feed-item">
              <strong>${escapeHtml(competition.Competicao || "—")}</strong>
              <small>${formatCurrency(competition.VolumeTotalApostado)} • média ${formatNumber(competition.MediaGolosPorJogo)} golos/jogo</small>
              <small>1: ${formatNumber((Number(competition.TaxaResultado1 || 0) * 100))}% | X: ${formatNumber((Number(competition.TaxaResultadoX || 0) * 100))}% | 2: ${formatNumber((Number(competition.TaxaResultado2 || 0) * 100))}%</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem competições</strong><small>Não há métricas agregadas disponíveis.</small></div>';

  elements.dashboardWindows.innerHTML = state.dashboard.JanelasTemporais.length
    ? state.dashboard.JanelasTemporais
        .slice(0, 8)
        .map(
          (windowSlice) => `
            <div class="feed-item">
              <strong>${formatDate(windowSlice.JanelaInicioUtc)}</strong>
              <small>${Number(windowSlice.Apostas || 0)} apostas • ${formatCurrency(windowSlice.VolumeTotal)} • média ${formatCurrency(windowSlice.MediaAposta)}</small>
              <small>máx. ${formatCurrency(windowSlice.MaxAposta)} | mín. ${formatCurrency(windowSlice.MinAposta)}</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem janelas</strong><small>Não há dados temporais para mostrar.</small></div>';

  elements.dashboardExposures.innerHTML = state.dashboard.ExposicoesJogos.length
    ? state.dashboard.ExposicoesJogos
        .slice(0, 10)
        .map(
          (item) => `
            <div class="feed-item">
              <strong>${escapeHtml(item.CodigoJogo || `Jogo #${item.JogoId}`)}${item.Competicao ? ` • ${escapeHtml(item.Competicao)}` : ""}</strong>
              <small>${formatCurrency(item.VolumeTotal)} de volume • exposição líquida ${formatCurrency(item.ExposicaoLiquida)}</small>
              <small>${Number(item.ApostasPendentes || 0)} apostas pendentes</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem exposição</strong><small>Os jogos aparecerão aqui assim que houver apostas.</small></div>';

  elements.dashboardBetTypes.innerHTML = state.dashboard.TiposAposta.length
    ? state.dashboard.TiposAposta
        .map(
          (item) => `
            <div class="feed-item">
              <strong>${escapeHtml(betTypeLabels[item.TipoAposta] || item.TipoAposta)}</strong>
              <small>${Number(item.TotalApostas || 0)} apostas • vitória ${(Number(item.TaxaVitoria || 0) * 100).toFixed(1)}%</small>
              <small>${Number(item.ApostasGanhas || 0)} ganhas | ${Number(item.ApostasPerdidas || 0)} perdidas • ${formatCurrency(item.VolumeTotal)}</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem tipo de aposta</strong><small>O worker ainda não agregou desempenho por tipo.</small></div>';

  elements.dashboardAlerts.innerHTML = state.dashboard.Alertas.length
    ? state.dashboard.Alertas
        .slice(0, 6)
        .map(
          (alert) => `
            <div class="feed-item alert-${escapeHtml(String(alert.Severidade || "Média")).toLowerCase()}">
              <strong>${escapeHtml(alert.Severidade || "Média")}${alert.Competicao ? ` • ${escapeHtml(alert.Competicao)}` : ""}</strong>
              <small>${escapeHtml(alert.Mensagem || "Sem mensagem")}</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem alertas</strong><small>Sem anomalias registadas no intervalo recente.</small></div>';

  elements.dashboardEvents.innerHTML = state.dashboard.EventosRecentes.length
    ? state.dashboard.EventosRecentes
        .slice(0, 8)
        .map(
          (event) => `
            <div class="feed-item">
              <strong>${escapeHtml(event.EventType || "Evento")}</strong>
              <small>#${Number(event.EventSequence || 0)} • ${escapeHtml(event.SourceSystem || "—")} • ${escapeHtml(event.AggregateType || "—")}:${escapeHtml(event.AggregateKey || "—")}</small>
              <small>${formatDate(event.CriadoEmUtc)}</small>
            </div>
          `
        )
        .join("")
    : '<div class="feed-item"><strong>Sem eventos</strong><small>O stream ainda não foi preenchido.</small></div>';
}

function renderMiddlewarePanel() {
  if (!elements.middlewareBroker) {
    return;
  }

  const stream = state.streamStatus;
  const eventCount = state.dashboard?.EventosRecentes?.length || 0;
  const alertCount = state.dashboard?.Alertas?.length || 0;

  if (!stream) {
    elements.middlewareBroker.textContent = "offline";
    elements.middlewareBrokerDetail.textContent = "Sem resposta de /api/analytics/stream.";
    elements.middlewareLastSequence.textContent = "--";
    elements.middlewareReplayCheckpoint.textContent = "--";
    elements.middlewarePendingEvents.textContent = "--";
    if (elements.middlewareEvidence) {
      elements.middlewareEvidence.innerHTML = `
        <div class="feed-item">
          <strong>A aguardar stream</strong>
          <small>Confirma se a Betting API, o SQL Server, o Kafka e o RabbitMQ estao ativos.</small>
        </div>
      `;
    }
    return;
  }

  elements.middlewareBroker.textContent = stream.Broker || "RabbitMQ + Kafka";
  elements.middlewareBrokerDetail.textContent = stream.Exchange || "betstrike.events / Kafka topics";
  elements.middlewareLastSequence.textContent = String(stream.LastEventSequence ?? 0);
  elements.middlewareReplayCheckpoint.textContent = String(stream.ReplayCheckpoint ?? 0);
  elements.middlewarePendingEvents.textContent = String(stream.PendingEvents ?? 0);

  if (elements.middlewareEvidence) {
    elements.middlewareEvidence.innerHTML = `
      <div class="feed-item">
        <strong>Eventos recentes no dashboard</strong>
        <small>${eventCount} eventos visiveis no snapshot atual. Ultima atualizacao: ${formatDate(stream.AtualizadoUtc)}</small>
      </div>
      <div class="feed-item">
        <strong>Alertas automaticos</strong>
        <small>${alertCount} alertas recentes. Usa uma aposta >= 100 EUR para provocar sinalizacao.</small>
      </div>
      <div class="feed-item">
        <strong>Replay controlado</strong>
        <small>Se pendentes for 0, o checkpoint esta alinhado com o stream persistido.</small>
      </div>
    `;
  }
}

function appendLog(kind, title, detail) {
  const entry = document.createElement("div");
  entry.className = `log-entry ${kind}`;
  entry.innerHTML = `
    <strong>${escapeHtml(title)}</strong>
    <div>${escapeHtml(typeof detail === "string" ? detail : JSON.stringify(detail, null, 2))}</div>
  `;
  elements.activityLog.prepend(entry);
}

async function handleSettingsSubmit(event) {
  event.preventDefault();
  const formData = new FormData(event.currentTarget);

  state.settings = {
    bettingBase: normalizeBaseUrl(formData.get("bettingApiBase"), DEFAULT_BETTING_BASE),
    resultsBase: normalizeBaseUrl(formData.get("resultsApiBase"), DEFAULT_RESULTS_BASE),
  };

  saveSettings();
  updateBaseLabels();
  appendLog("success", "Configuração guardada", state.settings);
  await loadData();
}

async function handleCreateUser(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const formData = new FormData(form);

  try {
    const payload = {
      nome: String(formData.get("nome") || "").trim(),
      email: String(formData.get("email") || "").trim(),
    };

    const createdId = await requestJson(state.settings.bettingBase, "/api/utilizadores", {
      method: "POST",
      body: JSON.stringify(payload),
    });

    state.lastCreatedUserId = Number(createdId);
    if (elements.registerBetUserId) {
      elements.registerBetUserId.value = String(createdId);
    }

    appendLog("success", `Utilizador criado #${createdId}`, payload);
    if (form) {
      form.reset();
    }
    await loadData();
  } catch (error) {
    appendLog("error", "Erro ao criar utilizador", error.message);
  }
}

async function handleCreateBettingGame(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const formData = new FormData(form);

  try {
    const payload = {
      codigoJogo: String(formData.get("codigoJogo") || "").trim(),
      dataJogo: normalizeDateInput(formData.get("dataJogo")),
      horaInicio: normalizeTimeInput(formData.get("horaInicio")),
      equipaCasa: String(formData.get("equipaCasa") || "").trim(),
      equipaFora: String(formData.get("equipaFora") || "").trim(),
      competicao: String(formData.get("competicao") || "").trim(),
      estado: Number(formData.get("estado") || 1),
    };

    const createdId = await requestJson(state.settings.bettingBase, "/api/apostas/jogos", {
      method: "POST",
      body: JSON.stringify(payload),
    });

    appendLog("success", `Jogo de apostas criado #${createdId}`, payload);
    if (form) {
      form.reset();
    }
    await loadData();
  } catch (error) {
    appendLog("error", "Erro ao criar jogo de apostas", error.message);
  }
}

async function handleCreateFederationGame(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const formData = new FormData(form);

  try {
    const payload = {
      codigoJogo: String(formData.get("codigoJogo") || "").trim(),
      dataJogo: normalizeDateInput(formData.get("dataJogo")),
      horaInicio: normalizeTimeInput(formData.get("horaInicio")),
      equipaCasa: String(formData.get("equipaCasa") || "").trim(),
      equipaFora: String(formData.get("equipaFora") || "").trim(),
      estado: Number(formData.get("estado") || 1),
    };

    const createdId = await requestJson(state.settings.resultsBase, "/api/jogos", {
      method: "POST",
      body: JSON.stringify(payload),
    });

    appendLog("success", `Jogo da federação criado #${createdId}`, payload);
    if (form) {
      form.reset();
    }
    await loadData();
  } catch (error) {
    appendLog("error", "Erro ao criar jogo da federação", error.message);
  }
}

async function handleRegisterBet(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const formData = new FormData(form);

  try {
    const jogoId = Number(formData.get("jogoId") || 0);
    const utilizadorId = Number(formData.get("utilizadorId") || 0);

    if (!Number.isInteger(jogoId) || jogoId <= 0) {
      appendLog("error", "Erro ao registar aposta", "Seleciona um jogo válido da API de Apostas.");
      return;
    }

    if (!state.bettingGames.some((game) => Number(game.Id) === jogoId)) {
      appendLog("error", "Erro ao registar aposta", "Jogo não existe na BD de Apostas. Usa um ID da lista de jogos de apostas.");
      return;
    }

    if (!Number.isInteger(utilizadorId) || utilizadorId <= 0) {
      appendLog("error", "Erro ao registar aposta", "Indica um Utilizador ID válido.");
      return;
    }

    const payload = {
      jogoId,
      utilizadorId,
      tipoAposta: String(formData.get("tipoAposta") || "1"),
      valorApostado: Number(formData.get("valorApostado") || 0),
      oddMomento: Number(formData.get("oddMomento") || 0),
    };

    const createdId = await requestJson(state.settings.bettingBase, "/api/apostas", {
      method: "POST",
      body: JSON.stringify(payload),
    });

    appendLog("success", `Aposta registada #${createdId}`, payload);
    if (form) {
      const preservedUserId = elements.registerBetUserId?.value || "";
      form.reset();
      if (elements.registerBetUserId && preservedUserId) {
        elements.registerBetUserId.value = preservedUserId;
      }
    }
    await loadData();
  } catch (error) {
    appendLog("error", "Erro ao registar aposta", error.message);
  }
}

async function handleInsertResult(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const formData = new FormData(form);

  try {
    const jogoId = Number(formData.get("jogoId") || 0);
    if (!Number.isInteger(jogoId) || jogoId <= 0 || !state.bettingGames.some((game) => Number(game.Id) === jogoId)) {
      appendLog("error", "Erro ao inserir resultado", "Seleciona um Jogo ID válido da API de Apostas.");
      return;
    }

    const payload = {
      jogoId,
      golosCasa: Number(formData.get("golosCasa") || 0),
      golosFora: Number(formData.get("golosFora") || 0),
    };

    await requestJson(state.settings.bettingBase, "/api/resultados", {
      method: "POST",
      body: JSON.stringify(payload),
    });

    appendLog("success", `Resultado inserido para o jogo #${payload.jogoId}`, payload);
    if (form) {
      form.reset();
    }
    await loadData();
  } catch (error) {
    appendLog("error", "Erro ao inserir resultado", error.message);
  }
}

async function handleStatsSubmit(event) {
  event.preventDefault();
  const formData = new FormData(event.currentTarget);
  const codigoJogo = String(formData.get("codigoJogo") || "").trim();
  const competicao = String(formData.get("competicao") || "").trim();

  state.statsGame = null;
  state.statsCompetition = null;
  state.statsCompetitionCode = competicao || "—";
  renderStatsPanel();

  try {
    if (codigoJogo) {
      state.statsGame = normalizeGameStats(
        await requestJson(state.settings.bettingBase, `/api/estatisticas/jogo/${encodeURIComponent(codigoJogo)}`)
      );
    }
  } catch (error) {
    appendLog("error", "Falha ao carregar estatísticas por jogo", error.message);
  }

  try {
    if (competicao) {
      state.statsCompetition = await requestJson(state.settings.bettingBase, `/api/estatisticas/competicao/${encodeURIComponent(competicao)}`);
    }
  } catch (error) {
    appendLog("error", "Falha ao carregar estatísticas por competição", error.message);
  }

  if (!codigoJogo && !competicao) {
    appendLog("neutral", "Pesquisa vazia", "Introduz um código de jogo, uma competição, ou ambos.");
  } else {
    appendLog("success", "Estatísticas carregadas", { codigoJogo, competicao });
  }

  renderStatsPanel();
}

function bindEvents() {
  elements.refreshButton.addEventListener("click", loadData);
  elements.settingsForm.addEventListener("submit", handleSettingsSubmit);
  elements.createUserForm.addEventListener("submit", handleCreateUser);
  elements.createBettingGameForm.addEventListener("submit", handleCreateBettingGame);
  elements.createFederationGameForm.addEventListener("submit", handleCreateFederationGame);
  elements.registerBetForm.addEventListener("submit", handleRegisterBet);
  elements.insertResultForm.addEventListener("submit", handleInsertResult);
  elements.statsForm.addEventListener("submit", handleStatsSubmit);

  elements.bettingGamesBody.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;

    const codigoJogo = decodeURIComponent(button.dataset.code || "");
    try {
      if (button.dataset.action === "edit-betting-game") {
        await editBettingGame(codigoJogo);
      } else if (button.dataset.action === "delete-betting-game") {
        await deleteBettingGame(codigoJogo);
      }
    } catch (error) {
      appendLog("error", "Falha na sincronização com a API de apostas", error.message);
    }
  });

  elements.resultsGamesBody.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;

    const codigoJogo = decodeURIComponent(button.dataset.code || "");
    try {
      if (button.dataset.action === "edit-federation-game") {
        await editFederationGame(codigoJogo);
      } else if (button.dataset.action === "delete-federation-game") {
        await deleteFederationGame(codigoJogo);
      }
    } catch (error) {
      appendLog("error", "Falha na sincronização com a API da Federação", error.message);
    }
  });

  elements.betsBody.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;

    try {
      if (button.dataset.action === "cancel-bet") {
        await cancelBet(Number(button.dataset.id), Number(button.dataset.user));
      }
    } catch (error) {
      appendLog("error", "Falha ao cancelar aposta", error.message);
    }
  });

  document.querySelectorAll("[data-filter]").forEach((button) => {
    button.addEventListener("click", () => {
      state.filter = button.dataset.filter || "all";
      renderGames();
      document.querySelectorAll("[data-filter]").forEach((chip) => chip.classList.toggle("active", chip === button));
    });
  });
}

function hydrateElements() {
  const ids = [
    "refreshButton",
    "swaggerBettingLink",
    "swaggerResultsLink",
    "bettingBaseLabel",
    "resultsBaseLabel",
    "bettingStatusDot",
    "resultsStatusDot",
    "summaryBettingGames",
    "summaryResultsGames",
    "summaryBets",
    "summaryStake",
    "bettingApiBase",
    "resultsApiBase",
    "settingsForm",
    "metricActiveGames",
    "metricPendingBets",
    "metricWonBets",
    "metricCompetition",
    "metricCompetitionDetail",
    "dashboardState",
    "dashboardUpdatedAt",
    "dashboardVolume",
    "dashboardVolumeTile",
    "dashboardActiveGames",
    "dashboardPendingBets",
    "dashboardWinRate",
    "dashboardCompetitions",
    "dashboardWindows",
    "dashboardExposures",
    "dashboardBetTypes",
    "dashboardAlerts",
    "dashboardEvents",
    "dashboardBettingsPerMinute",
    "dashboardMovingAverage",
    "dashboardPeakHour",
    "dashboardLowHour",
    "middlewareBroker",
    "middlewareBrokerDetail",
    "middlewareLastSequence",
    "middlewareReplayCheckpoint",
    "middlewarePendingEvents",
    "middlewareEvidence",
    "bettingGamesCount",
    "resultsGamesCount",
    "bettingGamesBody",
    "resultsGamesBody",
    "betsCount",
    "betsBody",
    "activityLog",
    "statsGameCode",
    "statsGameState",
    "gameStatsGrid",
    "statsCompetitionCode",
    "statsCompetitionState",
    "competitionStatsOutput",
    "lastUpdated",
  ];

  ids.forEach((id) => {
    elements[id] = document.getElementById(id);
  });

  elements.refreshButton = document.getElementById("refreshButton");
  elements.swaggerBettingLink = document.getElementById("swaggerBettingLink");
  elements.swaggerResultsLink = document.getElementById("swaggerResultsLink");
  elements.bettingBaseLabel = document.getElementById("bettingBaseLabel");
  elements.resultsBaseLabel = document.getElementById("resultsBaseLabel");
  elements.bettingStatusDot = document.getElementById("bettingStatusDot");
  elements.resultsStatusDot = document.getElementById("resultsStatusDot");
  elements.summaryBettingGames = document.getElementById("summaryBettingGames");
  elements.summaryResultsGames = document.getElementById("summaryResultsGames");
  elements.summaryBets = document.getElementById("summaryBets");
  elements.summaryStake = document.getElementById("summaryStake");
  elements.bettingApiBase = document.getElementById("bettingApiBase");
  elements.resultsApiBase = document.getElementById("resultsApiBase");
  elements.settingsForm = document.getElementById("settingsForm");
  elements.metricActiveGames = document.getElementById("metricActiveGames");
  elements.metricPendingBets = document.getElementById("metricPendingBets");
  elements.metricWonBets = document.getElementById("metricWonBets");
  elements.metricCompetition = document.getElementById("metricCompetition");
  elements.metricCompetitionDetail = document.getElementById("metricCompetitionDetail");
  elements.dashboardState = document.getElementById("dashboardState");
  elements.dashboardUpdatedAt = document.getElementById("dashboardUpdatedAt");
  elements.dashboardVolume = document.getElementById("dashboardVolume");
  elements.dashboardVolumeTile = document.getElementById("dashboardVolumeTile");
  elements.dashboardActiveGames = document.getElementById("dashboardActiveGames");
  elements.dashboardPendingBets = document.getElementById("dashboardPendingBets");
  elements.dashboardWinRate = document.getElementById("dashboardWinRate");
  elements.dashboardCompetitions = document.getElementById("dashboardCompetitions");
  elements.dashboardWindows = document.getElementById("dashboardWindows");
  elements.dashboardExposures = document.getElementById("dashboardExposures");
  elements.dashboardBetTypes = document.getElementById("dashboardBetTypes");
  elements.dashboardAlerts = document.getElementById("dashboardAlerts");
  elements.dashboardEvents = document.getElementById("dashboardEvents");
  elements.dashboardBettingsPerMinute = document.getElementById("dashboardBettingsPerMinute");
  elements.dashboardMovingAverage = document.getElementById("dashboardMovingAverage");
  elements.dashboardPeakHour = document.getElementById("dashboardPeakHour");
  elements.dashboardLowHour = document.getElementById("dashboardLowHour");
  elements.bettingGamesCount = document.getElementById("bettingGamesCount");
  elements.resultsGamesCount = document.getElementById("resultsGamesCount");
  elements.bettingGamesBody = document.getElementById("bettingGamesBody");
  elements.resultsGamesBody = document.getElementById("resultsGamesBody");
  elements.betsCount = document.getElementById("betsCount");
  elements.betsBody = document.getElementById("betsBody");
  elements.usersCount = document.getElementById("usersCount");
  elements.usersBody = document.getElementById("usersBody");
  elements.activityLog = document.getElementById("activityLog");
  elements.statsGameCode = document.getElementById("statsGameCode");
  elements.statsGameState = document.getElementById("statsGameState");
  elements.gameStatsGrid = document.getElementById("gameStatsGrid");
  elements.statsCompetitionCode = document.getElementById("statsCompetitionCode");
  elements.statsCompetitionState = document.getElementById("statsCompetitionState");
  elements.competitionStatsOutput = document.getElementById("competitionStatsOutput");
  elements.lastUpdated = document.getElementById("lastUpdated");

  elements.createUserForm = document.getElementById("createUserForm");
  elements.createBettingGameForm = document.getElementById("createBettingGameForm");
  elements.createFederationGameForm = document.getElementById("createFederationGameForm");
  elements.registerBetForm = document.getElementById("registerBetForm");
  elements.registerBetGameId = document.getElementById("registerBetGameId");
  elements.registerBetUserId = document.getElementById("registerBetUserId");
  elements.lastCreatedUserHint = document.getElementById("lastCreatedUserHint");
  elements.insertResultForm = document.getElementById("insertResultForm");
  elements.insertResultGameId = document.getElementById("insertResultGameId");
  elements.statsForm = document.getElementById("statsForm");
}

function hydrateSettingsForm() {
  elements.bettingApiBase.value = state.settings.bettingBase;
  elements.resultsApiBase.value = state.settings.resultsBase;
}

async function init() {
  hydrateElements();
  hydrateSettingsForm();
  updateBaseLabels();
  bindEvents();
  await loadData();
  setInterval(loadData, 5000);
}

window.addEventListener("DOMContentLoaded", init);
