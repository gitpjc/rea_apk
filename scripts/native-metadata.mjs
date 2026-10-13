import {readFile} from 'node:fs/promises';
const load=async name=>JSON.parse(await readFile('analysis/generated/native/'+name+'.json')).structuredContent.normalized_result;
const s=Buffer.from((await load('main-string-table')).bytes_hex,'hex');
console.log('Qt string block:',s.toString('latin1').split('\0').filter(Boolean));
const table=Buffer.from((await load('main-meta-table')).bytes_hex,'hex');
console.log('Qt metadata DWORDS:',Array.from({length:table.length/4},(_,i)=>table.readUInt32LE(i*4)));
try {
 const crc=Buffer.from((await load('usb-crc-table')).bytes_hex,'hex');
 let match=true;
 for(let i=0;i<256;i++){let v=i<<8;for(let b=0;b<8;b++)v=((v<<1)^((v&0x8000)?0x1021:0))&0xffff;if(v!==crc.readUInt16LE(i*2))match=false;}
 console.log('All 256 CRC table entries equal nonreflected polynomial 0x1021:',match);
} catch{}
