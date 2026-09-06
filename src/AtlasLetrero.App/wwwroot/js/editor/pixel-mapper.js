export function pixelIndex(x,y,m){let w=m.width,h=m.height;
 if(m.flipX)x=w-1-x;if(m.flipY)y=h-1-y;
 if(m.rotation===90){[x,y]=[h-1-y,x];[w,h]=[h,w];}else if(m.rotation===180){x=w-1-x;y=h-1-y;}else if(m.rotation===270){[x,y]=[y,w-1-x];[w,h]=[h,w];}
 if(m.origin.includes('R'))x=w-1-x;if(m.origin.includes('B'))y=h-1-y;
 if(m.direction==='vertical')return x*h+(m.wiring==='serpentine'&&x%2?h-1-y:y);
 return y*w+(m.wiring==='serpentine'&&y%2?w-1-x:x);
}
export function payload(frame,m){const result=new Uint8Array(frame.width*frame.height*3);for(let y=0;y<frame.height;y++)for(let x=0;x<frame.width;x++){const source=(y*frame.width+x)*4,target=pixelIndex(x,y,m)*3;for(let c=0;c<3;c++)result[target+c]=frame.data[source+'RGB'.indexOf(m.colorOrder[c])];}return result;}
