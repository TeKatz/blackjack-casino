import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {CosmeticTitle,PlayerPortrait,DicePreview,ProfileFrame,portraitPath,titleStyle} from '../src/lobby/Cosmetics';
describe('equipped cosmetic presentation',()=>{
 it('resolves the default and special portrait file formats',()=>{
  expect(portraitPath('portrait-house-guest')).toBe('/shop/portraits/house-guest.jpg');
  expect(portraitPath('portrait-moon-cat')).toBe('/shop/portraits/moon-cat.png');
  expect(portraitPath('../remote')).toBe('');
 });
 it('uses native special-title colors and gradients',()=>{
  expect(titleStyle('title-neon-magenta').color).toBe('#FF6BC7');
  expect(titleStyle('title-adventure-bell').backgroundImage).toContain('#65D7CF');
  expect(renderToStaticMarkup(<CosmeticTitle variant="title-gold">Winner</CosmeticTitle>)).toContain('#FFD877');
 });
 it('shows the equipped frame and actual portrait together',()=>{
  const html=renderToStaticMarkup(<PlayerPortrait portrait="portrait-house-guest" frame="dynasty-crown" large/>);
  expect(html).toContain('data-frame="dynasty-crown"');expect(html).toContain('house-guest.jpg');
  expect(renderToStaticMarkup(<ProfileFrame variant="static-slate"/>)).not.toContain(' animated');
 });
 it('keeps engraved dice and native colors in the shop preview',()=>{
  const html=renderToStaticMarkup(<DicePreview variant="dice-copper"/>);
  expect(html).toContain('data-engraved="true"');expect(html).toContain('#E0B38B');expect(html).toContain('#302A29');
 });
});
