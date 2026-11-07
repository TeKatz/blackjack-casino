import {Container, Graphics} from 'pixi.js';

/** One shared WebGL renderer handles all game-art effects. No GPU context per tile. */
export const ANIMATED_TILE_IDS=[
  'kniffel','chill-lounge','casino-empire','midnight-reels','neon-vault',
  'nine-lives','midnight-manor',
] as const;
export type AnimatedTileId=typeof ANIMATED_TILE_IDS[number];
export function isAnimatedTile(id:string):id is AnimatedTileId{
  return (ANIMATED_TILE_IDS as readonly string[]).includes(id);
}
const palettes:Record<AnimatedTileId,readonly number[]>={
  kniffel:[0xffebc0,0xd5bb7b,0xb2e5f0],
  'chill-lounge':[0xffd698,0xe8b76b,0x91eac8],
  'casino-empire':[0xffdfa4,0xcfa55e,0xa1d7c4],
  'midnight-reels':[0xffbc66,0xf7df96,0xfc737e],
  'neon-vault':[0x74f9ff,0xf66bee,0x8c8bff],
  'nine-lives':[0xe48bff,0xb879fc,0xffda8a],
  'midnight-manor':[0xffdca4,0xc8a5ff,0x98c3f5],
};
export function tileColors(id:AnimatedTileId):readonly number[]{return palettes[id];}

type Dot={node:Graphics;u:number;v:number;phase:number;speed:number};
type Accent={node:Graphics;u:number;phase:number;speed:number};
type Scene={
 art:HTMLElement;card:HTMLElement;kind:AnimatedTileId;
 group:Container;mask:Graphics;dots:Dot[];accents:Accent[];
 last:{x:number;y:number;w:number;h:number};energy:number;
};
const random=(seed:number)=>{
  let n=seed>>>0;
  return ()=>{n=(Math.imul(n,1664525)+1013904223)>>>0;return n/4294967296;};
};
function identify(card:HTMLElement):AnimatedTileId|null{
  for(const id of ANIMATED_TILE_IDS)if(card.classList.contains('game-'+id))return id;
  return null;
}
function accent(group:Container,kind:AnimatedTileId,i:number,color:number):Graphics{
  const g=new Graphics();
  if(kind==='neon-vault')g.ellipse(0,0,41+i*13,34+i*10).stroke({width:2,color,alpha:.7});
  else if(kind==='midnight-reels')g.roundRect(-7,-28,14,56,5).fill({color,alpha:.2});
  else if(kind==='nine-lives')g.ellipse(0,0,29+i*8,7+i*2).fill({color,alpha:.14});
  else if(kind==='midnight-manor')g.ellipse(0,0,11+i*5,21+i*6).fill({color,alpha:.2});
  else if(kind==='chill-lounge')g.ellipse(0,0,5+i*2,17+i*5).fill({color,alpha:.16});
  else if(kind==='casino-empire')g.roundRect(-6,-19,12,38,3).fill({color,alpha:.16});
  else g.ellipse(0,0,20+i*9,3+i).fill({color,alpha:.2});
  group.addChild(g);
  return g;
}
function createScene(stage:Container,art:HTMLElement,card:HTMLElement,kind:AnimatedTileId):Scene{
  const group=new Container();
  const mask=new Graphics();
  stage.addChild(mask);
  stage.addChild(group);
  group.mask=mask;
  const rand=random(kind.split('').reduce((n,c)=>Math.imul(n,33)+c.charCodeAt(0),721));
  const colors=palettes[kind];
  const count=kind==='neon-vault'||kind==='nine-lives'?19:15;
  const dots:Dot[]=Array.from({length:count},(_,i)=>{
    const radius=.7+rand()*1.45+(i%8===0?1.2:0);
    const node=new Graphics().circle(0,0,radius).fill({color:colors[i%colors.length]});
    group.addChild(node);
    return {node,u:rand(),v:rand(),phase:rand()*Math.PI*2,speed:.22+rand()*.68};
  });
  const number=kind==='neon-vault'?2:3;
  const accents:Accent[]=Array.from({length:number},(_,i)=>({
    node:accent(group,kind,i,colors[(i+1)%colors.length]),
    u:(i+1)/(number+1),phase:rand()*Math.PI*2,speed:.65+rand()*.7,
  }));
  return {art,card,kind,group,mask,dots,accents,last:{x:-1,y:-1,w:-1,h:-1},energy:0};
}
function removeScene(stage:Container,scene:Scene){
  scene.group.mask=null;
  stage.removeChild(scene.group,scene.mask);
  scene.group.destroy({children:true});
  scene.mask.destroy();
}
function animateAccents(s:Scene,time:number,w:number,h:number){
  const focus=Math.min(w,h);
  for(const [i,a] of s.accents.entries()){
    const wave=time*a.speed+a.phase;
    let x=a.u*w,y=.48*h;
    if(s.kind==='neon-vault'){
      x=w*.5;y=h*.5;
      a.node.scale.set(Math.min(w/220,h/120)*(1+.045*Math.sin(wave)));
      a.node.rotation=time*(i%2===0?.3:-.22);
      a.node.alpha=.34+.17*Math.sin(wave*2);
    }else if(s.kind==='midnight-reels'){
      x=w*(.37+i*.13);y=h*.5+Math.sin(wave)*h*.15;
      a.node.scale.set(focus/120);
      a.node.alpha=.12+.15*(.5+.5*Math.sin(time*3+i*1.7));
    }else if(s.kind==='nine-lives'){
      x=w*(.18+i*.31)+Math.sin(wave)*w*.04;
      y=h*(.5+i*.06)+Math.cos(wave)*h*.14;
      a.node.scale.set(focus/130);a.node.rotation=.12*Math.sin(wave);
      a.node.alpha=.08+.16*(.5+.5*Math.sin(wave));
    }else if(s.kind==='midnight-manor'){
      x=w*(.32+i*.18);y=h*(.37+i*.09)+Math.sin(wave)*h*.04;
      a.node.scale.set(focus/100);
      a.node.alpha=.14+.24*(.5+.5*Math.sin(time*5+i*2.4));
    }else if(s.kind==='chill-lounge'){
      x=w*(.37+i*.1)+Math.sin(wave)*w*.026;
      y=h*(.66-(.5+.5*Math.sin(time*.7+i))*.28);
      a.node.scale.set(focus/120);
      a.node.alpha=.11+.15*(.5+.5*Math.cos(wave));
    }else if(s.kind==='casino-empire'){
      x=w*(.27+i*.24);y=h*(.38+.09*Math.sin(wave));
      a.node.scale.set(focus/100);
      a.node.alpha=.12+.24*(.5+.5*Math.sin(time*1.9+i*3));
    }else{
      x=w*(.37+i*.26);y=h*(.48+.06*Math.sin(wave));
      a.node.scale.set(focus/120);a.node.rotation=Math.sin(wave)*.3;
      a.node.alpha=.1+.2*(.5+.5*Math.sin(wave));
    }
    a.node.position.set(x,y);
  }
}

