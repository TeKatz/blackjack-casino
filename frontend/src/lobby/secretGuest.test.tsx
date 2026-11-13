import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ShopShowcase} from './ShopShowcase';
import {PlayerPortrait,portraitPath} from './Cosmetics';
import type {ShopItem} from './shopBridge';

const guest:ShopItem={
 id:'portrait-secret-guest',name:'The Secret Guest',
 description:'She appears before the room remembers.',kind:'Portrait',
 visualVariant:'portrait-secret-guest',price:0,shopAvailable:false,
 requiredPrestige:0,owned:false,equipped:false,canBuy:false,
};
describe('The Secret Guest premium portrait',()=>{
 it('shows the bespoke animated image, gold effects and a static low-motion source',()=>{
  const html=renderToStaticMarkup(<ShopShowcase item={guest} motionEnabled/>);
  expect(html).toContain('data-secret-guest="true"');
  expect(html).toContain('data-testid="secret-guest-showcase"');
  expect(html).toContain('secret-guest-crown-gem');
  expect(html).toContain('secret-guest-sweep');
  expect(html).toContain('/shop/portraits/secret-guest.svg');
  expect(html).toContain('prefers-reduced-motion: reduce');
  expect(html).toContain('/shop/portraits/secret-guest.jpg');
 });
 it('never loads motion artwork when player animations are disabled',()=>{
  const html=renderToStaticMarkup(<ShopShowcase item={guest} motionEnabled={false}/>);
  expect(html).toContain('/shop/portraits/secret-guest.jpg');
  expect(html).not.toContain('/shop/portraits/secret-guest.svg');
  expect(html).not.toContain('<source');
 });
 it('keeps the distinct profile finish and still in the player identity view',()=>{
  expect(portraitPath('portrait-secret-guest')).toBe('/shop/portraits/secret-guest.jpg');
  const html=renderToStaticMarkup(<PlayerPortrait portrait="portrait-secret-guest" frame="classic"/>);
  expect(html).toContain('secret-guest-profile');
  expect(html).toContain('/shop/portraits/secret-guest.jpg');
  expect(html).toContain('secret-guest-profile-spark');
 });
 it('does not change other portrait markup or previews',()=>{
  const other={...guest,id:'portrait-midnight-lady',visualVariant:'portrait-midnight-lady'};
  const shop=renderToStaticMarkup(<ShopShowcase item={other}/>);
  expect(shop).not.toContain('secret-guest-showcase');
  expect(shop).toContain('/shop/portraits/midnight-lady.png');
  expect(renderToStaticMarkup(<PlayerPortrait portrait="portrait-midnight-lady" frame="classic"/>))
   .not.toContain('secret-guest-profile');
 });
});
