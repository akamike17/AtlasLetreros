import assert from 'node:assert/strict';
import fs from 'node:fs';
import {webcrypto} from 'node:crypto';
import {renderScene} from '../../src/AtlasLetrero.App/wwwroot/js/editor/scene-renderer.js';
import {rasterText} from '../../src/AtlasLetrero.App/wwwroot/js/assets/bitmap-font.js';
import {SimulatorTransport,PhysicalTransport,deploy,hash} from '../../src/AtlasLetrero.App/wwwroot/js/editor/deployment-controller.js';
import {SaveCoordinator} from '../../src/AtlasLetrero.App/wwwroot/js/editor/save-coordinator.js';
import {validateText} from '../../src/AtlasLetrero.App/wwwroot/js/assets/text-validation.js';

const font=JSON.parse(fs.readFileSync(new URL('../../src/AtlasLetrero.App/wwwroot/assets/fonts/5x7.json',import.meta.url)));
globalThis.crypto ??= webcrypto;
const bitmap=rasterText('SE REPARAN COMPUTADORAS Ñ',font);
const matrix={width:32,height:16,fps:12,brightness:80};
assert.deepEqual(validateText('HOLA',font,2,1,matrix),{width:43,height:14});
assert.throws(()=>validateText('HOLA',font,3,1,matrix),/matriz tiene 16/);
assert.throws(()=>validateText('',font,1,1,matrix),/Escribe el texto/);
const base={x:0,y:0,width:bitmap.width,height:bitmap.height,visible:true,color:'#94ed55',opacity:1,effect:{type:'none',speed:1}};
function fixture(object){const scene={frames:[{durationMs:1000,layers:[{visible:true,opacity:1,objects:[object]}]}]};return {project:{matrix,scenes:[scene]},scene};}
function lit(frame){return frame.data.some((v,i)=>i%4!==3&&v>0);}
for(const object of [
 {...base,type:'text',bitmap},
 {...base,type:'icon',bitmap:{width:1,height:1,data:[1]},width:1,height:1},
 {...base,type:'image',bitmap:{width:1,height:1,rgba:true,data:[255,0,0,255]},width:1,height:1},
 {...base,type:'emoji',bitmap:{width:1,height:1,rgba:true,data:[255,255,0,255]},width:1,height:1},
 {...base,type:'drawing',points:[[0,0],[1,1]]},
 {...base,type:'line',x1:0,y1:0,x2:8,y2:8},
 {...base,type:'rect',width:8,height:8},
 {...base,type:'ellipse',width:8,height:8}
]){const {project,scene}=fixture(object);assert.ok(lit(renderScene(project,scene,0)),object.type+' debe encender píxeles');}
const {project,scene}=fixture({...base,type:'text',bitmap,effect:{type:'blink',speed:1}});
assert.ok(lit(renderScene(project,scene,0)));
assert.ok(!lit(renderScene(project,scene,600)));
const transport=new SimulatorTransport();await deploy(project,scene,transport,()=>{});
for(const time of [0,250,500,750])assert.deepEqual(transport.frameAt(time).data,renderScene(project,scene,time).data,'El paquete recibido debe coincidir con el render');
console.log('PASS: 8 tipos de objeto, texto bitmap español, parpadeo y transporte de simulador.');

const originalPackage=transport.package,originalHash=transport.receivedHash;
for(const phase of ['Preparando','Enviando','Verificando','Activando']){
 const abort=new AbortController();
 await assert.rejects(deploy(project,scene,transport,status=>{if(status===phase)abort.abort();},abort.signal),{name:'AbortError'});
 assert.equal(transport.package,originalPackage,'Cancelar en '+phase+' conserva el último envío');
 assert.equal(transport.receivedHash,originalHash);assert.equal(transport.candidate,null);
}
await assert.rejects(deploy(project,scene,transport,()=>{},undefined,()=>false),/cambió/);
assert.equal(transport.package,originalPackage);
const corrupt=new TextEncoder().encode(JSON.stringify(originalPackage));corrupt[0]^=1;
await assert.rejects(transport.receive(corrupt,originalHash),/checksum/);
const invalid=new TextEncoder().encode(JSON.stringify({...originalPackage,fps:0}));
await assert.rejects(transport.receive(invalid,await hash(invalid)),/no es válido/);
assert.equal(transport.package,originalPackage);
await assert.rejects(deploy({...project,matrix:{...matrix,fps:0}},scene,transport,()=>{}),/Revisa/);
for(const fps of [12,24,60]){
 const p={...project,matrix:{...matrix,fps}},s=structuredClone(scene);
 s.frames[0].layers[0].objects[0].effect={type:'left',speed:1};
 await deploy(p,s,transport,()=>{});
 for(let i=0;i<fps;i++)assert.deepEqual(transport.frameAt(i*1000/fps).data,renderScene(p,s,i*1000/fps).data,'Paridad de cada muestra a '+fps+' FPS');
}
const sequence=structuredClone(scene);
sequence.frames[0].durationMs=10000;
sequence.frames.push({...structuredClone(scene.frames[0]),durationMs:1000});
assert.ok(lit(renderScene(project,sequence,10000)),'El parpadeo comienza de nuevo al cambiar de frame');
console.log('PASS: cancelación en 4 fases, corrupción, revisión modificada, último envío válido y paridad 12/24/60 FPS.');

