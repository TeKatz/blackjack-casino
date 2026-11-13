import {Assets, Container, Graphics, MeshPlane, Sprite, type Texture} from 'pixi.js';
import {createMicroDetails, type MotionDetails} from './LobbyMicroMotion';

/**
 * The nine lobby key-art windows share ONE PixiJS stage and GPU context.
 * Every scene is textured from original local art, never CSS/DOM illustrations.
 * The C# host and React retain navigation and accessible text/controls.
 */
export const MODE_SCENE_IDS = [
  'blackjack','kniffel','chill-lounge','casino-empire','midnight-reels',
  'neon-vault','nine-lives','midnight-manor','midnight-merge',
] as const;
export type ModeSceneId = typeof MODE_SCENE_IDS[number];

export type ModeSceneAssets = {back: string; front: string | null};
export function modeSceneAssets(id: ModeSceneId): ModeSceneAssets {
  if (id === 'nine-lives') {
    // Preserve the much-loved existing cat painting, not a replacement.
    return {back:'/lobby/art/nine-lives.jpg',front:null};
  }
  return {
    back: '/lobby/custom/' + id + '-back.svg',
    front: '/lobby/custom/' + id + '-front.svg',
  };
}
export const MODE_PALETTES: Record<ModeSceneId,readonly number[]> = {
  blackjack:[0xffdfa0,0x9cdebb,0xeebd7a],
  kniffel:[0xffd39a,0xff8b8f,0xf8ebc2],
  'chill-lounge':[0xf7dba4,0xecc0a0,0xb3d5c5],
  'casino-empire':[0xffd38a,0x92f2e4,0xe6be70],
  'midnight-reels':[0xffd47f,0xff8176,0xfef0bb],
  'neon-vault':[0x74f5ff,0xf77eff,0x97caff],
  'nine-lives':[0xd989ff,0xffd38d,0xb894ff],
  'midnight-manor':[0xc4a1fa,0xf7ca91,0xbcd0ff],
  'midnight-merge':[0xffd39a,0xe9a779,0xfff2bb],
};

export function coverSize(width:number,height:number,artWidth=960,artHeight=440):number {
  return Math.max(width/artWidth,height/artHeight);
}
export function rectVisible(rect:DOMRect,root:DOMRect):boolean {
  return rect.width>=24 && rect.height>=22 &&
    rect.bottom>root.top && rect.top<root.bottom &&
    rect.right>root.left && rect.left<root.right;
}
const hash=(name:string)=>Array.from(name).reduce((h,c)=>Math.imul(h,31)+c.charCodeAt(0)|0,1741)>>>0;
function seeded(seed:number){
  let s=seed>>>0;
  return ()=>{s=(Math.imul(s,1664525)+1013904223)>>>0;return s/4294967296;};
}
function kindFromCard(card:HTMLElement):ModeSceneId|null {
  for (const id of MODE_SCENE_IDS) if (card.classList.contains('game-'+id)) return id;
  return null;
}
type Ember = {node:Graphics;u:number;v:number;phase:number;rate:number};
type Scene = {
  id:ModeSceneId;art:HTMLElement;card:HTMLElement;
  group:Container;mask:Graphics;back:Sprite|MeshPlane|null;front:Sprite|null;
  cat:MeshPlane|null;catVertices:Float32Array|null;catWarped:boolean;
  detail:MotionDetails;signature:Graphics;em:Ember[];
  x:number;y:number;w:number;h:number;energy:number;reveal:number;hoverProgress:number;
};

