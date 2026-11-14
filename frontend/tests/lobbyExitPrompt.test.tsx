import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ExitPrompt} from '../src/lobby/ExitPrompt';

describe('React lobby exit confirmation',()=>{
  it('renders the English prompt and two actions without creating a browser iframe',()=>{
    const html=renderToStaticMarkup(<ExitPrompt language="en" onStay={()=>{}} onExit={()=>{}}/>);
    expect(html).toContain('Leaving the club?');
    expect(html).toContain('Stay in the casino');
    expect(html).toContain('Exit casino');
    expect(html).toContain('casino-lobby-exit-modal');
    expect(html).not.toContain('<iframe');
  });
  it('preserves the German text and native dialog semantics',()=>{
    const html=renderToStaticMarkup(<ExitPrompt language="de" onStay={()=>{}} onExit={()=>{}}/>);
    expect(html).toContain('Den Club verlassen?');
    expect(html).toContain('Im Casino bleiben');
    expect(html).toContain('Casino beenden');
    expect(html).toContain('<dialog');
    expect(html).toContain('casino-exit-description');
  });
});
