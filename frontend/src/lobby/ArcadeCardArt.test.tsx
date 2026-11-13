import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ArcadeCardArt} from './ArcadeCardArt';

const scenes=[
 ['midnight-wings','preview-wings-pipes','preview-wings-scroll'],
 ['cave-escape','preview-cave-ship','preview-cave-glide'],
 ['hexagon-hell','preview-hex-outer','preview-hex-approach'],
 ['perfect-stack','preview-stack-drop','preview-stack-vertical'],
 ['spike-rush','preview-spike-cube','preview-spike-jump'],
 ['blade-timing','preview-blade-throw','preview-blade-launch'],
 ['bullet-storm','arcade-bullet-dodger','au-dodge'],
 ['neon-breaker','arcade-breaker-ball','an-ball'],
 ['death-snake','arcade-snake-face','ad-bite'],
 ['endless-ascent','preview-ascent-player','preview-ascent-bound'],
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

 it('anchors Hexagon Hell to one center without moving the entire ring group',()=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id="hexagon-hell"/>);
  expect(svg).toContain('class="preview-hex-outer"');
  expect(svg).toContain('class="preview-hex-inner"');
  expect(svg).toContain('cx="160" cy="64" r="55"');
  expect(svg).not.toContain('class="arcade-hex-ring-out" transform="translate');
 });
 it('anchors Blade Timing rotor to the stationary wheel rim',()=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id="blade-timing"/>);
  expect(svg).toContain('class="preview-blade-rotor"');
  expect(svg).toContain('cx="159" cy="55" r="46"');
  expect(svg).not.toContain('class="arcade-blade-wheel" transform="translate');
 });
 it('renders Spike Rush with a two-eyed cube and no detached legs',()=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id="spike-rush"/>);
  expect(svg).toContain('width="34" height="34" rx="5"');
  expect(svg).toContain('x="125" y="84" width="4" height="5"');
  expect(svg).toContain('x="135" y="84" width="4" height="5"');
  expect(svg).not.toContain('class="arcade-spike-runner"');
 });
 it('draws a lower launch platform and higher landing platform for Endless Ascent',()=>{
  const svg=renderToStaticMarkup(<ArcadeCardArt id="endless-ascent"/>);
  expect(svg).toContain('x="82" y="106" width="89"');
  expect(svg).toContain('x="151" y="54" width="92"');
  expect(svg).toContain('class="preview-ascent-player"');
 });
});
