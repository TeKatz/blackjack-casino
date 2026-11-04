import { useEffect, useRef } from 'react';
import { Application, Container, Graphics, Sprite, Text, Texture } from 'pixi.js';
import type { ManorSnapshot } from '../bridge/protocol';
import { reelStops, visualIndex } from './reelTiming';
import type { ReelSpeed } from './reelTiming';
import { loadManorArtwork, MANOR_SYMBOL_NAMES } from './manorArtwork';
import type { ManorSymbolName } from './manorArtwork';
import { boundedPixelRatio, calculateCanvasLayout, summarizeFrameTimes } from './rendererDiagnostics';
import type { RendererDiagnostics } from './rendererDiagnostics';

type Props = {
  snapshot: ManorSnapshot;
  spinEpoch: number;
  reducedMotion: boolean;
  speed: ReelSpeed;
  onSpinningChange?: (spinning: boolean) => void;
  onReelStop?: (reel: number) => void;
  onSpinPresented?: (result: ManorSnapshot) => void;
  onDiagnostics?: (info: RendererDiagnostics) => void;
};
type Tile = { root: Container; backing: Graphics; artwork: Graphics; illustration: Sprite; letter: Text; name: Text; symbol: string };
type Reel = { tiles: Tile[] };
type Renderer = {
  update(snapshot: ManorSnapshot, epoch: number, motionOff: boolean, speed: ReelSpeed): void;
  setArtwork(images: Map<ManorSymbolName, HTMLImageElement>): void;
  dispose(): void;
};
const W = 960, H = 500, H_CELL = 100;
const SYMBOLS = MANOR_SYMBOL_NAMES;
const LETTERS: Record<string, string> = { Jack: 'J', Queen: 'Q', King: 'K', Ace: 'A' };
const accents: Record<string, number> = {
  Wild: 0x9cdee3, Scatter: 0xc7b8f7, Bonus: 0xe9bc79,
  Lady: 0xe8d6ba, Rose: 0xc9899e, Raven: 0xaba9c9,
};
const accent = (name: string) => accents[name] ?? 0xd9bd8b;

