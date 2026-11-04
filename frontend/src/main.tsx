import { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { browserPreview, sendCommand, subscribeToHost } from './bridge/protocol';
import type { ManorSnapshot } from './bridge/protocol';
import { ManorStage } from './graphics/ManorStage';
import './styles.css';

const embedded = Boolean(window.chrome?.webview);

function ManorApp() {
  const [snapshot, setSnapshot] = useState<ManorSnapshot>(browserPreview);
  const [connected, setConnected] = useState(!embedded);
  const [error, setError] = useState<string | null>(null);
  const [ambient, setAmbient] = useState(true);
  const [lowMotion, setLowMotion] = useState(window.matchMedia('(prefers-reduced-motion: reduce)').matches);

  useEffect(() => {
    const stop = subscribeToHost(message => {
      if (message.type === 'snapshot') {
        setSnapshot(message.payload);
        setConnected(true);
        setError(null);
      } else {
        setError(message.payload.message);
      }
    });
    sendCommand('client.ready');
    return stop;
  }, []);

  const motionOff = lowMotion || !ambient || !snapshot.animationsEnabled;
  const feature = snapshot.cycleActive ? 'A story is still in progress' : 'The night awaits';
  return (
    <main className="manor-app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true">☾</span>
          <div><span className="eyebrow">THE MIDNIGHT CLUB</span><h1>MIDNIGHT MANOR</h1></div>
        </div>
        <div className="topbar-actions">
          <span className={connected ? 'connection ready' : 'connection'}>{connected ? 'PREVIEW · CONNECTED' : 'CONNECTING TO CASINO'}</span>
          <button className="ghost-button" onClick={() => embedded ? sendCommand('ui.close') : window.close()}>Back to lobby</button>
        </div>
      </header>
      <section className="headline">
        <div>
          <span className="eyebrow">BEYOND THE MOONLIT DOORS</span>
          <h2>The house knows your name.</h2>
          <p>Gothic luxury, new rendering technology. The old game remains fully playable while this presentation is developed.</p>
        </div>
        <div className="stage-tag"><span className="tag-dot" /> MIGRATION / PHASE 1</div>
      </section>
      <div className="content-grid">
        <section className="machine" aria-label="Manor reel presentation">
          <div className="machine-title">
            <span>THE HAUNTED FIVE</span>
            <span>20 FIXED PAYLINES · 5 × 3</span>
          </div>
          <ManorStage snapshot={snapshot} reducedMotion={motionOff} />
          <div className="machine-footer">
            <span>{snapshot.hasCommittedSpin ? 'LAST COMMITTED SPIN' : 'NO SAVED SPIN · DESIGN STAND-IN'}</span>
            <span>{feature}</span>
          </div>
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
            <p>{snapshot.freeSpinsRemaining > 0
              ? `${snapshot.freeSpinsRemaining} free spins are saved. Open the classic Manor to continue them safely.`
              : 'Pending bonuses and payouts remain in the established C# Manor engine.'}</p>
          </div>
          <div className="status-card">
            <span className="eyebrow">DISPLAY SETTINGS</span>
            <label className="switch-row">
              <span>Ambient effects</span>
              <input type="checkbox" checked={ambient} onChange={event => setAmbient(event.target.checked)} />
            </label>
            <label className="switch-row">
              <span>Reduce motion</span>
              <input type="checkbox" checked={lowMotion} onChange={event => setLowMotion(event.target.checked)} />
            </label>
            <button className="ghost-button refresh" onClick={() => sendCommand('snapshot.request')} disabled={!embedded}>Refresh saved state</button>
          </div>
        </aside>
      </div>
      {error && <div className="error" role="alert">{error}</div>}
      <footer className="footer">
        <span>READ-ONLY PREVIEW · NO BETS OR WINS EXECUTED HERE</span>
        <span>THE MIDNIGHT CLUB / MODERN FRONTEND</span>
      </footer>
    </main>
  );
}

createRoot(document.getElementById('root')!).render(<ManorApp />);
