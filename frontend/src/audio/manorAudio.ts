/** WebView2-local, play-money-only sound effects. No assets, network or gameplay API. */
export type ManorCue =
  'spin' | 'reel' | 'smallWin' | 'bigWin' | 'epicWin' |
  'door' | 'moon' | 'footstep' | 'candle' | 'checkpoint' | 'exit' | 'threat';

export type ManorTone = Readonly<{
  hz: number;
  offset: number;
  duration: number;
  level: number;
  wave: OscillatorType;
  endHz?: number;
}>;

// Peaks are intentionally quiet. The maximum mix is additionally attenuated
// by the volume slider and master bus, and each voice uses a short fade-out.
export const MANOR_CUES: Readonly<Record<ManorCue, readonly ManorTone[]>> = {
  spin: [
    { hz: 128, offset: 0, duration: .24, level: .10, wave: 'triangle', endHz: 93 },
    { hz: 94, offset: .10, duration: .30, level: .065, wave: 'sine', endHz: 70 },
  ],
  reel: [
    { hz: 330, offset: 0, duration: .13, level: .075, wave: 'triangle', endHz: 280 },
    { hz: 156, offset: .025, duration: .18, level: .055, wave: 'sine' },
  ],
  smallWin: [
    { hz: 392, offset: 0, duration: .30, level: .095, wave: 'sine' },
    { hz: 523.25, offset: .13, duration: .37, level: .085, wave: 'sine' },
    { hz: 659.25, offset: .28, duration: .43, level: .075, wave: 'sine' },
  ],
  bigWin: [
    { hz: 261.63, offset: 0, duration: .45, level: .105, wave: 'triangle' },
    { hz: 392, offset: .13, duration: .51, level: .10, wave: 'sine' },
    { hz: 523.25, offset: .27, duration: .55, level: .10, wave: 'sine' },
    { hz: 783.99, offset: .46, duration: .62, level: .085, wave: 'sine' },
  ],
  epicWin: [
    { hz: 196, offset: 0, duration: .50, level: .095, wave: 'triangle' },
    { hz: 293.66, offset: .14, duration: .57, level: .10, wave: 'triangle' },
    { hz: 392, offset: .31, duration: .60, level: .095, wave: 'sine' },
    { hz: 587.33, offset: .50, duration: .71, level: .095, wave: 'sine' },
    { hz: 783.99, offset: .70, duration: .81, level: .065, wave: 'sine' },
  ],
  door: [
    { hz: 110, offset: 0, duration: .56, level: .11, wave: 'sawtooth', endHz: 65 },
    { hz: 164.81, offset: .15, duration: .58, level: .075, wave: 'sine', endHz: 123.47 },
  ],
  moon: [
    { hz: 329.63, offset: 0, duration: .57, level: .07, wave: 'sine' },
    { hz: 493.88, offset: .19, duration: .63, level: .085, wave: 'sine' },
    { hz: 659.25, offset: .37, duration: .76, level: .055, wave: 'sine' },
  ],
  footstep: [
    { hz: 105, offset: 0, duration: .14, level: .10, wave: 'triangle', endHz: 60 },
    { hz: 78, offset: .12, duration: .13, level: .075, wave: 'sine', endHz: 54 },
  ],
  candle: [
    { hz: 440, offset: 0, duration: .40, level: .065, wave: 'sine', endHz: 550 },
    { hz: 660, offset: .16, duration: .44, level: .045, wave: 'sine', endHz: 760 },
  ],
  checkpoint: [
    { hz: 261.63, offset: 0, duration: .32, level: .085, wave: 'triangle' },
    { hz: 392, offset: .17, duration: .45, level: .075, wave: 'sine' },
  ],
  exit: [
    { hz: 196, offset: 0, duration: .48, level: .09, wave: 'triangle' },
    { hz: 392, offset: .2, duration: .50, level: .08, wave: 'sine' },
    { hz: 523.25, offset: .41, duration: .62, level: .075, wave: 'sine' },
  ],
  threat: [
    { hz: 102, offset: 0, duration: .42, level: .065, wave: 'triangle', endHz: 82 },
    { hz: 99, offset: .04, duration: .45, level: .043, wave: 'sine', endHz: 75 },
  ],
};

