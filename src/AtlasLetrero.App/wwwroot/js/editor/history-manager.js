export class HistoryManager{
 constructor(limit=60){this.past=[];this.future=[];this.limit=limit;}
 record(document){this.past.push(structuredClone(document));if(this.past.length>this.limit)this.past.shift();this.future=[];}
 undo(document){if(!this.past.length)return document;this.future.push(structuredClone(document));return this.past.pop();}
 redo(document){if(!this.future.length)return document;this.past.push(structuredClone(document));return this.future.pop();}
}
