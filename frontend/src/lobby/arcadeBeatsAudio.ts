/**
 * Procedural original rhythm-groove instruments for Midnight Beats.
 * Musical pulses and falling-note timestamps come from arcadeBeatsEngine's
 * exact same deterministic half-beat grid. This module owns audio only.
 */
import type {BeatsGrade} from './arcadeBeatsEngine';
export type BeatAudioRef={current:AudioContext|null};
const NOTE_FREQUENCIES=[293.66,349.23,440,523.25] as const; // D4 F4 A4 C5
const ROOTS=[73.42,87.31,65.41,98.00] as const; // D2 F2 C2 G2
function contextFor(ref:BeatAudioRef):AudioContext{
 const context=ref.current??new AudioContext();
 ref.current=context;
 if(context.state==='suspended')void context.resume();
 return context;
}
function synth(ctx:AudioContext,frequency:number,wave:OscillatorType,
 amplitude:number,duration:number,slide=1,delay=0):void{
 const t=ctx.currentTime+delay,osc=ctx.createOscillator(),gain=ctx.createGain();
 osc.type=wave;osc.frequency.setValueAtTime(frequency,t);
 if(slide!==1)osc.frequency.exponentialRampToValueAtTime(Math.max(20,frequency*slide),t+duration*.85);
 gain.gain.setValueAtTime(.0001,t);
 gain.gain.exponentialRampToValueAtTime(Math.max(.0002,amplitude),t+.004);
 gain.gain.exponentialRampToValueAtTime(.0001,t+duration);
 osc.connect(gain).connect(ctx.destination);
 osc.onended=()=>{osc.disconnect();gain.disconnect();};
 osc.start(t);osc.stop(t+duration+.015);
}
const noiseCache=new WeakMap<AudioContext,AudioBuffer>();
function hat(ctx:AudioContext,volume:number):void{
 let buffer=noiseCache.get(ctx);
 if(!buffer){
  buffer=ctx.createBuffer(1,Math.floor(ctx.sampleRate*.065),ctx.sampleRate);
  const data=buffer.getChannelData(0);
  // This noise is sound synthesis only and is never used for chart generation.
  let x=0xa51c6d27;
  for(let i=0;i<data.length;i++){
   x=(Math.imul(x,1664525)+1013904223)>>>0;
   data[i]=x/2147483648-1;
  }
  noiseCache.set(ctx,buffer);
 }
 const t=ctx.currentTime,source=ctx.createBufferSource();
 const filter=ctx.createBiquadFilter(),gain=ctx.createGain();
 source.buffer=buffer;filter.type='highpass';filter.frequency.value=4700;
 gain.gain.setValueAtTime(.0001,t);gain.gain.exponentialRampToValueAtTime(volume,t+.002);
 gain.gain.exponentialRampToValueAtTime(.0001,t+.045);
 source.connect(filter).connect(gain).connect(ctx.destination);
 source.onended=()=>{source.disconnect();filter.disconnect();gain.disconnect();};
 source.start(t);source.stop(t+.05);
}
/** Play one musical eighth subdivision (never a background timer). */
export function playBeatsGroovePulse(ref:BeatAudioRef,enabled:boolean,
 pulse:number,bpm:number,lanes:readonly number[]=[]):void{
 if(!enabled)return;
 try{
  const ctx=contextFor(ref);
  hat(ctx,pulse%2===0?.017:.011);
   // The lead follows the exact lanes scheduled for this pulse, not a separate song.
   for(const lane of lanes){
    if(!Number.isInteger(lane)||lane<0||lane>=NOTE_FREQUENCIES.length)continue;
    const frequency=NOTE_FREQUENCIES[lane];
    synth(ctx,frequency,'triangle',.056,.20,1);
    synth(ctx,frequency*2,'sine',.014,.14,1);
   }
  if(pulse%2!==0)return;
  const beat=Math.floor(pulse/2),within=beat%4;
  const energy=Math.min(1,(bpm-104)/80);
  if(within===0||within===2){
   // Kick: short downward pitch envelope and a subtle click transient.
   synth(ctx,138,'sine',.115+energy*.02,.17,.38);
   synth(ctx,980,'triangle',.012,.021,.55);
  }else{
   // Snare: filtered noise and a soft lower-mid body.
   synth(ctx,206,'triangle',.060,.085,.64);
   synth(ctx,1700,'sawtooth',.013,.054,.63);
  }
  if(within===0||within===2||bpm>145){
   const root=ROOTS[Math.floor(beat/16)%ROOTS.length];
   synth(ctx,root,'triangle',.017,.19,.98);
   // Keep the low accompaniment quiet; the chart notes carry the melody.
  }
 }catch{/* A suspended/unsupported audio device never blocks the game. */}
}
/** A played lane is pitched to a coherent four-note D-minor arpeggio. */
export function playBeatsLaneHit(ref:BeatAudioRef,enabled:boolean,
 lane:number,grade:BeatsGrade):void{
 if(!enabled||lane<0||lane>=NOTE_FREQUENCIES.length)return;
 try{
  const ctx=contextFor(ref),f=NOTE_FREQUENCIES[lane];
  const volume=grade==='PERFECT'?.052:grade==='GREAT'?.039:.029;
  // A short confirmation accent sits above the synchronized lead.
  synth(ctx,f*2,'triangle',volume,.095,1.04);
  if(grade==='PERFECT')synth(ctx,f*3,'sine',.018,.09,1.02);
 }catch{/* Audio is optional. */}
}
export function playBeatsMiss(ref:BeatAudioRef,enabled:boolean):void{
 if(!enabled)return;
 try{
  const ctx=contextFor(ref);
  synth(ctx,166,'triangle',.039,.13,.58);
 }catch{/* Audio is optional. */}
}
/** Unlocks WebView2's audio context during the explicit START user gesture. */
export function unlockBeatsAudio(ref:BeatAudioRef,enabled:boolean):void{
 if(!enabled)return;
 try{void contextFor(ref);}catch{/* Keep keyboard play available without audio. */}
}
