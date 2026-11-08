import {afterEach,expect,it,vi} from 'vitest';
import {finishModeBoot} from '../src/boot/finishModeBoot';

afterEach(()=>vi.unstubAllGlobals());

it('keeps the startup mask until the committed UI signals readiness',()=>{
  const remove=vi.fn();
  const splash={dataset:{state:'loading'},remove};
  const getElementById=vi.fn(()=>splash);
  const pending:Array<()=>void>=[];
  vi.stubGlobal('document',{getElementById});
  vi.stubGlobal('window',{setTimeout:(callback:()=>void)=>{pending.push(callback);return 1;}});
  finishModeBoot();
  expect(getElementById).toHaveBeenCalledWith('mode-boot');
  expect(splash.dataset.state).toBe('ready');
  expect(remove).not.toHaveBeenCalled();
  finishModeBoot();
  expect(pending).toHaveLength(1);
  pending[0]();
  expect(remove).toHaveBeenCalledOnce();
});

it('is safe when the HTML boot mask is absent',()=>{
  vi.stubGlobal('document',{getElementById:()=>null});
  finishModeBoot();
});