export const boundedVolume = (value: number): number =>
  Number.isFinite(value) ? Math.max(0, Math.min(1, value)) : 0;

/** All audio is created after an explicit user gesture. Failures never block a spin. */
export class ManorAudio {
  private context: AudioContext | null = null;
  private bus: GainNode | null = null;
  private hum: OscillatorNode[] = [];
  private humLevel: GainNode | null = null;
  private enabled = true;
  private ambience = false;
  private volume = .35;

  arm(): void {
    if (!this.enabled) return;
    try {
      if (!this.context) {
        this.context = new AudioContext({ latencyHint: 'interactive' });
        this.bus = this.context.createGain();
        this.bus.gain.value = this.volume * .50;
        this.bus.connect(this.context.destination);
      }
      if (this.context.state === 'suspended')
        void this.context.resume().catch(() => undefined);
      this.syncHum();
    } catch {
      // Audio is optional. Renderer and C# transactions remain functional.
    }
  }

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
    this.mix();
    this.syncHum();
  }

  setVolume(value: number): void {
    this.volume = boundedVolume(value);
    this.mix();
  }

  setAmbience(enabled: boolean): void {
    this.ambience = enabled;
    this.syncHum();
  }

  play(cue: ManorCue): void {
    const context = this.context;
    const bus = this.bus;
    if (!this.enabled || !context || !bus || this.volume === 0) return;
    try {
      const start = context.currentTime + .007;
      for (const tone of MANOR_CUES[cue]) {
        const voice = context.createOscillator();
        const gain = context.createGain();
        const at = start + tone.offset;
        const end = at + tone.duration;
        voice.type = tone.wave;
        voice.frequency.setValueAtTime(tone.hz, at);
        if (tone.endHz)
          voice.frequency.exponentialRampToValueAtTime(tone.endHz, end);
        gain.gain.setValueAtTime(.0001, at);
        gain.gain.linearRampToValueAtTime(tone.level, at + Math.min(.035, tone.duration / 4));
        gain.gain.exponentialRampToValueAtTime(.0001, end);
        voice.connect(gain);
        gain.connect(bus);
        voice.start(at);
        voice.stop(end + .01);
        voice.onended = () => { voice.disconnect(); gain.disconnect(); };
      }
    } catch {
      // Never surface optional audio issues as a gameplay error.
    }
  }

  private mix(): void {
    if (this.bus && this.context)
      this.bus.gain.setTargetAtTime(this.enabled ? this.volume * .50 : 0,
        this.context.currentTime, .025);
  }

  private syncHum(): void {
    if (!this.context || !this.bus) return;
    const desired = this.enabled && this.ambience && this.volume > 0;
    if (!desired) {
      for (const voice of this.hum) {
        try { voice.stop(); } catch { /* Already stopped. */ }
        voice.disconnect();
      }
      this.hum = [];
      this.humLevel?.disconnect();
      this.humLevel = null;
      return;
    }
    if (this.hum.length) return;
    try {
      const gain = this.context.createGain();
      gain.gain.value = .025;
      gain.connect(this.bus);
      for (const hz of [55, 82.41]) {
        const osc = this.context.createOscillator();
        osc.type = 'sine';
        osc.frequency.value = hz;
        osc.connect(gain);
        osc.start();
        this.hum.push(osc);
      }
      this.humLevel = gain;
    } catch {
      // An absent audio output device must not affect the casino.
    }
  }

  dispose(): void {
    this.setAmbience(false);
    const context = this.context;
    this.context = null;
    this.bus = null;
    if (context) void context.close().catch(() => undefined);
  }
}
