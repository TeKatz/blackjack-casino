import { describe, expect, it } from 'vitest';
import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { ManorBonus3D } from '../src/graphics/ManorBonus3D';
import {
  demoState, liveState, isGodotMessage, isAllowedGodotChoice,
} from '../src/graphics/manorGodotProtocol';
import type { ManorRun } from '../src/bridge/protocol';

const sample: ManorRun = {
  status: 'Hunting', message: '', ghostClue: '', playerRoom: 0, turn: 0,
  exitTarget: 250, current: '5×', banked: '5×', hasCandle: false,
  hasKey: false, hasCharm: false, rooms: [
    { id: 0, name: 'Entrance Hall', x: 0, y: 2, area: 'Entrance', visited: false, available: false, locked: false },
    { id: 1, name: "Servants' Corridor", x: 1, y: 2, area: 'Corridor', visited: false, available: true, locked: false },
    { id: 4, name: 'Library', x: 1, y: 1, area: 'Library', visited: false, available: true, locked: false },
    { id: 7, name: 'Ballroom', x: 1, y: 3, area: 'Ballroom', visited: false, available: true, locked: false },
    { id: 16, name: 'Moonlit Gate', x: 7, y: 2, area: 'Entrance', visited: false, available: false, locked: true },
  ], passages: [],
};

