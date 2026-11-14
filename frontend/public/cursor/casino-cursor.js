/* Avoid per-component pointer hooks and keep state through GPU frames. */
(()=>{
 const root=document.documentElement;
 if(root.dataset.casinoCursorInstalled==='true')return;
 root.dataset.casinoCursorInstalled='true';
 let down=false;
 function set(value){if(value===down)return;down=value;root.dataset.casinoCursor=value?'pressed':'normal'}
 root.dataset.casinoCursor='normal';
 document.addEventListener('pointerdown',event=>{
   if(event.pointerType!=='touch'&&event.isPrimary&&event.button===0)set(true);
 },true);
 document.addEventListener('pointerup',()=>set(false),true);
 document.addEventListener('pointercancel',()=>set(false),true);
 document.addEventListener('pointermove',event=>{
   if(down&&event.pointerType==='mouse'&&(event.buttons&1)===0)set(false);
 },true);
 document.addEventListener('dragend',()=>set(false),true);
 window.addEventListener('mouseup',()=>set(false),true);
 window.addEventListener('blur',()=>set(false));
 document.addEventListener('visibilitychange',()=>{if(document.hidden)set(false)});
})();