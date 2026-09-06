import {el,button} from './dom.js';
export function modal(title,content,onSubmit,submitText='Insertar'){
 const dialog=el('dialog',{},el('form',{method:'dialog'},el('header',{},el('h2',{text:title}),button('×',()=>dialog.close(),{'aria-label':'Cerrar'})),content,el('footer',{},button('Cancelar',()=>dialog.close()),el('button',{type:'submit',class:'primary',text:submitText}))));
 document.body.append(dialog);const form=dialog.querySelector('form');form.addEventListener('submit',async e=>{e.preventDefault();if(!form.reportValidity())return;const submit=form.querySelector('[type=submit]');submit.disabled=true;try{await onSubmit(form);dialog.close();}catch(err){let p=form.querySelector('.error');if(!p){p=el('p',{class:'error'});content.append(p);}p.textContent=err.message;}finally{submit.disabled=false;}});dialog.addEventListener('close',()=>dialog.remove());dialog.showModal();return dialog;
}
export async function confirmDelete(text){return confirm(text);}
