import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernScratchcards} from '../src/lobby/ModernScratchcards';
import {ScratchcardMotif,scratchSymbolLabel} from '../src/lobby/ScratchcardMotifs';
import {isScratchcardMessage,isScratchcardSnapshot,previewScratchcards} from '../src/lobby/scratchcardsBridge';
import {clearedFraction,revealedIndexesForTicket,SCRATCH_BRUSH_WIDTH,SCRATCH_REVEAL_COVERAGE,strokeOverlapsRect,winningCellIndexes} from '../src/lobby/scratchcardsPresentation';

const validTicket={
  ticketId:'ddc29a2d-67af-4603-babf-d4f5d203fc50',
  gameId:'lucky-sevens',symbols:[0,0,1,1,2,2,3,3,4],prize:0,collected:false,bankrollAfter:980,
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
      gameId:'not-a-real-game'}})).toBe(false);
    expect(isScratchcardSnapshot({...snapshot,ticket:{...validTicket,
      gameId:'gold-rush',prize:10}})).toBe(false);
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

describe('scratchcard visual-only reveal mechanics',()=>{
  it('uses a 25-pixel brush and reveals at exactly 50% transparent surface',()=>{
    expect(SCRATCH_BRUSH_WIDTH).toBe(25);
    expect(SCRATCH_REVEAL_COVERAGE).toBe(.5);
    const width=20,height=20,step=2;
    const pixels=new Uint8ClampedArray(width*height*4);
    for(let i=3;i<pixels.length;i+=4)pixels[i]=255;
    // Repeated scrubbing of the same narrow strip cannot reveal the cell.
    for(let y=0;y<height;y++)for(let x=0;x<6;x++)
      pixels[(y*width+x)*4+3]=0;
    expect(clearedFraction(pixels,width,height,step)).toBeCloseTo(.3);
    expect(clearedFraction(pixels,width,height,step)).toBeLessThan(SCRATCH_REVEAL_COVERAGE);
    for(let y=0;y<height;y++)for(let x=6;x<10;x++)
      pixels[(y*width+x)*4+3]=0;
    expect(clearedFraction(pixels,width,height,step)).toBeCloseTo(.5);
    expect(clearedFraction(pixels,width,height,step)).toBeGreaterThanOrEqual(SCRATCH_REVEAL_COVERAGE);
  });
  it('routes one uninterrupted horizontal sweep across multiple fields and their gap',()=>{
    const cells=[
      {left:0,right:100,top:0,bottom:60},
      {left:108,right:208,top:0,bottom:60},
      {left:0,right:100,top:76,bottom:136},
    ];
    const start={x:30,y:29},end={x:185,y:29};
    expect(cells.map(rect=>strokeOverlapsRect(start,end,rect,10))).toEqual([true,true,false]);
    expect(strokeOverlapsRect({x:20,y:100},{x:80,y:100},cells[0],10)).toBe(false);
    // The very same drag is the only active pointer, and each field clips its own coating.
    const html=renderToStaticMarkup(<ModernScratchcards
      data={{...previewScratchcards,ticket:validTicket}}
      loaded={true} embedded={true} language="de" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('data-testid="scratch-ticket-grid"');
    expect(html.match(/data-scratch-index=/g)).toHaveLength(9);
  });
  it('rejects invalid pixels rather than accidentally exposing the ticket',()=>{
    expect(clearedFraction(new Uint8ClampedArray(0),170,105)).toBe(0);
    expect(clearedFraction(new Uint8ClampedArray(16),0,5)).toBe(0);
    expect(clearedFraction(new Uint8ClampedArray(16),2,2,0)).toBe(0);
  });
  it('highlights precisely the triple on winning cards, never losing pairs',()=>{
    const symbols=[1,2,3,1,4,2,1,3,4];
    expect(winningCellIndexes(symbols,50)).toEqual([0,3,6]);
    expect(winningCellIndexes(symbols,0)).toEqual([]);
    expect(winningCellIndexes(symbols.slice(0,8),50)).toEqual([]);
    expect(winningCellIndexes([1,1,2,2,3,3,4,4,0],10)).toEqual([]);
  });
  it('offers an independently muted sound button and honors reduced motion',()=>{
    const html=renderToStaticMarkup(<ModernScratchcards data={previewScratchcards}
      loaded={true} embedded={false} language="de" motionEnabled={false}
      pending={null} notice={null} onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('data-motion="off"');
    expect(html).toContain('aria-pressed="true"');
    expect(html).toContain('Ton an');
  });
});

describe('multiple scratchcard presentations',()=>{
  it('renders the current game with navigation and the server-backed price',()=>{
    const html=renderToStaticMarkup(<ModernScratchcards data={previewScratchcards}
      loaded={true} embedded={true} language="en" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('Next ticket: Gold Rush');
    expect(html).toContain('BUY TICKET');
    expect(html).toContain('$20.00');
    expect(html).toContain('scratch-ticket-lucky-sevens');
  });
  it('restores a Gold Rush ticket and prevents simultaneous purchases',()=>{
    const gold={...validTicket,gameId:'gold-rush',prize:50,collected:false};
    const html=renderToStaticMarkup(<ModernScratchcards
      data={{...previewScratchcards,ticket:gold}} loaded={true} embedded={true}
      language="de" pending={null} notice={null} onBack={()=>{}}
      onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('GOLD');
    expect(html).toContain('RUSH');
    expect(html).toContain('scratch-ticket-gold-rush');
    expect(html).toContain('LOS KAUFEN');
    expect(html).toContain('$100.00');
    expect(html).toContain('disabled');
  });
  it('never reuses the previous receipt reveal state for a fresh ticket',()=>{
    const oldReceipt='d9c28b66-bfa7-47b8-9a80-d0c429deeffe';
    const newReceipt='ab13702a-2a5b-473c-8e48-bbe58fa068b0';
    const former={ticketId:oldReceipt,indexes:[0,1,2,3,4,5,6,7,8]};
    expect(revealedIndexesForTicket(oldReceipt,false,former)).toHaveLength(9);
    expect(revealedIndexesForTicket(newReceipt,false,former)).toEqual([]);
    expect(revealedIndexesForTicket(newReceipt,true,former)).toHaveLength(9);
    const nextTicket={...validTicket,ticketId:newReceipt,gameId:'gold-rush',prize:0};
    const html=renderToStaticMarkup(<ModernScratchcards
      data={{...previewScratchcards,ticket:nextTicket}}
      loaded={true} embedded={true} language="en" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(html).toContain('scratch-ticket-gold-rush');
    expect(html).not.toContain('scratch-odds-winner');
    expect(html).not.toContain('scratch-win-banner');
    expect(html).toContain('Previous ticket: Lucky Sevens');
  });
});

describe('premium ticket series and custom art',()=>{
  it('contains four valid server-priced tier definitions in sequence',()=>{
    const games=previewScratchcards.games;
    expect(games.map(game=>game.id)).toEqual([
      'lucky-sevens','gold-rush','platinum-vault','diamond-royale',
    ]);
    expect(games.map(game=>game.price)).toEqual([20,100,500,2500]);
    expect(games.map(game=>game.prizeMultiplier)).toEqual([1,5,25,125]);
    expect(isScratchcardSnapshot(previewScratchcards)).toBe(true);
    expect(isScratchcardSnapshot({...previewScratchcards,ticket:{
      ...validTicket,gameId:'diamond-royale',prize:125000,
    }})).toBe(true);
    expect(isScratchcardSnapshot({...previewScratchcards,ticket:{
      ...validTicket,gameId:'platinum-vault',prize:25000,
    }})).toBe(true);
  });
  it('has five distinct recognisable vector symbols for each premium tier',()=>{
    for(const gameId of ['gold-rush','platinum-vault','diamond-royale']){
      const labels=Array.from({length:5},(_,index)=>scratchSymbolLabel(gameId,index));
      expect(new Set(labels).size).toBe(5);
      for(let index=0;index<5;index++){
        const html=renderToStaticMarkup(<ScratchcardMotif gameId={gameId} symbol={index}/>);
        expect(html).toContain('<svg');
        expect(html).toContain('data-motif="'+labels[index]+'"');
        expect(html).toContain('<path');
      }
    }
    expect(renderToStaticMarkup(<ScratchcardMotif gameId="lucky-sevens" symbol={4}/>))
      .toContain('>7</span>');
  });
  it.each([
    ['platinum-vault','PLATINUM','VAULT','$500.00','$25,000.00',
      'Previous ticket: Gold Rush','Next ticket: Diamond Royale'],
    ['diamond-royale','DIAMOND','ROYALE','$2,500.00','$125,000.00',
      'Previous ticket: Platinum Vault',null],
  ])('shows %s premium artwork, payout table and navigational arrows',
    (id,top,bottom,price,jackpot,previous,next)=>{
      const ticket={...validTicket,gameId:id,prize:0};
      const html=renderToStaticMarkup(<ModernScratchcards
        data={{...previewScratchcards,ticket}} loaded={true} embedded={true}
        language="en" pending={null} notice={null}
        onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
      expect(html).toContain('scratch-ticket-'+id);
      expect(html).toContain(top);
      expect(html).toContain(bottom);
      expect(html).toContain(price);
      expect(html).toContain(jackpot);
      expect(html).toContain(previous);
      if(next)expect(html).toContain(next);
      else expect(html).not.toContain('Next ticket:');
      expect(html).toContain('scratch-symbol-preview');
    });
  it('keeps the base Lucky Sevens symbols and allows four themes in messages',()=>{
    expect(scratchSymbolLabel('lucky-sevens',0)).toBe('spade');
    expect(scratchSymbolLabel('gold-rush',0)).toBe('gold-ingot');
    expect(scratchSymbolLabel('platinum-vault',0)).toBe('platinum-ingot');
    expect(scratchSymbolLabel('diamond-royale',0)).toBe('cut-diamond');
    const first=renderToStaticMarkup(<ModernScratchcards data={previewScratchcards}
      loaded={true} embedded={true} language="en" pending={null} notice={null}
      onBack={()=>{}} onBuy={()=>{}} onCollect={()=>{}}/>);
    expect(first).toContain('Next ticket: Gold Rush');
    expect(first).toContain('scratch-ticket-lucky-sevens');
  });
});
