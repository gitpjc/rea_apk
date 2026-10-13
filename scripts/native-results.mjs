import {readFile,readdir} from 'node:fs/promises';
const dir='analysis/generated/native';
for(const f of await readdir(dir)) {
 if(!f.endsWith('.json') || (process.argv[2]&&!f.includes(process.argv[2]))) continue;
 const r=JSON.parse(await readFile(dir+'/'+f));
 console.log(f, r.structuredContent?.evidence_id||'');
 console.log(JSON.stringify(r.structuredContent?.normalized_result||r.structuredContent||r,null,2));
}
