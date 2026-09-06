import {header,wireNew,api,el,$,safe} from '../app.js';
header();wireNew();
await safe(async()=>{const items=await api('projects');$('#recent').replaceChildren(...items.slice(0,5).map(p=>el('div',{class:'project-row'},el('div',{class:'project-name'},el('strong',{text:p.name}),el('small',{text:p.matrix.width+' × '+p.matrix.height+' LED'})),el('a',{class:'button',href:'/editor.html?id='+p.id,text:'Abrir'}))));if(!items.length)$('#recent').append(el('div',{class:'empty',text:'Tu primer letrero empieza aquí. Crea un proyecto para diseñarlo.'}));})();
