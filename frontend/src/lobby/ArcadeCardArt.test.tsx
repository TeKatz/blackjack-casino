import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {readFileSync} from 'node:fs';
const motion=readFileSync(new URL('./arcadeArtMotion.css',import.meta.url),'utf8');
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
 it.each(scenes)('%s has its own rendered illustration and gameplay motion', (id,subject,keyframe)=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id={id}/>);
  expect(svg).toContain('class="arcade-scene"');
  expect(svg).toContain('data-arcade-scene="'+id+'"');
  expect(svg).toContain('viewBox="0 0 320 128"');
  expect(svg).toContain('aria-hidden="true"');
  expect(svg).toContain('class="'+subject+'"');
  expect(svg).toContain('id="arcade-'+id+'-sky"');
  expect(svg).toContain('id="arcade-'+id+'-energy"');
  expect(motion).toContain('.arcade-game-card:is(:hover,:focus-visible) .'+subject);
  expect(motion).toContain('@keyframes '+keyframe);
 });
 it('keeps gradient definitions unique across all ten simultaneously mounted cards',()=>{
  const all=scenes.map(([id])=>renderToStaticMarkup(<ArcadeCardArt id={id}/>)).join('');
  const ids=Array.from(all.matchAll(/ id="(arcade-[^"]+)"/g),match=>match[1]);
  expect(ids.length).toBe(50);
  expect(new Set(ids).size).toBe(ids.length);
 });
 it('disables all animated SVG layers for OS and in-game reduced-motion preferences',()=>{
  expect(motion).toContain('@media(prefers-reduced-motion:reduce)');
  expect(motion).toContain('.casino-lobby-shell[data-reduced="true"] .arcade-scene *');
  expect(motion).toContain('animation:none!important');
  expect(motion).toContain(':focus-visible');
 });
});
