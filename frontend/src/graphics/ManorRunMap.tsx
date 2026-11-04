import { useEffect, useRef } from 'react';
import type { ManorRoom, ManorRun } from '../bridge/protocol';
import { availableManorRooms, mapScrollLeft } from './manorRunUi';

type Props = { run: ManorRun; disabled: boolean; onMove: (room: number) => void };
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

export function ManorRunMap({ run, disabled, onMove }: Props) {
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
      <div className="manor-map-guide" aria-label="Map legend">
        <span><i className="map-key player" /> Your location</span>
        <span><i className="map-key available" /> Available move</span>
        <span><i className="map-key visited" /> Explored</span>
        <span><i className="map-key blocked" /> Requires key</span>
      </div>
      <div className="manor-route-chooser" role="group"
        aria-label="Rooms available for your next move">
        <div className="manor-route-heading">
          <strong>CHOOSE YOUR NEXT ROOM</strong>
          <span>{moves.length} {moves.length === 1 ? 'passage' : 'passages'} accessible</span>
        </div>
        <div className="manor-route-list">
          {moves.map(room => (
            <button key={room.id} type="button" className="manor-route-choice"
              disabled={disabled} onClick={() => onMove(room.id)}
              aria-label={'Move to ' + room.name + (room.visited ? ', already explored' : ', unexplored')}>
              <span aria-hidden="true">⌑</span>
              <span>{room.name}<small>{room.visited ? 'ALREADY EXPLORED' : 'NOT YET EXPLORED'}</small></span>
              <strong>ENTER</strong>
            </button>
          ))}
          {moves.length === 0 && <p className="manor-route-empty">
            No adjacent room is available. Review your current run options.</p>}
        </div>
      </div>
      <div className="map-scroll" ref={viewport} role="region"
        aria-label="Scrollable Manor floorplan (use horizontal scroll to inspect rooms)"
        tabIndex={0}>
        <div className="manor-map-scene">
          <div className="map-moon" aria-hidden="true" />
          <div className="map-watermark" aria-hidden="true">MIDNIGHT MANOR · FLOORPLAN</div>
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
            const state = here ? 'current room' : room.available ? 'legal next move' :
              locked ? 'needs the key' : room.visited ? 'already visited' : 'not reachable now';
            return (
              <button key={room.id} type="button"
                style={{ left: position.x, top: position.y }}
                className={'manor-map-room ' + room.area.toLowerCase() +
                  (here ? ' here' : '') + (room.available ? ' available' : '') +
                  (room.visited ? ' explored' : '') + (isGate ? ' exit-gate' : '')}
                disabled={disabled || !room.available}
                title={room.name + ' · ' + state}
                aria-label={room.name + ', ' + state}
                aria-current={here ? 'location' : undefined}
                onClick={() => onMove(room.id)}>
                <span className="map-room-sigil" aria-hidden="true">
                  {here ? '◆' : isGate ? '☾' : SIGILS[room.area] ?? '◇'}
                </span>
                <span className="map-room-name">{room.name}</span>
                <span className="map-room-zone">{here ? 'YOU ARE HERE' :
                  room.available ? 'ENTER' : FLOOR_NAMES[room.area] ?? room.area.toUpperCase()}</span>
              </button>
            );
          })}
        </div>
      </div>
      <p className="map-privacy-note">The map follows your current room. Use the accessible
        destination buttons above when the floorplan is wider than your window.
        Only passages and moves authorized by the Manor engine are shown.
        The ghost remains hidden unless your candle reveals it.</p>
    </div>
  );
}
