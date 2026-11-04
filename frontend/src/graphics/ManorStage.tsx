import { useEffect, useRef } from 'react';
import { Application, Container, Graphics, Text } from 'pixi.js';
import type { ManorSnapshot } from '../bridge/protocol';

type Props = { snapshot: ManorSnapshot; reducedMotion: boolean };

const SYMBOL_MARKS: Record<string, string> = {
  Jack: 'J', Queen: 'Q', King: 'K', Ace: 'A', Candelabrum: '♜', Watch: '◷',
  Raven: '♠', Key: '⚿', Portrait: '♛', Rose: '✾', Lady: '♕',
  Wild: '◈', Scatter: '☾', Bonus: '⌑',
};

const accent = (symbol: string): number =>
  symbol === 'Wild' ? 0x9de9ed : symbol === 'Scatter' ? 0xc4a6ff :
    symbol === 'Bonus' ? 0xe6ba75 : symbol === 'Lady' ? 0xf8d7bb : 0xd8c5af;

function addText(container: Container, text: string, x: number, y: number, size: number, color: number) {
  const item = new Text({
    text,
    style: { fontFamily: 'Georgia, serif', fontSize: size, fill: color, fontWeight: 'bold', align: 'center' },
  });
  item.anchor.set(0.5);
  item.position.set(x, y);
  container.addChild(item);
}

export function ManorStage({ snapshot, reducedMotion }: Props) {
  const holder = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const host = holder.current;
    if (!host) return;
    const app = new Application();
    let disposed = false;
    let initialized = false;
    let resizeObserver: ResizeObserver | undefined;
    let pulse: Graphics | undefined;
    let tick = 0;
    const boot = async () => {
      await app.init({
        width: 960,
        height: 500,
        background: 0x11101a,
        antialias: true,
        preference: 'webgl',
        resolution: Math.min(window.devicePixelRatio || 1, 2),
        autoDensity: true,
      });
      initialized = true;
      if (disposed) { app.destroy(true, { children: true }); return; }
      host.appendChild(app.canvas);
      app.canvas.setAttribute('aria-label', 'Five reels and three rows of Midnight Manor symbols');
      app.canvas.style.width = '100%';
      app.canvas.style.height = '100%';
      app.canvas.style.display = 'block';

      const scene = new Container();
      app.stage.addChild(scene);
      const background = new Graphics()
        .rect(0, 0, 960, 500).fill(0x10101c)
        .roundRect(10, 10, 940, 480, 24).fill({ color: 0x261b29 })
        .roundRect(21, 21, 918, 458, 20).stroke({ color: 0x75624b, width: 3, alpha: 0.8 })
        .roundRect(36, 37, 888, 426, 16).fill({ color: 0x151722 });
      scene.addChild(background);
      for (let i = 0; i < 11; i++) {
        const x = 45 + i * 86;
        scene.addChild(new Graphics().moveTo(x, 50).lineTo(x - 18, 446).stroke({ color: 0xe1ba72, width: 0.65, alpha: 0.12 }));
      }
      addText(scene, 'THE HOUSE NEVER FORGETS', 480, 64, 17, 0xbba37d);

      const x0 = 72, y0 = 111, w = 155, h = 100, gap = 12;
      for (let reel = 0; reel < 5; reel++) {
        const x = x0 + reel * (w + gap);
        scene.addChild(new Graphics()
          .roundRect(x - 5, y0 - 8, w + 10, h * 3 + 16, 12).fill(0x0a0d16)
          .roundRect(x - 5, y0 - 8, w + 10, h * 3 + 16, 12).stroke({ color: 0x80644c, width: 2 }));
        for (let row = 0; row < 3; row++) {
          const symbol = snapshot.cells[reel * 3 + row] ?? 'Unknown';
          const winning = snapshot.winningCells.includes(reel * 3 + row);
          const cy = y0 + row * h;
          const tile = new Graphics()
            .roundRect(x, cy, w, h - 6, 7).fill({ color: winning ? 0x394849 : 0x242331 })
            .roundRect(x + 5, cy + 5, w - 10, h - 16, 5).stroke({
              color: winning ? 0xffd89c : 0x5a5060, width: winning ? 3 : 1.1,
            });
          scene.addChild(tile);
          addText(scene, SYMBOL_MARKS[symbol] ?? symbol.slice(0, 1).toUpperCase(), x + w / 2, cy + 40, 43, accent(symbol));
          addText(scene, symbol.toUpperCase(), x + w / 2, cy + 78, 10, 0xafa4b5);
        }
      }
      pulse = new Graphics().roundRect(42, 42, 876, 416, 14).stroke({ color: 0xc0a5ee, alpha: 0.15, width: 2 });
      scene.addChild(pulse);
      addText(scene, 'MIDNIGHT MANOR   /   PRESENTATION PROTOTYPE', 480, 449, 12, 0xa38f9d);

      const resize = () => {
        if (disposed) return;
        app.renderer.resize(Math.max(1, host.clientWidth), Math.max(1, host.clientHeight));
        const scale = Math.min(host.clientWidth / 960, host.clientHeight / 500);
        scene.scale.set(scale);
        scene.position.set((host.clientWidth - 960 * scale) / 2, (host.clientHeight - 500 * scale) / 2);
      };
      resizeObserver = new ResizeObserver(resize);
      resizeObserver.observe(host);
      resize();
      if (reducedMotion) app.ticker.stop();
      else app.ticker.add((ticker) => {
        tick += ticker.deltaTime * 0.025;
        if (pulse) pulse.alpha = 0.55 + 0.35 * Math.sin(tick);
      });
    };
    void boot().catch(error => {
      if (!disposed) host.textContent = `Renderer unavailable: ${String(error)}`;
    });
    return () => {
      disposed = true;
      resizeObserver?.disconnect();
      if (initialized) app.destroy(true, { children: true });
    };
  }, [snapshot, reducedMotion]);
  return <div className="manor-canvas" ref={holder} role="img" aria-label="Midnight Manor five-reel preview" />;
}
