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
    { hz: 132, offset: 0, duration: .20, level: .12, wave: 'triangle', endHz: 83 },
    { hz: 74, offset: .08, duration: .38, level: .07, wave: 'sine', endHz: 63 },
    { hz: 245, offset: .03, duration: .095, level: .045, wave: 'triangle', endHz: 105 },
  ],
  reel: [
    { hz: 245, offset: 0, duration: .11, level: .085, wave: 'triangle', endHz: 138 },
    { hz: 85, offset: .018, duration: .23, level: .10, wave: 'sine', endHz: 58 },
    { hz: 392, offset: .015, duration: .06, level: .026, wave: 'sine', endHz: 245 },
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

export function hostAudioPolicy(sound: boolean, music: boolean, musicVolume: number) {
  return { sound, music, ambientGain: .025 * boundedVolume(musicVolume) };
}

/** All audio is created after an explicit user gesture. Failures never block a spin. */
export class ManorAudio {
  private context: AudioContext | null = null;
  private bus: GainNode | null = null;
  private hum: OscillatorNode[] = [];
  private humLevel: GainNode | null = null;
  private reelLoop: { source: AudioBufferSourceNode; lowpass: BiquadFilterNode;
    gain: GainNode } | null = null;
  private enabled = true;
  private ambience = false;
  private volume = .35;
  private hostSoundEnabled = false;
  private hostMusicEnabled = false;
  private hostAmbientGain = 0;

  /** Global desktop preferences are an upper bound; local toggles cannot override mute. */
  setHostPolicy(soundEnabled: boolean, musicEnabled: boolean, musicVolume: number): void {
    const policy = hostAudioPolicy(soundEnabled, musicEnabled, musicVolume);
    this.hostSoundEnabled = policy.sound;
    this.hostMusicEnabled = policy.music;
    this.hostAmbientGain = policy.ambientGain;
    this.mix();
    this.syncHum();
    if (this.humLevel && this.context)
      this.humLevel.gain.setTargetAtTime(this.hostAmbientGain,
        this.context.currentTime, .06);
  }

  arm(): void {
    if (!this.enabled || !this.hostSoundEnabled) return;
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
    if (!enabled) this.stopReels();
    this.mix();
    this.syncHum();
  }

  setVolume(value: number): void {
    this.volume = boundedVolume(value);
    this.mix();
    this.syncHum();
  }

  setAmbience(enabled: boolean): void {
    this.ambience = enabled;
    this.syncHum();
  }

  /** Sound-design pitch variation is cosmetic and never affects reel outcomes. */
  play(cue: ManorCue, pitch = 1): void {
    const context = this.context;
    const bus = this.bus;
    if (!this.enabled || !this.hostSoundEnabled || !context || !bus || this.volume === 0) return;
    try {
      const start = context.currentTime + .007;
      for (const tone of MANOR_CUES[cue]) {
        const voice = context.createOscillator();
        const gain = context.createGain();
        const at = start + tone.offset;
        const end = at + tone.duration;
        voice.type = tone.wave;
        const factor = Number.isFinite(pitch) ? Math.max(.8, Math.min(1.2, pitch)) : 1;
        voice.frequency.setValueAtTime(tone.hz * factor, at);
        if (tone.endHz)
          voice.frequency.exponentialRampToValueAtTime(tone.endHz * factor, end);
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

  /** Continuous low mechanical rustle while the visual reels move. Generated in
   * memory and faded out after the last stop; no network or audio files needed. */
  startReels(): void {
    this.stopReels();
    const context = this.context;
    const bus = this.bus;
    if (!this.enabled || !this.hostSoundEnabled || !context || !bus || this.volume === 0)
      return;
    try {
      const buffer = context.createBuffer(1, context.sampleRate, context.sampleRate);
      const samples = buffer.getChannelData(0);
      // Noise is decorative only. Wagers and final reel stops come from the C# host.
      for (let i = 0; i < samples.length; i++)
        samples[i] = (Math.random() * 2 - 1) * .18;
      const source = context.createBufferSource();
      source.buffer = buffer;
      source.loop = true;
      const lowpass = context.createBiquadFilter();
      lowpass.type = 'lowpass';
      lowpass.frequency.value = 620;
      const gain = context.createGain();
      const now = context.currentTime;
      gain.gain.setValueAtTime(0, now);
      gain.gain.linearRampToValueAtTime(.13, now + .14);
      source.connect(lowpass);
      lowpass.connect(gain);
      gain.connect(bus);
      source.start();
      this.reelLoop = { source, lowpass, gain };
    } catch {
      // Audio device and decoding errors never block a committed spin.
    }
  }

  stopReels(): void {
    if (!this.reelLoop) return;
    const { source, lowpass, gain } = this.reelLoop;
    this.reelLoop = null;
    try {
      const now = this.context?.currentTime ?? 0;
      gain.gain.cancelScheduledValues(now);
      gain.gain.setTargetAtTime(0, now, .06);
      source.stop(now + .25);
    } catch { /* The loop was already stopped. */ }
    source.onended = () => {
      source.disconnect();
      lowpass.disconnect();
      gain.disconnect();
    };
  }

  private mix(): void {
    if (this.bus && this.context)
      this.bus.gain.setTargetAtTime(this.enabled && this.hostSoundEnabled ? this.volume * .50 : 0,
        this.context.currentTime, .025);
  }

  private syncHum(): void {
    if (!this.context || !this.bus) return;
    const desired = this.enabled && this.hostSoundEnabled && this.hostMusicEnabled &&
      this.ambience && this.volume > 0 && this.hostAmbientGain > 0;
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
      gain.gain.value = this.hostAmbientGain;
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
    this.stopReels();
    this.setAmbience(false);
    const context = this.context;
    this.context = null;
    this.bus = null;
    if (context) void context.close().catch(() => undefined);
  }
}
