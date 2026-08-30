(() => {
  const $ = selector => document.querySelector(selector), $$ = selector => [...document.querySelectorAll(selector)];
  const canvas = $('#pixelCanvas'); if (!canvas) return;
  const W = 32, H = 16, OFF = 0;
  let data = new Uint32Array(W * H), tool = 'pencil', color = 0xff6b35, down = false, start = null, baseline = null, zoom = 100;
  const undo = [], redo = [], cells = Array.from({ length: W * H }, (_, index) => {
    const cell = document.createElement('div'); cell.className = 'pixel'; cell.dataset.index = index; canvas.append(cell); return cell;
  });
  const xy = index => [index % W, Math.floor(index / W)], idx = (x, y) => y * W + x;
  const valid = (x, y) => x >= 0 && y >= 0 && x < W && y < H;
  function render(source = data) {
    cells.forEach((cell, i) => { const value = source[i]; cell.classList.toggle('on', value !== OFF); cell.style.setProperty('--pixel-color', '#' + value.toString(16).padStart(6, '0')); });
    estimatePower();
  }
  function checkpoint() { undo.push(data.slice()); if (undo.length > 80) undo.shift(); redo.length = 0; }
  function setPixel(x, y, value, target = data) { if (valid(x, y)) target[idx(x, y)] = value; }
  function line(x0, y0, x1, y1, value, target = data) {
    let dx = Math.abs(x1-x0), sx = x0<x1?1:-1, dy = -Math.abs(y1-y0), sy = y0<y1?1:-1, error = dx+dy;
    while (true) { setPixel(x0,y0,value,target); if(x0===x1&&y0===y1) break; const twice=2*error; if(twice>=dy){error+=dy;x0+=sx} if(twice<=dx){error+=dx;y0+=sy} }
  }
  function shape(x0,y0,x1,y1,target) {
    if(tool==='line') line(x0,y0,x1,y1,color,target);
    if(tool==='rectangle'){ line(x0,y0,x1,y0,color,target);line(x1,y0,x1,y1,color,target);line(x1,y1,x0,y1,color,target);line(x0,y1,x0,y0,color,target); }
    if(tool==='circle'){ const rx=Math.abs(x1-x0),ry=Math.abs(y1-y0); for(let a=0;a<Math.PI*2;a+=.035)setPixel(Math.round(x0+rx*Math.cos(a)),Math.round(y0+ry*Math.sin(a)),color,target); }
  }
  function flood(x,y){ const source=data[idx(x,y)]; if(source===color)return; const q=[[x,y]]; while(q.length){const [cx,cy]=q.pop();if(!valid(cx,cy)||data[idx(cx,cy)]!==source)continue;data[idx(cx,cy)]=color;q.push([cx-1,cy],[cx+1,cy],[cx,cy-1],[cx,cy+1]);} }
  function drawText(x,y){ const text=prompt('Texto para el letrero:','SE ARREGLAN COMPUTADORAS'); if(!text)return; const c=document.createElement('canvas');c.width=W;c.height=8;const g=c.getContext('2d');g.fillStyle='#fff';g.font='7px monospace';g.textBaseline='top';g.imageSmoothingEnabled=false;g.fillText(text.toUpperCase(),0,0);const image=g.getImageData(0,0,W,8).data;for(let py=0;py<8;py++)for(let px=0;px<W;px++)if(image[(py*W+px)*4+3]>80)setPixel(x+px,y+py,color); }
  function move(dx,dy){ const moved=new Uint32Array(W*H); for(let y=0;y<H;y++)for(let x=0;x<W;x++)setPixel(x+dx,y+dy,baseline[idx(x,y)],moved);data=moved; }
  function point(event){ const cell=event.target.closest('.pixel'); return cell?xy(+cell.dataset.index):null; }
  canvas.addEventListener('pointerdown', event => { const p=point(event);if(!p)return;event.preventDefault();canvas.setPointerCapture(event.pointerId);checkpoint();down=true;start=p;baseline=data.slice();
    if(tool==='pencil'||tool==='eraser')setPixel(...p,tool==='eraser'?OFF:color);else if(tool==='fill')flood(...p);else if(tool==='text')drawText(...p);render(); });
  canvas.addEventListener('pointermove', event => { if(!down)return;const p=point(event);if(!p)return;
    if(tool==='pencil'||tool==='eraser'){line(...start,...p,tool==='eraser'?OFF:color);start=p;}else if(['line','rectangle','circle'].includes(tool)){const preview=baseline.slice();shape(...start,...p,preview);render(preview);return;}else if(tool==='move')move(p[0]-start[0],p[1]-start[1]);render(); });
  canvas.addEventListener('pointerup', event => { if(!down)return;const p=point(event)||start;if(['line','rectangle','circle'].includes(tool)){data=baseline.slice();shape(...start,...p,data);}down=false;render(); });
  $$('.tool[data-tool]').forEach(button=>button.addEventListener('click',()=>{$$('.tool').forEach(x=>x.classList.remove('active'));button.classList.add('active');tool=button.dataset.tool;}));
  function history(from,to){if(!from.length)return;to.push(data.slice());data=from.pop();render();}
  $('#undoButton').onclick=()=>history(undo,redo);$('#redoButton').onclick=()=>history(redo,undo);
  const picker=$('#colorPicker'),hex=$('#colorHex');function choose(value){if(!/^#[0-9a-f]{6}$/i.test(value))return;color=parseInt(value.slice(1),16);picker.value=value;hex.value=value.toUpperCase();}
  picker.oninput=()=>choose(picker.value);hex.onchange=()=>choose(hex.value);$$('.swatches button').forEach(x=>x.onclick=()=>choose(getComputedStyle(x).getPropertyValue('--swatch').trim()));
  $('#gridToggle').onclick=event=>{canvas.classList.toggle('show-grid');event.currentTarget.classList.toggle('active')};
  function setZoom(next){zoom=Math.max(50,Math.min(180,next));canvas.style.setProperty('--pixel',Math.round(18*zoom/100)+'px');$('#zoomValue').textContent=zoom+'%';}
  $('#zoomIn').onclick=()=>setZoom(zoom+10);$('#zoomOut').onclick=()=>setZoom(zoom-10);
  const brightness=$('#brightness');brightness.oninput=()=>{$('#brightnessValue').textContent=brightness.value+'%';canvas.style.opacity=Math.max(.18,+brightness.value/100);estimatePower();};
  function estimatePower(){let load=0;data.forEach(v=>load+=((v>>16&255)+(v>>8&255)+(v&255))/(255*3));const amps=load*.06*(+brightness.value/100)+.3;$('#powerEstimate').textContent=amps.toFixed(1)+' A de 10 A disponibles';}
  function toast(message,error=false){const t=$('#toast');t.textContent=message;t.style.background=error?'#ffd9d1':'#eaf9f3';t.classList.add('show');setTimeout(()=>t.classList.remove('show'),2600);}
  function save(){localStorage.setItem('atlas-design',JSON.stringify({name:$('#designName').value,pixels:[...data],brightness:+brightness.value,animation:$('#animation').value,duration:+$('#duration').value,speed:+$('#speed').value}));toast('Diseño guardado en este equipo');}
  $$('.button.ghost').forEach(x=>x.onclick=save);
  async function send(){const button=$('#sendButton');button.disabled=true;button.textContent='Enviando…';try{const response=await fetch('/api/design',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({name:$('#designName').value,width:W,height:H,pixels:[...data],brightness:Math.round(+brightness.value*2.55),animation:$('#animation').value,durationSeconds:+$('#duration').value,speed:+$('#speed').value})});if(!response.ok)throw new Error((await response.json()).message||'No se pudo enviar');const result=await response.json();toast(`Reproduciendo ${result.pixels} píxeles en el simulador`);}catch(error){toast(error.message,true)}finally{button.disabled=false;button.textContent='Enviar al letrero';}}
  $('#sendButton').onclick=send;$('#playButton').onclick=send;$('#stopButton').onclick=async()=>{await fetch('/api/stop',{method:'POST',headers:{'Content-Type':'application/json'},body:'{"protocolVersion":1}'});toast('Reproducción detenida')};
  $('#imageButton').onclick=()=>$('#imageInput').click();$('#imageInput').onchange=event=>{const file=event.target.files[0];if(!file)return;const image=new Image();image.onload=()=>{checkpoint();const c=document.createElement('canvas');c.width=W;c.height=H;const g=c.getContext('2d');g.imageSmoothingEnabled=false;g.drawImage(image,0,0,W,H);const px=g.getImageData(0,0,W,H).data;for(let i=0;i<data.length;i++)data[i]=px[i*4+3]<40?0:(px[i*4]<<16|px[i*4+1]<<8|px[i*4+2]);render();URL.revokeObjectURL(image.src)};image.src=URL.createObjectURL(file)};
  window.addEventListener('keydown',event=>{if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='z'){event.preventDefault();history(event.shiftKey?redo:undo,event.shiftKey?undo:redo)}if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='s'){event.preventDefault();save()}const map={p:'pencil',e:'eraser',l:'line',r:'rectangle',c:'circle',f:'fill',t:'text',m:'move'};const selected=map[event.key.toLowerCase()];if(!event.ctrlKey&&selected)document.querySelector(`[data-tool="${selected}"]`)?.click();});
  const stored=localStorage.getItem('atlas-design');if(stored){try{const s=JSON.parse(stored);if(s.pixels?.length===W*H)data=Uint32Array.from(s.pixels);$('#designName').value=s.name||'Anuncio principal';brightness.value=s.brightness||72;$('#animation').value=s.animation||'none';$('#duration').value=s.duration||6;$('#speed').value=s.speed||1;}catch{localStorage.removeItem('atlas-design')}}else{const sample=['01110111011100100111011110111','10000100010000101000100010100','01110111011000101000111000111','00010100010000101000100010001','11100111011100100111011110111'];sample.forEach((row,y)=>[...row].forEach((bit,x)=>{if(bit==='1'&&x<W)data[idx(x,y+5)]=color}))}brightness.oninput();render();
})();
