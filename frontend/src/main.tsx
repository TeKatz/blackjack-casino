import { useCallback, useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  availableActions, browserPreview, sendAction, sendCommand, subscribeToHost,
} from './bridge/protocol';
import type { GameAction, ManorSnapshot } from './bridge/protocol';
import { ManorStage } from './graphics/ManorStage';
import { ManorRunMap } from './graphics/ManorRunMap';
import { ManorRoomScene } from './graphics/ManorRoomScene';
import { ManorHelp } from './ManorHelp';
import { localizeFeature, localizeRunText, t } from './i18n';
import type { ManorLanguage } from './i18n';
import { ghostBand, ghostBandLabel } from './graphics/manorRunUi';
import { ManorAudio } from './audio/manorAudio';
import { localizeMoment, runCue, runMoment, spinCue, spinMoment } from './graphics/manorMoments';
import type { ManorMoment } from './graphics/manorMoments';
import type { ReelSpeed } from './graphics/reelTiming';
import type { RendererDiagnostics } from './graphics/rendererDiagnostics';
import './styles.css';

const embedded = Boolean(window.chrome?.webview);
const defaultBet = (formatted: string): string =>
  formatted.replace(/[^0-9.]/g, '').split('.')[0] || '25';

function ManorApp() {
  const [snapshot, setSnapshot] = useState<ManorSnapshot>(browserPreview);
  const [languageOverride, setLanguageOverride] = useState<ManorLanguage | null>(null);
  const [helpOpen, setHelpOpen] = useState(false);
  const closeHelp = useCallback(() => setHelpOpen(false), []);
  const [connected, setConnected] = useState(!embedded);
  const [error, setError] = useState<string | null>(null);
  const [fatal, setFatal] = useState(false);
  const [pending, setPending] = useState<string | null>(null);
  const pendingRef = useRef<string | null>(null);
  const pendingTypeRef = useRef<GameAction | null>(null);
  const acceptedRevisionRef = useRef('0');
  const [spinEpoch, setSpinEpoch] = useState(0);
  const [animating, setAnimating] = useState(false);
  const [reelSpeed, setReelSpeed] = useState<ReelSpeed>('normal');
  const [bet, setBet] = useState('25');
  const [ambient, setAmbient] = useState(true);
  const [lowMotion, setLowMotion] = useState(
    window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const [confirmAuto, setConfirmAuto] = useState(false);
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [ambienceEnabled, setAmbienceEnabled] = useState(false);
  const [volume, setVolume] = useState(35);
  const [showDiagnostics, setShowDiagnostics] = useState(false);
  const [renderMetrics, setRenderMetrics] = useState<RendererDiagnostics | null>(null);
  const [moment, setMoment] = useState<ManorMoment | null>(null);
  const audio = useRef<ManorAudio | null>(null);
  if (audio.current === null) audio.current = new ManorAudio();
  useEffect(() => () => { audio.current?.dispose(); }, []);
  const presentation = useRef({ onSpin: (result: ManorSnapshot) => {
    setMoment(spinMoment(result));
    const cue = spinCue(result);
    if (cue) audio.current?.play(cue);
  }, onReel: (_index: number) => { audio.current?.play('reel'); } });
  presentation.current.onSpin = (result: ManorSnapshot) => {
    setMoment(spinMoment(result));
    const cue = spinCue(result);
    if (cue) audio.current?.play(cue);
  };

  useEffect(() => {
    const applySnapshot = (next: ManorSnapshot) => {
      // Ref is synchronous: compare against the last accepted C# revision even
      // when React batches two WebView2 responses into the same render.
      if (BigInt(next.revision) < BigInt(acceptedRevisionRef.current)) return;
      acceptedRevisionRef.current = next.revision;
      setSnapshot(next);
      setBet(defaultBet(next.selectedBet));
      setConnected(true);
    };
    const stop = subscribeToHost(message => {
      if (message.type === 'snapshot') {
        const beforeRevision = acceptedRevisionRef.current;
        applySnapshot(message.payload);
        if ((pendingRef.current === message.requestId || pendingRef.current === 'recovering') &&
            BigInt(message.payload.revision) > BigInt(beforeRevision)) {
          const type = pendingTypeRef.current;
          if (type?.startsWith('spin.')) {
            setSpinEpoch(previous => previous + 1);
            audio.current?.play('spin');
          } else if (type?.startsWith('run.')) {
            setMoment(runMoment(message.payload, type));
            audio.current?.play(runCue(message.payload, type));
          }
        }
        // An explicit recovery snapshot can reconcile a lost response after a timeout.
        if (pendingRef.current === message.requestId || pendingRef.current === 'recovering') {
          pendingRef.current = null;
          setPending(null);
          pendingTypeRef.current = null;
        }
        setError(null);
      } else {
        applySnapshot(message.payload.snapshot);
        if (pendingRef.current === message.requestId) {
          pendingRef.current = null;
          setPending(null);
          pendingTypeRef.current = null;
        }
        setError(message.payload.message);
        if (message.payload.fatal) setFatal(true);
      }
    });
    sendCommand('client.ready');
    return stop;
  }, []);

  const actions = availableActions(snapshot);
  const motionOff = lowMotion || !ambient || !snapshot.animationsEnabled;
  const disabled = !embedded || !connected || pending !== null || fatal || animating;
  const validBet = /^[0-9]{1,4}$/.test(bet) && Number(bet) >= 1 && Number(bet) <= 1000;
  const run = snapshot.run;
  const language: ManorLanguage = languageOverride ?? snapshot.language;
  const threat = run ? ghostBand(run.ghostClue) : null;
  const visibleMoment = moment ? localizeMoment(moment, language) : null;
  useEffect(() => { document.documentElement.lang = language; }, [language]);
  useEffect(() => {
    audio.current?.setHostPolicy(snapshot.globalSoundEnabled, snapshot.globalMusicEnabled,
      snapshot.globalMusicVolume);
  }, [snapshot.globalSoundEnabled, snapshot.globalMusicEnabled, snapshot.globalMusicVolume]);

  function act(type: GameAction, extras: { bet?: string; room?: number } = {}) {
    // Immediate ref guard prevents two same-frame clicks from placing two wagers.
    if (disabled || pendingRef.current !== null || !actions.includes(type)) return;
    if (type === 'spin.paid' && !validBet) {
      setError(t(language, 'invalidStake'));
      return;
    }
    setConfirmAuto(false);
    setMoment(null);
    setError(null);
    // The first actual button press unlocks Web Audio in WebView2.
    audio.current?.arm();
    const requestId = sendAction(type, { expectedRevision: snapshot.revision, ...extras });
    pendingRef.current = requestId;
    pendingTypeRef.current = type;
    setPending(requestId);
    // Do not resend a timed-out wager. Ask the C# host for its committed state.
    window.setTimeout(() => {
      if (pendingRef.current === requestId) {
        pendingRef.current = 'recovering';
        setError(t(language, 'timeout'));
        sendCommand('snapshot.request');
      }
    }, 12000);
  }

  return (
    <main className="manor-app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">☾</span>
          <div><span className="eyebrow">{t(language, 'club')}</span><h1>MIDNIGHT MANOR</h1></div>
        </div>
        <div className="topbar-actions">
          <span className={connected ? 'connection ready' : 'connection'}>
            {connected && embedded ? t(language, 'ready') :
              embedded ? t(language, 'connecting') : t(language, 'preview')}
          </span>
          <label className="language-picker" htmlFor="manor-language">
            <span>{t(language, 'language')}</span>
            <select id="manor-language" value={language}
              onChange={event => setLanguageOverride(event.target.value as ManorLanguage)}>
              <option value="de">Deutsch</option>
              <option value="en">English</option>
            </select>
          </label>
          <button className="ghost-button" type="button"
            onClick={() => setHelpOpen(true)}>{t(language, 'help')}</button>
          <button className="ghost-button" onClick={() =>
            embedded ? sendCommand('ui.close') : window.close()}>{t(language, 'back')}</button>
        </div>
      </header>

      <section className="headline">
        <div>
          <span className="eyebrow">{t(language, 'heroEyebrow')}</span>
          <h2>{t(language, 'heroTitle')}</h2>
          <p>{t(language, 'heroText')}</p>
        </div>
        <div className="stage-tag"><span className="tag-dot" /> {t(language, 'playtest')}</div>
      </section>

      <div className="content-grid">
        <section className="machine" aria-label={t(language, 'hauntedFive')}>
          <div className="machine-title">
            <span>{t(language, 'hauntedFive')}</span>
            <span>{t(language, 'paylinesHeader')}</span>
          </div>
          <ManorStage snapshot={snapshot} language={language} reducedMotion={motionOff} speed={reelSpeed}
            spinEpoch={spinEpoch} onSpinningChange={setAnimating}
            onReelStop={index => presentation.current.onReel(index)}
            onSpinPresented={result => presentation.current.onSpin(result)}
            onDiagnostics={showDiagnostics ? setRenderMetrics : undefined} />
          {showDiagnostics && (
            <div className="render-metrics" role="status" aria-live="off">
              <strong>{t(language, 'diagnostics')}</strong>
              <span>{motionOff ? t(language, 'tickerPaused') :
                renderMetrics
                  ? t(language, 'fpsObserved', renderMetrics.fpsEstimate, renderMetrics.averageMs,
                    renderMetrics.p95Ms, renderMetrics.overBudget, renderMetrics.frames)
                  : t(language, 'sample')}</span>
              <small>{renderMetrics
                ? t(language, 'dpiObserved', renderMetrics.width, renderMetrics.height, renderMetrics.pixelRatio)
                : t(language, 'diagnosticsHint')}</small>
            </div>
          )}
          {visibleMoment && (
            <div className={'manor-moment ' + visibleMoment.key + (motionOff ? ' no-motion' : '')}
              role="status" aria-live="polite">
              <span className="manor-moment-crest" aria-hidden="true">
                {visibleMoment.key === 'moon' ? '☾' : visibleMoment.key === 'door' ? '⌑' : '✦'}
              </span>
              <div className="manor-moment-copy">
                <strong>{visibleMoment.heading}</strong>
                <span>{visibleMoment.detail}</span>
                {visibleMoment.secondary && <small>{visibleMoment.secondary}</small>}
              </div>
            </div>
          )}
          <div className="machine-footer">
            <span>{snapshot.hasCommittedSpin
              ? snapshot.lastSpinFree ? t(language, 'lastFree') : t(language, 'lastPaid')
              : t(language, 'noSpin')}</span>
            <span>{snapshot.winningLines} {t(language, 'paidLines')} · {snapshot.scatters} {t(language, 'moons')} · {snapshot.doors} {t(language, 'doors')}</span>
          </div>
          {snapshot.winningLineDetails.length > 0 && (
            <details className="win-details">
              <summary>{t(language, 'viewWinLines', snapshot.winningLineDetails.length,
                t(language, snapshot.winningLineDetails.length === 1 ? 'lineSingular' : 'linePlural'))}</summary>
              <div className="win-line-grid">
                {snapshot.winningLineDetails.map(win => (
                  <div className="win-line-result" key={win.number}>
                    <strong>{t(language, 'line')} {win.number}</strong>
                    <span>{win.count} {t(language, 'matched')}</span>
                    <b>{win.amount}</b>
                  </div>
                ))}
              </div>
            </details>
          )}
          <div className="action-console">
            <div className="stake-control">
              <label htmlFor="manor-bet">{t(language, 'stake')}</label>
              <div className="bet-input">
                <span>$</span>
                <input id="manor-bet" type="number" inputMode="numeric"
                  min={1} max={1000} step={1} value={bet}
                  onChange={event => setBet(event.target.value)}
                  disabled={disabled || !snapshot.canSpin} />
              </div>
            </div>
            <div className="main-actions">
              {snapshot.canFreeSpin ?
                <button className="gold-button" disabled={disabled}
                  onClick={() => act('spin.free')}>{t(language, 'freeSpin', snapshot.freeSpinsRemaining)}</button> :
                <button className="gold-button" disabled={disabled || !snapshot.canSpin || !validBet}
                  onClick={() => act('spin.paid', { bet })}>
                  {pending ? t(language, 'saving') : t(language, 'spin')}
                </button>}
              <span className="action-hint">{t(language, run ? 'runHint' :
                snapshot.canFreeSpin ? 'freeHint' :
                  snapshot.cycleActive ? 'cycleHint' : 'committedHint')}</span>
            </div>
          </div>

          {run && (
            <section className="labyrinth" aria-label={t(language, 'runTitle')}>
              <div className="labyrinth-heading">
                <div><span className="eyebrow">{t(language, 'runEyebrow')}</span><h3>{t(language, 'runTitle')}</h3></div>
                <div className="turn-counter">{t(language, 'turn')} {run.turn} / 24</div>
              </div>
              <p className="run-message">{localizeRunText(run.message, language)}</p>
              <div className={'ghost-panel ' + threat} role="status" aria-live="polite">
                <span className="ghost-panel-label">{language === 'en'
                  ? ghostBandLabel[threat ?? 'uncertain']
                  : t(language, threat === 'revealed' ? 'ghostRevealed' :
                      threat === 'danger' ? 'ghostDanger' :
                      threat === 'approaching' ? 'ghostApproaching' :
                      threat === 'distant' ? 'ghostDistant' : 'ghostUnknown')}</span>
                <p>{localizeRunText(run.ghostClue, language)}</p>
              </div>
              <div className="run-summary">
                <span>{t(language, 'potential')} <strong>{run.current}</strong></span>
                <span>{t(language, 'checkpoint')} <strong>{run.banked}</strong></span>
                <span>{t(language, 'exit')} <strong>{run.exitTarget}×</strong></span>
                <span>{t(language, run.hasKey ? 'keyFound' : 'noKey')}</span>
                <span>{t(language, run.hasCharm ? 'charmReady' : 'noCharm')}</span>
              </div>
              <ManorRoomScene run={run} language={language} reducedMotion={motionOff} />
              <p className="map-instructions">{t(language, 'routeInstruction')}</p>
              <ManorRunMap run={run} disabled={disabled} language={language}
                onMove={room => act('run.move', { room })} />
              <div className="run-actions">
                <button className="ghost-button" disabled={disabled || !run.hasCandle}
                  onClick={() => act('run.candle')}>{t(language, 'candle')}</button>
                <button className="ghost-button" disabled={disabled}
                  onClick={() => act('run.follow')}>{t(language, 'follow')}</button>
                <button className="ghost-button" disabled={disabled}
                  onClick={() => act('run.cashout')}>{t(language, 'cashout')}</button>
                {!confirmAuto ? (
                  <button className="ghost-button" disabled={disabled}
                    onClick={() => setConfirmAuto(true)}>{t(language, 'resolve')}</button>
                ) : (
                  <div className="confirm-auto" role="group" aria-label={t(language, 'confirmGroup')}>
                    <span>{t(language, 'confirmQuestion')}</span>
                    <button className="ghost-button" onClick={() => act('run.auto')}
                      disabled={disabled}>{t(language, 'confirm')}</button>
                    <button className="ghost-button" onClick={() => setConfirmAuto(false)}>{t(language, 'cancel')}</button>
                  </div>
                )}
              </div>
            </section>
          )}
        </section>

        <aside className="sidebar" aria-label={t(language, 'account')}>
          <div className="status-card">
            <span className="eyebrow">{t(language, 'account')}</span>
            <h3>{t(language, 'chandelier')}</h3>
            <div className="money-stat"><span>{t(language, 'balance')}</span><strong>{snapshot.bankroll}</strong></div>
            <div className="money-stat"><span>{t(language, 'lastStake')}</span><strong>{snapshot.bet}</strong></div>
            <div className="money-stat"><span>{t(language, 'lastReturn')}</span><strong>{snapshot.payout}</strong></div>
            <div className="money-stat"><span>{t(language, 'cycleReturn')}</span><strong>{snapshot.cycleReturned}</strong></div>
          </div>
          <div className="status-card feature-card">
            <span className="eyebrow">{t(language, 'watching')}</span>
            <h3>{localizeFeature(snapshot.feature, language)}</h3>
            <p>{t(language, run ? 'runDescription' :
              snapshot.canFreeSpin ? 'freeDescription' :
                snapshot.cycleActive ? 'cycleDescription' : 'idleDescription')}</p>
          </div>
          <div className="status-card">
            <span className="eyebrow">{t(language, 'settings')}</span>
            <label className="switch-row"><span>{t(language, 'cabinetAnimation')}</span>
              <input type="checkbox" checked={ambient}
                onChange={event => setAmbient(event.target.checked)} /></label>
            <label className="switch-row"><span>{t(language, 'reduced')}</span>
              <input type="checkbox" checked={lowMotion}
                onChange={event => setLowMotion(event.target.checked)} /></label>
            <div className="audio-settings" aria-label={t(language, 'soundGroup')}>
              <span className="eyebrow">{t(language, 'sound')}</span>
              <label className="switch-row"><span>{t(language, 'sfx')}</span>
                <input type="checkbox" checked={soundEnabled && snapshot.globalSoundEnabled}
                  disabled={!snapshot.globalSoundEnabled}
                  onChange={event => {
                    const enabled = event.target.checked;
                    setSoundEnabled(enabled);
                    audio.current?.setEnabled(enabled);
                    if (enabled) audio.current?.arm();
                  }} />
              </label>
              <label className="switch-row"><span>{t(language, 'ambience')}</span>
                <input type="checkbox" checked={ambienceEnabled}
                  disabled={!soundEnabled || !snapshot.globalSoundEnabled || !snapshot.globalMusicEnabled}
                  onChange={event => {
                    const enabled = event.target.checked;
                    setAmbienceEnabled(enabled);
                    audio.current?.setAmbience(enabled);
                    if (enabled) audio.current?.arm();
                  }} />
              </label>
              <label className="audio-volume" htmlFor="manor-volume">
                <span>{t(language, 'volume')}</span><output htmlFor="manor-volume">{volume}%</output>
              </label>
              <input className="audio-slider" id="manor-volume" type="range" min={0} max={100}
                step={5} value={volume} disabled={!soundEnabled || !snapshot.globalSoundEnabled}
                onChange={event => {
                  const level = Number(event.target.value);
                  setVolume(level);
                  audio.current?.setVolume(level / 100);
                }} />
              <p className="display-hint">{t(language, 'soundHint')}</p>
              {!snapshot.globalSoundEnabled && <p className="audio-host-policy" role="status">
                {language === 'de' ? 'Ton ist in den Casino-Einstellungen global deaktiviert.' :
                  'Sound is disabled in the casino-wide settings.'}</p>}
              {snapshot.globalSoundEnabled && !snapshot.globalMusicEnabled &&
                <p className="audio-host-policy">
                  {language === 'de' ? 'Hintergrundmusik ist global deaktiviert.' :
                    'Background music is disabled in the casino-wide settings.'}</p>}
            </div>
            <label className="switch-row" htmlFor="manor-perf-diagnostics">
              <span>{t(language, 'diagnosticsToggle')}</span>
              <input type="checkbox" id="manor-perf-diagnostics"
                checked={showDiagnostics}
                onChange={event => {
                  setShowDiagnostics(event.target.checked);
                  setRenderMetrics(null);
                }} />
            </label>
            <label className="speed-control" htmlFor="reel-speed">{t(language, 'speed')}
              <select id="reel-speed" value={reelSpeed}
                onChange={event => setReelSpeed(event.target.value as ReelSpeed)}>
                <option value="normal">{t(language, 'normal')}</option>
                <option value="fast">{t(language, 'fast')}</option>
                <option value="turbo">{t(language, 'turbo')}</option>
              </select>
            </label>
            <p className="display-hint">{t(language, 'motionHint')}</p>
            <button className="ghost-button refresh"
              onClick={() => sendCommand('snapshot.request')} disabled={!embedded}>
              {t(language, 'refresh')}</button>
          </div>
        </aside>
      </div>
      {snapshot.saveNotice && <div className="save-notice" role="status">{snapshot.saveNotice}</div>}
      {error && <div className="error" role="alert">{error}</div>}
      {fatal && <div className="error" role="alert">{t(language, 'fatal')}</div>}
      {helpOpen && <ManorHelp language={language} rules={snapshot.rules}
        cycleActive={snapshot.cycleActive} onClose={closeHelp} />}
      <footer className="footer">
        <span>{t(language, 'footer')}</span>
        <span>{embedded ? t(language, 'revision', snapshot.revision) : t(language, 'browserFooter')}</span>
      </footer>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<ManorApp />);
