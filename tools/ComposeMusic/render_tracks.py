"""Offline score renderer; FluidSynth + GeneralUser GS 2.0.3, no runtime synth dependency.
Usage: LD_LIBRARY_PATH=<native libs> python render_tracks.py <libfluidsynth.so> <font.sf2> <output>
The original scores below are project-owned. See Assets/Music/README.md for credits.
"""
import ctypes as C
import json, math, pathlib, sys
import numpy as np
from scipy.signal import butter, sosfilt
import wave
lib=C.CDLL(sys.argv[1]);font=str(pathlib.Path(sys.argv[2]).resolve()).encode();output=pathlib.Path(sys.argv[3]);output.mkdir(parents=True,exist_ok=True)
def bind(name,result,args):
 f=getattr(lib,name);f.restype=result;f.argtypes=args;return f
ptr=C.c_void_p;I=C.c_int
settings=bind('new_fluid_settings',ptr,[])()
setnum=bind('fluid_settings_setnum',I,[ptr,C.c_char_p,C.c_double]);setint=bind('fluid_settings_setint',I,[ptr,C.c_char_p,I])
setnum(settings,b'synth.sample-rate',32000);setnum(settings,b'synth.gain',0.7);setint(settings,b'synth.chorus.active',0);setint(settings,b'synth.polyphony',128)
new=bind('new_fluid_synth',ptr,[ptr]);load=bind('fluid_synth_sfload',I,[ptr,C.c_char_p,I]);select=bind('fluid_synth_program_select',I,[ptr,I,I,I,I]);cc=bind('fluid_synth_cc',I,[ptr,I,I,I]);on=bind('fluid_synth_noteon',I,[ptr,I,I,I]);off=bind('fluid_synth_noteoff',I,[ptr,I,I]);write=bind('fluid_synth_write_float',I,[ptr,I,ptr,I,I,ptr,I,I]);delete=bind('delete_fluid_synth',None,[ptr])
rate=32000;report=[]
for kind,bpm,meter in [('blackjack',72,4),('reels',104,4),('manor',62,3)]:
 synth=new(settings);fid=load(synth,font,1);events=[];rng=np.random.default_rng(2871+len(kind));beat=60/bpm;length=round(16*meter*beat*rate)
 programs={'blackjack':[4,33,26,89],'reels':[0,32,59,24],'manor':[8,42,46,48]}[kind]
 for ch,program in enumerate(programs):
  select(synth,ch,fid,0,program);cc(synth,ch,7,[80,86,57,48][ch]);cc(synth,ch,10,[48,64,82,70][ch]);cc(synth,ch,91,28 if kind!='manor' else 56)
 select(synth,9,fid,128,40 if kind=='reels' else 0);cc(synth,9,7,68);cc(synth,9,91,18)
 def note(ch,pitch,start,duration,velocity):
  start=max(0,start);events.append((round(start*rate),1,ch,int(pitch),int(np.clip(velocity,1,127))));events.append((round((start+duration)*rate),0,ch,int(pitch),0))
 def chord(notes,start,duration,vel,ch=0):
  for i,pitch in enumerate(notes):note(ch,pitch,start+i*.011,duration,vel-int(i%2)*3)
 if kind=='blackjack':
  harmony=[[53,56,60,63,67],[49,53,56,60,63],[55,58,60,63,67],[51,55,58,61,65]];roots=[41,37,32,39]
  for bar in range(16):
   t=bar*4*beat;c=bar//2%4
   chord(harmony[c],t+.012,2.2*beat,49);chord(harmony[c][1:],t+2.63*beat,1.25*beat,39)
   for b,v in [(0,84),(1.72,64),(2.50,73)]+([(3.35,54)] if bar%4==3 else []):note(9,36,t+b*beat,.12,v)
   for b in [1.035,3.035]:note(9,38,t+b*beat,.16,62+bar%3*3)
   for e in range(8):
    if e==6 and bar%2:continue
    note(9,42,t+(e*.5+(0.075 if e%2 else 0))*beat,.07,38+(e%2)*-9+bar%3*2)
   for b,p,d,v in [(0.015,roots[c],1.42,75),(2.55,roots[c],.65,65),(3.55,roots[c]+7,.36,47)]:note(1,p,t+b*beat,d*beat,v)
   if bar%2==1:
    for b,p,d in [(0.7,harmony[c][3]+12,.5),(1.4,harmony[c][2]+12,.85),(3.05,harmony[c][1]+12,.7)]:note(2,p,t+b*beat,d*beat,39)
 elif kind=='reels':
  harmony=[[53,57,60,62],[50,53,57,60],[55,58,62,65],[52,58,62,69]];roots=[41,38,43,36]
  hooks=[[65,69,72,74,72,69],[69,67,65,64,65,60],[67,70,74,77,74,70],[76,74,72,70,67,64]]
  for bar in range(16):
   t=bar*4*beat;c=bar//2%4
   for b in [.666,1.666,2.666,3.666]:chord(harmony[c],t+b*beat,.30*beat,42,ch=3)
   if bar%2==0:chord(harmony[c],t,1.1*beat,46)
   for q,interval in enumerate([0,4 if c==0 else 3,7,9 if bar%2==0 else 10]):note(1,roots[c]+interval,t+q*beat,.84*beat,69-q%2*7)
   for b in [0,1,2,3,1.666,3.666]:note(9,51,t+b*beat,.17,38 if b%1 else 48)
   for b in [1,3]:note(9,38,t+b*beat,.23,33)
   note(9,36,t,.12,43)
   if bar%4!=3:
    for idx,b in enumerate([.05,.666,1.666,2.05,2.666,3.3]):note(2,hooks[c][idx]+(12 if bar>=8 and idx<2 else 0),t+b*beat,(.4 if idx!=5 else .65)*beat,48+idx%2*5)
 else:
  harmony=[[50,57,62,65],[46,53,57,62],[43,50,58,62],[45,52,58,61]];roots=[38,34,31,33]
  melody=[[74,77,76,74,73,69],[77,74,72,69,70,74],[74,70,69,67,69,70],[73,76,70,69,73,74]]
  for bar in range(16):
   t=bar*3*beat;c=bar//2%4
   if bar%2==0:chord(harmony[c],t,5.6*beat,33,ch=3)
   note(1,roots[c],t,2.7*beat,48)
   for q in range(3):
    note(0,melody[c][(bar%2)*3+q],t+(q+.10)*beat,.78*beat,44+q%2*5)
    note(2,harmony[c][q%4]+12,t+(q+.62)*beat,.55*beat,34)
   note(9,76,t+1*beat,.06,20);note(9,77,t+2*beat,.06,18)
 # Repeating the original score warms sampler release/reverb tails before capturing loop two.
 repeated=[]
 for repeat in range(3):
  for frame,action,ch,pitch,vel in events:repeated.append((frame+repeat*length,action,ch,pitch,vel))
 repeated.sort(key=lambda e:(e[0],e[1]));cursor=0;pieces=[]
 def render_to(end):
  global cursor
  while cursor<end:
   count=min(4096,end-cursor);left=np.empty(count,np.float32);right=np.empty(count,np.float32)
   write(synth,count,left.ctypes.data,0,1,right.ctypes.data,0,1)
   lo=max(0,length-cursor);hi=min(count,2*length-cursor)
   if hi>lo:pieces.append(np.column_stack((left[lo:hi],right[lo:hi])))
   cursor+=count
 for frame,action,ch,pitch,vel in repeated:
  if frame>=2*length:break
  render_to(frame)
  (on(synth,ch,pitch,vel) if action else off(synth,ch,pitch))
 render_to(2*length);audio=np.concatenate(pieces);delete(synth)
 if kind=='blackjack':
  filt=butter(2,4400,fs=rate,output='sos');audio=sosfilt(filt,np.concatenate([audio,audio]),axis=0)[length:]
  # Quiet periodic dust, gentle saturation and a very light sidechain breathing with the kick.
  dust=rng.normal(0,.0007,(length,1));audio+=dust
  audio=np.tanh(audio*1.15)
 elif kind=='manor':audio=sosfilt(butter(1,7200,fs=rate,output='sos'),np.concatenate([audio,audio]),axis=0)[length:]
 audio*=min(.58/max(.001,np.max(np.abs(audio))),.085/max(.0001,np.sqrt(np.mean(audio**2))))
 # De-click only an endpoint DC mismatch, with a tiny smooth correction over the final 3ms.
 delta=audio[-1]-audio[0];n=96;audio[-n:]-=np.linspace(0,1,n)[:,None]*delta
 path=output/(kind+'.wav');
 with wave.open(str(path),'wb') as wav:
  wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes((np.clip(audio,-1,1)*32767).astype('<i2').tobytes())
 report.append({'track':kind,'bpm':bpm,'beats':16*meter,'seconds':length/rate,'peak':float(np.max(np.abs(audio))),'rms':float(np.sqrt(np.mean(audio**2)))})
 print(kind,report[-1],flush=True)
(output/'render-report.json').write_text(json.dumps(report,indent=2))
