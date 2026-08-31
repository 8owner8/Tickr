/**
 * Tickr — Precision Dark Application Engine
 */

// Titlebar controls - bridge to WinForms via WebView2 PostMessage
(function setupTitlebar() {
  const isWebView2 = typeof chrome !== 'undefined' && chrome.webview;
  function send(msg) { if (isWebView2) chrome.webview.postMessage(msg); }

  document.getElementById('btn-tb-min')?.addEventListener('click', () => send('titlebar:minimize'));
  document.getElementById('btn-tb-max')?.addEventListener('click', () => send('titlebar:maximize'));
  document.getElementById('btn-tb-close')?.addEventListener('click', () => send('titlebar:close'));

  // Drag: on mousedown on the drag area send a drag message
  document.getElementById('titlebar-drag')?.addEventListener('mousedown', (e) => {
    if (e.button === 0) send('titlebar:drag');
  });

  // Double click on the titlebar toggles maximize
  document.getElementById('titlebar')?.addEventListener('dblclick', (e) => {
    if (e.target.closest('.tbtn')) return;
    send('titlebar:maximize');
  });
})();

// Borderless window resize — thin edge zones bridge mouse events to Win32 resize
(function setupResizeZones() {
  const isWebView2 = typeof chrome !== 'undefined' && chrome.webview;
  if (!isWebView2) return;

  document.body.classList.add('webview2');

  const zones = ['n', 's', 'e', 'w', 'nw', 'ne', 'sw', 'se'];
  for (const dir of zones) {
    const zone = document.createElement('div');
    zone.className = `rz rz-${dir}`;
    zone.addEventListener('mousedown', (e) => {
      if (e.button !== 0) return;
      e.preventDefault();
      e.stopPropagation();
      chrome.webview.postMessage(`titlebar:resize:${dir}`);
    });
    document.body.appendChild(zone);
  }
})();