describe('Godot 3D Manor bonus bridge', () => {
  it('only proposes rooms the authoritative C# snapshot marked available', () => {
    const state = liveState(sample);
    expect(state.roomId).toBe(0);
    expect(state.options.map(x => x.id)).toEqual([1, 4, 7]);
    expect(isAllowedGodotChoice(state, 16)).toBe(false);
    expect(isAllowedGodotChoice(state, 4)).toBe(true);
  });
  it('keeps the no-bankroll demo separate from an actual run', () => {
    const state = demoState(0);
    expect(state.demo).toBe(true);
    expect(state.roomId).toBe(0);
    expect(demoState(8).roomId).toBe(8);
    expect(state.options).toHaveLength(3);
    expect(state.options.map(x => x.id)).toEqual([1, 4, 7]);
    expect(demoState(4).options.some(x => x.id === 5)).toBe(true);
  });
  it('rejects spoofed and malformed iframe messages', () => {
    expect(isGodotMessage({ source: 'manor-godot', type: 'ready' })).toBe(true);
    expect(isGodotMessage({ source: 'manor-godot', type: 'choice', room: 7 })).toBe(true);
    expect(isGodotMessage({ source: 'manor-godot', type: 'hover', room: 7 })).toBe(true);
    expect(isGodotMessage({ source: 'manor-godot', type: 'hover', room: null })).toBe(true);
    expect(isGodotMessage({ source: 'manor-godot', type: 'hover', room: 17 })).toBe(false);
    expect(isGodotMessage({ source: 'manor-godot', type: 'choice', room: 17 })).toBe(false);
    expect(isGodotMessage({ source: 'manor-godot', type: 'choice', room: '7' })).toBe(false);
    expect(isGodotMessage({ source: 'manor-react', type: 'choice', room: 7 })).toBe(false);
    expect(isGodotMessage({ source: 'manor-godot', type: 'choice', room: 7, payout: '999×' })).toBe(false);
  });
  it('keeps the 3D bonus embedded in the machine surface with safe explicit exit', () => {
    const markup = renderToStaticMarkup(createElement(ManorBonus3D, {
      active: true, demo: true, run: sample, language: 'de', reducedMotion: true, disabled: false,
      onAction: () => {}, onExitDemo: () => {},
    }));
    expect(markup).toContain('manor-godot-frame');
    expect(markup).toContain('./manor3d/index.html');
    expect(markup).toContain('ÜBUNG · OHNE EINSATZ');
    expect(markup).toContain('Übung verlassen');
    expect(markup).not.toContain('TEST BONUS GAME');
    expect(markup).not.toContain('Normale 2D-Ansicht');
    expect(markup).toContain('is-active');
    expect(markup).toContain('phase-loading');
    expect(markup).not.toContain('manor-godot-portal');
    expect(markup).toContain('manor-godot-hud-left');
    expect(markup).toContain('manor-godot-hud-map');
    expect(markup).toContain('manor-godot-inventory');
    // The iframe has not sent 'ready' in SSR. Door choices appear only
    // after the Godot bridge is ready, never over a loading screen.
    expect(markup).not.toContain('manor-godot-door-actions');
    expect(markup).toContain('MANOR-GRUNDRISS');
    expect(markup).toContain('UNBEKANNTE PRÄSENZ');
    expect(markup).toContain('Jetzt auszahlen');
  });
  it('does not retain any iframe or WebGL context when inactive', () => {
    const markup = renderToStaticMarkup(createElement(ManorBonus3D, {
      active: false, demo: false, run: null, language: 'en', reducedMotion: false,
      disabled: false, onAction: () => {}, onExitDemo: () => {},
    }));
    expect(markup).toContain('is-warming');
    expect(markup).toContain('phase-hidden');
    expect(markup).toContain('aria-hidden="true"');
    expect(markup).not.toContain('manor-godot-frame');
    expect(markup).not.toContain('manor-godot-door-actions');
    expect(markup).not.toContain('Back to the slot');
  });

  it('presents committed C# reward without any possibility of a second payout', () => {
    const outcome = {
      status: 'Caught' as const, message: 'The ghost catches you. Checkpoint pays 25×.',
      multiplier: '25×', award: '$625.00',
    };
    const markup = renderToStaticMarkup(createElement(ManorBonus3D, {
      active: true, demo: false, run: sample, conclusion: outcome,
      language: 'de', reducedMotion: false, disabled: false,
      onAction: () => {}, onExitDemo: () => {},
      onCloseResult: () => {},
    }));
    expect(markup).toContain('DER GEIST HAT DICH GEFUNDEN');
    expect(markup).toContain('$625.00');
    expect(markup).toContain('manor-godot-conclusion');
    // The 3D graphics process is already released while the player reads the receipt.
    expect(markup).not.toContain('manor-godot-frame');
    expect(markup).toContain('Zurück zu den Walzen');
    expect(markup).not.toContain('manor-godot-door-actions');
  });

  it('shows a real C# practice run, including ghost and collected item status', () => {
    const practiceRun: ManorRun = {
      ...sample, turn: 2, playerRoom: 4, hasKey: true, hasCandle: true,
      current: '15×', banked: '15×',
      message: 'You enter Library. +10×. Checkpoint secured. Candle found.',
      ghostClue: 'A door slams to the east. The ghost is approaching.',
    };
    const markup = renderToStaticMarkup(createElement(ManorBonus3D, {
      active: true, demo: true, run: practiceRun, language: 'en',
      reducedMotion: true, disabled: false,
      onAction: () => {}, onExitDemo: () => {},
    }));
    expect(markup).toContain('PRACTICE · NO WAGER');
    expect(markup).toContain('15×');
    expect(markup).toContain('manor-godot-ghost-presence approaching');
    expect(markup).toContain('Candle');
    expect(markup).toContain('manor-godot-item owned usable');
    expect(markup).toContain('Cash out now');
  });

  it('shows live C# reward, risk and inventory without obscuring 3D doors', () => {
    const run: ManorRun = {
      ...sample, current: '80×', banked: '25×', turn: 7,
      hasKey: true, hasCandle: true, hasCharm: true,
      message: 'You enter Library. +10×. Checkpoint secured.',
      ghostClue: 'Cold air creeps from the east. The ghost is CLOSE (within two rooms).',
    };
    const markup = renderToStaticMarkup(createElement(ManorBonus3D, {
      active: true, demo: false, run, language: 'de', reducedMotion: false,
      disabled: false, onAction: () => {}, onExitDemo: () => {},
    }));
    expect(markup).toContain('80×');
    expect(markup).toContain('25×');
    expect(markup).toContain('ZUG 7 / 24');
    expect(markup).toContain('GEIST IN DER NÄHE');
    expect(markup).toContain('manor-godot-payout-panel');
    expect(markup).toContain('manor-godot-threat');
    expect(markup).toContain('manor-godot-map-room');
    expect(markup).toContain('Schlüssel');
    expect(markup).toContain('Kerze');
    expect(markup).toContain('Schutzzauber');
    expect(markup).toContain('Jetzt auszahlen');
    expect(markup).not.toContain('Normale 2D-Ansicht');
  });

});
