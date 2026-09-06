import {linePoints} from '../tools/line-tool.js';
import {rectanglePoints} from '../tools/rectangle-tool.js';
import {ellipsePoints} from '../tools/ellipse-tool.js';
import {floodPoints} from '../tools/fill-tool.js';
import {hitTest} from './selection-manager.js';
import {object} from '../project/project-model.js';
export const toolNames={select:'Selección',pencil:'Lápiz',eraser:'Borrador',line:'Línea',rectangle:'Rectángulo',ellipse:'Elipse',fill:'Relleno',text:'Texto',icon:'Icono',emoji:'Emoji',image:'Imagen'};
export class ToolManager {
 tool='select';gesture=null;
 constructor(state,viewport,refresh,insert){this.s=state;this.view=viewport;this.refresh=refresh;this.insert=insert;const c=viewport.canvas;c.addEventListener('pointerdown',e=>this.down(e));c.addEventListener('pointermove',e=>this.move(e));c.addEventListener('pointerup',e=>this.up(e));c.addEventListener('pointercancel',()=>{if(this.gesture){this.s.project=this.gesture.before;this.gesture=null;this.refresh();}});}
 activate(tool){if(['text','icon','emoji','image'].includes(tool)){this.insert(tool);return;}this.tool=tool;document.querySelectorAll('[data-tool]').forEach(b=>b.classList.toggle('active',b.dataset.tool===tool));}
 down(e){if(e.button!==0)return;const s=this.s,p=this.view.point(e),m=s.project.matrixConfiguration;if(p.x<0||p.y<0||p.x>=m.width||p.y>=m.height)return;this.view.canvas.setPointerCapture(e.pointerId);
 if(this.tool==='select'){const current=s.object;let hit=current&&Math.abs(p.x-(current.x+current.width))<=1&&Math.abs(p.y-(current.y+current.height))<=1?{layer:s.layer,object:current}:hitTest(s.frame.layers,p);if(hit){s.layerId=hit.layer.id;s.objectId=hit.object.id;this.gesture={start:p,before:structuredClone(s.project),original:structuredClone(hit.object),resize:Math.abs(p.x-(hit.object.x+hit.object.width))<=1&&Math.abs(p.y-(hit.object.y+hit.object.height))<=1};}else{s.objectId=null;this.gesture={start:p,before:structuredClone(s.project),box:true};}this.refresh();return;}
 if(s.layer.locked||!s.layer.visible)return;this.gesture={start:p,before:structuredClone(s.project),points:[p]};if(this.tool==='fill'){this.gesture.points=floodPoints(this.view.buffer,p);this.preview(p);}else this.preview(p);}
 move(e){if(!this.gesture)return;const p=this.view.point(e);if(this.tool==='select'){const g=this.gesture;if(g.box){g.end=p;return;}const o=this.s.object;if(g.resize){o.width=Math.max(1,p.x-g.original.x);o.height=Math.max(1,p.y-g.original.y);}else{o.x=g.original.x+p.x-g.start.x;o.y=g.original.y+p.y-g.start.y;}this.refresh(false);return;}if(this.tool==='pencil'||this.tool==='eraser')this.gesture.points.push(...linePoints(this.gesture.points.at(-1),p));this.preview(p);}
 preview(p){const g=this.gesture,s=this.s;let points=this.tool==='line'?linePoints(g.start,p):this.tool==='rectangle'?rectanglePoints(g.start,p):this.tool==='ellipse'?ellipsePoints(g.start,p):g.points;
 const scene=s.sceneId,index=s.frameIndex,layerId=s.layerId;s.project=structuredClone(g.before);s.sceneId=scene;s.frameIndex=index;s.layerId=layerId;
 if(this.tool==='eraser'){for(const o of s.layer.objects){const c=o.content;for(const point of points){const x=Math.floor((point.x-o.x)*(c.width||o.width)/o.width),y=Math.floor((point.y-o.y)*(c.height||o.height)/o.height);if(x<0||y<0||x>=(c.width||o.width)||y>=(c.height||o.height))continue;const i=y*(c.width||o.width)+x;if(c.mask)c.mask[i]=0;if(c.rgba)c.rgba[i*4+3]=0;}}}
 else{const m=s.project.matrixConfiguration,mask=new Array(m.width*m.height).fill(0);for(const q of points)if(q.x>=0&&q.y>=0&&q.x<m.width&&q.y<m.height)mask[q.y*m.width+q.x]=1;const o=object('Drawing',toolNames[this.tool],m.width,m.height,{width:m.width,height:m.height,mask},s.color);s.layer.objects.push(o);s.objectId=o.id;}this.refresh(false);}
 up(e){const g=this.gesture;if(!g)return;if(g.box){const end=g.end||g.start;const hit=this.s.frame.layers.flatMap(l=>l.objects.map(o=>({l,o}))).find(({l,o})=>!l.locked&&l.visible&&o.x>=Math.min(g.start.x,end.x)&&o.y>=Math.min(g.start.y,end.y)&&o.x+o.width<=Math.max(g.start.x,end.x)+1&&o.y+o.height<=Math.max(g.start.y,end.y)+1);if(hit){this.s.layerId=hit.l.id;this.s.objectId=hit.o.id;}}
 else if(JSON.stringify(g.before)!==JSON.stringify(this.s.project)){this.s.history.push(g.before);this.s.dirty.mark();}this.gesture=null;this.refresh();}
}
