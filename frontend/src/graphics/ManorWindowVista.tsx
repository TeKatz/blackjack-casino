import {useEffect,useRef} from 'react';

/**
 * Real-time WebGL fragment scene in the two Manor side windows. Everything
 * visible is shaded into GPU pixels: no SVG, HTML illustration or WPF drawing.
 * Only the two gutters are painted; the authoritative Pixi reels remain clear.
 */
const fragment="precision highp float;\nuniform vec2 u_resolution;\nuniform float u_time;\nfloat hash(float p){return fract(sin(p*127.1+3.17)*43758.5453);}\nfloat hash2(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}\nfloat noise2(vec2 p){\n vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);\n return mix(mix(hash2(i),hash2(i+vec2(1.,0.)),f.x),\n mix(hash2(i+vec2(0.,1.)),hash2(i+vec2(1.,1.)),f.x),f.y);\n}\nfloat fbm(vec2 p){\n float v=0.,a=.52;\n for(int i=0;i<4;i++){v+=noise2(p)*a;p=p*2.07+vec2(13.7,4.3);a*=.5;}\n return v;\n}\nfloat softDisc(vec2 p,vec2 c,float r){\n return 1.-smoothstep(r*.55,r*1.75,length(p-c));\n}\nfloat pineEdge(vec2 p,float n,float seed,float base){\n float cell=floor(p.x*n);\n float x=abs(fract(p.x*n)-.5)*2.;\n float h=.09+hash(cell+seed)*.15;\n float crown=base-h+pow(x,.73)*h;\n float trunk=base-.045;\n return 1.-smoothstep(min(crown,trunk)-.003,min(crown,trunk)+.004,p.y);\n}\nvec3 landscape(vec2 p,float side,float t){\n float wind=t*.0009;\n vec3 sky=mix(vec3(.012,.024,.050),vec3(.050,.073,.092),clamp(p.y*.94,0.,1.));\n float cloud=fbm(vec2(p.x*3.4+wind+side*8.,p.y*6.7+t*.00035));\n float clouds=smoothstep(.46,.79,cloud);\n sky=mix(sky,vec3(.085,.112,.127),clouds*.28);\n float moon=softDisc(p,vec2(side<.5?.69:.31,.20),.043);\n float halo=softDisc(p,vec2(side<.5?.69:.31,.20),.12);\n sky+=vec3(.15,.17,.14)*halo*.17*(1.-clouds*.58);\n sky=mix(sky,vec3(.42,.46,.40),moon*.59*(1.-clouds*.5));\n vec2 stars=floor(p*vec2(42.,30.));\n float star=step(.966,hash2(stars+vec2(side*18.,2.)));\n float point=1.-smoothstep(.025,.085,length(fract(p*vec2(42.,30.))-.5));\n sky+=star*point*vec3(.34,.44,.53)*(.6+.4*sin(t*.001+stars.x));\n float far=.48+.025*sin(p.x*7.6+side)+.022*noise2(vec2(p.x*9.,side+6.));\n sky=mix(sky,vec3(.033,.064,.070),smoothstep(far-.008,far+.014,p.y));\n float middle=.65+.033*sin(p.x*13.+side*7.)+.02*noise2(vec2(p.x*18.,side));\n sky=mix(sky,vec3(.017,.042,.049),smoothstep(middle-.01,middle+.015,p.y));\n float treeline=pineEdge(p,31.,side*16.,.76);\n sky=mix(sky,vec3(.012,.027,.035),treeline*.94);\n float closest=pineEdge(p,22.,side*9.+1.,.88);\n sky=mix(sky,vec3(.005,.014,.024),closest);\n float fog=noise2(vec2(p.x*2.+t*.00042,p.y*6.7+side*7.));\n float bank=exp(-pow((p.y-.57)/.15,2.));\n sky=mix(sky,vec3(.135,.193,.194),fog*bank*.16);\n // Only distant, barely visible exterior events: a passing lantern, slow\n // wisps of mist and very occasional fireflies. No strobes or jump scares.\n float path=fract(t/57000.+side*.41);\n vec2 lightPos=vec2(-.12+path*1.24,.78+sin(path*5.)*.018);\n float lamps=softDisc(p,lightPos,.018)*step(.58,p.y)*.42;\n sky+=vec3(.21,.14,.055)*lamps;\n float fireIndex=floor(t/2400.);\n vec2 firePos=vec2(hash(fireIndex+13.+side*7.)*.9+.05,\n  .54+hash(fireIndex+33.+side*9.)*.27);\n float firePulse=pow(max(0.,sin(t*.002+side*4.)),12.);\n sky+=vec3(.11,.16,.115)*softDisc(p,firePos,.018)*firePulse;\n // A fine, slow pane-rain texture on the right, uneven drizzle on the left.\n vec2 drizzle=vec2(p.x*48.,p.y*14.+t*(side<.5?.0018:.0012));\n vec2 rainCell=floor(drizzle),rainLocal=fract(drizzle);\n float ra=step(.965,hash2(rainCell+vec2(side*9.,4.)));\n float streak=(1.-smoothstep(.025,.08,abs(rainLocal.x-.5)))*\n   smoothstep(.03,.30,rainLocal.y)*(1.-smoothstep(.48,.92,rainLocal.y));\n sky+=ra*streak*vec3(.085,.13,.16)*.20;\n return sky;\n}\nvoid main(){\n vec2 frag=gl_FragCoord.xy;\n float width=u_resolution.x,height=u_resolution.y;\n float gutter=max(0.,(width-min(width,height*1.92))*.5);\n gutter=min(gutter,width*.29);\n if(gutter<20.|| (frag.x>=gutter && frag.x<=width-gutter)){\n  gl_FragColor=vec4(0.);return;\n }\n float side=frag.x<gutter?.0:1.;\n float x=(side<.5?frag.x:width-frag.x)/gutter;\n float y=1.-frag.y/height;\n float gr=noise2(vec2(x*26.+side*7.,y*46.));\n vec3 wall=mix(vec3(.038,.047,.060),vec3(.112,.108,.113),gr*.5+.19);\n float edge=max(0.,1.-abs(x-.5)*1.8);\n wall*=.78+edge*.18;\n // A tall, arched opening in dark masonry, with deeply recessed brass trim.\n float arch=.145+.075*pow(abs(x-.5)/.36,2.);\n float outer=step(.105,x)*step(x,.895)*step(arch,y)*step(y,.956);\n float innerArch=.18+.085*pow(abs(x-.5)/.29,2.);\n float inner=step(.18,x)*step(x,.82)*step(innerArch,y)*step(y,.91);\n vec3 color=wall;\n if(outer>.5){\n  color=mix(vec3(.090,.094,.103),vec3(.19,.17,.15),.21+gr*.24);\n  float frameEdge=1.-smoothstep(0.,.06,min(min(x-.105,.895-x),min(y-arch,.956-y)));\n  color+=vec3(.14,.115,.073)*frameEdge;\n  float trim=1.-smoothstep(.008,.027,\n   min(min(x-.15,.85-x),min(y-(arch+.012),.93-y)));\n  color=mix(color,vec3(.23,.195,.138),trim*.66);\n }\n if(inner>.5){\n  vec2 p=vec2((x-.18)/.64,(y-.18)/.73);\n  color=landscape(p,side,u_time);\n  float mullion=1.-smoothstep(.003,.014,abs(x-.5));\n  float crossbar=1.-smoothstep(.001,.010,abs(y-.555));\n  float frame=max(mullion*.88,crossbar*.72);\n  color=mix(color,vec3(.13,.135,.137),frame);\n  float glassEdge=1.-smoothstep(0.,.035,min(min(x-.18,.82-x),min(y-innerArch,.91-y)));\n  color=mix(color,vec3(.21,.18,.135),glassEdge*.46);\n  float reflect=pow(max(0.,1.-abs(x-(.23+y*.095))/.045),2.)*\n   (1.-smoothstep(.2,.85,y));\n  color+=reflect*vec3(.11,.125,.12)*.065;\n }\n // Fine vignette and physically dark inside corner edges.\n color*=.86+.14*edge;\n gl_FragColor=vec4(color,1.);\n}\n";
const vertex=`attribute vec2 a_position;
void main(){gl_Position=vec4(a_position,0.,1.);}`;

