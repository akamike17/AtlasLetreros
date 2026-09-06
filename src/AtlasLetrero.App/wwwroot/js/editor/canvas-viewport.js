export class CanvasViewport {
 constructor(canvas,dots=false){this.canvas=canvas;this.dots=dots;this.zoom=1;this.grid=true;this.observer=new ResizeObserver(()=>{if(this.buffer)this.draw(this.buffer);});this.observer.observe(canvas.parentElement);}
 draw(buffer,selected){this.buffer=buffer;const c=this.canvas,rect=c.parentElement.getBoundingClientRect(),dpr=devicePixelRatio||1;c.width=Math.max(1,Math.floor(rect.width*dpr));c.height=Math.max(1,Math.floor(rect.height*dpr));const ctx=c.getContext('2d');ctx.scale(dpr,dpr);ctx.fillStyle='#080e19';ctx.fillRect(0,0,rect.width,rect.height);
 const cell=Math.min((rect.width-24)/buffer.width,(rect.height-24)/buffer.height)*this.zoom,ox=(rect.width-cell*buffer.width)/2,oy=(rect.height-cell*buffer.height)/2;this.transform={cell,ox,oy};
 for(let y=0;y<buffer.height;y++)for(let x=0;x<buffer.width;x++){const i=(y*buffer.width+x)*4;const [r,g,b]=buffer.data.slice(i,i+3);ctx.fillStyle=r+g+b?`rgb(${r},${g},${b})`:'#142133';if(this.dots){ctx.beginPath();ctx.arc(ox+(x+.5)*cell,oy+(y+.5)*cell,Math.max(.3,cell*.37),0,Math.PI*2);ctx.fill();}else ctx.fillRect(ox+x*cell,oy+y*cell,Math.max(.5,cell-(this.grid&&cell>4?1:0)),Math.max(.5,cell-(this.grid&&cell>4?1:0)));}
 if(selected){ctx.strokeStyle='#ffffff';ctx.lineWidth=1;ctx.setLineDash([4,3]);ctx.strokeRect(ox+selected.x*cell,oy+selected.y*cell,selected.width*cell,selected.height*cell);ctx.setLineDash([]);ctx.fillStyle='#fff';ctx.fillRect(ox+(selected.x+selected.width)*cell-4,oy+(selected.y+selected.height)*cell-4,8,8);}
 }
 point(e){const r=this.canvas.getBoundingClientRect(),{ox,oy,cell}=this.transform;return {x:Math.floor((e.clientX-r.left-ox)/cell),y:Math.floor((e.clientY-r.top-oy)/cell)};}
}
