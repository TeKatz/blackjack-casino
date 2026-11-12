import { useCallback, useEffect, useRef, useState } from 'react';
import type { ManorRun, GameAction } from '../bridge/protocol';
import { localizeRoom, t } from '../i18n';
import type { ManorLanguage } from '../i18n';
import {
  demoState, liveState, isGodotMessage, isAllowedGodotChoice,
} from './manorGodotProtocol';
import type { GodotState } from './manorGodotProtocol';

type Props = {
  demo: boolean;
  run: ManorRun | null;
  language: ManorLanguage;
  reducedMotion: boolean;
  disabled: boolean;
  onAction: (action: GameAction, room?: number) => void;
  onExitDemo: () => void;
  onFallback: () => void;
};

/**
 * Same-origin Godot WebAssembly/WebGL2 child surface, not an extra Windows process.
 * This component never sends paid-spin or multiplier commands. A live room move
 * goes through the existing C# journal only after matching an authoritative
 * available destination. Demo navigation is React state and never touches C#.
 */
export function ManorBonus3D({
  demo, run, language, reducedMotion, disabled, onAction, onExitDemo, onFallback,
}: Props) {
  const frame = useRef<HTMLIFrameElement>(null);
  const [ready, setReady] = useState(false);
  const [failed, setFailed] = useState(false);
  const [demoRoom, setDemoRoom] = useState(0);
  const [demoSteps, setDemoSteps] = useState(0);
  const [choiceWaiting, setChoiceWaiting] = useState(false);
  const state: GodotState = demo ? demoState(demoRoom) : run ? liveState(run) : demoState(0);
  const latest = useRef(state);
  latest.current = state;
  const locked = disabled || choiceWaiting;

  const postState = useCallback((next: GodotState) => {
    frame.current?.contentWindow?.postMessage(next, window.location.origin);
  }, []);

  useEffect(() => {
    const listener = (event: MessageEvent) => {
      if (event.origin !== window.location.origin ||
        event.source !== frame.current?.contentWindow || !isGodotMessage(event.data)) return;
      if (event.data.type === 'ready') {
        setReady(true);
        setFailed(false);
        postState(latest.current);
        return;
      }
      if (disabled || choiceWaiting || !isAllowedGodotChoice(latest.current, event.data.room))
        return;
      const destination = event.data.room;
      if (demo) {
        setDemoRoom(destination);
        setDemoSteps(value => value + 1);
      } else if (run) {
        // Godot can only propose a room. The existing journal and ghost engine
        // remain responsible for validating/committing the actual transition.
        setChoiceWaiting(true);
        onAction('run.move', destination);
      }
    };
    window.addEventListener('message', listener);
    return () => window.removeEventListener('message', listener);
  }, [choiceWaiting, demo, disabled, onAction, postState, run]);

  useEffect(() => {
    if (ready) postState(state);
  }, [ready, demoRoom, run?.playerRoom, run?.turn, run?.hasKey, language, postState]);

  useEffect(() => { setChoiceWaiting(false); }, [run?.turn, run?.playerRoom]);

  useEffect(() => {
    if (ready) return;
    const failTimer = window.setTimeout(() => setFailed(true), 20000);
    return () => window.clearTimeout(failTimer);
  }, [ready]);

  const title = demo ? t(language, 'godotDemoTag') : t(language, 'godotLiveTag');
  const status = demo
    ? t(language, 'godotDemoProgress', demoSteps)
    : run ? run.current + ' · ' + t(language, 'checkpoint') + ' ' + run.banked : '';

  return (
    <section className={'manor-godot-surface' + (reducedMotion ? ' no-motion' : '')}
      aria-label={t(language, 'godotArea')}>
      <iframe
        className="manor-godot-frame"
        ref={frame}
        src="./manor3d/index.html"
        title={t(language, 'godotArea')}
        sandbox="allow-scripts allow-same-origin"
        loading="eager"
        onError={() => setFailed(true)}
      />
      <div className="manor-godot-upper" aria-live="polite">
        <div><span className="eyebrow">{title}</span>
          <strong>{localizeRoom(state.roomName, language)}</strong></div>
        <span className="manor-godot-result">{status}</span>
      </div>
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
    </section>
  );
}
