import {Container,Graphics} from 'pixi.js';

/** Soft, clipped GPU ambience attached to the existing shared lobby canvas. */
export function createLobbyHeroFx(stage:Container,root:HTMLElement) {
  const group=new Container();
  const mask=new Graphics();
  stage.addChild(mask,group);
  group.mask=mask;
  const dust=Array.from({length:14},(_,index)=>{
    const dot=new Graphics().circle(0,0,.7+(index%3)*.38)
      .fill({color:index%4===0?0xd4ffe2:0xf5d5a0});
    group.addChild(dot);
    return {node:dot,phase:index*2.399,u:((index*37)%101)/101,v:((index*53)%97)/97};
  });
  let lastX=NaN,lastY=NaN,lastW=NaN,lastH=NaN;
  return {
    tick(time:number,enabled:boolean) {
      const hero=root.querySelector<HTMLElement>('.lobby-hero');
      if(!hero||!enabled){group.visible=false;return;}
      const rect=hero.getBoundingClientRect();
      const base=root.getBoundingClientRect();
      const x=rect.left-base.left,y=rect.top-base.top,w=rect.width,h=rect.height;
      group.visible=w>70&&h>50&&rect.bottom>base.top&&rect.top<base.bottom;
      if(!group.visible)return;
      if(Math.abs(x-lastX)>.5||Math.abs(y-lastY)>.5||Math.abs(w-lastW)>.5||Math.abs(h-lastH)>.5){
        mask.clear().roundRect(x,y,w,h,12).fill(0xffffff);
        group.position.set(x,y);
        lastX=x;lastY=y;lastW=w;lastH=h;
      }
      for(const d of dust){
        // Keep floating motes on the illuminated table side, not over headings.
        const drift=(d.v+time*.022*(.7+d.u))%1;
        d.node.position.set(w*(.49+d.u*.48)+Math.sin(time*.61+d.phase)*5,
          h*(1-drift));
        d.node.alpha=(.09+.13*(.5+.5*Math.sin(time*.9+d.phase)))*
          Math.min(1,Math.max(0,(w-400)/260));
      }
    },
    destroy() {
      group.mask=null;
      stage.removeChild(group,mask);
      group.destroy({children:true});
      mask.destroy();
    },
  };
}
