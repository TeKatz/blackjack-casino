import {useEffect,useRef} from 'react';

/** A small icosahedron renderer rather than a rotating flat hexagon. All faces
 * have real 3D normals, coherent bevels, depth shading and numbered facets.
 * The final orientation comes only from the C#-committed check result. */
type V3=readonly [number,number,number];
const phi=(1+Math.sqrt(5))/2;
const vertices:V3[]=[
 [-1,phi,0],[1,phi,0],[-1,-phi,0],[1,-phi,0],
 [0,-1,phi],[0,1,phi],[0,-1,-phi],[0,1,-phi],
 [phi,0,-1],[phi,0,1],[-phi,0,-1],[-phi,0,1],
].map(v=>{const l=Math.hypot(...v);return [v[0]/l,v[1]/l,v[2]/l] as V3;});
const indices:number[][]=[
 [0,11,5],[0,5,1],[0,1,7],[0,7,10],[0,10,11],
 [1,5,9],[5,11,4],[11,10,2],[10,7,6],[7,1,8],
 [3,9,4],[3,4,2],[3,2,6],[3,6,8],[3,8,9],
 [4,9,5],[2,4,11],[6,2,10],[8,6,7],[9,8,1],
];
const sub=(a:V3,b:V3):V3=>[a[0]-b[0],a[1]-b[1],a[2]-b[2]];
const cross=(a:V3,b:V3):V3=>[
 a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0],
];
const normal=(v:V3):V3=>{
 const m=Math.hypot(...v)||1;return [v[0]/m,v[1]/m,v[2]/m];
};
const faces=indices.map((ids,i)=>{
 const [a,b,c]=ids.map(n=>vertices[n]);
 const centroid:V3=[(a[0]+b[0]+c[0])/3,
  (a[1]+b[1]+c[1])/3,(a[2]+b[2]+c[2])/3];
 let n=normal(cross(sub(b,a),sub(c,a)));
 if(n[0]*centroid[0]+n[1]*centroid[1]+n[2]*centroid[2]<0)
  n=[-n[0],-n[1],-n[2]];
 return {ids,number:i+1,normal:n};
});
const rotate=(v:V3,pitch:number,yaw:number,roll:number):V3=>{
 const cy=Math.cos(yaw),sy=Math.sin(yaw),
 cx=Math.cos(pitch),sx=Math.sin(pitch),
 cz=Math.cos(roll),sz=Math.sin(roll);
 const x=v[0]*cy+v[2]*sy, z=-v[0]*sy+v[2]*cy;
 const y=v[1]*cx-z*sx,zz=v[1]*sx+z*cx;
 return [x*cz-y*sz,x*sz+y*cz,zz];
};
const poseFor=(value:number)=>{
 const face=faces[((Math.round(value)||1)-1+20)%20];
 const [nx,ny,nz]=face.normal;
 return {yaw:Math.atan2(-nx,nz),pitch:Math.atan2(ny,Math.hypot(nx,nz))};
};

