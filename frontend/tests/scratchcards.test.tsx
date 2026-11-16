import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernScratchcards} from '../src/lobby/ModernScratchcards';
import {isScratchcardMessage,isScratchcardSnapshot,previewScratchcards} from '../src/lobby/scratchcardsBridge';
import {clearedFraction,SCRATCH_BRUSH_WIDTH,SCRATCH_REVEAL_COVERAGE,winningCellIndexes} from '../src/lobby/scratchcardsPresentation';

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

describe('scratchcard visual-only reveal mechanics',()=>{
  it('uses a narrow brush and a majority-of-surface threshold',()=>{
    expect(SCRATCH_BRUSH_WIDTH).toBeLessThan(38);
    expect(SCRATCH_REVEAL_COVERAGE).toBeGreaterThan(.6);
    const width=20,height=20,step=2;
    const pixels=new Uint8ClampedArray(width*height*4);
    for(let i=3;i<pixels.length;i+=4)pixels[i]=255;
    // Repeated scrubbing of the same narrow strip cannot reveal the cell.
    for(let y=0;y<height;y++)for(let x=0;x<6;x++)
      pixels[(y*width+x)*4+3]=0;
    expect(clearedFraction(pixels,width,height,step)).toBeCloseTo(.3);
    expect(clearedFraction(pixels,width,height,step)).toBeLessThan(SCRATCH_REVEAL_COVERAGE);
    for(let y=0;y<height;y++)for(let x=6;x<14;x++)
      pixels[(y*width+x)*4+3]=0;
    expect(clearedFraction(pixels,width,height,step)).toBeCloseTo(.7);
    expect(clearedFraction(pixels,width,height,step)).toBeGreaterThan(SCRATCH_REVEAL_COVERAGE);
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
