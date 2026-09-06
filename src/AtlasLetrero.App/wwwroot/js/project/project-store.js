import {api} from '../core/api-client.js';
export const openProject=id=>api('projects/'+id);
export const saveProject=p=>api('projects/'+p.id,'PUT',p);
export const autosaveProject=p=>api('projects/'+p.id+'/autosave','PUT',p);
