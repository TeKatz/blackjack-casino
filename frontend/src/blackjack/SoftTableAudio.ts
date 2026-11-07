/**
 * Soft, original table Foley: felt/card movement, muted chips, and a bespoke
 * four-note win flourish. All oscillators/noise are synthesised locally.
 * Never influences C# game state or contacts a remote service.
 */
export class SoftTableAudio{
  private context:AudioContext|null=null;
  private noise:AudioBuffer|null=null;
  private disposed=false;
  /** AudioContext creation/resume must happen in a user gesture. */
  arm(enabled:boolean){
    if(!enabled||this.disposed||typeof AudioContext==='undefined')return;
    try{
      if(!this.context)this.context=new AudioContext();
      if(this.context.state==='suspended')void this.context.resume().catch(()=>{});
    }catch{/* Missing/unavailable audio devices must never interrupt a hand. */}
  }
  private ready(enabled:boolean):AudioContext|null{
    return enabled&&!this.disposed&&this.context?.state==='running'?this.context:null;
  }
  private tone(ctx:AudioContext,start:number,hz:number,duration:number,volume:number,
    type:OscillatorType='sine'){
    const osc=ctx.createOscillator(),gain=ctx.createGain();
    osc.type=type;osc.frequency.setValueAtTime(hz,start);
    gain.gain.setValueAtTime(.0001,start);
    gain.gain.exponentialRampToValueAtTime(Math.max(.0002,volume),start+.025);
    gain.gain.exponentialRampToValueAtTime(.0001,start+duration);
    osc.connect(gain).connect(ctx.destination);
    osc.start(start);osc.stop(start+duration+.01);
  }
  private noiseBuffer(ctx:AudioContext):AudioBuffer{
    if(this.noise)return this.noise;
    const length=Math.round(ctx.sampleRate*.20);
    const buffer=ctx.createBuffer(1,length,ctx.sampleRate),samples=buffer.getChannelData(0);
    // A deterministic texture. Randomness never touches the gameplay RNG.
    let seed=0x96e7b12;
    for(let i=0;i<samples.length;i++){
      seed=(Math.imul(seed,1664525)+1013904223)>>>0;
      samples[i]=(seed/2147483648-1)*.38;
    }
    this.noise=buffer;return buffer;
  }
  /** Felt shuffle + padded tactile impact, staggered for newly dealt cards. */
  cards(count:number,enabled:boolean){
    const ctx=this.ready(enabled);if(!ctx)return;
    const n=Math.min(4,Math.max(0,count));
    for(let i=0;i<n;i++){
      const start=ctx.currentTime+.015+i*.09;
      const source=ctx.createBufferSource(),filter=ctx.createBiquadFilter(),env=ctx.createGain();
      source.buffer=this.noiseBuffer(ctx);filter.type='lowpass';
      filter.frequency.setValueAtTime(1300,start);
      filter.frequency.exponentialRampToValueAtTime(530,start+.14);
      env.gain.setValueAtTime(.0001,start);
      env.gain.exponentialRampToValueAtTime(.048,start+.023);
      env.gain.exponentialRampToValueAtTime(.0001,start+.15);
      source.connect(filter).connect(env).connect(ctx.destination);
      source.start(start);source.stop(start+.16);
      this.tone(ctx,start+.11,160+i*9,.085,.016,'triangle');
    }
  }
  chip(enabled:boolean){
    const ctx=this.ready(enabled);if(!ctx)return;
    const now=ctx.currentTime;
    this.tone(ctx,now,415,.11,.031,'sine');
    this.tone(ctx,now+.024,610,.09,.013,'triangle');
  }
  /** Warm non-jarring win signature: rising bell tones with a velvet tail. */
  win(enabled:boolean,blackjack:boolean){
    const ctx=this.ready(enabled);if(!ctx)return;
    const now=ctx.currentTime+.23;
    const notes=blackjack?[523.25,659.25,783.99,1046.5]:[392,523.25,659.25,783.99];
    notes.forEach((hz,i)=>{
      const at=now+i*.135;
      this.tone(ctx,at,hz,.41-i*.018,.042,'sine');
      this.tone(ctx,at,hz*.5,.32,.011,'triangle');
    });
    this.tone(ctx,now+.58,notes[3]*.5,.64,.014,'sine');
  }
  dispose(){
    this.disposed=true;
    const ctx=this.context;
    this.context=null;this.noise=null;
    if(ctx)void ctx.close().catch(()=>{});
  }
}
