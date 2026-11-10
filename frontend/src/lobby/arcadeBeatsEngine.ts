/**
 * Midnight Beats — deterministic endless four-lane rhythm game.
 *
 * A single pulse timeline drives BOTH note target timestamps and the procedural
 * musical groove. Inputs judge against those exact timestamps, never sprite
 * overlap or a wall-clock animation. Simulation uses the shared fixed 60 Hz step.
 */
export const BEATS_LANES=4;
export const BEATS_KEYS=['D','F','J','K'] as const;
export const BEATS_LANE_COLORS=['#7de8de','#f1c18c','#c4a2fb','#f58eb0'] as const;
export const BEATS_FIRST_PULSE_AT=.30;
export const BEATS_HIT_LINE_Y=418;
export const BEATS_START_Y=79;
export const BEATS_GOOD_WINDOW=.155;
export const BEATS_GREAT_WINDOW=.092;
export const BEATS_PERFECT_WINDOW=.046;
export type BeatsGrade='PERFECT'|'GREAT'|'GOOD'|'MISS';
export type BeatsJudgement=BeatsGrade|null;
export type BeatsNote={
 id:number;lane:number;at:number;travel:number;judgement:BeatsJudgement;
 judgedAt:number;
};
type Base={score:number;elapsed:number;over:boolean;seed:number};
export type BeatsRun=Base&{
 kind:'midnight-beats';notes:BeatsNote[];
 lives:number;combo:number;bestCombo:number;
 stage:number;bpm:number;successful:number;misses:number;
 perfect:number;great:number;good:number;
 nextPatternPulse:number;nextPatternAt:number;
 nextGroovePulse:number;nextGrooveAt:number;
 grooveCount:number;lastGroovePulse:number;
 lastGrade:BeatsGrade|null;lastLane:number;feedbackClock:number;
 stageClock:number;wrongLock:number;
 laneGlow:number[];lastHitPoints:number;
};
export type BeatsEvent='flap'|'score'|'stage'|'fail'|null;
const clamp=(v:number,a:number,b:number)=>Math.max(a,Math.min(b,v));
function random(run:Base){
 run.seed=(Math.imul(run.seed,1664525)+1013904223)>>>0;
 return run.seed/4294967296;
}
/** Speed increases at 4-bar boundaries, never in the middle of a measure. */
export function beatsStageForPulse(pulse:number):number{
 return 1+Math.floor(Math.max(0,pulse)/32);
}
export function beatsBpmForPulse(pulse:number):number{
 return Math.min(184,104+8*(beatsStageForPulse(pulse)-1));
}
/** Each pulse is one eighth-note. The same calculation powers sound and notes. */
export function beatsPulseDuration(pulse:number):number{
 return 30/beatsBpmForPulse(pulse);
}
export function beatsTravelTime(pulse:number):number{
 return Math.max(1.13,2.06-.075*(beatsStageForPulse(pulse)-1));
}
export function beatsTargetTime(pulse:number):number{
 let time=BEATS_FIRST_PULSE_AT;
 const fullStages=Math.floor(Math.max(0,pulse)/32);
 // Efficient, exact-on-the-same-grid lookup; all stage transitions are at pulses divisible by 32.
 for(let stage=0;stage<fullStages;stage++)time+=32*(30/beatsBpmForPulse(stage*32));
 time+=(Math.max(0,pulse)%32)*beatsPulseDuration(fullStages*32);
 return time;
}
export function createBeatsRun(seed=0x4a173a7b):BeatsRun{
 const run:BeatsRun={
  kind:'midnight-beats',score:0,elapsed:0,over:false,seed:(seed>>>0)||0x4a173a7b,
  notes:[],lives:5,combo:0,bestCombo:0,stage:1,bpm:104,
  successful:0,misses:0,perfect:0,great:0,good:0,
  nextPatternPulse:0,nextPatternAt:BEATS_FIRST_PULSE_AT,
  nextGroovePulse:0,nextGrooveAt:BEATS_FIRST_PULSE_AT,
  grooveCount:0,lastGroovePulse:-1,
  lastGrade:null,lastLane:-1,feedbackClock:0,stageClock:0,wrongLock:0,
  laneGlow:[0,0,0,0],lastHitPoints:0,
 };
 generateAhead(run);
 return run;
}
/**
 * No triple or quadruple chords. Every 8th pulse has at most two notes and is
 * separated by a minimum of one eighth-note from the next. Early runs use
 * single notes on quarter beats; half-beat syncopation appears later.
 */
