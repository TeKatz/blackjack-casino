import { useCallback, useEffect, useRef, useState } from 'react';
import type { ManorRun, GameAction } from '../bridge/protocol';
import { localizeRoom, localizeRunText, t } from '../i18n';
import type { ManorLanguage } from '../i18n';
import { ghostBand } from './manorRunUi';
import {
  demoState, liveState, isGodotMessage, isAllowedGodotChoice,
} from './manorGodotProtocol';
import type { GodotState } from './manorGodotProtocol';
import { makeDemoMap, legalMapDestinations } from './manorGodotMapData';
import type { ManorMapSnapshot } from './manorGodotMapData';
import { ManorGodotMap } from './ManorGodotMap';

type Props = {
  /** A single iframe remains mounted behind ordinary reels to avoid repeated WASM startup. */
  active: boolean;
  demo: boolean;
  run: ManorRun | null;
  language: ManorLanguage;
  reducedMotion: boolean;
  disabled: boolean;
  onAction: (action: GameAction, room?: number) => void;
  onExitDemo: () => void;
  onFallback: () => void;
};

type ItemKind = 'key' | 'candle' | 'charm';
type TransitionPhase = 'hidden' | 'loading' | 'entering' | 'inside' | 'leaving';

const ENTER_DURATION_MS = 840;
const EXIT_DURATION_MS = 640;

function ItemIcon({ kind }: { kind: ItemKind }) {
  return (
    <svg width="24" height="24" viewBox="0 0 24 24"
      fill="none" stroke="currentColor" strokeWidth="1.6"
      strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {kind === 'key' ? <>
        <circle cx="7.5" cy="8" r="4.5" />
        <path d="M10.6 11.4L20 20.5M15.1 15.5l2-2M17.8 18l2-2" />
      </> : kind === 'candle' ? <>
        <path d="M8 10h8v10H8zM7 20h10M12 10V7" />
        <path d="M12 2c1.8 2 2.2 3.2 0 5-2.1-1.4-2.1-3.2 0-5Z" />
        <path d="M8 14h8" opacity=".5" />
      </> : <>
        <path d="M12 2L19 6v6c0 4.2-2.4 7.1-7 10-4.6-2.9-7-5.8-7-10V6z" />
        <path d="M12 7v10M8 12h8" />
      </>}
    </svg>
  );
}

function MapExpandIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24"
    fill="none" stroke="currentColor" strokeWidth="1.8"
    strokeLinecap="round" aria-hidden="true">
    <path d="M9 4H4v5M15 4h5v5M4 15v5h5M20 15v5h-5" />
  </svg>;
}

function threatKey(run: ManorRun | null) {
  if (!run) return 'godotDemoThreat' as const;
  switch (ghostBand(run.ghostClue)) {
    case 'revealed': return 'ghostRevealed';
    case 'danger': return 'ghostDanger';
    case 'approaching': return 'ghostApproaching';
    case 'distant': return 'ghostDistant';
    default: return 'ghostUnknown';
  }
}

