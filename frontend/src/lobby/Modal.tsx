import {useEffect,useRef} from 'react';
import type {ReactNode} from 'react';
/** Native top layer isolates pointer, keyboard focus and scrolling behind a dialog. */
export function Modal({children,className,label,onClose}:{children:ReactNode;className:string;label:string;onClose:()=>void}){
 const ref=useRef<HTMLDialogElement>(null),close=useRef(onClose);close.current=onClose;
 useEffect(()=>{const dialog=ref.current!;const active=document.activeElement;dialog.showModal();
  const prior=document.documentElement.style.overflow;document.documentElement.style.overflow='hidden';
  const cancel=(e:Event)=>{e.preventDefault();close.current();};dialog.addEventListener('cancel',cancel);
  return()=>{dialog.removeEventListener('cancel',cancel);dialog.close();document.documentElement.style.overflow=prior;
   if(active instanceof HTMLElement)active.focus();};},[]);
 return <dialog ref={ref} className={className} aria-label={label} onClick={e=>{if(e.target===e.currentTarget)onClose();}}>{children}</dialog>;
}
