import { describe, expect, it } from 'vitest';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { ManorGodotMap } from '../src/graphics/ManorGodotMap';
import {
  candleReveal, makeDemoMap, legalMapDestinations, mapCoordinates,
} from '../src/graphics/manorGodotMapData';
import { demoState, liveState } from '../src/graphics/manorGodotProtocol';

const noop = () => {};

describe('always-revealed 3D Manor map', () => {
  it('renders all 17 rooms and all 23 original C# connections from the start', () => {
    const map = makeDemoMap(0);
    expect(map.rooms.map(room => room.id)).toEqual(Array.from({ length: 17 }, (_, i) => i));
    expect(map.passages).toHaveLength(23);
    expect(map.rooms.filter(room => room.visited).map(room => room.id)).toEqual([0]);
    const html = renderToStaticMarkup(createElement(ManorGodotMap, {
      map, language: 'en', hoveredRoom: null, onHover: noop,
    }));
    expect((html.match(/class="manor-godot-map-node/g) ?? [])).toHaveLength(17);
    expect((html.match(/class="manor-godot-map-edge/g) ?? [])).toHaveLength(23);
    expect(html).toContain('Full Manor floorplan, 1 of 17 rooms visited');
    expect(html).toContain('Moonlit Gate');
    expect(html).toContain('manor-godot-map-edge locked');
  });

  it('updates visited location while keeping the full map uncovered', () => {
    const map = makeDemoMap(4, [0, 4]);
    expect(map.playerRoom).toBe(4);
    expect(map.rooms.find(room => room.id === 0)?.visited).toBe(true);
    expect(map.rooms.find(room => room.id === 4)?.visited).toBe(true);
    expect(map.rooms.find(room => room.id === 16)?.visited).toBe(false);
    expect(legalMapDestinations(map).map(room => room.id)).toEqual([0, 1, 5]);
  });

  it('preserves a fourth legal exit through the enlarged map', () => {
    const map = makeDemoMap(1, [0, 1]);
    expect(legalMapDestinations(map).map(room => room.id)).toEqual([0, 2, 4, 7]);
    expect(demoState(1).options.map(choice => choice.id)).toEqual([0, 2, 4]);
    const run = {
      status: 'Hunting', message: 'You enter Servants Corridor.', ghostClue: '',
      playerRoom: 1, turn: 1, exitTarget: 250, current: '10×', banked: '5×',
      hasCandle: false, hasKey: false, hasCharm: false,
      rooms: map.rooms, passages: map.passages,
    };
    expect(liveState(run).options.map(choice => choice.id)).toEqual([0, 2, 4]);
    expect(legalMapDestinations(run).some(room => room.id === 7)).toBe(true);
    const html = renderToStaticMarkup(createElement(ManorGodotMap, {
      map: run, language: 'en', hoveredRoom: 7, onHover: noop,
      large: true, onSelect: noop,
    }));
    expect(html).toContain('Ballroom');
    expect(html).toContain('ROOM DIRECTORY');
    expect(html).toContain('4 accessible exits');
  });

  it('reveals exact ghost location only when the candle clue from C# allows it', () => {
    const rooms = makeDemoMap(0).rooms;
    expect(candleReveal('Cold air creeps from the east. The ghost is CLOSE (within two rooms).', rooms))
      .toBeNull();
    expect(candleReveal('Candle reveals: Library.', rooms)).toEqual({
      roomId: 4, name: 'Library',
    });
    expect(candleReveal('Candle reveals: Rear Foyer.', rooms)).toEqual({
      roomId: null, name: 'Rear Foyer',
    });
    const html = renderToStaticMarkup(createElement(ManorGodotMap, {
      map: { ...makeDemoMap(0), ghostClue: 'Candle reveals: Library.' },
      language: 'de', hoveredRoom: null, onHover: noop,
    }));
    expect(html).toContain('ghost-revealed');
    expect(html).toContain('Geist: Bibliothek');
  });

  it('highlights real target room and connected legal corridor on door hover', () => {
    expect(mapCoordinates({ x: 0, y: 2 })).toEqual({ x: 31, y: 79 });
    expect(mapCoordinates({ x: 7, y: 2 })).toEqual({ x: 304, y: 79 });
    const html = renderToStaticMarkup(createElement(ManorGodotMap, {
      map: makeDemoMap(0), language: 'en', hoveredRoom: 4, onHover: noop,
    }));
    expect(html).toContain('manor-godot-map-node reachable hovered');
    expect(html).toContain('manor-godot-map-edge available highlighted');
  });
});
