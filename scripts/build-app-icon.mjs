// Convert the extracted mobile launcher PNG into Windows icon frames; no image generation.
import fs from 'node:fs/promises';
import path from 'node:path';
import { createRequire } from 'node:module';
const require=createRequire(import.meta.url);
const sharp=require('sharp');
const root=path.resolve(import.meta.dirname,'..');
const input=path.join(root,'assets/app/iamp.png');
const sizes=[16,20,24,32,40,48,64,96,128,192,256];
const frames=await Promise.all(sizes.map(size=>sharp(input).resize(size,size,{kernel:'lanczos3'}).png().toBuffer()));
const header=Buffer.alloc(6+sizes.length*16);header.writeUInt16LE(1,2);header.writeUInt16LE(sizes.length,4);
let offset=header.length;
for(let i=0;i<sizes.length;i++){
 const at=6+i*16;
 header[at]=header[at+1]=sizes[i]===256?0:sizes[i];
 header.writeUInt16LE(1,at+4);header.writeUInt16LE(32,at+6);
 header.writeUInt32LE(frames[i].length,at+8);header.writeUInt32LE(offset,at+12);offset+=frames[i].length;
}
await fs.writeFile(path.join(root,'assets/app/iamp.ico'),Buffer.concat([header,...frames]));
console.log(`Created iamp.ico with ${sizes.join(', ')} pixel frames.`);