function text(value: string, size: number, color: number): Text {
  const result = new Text({ text: value, style: {
    fontFamily: 'Georgia, serif', fontSize: size, fill: color,
    align: 'center', fontWeight: 'bold',
  } });
  result.anchor.set(0.5);
  return result;
}
function stroke(g: Graphics, points: number[], color: number, width = 3): void {
  g.moveTo(points[0], points[1]);
  for (let i = 2; i < points.length; i += 2) g.lineTo(points[i], points[i + 1]);
  g.stroke({ color, width });
}
/** Hand-drawn vector silhouettes, reused per tile; no third-party assets. */
function drawArtwork(g: Graphics, name: string): void {
  g.clear();
  const gold = accent(name);
  if (LETTERS[name]) {
    stroke(g, [43, 69, 53, 64, 63, 69], gold, 1.5);
    stroke(g, [88, 69, 98, 64, 108, 69], gold, 1.5);
    g.circle(76, 15, 3).fill(gold);
  } else if (name === 'Watch') {
    g.circle(76, 43, 27).fill(0x30283a).stroke({ color: gold, width: 4 });
    g.circle(76, 43, 20).stroke({ color: 0x9e8a66, width: 1 });
    stroke(g, [76, 26, 76, 43, 88, 50], gold, 3);
    g.roundRect(69, 9, 14, 7, 2).fill(gold);
    g.circle(76, 8, 4).stroke({ color: gold, width: 2 });
  } else if (name === 'Candelabrum') {
    stroke(g, [76, 24, 76, 71, 59, 74, 93, 74], gold, 4);
    stroke(g, [76, 55, 55, 46, 52, 34, 44, 34], gold, 3);
    stroke(g, [76, 55, 97, 46, 100, 34, 108, 34], gold, 3);
    for (const x of [44, 76, 108]) {
      g.roundRect(x - 5, 30, 10, 15, 2).fill(0xe5d6b2);
      g.ellipse(x, 23, 4, 8).fill(0xf5b35b);
      g.circle(x, 25, 2).fill(0xffe7ab);
    }
  } else if (name === 'Key') {
    g.circle(59, 32, 17).stroke({ color: gold, width: 6 });
    g.circle(59, 32, 7).stroke({ color: 0x877351, width: 2 });
    stroke(g, [72, 45, 102, 75, 112, 65, 103, 58], gold, 8);
  } else if (name === 'Raven') {
    g.moveTo(42, 68).lineTo(57, 43).lineTo(53, 27).lineTo(67, 35)
      .lineTo(85, 22).lineTo(98, 29).lineTo(110, 35).lineTo(96, 41)
      .lineTo(104, 65).lineTo(86, 60).lineTo(77, 75).lineTo(63, 60)
      .closePath().fill(0x101520).stroke({ color: gold, width: 2 });
    g.circle(88, 34, 3).fill(0xf6d28c);
  } else if (name === 'Portrait' || name === 'Lady') {
    g.roundRect(44, 9, 64, 65, 4).fill(0x202534).stroke({ color: gold, width: 5 });
    g.roundRect(51, 16, 50, 51, 2).stroke({ color: 0x77627a, width: 1.5 });
    g.ellipse(76, 36, 13, 17).fill(0xb6a6a4);
    g.moveTo(54, 69).lineTo(60, 54).lineTo(92, 54).lineTo(98, 69).closePath().fill(0x7b6688);
    if (name === 'Lady') stroke(g, [62, 20, 68, 12, 76, 20, 84, 12, 90, 20], gold, 3);
  } else if (name === 'Rose') {
    stroke(g, [76, 41, 78, 76], 0x7c9670, 5);
    g.ellipse(61, 61, 13, 6).fill(0x6f8a6d);
    g.ellipse(91, 64, 13, 6).fill(0x6f8a6d);
    for (const [x, y] of [[63, 32], [88, 32], [75, 23], [75, 43]]) {
      g.ellipse(x, y, 14, 13).fill(0x92384f).stroke({ color: 0xd69bab, width: 1.5 });
    }
    g.circle(75, 33, 8).fill(0xe0a2b2);
  } else if (name === 'Wild') {
    g.roundRect(44, 10, 64, 65, 11).fill(0x243d48).stroke({ color: gold, width: 5 });
    g.roundRect(51, 17, 50, 51, 8).stroke({ color: 0x88bec2, width: 1 });
    stroke(g, [76, 23, 81, 39, 97, 44, 81, 49, 76, 64, 71, 49, 55, 44, 71, 39, 76, 23], 0xe3ffff, 2);
    g.circle(76, 44, 4).fill(0xf1ffff);
  } else if (name === 'Scatter') {
    g.circle(76, 43, 28).fill(0xe9d7ff);
    g.circle(91, 32, 24).fill(0x24212c);
    stroke(g, [43, 18, 54, 18], gold, 2);
    stroke(g, [49, 12, 49, 24], gold, 2);
    g.circle(105, 66, 3).fill(gold);
  } else if (name === 'Bonus') {
    g.roundRect(45, 11, 63, 65, 4).fill(0x4b3040).stroke({ color: gold, width: 4 });
    stroke(g, [76, 12, 76, 76], gold, 3);
    g.roundRect(51, 22, 20, 43, 2).stroke({ color: 0x967253, width: 2 });
    g.roundRect(81, 22, 20, 43, 2).stroke({ color: 0x967253, width: 2 });
    g.circle(70, 48, 2.5).fill(0xf7deb3);
    g.circle(82, 48, 2.5).fill(0xf7deb3);
  }
}
function makeTile(): Tile {
  const root = new Container();
  const backing = new Graphics();
  const artwork = new Graphics();
  const illustration = new Sprite(Texture.EMPTY);
  illustration.position.set(5, 4);
  illustration.visible = false;
  const letter = text('', 62, 0xe9ca92);
  const name = text('', 10, 0xd9c3a6);
  letter.position.set(76, 43);
  name.position.set(76, 81);
  root.addChild(backing, illustration, artwork, letter, name);
  return { root, backing, artwork, illustration, letter, name, symbol: '' };
}
function decorate(tile: Tile, symbol: string, winning: boolean, dim: boolean,
  textures: ReadonlyMap<string, Texture>): void {
  const color = accent(symbol);
  tile.backing.clear();
  tile.backing.roundRect(0, 1, 152, 97, 5)
    .fill(winning ? 0x3c424a : 0x24212c)
    .stroke({ color: winning ? 0xf3d69a : 0x60505e, width: winning ? 2.5 : 1 });
  tile.backing.roundRect(5, 6, 142, 87, 4)
    .stroke({ color, width: 0.8, alpha: winning ? 0.7 : 0.25 });
  tile.artwork.alpha = tile.letter.alpha = tile.illustration.alpha = dim ? 0.45 : 1;
  tile.name.alpha = dim ? 0.5 : 1;
  if (tile.symbol !== symbol) {
    tile.symbol = symbol;
    drawArtwork(tile.artwork, symbol);
    tile.letter.text = LETTERS[symbol] ?? '';
    tile.letter.style.fill = color;
    tile.name.text = symbol.toUpperCase();
  }
  const texture = textures.get(symbol);
  if (texture) {
    if (tile.illustration.texture !== texture) {
      tile.illustration.texture = texture;
      tile.illustration.width = 142;
      tile.illustration.height = 76;
    }
    tile.illustration.visible = true;
    tile.artwork.visible = false;
    tile.letter.visible = false;
  } else {
    tile.illustration.visible = false;
    tile.artwork.visible = true;
    tile.letter.visible = Boolean(LETTERS[symbol]);
  }
}
function createRenderer(app: Application, report: (value: boolean) => void,
  reelStop: (reel: number) => void,
  presentedSpin: (result: ManorSnapshot) => void,
  diagnostics: (samples: readonly number[]) => void,
  monitoring: () => boolean): Renderer {
  const scene = new Container();
  app.stage.addChild(scene);
  scene.addChild(new Graphics()
    .rect(0, 0, W, H).fill(0x0d111b)
    .roundRect(14, 14, 932, 472, 20).fill(0x291e2c)
    .roundRect(22, 22, 916, 456, 17).stroke({ color: 0x947650, width: 3 })
    .roundRect(36, 35, 888, 425, 10).fill(0x121522)
    .roundRect(47, 44, 866, 407, 8).stroke({ color: 0x4e4056, width: 1.5 }));
  for (let r = 0; r < 5; r++) {
    const x = 69 + 167 * r;
    scene.addChild(new Graphics()
      .roundRect(x - 5, 107, 163, 307, 9).fill(0x090e18)
      .roundRect(x - 5, 107, 163, 307, 9).stroke({ color: 0xa48a60, width: 2 }));
  }
  for (const x of [54, 906]) {
    scene.addChild(new Graphics()
      .roundRect(x - 5, 225, 10, 95, 2).fill(0x998363)
      .ellipse(x, 219, 8, 13).fill(0xf7a95d)
      .ellipse(x, 219, 4, 8).fill(0xffebba));
  }
  const heading = text('THE HAUNTED FIVE', 23, 0xe8d2a5);
  heading.position.set(480, 70);
  const footer = text('EST. 1893 · THE HOUSE HOLDS ITS SECRETS', 12, 0xc6ac84);
  footer.position.set(480, 451);
  scene.addChild(heading, footer);
  const reels: Reel[] = [];
  const textures = new Map<string, Texture>();
  // Cosmetic reel-stop glints. The source of every final symbol is still the
  // committed C# snapshot; these lights do not request or resolve game actions.
  const stopGlints: Graphics[] = [];
  const stopGlowMs = [0, 0, 0, 0, 0];
  const stoppedReels = new Set<number>();
  for (let r = 0; r < 5; r++) {
    const x = 71 + 167 * r;
    const mask = new Graphics().rect(x, 112, 152, 300).fill(0xffffff);
    const col = new Container();
    col.position.set(x, 112);
    col.mask = mask;
    scene.addChild(col, mask);
    const tiles: Tile[] = [];
    for (let row = 0; row < 4; row++) {
      const tile = makeTile();
      tile.root.y = row * H_CELL;
      col.addChild(tile.root);
      tiles.push(tile);
    }
    reels.push({ tiles });
    const glint = new Graphics().roundRect(x - 6, 107, 164, 307, 10)
      .stroke({ color: 0xffdfa0, width: 6, alpha: 0.85 });
    glint.alpha = 0;
    scene.addChild(glint);
    stopGlints.push(glint);
  }
  const frame = new Graphics().roundRect(42, 39, 876, 421, 9)
    .stroke({ color: 0xe4b97c, width: 1.2, alpha: 0.5 });
  scene.addChild(frame);
  const winningTrace = new Graphics();
  scene.addChild(winningTrace);
  const winningCaption = text('', 13, 0xf7dca6);
  winningCaption.position.set(480, 433);
  winningCaption.visible = false;
  scene.addChild(winningCaption);
  let shownLine = 0;
  let lineElapsed = 0;
  let motionDisabled = false;
  function paintWinningLine(result: ManorSnapshot | null, index: number): void {
    winningTrace.clear();
    winningCaption.visible = false;
    const details = result?.winningLineDetails ?? [];
    if (!details.length) return;
    const win = details[index % details.length];
    for (let reel = 0; reel < 5; reel++) {
      const x = 147 + 167 * reel;
      const y = 162 + win.rows[reel] * 100;
      if (reel === 0) winningTrace.moveTo(x, y);
      else winningTrace.lineTo(x, y);
    }
    winningTrace.stroke({ color: 0xf1cd83, width: 4, alpha: 0.72, cap: 'round', join: 'round' });
    for (let reel = 0; reel < win.count; reel++) {
      const x = 147 + 167 * reel;
      const y = 162 + win.rows[reel] * 100;
      winningTrace.circle(x, y, 12).stroke({ color: 0xffe4a9, width: 3, alpha: 0.92 });
    }
    winningCaption.text = 'PAYLINE ' + win.number + ' · ' + win.count +
      ' MATCHED · ' + win.amount;
    winningCaption.visible = true;
  }
  let epoch = -1;
  let presented: ManorSnapshot | null = null;
  let animation: { elapsed: number; stops: number[]; result: ManorSnapshot } | null = null;
  let speed: ReelSpeed = 'normal';
  let pulse = 0;
  let frameSamples: number[] = [];
  let frameWindow = 0;
  function settleColumn(r: number, result: ManorSnapshot): void {
    const wins = result.winningCells.length > 0;
    for (let row = 0; row < 4; row++) {
      const tile = reels[r].tiles[row];
      tile.root.y = row * H_CELL;
      tile.root.visible = row < 3;
      if (row < 3) {
        const index = r * 3 + row;
        const winning = result.winningCells.includes(index);
        decorate(tile, result.cells[index] ?? 'Jack', winning, wins && !winning, textures);
      }
    }
  }
  function finish(announce = true): void {
    if (!animation) return;
    const result = animation.result;
    animation = null;
    for (let r = 0; r < 5; r++) settleColumn(r, result);
    presented = result;
    shownLine = 0;
    lineElapsed = 0;
    paintWinningLine(result, shownLine);
    report(false);
    if (announce) presentedSpin(result);
  }
  const frameTick = (ticker: { deltaMS: number; elapsedMS: number }): void => {
    const dt = Math.min(50, ticker.deltaMS);
    if (monitoring()) {
      if (ticker.elapsedMS > 0 && ticker.elapsedMS <= 250) {
        if (frameSamples.length >= 240) frameSamples.shift();
        frameSamples.push(ticker.elapsedMS);
        frameWindow += ticker.elapsedMS;
      }
      if (frameWindow >= 2400) {
        diagnostics(frameSamples);
        frameSamples = [];
        frameWindow = 0;
      }
    } else if (frameSamples.length) {
      frameSamples = [];
      frameWindow = 0;
    }
    pulse += dt * 0.0015;
    frame.alpha = 0.7 + Math.sin(pulse) * 0.25;
    for (let r = 0; r < 5; r++) {
      if (motionDisabled) stopGlowMs[r] = 0;
      else stopGlowMs[r] = Math.max(0, stopGlowMs[r] - dt);
      stopGlints[r].alpha = stopGlowMs[r] > 0 ? 0.55 * (stopGlowMs[r] / 260) : 0;
    }
    if (!animation) {
      if (!motionDisabled && presented && presented.winningLineDetails.length > 1) {
        lineElapsed += dt;
        if (lineElapsed >= 1500) {
          lineElapsed = 0;
          shownLine = (shownLine + 1) % presented.winningLineDetails.length;
          paintWinningLine(presented, shownLine);
        }
      }
      return;
    }
    const current = animation;
    current.elapsed += dt;
    for (let r = 0; r < 5; r++) {
      if (current.elapsed >= current.stops[r]) {
        if (!stoppedReels.has(r)) {
          stoppedReels.add(r);
          stopGlowMs[r] = 260;
          reelStop(r);
        }
        settleColumn(r, current.result);
        continue;
      }
      const offset = (current.elapsed * 1.7) % H_CELL;
      const step = Math.floor(current.elapsed / (speed === 'turbo' ? 40 : 78));
      for (let row = 0; row < 4; row++) {
        const tile = reels[r].tiles[row];
        tile.root.visible = true;
        tile.root.y = row * H_CELL - offset;
        const symbol = SYMBOLS[visualIndex(step, r, row)];
        if (tile.symbol !== symbol) decorate(tile, symbol, false, false, textures);
      }
    }
    if (current.elapsed >= current.stops[4]) finish();
  };
  app.ticker.add(frameTick);
  return {
    update(result, spinEpoch, motionOff, nextSpeed) {
      speed = nextSpeed;
      motionDisabled = motionOff;
      if (motionOff) {
        finish();
        app.ticker.stop();
        frame.alpha = 0.75;
      } else {
        app.ticker.start();
      }
      if (epoch < 0) {
        epoch = spinEpoch;
        presented = result;
        for (let r = 0; r < 5; r++) settleColumn(r, result);
        paintWinningLine(result, 0);
      } else if (epoch !== spinEpoch) {
        finish(false);
        epoch = spinEpoch;
        const stops = reelStops(speed, motionOff);
        if (stops[4] > 0 && result.hasCommittedSpin) {
          animation = { elapsed: 0, stops, result };
          stoppedReels.clear();
          stopGlowMs.fill(0);
          presented = result;
          winningTrace.clear();
          winningCaption.visible = false;
          report(true);
        } else {
          for (let r = 0; r < 5; r++) settleColumn(r, result);
          presented = result;
          paintWinningLine(result, 0);
          report(false);
          presentedSpin(result);
        }
      } else if (!animation && presented !== result) {
        for (let r = 0; r < 5; r++) settleColumn(r, result);
        presented = result;
        shownLine = 0;
        paintWinningLine(result, 0);
      }
      // A stopped ticker does not redraw a newly committed outcome by itself.
      if (motionOff) app.render();
    },
    setArtwork(images) {
      // Individual failures are harmless: retain the local Graphics fallback.
      for (const [symbol, image] of images) {
        try {
          textures.set(symbol, Texture.from(image));
        } catch {
          // A browser may reject SVG image sources when WebGL is unavailable.
        }
      }
      if (presented && !animation) {
        for (let r = 0; r < 5; r++) settleColumn(r, presented);
      } else if (animation) {
        for (const reel of reels)
          for (const tile of reel.tiles)
            if (tile.symbol) decorate(tile, tile.symbol, false, false, textures);
      }
      if (motionDisabled) app.render();
    },
    dispose() {
      finish();
      app.ticker.remove(frameTick);
      textures.forEach(texture => texture.destroy(true));
      textures.clear();
    },
  };
}

