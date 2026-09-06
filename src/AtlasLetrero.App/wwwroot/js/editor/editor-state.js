import {HistoryManager} from './history-manager.js';
import {DirtyState} from '../project/dirty-state.js';
export class EditorState {
 constructor(project){this.project=project;this.sceneId=project.activeSceneId;this.frameIndex=0;this.layerId=this.frame.layers[0].id;this.objectId=null;this.history=new HistoryManager();this.dirty=new DirtyState();this.color='#36e6b0';}
 get scene(){return this.project.scenes.find(s=>s.id===this.sceneId)||this.project.scenes[0];}
 get frame(){return this.scene.frames[Math.min(this.frameIndex,this.scene.frames.length-1)];}
 get layer(){return this.frame.layers.find(l=>l.id===this.layerId)||this.frame.layers[0];}
 get object(){return this.layer?.objects.find(o=>o.id===this.objectId);}
 change(action){this.history.push(this.project);action();this.dirty.mark();}
}
