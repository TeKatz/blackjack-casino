import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {NavIcon} from './NavIcon';
import type {NavIconName} from './NavIcon';

const destinations:NavIconName[]=[
 'news','all','tables','slots','relax','empire','collections','shop','stats',
 'season','challenges','vault','daily','profile','settings','updates',
];

describe('semantic casino navigation art',()=>{
 it('renders a distinct SVG for every navigation destination',()=>{
  const drawings=destinations.map(name=>renderToStaticMarkup(<NavIcon name={name}/>));
  expect(new Set(drawings).size).toBe(destinations.length);
  for(const [index,html] of drawings.entries()){
   expect(html).toContain('data-nav-icon="'+destinations[index]+'"');
   expect(html).toContain('aria-hidden="true"');
   expect(html).toContain('<svg');
   expect(html).not.toContain('lobby-icon-atlas');
  }
 });
});
