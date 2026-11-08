import { describe, expect, it } from 'vitest';
import { fallbackLobby, isLobbySnapshot } from '../src/lobby/lobbyBridge';

describe('Modern lobby bridge: read-only C# snapshots', () => {
  it('accepts the nine-mode offline preview and every mode identifier is unique', () => {
    expect(isLobbySnapshot(fallbackLobby)).toBe(true);
    expect(fallbackLobby.modes).toHaveLength(9);
    expect(new Set(fallbackLobby.modes.map(mode => mode.id)).size).toBe(9);
    expect(fallbackLobby.modes.map(mode => mode.id)).toEqual([
      'blackjack', 'kniffel', 'chill-lounge', 'casino-empire', 'midnight-reels',
      'neon-vault', 'nine-lives', 'midnight-manor', 'midnight-merge',
    ]);
  });

  it('rejects counterfeit snapshots before presentation state is replaced', () => {
    expect(isLobbySnapshot({ ...fallbackLobby, bankroll: 5000 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, coins: 1115 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, seasonFraction: 1.5 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, challengeFraction: NaN })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, vaultFraction: -0.2 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, language: 'fr' })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, musicVolume: Number.NaN })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, musicVolume: 1.2 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, musicEnabled: 'yes' })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, showEmpireBalance: null })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, albumPacksReady: -1 })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, albumPacksReady: '3' })).toBe(false);
    expect(isLobbySnapshot({ ...fallbackLobby, albumPacksReady: 0, challengesReady: 2 })).toBe(true);
    expect(isLobbySnapshot({ ...fallbackLobby, albumPacksReady: 3, challengesReady: 2 })).toBe(true);
    expect(isLobbySnapshot({ ...fallbackLobby, modes: [] })).toBe(true);
    expect(isLobbySnapshot({
      ...fallbackLobby, modes: fallbackLobby.modes.map((m, i) =>
        i === 0 ? { ...m, id: '../host' } : m),
    })).toBe(false);
    expect(isLobbySnapshot({
      ...fallbackLobby, modes: fallbackLobby.modes.map((m, i) =>
        i === 0 ? { ...m, enabled: 'yes' } : m),
    })).toBe(false);
  });

  it('never treats secondary actions as currency or payout requests', () => {
    const permittedFields = Object.keys(fallbackLobby).sort();
    expect(permittedFields).not.toContain('payout');
    expect(permittedFields).not.toContain('spinResult');
    expect(permittedFields).not.toContain('saveData');
    expect(fallbackLobby.modes.every(mode => typeof mode.enabled === 'boolean')).toBe(true);
  });
});
