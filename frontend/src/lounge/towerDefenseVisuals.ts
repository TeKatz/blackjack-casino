import type {TdEffect,TdEnemy,TdPoint,TowerKind} from './towerDefenseBridge';

/** Native effects live for three fixed ticks; derive a stable emission identity without changing saves. */
export function shotKey(ticks:number,effect:TdEffect):string{
 const firedTick=ticks-(3-effect.ticks);
 return [firedTick,effect.kind,effect.fromX,effect.fromY,effect.toX,effect.toY].join(':');
}
export type VisualShot=TdEffect&{key:string};
export function collectNewShots(ticks:number,effects:readonly TdEffect[],seen:Set<string>):VisualShot[]{
 const fresh:VisualShot[]=[];
 for(const fx of effects){
  const key=shotKey(ticks,fx);
  if(!seen.has(key)){
   seen.add(key);
   fresh.push({...fx,key});
  }
 }
 return fresh;
}
export function positionOnPath(path:readonly TdPoint[],distance:number):TdPoint{
 let d=Math.max(0,distance);
 for(let i=1;i<path.length;i++){
  const a=path[i-1],b=path[i],length=Math.hypot(b.x-a.x,b.y-a.y);
  if(d<=length)return{x:a.x+(b.x-a.x)*d/length,y:a.y+(b.y-a.y)*d/length};
  d-=length;
 }
 return path[path.length-1];
}
export const enemySpeed:Readonly<Record<TdEnemy['kind'],number>>={
 grunt:56,runner:96,brute:32,shield:43,
};
export function previewRange(kind:TowerKind,level:number):number{
 return kind==='cannon'?148+10*level:kind==='frost'?152+9*level:kind==='ballista'?205+11*level:kind==='flame'?128+9*level:162+12*level;
}
export function previewDamage(kind:TowerKind,level:number):number{
 return kind==='cannon'?30+16*level:kind==='frost'?9+7*level:kind==='ballista'?49+24*level:kind==='flame'?16+11*level:12+10*level;
}
export function interpolateEnemy(enemy:TdEnemy,path:readonly TdPoint[],
 elapsedSeconds:number,speed:1|2,running:boolean):TdPoint{
 const extra=running?Math.min(.098,Math.max(0,elapsedSeconds))*speed:0;
 return positionOnPath(path,enemy.distance+enemySpeed[enemy.kind]*extra*(enemy.slow>0?.5:1));
}
