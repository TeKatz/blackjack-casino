/** Tiny locally synthesised Tower Defense SFX; never changes combat or requests network audio. */
export type BastionCue='build'|'upgrade'|'sell'|'wave'|'archer'|'frost'|'cannon'|'kill'|'gate'|'victory'|'defeat';
type Tone={frequency:number;end?:number;delay?:number;duration:number;level:number;type:OscillatorType};
const cues:Readonly<Record<BastionCue,readonly Tone[]>>={
 build:[{frequency:293.66,end:349.23,duration:.13,level:.07,type:'triangle'},
  {frequency:440,delay:.09,duration:.23,level:.055,type:'sine'}],
 upgrade:[{frequency:349.23,duration:.15,level:.06,type:'triangle'},
  {frequency:523.25,delay:.1,duration:.2,level:.057,type:'sine'},
  {frequency:783.99,delay:.23,duration:.27,level:.04,type:'sine'}],
 sell:[{frequency:392,end:260,duration:.14,level:.052,type:'triangle'},
  {frequency:260,end:196,delay:.11,duration:.19,level:.042,type:'sine'}],
 wave:[{frequency:110,end:74,duration:.37,level:.09,type:'triangle'},
  {frequency:164.81,end:123.47,delay:.12,duration:.4,level:.047,type:'sawtooth'}],
 archer:[{frequency:550,end:210,duration:.075,level:.032,type:'triangle'},
  {frequency:880,end:420,delay:.018,duration:.07,level:.012,type:'sine'}],
 frost:[{frequency:920,end:580,duration:.2,level:.037,type:'sine'},
  {frequency:1320,end:820,delay:.035,duration:.22,level:.025,type:'triangle'}],
 cannon:[{frequency:115,end:43,duration:.3,level:.105,type:'triangle'},
  {frequency:67,end:35,delay:.017,duration:.31,level:.055,type:'sawtooth'}],
 kill:[{frequency:580,end:395,duration:.1,level:.025,type:'sine'}],
 gate:[{frequency:92,end:51,duration:.24,level:.07,type:'sawtooth'}],
 victory:[{frequency:261.63,duration:.35,level:.075,type:'triangle'},
  {frequency:329.63,delay:.12,duration:.48,level:.075,type:'sine'},
  {frequency:392,delay:.26,duration:.55,level:.075,type:'triangle'},
  {frequency:523.25,delay:.4,duration:.7,level:.060,type:'sine'}],
 defeat:[{frequency:174.61,end:130.81,duration:.36,level:.075,type:'triangle'},
  {frequency:130.81,end:65.4,delay:.2,duration:.6,level:.070,type:'sawtooth'}],
};
const minimumInterval:Readonly<Record<BastionCue,number>>={
 build:0,upgrade:0,sell:0,wave:0,archer:95,frost:130,cannon:180,kill:130,gate:120,victory:0,defeat:0,
};
export class BastionAudio{
 private context:AudioContext|null=null;
 private bus:GainNode|null=null;
 private enabled=false;
 private last=new Map<BastionCue,number>();
 setEnabled(enabled:boolean){
  this.enabled=enabled;
  if(this.bus && this.context)
   this.bus.gain.setTargetAtTime(enabled?.15:0,this.context.currentTime,.02);
 }
 /** Call only from a real interaction; background ticks cannot unlock WebAudio. */
 arm(){
  if(!this.enabled)return;
  try{
   if(!this.context){
    this.context=new AudioContext({latencyHint:'interactive'});
    this.bus=this.context.createGain();
    this.bus.gain.value=.15;
    this.bus.connect(this.context.destination);
   }
   if(this.context.state==='suspended')void this.context.resume().catch(()=>undefined);
  }catch{/* A missing audio device must never block the game. */}
 }
 play(cue:BastionCue){
  if(!this.enabled || !this.context || !this.bus || this.context.state!=='running')return;
  const now=performance.now(),last=this.last.get(cue)??-Infinity;
  if(now-last<minimumInterval[cue])return;
  this.last.set(cue,now);
  const context=this.context;
  for(const tone of cues[cue]){
   try{
    const start=context.currentTime+(tone.delay??0);
    const oscillator=context.createOscillator();
    const envelope=context.createGain();
    oscillator.type=tone.type;
    oscillator.frequency.setValueAtTime(tone.frequency,start);
    if(tone.end!==undefined)oscillator.frequency.exponentialRampToValueAtTime(tone.end,start+tone.duration);
    envelope.gain.setValueAtTime(0,start);
    envelope.gain.linearRampToValueAtTime(tone.level,start+.012);
    envelope.gain.exponentialRampToValueAtTime(.0001,start+tone.duration);
    oscillator.connect(envelope);envelope.connect(this.bus);
    oscillator.onended=()=>{oscillator.disconnect();envelope.disconnect();};
    oscillator.start(start);oscillator.stop(start+tone.duration+.02);
   }catch{/* Optional sound output; visuals and saves are independent. */}
  }
 }
 dispose(){
  const ctx=this.context;
  this.context=null;this.bus=null;this.last.clear();
  if(ctx)void ctx.close().catch(()=>undefined);
 }
}
