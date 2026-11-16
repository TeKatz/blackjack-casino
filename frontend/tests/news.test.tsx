import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernNews} from '../src/lobby/ModernNews';
import {fallbackLobby,isLobbySnapshot} from '../src/lobby/lobbyBridge';

describe('offline player news',()=>{
  const entries=fallbackLobby.news??[];
  it('validates bilingual native news and startup flag separately from preview status',()=>{
    const native={...fallbackLobby,nativePreview:true,showStartupNews:true,
      newsReleaseSpecific:true,installedVersion:'0.8.3-dev.123'};
    expect(isLobbySnapshot(native)).toBe(true);
    expect(isLobbySnapshot({...native,showStartupNews:'true'})).toBe(false);
    expect(isLobbySnapshot({...native,newsReleaseSpecific:'true'})).toBe(false);
    expect(isLobbySnapshot({...native,news:[{...entries[0],accent:'javascript:invalid'}]}))
      .toBe(false);
    expect(isLobbySnapshot({...native,news:[{...entries[0],description:'x'.repeat(281)}]}))
      .toBe(false);
    expect(isLobbySnapshot({...native,news:Array(7).fill(entries[0])})).toBe(false);
  });
  it('renders friendly German text with edition and a visible close action',()=>{
    const html=renderToStaticMarkup(<ModernNews entries={entries}
      installedVersion="0.8.3-dev.123" releaseSpecific={true}
      language="de" motionEnabled={true} onClose={()=>{}}/>);
    expect(html).toContain('Neu im Casino');
    expect(html).toContain('NEU IN DIESEM UPDATE');
    expect(html).toContain('Vier Rubbellos-Editionen');
    expect(html).toContain('0.8.3-dev.123');
    expect(html).toContain('ZUM CASINO');
    expect(html).not.toContain('src/foo.tsx');
    expect(html).toContain('casino-news-editorial');
  });
  it('shows release-independent highlights without suggesting this exact version changed',()=>{
    const html=renderToStaticMarkup(<ModernNews entries={entries}
      installedVersion="Preview" releaseSpecific={false}
      language="en" motionEnabled={false} onClose={()=>{}}/>);
    expect(html).toContain('CASINO HIGHLIGHTS');
    expect(html).toContain('What’s new');
    expect(html).toContain('ENTER CASINO');
    expect(html).toContain('data-motion="off"');
  });
});
