import assert from 'node:assert/strict';
import fs from 'node:fs';
import {webcrypto} from 'node:crypto';
import {renderScene} from '../../src/AtlasLetrero.App/wwwroot/js/editor/scene-renderer.js';
import {rasterText} from '../../src/AtlasLetrero.App/wwwroot/js/assets/bitmap-font.js';
import {SimulatorTransport,deploy} from '../../src/AtlasLetrero.App/wwwroot/js/editor/deployment-controller.js';

const font=JSON.parse(fs.readFileSync(new URL('../../src/AtlasLetrero.App/wwwroot/assets/fonts/5x7.json',import.meta.url)));
globalThis.crypto ??= webcrypto;
const bitmap=rasterText('SE REPARAN COMPUTADORAS Ñ',font);
const matrix={width:32,height:16,fps:12,brightness:80};
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
