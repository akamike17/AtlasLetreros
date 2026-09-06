export class FrameBuffer {
 constructor(width,height,data){this.width=width;this.height=height;this.data=data?new Uint8ClampedArray(data):new Uint8ClampedArray(width*height*4);}
 set(x,y,color,alpha=1){x=Math.round(x);y=Math.round(y);if(x<0||y<0||x>=this.width||y>=this.height)return;const i=(y*this.width+x)*4;const a=Math.max(0,Math.min(1,alpha));for(let c=0;c<3;c++)this.data[i+c]=Math.round(color[c]*a+this.data[i+c]*(1-a));this.data[i+3]=255;}
}
export function rgb(hex){return [1,3,5].map(i=>parseInt(hex.slice(i,i+2),16));}
export function checksum(bytes){let crc=0xffffffff;for(const b of bytes){crc^=b;for(let i=0;i<8;i++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}return (~crc)>>>0;}
