export class FrameBuffer{
 constructor(width,height){this.width=width;this.height=height;this.data=new Uint8ClampedArray(width*height*4);for(let i=3;i<this.data.length;i+=4)this.data[i]=255;}
 pixel(x,y,color,alpha=1){x=Math.round(x);y=Math.round(y);if(x<0||y<0||x>=this.width||y>=this.height)return;const i=(y*this.width+x)*4;for(let c=0;c<3;c++)this.data[i+c]=Math.round(color[c]*alpha+this.data[i+c]*(1-alpha));}
}
export function rgb(hex){return [1,3,5].map(i=>parseInt(hex.slice(i,i+2),16));}
