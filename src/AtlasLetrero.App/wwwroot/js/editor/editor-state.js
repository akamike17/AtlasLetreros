import {HistoryManager} from './history-manager.js';
import {PlaybackClock} from './playback-clock.js';
import {frameAt} from './scene-renderer.js';
export class EditorState{
 constructor(project){this.project=project;this.history=new HistoryManager();this.clock=new PlaybackClock();this.layerId=this.frame.layers[0].id;this.objectId=null;this.dirty=false;this.revision=0;}
 get scene(){return this.project.scenes.find(s=>s.id===this.project.activeSceneId)||this.project.scenes[0];}
 get frame(){return frameAt(this.scene,this.clock.time).frame;}
 get layer(){return this.frame.layers.find(l=>l.id===this.layerId)||this.frame.layers[0];}
 get object(){return this.frame.layers.flatMap(l=>l.objects).find(o=>o.id===this.objectId);}
 change(action){this.history.record(this.project);action();this.touch();}
 touch(){this.dirty=true;this.revision++;this.onChange?.();}
 undo(){this.project=this.history.undo(this.project);this.touch();}
 redo(){this.project=this.history.redo(this.project);this.touch();}
}
