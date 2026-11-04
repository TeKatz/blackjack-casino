import type { ManorRoom, ManorRun } from '../bridge/protocol';

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
  return (
    <div className="manor-map-wrap">
      <div className="manor-map-guide" aria-label="Map legend">
        <span><i className="map-key player" /> Your location</span>
        <span><i className="map-key available" /> Available move</span>
        <span><i className="map-key visited" /> Explored</span>
        <span><i className="map-key blocked" /> Requires key</span>
      </div>
      <div className="map-scroll" role="region" aria-label="Scrollable Manor floorplan" tabIndex={0}>
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
      <p className="map-privacy-note">Only passages and moves authorized by the Manor engine are shown.
        The ghost remains hidden unless your candle reveals it.</p>
    </div>
  );
}
