import {describe,expect,it} from 'vitest';
import {readFileSync} from 'node:fs';

const motion=readFileSync(new URL('./arcadeArtMotion.css',import.meta.url),'utf8');
const mainLayers=[
 ['wings-course','aw-course'],['cave-glider','ac-glide'],
 ['hex-ring-out','ah-out'],['stack-active','as-align'],
 ['spike-runner','ar-jump'],['blade-shot','ab-shot'],
 ['bullet-dodger','au-dodge'],['breaker-ball','an-ball'],
 ['snake-face','ad-bite'],['ascent-jumper','ae-jump']
];

describe('Arcade card hover CSS',()=>{
 it.each(mainLayers)('%s has its own hover and keyboard-focus choreography',(layer,animation)=>{
  expect(motion).toContain('.arcade-game-card:is(:hover,:focus-visible) .arcade-'+layer);
  expect(motion).toContain('@keyframes '+animation);
 });
 it('has no free-running animation on non-interactive scenes',()=>{
  expect(motion).not.toMatch(/\.arcade-scene\s*\{[^}]*animation:/);
 });
 it('respects reduced motion from both WebView2 and the operating system',()=>{
  expect(motion).toContain('@media(prefers-reduced-motion:reduce)');
  expect(motion).toContain('.casino-lobby-shell[data-reduced="true"] .arcade-scene *');
  expect(motion).toContain('animation:none!important');
 });
});
