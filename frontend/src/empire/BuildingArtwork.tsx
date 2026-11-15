import {memo} from 'react';

const art:Record<string,string>={
  cashier:'luxury-hotel',poker:'poker-room',stage:'grand-stage',
  'high-limit':'high-limit-slots',hotel:'luxury-hotel',
  vault:'private-gaming-salon',penthouse:'penthouse-club',
};
const atlas:Record<string,number>={slots:0,tables:1,bar:2,roulette:3,vip:4};

/**
 * Reuses the existing hand-painted room collection instead of substituting
 * generic icons. The atlas uses exact 512px image cells.
 */
function BuildingArtwork({id,tier}:{id:string;tier:number}){
  const asset=art[id];const slice=atlas[id];
  return <span className={`ce-building-scene ce-scene-tier-${tier}`} aria-hidden="true">
    {asset?<img src={`./empire-art/${asset}.png`} draggable={false} alt="" decoding="async"/>:
      slice!==undefined?<svg className="ce-atlas-scene" viewBox="0 0 512 512"
        preserveAspectRatio="xMidYMid slice" focusable="false">
        <image href="./empire-art/rooms-atlas.png" width="1536" height="1024"
          x={-(slice%3)*512} y={-Math.floor(slice/3)*512}/>
      </svg>:
      <span className="ce-building-scene-fallback">♠</span>}
    <span className="ce-scene-vignette"/>
    <span className="ce-scene-light"/>
    <span className="ce-scene-corner"/>
  </span>;
}
export default memo(BuildingArtwork);
