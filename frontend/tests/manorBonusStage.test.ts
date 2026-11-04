import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { createElement } from 'react';
import type { ManorRun } from '../src/bridge/protocol';
import { ManorBonusStage } from '../src/graphics/ManorBonusStage';

const run: ManorRun = {
  status:'Hunting',message:'You enter Library.',ghostClue:'Distant footsteps echo through the Manor. The ghost is far away.',
  playerRoom:0,turn:2,exitTarget:250,current:'20×',banked:'10×',hasCandle:true,hasKey:false,
  hasCharm:false,
  rooms:[
    {id:0,name:'Entrance Hall',x:0,y:1,area:'Entrance',visited:true,available:false,locked:false},
    {id:1,name:'Library',x:1,y:1,area:'Library',visited:false,available:true,locked:false},
    {id:2,name:'Crypt',x:2,y:2,area:'Crypt',visited:false,available:false,locked:true},
  ],
  passages:[{from:0,to:1,requiresKey:false},{from:0,to:2,requiresKey:true}],
};
const html = (value: ManorRun) => renderToStaticMarkup(createElement(ManorBonusStage,{
  run:value,language:'de',disabled:false,reducedMotion:false,onAction:()=>undefined,
}));
describe('in-slot Manor bonus gameplay scene',()=>{
  it('renders the current room, settled multipliers, and real legal choices',()=>{
    const page=html(run);
    expect(page).toContain('manor-bonus-stage');
    expect(page).toContain('Eingangshalle');
    expect(page).toContain('Bibliothek');
    expect(page).toContain('20×');
    expect(page).toContain('10×');
    expect(page).toContain('bonus-room-button');
    expect(page).not.toContain('Krypta</span><small>');
  });
  it('never invents another legal destination or reveals secret ghost coordinates',()=>{
    const page=html({...run,rooms:run.rooms.map(room=>({...room,available:false}))});
    expect(page).not.toContain('bonus-room-button');
    expect(page).toContain('Kein angrenzender Raum');
    expect(page).not.toContain('ghost-position');
  });
  it('honors the reduced-motion option on the entire panel',()=>{
    const page=renderToStaticMarkup(createElement(ManorBonusStage,{
      run,language:'en',disabled:true,reducedMotion:true,onAction:()=>undefined,
    }));
    expect(page).toContain('manor-bonus-stage no-motion');
    expect(page).toContain('disabled=""');
  });
});
