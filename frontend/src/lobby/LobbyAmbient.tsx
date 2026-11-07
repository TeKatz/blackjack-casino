import { useEffect, useRef } from 'react';
import { Application, Graphics } from 'pixi.js';
import {createTileFx} from './TileFxEngine';

/** One small transparent effect layer. UI/labels stay DOM-rendered and accessible. */
export function LobbyAmbient({ enabled }: { enabled: boolean }) {
  const target = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const host = target.current;
    if (!host || !enabled) return;
    let disposed = false;
    let app: Application | undefined;
    let destroyTileFx = () => {};
    let pause = () => {};
    (async () => {
      const instance = new Application();
      try {
        await instance.init({
          resizeTo: host, backgroundAlpha: 0, antialias: false,
          resolution: Math.min(window.devicePixelRatio || 1, 1.5),
          autoDensity: true, preference: 'webgl',
        });
        if (disposed) { instance.destroy(true); return; }
        app = instance;
        instance.canvas.setAttribute('aria-hidden', 'true');
        host.appendChild(instance.canvas);
        const particles = Array.from({length: 26}, (_, i) => {
          const dot = new Graphics().circle(0, 0, 0.9 + (i % 3) * .45)
            .fill({color: i % 5 === 0 ? 0x78ecbd : 0xdcb576, alpha: .12 + (i % 4) * .07});
          dot.x = (i * 79.7 % 100) * host.clientWidth / 100;
          dot.y = (i * 31.7 % 100) * host.clientHeight / 100;
          instance.stage.addChild(dot);
          return {dot, phase: i * 2.21, speed: .08 + (i % 5) * .035};
        });
        const fx=createTileFx(instance.stage,host.parentElement??host);
        destroyTileFx=()=>fx.destroy();
        let clock = 0;
        instance.ticker.maxFPS = 30;
        instance.ticker.add(ticker => {
          clock += ticker.deltaTime/60;
          fx.tick(clock,ticker.deltaTime);
          const width = host.clientWidth, height = host.clientHeight;
          particles.forEach(({dot, phase, speed}) => {
            dot.y -= speed * ticker.deltaTime;
            if (dot.y < -8) dot.y = height + 8;
            dot.x += Math.sin(clock * .003 + phase) * .035 * ticker.deltaTime;
            dot.alpha = .48 + .32 * Math.sin(clock * .018 + phase);
            if (dot.x > width + 8) dot.x = -8;
          });
        });
        pause = () => {
          if (document.hidden) instance.ticker.stop();
          else instance.ticker.start();
        };
        document.addEventListener('visibilitychange', pause);
      } catch {
        // WPF lobby remains usable even if a GPU renderer cannot initialize.
        instance.destroy(true);
      }
    })();
    return () => {
      disposed = true;
      document.removeEventListener('visibilitychange', pause);
      destroyTileFx();
      app?.destroy(true);
    };
  }, [enabled]);
  return <div ref={target} className="lobby-pixi" aria-hidden="true" />;
}
