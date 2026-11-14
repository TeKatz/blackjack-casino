/**
 * Progressive 2x upscale of the existing three-plane atlas. GPU browsers often
 * bilinearly magnify the tiny 896px shipping texture on high-DPI monitors.
 * Resampling each plane in isolation prevents colour bleed at atlas seams.
 * This is a presentation fallback until a true source-resolution atlas ships.
 */
const source='/lobby/art/hero-parallax.avif';
let pending:Promise<string|null>|null=null;

export function heroAtlasPlaneCount():number{return 3;}
export function heroAtlasScale():number{return 2;}

function sharpen(image:ImageData,planeHeight:number) {
  const data=image.data;
  const original=new Uint8ClampedArray(data);
  const width=image.width,height=image.height;
  // Constrain 4-neighbour filtering to its own plate; transparent cutouts
  // do not borrow colour from adjacent plates or paint dark alpha fringes.
  for(let y=1;y<height-1;y++){
    if(y%planeHeight===0||y%planeHeight===planeHeight-1)continue;
    for(let x=1;x<width-1;x++){
      const i=(y*width+x)*4;
      if(original[i+3]<64)continue;
      const left=i-4,right=i+4,up=i-width*4,down=i+width*4;
      let count=0,s0=0,s1=0,s2=0;
      if(original[left+3]>=64){s0+=original[left];s1+=original[left+1];s2+=original[left+2];count++;}
      if(original[right+3]>=64){s0+=original[right];s1+=original[right+1];s2+=original[right+2];count++;}
      if(original[up+3]>=64){s0+=original[up];s1+=original[up+1];s2+=original[up+2];count++;}
      if(original[down+3]>=64){s0+=original[down];s1+=original[down+1];s2+=original[down+2];count++;}
      if(!count)continue;
      const gain=.42/count;
      data[i]=original[i]+gain*(count*original[i]-s0);
      data[i+1]=original[i+1]+gain*(count*original[i+1]-s1);
      data[i+2]=original[i+2]+gain*(count*original[i+2]-s2);
    }
  }
}

async function render():Promise<string|null>{
  if(typeof document==='undefined'||typeof window==='undefined')return null;
  if(!('createImageBitmap' in window))return null;
  try{
    const response=await fetch(source,{cache:'force-cache'});
    if(!response.ok)return null;
    const bitmap=await createImageBitmap(await response.blob());
    const count=heroAtlasPlaneCount();
    if(bitmap.width<2||bitmap.height%count!==0){bitmap.close();return null;}
    const ratio=heroAtlasScale(),plane=bitmap.height/count;
    const canvas=document.createElement('canvas');
    canvas.width=bitmap.width*ratio;
    canvas.height=bitmap.height*ratio;
    const context=canvas.getContext('2d',{willReadFrequently:true});
    if(!context){bitmap.close();return null;}
    context.imageSmoothingEnabled=true;
    context.imageSmoothingQuality='high';
    for(let i=0;i<count;i++)context.drawImage(bitmap,
      0,i*plane,bitmap.width,plane,
      0,i*plane*ratio,canvas.width,plane*ratio);
    bitmap.close();
    const pixels=context.getImageData(0,0,canvas.width,canvas.height);
    sharpen(pixels,plane*ratio);
    context.putImageData(pixels,0,0);
    const blob=await new Promise<Blob|null>(resolve=>
      canvas.toBlob(resolve,'image/webp',.94));
    canvas.width=canvas.height=0;
    return blob?URL.createObjectURL(blob):null;
  }catch{return null;}
}

/** At most one enhanced texture is generated in the lifetime of a document. */
export function enhancedHeroAtlas():Promise<string|null>{
  if(!pending)pending=render();
  return pending;
}
