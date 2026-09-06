export function mapPixel(x,y,m){
 let w=m.width,h=m.height;
 if(m.flipX)x=w-1-x;if(m.flipY)y=h-1-y;
 switch(Number(m.rotation)){case 90:[x,y]=[h-1-y,x];[w,h]=[h,w];break;case 180:x=w-1-x;y=h-1-y;break;case 270:[x,y]=[y,w-1-x];[w,h]=[h,w];break;}
 if(m.origin.includes('R'))x=w-1-x;if(m.origin.includes('B'))y=h-1-y;
 let major=m.primaryDirection==='vertical'?x:y,minor=m.primaryDirection==='vertical'?y:x,stride=m.primaryDirection==='vertical'?h:w;
 if(m.wiringMode==='serpentine'&&major%2)minor=stride-1-minor;return major*stride+minor;
}
export function physicalBytes(buffer,m){const result=new Uint8Array(m.width*m.height*3),order=m.colorOrder||'RGB';for(let y=0;y<m.height;y++)for(let x=0;x<m.width;x++){const from=(y*m.width+x)*4,to=mapPixel(x,y,m)*3;for(let c=0;c<3;c++)result[to+c]=buffer.data[from+'RGB'.indexOf(order[c])];}return result;}
