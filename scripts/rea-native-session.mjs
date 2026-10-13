import { Client } from '../.tools/rea/node_modules/@modelcontextprotocol/client/dist/index.mjs';
import { StdioClientTransport } from '../.tools/rea/node_modules/@modelcontextprotocol/client/dist/stdio.mjs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
const root = path.resolve(import.meta.dirname, '..');
const output = path.join(root, 'analysis/generated/native');
await mkdir(output, { recursive: true });
if (process.argv[2] === 'schemas') {
 const c=JSON.parse(await readFile(path.join(root,'analysis/generated/rea-mcp-catalog.json')));
 for(const t of c.tools.filter(t=>/procedure|reference|xref|pseudocode|function|bytes/.test(t.name))) console.log(t.name,JSON.stringify(t.inputSchema));
 process.exit(0);
}
const transport = new StdioClientTransport({ command: process.execPath,
 args: [path.join(root,'.tools/rea/node_modules/rea-agents/scripts/rea.mjs'),'mcp'],
 env: {...process.env,GHIDRA_INSTALL_DIR:path.join(root,'.tools/ghidra/ghidra_12.1.4_PUBLIC')}, stderr:'pipe'});
transport.stderr?.on('data', b=>process.stderr.write(b));
const client = new Client({name:'iamp-native-analysis',version:'0.1.0'});
let opened=false;
async function call(name,args,label=name) {
 console.log('CALL',label);
 const result=await client.callTool({name,arguments:args},undefined,{timeout:900000});
 await writeFile(path.join(output,label+'.json'),JSON.stringify(result,null,2));
 const body=result.structuredContent;
 console.log(JSON.stringify({evidence_id:body?.evidence_id,result:body?.result,isError:result.isError??false}));
 return result;
}
try {
 await client.connect(transport);
 await call('open_binary',{path:path.join(root,'analysis/generated/vendor/MooerStudioForF40i.exe'),provider_id:'ghidra'}); opened=true;
 const queueFile=path.join(root,'analysis/generated/native-jobs.json');
 try { await readFile(queueFile); } catch {
  await writeFile(queueFile,JSON.stringify([
   {name:'procedure_pseudo_code',args:{procedure:'0x42a880'},label:'usb-serializer-pseudo'},
   {name:'procedure_pseudo_code',args:{procedure:'0x412510'},label:'usb-crc-pseudo'},
   {name:'procedure_pseudo_code',args:{procedure:'0x4127f0'},label:'usb-report-send-pseudo'},
   {name:'procedure_pseudo_code',args:{procedure:'0x41feb0'},label:'device-info-pseudo'},
   {name:'read_bytes',args:{address:'0x483000',length:512},label:'usb-crc-table'},
   {name:'export_evidence_bundle',args:{path:path.join(output,'vendor-evidence.json'),overwrite:true},label:'evidence-export'},
   {name:'STOP'}
  ],null,2));
 }
 let index=0;
 queue: for(let pass=0;pass<240;pass++) {
  const jobs=JSON.parse(await readFile(queueFile));
  while(index<jobs.length) {const j=jobs[index++]; if(j.name==='STOP') break queue; await call(j.name,j.args,j.label);}
  await new Promise(r=>setTimeout(r,5000));
 }
} finally {
 if(opened) await call('close_binary',{snapshot_path:path.join(output,'vendor-snapshot.json'),overwrite:true},'close_binary');
 await client.close(); await transport.close();
}
