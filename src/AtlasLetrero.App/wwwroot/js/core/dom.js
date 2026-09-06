export const $=(q,root=document)=>root.querySelector(q);
export const $$=(q,root=document)=>[...root.querySelectorAll(q)];
export function el(tag,attrs={},...children){const e=document.createElement(tag); for(const [k,v] of Object.entries(attrs)){if(k.startsWith('on'))e.addEventListener(k.slice(2),v);else if(k==='class')e.className=v;else if(k==='text')e.textContent=v;else e.setAttribute(k,v);}e.append(...children);return e;}
export async function renderAsync(target,producer){const value=await producer();target.replaceChildren(value);}
export function field(label,input){return el('label',{},el('span',{text:label}),input);}
export function button(text,action,attrs={}){return el('button',{type:'button',...attrs,onclick:action,text});}
