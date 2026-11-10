/**
 * Arcade's self-contained Web Audio sound design. These twenty independent
 * synth voices are original, procedural and never require network assets.
 * Playback is presentation only: it cannot change deterministic game outcomes.
 */
import type {PlayableArcadeId} from './arcadeEngine';
export type ArcadeSoundCue='flap'|'score'|'stage'|'fail'|'pad'|'perfect'|'good'|
 'miss'|'bumper'|'launch'|'flip'|'life'|'solve'|'drill'|'dock'|'caught'|'short'|
 'collect'|'collision'|'jump'|'hook'|'release'|'victory'|'mineTick';
type Wave='sine'|'triangle'|'square'|'sawtooth';
export type ArcadeVoice={base:number;wave:Wave;partial:number;attack:number;
 decay:number;intervals:readonly number[];filter:number};
export const ECHO_PAD_NOTES=[261.63,329.63,392,493.88,587.33,698.46] as const;
/** Each game has a distinct carrier, harmonic character and melodic contour. */
export const ARCADE_VOICES:Record<PlayableArcadeId,ArcadeVoice>={
 'midnight-wings':{base:520,wave:'sine',partial:1.55,attack:.015,decay:.17,intervals:[0,7,12],filter:2600},
 'cave-escape':{base:194,wave:'triangle',partial:1.98,attack:.055,decay:.22,intervals:[0,3,7],filter:1150},
 'perfect-stack':{base:385,wave:'sine',partial:2.95,attack:.005,decay:.35,intervals:[0,5,12],filter:3300},
 'blade-timing':{base:282,wave:'square',partial:2.65,attack:.003,decay:.08,intervals:[0,9,14],filter:1650},
 'death-snake':{base:165,wave:'triangle',partial:1.2,attack:.007,decay:.11,intervals:[0,-3,-5],filter:970},
 'neon-breaker':{base:620,wave:'sine',partial:2.13,attack:.001,decay:.12,intervals:[0,12,19],filter:4300},
 'bullet-storm':{base:122,wave:'sawtooth',partial:1.52,attack:.003,decay:.13,intervals:[0,-4,-12],filter:1850},
 'hexagon-hell':{base:430,wave:'triangle',partial:1.41,attack:.009,decay:.14,intervals:[0,4,8],filter:2420},
 'spike-rush':{base:348,wave:'square',partial:1.87,attack:.003,decay:.1,intervals:[0,7,15],filter:1900},
 'endless-ascent':{base:465,wave:'sine',partial:2.51,attack:.018,decay:.23,intervals:[0,5,12],filter:3050},
 'midnight-beats':{base:293.66,wave:'triangle',partial:2,attack:.003,decay:.16,intervals:[0,3,7,10],filter:2800},
 'phantom-hook':{base:232,wave:'sine',partial:1.79,attack:.04,decay:.31,intervals:[0,5,9],filter:2150},
 'neon-drift':{base:115,wave:'sawtooth',partial:2.01,attack:.05,decay:.2,intervals:[0,-5,4],filter:1100},
 'gravity-chamber':{base:310,wave:'triangle',partial:1.3,attack:.03,decay:.39,intervals:[0,-7,7],filter:1380},
 'echo-sequence':{base:262,wave:'sine',partial:2.0,attack:.012,decay:.27,intervals:[0,4,7],filter:3300},
 'prism-lock':{base:748,wave:'sine',partial:1.42,attack:.06,decay:.33,intervals:[0,12,7],filter:5000},
 'meteor-miner':{base:192,wave:'triangle',partial:3.31,attack:.003,decay:.22,intervals:[0,-5,10],filter:1250},
 'circuit-rush':{base:538,wave:'square',partial:1.17,attack:.002,decay:.12,intervals:[0,7,12],filter:2230},
 'shadow-heist':{base:207,wave:'triangle',partial:1.73,attack:.008,decay:.17,intervals:[0,-2,5],filter:930},
 'pulse-perfect':{base:660,wave:'sine',partial:2.46,attack:.006,decay:.53,intervals:[0,7,12,19],filter:4800},
};
export type ArcadeSoundRef={current:AudioContext|null};
/** Pure, testable input for the audio graph. Pad frequencies are distinct. */
export function soundNotes(id:PlayableArcadeId,cue:ArcadeSoundCue,pad?:number):number[]{
 const v=ARCADE_VOICES[id];
 if(cue==='pad'&&id==='echo-sequence'&&Number.isInteger(pad)&&
  pad!==undefined&&pad>=0&&pad<ECHO_PAD_NOTES.length)return [ECHO_PAD_NOTES[pad]];
 if(cue==='perfect')return [v.base,v.base*1.5,v.base*2,v.base*2.5];
 if(cue==='good')return [v.base,v.base*1.5];
 if(cue==='miss'||cue==='fail'||cue==='life'||cue==='caught'||cue==='collision'||cue==='short')
  return [v.base*.6,v.base*.42];
 if(cue==='bumper')return [v.base,v.base*2.01,v.base*3.88];
 if(cue==='launch')return [v.base*.64,v.base*1.11];
 if(cue==='flip')return [v.base*.48];
 if(cue==='jump')return [v.base*.76];
 if(cue==='hook')return [v.base*.92,v.base*1.31];
 if(cue==='release')return [v.base*.9];
 if(cue==='mineTick')return [v.base*2.6];
 if(cue==='victory')return [v.base*.75,v.base,v.base*1.26,v.base*1.5,v.base*2];
 if(cue==='drill')return [v.base*.7,v.base*1.64];
 if(cue==='dock')return [v.base*.8,v.base*1.26,v.base*1.6];
 if(cue==='solve')return [v.base,v.base*1.25,v.base*1.5,v.base*2];
 if(cue==='score'||cue==='collect'){
  return v.intervals.map(n=>v.base*Math.pow(2,n/12));
 }
 if(cue==='stage')return [v.base*.8,v.base*1.2];
 return [v.base];
}
export function soundProfileSignature(id:PlayableArcadeId):string{
 const v=ARCADE_VOICES[id];
 return [v.base,v.wave,v.partial,v.attack,v.decay,v.filter,...v.intervals].join('|');
}
export function playArcadeSound(id:PlayableArcadeId,cue:ArcadeSoundCue,
 enabled:boolean,contextRef:ArcadeSoundRef,pad?:number):void{
 if(!enabled)return;
 try{
  const context=contextRef.current??new AudioContext();
  contextRef.current=context;
  if(context.state==='suspended')void context.resume();
  const profile=ARCADE_VOICES[id],notes=soundNotes(id,cue,pad);
  const special=cue==='perfect'||cue==='bumper'||cue==='solve'||cue==='victory';
  const miss=['miss','fail','life','caught','collision','short'].includes(cue);
  const decay=cue==='mineTick'?.065:cue==='jump'?.14:cue==='hook'?.14:cue==='release'?.12:cue==='victory'?.25:cue==='pad'?.24:special?.43:miss?.24:profile.decay;
  const amplitude=cue==='mineTick'?.022:cue==='jump'?.052:cue==='victory'?.058:cue==='pad'?.055:miss?.07:special?.068:.044;
  const attack=cue==='jump'||cue==='mineTick'?.003:Math.max(.002,profile.attack);
  const filter=context.createBiquadFilter();
  filter.type='lowpass';
  filter.frequency.setValueAtTime(miss?Math.min(1200,profile.filter):profile.filter,context.currentTime);
  filter.Q.value=1.0;
  filter.connect(context.destination);
  const baseTime=context.currentTime;
  const perNote=notes.length>1?(cue==='victory'?.105:special?.053:.067):0;
  let remaining=notes.length*2;
  const voiceNodes:GainNode[]=[];
  for(let i=0;i<notes.length;i++){
   const f=Math.max(38,notes[i]),t=baseTime+i*perNote,duration=decay+(special?.08:0);
   const voice=context.createGain();
   voice.gain.setValueAtTime(.0001,t);
   voice.gain.exponentialRampToValueAtTime(amplitude/(1+i*.3),t+attack);
   voice.gain.exponentialRampToValueAtTime(.0001,t+duration);
   voice.connect(filter);voiceNodes.push(voice);
   for(let h=0;h<2;h++){
    const oscillator=context.createOscillator();
    oscillator.type=h===0?(cue==='jump'||cue==='victory'?'triangle':profile.wave):'sine';
    const pitch=h===0?f:f*profile.partial;
    oscillator.frequency.setValueAtTime(pitch,t);
    // Distinct glides for hook, gravity and timing; no sound touches gameplay.
    const bend=cue==='jump'?1.83:cue==='hook'?1.3:cue==='release'?.67:cue==='mineTick'?.82:id==='phantom-hook'?1.17:id==='gravity-chamber'?0.75:
     id==='neon-drift'?0.89:miss?0.62:1.03;
    oscillator.frequency.exponentialRampToValueAtTime(Math.max(30,pitch*bend),
     t+duration*0.84);
    oscillator.connect(voice);
    oscillator.onended=()=>{
     oscillator.disconnect();
     remaining--;
     if(remaining===0){for(const node of voiceNodes)node.disconnect();filter.disconnect();}
    };
    oscillator.start(t);oscillator.stop(t+duration+.015);
   }
  }
 }catch{
  // Muted/autoplay-restricted WebView2 hosts keep gameplay fully functional.
 }
}