export function drawD20(canvas:HTMLCanvasElement,value:number,pitch:number,yaw:number,roll:number){
 const ctx=canvas.getContext('2d');if(!ctx)return;
 const scale=Math.min(2,window.devicePixelRatio||1);
 const w=174,h=172;
 const px=Math.round(w*scale),py=Math.round(h*scale);
 if(canvas.width!==px||canvas.height!==py){canvas.width=px;canvas.height=py;}
 ctx.setTransform(scale,0,0,scale,0,0);ctx.clearRect(0,0,w,h);
 const shadow=ctx.createRadialGradient(87,144,2,87,144,61);
 shadow.addColorStop(0,'#040e15ad');shadow.addColorStop(1,'#040e1500');
 ctx.fillStyle=shadow;ctx.beginPath();ctx.ellipse(87,144,69,19,0,0,Math.PI*2);ctx.fill();
 const projected=vertices.map(v=>{
  const r=rotate(v,pitch,yaw,roll), depth=3.15/(3.15-r[2]*.52);
  return {x:87+r[0]*68*depth,y:78-r[1]*68*depth,z:r[2]};
 });
 const ordered=faces.map(face=>{
  const n=rotate(face.normal,pitch,yaw,roll);
  const centroid=face.ids.reduce((a,i)=>a+projected[i].z,0)/3;
  return {...face,n,centroid};
 }).sort((a,b)=>a.centroid-b.centroid);
 for(const face of ordered){
  if(face.n[2]<-.02)continue;
  const [a,b,c]=face.ids.map(i=>projected[i]);
  const brightness=Math.max(0,face.n[0]*-.43+face.n[1]*.39+face.n[2]*.79);
  const fill=ctx.createLinearGradient(a.x,a.y,c.x,c.y);
  const energy=brightness*.52;
  fill.addColorStop(0,`rgb(${Math.round(39+energy*90)},${Math.round(60+energy*86)},${Math.round(67+energy*68)})`);
  fill.addColorStop(1,`rgb(${Math.round(16+energy*63)},${Math.round(29+energy*65)},${Math.round(42+energy*60)})`);
  ctx.beginPath();ctx.moveTo(a.x,a.y);ctx.lineTo(b.x,b.y);ctx.lineTo(c.x,c.y);ctx.closePath();
  ctx.fillStyle=fill;ctx.fill();
  ctx.strokeStyle=face.n[2]>.15?'#ddc89c':'#6d796e';
  ctx.lineWidth=face.n[2]>.5?1.9:1.35;ctx.stroke();
  if(face.n[2]>.2){
   const x=(a.x+b.x+c.x)/3,y=(a.y+b.y+c.y)/3;
   ctx.save();ctx.translate(x,y);
   const textSize=face.n[2]>.78?20:14;
   ctx.font=`700 ${textSize}px "Segoe UI",sans-serif`;
   ctx.textAlign='center';ctx.textBaseline='middle';
   ctx.shadowColor='#050b0e';ctx.shadowBlur=4;ctx.shadowOffsetY=2;
   ctx.fillStyle=face.n[2]>.72?'#fff0cc':'#e0d7b9';
   ctx.fillText(String(face.number),0,0);
   ctx.restore();
  }
 }
 // A restrained edge light is part of the die, not a full-screen flash.
 ctx.strokeStyle='#e9d2a5';ctx.lineWidth=.8;ctx.globalAlpha=.2;
 ctx.beginPath();ctx.ellipse(87,78,68,68,0,Math.PI*1.05,Math.PI*1.8);ctx.stroke();
 ctx.globalAlpha=1;
}

export function BackroomD20({value,rolling,resolved,motion}:{
 value:number;rolling:boolean;resolved:boolean;motion:boolean;
}){
 const ref=useRef<HTMLCanvasElement>(null);
 const valueRef=useRef(value),resolvedRef=useRef(resolved);
 valueRef.current=value;resolvedRef.current=resolved;
 useEffect(()=>{
  const canvas=ref.current;if(!canvas)return;
  let frame=0,settleAt:number|null=null;
  const start=performance.now();
  const paint=(now:number)=>{
   const target=poseFor(valueRef.current);
   if(rolling&&motion){
    const elapsed=(now-start)/1000;
    if(resolvedRef.current&&settleAt===null)settleAt=now;
    let pitch,yaw,roll;
    if(settleAt===null){
     pitch=target.pitch+elapsed*8.7;
     yaw=target.yaw+elapsed*11.8;
     roll=Math.sin(elapsed*3.3)*.24;
    }else{
     const t=Math.min(1,(now-settleAt)/500);
     const settle=(1-t)*(1-t)*(1-t);
     pitch=target.pitch+settle*(Math.PI*2.8);
     yaw=target.yaw+settle*(Math.PI*3.7);
     roll=settle*.18;
    }
    drawD20(canvas,valueRef.current,pitch,yaw,roll);
    frame=requestAnimationFrame(paint);
   }else{
    drawD20(canvas,valueRef.current,target.pitch,target.yaw,0);
   }
  };
  frame=requestAnimationFrame(paint);
  return()=>cancelAnimationFrame(frame);
 },[rolling,motion]);
 useEffect(()=>{
  if(rolling)return;
  const canvas=ref.current;
  if(canvas){const pose=poseFor(value);drawD20(canvas,value,pose.pitch,pose.yaw,0);}
 },[value,rolling]);
 return <div className={'backroom-d20-stage'+(rolling?' is-rolling':'')}>
  <canvas ref={ref} width="174" height="172" aria-hidden="true"/>
  <span className="backroom-d20-shadow" aria-hidden="true"/>
 </div>;
}
