/** Original, quiet Web Audio UI cues. No externally loaded audio or copyrighted music. */
import type {AlbumRarity} from './albumBridge';
let context:AudioContext|null=null;
export type AlbumCue='seal'|'flip'|'finish';
export function playAlbumCue(cue:AlbumCue,enabled:boolean,rarity:AlbumRarity='Common'):void{
 if(!enabled||typeof window==='undefined'||!window.AudioContext)return;
 try{
  context??=new AudioContext();
  const ctx=context;
  if(ctx.state==='suspended')void ctx.resume().catch(()=>{});
  const rank=['Common','Uncommon','Rare','Epic','Legendary'].indexOf(rarity);
  const start=ctx.currentTime+.006;
  const tone=(f:number,time:number,duration:number,level:number,shape:OscillatorType)=>{
   const o=ctx.createOscillator(),g=ctx.createGain();
   o.type=shape;
   o.frequency.setValueAtTime(f,time);
   o.frequency.exponentialRampToValueAtTime(Math.max(55,f*.63),time+duration);
   g.gain.setValueAtTime(.001,time);
   g.gain.exponentialRampToValueAtTime(Math.max(.002,level),time+.014);
   g.gain.exponentialRampToValueAtTime(.001,time+duration);
   o.connect(g).connect(ctx.destination);
   o.start(time);o.stop(time+duration+.015);
  };
  if(cue==='seal'){
   tone(110+rank*19,start,.18,.045,'triangle');
   tone(570+rank*40,start+.12,.35,.033,'sine');
  }else if(cue==='flip'){
   tone(290+rank*62,start,.085,.018,'triangle');
   tone(700+rank*85,start+.05,.2+rank*.045,.017+rank*.004,'sine');
   if(rank>=3)tone(1100+rank*100,start+.11,.29,.008,'sine');
  }else{
   tone(390,start,.22,.028,'triangle');
   tone(580,start+.09,.33,.022,'sine');
   tone(770,start+.17,.38,.02,'sine');
  }
 }catch{/* Audio permission or device failure must never affect pack transactions. */}
}
