import {useEffect,useId,useRef,useState} from 'react';
import {createPortal} from 'react-dom';
import type {CSSProperties,KeyboardEvent} from 'react';

export type SelectOption={value:string;label:string;disabled?:boolean};
export type SelectPlacement={left:number;top:number;width:number;maxHeight:number;above:boolean};

/** Keep the popup inside the viewport without clipping it to scrolling sidebars or dialogs. */
export function placeSelect(rect:{left:number;right:number;top:number;bottom:number;width:number},
 viewport:{width:number;height:number},count:number):SelectPlacement{
 const margin=9,gap=6;
 const width=Math.min(Math.max(124,rect.width),Math.max(124,viewport.width-2*margin));
 const left=Math.max(margin,Math.min(rect.left,viewport.width-width-margin));
 const ideal=Math.min(286,Math.max(58,count*39+12));
 const below=viewport.height-rect.bottom-margin-gap;
 const above=Math.max(0,rect.top-margin-gap);
 const up=below<Math.min(ideal,170)&&above>below;
 const available=Math.max(58,up?above:below);
 const maxHeight=Math.min(ideal,available);
 return {left,top:up?Math.max(margin,rect.top-gap-maxHeight):Math.min(rect.bottom+gap,viewport.height-margin-maxHeight),
   width,maxHeight,above:up};
}
export function moveSelectIndex(current:number,direction:1|-1,options:readonly SelectOption[]):number{
 if(!options.some(o=>!o.disabled))return current;
 for(let i=1;i<=options.length;i++){
   const next=(current+direction*i+options.length*4)%options.length;
   if(!options[next].disabled)return next;
 }
 return current;
}

/** Accessible browser-rendered listbox; avoids native Windows select popup colors. */
export function PremiumSelect({value,options,disabled=false,busy=false,onChange,label,className=''}:{
 value:string;options:readonly SelectOption[];disabled?:boolean;busy?:boolean;
 onChange:(value:string)=>void;label:string;className?:string;
}){
 const [open,setOpen]=useState(false);
 const [active,setActive]=useState(0);
 const [placement,setPlacement]=useState<SelectPlacement|null>(null);
 const trigger=useRef<HTMLButtonElement>(null);
 const list=useRef<HTMLDivElement>(null);
 const id=useId();
 const selected=Math.max(0,options.findIndex(option=>option.value===value));
 const chosen=options.find(option=>option.value===value);
 const popupId='bj-menu-'+id.replace(/:/g,'');
 const optionId=(i:number)=>popupId+'-option-'+i;
 const locate=()=>{
   const rect=trigger.current?.getBoundingClientRect();
   if(rect)setPlacement(placeSelect(rect,{width:window.innerWidth,height:window.innerHeight},options.length));
 };
 const close=(returnFocus=false)=>{
   setOpen(false);
   if(returnFocus)trigger.current?.focus({preventScroll:true});
 };
 const reveal=(index=selected)=>{
   if(disabled||busy||options.length===0)return;
   const next=options[index]?.disabled?moveSelectIndex(index,1,options):index;
   setActive(next);
   locate();
   setOpen(true);
 };
 const select=(index:number)=>{
   const option=options[index];
   if(!option||option.disabled||busy)return;
   if(option.value!==value)onChange(option.value);
   close(true);
 };
 useEffect(()=>{
   if(!open)return;
   list.current?.focus({preventScroll:true});
   const outside=(e:PointerEvent)=>{
     const target=e.target;
     if(!(target instanceof Node)||(!trigger.current?.contains(target)&&!list.current?.contains(target)))close();
   };
   const refresh=()=>locate();
   document.addEventListener('pointerdown',outside,true);
   window.addEventListener('resize',refresh);
   document.addEventListener('scroll',refresh,true);
   return ()=>{
     document.removeEventListener('pointerdown',outside,true);
     window.removeEventListener('resize',refresh);
     document.removeEventListener('scroll',refresh,true);
   };
 },[open,options.length]);
 useEffect(()=>{if((disabled||busy)&&open)setOpen(false);},[disabled,busy,open]);
 const keys=(event:KeyboardEvent)=>{
   if(event.key==='Escape'){event.preventDefault();event.stopPropagation();close(true);return;}
   if(event.key==='Tab'){close();return;}
   if(event.key==='ArrowDown'||event.key==='ArrowUp'){
     event.preventDefault();event.stopPropagation();
     const direction=event.key==='ArrowDown'?1:-1;
     if(!open)reveal(moveSelectIndex(selected,direction,options));
     else setActive(current=>moveSelectIndex(current,direction,options));
     return;
   }
   if(event.key==='Home'||event.key==='End'){
     event.preventDefault();event.stopPropagation();
     const first=event.key==='Home'?0:options.length-1;
     const next=options[first]?.disabled?moveSelectIndex(first,event.key==='Home'?1:-1,options):first;
     if(!open)reveal(next);else setActive(next);
     return;
   }
   if(event.key==='Enter'||event.key===' '){
     event.preventDefault();event.stopPropagation();
     if(open)select(active);else reveal();
   }
 };
 const popupStyle:CSSProperties=placement?{
   left:placement.left,top:placement.top,width:placement.width,maxHeight:placement.maxHeight
 }:{};
 return <>
  <button type="button" ref={trigger} className={'bj-premium-select '+className}
    disabled={disabled} aria-disabled={disabled||busy} aria-label={label} aria-haspopup="listbox" aria-expanded={open}
    aria-controls={open?popupId:undefined}
    onClick={()=>{if(!busy){if(open)close();else reveal();}}} onKeyDown={keys}>
   <span className="bj-select-value">{chosen?.label??value}</span>
   <span className="bj-select-chevron" aria-hidden="true"/>
  </button>
  {open&&placement&&createPortal(<div ref={list} id={popupId} role="listbox"
     className="bj-select-popover" tabIndex={-1} aria-label={label}
     aria-activedescendant={optionId(active)} style={popupStyle} onKeyDown={keys}>
    {options.map((option,index)=><div key={option.value+'-'+index}
      id={optionId(index)} role="option" aria-selected={option.value===value}
      aria-disabled={option.disabled||undefined} data-active={index===active}
      className="bj-select-option"
      onPointerMove={()=>{if(!option.disabled)setActive(index);}}
      onClick={()=>select(index)}>
     <span>{option.label}</span>{option.value===value&&<span className="bj-select-check" aria-hidden="true">✓</span>}
    </div>)}
   </div>,document.body)}
 </>;
}
