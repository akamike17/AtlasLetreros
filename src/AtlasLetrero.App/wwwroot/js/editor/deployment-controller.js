import {duration,renderScene} from './scene-renderer.js';
import {FrameBuffer} from './framebuffer.js';
export async function hash(bytes){const digest=await crypto.subtle.digest('SHA-256',bytes);return Array.from(new Uint8Array(digest),b=>b.toString(16).padStart(2,'0')).join('');}
export const limit=8*1024*1024;
export class SimulatorTransport {
 constructor(){this.package=null;this.receivedHash=null;this.candidate=null;}
 async receive(bytes,expected){
  this.candidate=null;
  if(bytes.length>limit)throw new Error('La animación supera 64 MB.');
  const copy=bytes.slice(),actual=await hash(copy);
  if(actual!==expected)throw new Error('El checksum del paquete recibido no coincide.');
  const packet=JSON.parse(new TextDecoder().decode(copy));
  if(packet.version!==1||!Number.isInteger(packet.width)||packet.width<1||packet.width>256||!Number.isInteger(packet.height)||packet.height<1||packet.height>256||!Number.isInteger(packet.fps)||packet.fps<1||packet.fps>60||!Number.isFinite(packet.durationMs)||packet.durationMs<=0||!Array.isArray(packet.frames)||packet.frames.length!==Math.ceil(packet.durationMs*packet.fps/1000)||packet.frames.some(f=>!Array.isArray(f)||f.length!==packet.width*packet.height*4||f.some(v=>!Number.isInteger(v)||v<0||v>255)))throw new Error('El paquete del simulador no es válido.');
  this.candidate={packet,checksum:actual};return actual;
 }
 activate(checksum){
  if(!this.candidate||this.candidate.checksum!==checksum)throw new Error('No hay un paquete verificado para activar.');
  this.package=this.candidate.packet;this.receivedHash=checksum;this.candidate=null;
 }
 frameAt(time){const p=this.package;if(!p)return null;const frame=new FrameBuffer(p.width,p.height);const index=Math.max(0,Math.min(p.frames.length-1,Math.floor(time*p.fps/1000+1e-9)));frame.data.set(p.frames[index]);return frame;}
}

export class PhysicalTransport {
 constructor(){this.lastChecksum=null;}
 async receive(bytes,expected,signal){const packet=JSON.parse(new TextDecoder().decode(bytes));const response=await fetch('/api/devices/upload',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({checksum:expected,packet}),signal});const result=await response.json();if(!response.ok||result.checksum!==expected||result.verified!==true||result.activated===true)throw new Error(result.message||'El firmware no confirmó la verificación.');this.lastChecksum={checksum:result.checksum,candidateId:result.candidateId??expected};return result.checksum;}
 async activate(checksum,signal){if(this.lastChecksum?.checksum!==checksum)throw new Error('El ESP32 no confirmó el checksum verificado.');const response=await fetch('/api/devices/activate',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({candidateId:this.lastChecksum.candidateId,checksum}),signal});const result=await response.json();if(!response.ok||(result.checksum??checksum)!==checksum||result.activated!==true)throw new Error(result.message||'El firmware no confirmó la activación.');this.lastChecksum=null;}
}
export async function deploy(project,scene,transport,notify,signal,canActivate=()=>true){
 try{
  notify('Validando',0);signal?.throwIfAborted();
  const snapshot=structuredClone(project),sceneCopy=structuredClone(scene),{width,height,fps}=snapshot.matrix;
  const total=duration(sceneCopy),count=Math.ceil(total*fps/1000);
  if(!Number.isInteger(width)||width<1||width>256||!Number.isInteger(height)||height<1||height>256||!Number.isInteger(fps)||fps<1||fps>60||!Number.isFinite(total)||total<=0||!sceneCopy.frames.length||sceneCopy.frames.some(f=>!Number.isInteger(f.durationMs)||f.durationMs<20||f.durationMs>600000))throw new Error('Revisa la matriz, los FPS y las duraciones antes de enviar.');
  if(count*width*height*4>limit)throw new Error('La animación supera 64 MB. Reduce duración, resolución o FPS.');
  notify('Preparando',5);const frames=[];let encodedSize=256;
  for(let i=0;i<count;i++){
   signal?.throwIfAborted();const frame=Array.from(renderScene(snapshot,sceneCopy,i*1000/fps).data);
   encodedSize+=JSON.stringify(frame).length+1;
   if(encodedSize>limit)throw new Error('La animación supera 64 MB. Reduce duración, resolución o FPS.');
   frames.push(frame);
   if(i%12===0){notify('Preparando',5+Math.round(i/count*65));await new Promise(r=>setTimeout(r,0));}
  }
  const packet={version:1,width,height,fps,durationMs:total,frames};
  const bytes=new TextEncoder().encode(JSON.stringify(packet)),checksum=await hash(bytes);
  signal?.throwIfAborted();notify('Enviando',80);await new Promise(r=>setTimeout(r,0));
  signal?.throwIfAborted();notify('Verificando',92);const received=await transport.receive(bytes,checksum,signal);
  if(received!==checksum)throw new Error('Falló la verificación del simulador.');
  notify('Activando',98);signal?.throwIfAborted();
  if(!canActivate())throw new Error('El proyecto cambió durante el envío. Envía de nuevo.');
  await transport.activate(checksum,signal);
  return {checksum,frames:frames.length,bytes:bytes.length};
 }catch(error){transport.candidate=null;throw error;}
}