function compile(gl:WebGLRenderingContext,type:number,source:string):WebGLShader{
 const shader=gl.createShader(type);
 if(!shader)throw new Error('WebGL shader allocation failed');
 gl.shaderSource(shader,source);gl.compileShader(shader);
 if(!gl.getShaderParameter(shader,gl.COMPILE_STATUS)){
  const issue=gl.getShaderInfoLog(shader);
  gl.deleteShader(shader);
  throw new Error('Manor window shader compilation failed: '+issue);
 }
 return shader;
}

export function ManorWindowVista({animate}:{animate:boolean}){
 const ref=useRef<HTMLCanvasElement>(null);
 const animateRef=useRef(animate);
 animateRef.current=animate;
 useEffect(()=>{
  const canvas=ref.current;if(!canvas)return;
  const gl=canvas.getContext('webgl',{
   alpha:true,antialias:false,premultipliedAlpha:false,
   depth:false,stencil:false,preserveDrawingBuffer:false,powerPreference:'low-power',
  });
  if(!gl)return;
  let program:WebGLProgram|null=null;
  let buffer:WebGLBuffer|null=null;
  let frame=0,last=0,lastAnimated=0;
  let lost=false;
  let observer:ResizeObserver|null=null;
  try{
   const vs=compile(gl,gl.VERTEX_SHADER,vertex);
   const fs=compile(gl,gl.FRAGMENT_SHADER,fragment);
   program=gl.createProgram();
   if(!program)throw new Error('Manor shader program unavailable');
   gl.attachShader(program,vs);gl.attachShader(program,fs);gl.linkProgram(program);
   gl.deleteShader(vs);gl.deleteShader(fs);
   if(!gl.getProgramParameter(program,gl.LINK_STATUS))
    throw new Error('Manor windows shader link failed: '+gl.getProgramInfoLog(program));
   gl.useProgram(program);
   buffer=gl.createBuffer();
   gl.bindBuffer(gl.ARRAY_BUFFER,buffer);
   gl.bufferData(gl.ARRAY_BUFFER,
    new Float32Array([-1,-1,1,-1,-1,1,-1,1,1,-1,1,1]),gl.STATIC_DRAW);
   const attrib=gl.getAttribLocation(program,'a_position');
   gl.enableVertexAttribArray(attrib);
   gl.vertexAttribPointer(attrib,2,gl.FLOAT,false,0,0);
   const uSize=gl.getUniformLocation(program,'u_resolution');
   const uTime=gl.getUniformLocation(program,'u_time');
   const resize=()=>{
    const parent=canvas.parentElement;if(!parent)return;
    const r=parent.getBoundingClientRect();
    // A single low-resolution shader surface for both gutters; do not run
    // two competing GPU contexts behind the Pixi slot.
    const scale=Math.min(window.devicePixelRatio||1,.88);
    const w=Math.max(1,Math.ceil(r.width*scale));
    const h=Math.max(1,Math.ceil(r.height*scale));
    if(canvas.width===w&&canvas.height===h)return;
    canvas.width=w;canvas.height=h;gl.viewport(0,0,w,h);last=0;
   };
   observer=new ResizeObserver(resize);
   if(canvas.parentElement)observer.observe(canvas.parentElement);
   resize();
   const tick=(now:number)=>{
    frame=requestAnimationFrame(tick);
    if(document.hidden||lost)return;
    const motion=animateRef.current;
    if(last&&now-last<(motion?50:1000))return; // 20fps maximum
    last=now;
    if(motion)lastAnimated=now;
    gl.clearColor(0,0,0,0);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.useProgram(program);
    gl.uniform2f(uSize,canvas.width,canvas.height);
    gl.uniform1f(uTime,motion?now:lastAnimated);
    gl.drawArrays(gl.TRIANGLES,0,6);
   };
   const onLost=(e:Event)=>{e.preventDefault();lost=true;};
   canvas.addEventListener('webglcontextlost',onLost);
   frame=requestAnimationFrame(tick);
   return()=>{
    cancelAnimationFrame(frame);observer?.disconnect();
    canvas.removeEventListener('webglcontextlost',onLost);
    if(buffer)gl.deleteBuffer(buffer);
    if(program)gl.deleteProgram(program);
   };
  }catch{
   observer?.disconnect();
   if(buffer)gl.deleteBuffer(buffer);
   if(program)gl.deleteProgram(program);
  }
 },[]);
 return <canvas className="manor-window-vista" ref={ref}
  aria-hidden="true" data-testid="manor-window-vista"/>;
}