function makeSignature(id:ModeSceneId,color:number):Graphics {
  const g=new Graphics();
  if(id==='neon-vault'){
    g.circle(0,0,42).stroke({color,width:2,alpha:.66});
    g.circle(0,0,29).stroke({color:MODE_PALETTES[id][1],width:1.5,alpha:.6});
  }else if(id==='midnight-reels'){
    for(const x of [-25,0,25]) g.roundRect(x-4,-22,8,44,3).fill({color,alpha:.2});
  }else if(id==='nine-lives'){
    g.ellipse(-21,0,15,7).fill({color,width:1,alpha:.18});
    g.ellipse(21,0,15,7).fill({color,width:1,alpha:.18});
  }else if(id==='midnight-manor'){
    g.ellipse(0,0,12,27).fill({color,alpha:.2});
    g.ellipse(0,0,4,10).fill({color:0xffefc5,alpha:.38});
  }else if(id==='chill-lounge'){
    for(const x of [-12,0,12]) g.ellipse(x,0,4,20).fill({color,alpha:.2});
  }else if(id==='casino-empire'){
    for(let i=0;i<5;i++) g.roundRect(i*12-28,-14,6,28,1).fill({color,alpha:.22});
  }else if(id==='kniffel'){
    for(const x of [-29,0,29]) g.circle(x,0,9).stroke({color,width:2,alpha:.3});
  }else if(id==='midnight-merge'){
    g.circle(-24,0,9).stroke({color,width:2,alpha:.5});
    g.circle(24,0,9).stroke({color,width:2,alpha:.5});
    g.moveTo(-10,0).lineTo(10,0).stroke({color,width:3,alpha:.6});
  }else{
    g.moveTo(-27,-19).lineTo(27,19).stroke({color,width:4,alpha:.26});
    g.moveTo(-23,-19).lineTo(31,19).stroke({color:0xfff3d1,width:1,alpha:.5});
  }
  return g;
}
function createScene(stage:Container,art:HTMLElement,card:HTMLElement,id:ModeSceneId):Scene {
  const group=new Container(),mask=new Graphics();
  stage.addChild(mask,group);
  group.mask=mask;
  // No generic oval light patches: all lighting is attached to its object.
  const signature=makeSignature(id,MODE_PALETTES[id][0]);
  const detail=createMicroDetails(id);
  const random=seeded(hash(id));
  const em:Ember[]=Array.from({length:id==='nine-lives'||id==='neon-vault'?12:8},(_,i)=>{
    const size=.8+random()*1.35;
    const node=new Graphics().circle(0,0,size).fill({color:MODE_PALETTES[id][i%3]});
    node.alpha=0;
    group.addChild(node);
    return {node,u:random(),v:random(),phase:random()*Math.PI*2,rate:.3+random()*.6};
  });
  group.addChild(signature,detail.layer);
  return {id,art,card,group,mask,back:null,front:null,cat:null,catVertices:null,catWarped:false,
    // Recreated views reuse decoded textures without fading in from blank.
    detail,signature,em,x:NaN,y:NaN,w:0,h:0,
    energy:card.matches(':hover')||card.matches(':focus-within')?1:0,
    reveal:1,hoverProgress:card.matches(':hover')?1:0};
}
function disposeScene(stage:Container,s:Scene){
  delete s.art.dataset.pixiArtReady;
  s.group.mask=null;
  stage.removeChild(s.mask,s.group);
  s.group.destroy({children:true});
  s.mask.destroy();
}
function attachArt(s:Scene,resources:Map<ModeSceneId,{back:Texture;front:Texture|null}>){
  if(s.back)return;
  const data=resources.get(s.id);
  if(!data)return;
  if(s.id==='nine-lives'){
    // Warp only the center of the existing cat photo. The outer vertices
    // remain stationary: no hard masked cutout or duplicated cat head.
    const plane=new MeshPlane({texture:data.back,verticesX:11,verticesY:9});
    s.catVertices=Float32Array.from(plane.geometry.getAttribute('aPosition').buffer.data);
    plane.alpha=0;
    s.group.addChildAt(plane,0);
    s.back=plane;
    s.cat=plane;
  }else{
    const back=new Sprite(data.back);
    back.anchor.set(.5);
    back.alpha=0;
    s.group.addChildAt(back,0);
    s.back=back;
  }
  // Keep CSS fallback until the first fully positioned GPU render frame.
  if(data.front){
    const front=new Sprite(data.front);
    front.anchor.set(.5);
    front.alpha=0;
    s.group.addChildAt(front,1);
    s.front=front;
  }
}
function placeArt(s:Scene,time:number,motion:boolean){
  const w=s.w,h=s.h;
  const hovered=motion&&(s.card.matches(':hover')||s.card.matches(':focus-within'));
  s.energy+=(Number(hovered)-s.energy)*(motion ? .14 : 1);
  const dx=motion?Number.parseFloat(s.card.style.getPropertyValue('--motion-x'))||0:0;
  const dy=motion?Number.parseFloat(s.card.style.getPropertyValue('--motion-y'))||0:0;
  const pulse=motion?Math.sin(time*.62+hash(s.id)%11):0;
  // Fade in decoded textures without exposing an empty browser rectangle.
  // Reduced-motion mode skips the transition entirely.
  s.reveal=motion?Math.min(1,s.reveal+.12):1;
  if(s.back){
    // Nine Lives has decorative orange borders in the original photo.
    // Crop those in BOTH idle and hover states, without replacing the image.
    const crop=s.id==='nine-lives'?1.145:1.025;
    const hoverZoom=s.id==='nine-lives'?.012:.04;
    const scale=coverSize(w,h,s.back.texture.width,s.back.texture.height)
      *(crop+s.energy*hoverZoom);
    s.back.alpha=s.reveal;
    s.back.scale.set(scale);
    if(s.cat){
      s.back.position.set(w*.5-s.back.texture.width*scale*.5-dx*.35,
        h*.5-s.back.texture.height*scale*.5-dy*.3);
      const original=s.catVertices!;
      const vertexBuffer=s.cat.geometry.getAttribute('aPosition').buffer;
      const coords=vertexBuffer.data;
      const tw=s.cat.texture.width,th=s.cat.texture.height;
      const turn=motion?s.energy:0;
      if(turn>.001 || s.catWarped){
        for(let i=0;i<original.length;i+=2){
          const u=original[i]/tw,v=original[i+1]/th;
          const ellipse=((u-.5)/.32)**2+((v-.47)/.44)**2;
          const influence=Math.exp(-2*ellipse)*turn;
          coords[i]=original[i]+influence*tw*.025*(.8+.25*v);
          coords[i+1]=original[i+1]+influence*th*.011*(u-.48);
        }
        vertexBuffer.update();
        s.catWarped=turn>.001;
      }
    }else{
      s.back.position.set(w*.5-dx*.35,h*.5-dy*.3);
    }
  }
  if(s.front){
    s.front.alpha=s.reveal;
    s.front.position.set(w*.5+dx*.58,h*.5+dy*.44-pulse*.7);
    s.front.scale.set(coverSize(w,h,s.front.texture.width,s.front.texture.height)
      *(1.025+s.energy*.055));
  }
  // The SVG foreground and Pixi micro-objects share EXACTLY one transform.
  // Using the base cover scale here made the lit windows, dice and merge
  // objects drift off the painted positions whenever the card was hovered.
  // Only the corrected tiles need a foreground-locked overlay. Preserve
  // the already-approved Blackjack, Lounge, Reels and Vault motion exactly.
  const foregroundLocked=s.id==='kniffel'||s.id==='casino-empire'
    ||s.id==='midnight-manor'||s.id==='midnight-merge';
  const artScale=foregroundLocked
    ? (s.front?.scale.x ?? coverSize(w,h)) : coverSize(w,h);
  const artCenterX=foregroundLocked
    ? (s.front?.position.x ?? w*.5) : w*.5+dx*.15;
  const artCenterY=foregroundLocked
    ? (s.front?.position.y ?? h*.5) : h*.5+dy*.15;
  s.detail.layer.scale.set(artScale);
  s.detail.layer.position.set(artCenterX-480*artScale,
    artCenterY-220*artScale);
  s.hoverProgress=motion?Math.max(0,Math.min(1,s.hoverProgress+(hovered?.075:-.105))):0;
  s.detail.tick({hover:s.energy,progress:s.hoverProgress,time,enabled:motion});
  s.signature.position.set(s.id==='neon-vault'?w*.53:s.id==='nine-lives'?w*.5:w*.61,h*.48);
  s.signature.scale.set(Math.min(w/230,h/112)*(1+s.energy*.08));
  s.signature.rotation=motion&&s.id==='neon-vault'?time*.19*s.energy:0;
  s.signature.alpha=motion ? s.energy*.28 : 0;
  for(const p of s.em){
    p.node.visible=motion && s.energy>.015;
    if(!motion || s.energy<=.015)continue;
    const v=(p.v+time*.045*p.rate)%1;
    p.node.position.set((p.u+.02*Math.sin(time*.65+p.phase))*w,
      (1-v)*h);
    p.node.alpha=(.18+.4*Math.pow(Math.sin(time*.8+p.phase),2))*(.55+.45*s.energy);
    p.node.scale.set(1+s.energy*.45);
  }
}

