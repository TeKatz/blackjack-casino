import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernAccount,PrestigeMark} from './ModernAccount';
import {previewAccount} from './recordsBridge';

const base={
 loaded:true,language:'en' as const,pending:null,notice:null,prestigeOutcome:null,
 motionEnabled:true,
 onBack:()=>{},onRoute:(_route:string)=>{},onRename:(_name:string)=>{},
 onEquip:(_id:string)=>{},onPrestige:()=>{},onExport:()=>{},onFolder:()=>{},
};

describe('Player Profile presentation',()=>{
 it('shows an actionable native prestige IV at level 100, and the new data options',()=>{
   const html=renderToStaticMarkup(<ModernAccount {...base}
     data={{...previewAccount,displayName:'Timo',level:100,prestige:3}}/>);
   expect(html).toContain('ASCEND TO PRESTIGE 4');
   expect(html).toContain('P3');
   expect(html).toContain('P4');
   expect(html).toContain('Data &amp; profile options');
   expect(html).toContain('Edit Identity');
   expect(html).not.toContain('Export &amp; advanced options');
 });
 it('does not offer a premature prestige command below level 100',()=>{
   const html=renderToStaticMarkup(<ModernAccount {...base} data={{
     ...previewAccount,level:99,prestige:3}}/>);
   expect(html).not.toContain('ASCEND TO PRESTIGE 4');
   expect(html).toContain('LEVEL 99 / 100');
 });
 it('keeps the shared header prestige crest compact',()=>{
   const html=renderToStaticMarkup(<PrestigeMark prestige={3} size="compact"/>);
   expect(html).toContain('prestige-seal--compact');
   expect(html).toContain('P3');
 });
});