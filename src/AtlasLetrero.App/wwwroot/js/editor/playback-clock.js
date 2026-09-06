export class PlaybackClock{
 constructor(){this.time=0;this.playing=false;this.loop=true;this.last=null;}
 play(){this.playing=true;this.last=null;}
 pause(){this.playing=false;this.last=null;}
 stop(){this.pause();this.time=0;}
 tick(timestamp,duration){if(this.playing){if(this.last!==null)this.time+=timestamp-this.last;this.last=timestamp;if(this.time>=duration){if(this.loop)this.time%=Math.max(1,duration);else{this.time=duration-1;this.pause();}}}return this.time;}
}
