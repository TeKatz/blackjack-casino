import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {RestorationScene,sameRestorationArtwork} from './RestorationScene';
import type {MergeRoom} from './bridge';

const areas=['lobby','bar','gaming-floor','conservatory','lounge','gallery',
 'theater','terrace','vip-suites','observatory'];
function room(areaId:string,phase:number):MergeRoom{
 const milestones=[0,1,2,3].map(n=>({id:'step'+n,name:'Step '+n,points:100*n,unlocked:n<=phase}));
 return {areaId,areaName:areaId,areaIndex:areas.indexOf(areaId)+1,
  current:{id:'step'+phase,name:'Step '+phase,points:phase*100},
  next:phase===3?null:{id:'step'+(phase+1),name:'Step '+(phase+1),points:(phase+1)*100},
  pointsUntilNext:phase===3?0:100,upgradeProgress:phase===3?1:0,milestones};
}
describe('Ten cinematic Midnight Merge restoration scenes',()=>{
 it.each(areas)('renders %s with four distinct authoritative stage phases',areaId=>{
  const samples=[0,1,2,3].map(phase=>renderToStaticMarkup(
    <RestorationScene stage={room(areaId,phase)}/>));
  for(let phase=0;phase<4;phase++){
   expect(samples[phase]).toContain('data-scene="'+areaId+'"');
   expect(samples[phase]).toContain('data-phase="'+phase+'"');
   expect(samples[phase]).toContain('<svg');
  }
  expect(new Set(samples).size).toBe(4);
 });
 it('respects disabled animations without changing restoration appearance',()=>{
  const image=renderToStaticMarkup(<RestorationScene stage={room('observatory',3)} animated={false}/>);
  expect(image).toContain('merge-room-art static');
  expect(image).not.toContain('merge-restored-atmosphere');
 });
 it('shows a full uncropped panorama while expanded',()=>{
  const image=renderToStaticMarkup(<RestorationScene stage={room('gaming-floor',3)} fit="contain"/>);
  expect(image).toContain('preserveAspectRatio="xMidYMid meet"');
  expect(image).toContain('data-complete="true"');
  expect(renderToStaticMarkup(<RestorationScene stage={room('gaming-floor',3)}/>))
    .toContain('preserveAspectRatio="xMidYMid slice"');
 });
 it.each(areas)('gives completed %s an ambient layer without introducing one earlier',areaId=>{
  const completed=renderToStaticMarkup(<RestorationScene stage={room(areaId,3)}/>);
  const underConstruction=renderToStaticMarkup(<RestorationScene stage={room(areaId,2)}/>);
  expect(completed).toContain('merge-restored-atmosphere');
  expect(underConstruction).not.toContain('merge-restored-atmosphere');
  expect(completed).toContain('merge-depth-far');
  expect(completed).toContain('merge-depth-middle');
  expect(completed).toContain('merge-depth-near');
 });
 it('does not replace unchanged room SVGs when only board or delivery progress changes',()=>{
  const a=room('gaming-floor',2);
  const b={...a,upgradeProgress:.4,pointsUntilNext:40,
    milestones:a.milestones.map(x=>({...x}))};
  expect(sameRestorationArtwork({stage:a,animated:true,fit:'crop'},
    {stage:b,animated:true,fit:'crop'})).toBe(true);
  expect(sameRestorationArtwork({stage:a,animated:true,fit:'crop'},
    {stage:room('gaming-floor',3),animated:true,fit:'crop'})).toBe(false);
  expect(sameRestorationArtwork({stage:a,animated:true,fit:'crop'},
    {stage:a,animated:true,fit:'contain'})).toBe(false);
  expect(sameRestorationArtwork({stage:a,animated:true,fit:'crop'},
    {stage:a,animated:false,fit:'crop'})).toBe(false);
 });
});
