import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernVault} from './ModernVault';
import {previewVault} from './recordsBridge';

const noop=()=>{};
const render=(tierId:string,full=false)=>renderToStaticMarkup(
 <ModernVault data={{...previewVault,tierId,isFull:full,tier:tierId+' Vault'}}
  loaded language="en" pending={false} reward={null} notice={null}
  onBack={noop} onOpen={noop} onAlbum={noop}/>
);

describe('premium Vault presentation',()=>{
 it('renders the five correctly colored tiers and enlarged safe without changing locked action',()=>{
  const html=render('platinum');
  for(const tier of ['bronze','silver','gold','platinum','diamond'])
   expect(html).toContain('data-tier="'+tier+'"');
  expect(html).toContain('class="vault-safe-body"');
  expect(html).toContain('class="vault-bolt bolt-nw"');
  expect(html).toContain('aria-current="step"');
  expect(html).toMatch(/class="vault-break" disabled=""/);
 });
 it('retains native action eligibility instead of unlocking a partly filled Vault',()=>{
  const html=render('bronze',true);
  expect(html).toMatch(/class="vault-break" type="button"/);
  expect(html).not.toMatch(/class="vault-break" disabled/);
 });
});
