import {FrameBuffer,rgb} from './framebuffer.js';
import {frameAt} from './playback-clock.js';
export function effectState(o,time,m){let x=o.x,y=o.y,alpha=o.opacity;const sec=time/1000,speed=o.effect?.speed||12,t=o.effect?.type;
 if(t==='blink')alpha*=Math.floor(sec*speed/6)%2===0?1:0;
 if(t==='marquee-left')x=((o.x-sec*speed)%(m.width+o.width)+(m.width+o.width))%(m.width+o.width)-o.width;
 if(t==='marquee-right')x=((o.x+sec*speed)%(m.width+o.width))-o.width;
 if(t==='slide')x=o.x-m.width*Math.max(0,1-sec*speed/12);
 if(t==='fade')alpha*=Math.min(1,sec*speed/12);
 if(t==='appear')alpha*=sec>=12/speed?1:0;
 if(t==='disappear')alpha*=sec<12/speed?1:0;
 if(t==='pulse')alpha*=.5+.5*Math.sin(sec*speed/3);
 return {x:Math.round(x),y:Math.round(y),alpha};
}
export function renderScene(project,scene,time){const m=project.matrixConfiguration,b=new FrameBuffer(m.width,m.height),f=frameAt(scene,time).frame;
 for(const layer of [...f.layers].sort((a,b)=>a.order-b.order)){if(!layer.visible)continue;for(const o of layer.objects){if(!o.visible)continue;const e=effectState(o,time,m),content=o.content,color=rgb(o.style.color||'#ffffff'),sw=content.width||o.width,sh=content.height||o.height;
 const angle=(o.rotation||0)*Math.PI/180,cos=Math.cos(angle),sin=Math.sin(angle),cx=o.width/2,cy=o.height/2;
 for(let yy=0;yy<o.height;yy++)for(let xx=0;xx<o.width;xx++){const sx=Math.min(sw-1,Math.floor(xx*sw/o.width)),sy=Math.min(sh-1,Math.floor(yy*sh/o.height)),i=sy*sw+sx;let c=color,a=e.alpha*layer.opacity;
 if(content.rgba){c=content.rgba.slice(i*4,i*4+3);a*=content.rgba[i*4+3]/255;}else if(!content.mask?.[i])continue;
 b.set(e.x+Math.round((xx-cx)*cos-(yy-cy)*sin+cx),e.y+Math.round((xx-cx)*sin+(yy-cy)*cos+cy),c,a);
 }} }
 const brightness=m.brightnessLimit/100;for(let i=0;i<b.data.length;i+=4){b.data[i]*=brightness;b.data[i+1]*=brightness;b.data[i+2]*=brightness;b.data[i+3]=255;}return b;
}
