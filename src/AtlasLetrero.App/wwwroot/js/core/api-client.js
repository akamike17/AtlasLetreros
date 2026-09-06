export async function api(path, method='GET', body, signal) {
  const response=await fetch('/api/'+path,{method,signal,headers:body ? {'Content-Type':'application/json'} : {},body:body?JSON.stringify(body):undefined});
  const value=await response.json(); if(!response.ok) throw new Error(value.message||'No se pudo completar la operación.'); return value;
}
