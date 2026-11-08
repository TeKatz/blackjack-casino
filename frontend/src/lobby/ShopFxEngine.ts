import {Container,Graphics} from 'pixi.js';
import {shopFxCount} from './ShopVisualModel';
import type {ShopKind} from './shopBridge';
import type {VisualTier} from './ShopVisualModel';

/**
 * GPU sparkles are tied to cosmetic objects, not the card's full surface.
 * Reuses the single lobby PixiJS WebGL context. Only visible products are active.
 */
type Spark={node:Graphics;phase:number;orbit:number;radius:number;speed:number};
type Scene={
 root:HTMLElement;object:HTMLElement;container:Container;mask:Graphics;
 kind:ShopKind;quality:VisualTier;sparks:Spark[];last:{x:number;y:number;w:number;h:number};
};
const materials:Record<string,number>={
 heritage:0xeecf8f,deco:0xf3c885,velvet:0xf3aab2,jade:0x8af0ba,
 ocean:0x82defc,celestial:0xd9b9ff,solar:0xffd57b,obsidian:0x80d6e6,
 crystal:0xc8f5ff,porcelain:0xffefcf,metal:0xebcc91,prism:0xf2b9ff,
 forest:0x9dedb7,neon:0x80fff4,royal:0xe2c18f,ember:0xff9778,
 aurora:0xa5baff,moonlit:0xc8a3ff,
};
function hash(seed:string){let n=2166136261;for(const c of seed){n=Math.imul(n^c.charCodeAt(0),16777619);}return n>>>0;}
function seeded(seed:number){let n=seed>>>0;return()=>{n=(Math.imul(n,1664525)+1013904223)>>>0;return n/4294967296;};}
function scene(stage:Container,root:HTMLElement):Scene|null{
 const object=root.querySelector<HTMLElement>('.showcase-object');
 if(!object)return null;
 const kind=root.dataset.shopKind as ShopKind;
 if(!['Frame','CardSet','DiceSet','Portrait','Title'].includes(kind))return null;
 const quality=(root.dataset.quality??'classic') as VisualTier;
 const color=materials[root.dataset.material??'heritage']??0xf7d495;
 const group=new Container(),mask=new Graphics();
 stage.addChild(mask);stage.addChild(group);group.mask=mask;
 const random=seeded(hash(root.dataset.shopFx??kind));
 const sparks=Array.from({length:shopFxCount(kind,quality)},(_,i)=>{
   const radius=(i%4===0?1.8:.85)+random()*1.1;
   const node=new Graphics().circle(0,0,radius).fill({color:i%5===0?0xffffff:color,alpha:.72});
   if(i%6===0)node.scale.set(1.2);
   group.addChild(node);
   return {node,phase:random()*Math.PI*2,orbit:random(),radius,speed:.38+random()*.85};
 });
 return {root,object,container:group,mask,kind,quality,sparks,last:{x:-1,y:-1,w:-1,h:-1}};
}
function destroy(stage:Container,entry:Scene){
 entry.container.mask=null;stage.removeChild(entry.container,entry.mask);
 entry.container.destroy({children:true});entry.mask.destroy();
}
function visibleInViewport(el:HTMLElement,viewport:DOMRect):boolean{
 const r=el.getBoundingClientRect();
 return r.width>=30&&r.height>=30&&r.top<viewport.bottom&&r.bottom>viewport.top
   &&r.left<viewport.right&&r.right>viewport.left;
}
export function createShopFx(stage:Container,lobbyRoot:HTMLElement){
 const scenes=new Map<HTMLElement,Scene>();
 let count=0;
 const reconcile=()=>{
  const scroll=lobbyRoot.querySelector<HTMLElement>('.shop-scroll');
  const dialog=lobbyRoot.querySelector('.shop-confirm-overlay');
  const active=new Set<HTMLElement>();
  if(scroll&&!dialog){
   const viewport=scroll.getBoundingClientRect();
   // Strict cap prevents large catalogs creating hundreds of GPU objects.
   for(const element of scroll.querySelectorAll<HTMLElement>('.shop-product .premium-showcase')){
    if(active.size>=18)break;
    if(!visibleInViewport(element,viewport))continue;
    active.add(element);
    if(!scenes.has(element)){
      const newScene=scene(stage,element);
      if(newScene)scenes.set(element,newScene);
    }
   }
  }
  for(const [el,s] of scenes)if(!active.has(el)){
    destroy(stage,s);scenes.delete(el);
  }
 };
 reconcile();
 return {
  tick(time:number,delta:number){
   if(count++%10===0)reconcile();
   const root=lobbyRoot.getBoundingClientRect();
   for(const s of scenes.values()){
    const r=s.object.getBoundingClientRect();
    const x=r.left-root.left,y=r.top-root.top,w=r.width,h=r.height;
    const v=s.last;
    if(Math.abs(v.x-x)>.5||Math.abs(v.y-y)>.5||Math.abs(v.w-w)>.5||Math.abs(v.h-h)>.5){
      s.mask.clear().roundRect(x,y,w,h,Math.min(24,w*.1)).fill(0xffffff);
      s.container.position.set(x,y);v.x=x;v.y=y;v.w=w;v.h=h;
    }
    const hovered=s.root.closest('.shop-product')?.matches(':hover,:focus-within')??false;
    const energy=hovered?1.18:.65;
    for(let i=0;i<s.sparks.length;i++){
      const p=s.sparks[i],t=time*p.speed+p.phase;
      let px=0,py=0;
      if(s.kind==='Frame'){
        const a=p.orbit*Math.PI*2+t*.16;
        px=w/2+Math.cos(a)*w*.39;py=h/2+Math.sin(a)*h*.37;
      }else if(s.kind==='Portrait'){
        const a=p.orbit*Math.PI*2+t*.14;
        px=w/2+Math.cos(a)*w*.34;py=h/2+Math.sin(a)*h*.34;
      }else if(s.kind==='CardSet'){
        px=w*(.22+p.orbit*.54)+Math.sin(t)*w*.025;
        py=h*(.16+(1-(time*.09*p.speed+p.orbit)%1)*.70);
      }else if(s.kind==='DiceSet'){
        px=w*(.22+p.orbit*.58)+Math.sin(t)*w*.045;
        py=h*(.23+.48*(.5+.5*Math.cos(t*1.2+i)));
      }else{
        px=w*(.1+p.orbit*.8);py=h*(.25+.5*(.5+.5*Math.sin(t)));
      }
      p.node.position.set(px,py);
      p.node.alpha=Math.max(.07,Math.min(.88,(.17+.54*Math.pow(Math.sin(t*1.35),2))*energy));
      p.node.scale.set((1+(hovered?.35:0))*(s.quality==='showpiece'?1.2:1));
    }
   }
  },
  destroy(){for(const s of scenes.values())destroy(stage,s);scenes.clear();}
 };
}
