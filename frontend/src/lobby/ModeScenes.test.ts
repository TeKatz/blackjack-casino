import {describe,it,expect} from 'vitest';
import {existsSync,readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {
  MODE_SCENE_IDS,MODE_PALETTES,modeSceneAssets,coverSize,rectVisible
} from './ModeScenes';

const assetPath=(path:string)=>fileURLToPath(new URL('../../public'+path,import.meta.url));

describe('custom PixiJS lobby key art',()=>{
  it('covers every shipped mode with an independent texture identity',()=>{
    expect(MODE_SCENE_IDS).toEqual([
      'blackjack','kniffel','chill-lounge','casino-empire','midnight-reels',
      'neon-vault','nine-lives','midnight-manor','midnight-merge',
    ]);
    expect(new Set(MODE_SCENE_IDS).size).toBe(9);
    for(const id of MODE_SCENE_IDS){
      const assets=modeSceneAssets(id);
      expect(existsSync(assetPath(assets.back))).toBe(true);
      expect(MODE_PALETTES[id]).toHaveLength(3);
      if(id==='nine-lives'){
        expect(assets.back).toBe('/lobby/art/nine-lives.jpg');
        expect(assets.front).toBeNull();
      }else{
        expect(assets.back).toBe('/lobby/custom/'+id+'-back.svg');
        expect(assets.front).toBe('/lobby/custom/'+id+'-front.svg');
        expect(existsSync(assetPath(assets.front!))).toBe(true);
      }
    }
  });
  it('uses custom local SVG textures with separate movable foreground artwork',()=>{
    for(const id of MODE_SCENE_IDS.filter(id=>id!=='nine-lives')){
      for(const path of [modeSceneAssets(id).back,modeSceneAssets(id).front!]){
        const svg=readFileSync(assetPath(path),'utf8');
        expect(svg).toMatch(/^<svg xmlns="http:\/\/www.w3.org\/2000\/svg"/);
        expect(svg).toContain('viewBox="0 0 960 440"');
        expect(svg.length).toBeGreaterThan(1500);
        expect(svg).not.toMatch(/<image\b|<foreignObject\b|href="https?:/i);
      }
    }
  });
  it('fits cinematic art across portrait, wide and compact viewport crops',()=>{
    expect(coverSize(960,440)).toBe(1);
    expect(coverSize(300,100)).toBeCloseTo(300/960);
    expect(coverSize(250,180)).toBeCloseTo(180/440);
  });
  it('clips scenes outside the visible lobby and rejects collapsed thumbnails',()=>{
    const box=(left:number,top:number,width:number,height:number)=>({
      left,top,width,height,right:left+width,bottom:top+height,
    } as DOMRect);
    const root=box(0,0,1200,720);
    expect(rectVisible(box(100,140,260,120),root)).toBe(true);
    expect(rectVisible(box(1320,140,260,120),root)).toBe(false);
    expect(rectVisible(box(140,801,260,120),root)).toBe(false);
    expect(rectVisible(box(100,140,0,120),root)).toBe(false);
  });
  it('uses Pixi-backed windows rather than DOM fake cards, dice or merge art',()=>{
    const source=readFileSync(fileURLToPath(new URL('./main.tsx',import.meta.url)),'utf8');
    expect(source).toContain('data-mode-art={mode.id}');
    expect(source).toContain('<LobbyAmbient motionEnabled=');
    expect(source).not.toContain('<GamePreview');
    expect(source).not.toContain('<PremiumTileLayers');
    expect(source).not.toContain("className={'game-art-image");
  });
});
