export const uid=()=>crypto.randomUUID();
export const clone=x=>structuredClone(x);
export function layer(name='Capa 1'){return {id:uid(),name,visible:true,locked:false,opacity:1,order:0,objects:[]};}
export function frame(layers=[layer()]){return {id:uid(),durationMs:1000,layers};}
export function scene(name='Principal'){return {id:uid(),name,active:true,durationMs:1000,layers:[],frames:[frame()]};}
export function project(name,matrixConfiguration){const s=scene();return {id:uid(),name,formatVersion:1,matrixConfiguration,scenes:[s],activeSceneId:s.id,palette:['#36e6b0','#ffce66','#ffffff','#ff5577'],embeddedAssets:[],createdUtc:new Date().toISOString(),modifiedUtc:new Date().toISOString()};}
export const defaultMatrix=()=>({technology:'WS2812B',width:32,height:16,wiringMode:'serpentine',origin:'TL',primaryDirection:'horizontal',colorOrder:'GRB',rotation:0,flipX:false,flipY:false,preferredFps:12,brightnessLimit:80});
export function object(type,name,w,h,content,color='#36e6b0'){return {id:uid(),type,name,x:0,y:0,width:w,height:h,rotation:0,opacity:1,visible:true,content,style:{color},effect:{type:'none',speed:12}};}
