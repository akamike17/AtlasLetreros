export const uid=()=>crypto.randomUUID();
export const newLayer=(name='Capa 1')=>({id:uid(),name,visible:true,locked:false,opacity:1,objects:[]});
export const newFrame=()=>({id:uid(),durationMs:1000,layers:[newLayer()]});
export const newScene=(name='Principal')=>({id:uid(),name,active:true,durationMs:1000,frames:[newFrame()]});
export function newProject(name,matrix){const scene=newScene();return {id:uid(),name,formatVersion:1,matrix,scenes:[scene],activeSceneId:scene.id,palette:['#94ed55','#ff5d55','#55bfff','#ffffff'],embeddedAssets:[],createdUtc:new Date().toISOString(),modifiedUtc:new Date().toISOString()};}
export function matrixFromForm(data){return {technology:data.get('technology'),width:+data.get('width'),height:+data.get('height'),wiring:data.get('wiring'),origin:data.get('origin'),direction:data.get('direction'),colorOrder:data.get('colorOrder'),fps:+data.get('fps'),brightness:+data.get('brightness'),rotation:+(data.get('rotation')||0),flipX:data.has('flipX'),flipY:data.has('flipY')};}