/** One clipped PixiJS scene per visible tile, one WebGL renderer total. */
export function createModeScenes(stage:Container,root:HTMLElement,motion:()=>boolean){
  const scenes=new Map<HTMLElement,Scene>();
  const resources=new Map<ModeSceneId,{back:Texture;front:Texture|null}>();
  let disposed=false,frame=0;
  const reconcile=()=>{
    if(disposed)return;
    const present=new Set<HTMLElement>();
    for(const art of root.querySelectorAll<HTMLElement>('.lobby-game .game-art-window')){
      const card=art.closest<HTMLElement>('.lobby-game');
      if(!card||!card.isConnected||card.classList.contains('unavailable'))continue;
      const id=kindFromCard(card);
      if(!id)continue;
      present.add(art);
      if(!scenes.has(art))scenes.set(art,createScene(stage,art,card,id));
      attachArt(scenes.get(art)!,resources);
    }
    for(const [art,s] of scenes){
      if(!present.has(art)){disposeScene(stage,s);scenes.delete(art);}
    }
  };
  const ready=Promise.all(MODE_SCENE_IDS.map(async id=>{
    const asset=modeSceneAssets(id);
    try{
      const [back,front]=await Promise.all([
        Assets.load<Texture>(asset.back),
        asset.front?Assets.load<Texture>(asset.front):Promise.resolve(null),
      ]);
      if(!disposed){
        resources.set(id,{back,front});
        reconcile();
      }
    }catch(error){
      // A single corrupt optional asset must not crash the lobby or block navigation.
      console.warn('Lobby key-art unavailable:',id,error);
    }
  }));
  reconcile();
  return {
    ready,
    tick(time:number){
      if(disposed)return;
      if(frame++%12===0)reconcile();
      const rootRect=root.getBoundingClientRect();
      for(const s of scenes.values()){
        const rect=s.art.getBoundingClientRect();
        const visible=rectVisible(rect,rootRect);
        s.group.visible=visible;
        if(!visible)continue;
        const x=rect.left-rootRect.left,y=rect.top-rootRect.top,w=rect.width,h=rect.height;
        if(Math.abs(x-s.x)>.5||Math.abs(y-s.y)>.5||Math.abs(w-s.w)>.5||Math.abs(h-s.h)>.5){
          s.mask.clear().rect(x,y,w,h).fill(0xffffff);
          s.group.position.set(x,y);
          s.x=x;s.y=y;s.w=w;s.h=h;
        }
        placeArt(s,time,motion());
        // Only switch off the CSS fallback when actual sprites have been
        // placed at the final card bounds, avoiding a double-crop morph.
        if (s.back && s.art.dataset.pixiArtReady !== 'true')
          s.art.dataset.pixiArtReady='true';
      }
    },
    destroy(){
      disposed=true;
      for(const s of scenes.values())disposeScene(stage,s);
      scenes.clear();
    },
  };
}