/** This is strictly a presentation of C# run snapshots; no local bonus RNG. */
export function ManorBonus3D({
  active, demo, run, language, reducedMotion, disabled, onAction, onExitDemo, onFallback,
}: Props) {
  const frame = useRef<HTMLIFrameElement>(null);
  const [ready, setReady] = useState(false);
  // Preserve the final 3D frame and HUD for an exit animation, even when
  // C# removes the finished run from its next authoritative snapshot.
  const [phase, setPhase] = useState<TransitionPhase>(
    active ? 'loading' : 'hidden');
  const phaseRef = useRef<TransitionPhase>(active ? 'loading' : 'hidden');
  const previouslyActive = useRef(active);
  const [failed, setFailed] = useState(false);
  const [demoRoom, setDemoRoom] = useState(0);
  const [demoSteps, setDemoSteps] = useState(0);
  const [demoVisited, setDemoVisited] = useState<number[]>([0]);
  const [choiceWaiting, setChoiceWaiting] = useState(false);
  const [mapOpen, setMapOpen] = useState(false);
  const [hoveredRoom, setHoveredRoom] = useState<number | null>(null);
  const [eventMessage, setEventMessage] = useState<string | null>(null);
  const lastEvent = useRef('');
  const hadPending = useRef(false);
  const state: GodotState = demo ? demoState(demoRoom) : run ? liveState(run) : demoState(0);
  const map: ManorMapSnapshot = demo ? makeDemoMap(demoRoom, demoVisited) :
    run ?? makeDemoMap(0);
  const lastShown = useRef({ demo, run, map, state });
  if (active) lastShown.current = { demo, run, map, state };
  const shown = active ? { demo, run, map, state } : lastShown.current;
  const shownRun = shown.run;
  const shownDemo = shown.demo;
  const shownMap = shown.map;
  const shownState = shown.state;
  const displayed = active || phase !== 'hidden';
  const latest = useRef({ state, active, reducedMotion });
  latest.current = { state, active, reducedMotion };
  const locked = disabled || choiceWaiting || phase !== 'inside';
  const currentName = shownMap.rooms.find(room => room.id === shownMap.playerRoom)?.name ??
    shownState.roomName;
  const band = shownRun ? ghostBand(shownRun.ghostClue) : 'uncertain';

  useEffect(() => {
    const wasActive = previouslyActive.current;
    previouslyActive.current = active;
    let timer: number | undefined;
    const change = (next: TransitionPhase) => {
      phaseRef.current = next;
      setPhase(next);
    };

    if (active) {
      // Play the reveal only after the WebGL canvas is ready. While Godot
      // boots we show a clear loading state instead of a blank iframe.
      if (!ready) change('loading');
      else if (reducedMotion) change('inside');
      else if (!wasActive || phaseRef.current === 'loading' ||
        phaseRef.current === 'entering' || phaseRef.current === 'leaving') {
        change('entering');
        timer = window.setTimeout(() => change('inside'), ENTER_DURATION_MS);
      }
    } else if (wasActive || phaseRef.current === 'leaving') {
      if (reducedMotion) change('hidden');
      else {
        change('leaving');
        timer = window.setTimeout(() => change('hidden'), EXIT_DURATION_MS);
      }
    } else {
      change('hidden');
    }
    return () => { if (timer !== undefined) window.clearTimeout(timer); };
  }, [active, ready, reducedMotion]);

  const postState = useCallback((next: GodotState, visible: boolean, reduce: boolean) => {
    frame.current?.contentWindow?.postMessage(
      { ...next, active: visible, reducedMotion: reduce }, window.location.origin);
  }, []);

  // Both the enlarged map and the visible 3D doors use the same C#-approved
  // destinations. The map also exposes a FOURTH edge where a physical room
  // would otherwise have only three door models.
  const chooseDestination = useCallback((destination: number) => {
    if (!active || locked || !legalMapDestinations(map).some(room => room.id === destination))
      return;
    setHoveredRoom(null);
    setMapOpen(false);
    if (demo) {
      setDemoRoom(destination);
      setDemoVisited(previous => previous.includes(destination) ?
        previous : [...previous, destination]);
      setDemoSteps(value => value + 1);
    } else if (run) {
      setChoiceWaiting(true);
      onAction('run.move', destination);
    }
  }, [active, locked, map, demo, run, onAction]);

  useEffect(() => {
    const listener = (event: MessageEvent) => {
      if (event.origin !== window.location.origin ||
        event.source !== frame.current?.contentWindow || !isGodotMessage(event.data)) return;
      if (event.data.type === 'ready') {
        setReady(true);
        setFailed(false);
        postState(latest.current.state, latest.current.active, latest.current.reducedMotion);
        return;
      }
      if (event.data.type === 'hover') {
        if (!active || mapOpen) return;
        const hovered = event.data.room;
        setHoveredRoom(hovered !== null &&
          isAllowedGodotChoice(latest.current.state, hovered) ? hovered : null);
        return;
      }
      if (!active || mapOpen || !isAllowedGodotChoice(latest.current.state, event.data.room))
        return;
      chooseDestination(event.data.room);
    };
    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [active, mapOpen, chooseDestination, postState]);

  useEffect(() => {
    if (ready) postState(state, active, reducedMotion);
  }, [ready, active, reducedMotion, demoRoom, run?.playerRoom, run?.turn,
    run?.hasKey, run?.hasCandle, run?.hasCharm, language, postState]);

  useEffect(() => { setChoiceWaiting(false); }, [run?.turn, run?.playerRoom]);

  useEffect(() => {
    if (disabled) hadPending.current = true;
    else if (hadPending.current) {
      hadPending.current = false;
      setChoiceWaiting(false);
    }
  }, [disabled]);

  useEffect(() => {
    if (!active || !demo) return;
    setDemoRoom(0);
    setDemoSteps(0);
    setDemoVisited([0]);
  }, [active, demo]);

  useEffect(() => {
    if (!active) { setMapOpen(false); setHoveredRoom(null); }
  }, [active]);

  useEffect(() => {
    if (!mapOpen) return;
    const escape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMapOpen(false);
    };
    window.addEventListener('keydown', escape);
    return () => window.removeEventListener('keydown', escape);
  }, [mapOpen]);

  // Event copy is derived only from the acknowledged run message. It never
  // fires on iframe messages or speculative navigation attempts.
  useEffect(() => {
    if (!active || demo || !run) {
      setEventMessage(null);
      if (!active) lastEvent.current = '';
      return;
    }
    const key = String(run.turn) + ':' + run.message;
    if (lastEvent.current === key) return;
    lastEvent.current = key;
    setEventMessage(run.message);
    const timer = window.setTimeout(() => setEventMessage(null), 3600);
    return () => window.clearTimeout(timer);
  }, [active, demo, run?.turn, run?.message]);

  useEffect(() => {
    if (ready || !active) return;
    const failTimer = window.setTimeout(() => setFailed(true), 18000);
    return () => window.clearTimeout(failTimer);
  }, [active, ready]);

  const pick = (slot: number) => {
    if (!active || !ready || locked || mapOpen || !state.options[slot]) return;
    // Physical 3D door animation remains in Godot, but legal movement and
    // payout confirmation remain entirely in C#.
    frame.current?.contentWindow?.postMessage(
      { source: 'manor-react', type: 'pick', slot }, window.location.origin);
  };

  const item = (kind: ItemKind, possessed: boolean, label: string) =>
    kind === 'candle' ? (
      <button type="button" key={kind}
        className={'manor-godot-item' + (possessed ? ' owned usable' : '')}
        disabled={!active || demo || locked || !possessed}
        onClick={() => onAction('run.candle')}
        title={label} aria-label={label + ' · ' +
          t(language, possessed ? 'godotInventoryAvailable' : 'godotInventoryMissing')}>
        <ItemIcon kind={kind}/><span>{label}</span>
      </button>
    ) : (
      <div key={kind} className={'manor-godot-item' + (possessed ? ' owned' : '')}
        title={label} aria-label={label + ' · ' +
          t(language, possessed ? 'godotInventoryAvailable' : 'godotInventoryMissing')}>
        <ItemIcon kind={kind}/><span>{label}</span>
      </div>
    );

  return (
    <section className={'manor-godot-surface' +
      (displayed ? ' is-active' : ' is-warming') +
      ' phase-' + phase +
      (reducedMotion ? ' no-motion' : '')}
      aria-label={t(language, 'godotArea')} aria-hidden={!displayed}>
      <iframe className="manor-godot-frame" ref={frame}
        src="./manor3d/index.html" title={t(language, 'godotArea')}
        sandbox="allow-scripts allow-same-origin" loading="eager"
        tabIndex={active && !mapOpen ? 0 : -1}
        onMouseLeave={() => setHoveredRoom(null)}
        onError={() => setFailed(true)} />

      {displayed && <>
        <div className="manor-godot-hud-left">
          <header className="manor-godot-room-heading">
            <span className="eyebrow">{t(language, shownDemo ? 'godotDemoTag' : 'godotLiveTag')}</span>
            <strong>{localizeRoom(currentName, language)}</strong>
            <small>{shownDemo ? t(language, 'godotDemoProgress', demoSteps) :
              t(language, 'godotTurnOf', shownRun?.turn ?? 0, 24)}</small>
          </header>
          <div className="manor-godot-payout-panel" aria-label={t(language, 'godotRewardStatus')}>
            <div className="manor-godot-payout-pair">
              <span><small>{t(language, 'potential')}</small><strong>{shownRun?.current ?? '—'}</strong></span>
              <span><small>{t(language, 'checkpoint')}</small><strong>{shownRun?.banked ?? '—'}</strong></span>
            </div>
            <span className="manor-godot-target">
              {t(language, 'godotExitTarget')} <b>{shownRun ? String(shownRun.exitTarget) + '×' : '—'}</b>
            </span>
          </div>
          <div className={'manor-godot-threat ' + band} role="status">
            <div className="manor-godot-threat-title">
              <span className="manor-godot-threat-dot" aria-hidden="true"/>
              {t(language, threatKey(shownDemo ? null : shownRun))}
            </div>
            <p>{shownDemo ? t(language, 'godotDemoGhostHint') :
              localizeRunText(shownRun?.ghostClue ?? '', language)}</p>
          </div>
          {eventMessage && !shownDemo &&
            <div key={eventMessage} className="manor-godot-event"
              role="status" aria-live="polite">
              <span>{t(language, 'godotEventTitle')}</span>
              <p>{localizeRunText(eventMessage, language)}</p>
            </div>}
        </div>

        <aside className="manor-godot-hud-map" aria-label={t(language, 'godotMapHeading')}>
          <div className="manor-godot-mini-map-heading">
            <strong>{t(language, 'godotMapHeading')}</strong>
            <button type="button" disabled={!active || phase !== 'inside'}
              onClick={() => setMapOpen(true)}
              aria-label={t(language, 'godotMapExpand')} title={t(language, 'godotMapExpand')}>
              <MapExpandIcon/>
            </button>
          </div>
          <ManorGodotMap map={shownMap} language={language}
            hoveredRoom={hoveredRoom} onHover={setHoveredRoom}/>
        </aside>

        {ready && !failed && shownState.options.length > 0 &&
          <nav className="manor-godot-door-actions" aria-label={t(language, 'chooseRoom')}>
            {shownState.options.map((choice, slot) =>
              <button key={slot} type="button"
                className={'manor-godot-door-choice' +
                  (hoveredRoom === choice.id ? ' is-highlighted' : '')}
                disabled={!active || locked || mapOpen} onClick={() => pick(slot)}
                onMouseEnter={() => setHoveredRoom(choice.id)}
                onMouseLeave={() => setHoveredRoom(null)}
                onFocus={() => setHoveredRoom(choice.id)}
                onBlur={() => setHoveredRoom(null)}
                aria-label={t(language, 'moveTo', localizeRoom(choice.name, language))}>
                <small>{String(slot + 1).padStart(2, '0')}</small>
                <span>{localizeRoom(choice.name, language)}</span>
              </button>)}
          </nav>}

        <div className="manor-godot-inventory" aria-label={t(language, 'godotInventoryHeading')}>
          <small>{t(language, 'godotInventoryHeading')}</small>
          <div className="manor-godot-inventory-items">
            {item('key', !shownDemo && Boolean(shownRun?.hasKey), t(language, 'godotItemKey'))}
            {item('candle', !shownDemo && Boolean(shownRun?.hasCandle), t(language, 'godotItemCandle'))}
            {item('charm', !shownDemo && Boolean(shownRun?.hasCharm), t(language, 'godotItemCharm'))}
          </div>
        </div>

        <div className="manor-godot-action-dock">
          {!shownDemo && shownRun && <>
            <button className="manor-godot-cashout" type="button"
              disabled={!active || locked || mapOpen} onClick={() => onAction('run.cashout')}>
              {t(language, 'cashout')} · {shownRun.current}
            </button>
            <button className="ghost-button" type="button"
              onClick={onFallback}>{t(language, 'godotClassic')}</button>
          </>}
          {shownDemo && <button className="ghost-button" type="button"
            onClick={onExitDemo}>{t(language, 'godotReturn')}</button>}
        </div>
        <p className="manor-godot-hint">{t(language, 'godotHint')}</p>
        {shownDemo && <span className="manor-godot-safe">{t(language, 'godotNoPayout')}</span>}

        {(!ready || failed) && active && <div className="manor-godot-loading" role="status">
          <strong>{failed ? t(language, 'godotUnavailable') : t(language, 'godotLoading')}</strong>
          <span>{failed ? t(language, 'godotUnavailableDetail') :
            t(language, 'godotLoadingDetail')}</span>
          <button type="button" className="ghost-button"
            onClick={demo ? onExitDemo : onFallback}>
            {t(language, demo ? 'godotReturn' : 'godotClassic')}
          </button>
        </div>}

        {mapOpen && active && <div className="manor-godot-map-overlay">
          <div className="manor-godot-map-backdrop"
            onClick={() => setMapOpen(false)} aria-hidden="true"/>
          <section className="manor-godot-map-dialog"
            role="dialog" aria-label={t(language, 'godotMapHeading')}>
            <header>
              <div>
                <span className="eyebrow">{t(language, 'godotMapComplete')}</span>
                <h2>{t(language, 'floorplan')}</h2>
              </div>
              <button className="ghost-button" type="button"
                autoFocus onClick={() => setMapOpen(false)}>
                {t(language, 'godotMapClose')}
              </button>
            </header>
            <ManorGodotMap map={shownMap} language={language}
              large disabled={locked} hoveredRoom={hoveredRoom}
              onHover={setHoveredRoom} onSelect={chooseDestination}/>
            <p className="manor-godot-map-footer">{t(language, 'godotMapNote')}</p>
          </section>
        </div>}
        {(phase === 'entering' || phase === 'leaving') && !reducedMotion &&
          <div className={'manor-godot-portal phase-' + phase} aria-hidden="true">
            <span className="manor-godot-portal-leaf left"/>
            <span className="manor-godot-portal-leaf right"/>
            <span className="manor-godot-portal-light"/>
          </div>}
      </>}
    </section>
  );
}
