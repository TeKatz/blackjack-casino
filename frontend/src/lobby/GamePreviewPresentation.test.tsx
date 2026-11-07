import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {GamePreview} from './GamePreview';

describe('cinematic lobby key-art ownership',()=>{
 it('does not render a second collection of dice, slot reels or chess pieces over the hero art',()=>{
  for(const id of ['kniffel','chill-lounge','casino-empire',
   'midnight-reels','neon-vault','nine-lives','midnight-manor'])
   expect(renderToStaticMarkup(<GamePreview id={id}/>)).toBe('');
 });
 it('retains live previews only where they communicate gameplay',()=>{
  expect(renderToStaticMarkup(<GamePreview id="blackjack"/>)).toContain('preview-blackjack-hand');
  expect(renderToStaticMarkup(<GamePreview id="midnight-merge"/>)).toContain('preview-merge-grid');
 });
});