/** Reconcile React route changes, clip each scene to its real image rect, and update at 30 FPS. */
export function createTileFx(stage:Container,root:HTMLElement){
  const scenes=new Map<HTMLElement,Scene>();
  let frame=0;
  const refresh=()=>{
    const keep=new Set<HTMLElement>();
    for(const art of root.querySelectorAll<HTMLElement>('.lobby-game .game-art-window')){
      const card=art.closest<HTMLElement>('.lobby-game');
      if(!card||!card.isConnected||card.classList.contains('unavailable'))continue;
      const kind=identify(card);
      if(!kind)continue;
      keep.add(art);
      if(!scenes.has(art))scenes.set(art,createScene(stage,art,card,kind));
    }
    for(const [art,scene] of scenes){
      if(!keep.has(art)){removeScene(stage,scene);scenes.delete(art);}
    }
  };
  refresh();
  return {
    tick(time:number,deltaTime:number){
      if(frame++%12===0)refresh();
      const rootRect=root.getBoundingClientRect();
      for(const s of scenes.values()){
        const rect=s.art.getBoundingClientRect();
        const x=rect.left-rootRect.left,y=rect.top-rootRect.top,w=rect.width,h=rect.height;
        const visible=w>=24&&h>=22&&rect.bottom>rootRect.top&&rect.top<rootRect.bottom
          &&rect.right>rootRect.left&&rect.left<rootRect.right;
        s.group.visible=visible;
        if(!visible)continue;
        const box=s.last;
        if(Math.abs(x-box.x)>.5||Math.abs(y-box.y)>.5
          ||Math.abs(w-box.w)>.5||Math.abs(h-box.h)>.5){
          s.mask.clear().rect(x,y,w,h).fill(0xffffff);
          s.group.position.set(x,y);
          box.x=x;box.y=y;box.w=w;box.h=h;
        }
        const hovered=s.card.matches(':hover')||s.card.matches(':focus-within');
        s.energy+=(Number(hovered)-s.energy)*Math.min(.17,.07*deltaTime);
        s.group.alpha=.42+s.energy*.58;
        animateAccents(s,time,w,h);
        for(const p of s.dots){
          const ascent=(time*.105*p.speed+p.v)%1;
          let px=(p.u+Math.sin(time*.53+p.phase)*.025)*w;
          let py=(1-ascent)*h;
          if(s.kind==='nine-lives'||s.kind==='midnight-manor'){
            px+=Math.sin(ascent*6.28+time*.7+p.phase)*w*.07;
            py+=Math.cos(time*.39+p.phase)*h*.035;
          }else if(s.kind==='neon-vault')px+=Math.sin(time*1.8+p.phase)*w*.04;
          else if(s.kind==='kniffel')px+=Math.sin(time*.93+p.phase)*w*.06;
          p.node.position.set(px,py);
          p.node.alpha=Math.max(.08,Math.min(.9,
            (.22+.5*Math.pow(Math.sin(time*1.2+p.phase),2))*(.7+s.energy*.5)));
          p.node.scale.set(1+s.energy*.35);
        }
      }
    },
    destroy(){for(const s of scenes.values())removeScene(stage,s);scenes.clear();}
  };
}
