import { describe, expect, it } from 'vitest';
import { browserPreview } from '../src/bridge/protocol';
import { isFreshOwnSpin, visibleDuringSpin } from '../src/graphics/presentationGate';

describe('commit before display: do not spoil the slot result',()=>{
  it('hides bankroll, line wins and bonus unlocks until the visual stop',()=>{
    const previous={...browserPreview,revision:'51',bankroll:'$100.00',payout:'$0.00'};
    const committed={...previous,revision:'52',bankroll:'$4,000.00',payout:'$3,925.00',
      doors:3,scatters:4,feature:'Escape the Manor · choose your path',winningLines:12,
      run:{status:'Hunting',message:'You enter Library.',ghostClue:'',playerRoom:1,
        turn:1,exitTarget:250,current:'25×',banked:'5×',hasCandle:false,hasKey:false,
        hasCharm:false,passages:[],rooms:[]}};
    const concealed=visibleDuringSpin(previous,committed,true);
    expect(concealed).toBe(previous);
    expect(concealed.bankroll).toBe('$100.00');
    expect(concealed.doors).toBe(0);
    expect(concealed.run).toBeNull();
    expect(visibleDuringSpin(previous,committed,false)).toBe(committed);
  });
  it('starts visual reveals only for newly acknowledged own mutations',()=>{
    expect(isFreshOwnSpin('command-1','command-1','spin.paid','3','4')).toBe(true);
    expect(isFreshOwnSpin('response','recovering','spin.free','3','4')).toBe(true);
    expect(isFreshOwnSpin('response',null,'spin.paid','3','4')).toBe(false);
    expect(isFreshOwnSpin('command-1','command-1','spin.paid','4','4')).toBe(false);
    expect(isFreshOwnSpin('command-1','command-1','run.move','3','4')).toBe(false);
    expect(isFreshOwnSpin('unexpected','command-1','spin.paid','3','4')).toBe(false);
  });
});
