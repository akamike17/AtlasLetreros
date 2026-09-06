import {CanvasViewport} from './canvas-viewport.js';
import {FrameBuffer,checksum} from './framebuffer.js';
export class SimulatorView extends CanvasViewport {
 constructor(canvas){super(canvas,true);this.received=null;}
 async receive(packet){const transferred=structuredClone(packet);await new Promise(resolve=>requestAnimationFrame(resolve));for(const f of transferred.frames){if(f.data.length!==transferred.width*transferred.height*4||checksum(f.data)!==f.checksum)throw new Error('El simulador recibió un frame dañado.');}if(!transferred.frames.length)throw new Error('El paquete no contiene frames.');this.received=transferred;const first=transferred.frames[0];this.draw(new FrameBuffer(transferred.width,transferred.height,first.data));return {checksum:checksum(new Uint8Array(transferred.frames.flatMap(f=>f.data))),frames:transferred.frames.length};}
 drawReceived(time){if(!this.received)return;const p=this.received,index=Math.min(p.frames.length-1,Math.floor(time/p.sampleMs));this.draw(new FrameBuffer(p.width,p.height,p.frames[index].data));}
}
