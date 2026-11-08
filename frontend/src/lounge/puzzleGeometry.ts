/** Shared complementary silhouettes. Geometry is visual; native coordinates and snapping stay unchanged. */
export type Segment={kind:'M'|'L'|'C';values:number[]};
export function pieceSegments(id:number,columns:number,rows:number,w:number,h:number):Segment[]{
 const col=id%columns,row=Math.floor(id/columns),depth=Math.min(w,h)*.19;
 const result:Segment[]=[{kind:'M',values:[0,0]}];
 function edge(x:number,y:number,dx:number,dy:number,tab:number){
  const px=dy/Math.hypot(dx,dy)*depth*tab,py=-dx/Math.hypot(dx,dy)*depth*tab;
  const at=(t:number,k=0)=>[x+dx*t+px*k,y+dy*t+py*k];
  if(!tab){result.push({kind:'L',values:at(1)});return;}
  result.push({kind:'L',values:at(.34)});
  result.push({kind:'C',values:[...at(.46),...at(.32,1),...at(.5,1)]});
  result.push({kind:'C',values:[...at(.68,1),...at(.54),...at(.66)]});
  result.push({kind:'L',values:at(1)});
 }
 const sign=(n:number)=>n%2?1:-1;
 edge(0,0,w,0,row===0?0:-sign(row*columns+col));
 edge(w,0,0,h,col===columns-1?0:sign(row*columns+col+17));
 edge(w,h,-w,0,row===rows-1?0:sign((row+1)*columns+col));
 edge(0,h,0,-h,col===0?0:-sign(row*columns+col-1+17));
 return result;
}
export function piecePath(id:number,columns:number,rows:number,w:number,h:number){return pieceSegments(id,columns,rows,w,h).map(s=>s.kind+s.values.join(' ')).join(' ')+'Z';}
/** Stable mixed tray order: never expose the solved row-by-row order. */
export function trayRank(id:number,gameId:string){let h=2166136261;for(const c of gameId+':'+id)h=Math.imul(h^c.charCodeAt(0),16777619);return h>>>0;}
