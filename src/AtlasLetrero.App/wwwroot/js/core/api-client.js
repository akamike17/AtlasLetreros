export async function api(path,method='GET',data){
 const response=await fetch('/api/'+path,{method,headers:data?{'Content-Type':'application/json'}:{},body:data?JSON.stringify(data):undefined});
 const result=await response.json();if(!response.ok)throw new Error(result.message||'No se pudo completar la operación.');return result;
}