/** Pixi canvas, masks, and 20 tile objects survive all state refreshes and spins. */
export function ManorStage({ snapshot, spinEpoch, reducedMotion, speed,
  onSpinningChange, onReelStop, onSpinPresented, onDiagnostics }: Props) {
  const holder = useRef<HTMLDivElement>(null);
  const renderer = useRef<Renderer | null>(null);
  const latest = useRef({ snapshot, spinEpoch, reducedMotion, speed });
  const changed = useRef(onSpinningChange);
  const stopCue = useRef(onReelStop);
  const presentationCue = useRef(onSpinPresented);
  const diagnosticCue = useRef(onDiagnostics);
  const currentSurface = useRef({ width: W, height: H, pixelRatio: 1 });
  latest.current = { snapshot, spinEpoch, reducedMotion, speed };
  changed.current = onSpinningChange;
  stopCue.current = onReelStop;
  presentationCue.current = onSpinPresented;
  diagnosticCue.current = onDiagnostics;
  useEffect(() => {
    const host = holder.current;
    if (!host) return;
    const app = new Application();
    let dead = false;
    let initialized = false;
    let observer: ResizeObserver | null = null;
    let resolutionWatcher: MediaQueryList | null = null;
    let resize: (() => void) | null = null;
    const watchResolution = () => {
      resolutionWatcher?.removeEventListener('change', watchResolution);
      resize?.();
      // Re-register for the *new* scaling ratio when a window switches monitors.
      resolutionWatcher = window.matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`);
      resolutionWatcher.addEventListener('change', watchResolution);
    };
    void (async () => {
      await app.init({
        width: W, height: H, background: 0x10121c, antialias: true,
        preference: 'webgl', resolution: boundedPixelRatio(window.devicePixelRatio),
        autoDensity: true,
      });
      initialized = true;
      if (dead) { app.destroy(true, { children: true }); return; }
      app.canvas.style.width = '100%';
      app.canvas.style.height = '100%';
      app.canvas.style.display = 'block';
      app.canvas.setAttribute('aria-hidden', 'true');
      host.appendChild(app.canvas);
      const instance = createRenderer(app, value => changed.current?.(value),
        reel => stopCue.current?.(reel), result => presentationCue.current?.(result),
        samples => {
          const bounds = currentSurface.current;
          const info = summarizeFrameTimes(samples, bounds.width, bounds.height, bounds.pixelRatio);
          if (info) diagnosticCue.current?.(info);
        },
        () => Boolean(diagnosticCue.current));
      renderer.current = instance;
      resize = () => {
        if (dead) return;
        const bounds = calculateCanvasLayout(host.clientWidth, host.clientHeight,
          window.devicePixelRatio);
        currentSurface.current = {
          width: bounds.width, height: bounds.height, pixelRatio: bounds.pixelRatio,
        };
        app.renderer.resize(bounds.width, bounds.height, bounds.pixelRatio);
        app.stage.scale.set(bounds.scale);
        app.stage.position.set(bounds.x, bounds.y);
        if (latest.current.reducedMotion) app.render();
      };
      observer = new ResizeObserver(resize);
      observer.observe(host);
      window.addEventListener('resize', resize);
      watchResolution();
      const state = latest.current;
      instance.update(state.snapshot, state.spinEpoch, state.reducedMotion, state.speed);
      // Never block the authoritative first snapshot or controls on art decoding.
      void loadManorArtwork().then(images => {
        if (!dead) instance.setArtwork(images);
      });
    })().catch(error => {
      if (!dead) host.textContent = 'Renderer unavailable: ' + String(error);
    });
    return () => {
      dead = true;
      observer?.disconnect();
      if (resize) window.removeEventListener('resize', resize);
      resolutionWatcher?.removeEventListener('change', watchResolution);
      renderer.current?.dispose();
      renderer.current = null;
      if (initialized) app.destroy(true, { children: true });
    };
  }, []);
  useEffect(() => {
    renderer.current?.update(snapshot, spinEpoch, reducedMotion, speed);
  }, [snapshot, spinEpoch, reducedMotion, speed]);
  return (
    <div className="manor-canvas" ref={holder} role="img"
      aria-label={'Midnight Manor, five reels and three rows. Symbols: ' +
        snapshot.cells.join(', ') + '. Winning cells: ' +
        snapshot.winningCells.map(index => String(index + 1)).join(', ')} />
  );
}
