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
  return this.buffer(6.1,(data,rate)=>{
   let seed=1234567,low=0,phase=0,impact=0,ring=0;
   const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
   for(let i=0;i<data.length;i++){
    const t=i/rate,p=t/6.1, speed=Math.pow(1-p,1.8);
    const raw=noise();low+=.09*(raw-low);
    // Ivory rolling on the outer track, then discrete pocket impacts.
    phase+=(3+37*speed)/rate;if(phase>=1){phase-=1;impact=.45+speed*.25;ring=1;}
    impact*=Math.exp(-1/(rate*.008));ring*=Math.exp(-1/(rate*.022));
    const fade=Math.min(1,t/.07,(6.1-t)/.23);
    const track=(raw-low)*(.065+.10*speed)+low*.12;
    const click=impact*(raw*.45+Math.sin(t*2*Math.PI*1850)*.35)+ring*Math.sin(t*2*Math.PI*730)*.035;
    const drop=p>.57?Math.sin((p-.57)*Math.PI*12)*Math.exp(-(p-.57)*5)*.035:0;
    data[i]=(track+click+drop)*Math.max(0,fade);
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
 roll(){this.stopRoll();if(!this.context)return;try{this.rollBuffer??=this.makeRoll();this.rolling=this.play(this.rollBuffer,.65);}catch{/* optional audio */}}
 stopRoll(){if(this.rolling){try{this.rolling.stop();}catch{}this.rolling=null;}}
 win(){this.stopRoll();if(!this.context)return;try{if(this.outcome)try{this.outcome.stop();}catch{}this.winBuffer??=this.makeWin();this.outcome=this.play(this.winBuffer,.8);}catch{/* optional audio */}}
 dispose(){this.stopRoll();if(this.outcome)try{this.outcome.stop();}catch{}if(this.context)void this.context.close().catch(()=>{});this.context=null;this.outcome=null;this.rollBuffer=null;this.winBuffer=null;}
}
