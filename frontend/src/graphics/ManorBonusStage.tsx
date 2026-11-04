import { useEffect, useRef, useState } from 'react';
import type { GameAction, ManorRun } from '../bridge/protocol';
import { t, localizeRoom, localizeRunText } from '../i18n';
import type { ManorLanguage } from '../i18n';
import { ManorRoomScene } from './ManorRoomScene';
import { ManorRunMap } from './ManorRunMap';
import { availableManorRooms, ghostBand } from './manorRunUi';

type Props = {
  run: ManorRun;
  language: ManorLanguage;
  disabled: boolean;
  reducedMotion: boolean;
  onAction: (action: GameAction, room?: number) => void;
};

/** The entire bonus occupies the reel window. All choices remain C#-authorized. */
export function ManorBonusStage({ run, language, disabled, reducedMotion, onAction }: Props) {
  const [mapOpen, setMapOpen] = useState(false);
  const [confirmAuto, setConfirmAuto] = useState(false);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const threat = ghostBand(run.ghostClue);
  const choices = availableManorRooms(run);

  useEffect(() => {
    titleRef.current?.focus({ preventScroll: true });
  }, []);

  const act = (type: GameAction, room?: number) => {
    setConfirmAuto(false);
    onAction(type, room);
  };
  return (
    <section className={'manor-bonus-stage' + (reducedMotion ? ' no-motion' : '')}
      aria-label={t(language, 'runTitle')}>
      <div className="bonus-stage-header">
        <div>
          <span className="eyebrow">{t(language, 'runEyebrow')} · {t(language, 'turn')} {run.turn} / 24</span>
          <h2 ref={titleRef} tabIndex={-1}>{t(language, 'runTitle')}</h2>
        </div>
        <button className="ghost-button bonus-map-toggle" type="button"
          aria-expanded={mapOpen} onClick={() => setMapOpen(!mapOpen)}>
          {t(language, mapOpen ? 'bonusReturn' : 'bonusMap')}
        </button>
      </div>
      <div className="bonus-stage-multipliers">
        <span>{t(language, 'potential')} <strong>{run.current}</strong></span>
        <span>{t(language, 'checkpoint')} <strong>{run.banked}</strong></span>
        <span>{t(language, 'exit')} <strong>{run.exitTarget}×</strong></span>
        {run.hasKey && <span>{t(language, 'keyFound')}</span>}
        {run.hasCharm && <span>{t(language, 'charmReady')}</span>}
      </div>
      {mapOpen ? (
        <div className="bonus-map-view">
          <ManorRunMap run={run} disabled={disabled} language={language}
            onMove={room => act('run.move', room)} />
        </div>
      ) : (
        <div className="bonus-stage-content">
          <div className="bonus-story">
            <ManorRoomScene run={run} language={language} reducedMotion={reducedMotion} />
            <p className="bonus-message" role="status">{localizeRunText(run.message, language)}</p>
            <p className={'bonus-ghost-clue ' + threat}>{localizeRunText(run.ghostClue, language)}</p>
          </div>
          <div className="bonus-choices" role="group" aria-label={t(language, 'chooseRoom')}>
            <strong className="bonus-choices-title">{t(language, 'chooseRoom')}</strong>
            <div className="bonus-room-buttons">
              {choices.map(room => (
                <button key={room.id} className="bonus-room-button" type="button"
                  disabled={disabled} onClick={() => act('run.move', room.id)}>
                  <span aria-hidden="true">⌑</span>
                  <span>{localizeRoom(room.name, language)}</span>
                  <small>{t(language, 'enter')}</small>
                </button>
              ))}
              {choices.length === 0 && <p>{t(language, 'noAdjacent')}</p>}
            </div>
          </div>
        </div>
      )}
      <div className="bonus-toolbar">
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
          <div className="bonus-auto-confirm" role="group" aria-label={t(language, 'confirmGroup')}>
            <span>{t(language, 'confirmQuestion')}</span>
            <button className="ghost-button" disabled={disabled}
              onClick={() => act('run.auto')}>{t(language, 'confirm')}</button>
            <button className="ghost-button" onClick={() => setConfirmAuto(false)}>
              {t(language, 'cancel')}
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
