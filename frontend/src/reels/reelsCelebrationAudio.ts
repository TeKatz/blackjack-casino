/** Original, short Web Audio celebration cue. No downloaded soundtrack.
 * Initialized only by user gestures. Never affects native spin or payout logic. */
let context:AudioContext|null=null;
export function armReelsCelebrationAudio():void{
 try{
  if(typeof AudioContext==='undefined')return;
  context??=new AudioContext();
  if(context.state==='suspended')void context.resume().catch(()=>{});
 }catch{/* Some Windows WebView2 configurations disable browser audio. */}
}
export function playReelsCelebrationAudio(mega:boolean):void{
 if(!context||context.state!=='running')return;
 try{
  const at=context.currentTime+.015;
  const notes=mega?[392,523.25,659.25,783.99,1046.5,1318.51]:[440,554.37,659.25,880];
  notes.forEach((hz,i)=>{
   const begin=at+i*(mega?.108:.112);
   const oscillator=context!.createOscillator();
   const gain=context!.createGain();
   oscillator.type=i%2===0?'sine':'triangle';
   oscillator.frequency.setValueAtTime(hz,begin);
   gain.gain.setValueAtTime(0,begin);
   gain.gain.linearRampToValueAtTime(mega?.055:.043,begin+.015);
   gain.gain.exponentialRampToValueAtTime(.0001,begin+.26);
   oscillator.connect(gain);gain.connect(context!.destination);
   oscillator.start(begin);oscillator.stop(begin+.28);
   oscillator.onended=()=>{oscillator.disconnect();gain.disconnect();};
  });
 }catch{/* Audio failures must never affect the casino or autospin sequence. */}
}
export function disposeReelsCelebrationAudio():void{
 const old=context;context=null;
 if(old)void old.close().catch(()=>{});
}
