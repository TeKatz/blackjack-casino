import { useEffect, useRef } from 'react';
import type { ManorRoom, ManorRun } from '../bridge/protocol';
import { availableManorRooms, mapScrollLeft } from './manorRunUi';
import { localizeArea, localizeRoom, t } from '../i18n';
import type { ManorLanguage } from '../i18n';

type Props = { run: ManorRun; disabled: boolean; onMove: (room: number) => void; language: ManorLanguage };
const FLOOR_NAMES: Record<string, string> = {
  Entrance: 'ENTRANCE', Library: 'LIBRARY', Ballroom: 'BALLROOM',
  Corridor: 'CORRIDOR', Attic: 'ATTIC', Crypt: 'FORBIDDEN',
};
const SIGILS: Record<string, string> = {
  Entrance: '✧', Library: '✥', Ballroom: '◇',
  Corridor: '⌁', Attic: '△', Crypt: '✦',
};

// Position is presentation-only. Connected edges and legal moves are C# facts.
export function roomPosition(room: Pick<ManorRoom, 'x' | 'y'>): { x: number; y: number } {
  return { x: 70 + room.x * 123, y: 70 + (room.y - 1) * 134 };
}

export function ManorRunMap({ run, disabled, onMove, language }: Props) {
  const rooms = new Map(run.rooms.map(room => [room.id, room]));
  const viewport = useRef<HTMLDivElement>(null);
  const moves = availableManorRooms(run);
  useEffect(() => {
    const scroll = viewport.current;
    const current = rooms.get(run.playerRoom);
    if (scroll && current) {
      scroll.scrollLeft = mapScrollLeft(roomPosition(current).x,
        scroll.clientWidth, scroll.scrollWidth);
    }
    // Only recenter after a confirmed player move, not during status refreshes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [run.playerRoom]);
  return (
    <div className="manor-map-wrap">
      <div className="manor-map-guide" aria-label={t(language, 'mapLegend')}>
        <span><i className="map-key player" /> {t(language, 'mapPlayer')}</span>
        <span><i className="map-key available" /> {t(language, 'mapAvailable')}</span>
        <span><i className="map-key visited" /> {t(language, 'mapVisited')}</span>
        <span><i className="map-key blocked" /> {t(language, 'mapBlocked')}</span>
      </div>
      <div className="manor-route-chooser" role="group"
        aria-label={t(language, 'chooseRoom')}>
        <div className="manor-route-heading">
          <strong>{t(language, 'chooseRoom')}</strong>
          <span>{moves.length} {t(language, moves.length === 1 ? 'passageSingular' : 'passagePlural')} {t(language, 'accessible')}</span>
        </div>
        <div className="manor-route-list">
          {moves.map(room => (
            <button key={room.id} type="button" className="manor-route-choice"
              disabled={disabled} onClick={() => onMove(room.id)}
              aria-label={t(language, 'moveTo', localizeRoom(room.name, language)) +
                ', ' + t(language, room.visited ? 'explored' : 'unexplored')}>
              <span aria-hidden="true">⌑</span>
              <span>{localizeRoom(room.name, language)}<small>{t(language, room.visited ? 'explored' : 'unexplored')}</small></span>
              <strong>{t(language, 'enter')}</strong>
            </button>
          ))}
          {moves.length === 0 && <p className="manor-route-empty">
            {t(language, 'noAdjacent')}</p>}
        </div>
      </div>
      <div className="map-scroll" ref={viewport} role="region"
        aria-label={t(language, 'mapRegion')}
        tabIndex={0}>
        <div className="manor-map-scene">
          <div className="map-moon" aria-hidden="true" />
          <div className="map-watermark" aria-hidden="true">{t(language, 'floorplan')}</div>
          <svg className="map-passages" viewBox="0 0 1000 408" aria-hidden="true">
            {run.passages.map(edge => {
              const from = rooms.get(edge.from);
              const to = rooms.get(edge.to);
              if (!from || !to) return null;
              const a = roomPosition(from);
              const b = roomPosition(to);
              const blocked = edge.requiresKey && !run.hasKey;
              const traversable = (edge.from === run.playerRoom && to.available) ||
                (edge.to === run.playerRoom && from.available);
              return (
                <g key={edge.from + '-' + edge.to}>
                  <path d={`M${a.x} ${a.y} L${b.x} ${b.y}`}
                    className={'map-passage' +
                      (blocked ? ' sealed' : '') +
                      (traversable ? ' route-ready' : '')} />
                  {edge.requiresKey && (
                    <g transform={`translate(${(a.x + b.x) / 2} ${(a.y + b.y) / 2})`}>
                      <circle r="11" className={blocked ? 'map-lock-dot sealed' : 'map-lock-dot opened'} />
                      <path d={blocked ? 'M-4 -1 V-5 A4 4 0 0 1 4 -5 V-1 M-5 -1 H5 V6 H-5 Z' :
                        'M-4 -1 V-5 A4 4 0 0 1 4 -5 M-5 -1 H5 V6 H-5 Z'}
                        className="map-lock-icon" />
                    </g>
                  )}
                </g>
              );
            })}
          </svg>
          {run.rooms.map(room => {
            const here = room.id === run.playerRoom;
            const position = roomPosition(room);
            const isGate = room.id === 16;
            const locked = room.locked && !run.hasKey;
            const state = t(language, here ? 'currentRoom' : room.available ? 'nextMove' :
              locked ? 'needsKey' : room.visited ? 'alreadyVisited' : 'notReachable');
            return (
              <button key={room.id} type="button"
                style={{ left: position.x, top: position.y }}
                className={'manor-map-room ' + room.area.toLowerCase() +
                  (here ? ' here' : '') + (room.available ? ' available' : '') +
                  (room.visited ? ' explored' : '') + (isGate ? ' exit-gate' : '')}
                disabled={disabled || !room.available}
                title={localizeRoom(room.name, language) + ' · ' + state}
                aria-label={localizeRoom(room.name, language) + ', ' + state}
                aria-current={here ? 'location' : undefined}
                onClick={() => onMove(room.id)}>
                <span className="map-room-sigil" aria-hidden="true">
                  {here ? '◆' : isGate ? '☾' : SIGILS[room.area] ?? '◇'}
                </span>
                <span className="map-room-name">{localizeRoom(room.name, language)}</span>
                <span className="map-room-zone">{here ? t(language, 'here') :
                  room.available ? t(language, 'enter') :
                    language === 'de' ? localizeArea(room.area, language).toUpperCase() :
                      FLOOR_NAMES[room.area] ?? room.area.toUpperCase()}</span>
              </button>
            );
          })}
        </div>
      </div>
      <p className="map-privacy-note">{t(language, 'mapPrivacy')}</p>
    </div>
  );
}
