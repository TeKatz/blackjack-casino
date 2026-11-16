import {Container,Graphics} from 'pixi.js';
import type {Application} from 'pixi.js';

export type HerdPhase='intro'|'background';

const formation=[
 {offset:0,size:1,depth:.81,stride:.6},
 {offset:99,size:.82,depth:.68,stride:2.1},
 {offset:171,size:.64,depth:.74,stride:4.4},
 {offset:244,size:.94,depth:.87,stride:1.2},
 {offset:326,size:.72,depth:.77,stride:3.3},
] as const;
const silhouetteWidth=108;
const herdLength=formation[formation.length-1].offset;
const brass=0xe8b76e;
const ink=0x301c28;

export function herdPosition(phase:HerdPhase,index:number,width:number,height:number,
 age:number,introDuration:number){
 const {offset,size,depth}=formation[index];
 // Use the same formation in both shots, with a slower drift on the palace grounds.
 const scale=Math.min(height*(phase==='intro'?.54:.55),width*.24)/100;
 const margin=silhouetteWidth*scale;
 const span=width+2*margin;
 const progress=Math.min(1,age/introDuration);
 const x=phase==='intro'
  ?-margin-scale*offset+progress*(span+scale*herdLength)
  :(margin+width*(.96-.90*offset/herdLength+.14*age))%span-margin;
 return {x,y:height*depth,scale:scale*size};
}

function elephant(){
 const root=new Container();
 const body=new Container();
 const shadow=new Graphics()
  .ellipse(0,1,67,8).fill({color:ink,alpha:.15})
  .ellipse(1,1,47,5).fill({color:ink,alpha:.23});
 root.addChild(shadow,body);

 const leg=(x:number,y:number,near:boolean)=>{
  const art=new Graphics().moveTo(-12,-4).quadraticCurveTo(-17,12,-10,24)
   .lineTo(-11,49).quadraticCurveTo(-9,55,4,54).lineTo(12,53)
   .quadraticCurveTo(17,49,11,45).lineTo(9,21).lineTo(12,0).closePath()
   .fill(near?0x637580:0x3d4552).stroke({color:ink,width:1.5});
  art.moveTo(5,50).lineTo(6,53).moveTo(10,49).lineTo(11,52)
   .stroke({color:0xe1af83,width:1.05,alpha:.75});
  art.position.set(x,y);
  body.addChild(art);
  return art;
 };
 const farHind=leg(-32,-54,false);
 const farFore=leg(36,-55,false);

 const tail=new Graphics().moveTo(1,0).bezierCurveTo(-14,3,-5,22,-17,30)
  .stroke({color:0x58636e,width:4,cap:'round'})
  .ellipse(-17,30,3,5).fill(ink);
 tail.position.set(-60,-72);
 body.addChild(tail);

 const torso=new Graphics().moveTo(-64,-69).bezierCurveTo(-63,-98,-30,-105,1,-96)
  .bezierCurveTo(19,-91,25,-103,47,-98).bezierCurveTo(63,-94,71,-76,62,-59)
  .bezierCurveTo(42,-42,19,-42,-3,-42).bezierCurveTo(-25,-42,-49,-43,-60,-55)
  .quadraticCurveTo(-65,-61,-64,-69).closePath()
  .fill(0x667985).stroke({color:ink,width:2});
 torso.moveTo(-61,-73).bezierCurveTo(-57,-97,-26,-102,3,-94)
  .quadraticCurveTo(24,-94,42,-97).stroke({color:0xf5ce96,width:2.6,alpha:.85,cap:'round'});
 torso.moveTo(-54,-60).quadraticCurveTo(-30,-42,-7,-45)
  .quadraticCurveTo(17,-42,45,-52).quadraticCurveTo(15,-44,-7,-55)
  .quadraticCurveTo(-28,-50,-54,-60).closePath().fill({color:0x273747,alpha:.3});
 body.addChild(torso);

 const nearHind=leg(-44,-54,true);
 const nearFore=leg(22,-54,true);
 const blanket=new Graphics().moveTo(-41,-97).quadraticCurveTo(-13,-102,16,-93)
  .lineTo(13,-62).lineTo(-13,-53).lineTo(-38,-61).closePath()
  .fill(0x842944).stroke({color:brass,width:3.1});
 blanket.moveTo(-37,-91).quadraticCurveTo(-11,-96,11,-89).lineTo(8,-66)
  .lineTo(-13,-59).lineTo(-32,-66).closePath().stroke({color:0xe7aa60,width:1.2});
 blanket.moveTo(-13,-88).lineTo(-4,-77).lineTo(-13,-66).lineTo(-22,-77)
  .closePath().fill(0xbb5161).stroke({color:brass,width:1.5});
 for(let i=0;i<5;i++)blanket.circle(-32+i*9,-59+Math.sin(i*Math.PI/4)*4,1.55).fill(brass);
 body.addChild(blanket);

 const head=new Container();
 head.position.set(46,-77);
 body.addChild(head);
 const trunk=new Graphics().moveTo(23,-3).bezierCurveTo(28,8,24,24,29,30)
  .quadraticCurveTo(34,35,42,23).quadraticCurveTo(48,22,47,29)
  .bezierCurveTo(44,43,34,49,25,45).bezierCurveTo(13,40,11,20,8,10)
  .lineTo(8,-5).closePath().fill(0x6f828d).stroke({color:ink,width:1.7});
 trunk.position.set(0,7);
 trunk.moveTo(25,16).quadraticCurveTo(21,23,28,32)
  .stroke({color:0xc3adb0,alpha:.55,width:1.5,cap:'round'});
 head.addChild(trunk);
 const face=new Graphics().moveTo(-17,-7).bezierCurveTo(-15,-31,17,-31,25,-11)
  .quadraticCurveTo(33,4,20,14).bezierCurveTo(1,28,-20,17,-17,-7)
  .closePath().fill(0x768991).stroke({color:ink,width:1.5});
 face.moveTo(6,12).quadraticCurveTo(19,19,39,8).quadraticCurveTo(26,25,11,21)
  .quadraticCurveTo(5,18,6,12).closePath().fill(0xffe0a7);
 face.moveTo(-3,-21).quadraticCurveTo(12,-27,23,-14).lineTo(25,-8)
  .quadraticCurveTo(10,-17,-4,-12).closePath().fill(brass);
 face.moveTo(13,-23).lineTo(20,-16).lineTo(15,-11).lineTo(9,-17).closePath()
  .fill(0x9c2547).stroke({color:0xffdf9f,width:1.4});
 face.circle(16,-1,2.65).fill(ink).circle(16.6,-1.8,.85).fill(0xffe5b1);
 head.addChild(face);

 const ear=new Graphics().moveTo(4,-11).bezierCurveTo(-5,-21,-24,-16,-26,-6)
  .bezierCurveTo(-28,7,-21,20,-14,25).bezierCurveTo(-7,28,1,11,4,-11)
  .closePath().fill(0x586c7b).stroke({color:ink,width:1.5});
 ear.moveTo(-1,-9).bezierCurveTo(-10,-15,-22,-11,-22,-3)
  .quadraticCurveTo(-20,14,-13,18).quadraticCurveTo(-5,18,-1,-9).closePath()
  .fill(0xa37c80);
 head.addChild(ear);
 return {root,body,head,ear,trunk,tail,shadow,legs:[farHind,farFore,nearHind,nearFore]};
}

