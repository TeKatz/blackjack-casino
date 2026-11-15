import {describe,expect,it} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {ChoiceButtons} from '../src/lounge/adventure';
import type {TaleRun} from '../src/lounge/adventureBridge';

const copy={what:'WHAT DO YOU DO?',roll:'ROLL THE D20'} as Parameters<typeof ChoiceButtons>[0]['copy'];
const render=(choices:unknown[])=>renderToStaticMarkup(createElement(ChoiceButtons,{
 run:{choices} as unknown as TaleRun,copy,pending:false,send:()=>{},
}));

describe('Tales story options',()=>{
 it('never prints numeric zero for the empty optional event array',()=>{
  const html=render([
   {id:'ask',text:'Ask about the notice.',events:[],check:null,encounter:null},
   {id:'leave',text:'Continue to the inn.',events:[],check:null,encounter:null},
  ]);
  expect(html).toContain('Ask about the notice.');
  expect(html).toContain('Continue to the inn.');
  expect(html).not.toMatch(/<small(?:\s[^>]*)?>0<\/small>/);
  expect(html).not.toContain('backroom-choice-event');
  expect((html.match(/class="backroom-choice"/g)||[])).toHaveLength(2);
 });
 it('keeps actual event, check and encounter information',()=>{
  const html=render([
   {id:'read',text:'Read the ledger.',events:['DiscoverCaseNode'],
    check:{ability:'Intelligence',difficulty:14},encounter:null},
   {id:'wager',text:'Accept the wager.',events:[],check:null,encounter:'Blackjack'},
  ]);
  expect(html).toContain('Intelligence · DC 14');
  expect(html).toContain('Clue');
  expect((html.match(/class="backroom-choice-event"/g)||[])).toHaveLength(1);
  expect(html).toContain('BLACKJACK');
  expect(html).not.toMatch(/<small(?:\s[^>]*)?>0<\/small>/);
 });
});
