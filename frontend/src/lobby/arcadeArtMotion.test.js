import {describe,expect,it} from 'vitest';
import {readFileSync} from 'node:fs';

const oldMotion=readFileSync(new URL('./arcadeArtMotion.css',import.meta.url),'utf8');
const motion=readFileSync(new URL('./arcadeArtMotionV2.css',import.meta.url),'utf8');
const corrected=[
 ['wings-pipes','preview-wings-scroll'],
 ['cave-ship','preview-cave-glide'],
 ['hex-outer','preview-hex-approach'],
 ['stack-drop','preview-stack-vertical'],
 ['spike-cube','preview-spike-jump'],
 ['blade-throw','preview-blade-launch'],
 ['ascent-player','preview-ascent-bound']
];
const accepted=[['bullet-dodger','au-dodge'],['breaker-ball','an-ball'],['snake-face','ad-bite']];

describe('Arcade gameplay hover contracts',()=>{
 it.each(corrected)('corrected %s has interactive-only game motion',(layer,keyframe)=>{
  expect(motion).toContain('.arcade-game-card:is(:hover,:focus-visible) .preview-'+layer);
  expect(motion).toContain('@keyframes '+keyframe);
 });
 it.each(accepted)('accepted %s still uses the untouched animation',(layer,keyframe)=>{
  expect(oldMotion).toContain('.arcade-game-card:is(:hover,:focus-visible) .arcade-'+layer);
  expect(oldMotion).toContain('@keyframes '+keyframe);
 });
 it('rotates hex rings and blades around fixed viewBox coordinates, not moving SVG groups',()=>{
  expect(motion).toContain('transform-origin:160px 64px');
  expect(motion).toContain('transform-origin:159px 55px');
  expect((motion.match(/transform-box:view-box/g)||[]).length).toBeGreaterThanOrEqual(2);
  expect(motion).not.toMatch(/\.preview-(hex|blade)[\w-]*\{[^}]*translate:\s*[1-9]/);
 });
 it('drops the stack vertically without sideways motion',()=>{
  const keyframe=motion.slice(motion.indexOf('@keyframes preview-stack-vertical'),
    motion.indexOf('@keyframes preview-stack-landing'));
  expect(keyframe).toContain('translate:0 28px');
  expect(keyframe).not.toMatch(/translate:\s*[1-9]/);
 });
 it('makes the spike pass before the cube lands',()=>{
  expect(motion).toContain('to{translate:-165px 0}');
  expect(motion).toContain('51%{translate:0 -59px');
  expect(motion).toContain('91%,100%{translate:0 0;rotate:360deg}');
 });
 it('lands the ascent character on the upper platform at its top surface',()=>{
  expect(motion).toContain('26%{translate:6px -45px');
  expect(motion).toContain('45%{translate:34px -75px');
  expect(motion).toContain('70%,87%{translate:55px -52px');
 });
 it('disables all motion for operating system and in-game preferences',()=>{
  expect(motion).toContain('@media(prefers-reduced-motion:reduce)');
  expect(motion).toContain('.casino-lobby-shell[data-reduced="true"]');
  expect(motion).toContain('animation:none!important');
 });
});
