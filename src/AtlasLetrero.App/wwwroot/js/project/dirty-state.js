export class DirtyState {dirty=false;revision=0;mark(){this.dirty=true;this.revision++;}clean(revision=this.revision){if(revision===this.revision)this.dirty=false;}}
