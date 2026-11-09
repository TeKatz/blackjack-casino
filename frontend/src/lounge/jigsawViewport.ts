/** Jigsaw camera and safe placement math, in the stable 960 × 600 game space. */
export type PuzzleView={zoom:number;x:number;y:number};
export type PuzzleFrame={width:number;height:number};
export const BOARD_WIDTH=960,BOARD_HEIGHT=600;
export function viewport(frame:PuzzleFrame,view:PuzzleView){
 const fit=Math.max(.001,Math.min(Math.max(1,frame.width)/BOARD_WIDTH,Math.max(1,frame.height)/BOARD_HEIGHT));
 const visibleW=frame.width/fit,visibleH=frame.height/fit;
 const maxX=Math.max(0,(BOARD_WIDTH*view.zoom-visibleW)/2);
 const maxY=Math.max(0,(BOARD_HEIGHT*view.zoom-visibleH)/2);
 const x=maxX===0?0:Math.max(-maxX,Math.min(maxX,view.x));
 const y=maxY===0?0:Math.max(-maxY,Math.min(maxY,view.y));
 return {fit,x,y,maxX,maxY,
  offsetX:(visibleW-BOARD_WIDTH*view.zoom)/2+x,
  offsetY:(visibleH-BOARD_HEIGHT*view.zoom)/2+y};
}
export function boundedView(frame:PuzzleFrame,view:PuzzleView):PuzzleView{
 const zoom=Math.max(1,Math.min(3,view.zoom));
 const bounds=viewport(frame,{...view,zoom});
 return {zoom,x:bounds.x,y:bounds.y};
}
export function toBoard(frame:PuzzleFrame,view:PuzzleView,x:number,y:number){
 const g=viewport(frame,view);
 return {x:(x/g.fit-g.offsetX)/view.zoom,y:(y/g.fit-g.offsetY)/view.zoom};
}
export function clampPiece(x:number,y:number,columns:number,rows:number){
 return {x:Math.max(0,Math.min(BOARD_WIDTH-BOARD_WIDTH/columns,x)),
  y:Math.max(0,Math.min(BOARD_HEIGHT-BOARD_HEIGHT/rows,y))};
}
export function clampGroupMotion(
 pieces:ReadonlyArray<{x:number;y:number}>,group:readonly number[],
 id:number,targetX:number,targetY:number,columns:number,rows:number){
 const w=BOARD_WIDTH/columns,h=BOARD_HEIGHT/rows;
 const minimumX=Math.max(...group.map(i=>-pieces[i].x));
 const maximumX=Math.min(...group.map(i=>BOARD_WIDTH-w-pieces[i].x));
 const minimumY=Math.max(...group.map(i=>-pieces[i].y));
 const maximumY=Math.min(...group.map(i=>BOARD_HEIGHT-h-pieces[i].y));
 const dx=targetX-pieces[id].x,dy=targetY-pieces[id].y;
 return {dx:Math.max(minimumX,Math.min(maximumX,dx)),
  dy:Math.max(minimumY,Math.min(maximumY,dy))};
}
export function needsRecovery(piece:{onBoard:boolean;locked:boolean;x:number;y:number},columns:number,rows:number){
 return piece.onBoard&&!piece.locked&&(
  piece.x<-.01||piece.y<-.01||
  piece.x>BOARD_WIDTH-BOARD_WIDTH/columns+.01||
  piece.y>BOARD_HEIGHT-BOARD_HEIGHT/rows+.01);
}
