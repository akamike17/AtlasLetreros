// Serialize autosave and explicit saves so navigation can await durable storage.
export class SaveCoordinator {
 constructor(state, write) { this.state=state; this.write=write; this.pending=Promise.resolve(); this.busy=0; }
 save(recovery=false) {
  this.busy++;
  const operation=this.pending.catch(()=>{}).then(async()=>{
   const revision=this.state.revision;
   await this.write(structuredClone(this.state.project),recovery);
   if(!recovery&&revision===this.state.revision)this.state.dirty=false;
  }).finally(()=>this.busy--);
  this.pending=operation;
  return operation;
 }
}
