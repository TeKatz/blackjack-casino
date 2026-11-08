import { describe, expect, it } from 'vitest';
import type { ManorRun } from '../src/bridge/protocol';
import { roomSceneInfo } from '../src/graphics/ManorRoomScene';

const sample: ManorRun = {
  status:'Hunting',message:'',ghostClue:'A door slams to the east. The ghost is approaching.',
  playerRoom:6,turn:2,exitTarget:250,current:'15×',banked:'5×',
  hasCandle:false,hasKey:false,hasCharm:false,passages:[],
  rooms:[{id:6,name:'Study',x:3,y:1,area:'Library',visited:true,available:false,locked:false}],
};
describe('authoritative chamber vignette',()=>{
  it('uses only public room and hint fields',()=>{
    expect(roomSceneInfo(sample)).toEqual({name:'Study',area:'Library',threat:'approaching'});
    expect(roomSceneInfo({...sample,ghostClue:''}).threat).toBe('uncertain');
    expect(roomSceneInfo({...sample,rooms:[]})).toMatchObject({name:'Entrance Hall',area:'Entrance'});
  });
  it('recognizes all six C# areas without guessing any hidden position',()=>{
    for(const area of ['Entrance','Library','Ballroom','Corridor','Attic','Crypt']){
      const input={...sample,rooms:[{...sample.rooms[0],area}]};
      expect(roomSceneInfo(input).area).toBe(area);
    }
    expect(roomSceneInfo({...sample,rooms:[{...sample.rooms[0],area:'Unknown'}]}).area).toBe('Entrance');
  });
});
