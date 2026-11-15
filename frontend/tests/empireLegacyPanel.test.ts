import {describe,it,expect} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {EmpireLegacyPanel} from '../src/empire/EmpireLegacyPanel';
import type {EmpireLegacy} from '../src/empire/empireBridge';

function sample():EmpireLegacy{
  return {
    unlocked:true,tokens:5,earned:1,total:20,
    upgrades:Array.from({length:15},(_,i)=>({
      id:(i<5?'active-':i<10?'passive-':'hybrid-')+(i%5+1),
      path:i<5?'active':i<10?'passive':'hybrid',
      name:'Sample upgrade '+i,description:'A permanent effect',cost:i%5+1,
      minimumPrestige:i%5+1,previousId:i%5===0?null:'previous',
      owned:false,unlocked:i%5===0,canBuy:i%5===0,
    })),
    milestones:Array.from({length:20},(_,i)=>({
      id:'milestone-'+i,name:'Long term goal '+i,description:'Make progress',
      category:i<5?'clicks':i<10?'credits':i<15?'levels':'prestige',
      current:'5',target:'100',earned:i===0,progress:.05,
    })),
  };
}

describe('Empire Legacy panel',()=>{
  it('shows one next permanent upgrade for each specialization, not all fifteen',()=>{
    const html=renderToStaticMarkup(createElement(EmpireLegacyPanel,
      {legacy:sample(),prestige:1,de:false,disabled:false,buy:()=>{}}));
    expect(html).toContain('Your Legacy');
    expect(html).toContain('High Roller');
    expect(html).toContain('Tycoon');
    expect(html).toContain('Inner Circle');
    expect(html).toContain('Sample upgrade 0');
    expect(html).not.toContain('Sample upgrade 1');
    expect(html).toContain('5');
  });
  it('keeps sections collapsible and milestones discoverable',()=>{
    const html=renderToStaticMarkup(createElement(EmpireLegacyPanel,
      {legacy:sample(),prestige:1,de:true,disabled:false,buy:()=>{}}));
    expect(html).toContain('Dauerhafte');
    expect(html).toContain('MEILENSTEINE');
    expect(html).toContain('inert');
    expect(html).toContain('aria-expanded');
  });
});
