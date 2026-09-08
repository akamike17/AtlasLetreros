// Batch effect helper for drawings made from many independent LED objects.
// It deliberately reuses the editor's existing inspector change path so
// persistence, undo/redo, rendering and deployment remain unchanged.
const root=document.querySelector('#editor');
if(root){
 const selectedLayerRange=()=>{
  const chips=[...root.querySelectorAll('.layer-list .layer-chip')];
  const selected=chips.findIndex(chip=>chip.classList.contains('selected'));
  if(selected<0)return null;
  const count=chip=>{
   const label=chip.querySelector('button:last-child')?.textContent||'';
   const match=label.match(/·\s*(\d+)\s*$/);
   return match?Number(match[1]):0;
  };
  return {offset:chips.slice(0,selected).reduce((sum,chip)=>sum+count(chip),0),count:count(chips[selected])};
 };
 const install=()=>{
  const inspector=root.querySelector('.inspector');
  if(!inspector||inspector.querySelector('[data-batch-effects]'))return;
  const effect=inspector.querySelector('[name=effect]');
  const speed=inspector.querySelector('[name=speed]');
  if(!effect||!speed)return;
  const bar=document.createElement('div');bar.dataset.batchEffects='true';bar.className='row';bar.style.marginTop='10px';
  const apply=document.createElement('button');apply.type='button';apply.textContent='Efecto a toda la capa';apply.title='Aplica el efecto y velocidad actuales a todos los objetos de la capa seleccionada';
  apply.addEventListener('click',()=>{
   const wantedEffect=inspector.querySelector('[name=effect]')?.value;
   const wantedSpeed=inspector.querySelector('[name=speed]')?.value;
   const range=selectedLayerRange();
   if(!wantedEffect||!wantedSpeed||!range||range.count===0)return;
   for(let i=0;i<range.count;i++){
    const buttons=[...inspector.querySelectorAll('.object-list button')];
    const target=buttons[range.offset+i];
    if(!target)continue;
    target.click();
    const currentEffect=inspector.querySelector('[name=effect]');
    const currentSpeed=inspector.querySelector('[name=speed]');
    if(!currentEffect||!currentSpeed)continue;
    currentEffect.value=wantedEffect;
    currentSpeed.value=wantedSpeed;
    currentEffect.dispatchEvent(new Event('change',{bubbles:true}));
   }
  });
  bar.append(apply);inspector.append(bar);
 };
 new MutationObserver(()=>queueMicrotask(install)).observe(root,{childList:true,subtree:true});
 install();
}
