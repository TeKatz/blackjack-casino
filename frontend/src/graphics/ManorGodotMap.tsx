import type { ManorRoom } from '../bridge/protocol';
import { localizeRoom, t } from '../i18n';
import type { ManorLanguage } from '../i18n';
import {
  candleReveal, legalMapDestinations, mapCoordinates,
} from './manorGodotMapData';
import type { ManorMapSnapshot } from './manorGodotMapData';

type Props = {
  map: ManorMapSnapshot;
  language: ManorLanguage;
  hoveredRoom: number | null;
  onHover: (room: number | null) => void;
  large?: boolean;
  disabled?: boolean;
  onSelect?: (room: number) => void;
};

/** Always-revealed map. Edges and live availability are C# snapshot facts. */
export function ManorGodotMap({
  map, language, hoveredRoom, onHover, large = false, disabled = false, onSelect,
}: Props) {
  const rooms = new Map(map.rooms.map(room => [room.id, room]));
  const revealed = candleReveal(map.ghostClue, map.rooms);
  const available = legalMapDestinations(map);
  const visitedCount = map.rooms.filter(room => room.visited).length;
  const hover = hoveredRoom === null ? null : rooms.get(hoveredRoom);

  return (
    <div className={'manor-godot-map-content' + (large ? ' expanded' : '')}>
      <svg className="manor-godot-map-svg" viewBox="0 0 340 162"
        role="img" aria-label={t(language, 'godotMapCompleteLabel',
          visitedCount, map.rooms.length)}>
        <rect x="10" y="11" width="319" height="136" rx="7"
          className="manor-godot-map-outline" />
        <text x="22" y="23" className="manor-godot-map-compass" aria-hidden="true">N ↑</text>
        {map.passages.map(edge => {
          const from = rooms.get(edge.from);
          const to = rooms.get(edge.to);
          if (!from || !to) return null;
          const a = mapCoordinates(from);
          const b = mapCoordinates(to);
          const requiresKey = edge.requiresKey && !map.hasKey;
          const routeAvailable = (edge.from === map.playerRoom && to.available) ||
            (edge.to === map.playerRoom && from.available);
          const highlighted = hoveredRoom !== null &&
            (edge.from === hoveredRoom || edge.to === hoveredRoom) && routeAvailable;
          return (
            <g key={edge.from + ':' + edge.to}>
              <path d={'M' + a.x + ' ' + a.y + 'L' + b.x + ' ' + b.y}
                className={'manor-godot-map-edge' +
                  (requiresKey ? ' locked' : '') +
                  (routeAvailable ? ' available' : '') +
                  (highlighted ? ' highlighted' : '')} />
              {edge.requiresKey && <g
                transform={'translate(' + (a.x + b.x) / 2 + ' ' +
                  (a.y + b.y) / 2 + ')'}>
                <circle r="5.2" className={requiresKey ?
                  'manor-godot-map-lock' : 'manor-godot-map-unlocked'} />
                <path d={requiresKey ? 'M-2 -1V-2A2 2 0 0 1 2 -2V-1M-2.5 -1H2.5V3H-2.5Z' :
                  'M-2 -1V-2A2 2 0 0 1 2 -2M-2.5 -1H2.5V3H-2.5Z'}
                  className="manor-godot-map-lock-icon" />
              </g>}
            </g>
          );
        })}
        {map.rooms.map(room => {
          const pos = mapCoordinates(room);
          const here = room.id === map.playerRoom;
          const isGhost = revealed?.roomId === room.id;
          const hoverThis = hoveredRoom === room.id;
          const title = localizeRoom(room.name, language) + ' · ' +
            t(language, here ? 'currentRoom' :
              room.available ? 'nextMove' :
                room.visited ? 'alreadyVisited' : 'notReachable');
          return (
            <g key={room.id}
              className={'manor-godot-map-node' +
                (here ? ' here' : '') +
                (room.visited ? ' visited' : '') +
                (room.available ? ' reachable' : '') +
                (hoverThis ? ' hovered' : '') +
                (isGhost ? ' ghost-revealed' : '')}
              onMouseEnter={() => onHover(room.id)}
              onMouseLeave={() => onHover(null)}>
              <title>{title}</title>
              {here && <circle cx={pos.x} cy={pos.y} r="13"
                className="manor-godot-map-current-ring" />}
              {isGhost && <circle cx={pos.x} cy={pos.y} r="15"
                className="manor-godot-map-ghost-ring" />}
              <rect x={pos.x - 9} y={pos.y - 8} width="18" height="16" rx="3"
                className="manor-godot-map-room" />
              <text x={pos.x} y={pos.y + 3.3} textAnchor="middle"
                className="manor-godot-map-index">{room.id === 16 ? 'M' :
                  String(room.id + 1)}</text>
            </g>
          );
        })}
      </svg>
      <div className="manor-godot-map-summary" aria-live="polite">
        {hover ? <span>{localizeRoom(hover.name, language)}</span> :
          <span>{t(language, 'godotMapExplored', visitedCount, map.rooms.length)}</span>}
        {revealed && <span className="manor-godot-map-ghost-note">
          {t(language, 'godotGhostLocated', localizeRoom(revealed.name, language))}
        </span>}
      </div>
      {large && <>
        <div className="manor-godot-map-legend">
          <span><i className="map-current" />{t(language, 'mapPlayer')}</span>
          <span><i className="map-available" />{t(language, 'mapAvailable')}</span>
          <span><i className="map-visited" />{t(language, 'mapVisited')}</span>
          <span><i className="map-locked" />{t(language, 'mapBlocked')}</span>
        </div>
        <div className="manor-godot-map-route-heading">
          <strong>{t(language, 'godotMapAllRooms')}</strong>
          <span>{t(language, 'godotMapRouteCount', available.length)}</span>
        </div>
        <div className="manor-godot-map-room-list">
          {map.rooms.map((room: ManorRoom) => {
            const here = room.id === map.playerRoom;
            return (
              <button key={room.id} type="button"
                className={'manor-godot-map-route' +
                  (here ? ' here' : '') +
                  (room.available ? ' available' : '') +
                  (room.id === hoveredRoom ? ' hovered' : '')}
                disabled={disabled || !room.available}
                onClick={() => onSelect?.(room.id)}
                onMouseEnter={() => onHover(room.id)}
                onMouseLeave={() => onHover(null)}
                aria-label={localizeRoom(room.name, language) + ', ' +
                  t(language, here ? 'currentRoom' : room.available ?
                    'nextMove' : room.visited ? 'alreadyVisited' : 'notReachable')}>
                <span className="manor-godot-route-index">{String(room.id + 1).padStart(2, '0')}</span>
                <span>{localizeRoom(room.name, language)}</span>
                <small>{here ? t(language, 'here') : room.available ?
                  t(language, 'enter') : room.visited ?
                    t(language, 'explored') : '—'}</small>
              </button>
            );
          })}
        </div>
      </>}
    </div>
  );
}
