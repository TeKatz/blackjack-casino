/**
 * Small original cabinet sound palette. No fetched, copied, or licensed retail samples.
 * A single AudioContext is created on user interaction and reused for every cue.
 */
export type RubyCue='bet'|'spin'|'stop'|'anticipation'|'elephant'|'hoof'|'bonus'|'win'|'big-win';
type ContextRef={current:AudioContext|null};

function chirp(ctx:AudioContext,at:number,from:number,to:number,length:number,
 gain:number,type:OscillatorType='sine'){
 const oscillator=ctx.createOscillator(),amp=ctx.createGain();
 oscillator.type=type;
 oscillator.frequency.setValueAtTime(Math.max(25,from),at);
 oscillator.frequency.exponentialRampToValueAtTime(Math.max(25,to),at+length);
 amp.gain.setValueAtTime(.0001,at);
 amp.gain.exponentialRampToValueAtTime(gain,at+Math.min(.035,length*.2));
 amp.gain.exponentialRampToValueAtTime(.0001,at+length);
 oscillator.connect(amp);amp.connect(ctx.destination);
 oscillator.start(at);oscillator.stop(at+length+.025);
}
function noise(ctx:AudioContext,at:number,duration:number,frequency:number,gain:number){
 const size=Math.ceil(ctx.sampleRate*duration);
 const buffer=ctx.createBuffer(1,size,ctx.sampleRate),pcm=buffer.getChannelData(0);
 // White noise belongs solely to the decorative cue. It can never alter game odds.
 for(let i=0;i<size;i++)pcm[i]=(Math.random()*2-1)*(.55+.45*Math.sin(i*.002));
 const source=ctx.createBufferSource();source.buffer=buffer;
 const filter=ctx.createBiquadFilter();filter.type='lowpass';filter.frequency.value=frequency;
 const amp=ctx.createGain();
 amp.gain.setValueAtTime(.0001,at);
 amp.gain.exponentialRampToValueAtTime(gain,at+.014);
 amp.gain.exponentialRampToValueAtTime(.0001,at+duration);
 source.connect(filter);filter.connect(amp);amp.connect(ctx.destination);
 source.start(at);source.stop(at+duration+.01);
}
function elephant(ctx:AudioContext,at:number){
 // Layered brass-like rising pitch + throaty subharmonic and breathy trunk resonance.
 chirp(ctx,at,112,290,.69,.062,'sawtooth');
 chirp(ctx,at+.025,167,430,.64,.024,'triangle');
 chirp(ctx,at+.24,235,340,.38,.028,'sawtooth');
 chirp(ctx,at+.56,292,118,.53,.059,'sawtooth');
 chirp(ctx,at+.63,160,96,.44,.026,'triangle');
 noise(ctx,at+.07,.56,1350,.018);
}
export function playRubyCue(ref:ContextRef,cue:RubyCue,enabled:boolean){
 if(!enabled)return;
 try{
  const ctx=ref.current??new AudioContext();
  ref.current=ctx;
  if(ctx.state==='suspended')void ctx.resume();
  const t=ctx.currentTime+.005;
  switch(cue){
   case 'bet':
    chirp(ctx,t,660,540,.055,.028,'triangle');break;
   case 'spin':
    noise(ctx,t,.30,890,.022);
    chirp(ctx,t,350,135,.28,.035,'triangle');
    chirp(ctx,t+.12,170,112,.24,.03,'sine');break;
   case 'stop':
    noise(ctx,t,.085,420,.018);
    chirp(ctx,t,160,75,.115,.054,'triangle');break;
   case 'anticipation':
    chirp(ctx,t,310,380,.22,.024,'sine');
    chirp(ctx,t+.27,380,480,.23,.027,'sine');break;
   case 'elephant':
    elephant(ctx,t);break;
   case 'hoof':
    for(let i=0;i<7;i++){
     const at=t+i*.21+(i%2)*.045;
     noise(ctx,at,.12,180,.032);
     chirp(ctx,at,83,48,.13,.045,'triangle');
    }break;
   case 'bonus':
    chirp(ctx,t,105,75,.72,.052,'sine');
    chirp(ctx,t+.08,340,520,.56,.05,'triangle');
    chirp(ctx,t+.18,525,740,.52,.023,'sine');
    elephant(ctx,t+.45);break;
   case 'win':
    [523,659,784,1047].forEach((hz,i)=>{
     chirp(ctx,t+i*.11,hz,hz*1.01,.28,.028,'sine');
     chirp(ctx,t+i*.11,hz/2,hz/2,.32,.013,'triangle');
    });break;
   case 'big-win':
    [392,523,659,784,1047,1319].forEach((hz,i)=>{
     chirp(ctx,t+i*.13,hz,hz*1.05,.46,.036,'triangle');
     chirp(ctx,t+i*.13,hz*2,hz*2,.35,.012,'sine');
    });
    noise(ctx,t+.45,.50,2400,.009);break;
  }
 }catch{
  // Missing audio devices and restricted WebViews must never block the slot.
 }
}
