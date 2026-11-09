import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ArcadeCardArt} from './ArcadeCardArt';

const scenes=[
 ['midnight-wings','arcade-wings-course','aw-course'],
 ['cave-escape','arcade-cave-glider','ac-glide'],
 ['hexagon-hell','arcade-hex-ring-out','ah-out'],
 ['perfect-stack','arcade-stack-active','as-align'],
 ['spike-rush','arcade-spike-runner','ar-jump'],
 ['blade-timing','arcade-blade-shot','ab-shot'],
 ['bullet-storm','arcade-bullet-dodger','au-dodge'],
 ['neon-breaker','arcade-breaker-ball','an-ball'],
 ['death-snake','arcade-snake-face','ad-bite'],
 ['endless-ascent','arcade-ascent-jumper','ae-jump'],
] as const;

describe('Arcade tile illustrations and motion ownership',()=>{
 it.each(scenes)('%s renders its own gameplay scene', (id,subject)=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id={id}/>);
  expect(svg).toContain('class="arcade-scene"');
  expect(svg).toContain('data-arcade-scene="'+id+'"');
  expect(svg).toContain('viewBox="0 0 320 128"');
  expect(svg).toContain('aria-hidden="true"');
  expect(svg).toContain('class="'+subject+'"');
  expect(svg).toContain('id="arcade-'+id+'-sky"');
  expect(svg).toContain('id="arcade-'+id+'-energy"');
 });
 it('keeps gradient definitions unique across all ten simultaneously mounted cards',()=>{
  const all=scenes.map(([id])=>renderToStaticMarkup(<ArcadeCardArt id={id}/>)).join('');
  const ids=Array.from(all.matchAll(/ id="(arcade-[^"]+)"/g),match=>match[1]);
  expect(ids.length).toBe(50);
  expect(new Set(ids).size).toBe(ids.length);
 });
});
