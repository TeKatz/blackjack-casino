import {memo} from 'react';
import type {CSSProperties} from 'react';

const files:Record<string,string>={'high-limit-slots':'high-limit-slots',poker:'poker-room',dining:'fine-dining',stage:'grand-stage','private-salon':'private-gaming-salon',hotel:'luxury-hotel',spa:'spa-wellness','sky-bar':'sky-bar','convention-hall':'convention-hall','penthouse-club':'penthouse-club'};
export function roomArt(id:string):CSSProperties {
  return {backgroundImage:`url('./empire-art/${files[id]??'rooms-atlas'}.png')`};
}
export const EmpireArtwork=memo(function EmpireArtwork({roomId,className}:{roomId:string;className:string}){
  if(files[roomId])return <div className={className} style={roomArt(roomId)} aria-hidden="true"/>;
  const index=Math.max(0,['slots','tables','bar','roulette','vip'].indexOf(roomId));
  // An exact source-tile viewport, then aspect-preserving cover into any DOM box.
  // SVG clipping avoids CSS percentage/container-size ambiguity on button elements.
  return <svg className={className+' empire-atlas-art'} viewBox="0 0 512 512" preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
    <image href="./empire-art/rooms-atlas.png" x={-(index%3)*512} y={-Math.floor(index/3)*512} width={1536} height={1024}/>
  </svg>;
});
