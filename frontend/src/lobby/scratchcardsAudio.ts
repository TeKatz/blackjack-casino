/** Original offline Web Audio cues for the Lucky Sevens presentation only. */
let audio: AudioContext | null = null;
let lastWinTime = -1;

/** Starts the browser audio context on a real pointer/keyboard action. */
export function unlockScratchcardAudio(enabled: boolean): void {
  if (!enabled || typeof window === 'undefined' || !window.AudioContext) return;
  try {
    audio ??= new AudioContext();
    if (audio.state === 'suspended') void audio.resume().catch(() => {});
  } catch { /* Unsupported output devices must not interrupt scratching. */ }
}

/** One warm casino bell phrase with a crisp silver shimmer; never triggers settlement. */
export function playScratchcardWin(enabled: boolean): void {
  if (!enabled) return;
  unlockScratchcardAudio(true);
  if (!audio || audio.state === 'closed') return;
  try {
    const ctx = audio, now = ctx.currentTime + .008;
    // Ignore accidental duplicate pointer-up events in the same reveal.
    if (lastWinTime >= 0 && now - lastWinTime < .35) return;
    lastWinTime = now;
    const bell = (hz: number, offset: number, duration: number, volume: number) => {
      const start = now + offset;
      const oscillator = ctx.createOscillator();
      const shimmer = ctx.createOscillator();
      const gain = ctx.createGain();
      oscillator.type = 'sine';
      oscillator.frequency.setValueAtTime(hz, start);
      shimmer.type = 'sine';
      shimmer.frequency.setValueAtTime(hz * 2.01, start);
      const upper = ctx.createGain();
      upper.gain.value = .18;
      oscillator.connect(gain);
      shimmer.connect(upper).connect(gain);
      gain.connect(ctx.destination);
      gain.gain.setValueAtTime(.0001, start);
      gain.gain.exponentialRampToValueAtTime(volume, start + .016);
      gain.gain.exponentialRampToValueAtTime(.0001, start + duration);
      oscillator.start(start); shimmer.start(start);
      oscillator.stop(start + duration + .02);
      shimmer.stop(start + duration + .02);
      oscillator.onended = () => { oscillator.disconnect(); shimmer.disconnect(); upper.disconnect(); gain.disconnect(); };
    };
    bell(523.25, 0, .42, .034);
    bell(659.25, .095, .46, .035);
    bell(783.99, .205, .62, .039);
    bell(1046.50, .32, .92, .032);
    bell(1567.98, .47, 1.12, .017);
  } catch { /* Presentation audio is never allowed to block ticket actions. */ }
}
