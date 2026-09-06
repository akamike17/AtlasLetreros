export const events=new EventTarget();
export const emit=(name,detail)=>events.dispatchEvent(new CustomEvent(name,{detail}));
