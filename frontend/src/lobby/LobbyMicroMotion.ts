import {Container, Graphics} from 'pixi.js';
import type {ModeSceneId} from './ModeScenes';

/**
 * Per-object GPU micro-interactions. All positions are in the 960x440 art
 * design space and the caller scales the container to the actual thumbnail.
 * No SVG/HTML animation, no timers or per-card WebGL contexts.
 */
export type MotionFrame={hover:number;progress:number;time:number;enabled:boolean};
export type MotionDetails={layer:Container;tick:(frame:MotionFrame)=>void};
const clamp=(value:number)=>Math.max(0,Math.min(1,value));
export const smooth=(value:number)=>{const x=clamp(value);return x*x*(3-2*x);};
export const delayed=(progress:number,delay:number)=>smooth((progress-delay)/(1-delay));
const gold=0xe8c17b;

function steam(layer:Container):MotionDetails {
  const trails=Array.from({length:3},(_,i)=>{
    const g=new Graphics();
    g.moveTo(0,27).bezierCurveTo(-14,-1,17,-22,0,-55)
      .bezierCurveTo(-10,-70,15,-85,0,-98)
      .stroke({width:5-i*.7,color:0xf4e1c0,alpha:.7});
    g.position.set(611+i*15,246-i*3);
    g.alpha=0;
    layer.addChild(g);
    return g;
  });
  return {layer,tick:({hover,time,enabled})=>{
    trails.forEach((g,i)=>{
      g.alpha=enabled?hover*(.34+.32*Math.sin(time*1.7+i*1.4)**2):0;
      g.y=246-i*3-hover*(8+i*4)+(enabled?Math.sin(time*1.25+i)*4*hover:0);
      g.x=611+i*15+(enabled?Math.sin(time*.72+i*.8)*5*hover:0);
      g.scale.set(1,1+hover*.22);
    });
  }};
}
function slotIcon(kind:number):Graphics {
  const g=new Graphics();
  if(kind===0){ // glowing cherries
    g.circle(-16,10,17).fill(0xb52339).stroke({width:4,color:0xe87e76});
    g.circle(18,4,17).fill(0xca293f).stroke({width:4,color:0xf7ad8e});
    g.moveTo(-16,-7).bezierCurveTo(-13,-35,21,-43,18,-14).stroke({width:6,color:0x436a46});
    g.moveTo(3,-29).lineTo(32,-45).stroke({width:7,color:0x5f9a51});
  }else if(kind===1){ // 7, drawn as a geometric symbol
    g.moveTo(-37,-35).lineTo(37,-35).lineTo(-14,42).lineTo(-39,42).lineTo(7,-8).lineTo(-37,-8).closePath()
      .fill(0xb52d41).stroke({width:5,color:0xf4a488});
  }else if(kind===2){ // bell
    g.moveTo(-32,30).bezierCurveTo(-8,0,-29,-38,0,-39)
      .bezierCurveTo(29,-38,8,0,32,30).closePath()
      .fill(gold).stroke({width:5,color:0x9d713f});
    g.circle(0,36,9).fill(0xa26d3c);
    g.moveTo(-36,30).lineTo(36,30).stroke({width:6,color:0xffe2a2});
  }else{
    g.roundRect(-32,-29,64,58,9).fill(0x272d46).stroke({width:6,color:gold});
    g.moveTo(-24,-7).lineTo(23,-7).stroke({width:9,color:0xddb16c});
  }
  return g;
}
function reels(layer:Container):MotionDetails{
  const strips:Container[]=[];
  [201,385,569].forEach((x,i)=>{
    const panel=new Graphics().roundRect(x,172,144,173,10)
      .fill(0xead7b3).stroke({width:6,color:0xbb9563});
    layer.addChild(panel);
    const mask=new Graphics().rect(x+7,179,130,159).fill(0xffffff);
    const strip=new Container();
    for(let j=-1;j<=2;j++){
      const symbol=slotIcon((i+j+4)%4);
      symbol.position.set(x+72,260+j*173);
      strip.addChild(symbol);
    }
    layer.addChild(mask,strip);
    strip.mask=mask;
    strips.push(strip);
    const edge=new Graphics().roundRect(x+5,177,134,161,5)
      .stroke({width:4,color:0xf8e9bf,alpha:.55});
    layer.addChild(edge);
  });
  return {layer,tick:({progress})=>{
    strips.forEach((strip,i)=>strip.y=-173*delayed(progress,i*.17));
  }};
}
function vault(layer:Container):MotionDetails{
  const rings:Container[]=[];
  [179,142,99].forEach((radius,i)=>{
    const ring=new Container();
    ring.position.set(474,222);
    const g=new Graphics().circle(0,0,radius)
      .stroke({width:i===0?10:7,color:i===1?0xd68dfd:0x79e9ff,alpha:.8});
    const segments=12+i*4;
    for(let j=0;j<segments;j++){
      const a=j*Math.PI*2/segments;
      g.moveTo(Math.cos(a)*(radius-10),Math.sin(a)*(radius-10))
        .lineTo(Math.cos(a)*(radius+4),Math.sin(a)*(radius+4))
        .stroke({width:4,color:i===1?0xf0b0ff:0xb7faff,alpha:.9});
    }
    ring.addChild(g);
    layer.addChild(ring);
    rings.push(ring);
  });
  let phase=0;
  return {layer,tick:({hover,enabled})=>{
    if(enabled)phase+=hover*.036;
    rings.forEach((ring,i)=>{
      ring.rotation=phase*(1-i*.22); // ALL clockwise
      ring.alpha=.18+hover*(.58-i*.08);
    });
  }};
}
function dieShape():Graphics{
  const g=new Graphics().roundRect(-67,-65,134,130,19)
    .fill(0x8d2746).stroke({width:7,color:0xe5b76d});
  for(const [x,y] of [[-25,-27],[26,-27],[0,0],[-25,27],[26,27]]){
    g.circle(x,y,10).fill(0xffe4ab).stroke({width:3,color:0x845333});
  }
  return g;
}
function dice(layer:Container):MotionDetails{
  const face=dieShape();
  face.position.set(576,146);
  face.rotation=.3;
  layer.addChild(face);
  return {layer,tick:({hover,time,enabled})=>{
    face.rotation=.3+hover*.16*(enabled?Math.sin(time*4.5):0);
    face.y=146-hover*7;
    face.x=576+hover*5;
  }};
}
function empire(layer:Container):MotionDetails{
  const windows:Graphics[]=[];
  for(let i=0;i<8;i++){
    const x=285+(i%4)*106,y=224+Math.floor(i/4)*65;
    const g=new Graphics().roundRect(x+2,y+2,33,40,3)
      .fill(0xffd397);
    g.alpha=0;
    layer.addChild(g);windows.push(g);
  }
  const line=new Graphics().moveTo(255,215).lineTo(692,215)
    .stroke({width:3,color:0xa8fff1,alpha:.7});
  layer.addChild(line);line.alpha=0;
  return {layer,tick:({hover,time,enabled})=>{
    windows.forEach((g,i)=>g.alpha=enabled?hover*(.08+.24*Math.max(0,Math.sin(time*2-i*.58))):0);
    line.alpha=hover*.3;
    line.y=hover*(enabled?Math.sin(time*1.2)*7:0);
  }};
}
function blackjack(layer:Container):MotionDetails{
  const ring=new Graphics().circle(0,0,81)
    .stroke({width:5,color:0xffe7a8,alpha:.7});
  ring.position.set(779,194);
  layer.addChild(ring);
  const glint=new Graphics().moveTo(-23,-25).lineTo(16,15)
    .stroke({width:9,color:0xffffff,alpha:.54});
  glint.position.set(779,194);
  layer.addChild(glint);
  return {layer,tick:({hover,time,enabled})=>{
    ring.alpha=hover*.5;
    ring.scale.set(1+hover*.045);
    glint.alpha=hover*.55;
    glint.x=779+(enabled?Math.sin(time*2)*17*hover:0);
  }};
}
function manor(layer:Container):MotionDetails{
  const windows:Graphics[]=[];
  for(const x of [116,809]){
    const g=new Graphics().roundRect(x,257,38,67,6).fill(0xffd79a);
    layer.addChild(g);windows.push(g);
  }
  const ghost=new Graphics()
    .moveTo(0,-21).bezierCurveTo(-24,-28,-28,-2,-24,17)
    .lineTo(-14,10).lineTo(-5,19).lineTo(5,10).lineTo(17,21)
    .bezierCurveTo(24,10,23,-26,0,-21).closePath()
    .fill({color:0xd8daf5,alpha:.46});
  ghost.position.set(479,210);
  layer.addChild(ghost);
  return {layer,tick:({hover,time,enabled})=>{
    windows.forEach((g,i)=>g.alpha=enabled?hover*(.18+.42*Math.abs(Math.sin(time*3.2+i*2.3))):0);
    ghost.alpha=hover*.42;
    ghost.x=479+(enabled?hover*Math.sin(time*.85)*21:0);
    ghost.y=210-hover*16;
  }};
}
function key():Graphics{
  const g=new Graphics().circle(-14,-18,19).stroke({width:12,color:gold});
  g.circle(-14,-18,7).fill(0x133a32).stroke({width:2,color:0xffe9b9});
  g.moveTo(-3,-4).lineTo(34,32).lineTo(45,21)
    .moveTo(20,18).lineTo(9,29)
    .stroke({width:13,color:gold});
  return g;
}
function merge(layer:Container):MotionDetails{
  const keys=[key(),key()];
  const origins=[{x:252,y:217},{x:421,y:217}];
  keys.forEach((g,i)=>{g.position.set(origins[i].x,origins[i].y);layer.addChild(g);});
  const item=new Graphics();
  item.moveTo(0,-63).lineTo(0,52).stroke({width:13,color:0xe6c48e});
  item.moveTo(-41,-17).lineTo(40,-17).stroke({width:9,color:gold});
  for(const x of [-41,0,40]){
    item.moveTo(x,-17).lineTo(x,-44).stroke({width:7,color:gold});
    item.moveTo(x-12,-45).lineTo(x+12,-45).stroke({width:5,color:0xffe0a1});
    item.moveTo(x,-48).lineTo(x-8,-66).lineTo(x+8,-66).closePath().fill(0xffdb8d);
  }
  item.roundRect(-36,49,72,10,4).fill(0xffd893);
  item.position.set(730,222);
  layer.addChild(item);
  const sparkle=new Graphics().moveTo(0,-18).lineTo(0,18)
    .moveTo(-18,0).lineTo(18,0)
    .stroke({width:5,color:0xffe9b9});
  sparkle.position.set(731,150);
  layer.addChild(sparkle);
  return {layer,tick:({progress,hover,time,enabled})=>{
    keys.forEach((g,i)=>{
      const p=delayed(progress,i*.12);
      g.x=origins[i].x+(i===0?30:-30)*p;
      g.y=origins[i].y-p*11;
      g.rotation=(i===0?1:-1)*p*.16;
      g.alpha=1-.56*p;
    });
    item.scale.set(1+hover*.06);
    sparkle.alpha=hover*(.55+.3*Math.sin(time*3)**2);
    sparkle.rotation=enabled?time*.31:0;
  }};
}
export function createMicroDetails(id:ModeSceneId):MotionDetails{
  const layer=new Container();
  switch(id){
    case 'blackjack':return blackjack(layer);
    case 'kniffel':return dice(layer);
    case 'chill-lounge':return steam(layer);
    case 'casino-empire':return empire(layer);
    case 'midnight-reels':return reels(layer);
    case 'neon-vault':return vault(layer);
    case 'midnight-manor':return manor(layer);
    case 'midnight-merge':return merge(layer);
    case 'nine-lives':return {layer,tick:()=>{}}; // cat head moves via image mesh
  }
}