export function createRubyHerd(app:Application){
 const animals=formation.map(member=>({...elephant(),...member}));
 const dust=formation.map(()=>Array.from({length:6},(_,i)=>new Graphics()
  .ellipse(0,0,3+i%3,1.5+i%2).fill(i%2===0?0xfad48f:0xc77b48)));
 app.stage.eventMode='none';
 app.stage.addChild(...dust.flat());
 [...animals].sort((a,b)=>a.depth-b.depth)
  .forEach(animal=>app.stage.addChild(animal.root));

 return (phase:HerdPhase,age:number,introDuration:number)=>{
  const {width,height}=app.screen;
  const background=phase==='background';
  animals.forEach(({root,body,head,ear,trunk,tail,shadow,legs,stride},index)=>{
   const {x,y,scale}=herdPosition(phase,index,width,height,age,introDuration);
   const step=age*(background?8:11)+stride;
   const opacity=background?Math.min(1,age/.22)*.78:.96;
   root.position.set(x,y);
   root.scale.set(scale);
   root.alpha=opacity;
   root.tint=background?0xbd947c:0xffffff;
   root.visible=x>-silhouetteWidth*scale&&x<width+silhouetteWidth*scale;
   body.y=-Math.abs(Math.sin(step))*4;
   head.rotation=Math.sin(step)*.035;
   ear.rotation=Math.cos(step)*.105;
   trunk.rotation=Math.sin(step-.35)*.15;
   tail.rotation=Math.sin(step-.65)*.23;
   shadow.scale.x=.97+.06*Math.sin(step*2);
   legs.forEach((leg,i)=>{
    const cycle=step+(i===0||i===3?0:Math.PI);
    leg.rotation=Math.sin(cycle)*(background?.24:.38);
    leg.y=(i===1?-55:-54)-Math.max(0,Math.cos(cycle))*5;
   });
   dust[index].forEach((grain,i)=>{
    const drift=(age*(background?1.4:2.7)+i/6+index*.23)%1;
    grain.visible=root.visible;
    grain.position.set(x+(-31-drift*81+(i%3)*19)*scale,y+(3-drift*(13+i*2))*scale);
    grain.scale.set(scale*(.8+drift*1.7));
    grain.alpha=opacity*(background?.47:.72)*Math.sin(Math.PI*drift);
   });
  });
 };
}