(function () {
  'use strict';

  const isWebView2 = typeof chrome !== 'undefined' && chrome.webview;

  const state = {
    bots: {},
    games: {},
    gamesErrors: {},
    gamesFetchedAt: 0,
    gamesLoading: false,
    activeTab: 'dashboard',
    twoFaCountdown: 30,
    apiBase: window.location.protocol === 'file:' ? 'http://127.0.0.1:1242' : '',
    logEntries: [],
    awaitingPassword: false,
    qrPollTimer: null,
    qrPolling: false,
    qrPollFailures: 0,
    qrStartedAt: 0,
    qrBotName: null,
    qrLastChallenge: null,
    credentialsBotName: null,
    credentialsPollTimer: null,
    credentialsInputType: 0,
    credentialsEnabling: false,
    selectedGames: new Set(),
    farmingActionPending: false,
    liveGameMetadata: new Map(),
    removeAccountBotName: null,
    removeAccountPending: false
  };

  const el = {
    navItems:            document.querySelectorAll('.nav-item[data-tab]'),
    tabViews:            document.querySelectorAll('.tab-view'),
    botGridContainer:    document.getElementById('bot-grid-container'),
    gamesGridContainer:  document.getElementById('games-grid-container'),
    statBotsCount:       document.getElementById('stat-bots-count'),
    statCardsRemaining:  document.getElementById('stat-cards-remaining'),
    statWalletBalance:   document.getElementById('stat-wallet-balance'),
    gamesSearchInput:    document.getElementById('games-search-input'),
    gamesBotFilter:      document.getElementById('games-bot-filter'),
    gamesSort:           document.getElementById('games-sort'),
    gamesLibraryStatus:  document.getElementById('games-library-status'),
    gamesSelectionCount: document.getElementById('games-selection-count'),
    btnStartSelected:    document.getElementById('btn-start-selected'),
    btnStopFarming:      document.getElementById('btn-stop-farming'),
    twofaBotSelect:      document.getElementById('twofa-bot-select'),
    twofaCodeVal:        document.getElementById('twofa-code-val'),
    twofaCountdown:      document.getElementById('twofa-countdown'),
    twofaTimerFill:      document.getElementById('twofa-timer-fill'),
    twofaConfirmations:  document.getElementById('twofa-confirmations-list'),
    redeemerBotSelect:   document.getElementById('redeemer-bot-select'),
    redeemerKeysInput:   document.getElementById('redeemer-keys-input'),
    btnRedeemKeys:       document.getElementById('btn-redeem-keys'),
    btnClearKeys:        document.getElementById('btn-clear-keys'),
    redeemerResultsBox:  document.getElementById('redeemer-results-box'),
    redeemerResultsList: document.getElementById('redeemer-results-list'),
    terminalLogs:        document.getElementById('terminal-logs-container'),
    terminalInput:       document.getElementById('terminal-cmd-input'),
    btnClearTerminal:    document.getElementById('btn-clear-terminal'),
    modalAddBot:         document.getElementById('modal-add-bot'),
    btnAddBot:           document.getElementById('btn-add-bot'),
    btnCloseModal:       document.getElementById('btn-close-modal'),
    authTabBtns:         document.querySelectorAll('.auth-tab-btn[data-auth-tab]'),
    btnSubmitCreds:      document.getElementById('btn-submit-credentials'),
    credentialsProgress: document.getElementById('credentials-progress'),
    credentialsStatus:   document.getElementById('credentials-status'),
    credentialsHelp:     document.getElementById('credentials-help'),
    steamMobileApproval: document.getElementById('steam-mobile-approval'),
    credentialsCodeGroup:document.getElementById('credentials-code-group'),
    credentialsCodeLabel:document.getElementById('credentials-code-label'),
    credentialsCode:     document.getElementById('credentials-code'),
    btnSubmitGuardCode:  document.getElementById('btn-submit-guard-code'),
    btnQrStart:          document.getElementById('btn-qr-start'),
    btnQrCancel:         document.getElementById('btn-qr-cancel'),
    qrMock:              document.getElementById('qr-mock'),
    qrStatus:            document.getElementById('qr-status'),
    btnSubmitMafile:     document.getElementById('btn-submit-mafile'),
    modalIpcPassword:    document.getElementById('modal-ipc-password'),
    ipcPasswordInput:    document.getElementById('ipc-password-input'),
    btnSubmitIpcPassword:document.getElementById('btn-submit-ipc-password'),
    toastContainer:      document.getElementById('toast-container'),
    sidebarConnDot:      document.getElementById('sidebar-conn-dot'),
    sidebarConnLabel:    document.getElementById('sidebar-conn-label'),
    startupLoader:       document.getElementById('startup-loader'),
    startupLoaderStatus: document.getElementById('startup-loader-status'),
    modalRemoveAccount:  document.getElementById('modal-remove-account'),
    removeAccountMessage:document.getElementById('remove-account-message'),
    btnCancelRemoveAccount: document.getElementById('btn-cancel-remove-account'),
    btnConfirmRemoveAccount:document.getElementById('btn-confirm-remove-account'),
  };

  async function init() {
    const loadingStartedAt = performance.now();
    setupNavigation();
    setupModal();
    setupRemoveAccountModal();
    setupAuthTabs();
    setupActions();
    setupTerminal();
    setup2FA();
    setupIpcPassword();
    setupRipple();
    setupGrainOverlay();
    setLoaderStatus('Connecting to the local Tickr service…');
    await fetchBotsData();
    setLoaderStatus('Loading your Steam library…');
    await fetchOwnedGames();
    const remainingDelay = Math.max(0, 850 - (performance.now() - loadingStartedAt));
    if (remainingDelay) await new Promise(resolve => setTimeout(resolve, remainingDelay));
    setLoaderStatus('Ready');
    document.body.classList.remove('loading');
    el.startupLoader?.classList.add('is-complete');
    setInterval(fetchBotsData, 5000);
    setInterval(updateLiveGameMetadata, 1000);
  }

  function setLoaderStatus(message) {
    if (el.startupLoaderStatus) el.startupLoaderStatus.textContent = message;
  }

  function setupNavigation() {
    el.navItems.forEach(item => {
      item.addEventListener('click', () => switchTab(item.getAttribute('data-tab')));
    });
  }

  function switchTab(tabId) {
    state.activeTab = tabId;
    el.navItems.forEach(i => i.classList.toggle('active', i.getAttribute('data-tab') === tabId));
    el.tabViews.forEach(v => {
      const active = v.id === `view-${tabId}`;
      v.classList.toggle('active', active);
    });
    if (tabId === 'games') fetchOwnedGames();
    if (tabId === 'twofa') refresh2FA();
  }

  function setupModal() {
    el.btnAddBot.addEventListener('click', () => {
      switchAuthTab('credentials');
      el.modalAddBot.classList.add('active');
    });
    el.btnCloseModal.addEventListener('click', () => { el.modalAddBot.classList.remove('active'); stopQrPolling(); stopCredentialsPolling(); });
    el.modalAddBot.addEventListener('click', e => {
      if (e.target === el.modalAddBot) { el.modalAddBot.classList.remove('active'); stopQrPolling(); stopCredentialsPolling(); }
    });
  }

  function setupRemoveAccountModal() {
    el.btnCancelRemoveAccount?.addEventListener('click', closeRemoveAccountModal);
    el.btnConfirmRemoveAccount?.addEventListener('click', removeAccount);
    el.modalRemoveAccount?.addEventListener('click', event => {
      if (event.target === el.modalRemoveAccount && !state.removeAccountPending) closeRemoveAccountModal();
    });
  }

  function switchAuthTab(targetId) {
    el.authTabBtns.forEach(b => b.classList.toggle('active', b.getAttribute('data-auth-tab') === targetId));
    document.querySelectorAll('.auth-panel').forEach(p => {
      p.style.display = p.id === `auth-tab-${targetId}` ? 'flex' : 'none';
    });
  }

  function setupAuthTabs() {
    el.authTabBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        stopQrPolling();
        switchAuthTab(btn.getAttribute('data-auth-tab'));
      });
    });

    el.btnSubmitCreds.addEventListener('click', async () => {
      const login = document.getElementById('modal-steam-login').value.trim();
      const pass = document.getElementById('modal-steam-password').value;
      if (!login || !pass) { toast('Enter your Steam username and password.', 'error'); return; }
      const name = createInternalBotName(login);
      try {
        el.btnSubmitCreds.disabled = true;
        el.credentialsProgress.style.display = 'flex';
        el.credentialsStatus.textContent = 'Connecting to Steam…';
        el.credentialsHelp.textContent = 'Keep this window open. Tickr will ask for Steam Guard here if Steam requires it.';
        await apiPost(`/Api/Bot/${encodeURIComponent(name)}/Credentials`, { SteamLogin: login, SteamPassword: pass });
        document.getElementById('modal-steam-password').value = '';
        state.credentialsBotName = name;
        el.credentialsStatus.textContent = 'Waiting for Steam confirmation…';
        el.credentialsHelp.textContent = 'Tickr will continue automatically after you approve the request.';
        el.steamMobileApproval.style.display = 'flex';
        state.credentialsPollTimer = setInterval(pollCredentialsLogin, 1000);
        await pollCredentialsLogin();
      } catch (err) {
        toast(`Failed: ${err.message}`, 'error');
        resetCredentialsPanel();
      }
    });
    el.btnSubmitGuardCode?.addEventListener('click', submitGuardCode);
    el.credentialsCode?.addEventListener('keydown', event => { if (event.key === 'Enter') submitGuardCode(); });

    // ── QR login ────────────────────────────────────────────
    el.btnQrStart?.addEventListener('click', startQrLogin);
    el.btnQrCancel?.addEventListener('click', cancelQrLogin);

    // ── .maFile import ──────────────────────────────────────
    el.btnSubmitMafile?.addEventListener('click', async () => {
      const name = createInternalBotName('Authenticator');
      const content = document.getElementById('modal-mafile-content').value.trim();
      if (!content) { toast('Paste your .maFile JSON.', 'error'); return; }
      try {
        await apiPost(`/Api/Bot/${encodeURIComponent(name)}/MaFile`, { maFile: content });
        toast(`.maFile imported for "${name}".`, 'success');
        el.modalAddBot.classList.remove('active');
        fetchBotsData();
      } catch (err) {
        toast(`Failed: ${err.message}`, 'error');
      }
    });
  }

  /* ── QR login flow ────────────────────────────────────── */

  function createInternalBotName(seed) {
    const clean = String(seed || 'Account').replace(/[^A-Za-z0-9_-]/g, '').slice(0, 24) || 'Account';
    let candidate = `Steam_${clean}`;
    let suffix = 2;
    while (state.bots[candidate]) candidate = `Steam_${clean}_${suffix++}`;
    return candidate;
  }

  async function pollCredentialsLogin() {
    if (!state.credentialsBotName) return;
    try {
      const result = await apiGet(`/Api/Bot/${encodeURIComponent(state.credentialsBotName)}`);
      const bot = result?.[state.credentialsBotName];
      if (!bot) return;

      if (bot.IsConnectedAndLoggedOn) {
        const displayName = bot.Nickname || bot.BotConfig?.SteamLogin || 'Steam account';

        if (!bot.BotConfig?.Enabled && !state.credentialsEnabling) {
          state.credentialsEnabling = true;
          el.credentialsStatus.textContent = 'Steam accepted the login. Saving the session…';
          el.credentialsHelp.textContent = 'Tickr is securing the reusable Steam session so future starts do not request another email code.';
          await apiPost(`/Api/Bot/${encodeURIComponent(state.credentialsBotName)}`, { BotConfig: { Enabled: true } });
          return;
        }

        if (state.credentialsEnabling && !bot.BotConfig?.Enabled) return;

        stopCredentialsPolling();
        toast(`${displayName} connected successfully.`, 'success');
        el.modalAddBot.classList.remove('active');
        resetCredentialsPanel();
        await fetchBotsData();
        await fetchOwnedGames(true);
        return;
      }

      const inputTypes = { SteamGuard: 3, TwoFactorAuthentication: 5 };
      const required = Number(bot.RequiredInput) || inputTypes[bot.RequiredInput] || 0;
      if (required === 3 || required === 5) {
        state.credentialsInputType = required;
        el.steamMobileApproval.style.display = 'none';
        el.credentialsCodeGroup.style.display = 'flex';
        el.credentialsCodeLabel.textContent = required === 3 ? 'Steam Guard email code' : 'Steam Guard mobile code';
        el.credentialsStatus.textContent = 'Steam Guard confirmation required';
        el.credentialsHelp.textContent = required === 3 ? 'Enter the 5-character code Steam sent to your email.' : 'Enter the current code from the Steam Mobile app.';
        if (document.activeElement !== el.credentialsCode) el.credentialsCode.focus();
      }
    } catch (error) {
      el.credentialsStatus.textContent = 'Connecting to Steam…';
      el.credentialsHelp.textContent = error.message;
    }
  }

  async function submitGuardCode() {
    const code = el.credentialsCode.value.trim();
    if (!state.credentialsBotName || !state.credentialsInputType || !code) return;
    try {
      el.btnSubmitGuardCode.disabled = true;
      const type = state.credentialsInputType;
      await apiPost(`/Api/Bot/${encodeURIComponent(state.credentialsBotName)}/Input`, { Type: type, Value: code });
      el.credentialsCode.value = '';
      el.credentialsCodeGroup.style.display = 'none';
      el.credentialsStatus.textContent = 'Code submitted. Authenticating with Steam…';
      el.credentialsHelp.textContent = 'Please wait while Steam finishes signing in.';
      state.credentialsInputType = 0;
      await pollCredentialsLogin();
    } catch (error) {
      toast(`Code was not accepted: ${error.message}`, 'error');
    } finally {
      el.btnSubmitGuardCode.disabled = false;
    }
  }

  function stopCredentialsPolling() {
    if (state.credentialsPollTimer) clearInterval(state.credentialsPollTimer);
    state.credentialsPollTimer = null;
  }

  function resetCredentialsPanel() {
    stopCredentialsPolling();
    state.credentialsBotName = null;
    state.credentialsInputType = 0;
    state.credentialsEnabling = false;
    el.btnSubmitCreds.disabled = false;
    el.credentialsProgress.style.display = 'none';
    el.steamMobileApproval.style.display = 'none';
    el.credentialsCodeGroup.style.display = 'none';
    el.credentialsCode.value = '';
  }

  async function startQrLogin() {
    const name = createInternalBotName(`Account_${Date.now().toString(36)}`);

    stopQrPolling();
    state.qrBotName = name;
    state.qrLastChallenge = null;
    state.qrPollFailures = 0;
    state.qrStartedAt = Date.now();

    el.qrMock.textContent = '...';
    el.qrStatus.textContent = 'Initiating QR login session...';
    el.btnQrStart.disabled = true;

    try {
      // Make sure the bot config exists (no credentials needed for QR login)
      await apiPost(`/Api/Bot/${encodeURIComponent(name)}`, { BotConfig: { Enabled: true } });
      await apiPost(`/Api/Bot/${encodeURIComponent(name)}/QrLogin`, {});

      el.btnQrStart.style.display = 'none';
      el.btnQrCancel.style.display = 'block';
      el.qrStatus.textContent = 'Scan the QR code with the Steam mobile app...';

      state.qrPollTimer = setInterval(pollQrLogin, 2000);
      pollQrLogin();
    } catch (err) {
      toast(`Failed to start QR login: ${err.message}`, 'error');
      resetQrPanel();
    }
  }

  async function pollQrLogin() {
    if (!state.qrBotName) return stopQrPolling();
    if (state.qrPolling) return;
    if (Date.now() - state.qrStartedAt > 5 * 60 * 1000) {
      toast('QR login expired. Start a new session and scan the fresh code.', 'error');
      await cancelQrLogin();
      return;
    }

    state.qrPolling = true;

    let res;
    try {
      res = await apiGet(`/Api/Bot/${encodeURIComponent(state.qrBotName)}/QrLogin`);
      state.qrPollFailures = 0;
    } catch (error) {
      state.qrPollFailures++;
      if (state.qrPollFailures >= 5) {
        toast(`QR status is unavailable: ${error.message}`, 'error');
        await cancelQrLogin();
      }
      return;
    } finally {
      state.qrPolling = false;
    }

    const status = res?.StateText || 'Idle';

    if (status === 'AwaitingConfirmation') {
      const url = res?.ChallengeURL;
      if (url && url !== state.qrLastChallenge) {
        state.qrLastChallenge = url;
        renderQrCode(url);
        el.qrStatus.textContent = 'Scan the QR code with the Steam mobile app...';
      }
      return;
    }

    if (status === 'LoggingOn') {
      el.qrStatus.textContent = 'Approved. Connecting Tickr to Steam...';
      el.qrMock.textContent = '✓';
      return;
    }

    if (status === 'Completed') {
      const completedBotName = state.qrBotName;
      stopQrPolling();
      toast(`Account "${completedBotName}" securely connected via Steam QR.`, 'success');
      el.modalAddBot.classList.remove('active');
      resetQrPanel();
      await fetchBotsData();
      await fetchOwnedGames(true);
      return;
    }

    if (status === 'Failed') {
      stopQrPolling();
      toast('QR login failed or timed out. Please try again.', 'error');
      resetQrPanel();
      fetchBotsData();
    }
  }

  async function cancelQrLogin() {
    if (state.qrBotName) {
      try { await apiDelete(`/Api/Bot/${encodeURIComponent(state.qrBotName)}/QrLogin`); } catch { /* ignored */ }
    }
    stopQrPolling();
    resetQrPanel();
  }

  function stopQrPolling() {
    if (state.qrPollTimer) { clearInterval(state.qrPollTimer); state.qrPollTimer = null; }
    state.qrPolling = false;
  }

  function resetQrPanel() {
    stopQrPolling();
    state.qrBotName = null;
    state.qrLastChallenge = null;
    state.qrPollFailures = 0;
    state.qrStartedAt = 0;
    el.qrMock.textContent = 'QR';
    el.qrStatus.textContent = 'Start the login to generate a secure QR code...';
    el.btnQrStart.style.display = 'block';
    el.btnQrStart.disabled = false;
    el.btnQrCancel.style.display = 'none';
  }

  function renderQrCode(text) {
    if (typeof qrcode === 'undefined') return;
    el.qrMock.innerHTML = '';
    const qr = qrcode(0, 'L');
    qr.addData(text);
    qr.make();
    el.qrMock.innerHTML = qr.createImgTag(3, 0);
  }

  /* ── IPC password handling ────────────────────────────── */

  function setupIpcPassword() {
    el.btnSubmitIpcPassword?.addEventListener('click', submitIpcPassword);
    el.ipcPasswordInput?.addEventListener('keydown', e => {
      if (e.key === 'Enter') submitIpcPassword();
    });
  }

  function submitIpcPassword() {
    const pwd = el.ipcPasswordInput.value;
    if (!pwd) { toast('Enter the IPC password.', 'error'); return; }
    sessionStorage.setItem('tickr_ipc_password', pwd);
    el.ipcPasswordInput.value = '';
    state.awaitingPassword = false;
    el.modalIpcPassword.classList.remove('active');
    toast('Password saved for this session.', 'success');
    fetchBotsData();
  }

  function openPasswordModal() {
    if (state.awaitingPassword) return;
    state.awaitingPassword = true;
    el.modalIpcPassword.classList.add('active');
    el.ipcPasswordInput.focus();
  }

  /* ── API ──────────────────────────────────────────────── */

  async function apiFetch(endpoint, options = {}) {
    const url = endpoint.startsWith('http') ? endpoint : state.apiBase + endpoint;
    const headers = { ...(options.headers || {}) };

    const pwd = sessionStorage.getItem('tickr_ipc_password');
    if (pwd) headers['Authentication'] = pwd;

    const res = await fetch(url, { ...options, headers });

    if (res.status === 401) {
      sessionStorage.removeItem('tickr_ipc_password');
      openPasswordModal();
      throw new Error('IPC password required');
    }

    return res;
  }

  async function apiGet(endpoint) {
    const res = await apiFetch(endpoint, {
      headers: { 'Content-Type': 'application/json' }
    });
    const data = await readApiResponse(res);
    if (!res.ok || data?.Success === false) throw new Error(data?.Message || `HTTP ${res.status}`);
    return data.Result ?? data;
  }

  async function apiPost(endpoint, body) {
    const res = await apiFetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    const data = await readApiResponse(res);
    if (!res.ok || data?.Success === false) throw new Error(data?.Message || `HTTP ${res.status}`);
    return data.Result ?? data;
  }

  async function apiDelete(endpoint) {
    const res = await apiFetch(endpoint, { method: 'DELETE' });
    const data = await readApiResponse(res);
    if (!res.ok || data?.Success === false) throw new Error(data?.Message || `HTTP ${res.status}`);
    return data.Result ?? data;
  }

  async function readApiResponse(response) {
    const contentType = response.headers.get('content-type') || '';
    if (!contentType.includes('application/json')) return null;
    try { return await response.json(); } catch { return null; }
  }

  async function cmd(command) {
    log(`› ${command}`);
    try {
      const res = await apiPost('/Api/Command', { Command: command });
      log(res || 'Done.');
      toast('Command sent.', 'success');
      fetchBotsData();
      return res;
    } catch (err) {
      log(`Error: ${err.message}`, 'error');
      toast(err.message, 'error');
    }
  }

  /* ── Fetch bots ───────────────────────────────────────── */

  async function fetchBotsData() {
    try {
      const bots = await apiGet('/Api/Bot/%40ALL');
      if (bots && typeof bots === 'object') {
        // Disabled profiles are unfinished/archived configurations, not active accounts.
        // Keep them out of account totals and library filters while still allowing the
        // dedicated credentials poller to finish an explicitly initiated login.
        state.bots = Object.fromEntries(Object.entries(bots).filter(([, bot]) => bot.IsConnectedAndLoggedOn || bot.BotConfig?.Enabled));
        setConnected(true);
      }
    } catch {
      setConnected(false);
      // Remove dummy data, if it fails, it fails (real app behavior)
      state.bots = {};
    }

    renderDashboard();
    updateDropdowns();
    if (state.activeTab === 'games') {
      updateRunningGameStates();
      fetchOwnedGames();
    }
  }

  function setConnected(online) {
    el.sidebarConnDot.className = 'status-indicator ' + (online ? 'online' : 'offline');
    el.sidebarConnLabel.textContent = online ? 'Connected' : 'Offline';
  }

  /* ── Dashboard ────────────────────────────────────────── */

  function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>'"]/g, char => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
    })[char]);
  }

  function formatPlaytime(minutes) {
    const totalMinutes = Math.max(0, Math.floor(Number(minutes) || 0));
    if (totalMinutes < 60) return `${totalMinutes} min`;
    const hours = Math.floor(totalMinutes / 60);
    const remainingMinutes = totalMinutes % 60;
    return remainingMinutes ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }

  function gameRuntimeKey(botName, appID) {
    return `${encodeURIComponent(botName)}:${appID}`;
  }

  function accrueLivePlaytime(metadata, now = Date.now()) {
    if (metadata.running) metadata.playtimeMs += Math.max(0, now - metadata.lastTick);
    metadata.lastTick = now;
  }

  function ensureLiveGameMetadata(botName, game, now = Date.now()) {
    const key = gameRuntimeKey(botName, game.AppID);
    const serverPlaytimeMs = Math.max(0, Number(game.PlaytimeMinutes) || 0) * 60000;
    const serverLastPlayed = game.LastPlayedAt ? new Date(game.LastPlayedAt).getTime() : 0;
    const running = (state.bots[botName]?.HourBoostedAppIDs || []).includes(game.AppID);
    let metadata = state.liveGameMetadata.get(key);

    if (!metadata) {
      metadata = { playtimeMs: serverPlaytimeMs, lastTick: now, lastPlayedAt: serverLastPlayed, running: false };
      state.liveGameMetadata.set(key, metadata);
    } else {
      accrueLivePlaytime(metadata, now);
      metadata.playtimeMs = Math.max(metadata.playtimeMs, serverPlaytimeMs);
      metadata.lastPlayedAt = Math.max(metadata.lastPlayedAt || 0, serverLastPlayed);
    }

    if (running && !metadata.running) {
      metadata.running = true;
      metadata.lastTick = now;
      metadata.lastPlayedAt = now;
    } else if (!running && metadata.running) {
      accrueLivePlaytime(metadata, now);
      metadata.running = false;
      metadata.lastPlayedAt = now;
    }

    return metadata;
  }

  function syncLiveGameMetadata() {
    const now = Date.now();
    Object.entries(state.games).forEach(([botName, games]) => {
      games.forEach(game => ensureLiveGameMetadata(botName, game, now));
    });
  }

  function formatLivePlaytime(metadata) {
    if (!metadata?.running) return formatPlaytime((metadata?.playtimeMs || 0) / 60000);
    const totalSeconds = Math.max(0, Math.floor(metadata.playtimeMs / 1000));
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return hours > 0
      ? `${hours}h ${String(minutes).padStart(2, '0')}m ${String(seconds).padStart(2, '0')}s`
      : `${minutes}m ${String(seconds).padStart(2, '0')}s`;
  }

  function formatLastPlayed(metadata) {
    if (metadata?.running) return 'Now';
    if (!metadata?.lastPlayedAt) return 'Never';
    return new Date(metadata.lastPlayedAt).toLocaleDateString('en-GB', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  function renderDashboard() {
    const entries = Object.entries(state.bots);
    el.statBotsCount.textContent = entries.length;

    let runningGames = 0;
    let totalWallet = 0;
    entries.forEach(([, b]) => {
      runningGames += (b.HourBoostedAppIDs || []).length;
      if (b.WalletBalance) {
        const num = parseFloat(b.WalletBalance);
        if (!isNaN(num)) totalWallet += num;
      }
    });

    el.statCardsRemaining.textContent = runningGames;
    el.statWalletBalance.textContent = `${totalWallet.toFixed(2)} €`;

    if (entries.length === 0) {
      el.botGridContainer.innerHTML = `
        <div class="empty-cta">
          <div class="empty-icon">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/></svg>
            <div class="empty-ring"></div>
            <div class="empty-ring empty-ring-2"></div>
          </div>
          <div class="empty-label">No Steam accounts</div>
          <div class="empty-sub">Connect an account, select games, and start tracking playtime.</div>
          <button class="action-btn accent-btn" onclick="document.getElementById('modal-add-bot').classList.add('active')">Add Steam account</button>
        </div>`;
      return;
    }

    el.botGridContainer.innerHTML = entries.map(([name, bot], idx) => {
      const online = bot.IsConnectedAndLoggedOn;
      const runningIDs = bot.HourBoostedAppIDs || [];
      const farming = runningIDs.length > 0;
      const gamesCount = runningIDs.length;

      let pillClass = 'pill-offline', pillText = 'Offline';
      let dotClass = '';
      if (online) {
        if (farming)      { pillClass = 'pill-farming'; pillText = 'Running'; dotClass = 'farming'; }
        else              { pillClass = 'pill-online';  pillText = 'Ready';  dotClass = 'online'; }
      }

      const knownGames = state.games[name] || [];
      const runningNames = runningIDs.map(appID => knownGames.find(game => game.AppID === appID)?.Name || `App ${appID}`);
      const gameName = farming ? `${runningNames.slice(0, 2).join(', ')}${runningNames.length > 2 ? ` and ${runningNames.length - 2} more` : ''}` : 'Waiting for game selection';

      const safeName = escapeHtml(bot.Nickname || (online ? 'Steam account' : 'Connecting…'));
      const safeGameName = escapeHtml(gameName);
      const safeAvatar = escapeHtml(bot.AvatarUrl || 'tickr-logo.jpg');

      return `
        <div class="bot-card" style="--i:${idx}">
          <div class="bot-header">
            <div class="bot-avatar-wrap">
              <img class="bot-avatar" src="${safeAvatar}" alt="${safeName}" onerror="this.src='tickr-logo.jpg'">
              <div class="bot-status-dot ${dotClass}"></div>
            </div>
            <div class="bot-meta">
              <div class="bot-name">${safeName}</div>
              <div class="bot-status-row">
                <span class="status-pill ${pillClass}">${pillText}</span>
                <span class="bot-game-label">${safeGameName}</span>
              </div>
            </div>
            <div class="bot-header-tools">
              <div style="font-family:var(--mono);font-size:10.5px;color:var(--text-3);white-space:nowrap">${gamesCount} running</div>
              <button class="bot-remove-button" data-bot-action="remove" data-bot="${encodeURIComponent(name)}" title="Remove account" aria-label="Remove ${safeName}">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18"/><path d="M8 6V4h8v2"/><path d="M19 6l-1 14H6L5 6"/><path d="M10 11v5M14 11v5"/></svg>
              </button>
            </div>
          </div>

          <div class="bot-progress">
            <div class="progress-meta">
              <span>${safeGameName}</span>
              <span>${farming ? 'Playtime is increasing' : '—'}</span>
            </div>
          </div>

          <div class="bot-actions">
            <button class="action-btn accent-btn" data-bot-action="choose" data-bot="${encodeURIComponent(name)}">Choose games</button>
            <button class="action-btn danger-btn" data-bot-action="stop" data-bot="${encodeURIComponent(name)}" ${farming ? '' : 'disabled'}>Stop</button>
          </div>
        </div>`;
    }).join('');
  }

  /* ── Games Grid ───────────────────────────────────────── */

  async function fetchOwnedGames(force = false) {
    const onlineBots = Object.entries(state.bots).filter(([, bot]) => bot.IsConnectedAndLoggedOn);
    const cacheIsFresh = Date.now() - state.gamesFetchedAt < 60 * 1000;

    if (state.gamesLoading || (!force && cacheIsFresh && onlineBots.every(([name]) => Array.isArray(state.games[name])))) {
      updateRunningGameStates();
      return;
    }

    if (onlineBots.length === 0) {
      state.games = {};
      state.gamesErrors = {};
      renderGamesGrid();
      return;
    }

    state.gamesLoading = true;
    const previousLibraryShape = Object.entries(state.games)
      .flatMap(([botName, games]) => games.map(game => gameRuntimeKey(botName, game.AppID)))
      .sort()
      .join('|');
    const hadLibrary = Object.keys(state.games).length > 0;

    el.gamesLibraryStatus.textContent = hadLibrary ? 'Syncing playtime with Steam…' : 'Loading your Steam library…';

    const activeNames = new Set(onlineBots.map(([name]) => name));
    Object.keys(state.games).filter(name => !activeNames.has(name)).forEach(name => delete state.games[name]);
    Object.keys(state.gamesErrors).filter(name => !activeNames.has(name)).forEach(name => delete state.gamesErrors[name]);

    await Promise.all(onlineBots.map(async ([botName]) => {
      try {
        const games = await apiGet(`/Api/Bot/${encodeURIComponent(botName)}/Games`);
        state.games[botName] = Array.isArray(games) ? games : [];
        delete state.gamesErrors[botName];
      } catch (error) {
        state.gamesErrors[botName] = error.message;
      }
    }));

    state.gamesFetchedAt = Date.now();
    state.gamesLoading = false;
    syncLiveGameMetadata();

    const nextLibraryShape = Object.entries(state.games)
      .flatMap(([botName, games]) => games.map(game => gameRuntimeKey(botName, game.AppID)))
      .sort()
      .join('|');

    if (!hadLibrary || previousLibraryShape !== nextLibraryShape) renderGamesGrid();
    else updateLiveGameMetadata();
  }

  function renderGamesGrid() {
    let all = [];
    Object.entries(state.games).forEach(([botName, games]) => {
      const runningIDs = new Set(state.bots[botName]?.HourBoostedAppIDs || []);
      games.forEach(game => all.push({ ...game, botName, running: runningIDs.has(game.AppID) }));
    });

    const search = (el.gamesSearchInput?.value || '').toLowerCase();
    const filterBot = el.gamesBotFilter?.value;

    const sort = el.gamesSort?.value || 'name';
    const filtered = all.filter(g =>
      ((g.Name || '').toLowerCase().includes(search) || String(g.AppID).includes(search)) &&
      (filterBot === 'all' || g.botName === filterBot)
    );

    filtered.sort((a, b) => {
      if (sort === 'running') return Number(b.running) - Number(a.running) || (a.Name || '').localeCompare(b.Name || '');
      if (sort === 'playtime') return (b.PlaytimeMinutes || 0) - (a.PlaytimeMinutes || 0) || (a.Name || '').localeCompare(b.Name || '');
      if (sort === 'recent') return new Date(b.LastPlayedAt || 0) - new Date(a.LastPlayedAt || 0) || (a.Name || '').localeCompare(b.Name || '');
      return (a.Name || '').localeCompare(b.Name || '', undefined, { sensitivity: 'base' });
    });

    const errorEntries = Object.entries(state.gamesErrors);
    el.gamesLibraryStatus.textContent = state.gamesLoading
      ? 'Loading your Steam library…'
      : `${all.length.toLocaleString()} games · ${Object.keys(state.games).length} connected account(s)${errorEntries.length ? ` · ${errorEntries.length} sync error(s)` : ''}`;

    const validKeys = new Set(all.map(game => `${game.botName}:${game.AppID}`));
    [...state.selectedGames].filter(key => !validKeys.has(key)).forEach(key => state.selectedGames.delete(key));
    updateGamesSelectionControls();

    if (filtered.length === 0) {
      const message = Object.keys(state.bots).length === 0
        ? 'Connect a Steam account to load your library.'
        : all.length === 0
          ? 'The library is currently unavailable. Check the Steam account connection.'
          : 'No games match the current filters.';
      el.gamesGridContainer.innerHTML = `<div class="empty-cta" style="grid-column:1/-1"><div class="empty-sub">${escapeHtml(message)}</div></div>`;
      return;
    }

    el.gamesGridContainer.innerHTML = filtered.map((g, idx) => {
      const title = g.Name || `App ${g.AppID}`;
      const metadata = ensureLiveGameMetadata(g.botName, g);
      const lastPlayed = formatLastPlayed(metadata);
      const selectionKey = `${g.botName}:${g.AppID}`;
      const selected = state.selectedGames.has(selectionKey);
      const running = g.running;

      return `
      <div class="game-card selectable${selected ? ' selected' : ''}${running ? ' running' : ''}" style="--i:${Math.min(idx, 20)}" data-selectable="true" data-bot="${encodeURIComponent(g.botName)}" data-appid="${g.AppID}">
        <img class="game-banner" loading="lazy" src="https://cdn.cloudflare.steamstatic.com/steam/apps/${g.AppID}/header.jpg" onerror="this.onerror=null;this.src='tickr-logo.jpg'" alt="${escapeHtml(title)}">
        <div class="game-body">
          <div class="game-select-row">
            <label class="game-select-label"><input type="checkbox" ${selected ? 'checked' : ''} tabindex="-1"> Select</label>
            <span class="game-running-badge${running ? ' active' : ''}">${running ? '● Running' : 'Not running'}</span>
          </div>
          <div class="game-title">${escapeHtml(title)}</div>
          <div class="game-stats-row">
            <span>${escapeHtml(state.bots[g.botName]?.Nickname || 'Steam account')}</span>
            <span class="game-playtime">${formatLivePlaytime(metadata)}</span>
          </div>
          <div class="game-stats-row"><span>Last played</span><span class="game-last-played">${escapeHtml(lastPlayed)}</span></div>
          <div class="game-hint">${running ? 'Steam sees this game as active — playtime is increasing.' : 'Select this game, then click “Start selected”.'}</div>
        </div>
      </div>`;
    }).join('');
  }

  function updateGamesSelectionControls() {
    const count = state.selectedGames.size;
    const running = Object.values(state.bots).reduce((total, bot) => total + (bot.HourBoostedAppIDs || []).length, 0);
    el.gamesSelectionCount.textContent = `Selected: ${count} · Running: ${running}`;
    el.btnStartSelected.disabled = count === 0 || state.farmingActionPending;
    el.btnStopFarming.disabled = state.farmingActionPending || running === 0;
  }

  function updateRunningGameStates() {
    syncLiveGameMetadata();
    document.querySelectorAll('.game-card[data-bot][data-appid]').forEach(card => {
      const botName = decodeURIComponent(card.dataset.bot || '');
      const appID = Number(card.dataset.appid);
      const running = (state.bots[botName]?.HourBoostedAppIDs || []).includes(appID);
      card.classList.toggle('running', running);
      const badge = card.querySelector('.game-running-badge');
      if (badge) {
        badge.classList.toggle('active', running);
        badge.textContent = running ? '● Running' : 'Not running';
      }
      const hint = card.querySelector('.game-hint');
      if (hint) hint.textContent = running ? 'Steam sees this game as active — playtime is increasing.' : 'Select this game, then click “Start selected”.';
    });
    updateLiveGameMetadata();
    updateGamesSelectionControls();
  }

  function updateLiveGameMetadata() {
    const now = Date.now();
    Object.entries(state.games).forEach(([botName, games]) => {
      games.forEach(game => ensureLiveGameMetadata(botName, game, now));
    });

    document.querySelectorAll('.game-card[data-bot][data-appid]').forEach(card => {
      const botName = decodeURIComponent(card.dataset.bot || '');
      const appID = Number(card.dataset.appid);
      const game = state.games[botName]?.find(item => item.AppID === appID);
      if (!game) return;
      const metadata = ensureLiveGameMetadata(botName, game, now);
      const playtime = card.querySelector('.game-playtime');
      const lastPlayed = card.querySelector('.game-last-played');
      if (playtime) playtime.textContent = formatLivePlaytime(metadata);
      if (lastPlayed) lastPlayed.textContent = formatLastPlayed(metadata);
    });
  }

  /* ── 2FA ──────────────────────────────────────────────── */

  function setup2FA() {
    document.getElementById('btn-copy-2fa').addEventListener('click', () => {
      const code = el.twofaCodeVal.textContent.replace(/[^A-Z0-9]/gi, '');
      if (code) { navigator.clipboard.writeText(code); toast('Code copied.', 'success'); }
    });
    document.getElementById('btn-2fa-accept-all').addEventListener('click', () => cmd('2faok @ALL'));
    document.getElementById('btn-2fa-reject-all').addEventListener('click', () => cmd('2fano @ALL'));

    el.twofaBotSelect?.addEventListener('change', refresh2FA);

    setInterval(() => {
      state.twoFaCountdown--;
      if (state.twoFaCountdown <= 0) { state.twoFaCountdown = 30; refresh2FA(); }
      el.twofaCountdown.textContent = `${state.twoFaCountdown}s`;
      if (el.twofaTimerFill) el.twofaTimerFill.style.width = `${(state.twoFaCountdown / 30) * 100}%`;
    }, 1000);
  }

  async function refresh2FA() {
    const bot = el.twofaBotSelect?.value || Object.keys(state.bots)[0];
    if (!bot) {
      el.twofaCodeVal.textContent = "— — — — —";
      return;
    }
    try {
      const res = await apiGet(`/Api/TwoFactorAuthentication/${encodeURIComponent(bot)}`);
      if (res?.Token) el.twofaCodeVal.textContent = res.Token;
    } catch {
      // If unable to fetch, just show dashes
      el.twofaCodeVal.textContent = "— — — — —";
    }
  }

  /* ── Actions ──────────────────────────────────────────── */

  function setupActions() {
    document.getElementById('btn-refresh-data').addEventListener('click', async () => {
      await fetchBotsData();
      if (state.activeTab === 'games') await fetchOwnedGames(true);
      toast('Data refreshed.', 'success');
    });

    el.btnStartSelected?.addEventListener('click', startSelectedGames);
    el.btnStopFarming?.addEventListener('click', stopVisibleFarming);

    el.gamesSearchInput?.addEventListener('input', renderGamesGrid);
    el.gamesBotFilter?.addEventListener('change', renderGamesGrid);
    el.gamesSort?.addEventListener('change', renderGamesGrid);

    el.botGridContainer.addEventListener('click', event => {
      const button = event.target.closest('button[data-bot-action]');
      if (!button) return;
      const botName = decodeURIComponent(button.dataset.bot || '');
      const action = button.dataset.botAction;
      if (!botName) return;
      if (action === 'choose') {
        switchTab('games');
        if (el.gamesBotFilter) el.gamesBotFilter.value = botName;
        renderGamesGrid();
      } else if (action === 'stop') {
        stopFarming([botName]);
      } else if (action === 'remove') {
        openRemoveAccountModal(botName);
      }
    });

    el.gamesGridContainer.addEventListener('click', event => {
      const card = event.target.closest('.game-card[data-selectable="true"]');
      if (!card) return;
      const appID = Number(card.dataset.appid);
      if (!Number.isInteger(appID) || appID <= 0) return;
      const botName = decodeURIComponent(card.dataset.bot || '');
      if (!botName) return;
      const key = `${botName}:${appID}`;
      if (state.selectedGames.has(key)) {
        state.selectedGames.delete(key);
      } else {
        if (state.selectedGames.size >= 32) {
          toast('Steam allows no more than 32 games to run at once.', 'error');
          return;
        }
        state.selectedGames.add(key);
      }
      const selected = state.selectedGames.has(key);
      card.classList.toggle('selected', selected);
      const checkbox = card.querySelector('input[type="checkbox"]');
      if (checkbox) checkbox.checked = selected;
      updateGamesSelectionControls();
    });

    el.btnRedeemKeys.addEventListener('click', async () => {
      const keys = el.redeemerKeysInput.value.trim();
      if (!keys) { toast('Enter CD-keys to redeem.', 'error'); return; }
      const bot = el.redeemerBotSelect.value;
      el.redeemerResultsBox.style.display = 'flex';
      el.redeemerResultsList.textContent = 'Activating...';
      const res = await cmd(`redeem ${bot} ${keys.replace(/\n/g, ',')}`);
      if (res) el.redeemerResultsList.textContent = String(res);
    });

    el.btnClearKeys.addEventListener('click', () => {
      el.redeemerKeysInput.value = '';
      el.redeemerResultsBox.style.display = 'none';
    });
  }

  async function startSelectedGames() {
    if (state.selectedGames.size === 0 || state.farmingActionPending) return;

    const selections = new Map();
    state.selectedGames.forEach(key => {
      const separator = key.lastIndexOf(':');
      const botName = key.slice(0, separator);
      const appID = Number(key.slice(separator + 1));
      if (!selections.has(botName)) selections.set(botName, []);
      selections.get(botName).push(appID);
    });

    state.farmingActionPending = true;
    updateGamesSelectionControls();

    try {
      await Promise.all([...selections].map(([botName, appIDs]) =>
        apiPost(`/Api/Bot/${encodeURIComponent(botName)}/Farming/Start`, { AppIDs: appIDs })
      ));
      state.selectedGames.clear();
      document.querySelectorAll('.game-card.selected').forEach(card => {
        card.classList.remove('selected');
        const checkbox = card.querySelector('input[type="checkbox"]');
        if (checkbox) checkbox.checked = false;
      });
      toast('Selected games are running. Steam is now tracking playtime.', 'success');
      await fetchBotsData();
      updateRunningGameStates();
    } catch (error) {
      toast(`Could not start the games: ${error.message}`, 'error');
    } finally {
      state.farmingActionPending = false;
      updateGamesSelectionControls();
    }
  }

  async function stopVisibleFarming() {
    const filteredBot = el.gamesBotFilter?.value;
    const botNames = filteredBot && filteredBot !== 'all'
      ? [filteredBot]
      : Object.entries(state.bots).filter(([, bot]) => (bot.HourBoostedAppIDs || []).length > 0).map(([name]) => name);
    await stopFarming(botNames);
  }

  async function stopFarming(botNames) {
    if (!botNames.length || state.farmingActionPending) return;
    state.farmingActionPending = true;
    updateGamesSelectionControls();
    try {
      await Promise.all(botNames.map(botName => apiPost(`/Api/Bot/${encodeURIComponent(botName)}/Farming/Stop`, {})));
      toast('Running games stopped.', 'success');
      await fetchBotsData();
    } catch (error) {
      toast(`Could not stop the games: ${error.message}`, 'error');
    } finally {
      state.farmingActionPending = false;
      updateGamesSelectionControls();
    }
  }

  function openRemoveAccountModal(botName) {
    const bot = state.bots[botName];
    if (!bot || state.removeAccountPending) return;
    state.removeAccountBotName = botName;
    const displayName = bot.Nickname || bot.BotConfig?.SteamLogin || 'this Steam account';
    el.removeAccountMessage.textContent = `Remove “${displayName}” from Tickr and delete its saved local session?`;
    el.modalRemoveAccount.classList.add('active');
  }

  function closeRemoveAccountModal() {
    if (state.removeAccountPending) return;
    state.removeAccountBotName = null;
    el.modalRemoveAccount?.classList.remove('active');
  }

  async function removeAccount() {
    const botName = state.removeAccountBotName;
    if (!botName || state.removeAccountPending) return;
    const displayName = state.bots[botName]?.Nickname || 'Steam account';
    state.removeAccountPending = true;
    el.btnCancelRemoveAccount.disabled = true;
    el.btnConfirmRemoveAccount.disabled = true;
    el.btnConfirmRemoveAccount.textContent = 'Removing…';

    try {
      await apiDelete(`/Api/Bot/${encodeURIComponent(botName)}`);
      delete state.bots[botName];
      delete state.games[botName];
      delete state.gamesErrors[botName];
      [...state.selectedGames].filter(key => key.startsWith(`${botName}:`)).forEach(key => state.selectedGames.delete(key));
      const runtimePrefix = `${encodeURIComponent(botName)}:`;
      [...state.liveGameMetadata.keys()].filter(key => key.startsWith(runtimePrefix)).forEach(key => state.liveGameMetadata.delete(key));
      el.modalRemoveAccount.classList.remove('active');
      state.removeAccountBotName = null;
      renderDashboard();
      updateDropdowns();
      renderGamesGrid();
      toast(`${displayName} was removed from Tickr.`, 'success');
      await fetchBotsData();
    } catch (error) {
      toast(`Could not remove the account: ${error.message}`, 'error');
    } finally {
      state.removeAccountPending = false;
      el.btnCancelRemoveAccount.disabled = false;
      el.btnConfirmRemoveAccount.disabled = false;
      el.btnConfirmRemoveAccount.textContent = 'Remove account';
    }
  }

  /* ── Terminal ─────────────────────────────────────────── */

  function setupTerminal() {
    el.terminalInput.addEventListener('keydown', async e => {
      if (e.key === 'Enter') {
        const command = el.terminalInput.value.trim();
        if (command) { el.terminalInput.value = ''; await cmd(command); }
      }
    });
    el.btnClearTerminal.addEventListener('click', () => {
      el.terminalLogs.innerHTML = '';
      log('[Tickr] Log cleared.');
    });
  }

  function log(msg, type = 'info') {
    const entry = document.createElement('div');
    entry.className = `log-entry log-${type}`;
    const t = new Date().toLocaleTimeString('en-GB', { hour12: false });
    entry.textContent = `[${t}] ${msg}`;
    el.terminalLogs.appendChild(entry);
    el.terminalLogs.scrollTop = el.terminalLogs.scrollHeight;
  }

  function updateDropdowns() {
    const bots = Object.keys(state.bots);
    const opts = bots.map(b => `<option value="${escapeHtml(b)}">${escapeHtml(state.bots[b]?.Nickname || (state.bots[b]?.IsConnectedAndLoggedOn ? 'Steam account' : 'Connecting…'))}</option>`).join('');
    const updateSelect = (select, html, fallback) => {
      if (!select) return;
      const previous = select.value;
      select.innerHTML = html;
      select.value = [...select.options].some(option => option.value === previous) ? previous : fallback;
    };

    updateSelect(el.gamesBotFilter, `<option value="all">All accounts</option>${opts}`, 'all');
    updateSelect(el.twofaBotSelect, opts || `<option value="">No accounts</option>`, bots[0] || '');
    updateSelect(el.redeemerBotSelect, `<option value="@ALL">All accounts</option>${opts}`, '@ALL');
  }

  /* ── Toast ────────────────────────────────────────────── */

  function toast(message, type = 'info') {
    const t = document.createElement('div');
    t.className = `toast${type === 'error' ? ' toast-error' : type === 'success' ? ' toast-success' : ''}`;
    t.textContent = message;
    el.toastContainer.appendChild(t);
    setTimeout(() => { t.style.opacity = '0'; setTimeout(() => t.remove(), 300); }, 3200);
  }

  /* ── Public API ───────────────────────────────────────── */

  window.T = {
    farm:  b => cmd(`farm ${b}`),
    pause: b => cmd(`pause ${b}`),
    loot:  b => cmd(`loot ${b}`),
    twofa: b => { switchTab('twofa'); if (el.twofaBotSelect) el.twofaBotSelect.value = b; refresh2FA(); }
  };

  window.cmd = cmd;

  /* ── Ripple effect ────────────────────────────────────── */

  function setupRipple() {
    document.addEventListener('click', e => {
      const btn = e.target.closest('.action-btn, .btn-add-account, .nav-item');
      if (!btn) return;
      const rect = btn.getBoundingClientRect();
      const ripple = document.createElement('span');
      ripple.className = 'ripple-effect';
      const size = Math.max(rect.width, rect.height) * 1.4;
      ripple.style.width = ripple.style.height = size + 'px';
      ripple.style.left = (e.clientX - rect.left - size / 2) + 'px';
      ripple.style.top = (e.clientY - rect.top - size / 2) + 'px';
      btn.appendChild(ripple);
      ripple.addEventListener('animationend', () => ripple.remove());
    });
  }

  /* ── Grain overlay (canvas noise) ────────────────────── */

  function setupGrainOverlay() {
    const canvas = document.createElement('canvas');
    const sz = 128;
    canvas.width = sz;
    canvas.height = sz;
    const ctx = canvas.getContext('2d');
    const img = ctx.createImageData(sz, sz);
    for (let i = 0; i < img.data.length; i += 4) {
      const v = Math.random() * 255;
      img.data[i] = v;
      img.data[i + 1] = v;
      img.data[i + 2] = v;
      img.data[i + 3] = 10;
    }
    ctx.putImageData(img, 0, 0);
    const overlay = document.createElement('div');
    overlay.className = 'grain-overlay';
    overlay.style.backgroundImage = `url(${canvas.toDataURL('image/png')})`;
    overlay.style.backgroundRepeat = 'repeat';
    overlay.style.backgroundSize = '128px 128px';
    overlay.style.opacity = '0.4';
    document.body.appendChild(overlay);
  }

  window.addEventListener('DOMContentLoaded', init);
})();
