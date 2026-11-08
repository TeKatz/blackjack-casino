import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ShopShowcase} from './ShopShowcase';
import type {ShopItem} from './shopBridge';

const secret:ShopItem={
  id:'portrait-secret-guest',
  kind:'Portrait',
  name:'The Secret Guest',
  description:'She appears before the room remembers.',
  visualVariant:'portrait-secret-guest',
  price:0,
  shopAvailable:false,
  requiredPrestige:0,
  owned:false,
  equipped:false,
  canBuy:false,
};

describe('The Secret Guest exclusive presentation',()=>{
  it('renders the animated premium card and reduced-motion source when motion is enabled',()=>{
    const html=renderToStaticMarkup(<ShopShowcase item={secret} motionEnabled/>);
    expect(html).toContain('data-testid="secret-guest-showcase"');
    expect(html).toContain('data-secret-guest="true"');
    expect(html).toContain('src="/shop/portraits/secret-guest.svg"');
    expect(html).toContain('loading="eager"');
    expect(html).toContain('srcSet="/shop/portraits/secret-guest.jpg"');
    expect(html).toContain('media="(prefers-reduced-motion: reduce)"');
    expect(html).toContain('secret-guest-crown-gem');
    expect(html).toContain('secret-guest-embers');
  });

  it('renders the static approved portrait and no animated source when motion is disabled',()=>{
    const html=renderToStaticMarkup(<ShopShowcase item={secret} motionEnabled={false}/>);
    expect(html).toContain('data-testid="secret-guest-showcase"');
    expect(html).toContain('src="/shop/portraits/secret-guest.jpg"');
    expect(html).not.toContain('src="/shop/portraits/secret-guest.svg"');
    expect(html).not.toContain('media="(prefers-reduced-motion: reduce)"');
  });
});
