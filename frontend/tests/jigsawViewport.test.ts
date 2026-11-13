import {describe,it,expect} from 'vitest';
import {boundedView,viewport,toBoard,clampPiece,clampGroupMotion,needsRecovery} from '../src/lounge/jigsawViewport';
describe('Jigsaw viewport safety',()=>{
 it('fits and centers a 960×600 board in a wide responsive canvas',()=>{
  const frame={width:1640,height:850},view={zoom:1,x:0,y:0};
  const v=viewport(frame,view);
  expect(v.fit).toBeCloseTo(850/600);
  expect(v.offsetX).toBeGreaterThan(0);
  expect(toBoard(frame,view,(v.offsetX+480)*v.fit,(v.offsetY+300)*v.fit)).toEqual({x:480,y:300});
 });
 it('keeps the reference image in view while zoomed and after resize',()=>{
  expect(boundedView({width:1600,height:850},{zoom:3,x:99999,y:-99999})).toEqual(
   {zoom:3,x:viewport({width:1600,height:850},{zoom:3,x:0,y:0}).maxX,
    y:-viewport({width:1600,height:850},{zoom:3,x:0,y:0}).maxY});
  expect(boundedView({width:1600,height:850},{zoom:1,x:100,y:-40})).toEqual({zoom:1,x:0,y:0});
 });
 it('keeps a new piece fully on the board',()=>{
  expect(clampPiece(-200,999,40,25)).toEqual({x:0,y:576});
 });
 it('constrains whole connected groups without splitting them',()=>{
  const pieces=[{x:910,y:520},{x:934,y:520}];
  expect(clampGroupMotion(pieces,[0,1],0,1300,1200,40,25)).toEqual({dx:2,dy:56});
 });
 it('identifies legacy offboard pieces for recovery without touching locked pieces',()=>{
  expect(needsRecovery({onBoard:true,locked:false,x:-120,y:10},40,25)).toBe(true);
  expect(needsRecovery({onBoard:true,locked:true,x:-120,y:10},40,25)).toBe(false);
  expect(needsRecovery({onBoard:true,locked:false,x:935,y:576},40,25)).toBe(false);
 });
});