let release;const writes=[];
const state={project:{name:'Inicial'},revision:0,dirty:true};
const saves=new SaveCoordinator(state,async(document,recovery)=>{writes.push({document,recovery});if(recovery)await new Promise(r=>release=r);});
const autosave=saves.save(true);await new Promise(r=>setTimeout(r,0));
state.project.name='Último cambio';state.revision++;
const explicit=saves.save();assert.equal(writes.length,1);release();await Promise.all([autosave,explicit]);
assert.equal(writes[1].document.name,'Último cambio');assert.equal(writes[1].recovery,false);assert.equal(state.dirty,false);
let finish;state.dirty=true;
const delayed=new SaveCoordinator(state,()=>new Promise(r=>finish=r));
const saving=delayed.save();await new Promise(r=>setTimeout(r,0));state.revision++;finish();await saving;
assert.equal(state.dirty,true,'Un cambio durante el guardado continúa pendiente');
let fail=true;const retry=new SaveCoordinator(state,async()=>{if(fail)throw Error('Disco ocupado');});
await assert.rejects(retry.save(),/Disco ocupado/);assert.equal(state.dirty,true);fail=false;await retry.save();assert.equal(state.dirty,false);
console.log('PASS: guardado explícito espera autosave, conserva cambios concurrentes y permite reintentar errores.');

const originalFetch=globalThis.fetch;
globalThis.fetch=async()=>({ok:false,json:async()=>({message:'Desconexión durante transferencia'})});
await assert.rejects(new PhysicalTransport().receive(new TextEncoder().encode('{}'),'0'.repeat(64)),/Desconexión/);
globalThis.fetch=async()=>({ok:true,json:async()=>({checksum:'0'.repeat(64),verified:false,activated:false})});
await assert.rejects(new PhysicalTransport().receive(new TextEncoder().encode('{}'),'0'.repeat(64)),/verificación/);
globalThis.fetch=originalFetch;
const physicalProject=structuredClone(project),physicalScene=structuredClone(scene);
let activateCalls=0;
globalThis.fetch=async(url,options)=>{
 if(url==='/api/devices/upload')return {ok:true,json:async()=>({checksum:JSON.parse(options.body).checksum,verified:true,activated:false})};
 if(url==='/api/devices/activate'){activateCalls++;return {ok:true,json:async()=>({checksum:JSON.parse(options.body).checksum,activated:true})};}
 throw new Error('URL inesperada: '+url);
};
const physical=new PhysicalTransport();
await deploy(physicalProject,physicalScene,physical,()=>{});
assert.equal(activateCalls,1,'deploy debe activar el transporte físico exactamente una vez');
const cancelled=new AbortController();
const cancelledPhysical=new PhysicalTransport();
await assert.rejects(deploy(physicalProject,physicalScene,cancelledPhysical,status=>{if(status==='Activando')cancelled.abort();},cancelled.signal),{name:'AbortError'});
assert.equal(activateCalls,1,'la cancelación antes de activate no debe llamar al endpoint');
const rejectedPhysical=new PhysicalTransport();
await assert.rejects(deploy(physicalProject,physicalScene,rejectedPhysical,()=>{},undefined,()=>false),/cambió/);
assert.equal(activateCalls,1,'canActivate() === false no debe llamar al endpoint');
globalThis.fetch=originalFetch;
console.log('PASS: transporte físico verifica y activa una sola vez; cancelación y canActivate() bloquean la activación.');
console.log('BLOCKED BY HARDWARE: transporte físico sólo pasa con ESP32 AtlasLED y ACK/VERIFY/ACTIVATE reales.');
