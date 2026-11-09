/** Hand-built Midnight Sweep sonic palette: bell glass, brushed metal and submerged bass.
 * Pure WebAudio synthesis, no reused assets and no network I/O. */
export type SweepCue='start'|'reveal'|'flag'|'unflag'|'scan'|'chord'|'win'|'lose';
type Note={hz:number;to?:number;at?:number;time:number;volume:number;wave:OscillatorType};
const score:Record<SweepCue,Note[]>={
 start:[{hz:196,to:294,time:.25,volume:.046,wave:'triangle'},{hz:587.33,at:.14,time:.36,volume:.035,wave:'sine'}],
 reveal:[{hz:460,to:720,time:.082,volume:.038,wave:'sine'},{hz:210,to:115,time:.088,volume:.019,wave:'triangle'}],
 flag:[{hz:780,to:1047,time:.11,volume:.04,wave:'triangle'},{hz:1568,at:.065,time:.14,volume:.018,wave:'sine'}],
 unflag:[{hz:560,to:340,time:.13,volume:.036,wave:'triangle'}],
 scan:[{hz:230,to:910,time:.48,volume:.052,wave:'sine'},{hz:1840,to:1280,at:.14,time:.45,volume:.028,wave:'sine'},
       {hz:116,to:78,time:.37,volume:.027,wave:'triangle'}],
 chord:[{hz:400,to:750,time:.15,volume:.044,wave:'triangle'}],
 win:[{hz:293.66,time:.35,volume:.046,wave:'triangle'},{hz:440,at:.12,time:.48,volume:.046,wave:'sine'},
      {hz:587.33,at:.27,time:.55,volume:.05,wave:'sine'},{hz:880,at:.43,time:.77,volume:.045,wave:'triangle'},
      {hz:1174.66,at:.62,time:.9,volume:.025,wave:'sine'}],
 lose:[{hz:136,to:57,time:.6,volume:.078,wave:'sawtooth'},
       {hz:196,to:92,at:.15,time:.52,volume:.034,wave:'triangle'},
       {hz:422,to:105,at:.27,time:.42,volume:.017,wave:'sine'}],
};
export class SweepAudio{
 private ctx:AudioContext|null=null;
 private master:GainNode|null=null;
 private enabled=false;
 private lastCue:Record<string,number>={};
 setEnabled(value:boolean){this.enabled=value;if(this.ctx&&this.master)this.master.gain.setTargetAtTime(value?.18:0,this.ctx.currentTime,.025);}
 /** Only user input may unlock the device. */
 arm(){
  if(!this.enabled)return;
  try{
   if(!this.ctx){
    this.ctx=new AudioContext({latencyHint:'interactive'});
    this.master=this.ctx.createGain();this.master.gain.value=.18;this.master.connect(this.ctx.destination);
   }
   if(this.ctx.state==='suspended')void this.ctx.resume().catch(()=>undefined);
  }catch{/* Audio is optional. */}
 }
 play(cue:SweepCue){
  const ctx=this.ctx,bus=this.master;
  if(!this.enabled||!ctx||!bus||ctx.state!=='running')return;
  const now=performance.now();
  if(now-(this.lastCue[cue]??-Infinity)<(cue==='reveal'?45:80))return;
  this.lastCue[cue]=now;
  for(const n of score[cue]){
   try{
    const t=ctx.currentTime+(n.at??0);
    const osc=ctx.createOscillator(),gain=ctx.createGain();
    osc.type=n.wave;
    osc.frequency.setValueAtTime(n.hz,t);
    if(n.to)osc.frequency.exponentialRampToValueAtTime(Math.max(20,n.to),t+n.time);
    gain.gain.setValueAtTime(.0001,t);
    gain.gain.exponentialRampToValueAtTime(n.volume,t+.013);
    gain.gain.exponentialRampToValueAtTime(.0001,t+n.time);
    osc.connect(gain);gain.connect(bus);
    osc.onended=()=>{osc.disconnect();gain.disconnect();};
    osc.start(t);osc.stop(t+n.time+.02);
   }catch{/* No interaction depends on a working sound card. */}
  }
 }
 dispose(){const c=this.ctx;this.ctx=null;this.master=null;if(c)void c.close().catch(()=>undefined);}
}
