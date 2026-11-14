import {describe,it,expect} from 'vitest';
import {readFileSync} from 'node:fs';
const root=new URL('../../',import.meta.url);
const read=path=>readFileSync(new URL(path,root),'utf8');
const pages=['blackjack','empire','exit','index','lobby','lounge','midnight-dice','midnight-merge','neon-vault','nine-lives','reels','roulette'];
describe('two-state global casino pointer',()=>{
 it('loads on every Vite HTML entry before React',()=>{
   for(const page of pages){const html=read(page+'.html');
     expect(html).toContain('/cursor/casino-cursor.css');
     expect(html).toContain('/cursor/casino-cursor.js');
   }
 });
 it('ships two 32px vector cursor states and a clear accessibility fallback',()=>{
   for(const state of ['normal','pressed']){
     expect(read('public/cursor/casino-'+state+'.svg')).toContain('viewBox="0 0 32 32"');
   }
   const css=read('public/cursor/casino-cursor.css');
   expect(css).toContain('forced-colors:active');
   expect(css).toContain('casino-pressed.svg');
 });
 it('releases the pressed cursor on cancelled pointer and lost window focus',()=>{
   const script=read('public/cursor/casino-cursor.js');
   expect(script).toContain("pointercancel");
   expect(script).toContain("visibilitychange");
   expect(script).toContain("blur");
 });
});
