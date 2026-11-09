import type {ArcadeControls,ArcadeDirection} from './arcadeEngine';

/** Track physical keys independently; A+Left must not release the shared direction early. */
const codes:Record<string,ArcadeDirection>={
 KeyA:'left',ArrowLeft:'left',KeyD:'right',ArrowRight:'right',
 KeyW:'up',ArrowUp:'up',KeyS:'down',ArrowDown:'down',
};
const names:Record<string,ArcadeDirection>={
 a:'left',arrowleft:'left',d:'right',arrowright:'right',
 w:'up',arrowup:'up',s:'down',arrowdown:'down',
};
export function arcadeDirection(code:string,key:string):ArcadeDirection|null{
 return codes[code]??names[key.toLowerCase()]??null;
}
const keyIdentity=(code:string,key:string)=>code||key.toLowerCase();
export class ArcadeHeldKeys {
 private held=new Map<string,ArcadeDirection>();
 press(code:string,key:string):ArcadeDirection|null{
  const direction=arcadeDirection(code,key);
  if(direction)this.held.set(keyIdentity(code,key),direction);
  return direction;
 }
 release(code:string,key:string):void{this.held.delete(keyIdentity(code,key));}
 clear():void{this.held.clear();}
 controls():ArcadeControls{
  const values=new Set(this.held.values());
  return {left:values.has('left'),right:values.has('right'),
   up:values.has('up'),down:values.has('down')};
 }
}
