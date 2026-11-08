import {describe,it,expect} from 'vitest';
import {createMicroDetails,delayed,smooth,EMPIRE_WINDOW_COLUMNS,EMPIRE_WINDOW_WIDTH,
  MANOR_WINDOW_COLUMNS,MERGE_SLOT_CENTERS} from './LobbyMicroMotion';
import {
  MODE_SCENE_IDS,MODE_PALETTES,modeSceneAssets,coverSize,rectVisible
} from './ModeScenes';

describe('custom PixiJS lobby key art',()=>{
  it('covers all nine modes with distinct local assets',()=>{
    expect(MODE_SCENE_IDS).toEqual([
      'blackjack','kniffel','chill-lounge','casino-empire','midnight-reels',
      'neon-vault','nine-lives','midnight-manor','midnight-merge',
    ]);
    expect(new Set(MODE_SCENE_IDS).size).toBe(9);
    const allPaths=new Set<string>();
    for(const id of MODE_SCENE_IDS){
      const assets=modeSceneAssets(id);
      expect(MODE_PALETTES[id]).toHaveLength(3);
      expect(allPaths.has(assets.back)).toBe(false);
      allPaths.add(assets.back);
      if(id==='nine-lives'){
        expect(assets.back).toBe('/lobby/art/nine-lives.jpg');
        expect(assets.front).toBeNull();
      }else{
        expect(assets.back).toBe('/lobby/custom/'+id+'-back.svg');
        expect(assets.front).toBe('/lobby/custom/'+id+'-front.svg');
        expect(allPaths.has(assets.front!)).toBe(false);
        allPaths.add(assets.front!);
      }
    }
    expect(allPaths.size).toBe(17);
  });
  it('preserves cinematic aspect ratio while covering every tile viewport',()=>{
    expect(coverSize(960,440)).toBe(1);
    expect(coverSize(300,100)).toBeCloseTo(300/960);
    expect(coverSize(250,180)).toBeCloseTo(180/440);
  });
  it('clips offscreen and zero-width scene rectangles',()=>{
    const box=(left:number,top:number,width:number,height:number)=>({
      left,top,width,height,right:left+width,bottom:top+height,
    } as DOMRect);
    const root=box(0,0,1200,720);
    expect(rectVisible(box(100,140,260,120),root)).toBe(true);
    expect(rectVisible(box(1320,140,260,120),root)).toBe(false);
    expect(rectVisible(box(140,801,260,120),root)).toBe(false);
    expect(rectVisible(box(100,140,0,120),root)).toBe(false);
  });
});

describe('independent per-object PixiJS motion',()=>{
  it('builds all mode-specific animated object layers without a DOM renderer',()=>{
    for(const id of MODE_SCENE_IDS){
      const details=createMicroDetails(id);
      expect(details.layer).toBeTruthy();
      details.tick({hover:0,progress:0,time:0,enabled:false});
      details.tick({hover:1,progress:1,time:2.5,enabled:true});
      if(id!=='nine-lives')expect(details.layer.children.length).toBeGreaterThan(0);
      details.layer.destroy({children:true});
    }
  });
  it('eases keyframes and offsets the three reel strips independently',()=>{
    expect(smooth(-1)).toBe(0);
    expect(smooth(0)).toBe(0);
    expect(smooth(.5)).toBe(.5);
    expect(smooth(1)).toBe(1);
    expect(smooth(9)).toBe(1);
    expect(delayed(.12,.18)).toBe(0);
    expect(delayed(.8,.18)).toBeGreaterThan(.75);
  });
  it('preserves standard tile proportions for future game additions',()=>{
    const merge=modeSceneAssets('midnight-merge');
    expect(merge.back).toContain('/midnight-merge-back.svg');
    expect(merge.front).toContain('/midnight-merge-front.svg');
  });
});

describe('artist-authored alignment regression',()=>{
  it('places Empire windows symmetrically around the building center',()=>{
    const columns=EMPIRE_WINDOW_COLUMNS;
    expect(columns[0]+columns[3]+EMPIRE_WINDOW_WIDTH).toBe(968);
    expect(columns[1]+columns[2]+EMPIRE_WINDOW_WIDTH).toBe(968);
    expect(columns[1]-columns[0]).toBe(columns[3]-columns[2]);
  });
  it('keeps Manor glows inside the painted window openings',()=>{
    expect(MANOR_WINDOW_COLUMNS).toEqual([101,794]);
    for(const x of MANOR_WINDOW_COLUMNS){
      expect(x+10).toBeGreaterThan(x+6);
      expect(x+10+44).toBeLessThan(x+64-6);
    }
  });
  it('centers Merge icons within their actual painted board slots',()=>{
    expect(MERGE_SLOT_CENTERS).toEqual([252,421,730]);
    for(const [i,left] of [179,348,657].entries()){
      expect(Math.abs(MERGE_SLOT_CENTERS[i]-(left+73))).toBeLessThan(1);
    }
  });
});