function generateAtPulse(run:BeatsRun,pulse:number,at:number):void{
 if(pulse<8)return; // Four counted-in beats, with time to understand the lanes.
 const stage=beatsStageForPulse(pulse);
 const even=pulse%2===0;
 const syncChance=Math.min(.44,.10+.065*(stage-2));
 if(!even&&(stage<3||random(run)>=syncChance))return;
 const id=pulse*4;
 const primary=Math.floor(random(run)*BEATS_LANES);
 const travel=beatsTravelTime(pulse);
 run.notes.push({id:id+primary,lane:primary,at,travel,judgement:null,judgedAt:0});
 // Chords introduce themselves slowly. Opposite-spaced lanes remain comfortably playable.
 if(even&&stage>=3&&random(run)<Math.min(.40,.11+(stage-3)*.045)){
  const other=(primary+2)%4;
  run.notes.push({id:id+other,lane:other,at,travel,judgement:null,judgedAt:0});
 }
}
function generateAhead(run:BeatsRun):void{
 // Extra headroom lets the first note scroll smoothly from the very top.
 // This is a bounded, per-frame deterministic lookahead, never a pending timer.
 while(run.nextPatternAt<=run.elapsed+2.20){
  const index=run.nextPatternPulse;
  generateAtPulse(run,index,run.nextPatternAt);
  run.nextPatternAt+=beatsPulseDuration(index);
  run.nextPatternPulse++;
 }
}
function takeLife(run:BeatsRun,lane:number):BeatsEvent{
 run.lives--;run.misses++;run.combo=0;
 run.lastGrade='MISS';run.lastLane=lane;run.lastHitPoints=0;
 run.feedbackClock=.60;
 if(lane>=0&&lane<4)run.laneGlow[lane]=.26;
 if(run.lives<=0){run.over=true;return 'fail';}
 return 'stage';
}
export function hitBeatsLane(run:BeatsRun,lane:number):BeatsEvent{
 if(run.over||!Number.isInteger(lane)||lane<0||lane>=BEATS_LANES)return null;
 // Do not punish exploratory keypresses during the initial musical count-in.
 if(run.elapsed<beatsTargetTime(8)-BEATS_GOOD_WINDOW)return null;
 const nearest=run.notes.filter(n=>n.judgement===null&&n.lane===lane)
  .sort((a,b)=>Math.abs(a.at-run.elapsed)-Math.abs(b.at-run.elapsed))[0];
 const error=nearest?Math.abs(nearest.at-run.elapsed):Infinity;
 if(error>BEATS_GOOD_WINDOW){
  if(run.wrongLock>0)return null;
  run.wrongLock=.105;return takeLife(run,lane);
 }
 const grade:BeatsGrade=error<=BEATS_PERFECT_WINDOW?'PERFECT':
  error<=BEATS_GREAT_WINDOW?'GREAT':'GOOD';
 nearest.judgement=grade;nearest.judgedAt=run.elapsed;
 run.combo++;run.bestCombo=Math.max(run.bestCombo,run.combo);
 run.successful++;
 if(grade==='PERFECT')run.perfect++;
 else if(grade==='GREAT')run.great++;
 else run.good++;
 const multiplier=1+Math.min(7,Math.floor((run.combo-1)/10));
 const points=(grade==='PERFECT'?300:grade==='GREAT'?200:100)*multiplier;
 run.score+=points;run.lastHitPoints=points;
 run.lastGrade=grade;run.lastLane=lane;run.feedbackClock=.56;
 run.laneGlow[lane]=.27;
 return 'score';
}
export function stepBeatsRun(run:BeatsRun,dt:number):BeatsEvent{
 if(run.over||!Number.isFinite(dt)||dt<=0)return null;
 const step=Math.min(dt,1/60),before=run.elapsed;
 run.elapsed+=step;
 run.feedbackClock=Math.max(0,run.feedbackClock-step);
 run.stageClock=Math.max(0,run.stageClock-step);
 run.wrongLock=Math.max(0,run.wrongLock-step);
 for(let i=0;i<4;i++)run.laneGlow[i]=Math.max(0,run.laneGlow[i]-step);
 generateAhead(run);
 let event:BeatsEvent=null;
 // Count groove subdivisions on exactly the timeline on which note hits are authored.
 while(run.nextGrooveAt<=run.elapsed+1e-8){
  const pulse=run.nextGroovePulse;
  run.lastGroovePulse=pulse;
  run.nextGroovePulse++;
  run.nextGrooveAt+=beatsPulseDuration(pulse);
  run.grooveCount++;
  const nextStage=beatsStageForPulse(run.nextGroovePulse);
  if(nextStage>run.stage){
   run.stage=nextStage;run.bpm=beatsBpmForPulse(run.nextGroovePulse);
   run.stageClock=1.45;event='stage';
  }
 }
 // Resolve deadlines automatically. A missed chord can cost two lives because
 // its two notes are distinct targets, exactly as if two keys were released.
 for(const note of run.notes){
  if(note.judgement===null&&note.at+BEATS_GOOD_WINDOW<run.elapsed){
   note.judgement='MISS';note.judgedAt=run.elapsed;
   const loss=takeLife(run,note.lane);
   if(loss==='fail')return 'fail';
   event=loss;
  }
 }
 // Keep the visual tail for a fraction of a second; memory is bounded even
 // during long runs and regardless of whether the player hits any notes.
 run.notes=run.notes.filter(n=>n.judgement===null||run.elapsed-n.judgedAt<.35);
 if(run.elapsed<before)return null;
 return event;
}
