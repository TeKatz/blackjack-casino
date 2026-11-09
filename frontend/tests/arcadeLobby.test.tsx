import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ArcadeMenu} from '../src/lobby/ArcadeMenu';
import {ArcadeGameScreen} from '../src/lobby/ArcadeGameScreen';
import {ARCADE_MODE_ID,arcadeCategoryNames,arcadeGames,arcadeLobbyMode} from '../src/lobby/arcadeCatalog';

describe('Arcade Games lobby collection',()=>{
 it('adds one correctly localized main-lobby tile',()=>{
  const de=arcadeLobbyMode('de'),en=arcadeLobbyMode('en');
  expect(de.id).toBe(ARCADE_MODE_ID);
  expect(de.title).toBe('Arcade Games');
  expect(de.enabled).toBe(true);
  expect(de.secondary).toBe(false);
  expect(de.description).not.toEqual(en.description);
 });
 it('ships ten distinct future minigames, with bounded duration and complete localization',()=>{
  expect(arcadeGames).toHaveLength(10);
  expect(new Set(arcadeGames.map(g=>g.id)).size).toBe(10);
  expect(arcadeGames.map(g=>g.name)).toEqual([
   'Midnight Wings','Cave Escape','Hexagon Hell','Perfect Stack','Spike Rush',
   'Blade Timing','Bullet Storm','Neon Breaker','Death Snake','Endless Ascent'
  ]);
  for(const game of arcadeGames){
   expect(game.seconds[0]).toBeGreaterThan(0);
   expect(game.seconds[1]).toBeGreaterThanOrEqual(game.seconds[0]);
   expect(game.seconds[1]).toBeLessThanOrEqual(180);
   expect(game.difficulty).toBeGreaterThanOrEqual(1);
   expect(game.difficulty).toBeLessThanOrEqual(5);
   expect(arcadeCategoryNames[game.category]).toBeTruthy();
   for(const language of ['de','en'] as const){
    expect(game.description[language].length).toBeGreaterThan(15);
    expect(game.detail[language].length).toBeGreaterThan(30);
    expect(game.controls[language].length).toBeGreaterThan(1);
   }
  }
 });
 it('renders the whole accessible selection in both languages without pretending games are live',()=>{
  for(const language of ['de','en'] as const){
   const html=renderToStaticMarkup(<ArcadeMenu language={language} onBack={()=>{}}/>);
   expect(html).toContain('data-testid="arcade-menu"');
   expect((html.match(/data-arcade-id=/g)||[]).length).toBe(10);
   expect(html).toContain('aria-pressed="true"');
   expect(html).toContain('Midnight Wings');
   expect(html).toContain('Endless Ascent');
   expect(html).toContain(language==='de'?'7 SPIELBAR · 3 GEPLANT':'7 PLAYABLE · 3 PLANNED');
   expect((html.match(/arcade-card-live/g)||[]).length).toBe(7);
   expect((html.match(/arcade-card-soon/g)||[]).length).toBe(10);
   expect(html).not.toContain('lobby.open');
  }
 });
 it('renders independent game screens for the first seven games with local scoreboards',()=>{
  for(const id of ['midnight-wings','cave-escape','perfect-stack','blade-timing',
   'death-snake','neon-breaker','bullet-storm'] as const){
   const html=renderToStaticMarkup(<ArcadeGameScreen id={id} language="de" onBack={()=>{}}/>);
   expect(html).toContain('data-testid="arcade-game-screen"');
   expect(html).toContain('data-game-id="'+id+'"');
   expect(html).toContain('data-testid="arcade-canvas"');
   expect(html).toContain('data-testid="arcade-score"');
   expect(html).toContain('data-testid="arcade-best"');
   expect(html).toContain('SPIEL STARTEN');
  }
 });
});