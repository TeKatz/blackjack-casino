import { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  availableActions, browserPreview, sendAction, sendCommand, subscribeToHost,
} from './bridge/protocol';
import type { GameAction, ManorSnapshot } from './bridge/protocol';
import { ManorStage } from './graphics/ManorStage';
import './styles.css';

const embedded = Boolean(window.chrome?.webview);
const defaultBet = (formatted: string): string =>
  formatted.replace(/[^0-9.]/g, '').split('.')[0] || '25';

function ManorApp() {
  const [snapshot, setSnapshot] = useState<ManorSnapshot>(browserPreview);
  const [connected, setConnected] = useState(!embedded);
  const [error, setError] = useState<string | null>(null);
  const [fatal, setFatal] = useState(false);
  const [pending, setPending] = useState<string | null>(null);
  const pendingRef = useRef<string | null>(null);
  const [bet, setBet] = useState('25');
  const [ambient, setAmbient] = useState(true);
  const [lowMotion, setLowMotion] = useState(
    window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const [confirmAuto, setConfirmAuto] = useState(false);

  useEffect(() => {
    const applySnapshot = (next: ManorSnapshot) => {
      // Late WebView2 replies must never roll the displayed revision backward.
      setSnapshot(previous => BigInt(next.revision) >= BigInt(previous.revision) ? next : previous);
      setBet(defaultBet(next.selectedBet));
      setConnected(true);
    };
    const stop = subscribeToHost(message => {
      if (message.type === 'snapshot') {
        applySnapshot(message.payload);
        // An explicit recovery snapshot can reconcile a lost response after a timeout.
        if (pendingRef.current === message.requestId || pendingRef.current === 'recovering') {
          pendingRef.current = null;
          setPending(null);
        }
        setError(null);
      } else {
        applySnapshot(message.payload.snapshot);
        if (pendingRef.current === message.requestId) {
          pendingRef.current = null;
          setPending(null);
        }
        setError(message.payload.message);
        if (message.payload.fatal) setFatal(true);
      }
    });
    sendCommand('client.ready');
    return stop;
  }, []);

  const actions = availableActions(snapshot);
  const motionOff = lowMotion || !ambient || !snapshot.animationsEnabled;
  const disabled = !embedded || !connected || pending !== null || fatal;
  const validBet = /^[0-9]{1,4}$/.test(bet) && Number(bet) >= 1 && Number(bet) <= 1000;
  const run = snapshot.run;

  function act(type: GameAction, extras: { bet?: string; room?: number } = {}) {
    // Immediate ref guard prevents two same-frame clicks from placing two wagers.
    if (disabled || pendingRef.current !== null || !actions.includes(type)) return;
    if (type === 'spin.paid' && !validBet) {
      setError('Choose a whole-dollar bet between $1 and $1,000.');
      return;
    }
    setConfirmAuto(false);
    setError(null);
    const requestId = sendAction(type, { expectedRevision: snapshot.revision, ...extras });
    pendingRef.current = requestId;
    setPending(requestId);
    // Do not resend a timed-out wager. Ask the C# host for its committed state.
    window.setTimeout(() => {
      if (pendingRef.current === requestId) {
        pendingRef.current = 'recovering';
        setError('The reply is taking longer than expected. Reading the committed state; do not repeat your action.');
        sendCommand('snapshot.request');
      }
    }, 12000);
  }

  return (
    <main className="manor-app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">☾</span>
          <div><span className="eyebrow">THE MIDNIGHT CLUB</span><h1>MIDNIGHT MANOR</h1></div>
        </div>
        <div className="topbar-actions">
          <span className={connected ? 'connection ready' : 'connection'}>
            {connected && embedded ? 'C# ENGINE · CONNECTED' :
              embedded ? 'CONNECTING TO CASINO' : 'BROWSER DESIGN PREVIEW'}
          </span>
          <button className="ghost-button" onClick={() =>
            embedded ? sendCommand('ui.close') : window.close()}>Back to lobby</button>
        </div>
      </header>

      <section className="headline">
        <div>
          <span className="eyebrow">THE HOUSE REMEMBERS EVERY CHOICE</span>
          <h2>The house knows your name.</h2>
          <p>Every spin, payout and room choice comes from the original C# game engine.
            Your balance and ongoing features are committed to the same saved game as classic Manor.</p>
        </div>
        <div className="stage-tag"><span className="tag-dot" /> MIGRATION / PHASE 2</div>
      </section>

      <div className="content-grid">
        <section className="machine" aria-label="Manor reel game">
          <div className="machine-title">
            <span>THE HAUNTED FIVE</span>
            <span>20 FIXED PAYLINES · 5 × 3</span>
          </div>
          <ManorStage snapshot={snapshot} reducedMotion={motionOff} />
          <div className="machine-footer">
            <span>{snapshot.hasCommittedSpin
              ? snapshot.lastSpinFree ? 'LAST HAUNTED NIGHT FREE SPIN' : 'LAST PAID SPIN'
              : 'NO STORED SPIN · PRESENTATION STAND-IN'}</span>
            <span>{snapshot.winningLines} PAID LINES · {snapshot.scatters} MOONS · {snapshot.doors} DOORS</span>
          </div>
          <div className="action-console">
            <div className="stake-control">
              <label htmlFor="manor-bet">BET · PLAY MONEY</label>
              <div className="bet-input">
                <span>$</span>
                <input id="manor-bet" type="number" inputMode="numeric"
                  min={1} max={1000} step={1} value={bet}
                  onChange={event => setBet(event.target.value)}
                  disabled={disabled || !snapshot.canSpin} />
              </div>
            </div>
            <div className="main-actions">
              {snapshot.canFreeSpin ?
                <button className="gold-button" disabled={disabled}
                  onClick={() => act('spin.free')}>PLAY FREE SPIN · {snapshot.freeSpinsRemaining} LEFT</button> :
                <button className="gold-button" disabled={disabled || !snapshot.canSpin || !validBet}
                  onClick={() => act('spin.paid', { bet })}>
                  {pending ? 'SAVING RESULT…' : 'SPIN THE REELS'}
                </button>}
              <span className="action-hint">{run ? 'The Manor is open · choose your path below' :
                snapshot.canFreeSpin ? 'No extra wager is charged for free spins' :
                snapshot.cycleActive ? 'Resolve the current feature before a new wager' :
                'C# determines and saves the result before the reels display it'}</span>
            </div>
          </div>

          {run && (
            <section className="labyrinth" aria-label="Escape the Manor">
              <div className="labyrinth-heading">
                <div><span className="eyebrow">INTERACTIVE BONUS</span><h3>Escape the Manor</h3></div>
                <div className="turn-counter">TURN {run.turn} / 24</div>
              </div>
              <p className="run-message">{run.message}</p>
              <p className="ghost-warning">{run.ghostClue}</p>
              <div className="run-summary">
                <span>Potential <strong>{run.current}</strong></span>
                <span>Checkpoint <strong>{run.banked}</strong></span>
                <span>Exit <strong>{run.exitTarget}×</strong></span>
                <span>{run.hasKey ? 'KEY FOUND' : 'NO KEY'}</span>
                <span>{run.hasCharm ? 'CHARM READY' : 'NO CHARM'}</span>
              </div>
              <p className="map-instructions">
                Your room is marked in gold. Click an illuminated adjacent room to move.
                Blocked passages require a key. Room choices and prizes are committed immediately.
              </p>
              <div className="map-scroll" role="region" aria-label="Manor room map" tabIndex={0}>
                <div className="room-grid">
                  {run.rooms.map(room => (
                    <button key={room.id}
                      style={{ gridColumn: room.x + 1, gridRow: room.y + 1 }}
                      className={'room-node' +
                        (room.id === run.playerRoom ? ' current' : '') +
                        (room.available ? ' available' : '') +
                        (room.visited ? ' visited' : '')}
                      title={room.locked ? room.name + ' · requires key' : room.name}
                      disabled={disabled || !room.available}
                      onClick={() => act('run.move', { room: room.id })}>
                      <span className="room-emblem">{room.id === run.playerRoom ? '◆' :
                        room.available ? '◇' : room.visited ? '·' : '◌'}</span>
                      <span>{room.name}</span>
                    </button>
                  ))}
                </div>
              </div>
              <div className="run-actions">
                <button className="ghost-button" disabled={disabled || !run.hasCandle}
                  onClick={() => act('run.candle')}>Use candle</button>
                <button className="ghost-button" disabled={disabled}
                  onClick={() => act('run.follow')}>Follow safest route · 1 step</button>
                <button className="ghost-button" disabled={disabled}
                  onClick={() => act('run.cashout')}>Cash out now</button>
                {!confirmAuto ? (
                  <button className="ghost-button" disabled={disabled}
                    onClick={() => setConfirmAuto(true)}>Auto-resolve entire run</button>
                ) : (
                  <div className="confirm-auto" role="group" aria-label="Confirm automatic resolution">
                    <span>Finish the whole run automatically?</span>
                    <button className="ghost-button" onClick={() => act('run.auto')}
                      disabled={disabled}>Confirm</button>
                    <button className="ghost-button" onClick={() => setConfirmAuto(false)}>Cancel</button>
                  </div>
                )}
              </div>
            </section>
          )}
        </section>

        <aside className="sidebar" aria-label="Game status">
          <div className="status-card">
            <span className="eyebrow">YOUR MANOR ACCOUNT</span>
            <h3>Under the chandelier</h3>
            <div className="money-stat"><span>Balance</span><strong>{snapshot.bankroll}</strong></div>
            <div className="money-stat"><span>Last stake</span><strong>{snapshot.bet}</strong></div>
            <div className="money-stat"><span>Last reel return</span><strong>{snapshot.payout}</strong></div>
            <div className="money-stat"><span>Current cycle return</span><strong>{snapshot.cycleReturned}</strong></div>
          </div>
          <div className="status-card feature-card">
            <span className="eyebrow">THE HOUSE IS WATCHING</span>
            <h3>{snapshot.feature}</h3>
            <p>{run ? 'Choose your rooms, reveal the ghost with a candle, bank a checkpoint or leave with your current reward.' :
              snapshot.canFreeSpin ? 'Continue the saved free spins at your own pace. The same C# payout table is used.' :
              snapshot.cycleActive ? 'A committed bonus is still being resolved.' :
              'The existing C# engine controls all reels, payouts and bonus triggers.'}</p>
          </div>
          <div className="status-card">
            <span className="eyebrow">DISPLAY SETTINGS</span>
            <label className="switch-row"><span>Ambient effects</span>
              <input type="checkbox" checked={ambient}
                onChange={event => setAmbient(event.target.checked)} /></label>
            <label className="switch-row"><span>Reduce motion</span>
              <input type="checkbox" checked={lowMotion}
                onChange={event => setLowMotion(event.target.checked)} /></label>
            <button className="ghost-button refresh"
              onClick={() => sendCommand('snapshot.request')} disabled={!embedded}>
              Refresh saved state</button>
          </div>
        </aside>
      </div>
      {snapshot.saveNotice && <div className="save-notice" role="status">{snapshot.saveNotice}</div>}
      {error && <div className="error" role="alert">{error}</div>}
      {fatal && <div className="error" role="alert">
        The save outcome may already be committed. Close and reopen the modern Manor to recover
        it. Do not place another wager in this window.</div>}
      <footer className="footer">
        <span>PLAY-MONEY ONLY · C# SETTLEMENT · DURABLE SAVE</span>
        <span>{embedded ? 'CURRENT REVISION ' + snapshot.revision : 'BROWSER DESIGN PREVIEW · NO GAMEPLAY'}</span>
      </footer>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<ManorApp />);
