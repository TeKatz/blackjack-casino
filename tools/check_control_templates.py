"""Fail if a casino dropdown can delegate its hit surface to Windows theme chrome."""
from pathlib import Path
import sys,xml.etree.ElementTree as ET
ns={'p':'http://schemas.microsoft.com/winfx/2006/xaml/presentation'}
errors=[];count=0
for path in Path('src/BlackjackCasino.Desktop').rglob('*.xaml'):
 root=ET.parse(path).getroot()
 for template in root.findall('.//p:ControlTemplate',ns):
  if template.get('TargetType')!='ComboBox':continue
  count+=1
  toggles=template.findall('.//p:ToggleButton',ns)
  if not toggles:errors.append(f'{path}: ComboBox template has no owned dropdown toggle')
  for toggle in toggles:
   if toggle.get('Style')!='{StaticResource CasinoDropDownHitArea}':errors.append(f'{path}: dropdown must use CasinoDropDownHitArea; default ToggleButton repaints blue/white on hover/disable')
shared=ET.parse('src/BlackjackCasino.Desktop/Presentation/CasinoControls.xaml').getroot()
key='{http://schemas.microsoft.com/winfx/2006/xaml}Key'
style=next((s for s in shared.findall('p:Style',ns) if s.get(key)=='CasinoDropDownHitArea'),None)
if style is None:errors.append('Shared dropdown hit-area style is missing')
else:
 templates=style.findall('.//p:ControlTemplate',ns)
 if len(templates)!=1 or templates[0].find('p:Border',ns) is None or templates[0].find('p:Border',ns).get('Background')!='Transparent':errors.append('Dropdown hit area must own a transparent Border template')
 if style.findall('.//p:Trigger',ns):errors.append('The shared hit area may not paint hover/disabled theme states')
if count<2:errors.append('Expected global and Manor dropdown contracts')
if errors:print('\n'.join(errors));sys.exit(1)
print(f'PASS: {count} dropdown templates share the theme-independent transparent hit surface.')

if '--self-test' in sys.argv:
 import shutil,tempfile,subprocess
 with tempfile.TemporaryDirectory() as folder:
  test=Path(folder)
  for source in Path('src/BlackjackCasino.Desktop').rglob('*.xaml'):
   destination=test/source;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,destination)
  unsafe=test/'src/BlackjackCasino.Desktop/Presentation/CasinoControls.xaml'
  unsafe.write_text(unsafe.read_text().replace('Style="{StaticResource CasinoDropDownHitArea}"','',1))
  result=subprocess.run([sys.executable,str(Path(__file__).resolve())],cwd=test,capture_output=True,text=True)
  if result.returncode==0:raise SystemExit('FAIL: regression guard accepted the original unsafe dropdown')
  print('PASS: negative self-test rejects the original missing-toggle-template regression.')
