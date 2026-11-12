/** Original, offline sound design. No asset downloads, loops or external audio. */
export class RouletteAudio {
 private context:AudioContext|null=null;
 private rolling:AudioBufferSourceNode|null=null;
 private rollBuffer:AudioBuffer|null=null;
 private winBuffer:AudioBuffer|null=null;
 private outcome:AudioBufferSourceNode|null=null;
 unlock(){try{this.context??=new AudioContext();void this.context.resume().catch(()=>{});}catch{/* optional audio */}}
 private buffer(seconds:number,render:(data:Float32Array,rate:number)=>void){
  const ctx=this.context!;const b=ctx.createBuffer(1,Math.ceil(ctx.sampleRate*seconds),ctx.sampleRate);
  render(b.getChannelData(0),ctx.sampleRate);return b;
 }
 private play(buffer:AudioBuffer,volume:number){
  const ctx=this.context!;const node=ctx.createBufferSource(),gain=ctx.createGain();
  node.buffer=buffer;gain.gain.value=volume;node.connect(gain);gain.connect(ctx.destination);
  node.onended=()=>{node.disconnect();gain.disconnect();};node.start();return node;
 }
 private makeRoll(){
  // Subtle felt-and-wood roll, independent of the settled roulette outcome.
  return this.buffer(6.1,(data,rate)=>{
   let seed=1234567,low=0,phase=0,tap=0;
   const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
   for(let i=0;i<data.length;i++){
    const t=i/rate,p=t/6.1,speed=Math.pow(1-p,1.65),raw=noise();
    low+=.006*(raw-low);
    phase+=(1.8+12*speed)/rate;
    if(phase>=1){phase-=1;tap=1;}
    tap*=Math.exp(-1/(rate*.017));
    const fade=Math.max(0,Math.min(1,t/.14,(6.1-t)/.34));
    const felt=low*(.055+.025*speed)+(raw-low)*.0035;
    const woodenTap=tap*(Math.sin(t*2*Math.PI*390)*.028+Math.sin(t*2*Math.PI*590)*.009);
    data[i]=(felt+woodenTap)*fade;
   }
  });
 }
 private makeWin(){
  return this.buffer(1.65,(data,rate)=>{
   const notes=[{f:523.25,t:0},{f:659.25,t:.11},{f:783.99,t:.22},{f:1046.5,t:.36},{f:1318.51,t:.49}];
   for(let i=0;i<data.length;i++){const t=i/rate;let sample=0;for(const n of notes){const a=t-n.t;if(a<0)continue;const e=Math.min(1,a/.014)*Math.exp(-a*4.7);sample+=e*(Math.sin(a*n.f*2*Math.PI)*.16+Math.sin(a*n.f*4*Math.PI)*.06+Math.sin(a*n.f*6.02*Math.PI)*.025);}
    data[i]=sample*Math.min(1,(1.65-t)/.12);
   }
  });
 }
 roll(){this.stopRoll();if(!this.context)return;try{this.rollBuffer??=this.makeRoll();this.rolling=this.play(this.rollBuffer,.33);}catch{/* optional audio */}}
 stopRoll(){if(this.rolling){try{this.rolling.stop();}catch{}this.rolling=null;}}
 win(){this.stopRoll();if(!this.context)return;try{if(this.outcome)try{this.outcome.stop();}catch{}this.winBuffer??=this.makeWin();this.outcome=this.play(this.winBuffer,.8);}catch{/* optional audio */}}
 dispose(){this.stopRoll();if(this.outcome)try{this.outcome.stop();}catch{}if(this.context)void this.context.close().catch(()=>{});this.context=null;this.outcome=null;this.rollBuffer=null;this.winBuffer=null;}
}
