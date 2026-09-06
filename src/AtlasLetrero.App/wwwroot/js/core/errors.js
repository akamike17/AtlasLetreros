export function report(error){const status=document.querySelector('#status');if(status){status.textContent=error.message||String(error);status.classList.add('error');}else alert(error.message||String(error));}
export function guard(action){return (...args)=>Promise.resolve().then(()=>action(...args)).catch(report);}
