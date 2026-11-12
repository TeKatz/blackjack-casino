import { useCallback, useEffect, useRef, useState } from 'react';
import type { ManorRun, GameAction } from '../bridge/protocol';
import { localizeRoom, t } from '../i18n';
import type { ManorLanguage } from '../i18n';
import {
  demoState, liveState, isGodotMessage, isAllowedGodotChoice,
} from './manorGodotProtocol';
import type { GodotState } from './manorGodotProtocol';

type Props = {
  /** Stays mounted during ordinary reel play so WebAssembly can warm up once. */
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

/** A single persistent Godot instance behind the PixiJS machine, not a second window. */
export function ManorBonus3D({
  active, demo, run, language, reducedMotion, disabled, onAction, onExitDemo, onFallback,
}: Props) {
  const frame = useRef<HTMLIFrameElement>(null);
  const [ready, setReady] = useState(false);
  const [failed, setFailed] = useState(false);
  const [demoRoom, setDemoRoom] = useState(0);
  const [demoSteps, setDemoSteps] = useState(0);
  const [choiceWaiting, setChoiceWaiting] = useState(false);
  const hadPending = useRef(false);
  const state: GodotState = demo ? demoState(demoRoom) : run ? liveState(run) : demoState(0);
  const latest = useRef({ state, active, reducedMotion });
  latest.current = { state, active, reducedMotion };
  const locked = disabled || choiceWaiting;

  const postState = useCallback((next: GodotState, visible: boolean, reduce: boolean) => {
    frame.current?.contentWindow?.postMessage(
      { ...next, active: visible, reducedMotion: reduce }, window.location.origin);
  }, []);

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
      // A warmed-up but hidden iframe is never allowed to request gameplay.
      if (!active || disabled || choiceWaiting ||
        !isAllowedGodotChoice(latest.current.state, event.data.room)) return;
      const destination = event.data.room;
      if (demo) {
        setDemoRoom(destination);
        setDemoSteps(value => value + 1);
      } else if (run) {
        // Only C# decides and saves real moves and ghost behavior.
        setChoiceWaiting(true);
        onAction('run.move', destination);
      }
    };
    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [active, choiceWaiting, demo, disabled, onAction, postState, run]);

  useEffect(() => {
    if (ready) postState(state, active, reducedMotion);
  }, [ready, active, reducedMotion, demoRoom, run?.playerRoom, run?.turn, run?.hasKey,
    run?.hasCandle, run?.hasCharm, language, postState]);

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
  }, [active, demo]);

  useEffect(() => {
    if (ready || !active) return;
    const failTimer = window.setTimeout(() => setFailed(true), 18000);
    return () => window.clearTimeout(failTimer);
  }, [active, ready]);

  const pick = (slot: number) => {
    if (!active || !ready || locked || !state.options[slot]) return;
    // The choice still flows through Godot's physical door animation and its
    // existing postMessage reply; this never bypasses C# for live bonuses.
    frame.current?.contentWindow?.postMessage(
      { source: 'manor-react', type: 'pick', slot }, window.location.origin);
  };

  const title = demo ? t(language, 'godotDemoTag') : t(language, 'godotLiveTag');
  const status = demo
    ? t(language, 'godotDemoProgress', demoSteps)
    : run ? run.current + ' · ' + t(language, 'checkpoint') + ' ' + run.banked : '';

  return (
    <section className={'manor-godot-surface' +
      (active ? ' is-active' : ' is-warming') +
      (reducedMotion ? ' no-motion' : '')}
      aria-label={t(language, 'godotArea')}
      aria-hidden={!active}>
      <iframe
        className="manor-godot-frame"
        ref={frame}
        src="./manor3d/index.html"
        title={t(language, 'godotArea')}
        sandbox="allow-scripts allow-same-origin"
        loading="eager"
        tabIndex={active ? 0 : -1}
        onError={() => setFailed(true)}
      />
      {active && <>
        <div className="manor-godot-upper" aria-live="polite">
          <div><span className="eyebrow">{title}</span>
            <strong>{localizeRoom(state.roomName, language)}</strong></div>
          <span className="manor-godot-result">{status}</span>
        </div>
        {ready && !failed && state.options.length > 0 &&
          <nav className="manor-godot-door-actions" aria-label={t(language, 'chooseRoom')}>
            {state.options.map((choice, slot) =>
              <button key={slot} type="button" className="manor-godot-door-choice"
                disabled={locked} onClick={() => pick(slot)}
                aria-label={t(language, 'moveTo', localizeRoom(choice.name, language))}>
                <small>{String(slot + 1).padStart(2, '0')}</small>
                <span>{localizeRoom(choice.name, language)}</span>
              </button>)}
          </nav>}
        {(!ready || failed) && <div className="manor-godot-loading" role="status">
          <strong>{failed ? t(language, 'godotUnavailable') : t(language, 'godotLoading')}</strong>
          <span>{failed ? t(language, 'godotUnavailableDetail') :
            t(language, 'godotLoadingDetail')}</span>
          {failed && <button type="button" className="ghost-button"
            onClick={demo ? onExitDemo : onFallback}>{t(language, 'godotReturn')}</button>}
        </div>}
        <div className="manor-godot-bottom">
          <span>{t(language, 'godotHint')}</span>
          <div className="manor-godot-controls">
            {!demo && run && <>
              {run.hasCandle &&
                <button type="button" className="ghost-button"
                  disabled={locked} onClick={() => onAction('run.candle')}>
                  {t(language, 'candle')}
                </button>}
              <button className="ghost-button" type="button"
                disabled={locked} onClick={() => onAction('run.cashout')}>
                {t(language, 'cashout')}</button>
              <button className="ghost-button" type="button"
                onClick={onFallback}>{t(language, 'godotClassic')}</button>
            </>}
            {demo && <button type="button" className="ghost-button"
              onClick={onExitDemo}>{t(language, 'godotReturn')}</button>}
          </div>
        </div>
        {demo && <span className="manor-godot-safe">{t(language, 'godotNoPayout')}</span>}
      </>}
    </section>
  );
}
