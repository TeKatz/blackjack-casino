import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernScratchcards} from '../src/lobby/ModernScratchcards';
import {isScratchcardMessage,isScratchcardSnapshot,previewScratchcards} from '../src/lobby/scratchcardsBridge';

const validTicket={
  ticketId:'ddc29a2d-67af-4603-babf-d4f5d203fc50',
  symbols:[0,0,1,1,2,2,3,3,4],prize:0,collected:false,bankrollAfter:980,
};

describe('scratchcards lobby integration',()=>{
  it('validates native tickets and rejects malformed or counterfeit messages',()=>{
    const snapshot={...previewScratchcards,ticket:validTicket};
    expect(isScratchcardSnapshot(snapshot)).toBe(true);
    expect(isScratchcardMessage({type:'lobby.scratchcards.snapshot',data:snapshot})).toBe(true);
    expect(isScratchcardMessage({type:'lobby.scratchcards.result',
      data:{action:'collect',success:true,message:'Collected.'}})).toBe(true);
    expect(isScratchcardSnapshot({...snapshot,price:0})).toBe(false);
    expect(isScratchcardSnapshot({...snapshot,ticket:{...validTicket,
      symbols:[0,1,2]}})).toBe(false);
    expect(isScratchcardSnapshot({...snapshot,ticket:{...validTicket,
      prize:999999}})).toBe(false);
    expect(isScratchcardSnapshot({...snapshot,ticket:{...validTicket,
      ticketId:'../../secrets'}})).toBe(false);
    expect(isScratchcardMessage({type:'lobby.scratchcards.result',
      data:{action:'buy',success:'yes',message:'fake'}})).toBe(false);
  });
  it('renders a standalone casino submenu rather than another lobby game tile',()=>{
    const html=renderToStaticMarkup(<ModernScratchcards data={previewScratchcards}
      loaded={true} embedded={false} language="de" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('Rubbellose');
    expect(html).toContain('LUCKY');
    expect(html).toContain('SEVENS');
    expect(html).toContain('86,25');
    expect(html).toContain('disabled');
    expect(html).not.toContain('lobby-game');
    expect(html).not.toContain('data-lobby-id');
  });
  it('keeps unrevealed prize and entry price separate',()=>{
    const html=renderToStaticMarkup(<ModernScratchcards
      data={{...previewScratchcards,ticket:validTicket}}
      loaded={true} embedded={true} language="en" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('ACTIVE TICKET');
    expect(html).toContain('Hidden prize');
    expect(html).toContain('BUY TICKET');
    expect(html).not.toContain('Collect your prize.');
  });
});
