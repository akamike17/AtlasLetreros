export class PlaybackClock {time=0;playing=false;loop=true;last=null;
 play(){this.playing=true;this.last=null;}pause(){this.playing=false;this.last=null;}stop(){this.pause();this.time=0;}
 tick(now,duration){if(!this.playing){this.last=null;return;}if(this.last!==null)this.time+=now-this.last;this.last=now;if(this.time>=duration){if(this.loop)this.time%=Math.max(1,duration);else{this.time=Math.max(0,duration-1);this.pause();}}}
}
export function frameAt(scene,time){let start=0;for(let i=0;i<scene.frames.length;i++){const end=start+scene.frames[i].durationMs;if(time<end)return {frame:scene.frames[i],index:i,local:time-start};start=end;}return {frame:scene.frames.at(-1),index:scene.frames.length-1,local:0};}
export const duration=scene=>scene.frames.reduce((n,f)=>n+f.durationMs,0);
