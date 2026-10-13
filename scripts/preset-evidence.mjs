// Offline, bounded preset-structure analysis. No device or network access.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
const report=JSON.parse(await readFile('analysis/generated/protocol-capture.json'));
const logBytes=await readFile('analysis/private/iamp-logcat.txt');
const lines=logBytes.toString('utf8').split(/\r?\n/);
const responses=report.application_frames.filter(f=>f.direction==='incoming'&&[0x12,0x22].includes(Buffer.from(f.hex,'hex')[4]));
assert.equal(responses.length,2,'Review coverage if more than one state/detail reply is present');
const state=responses.find(f=>Buffer.from(f.hex,'hex')[4]===0x12);
const detail=responses.find(f=>Buffer.from(f.hex,'hex')[4]===0x22);
assert.ok(state.crc16_gsm_valid&&detail.crc16_gsm_valid);
const payload=Buffer.from(detail.hex,'hex').subarray(4,-2);
const blocks=[];
let offset=22;
for(let n=0;n<20;n++){
 const start=offset,sequence=payload[offset++],type=payload[offset++],parameterBytes=payload[offset++];
 assert.equal(sequence,n+1);assert.equal(parameterBytes%2,0);assert.ok(offset+parameterBytes<=payload.length);
 const parameters=Array.from({length:parameterBytes/2},(_,i)=>payload.readUInt16LE(offset+i*2));
 offset+=parameterBytes;blocks.push({payloadOffset:start,sequence,type,parameterBytes,parameters});
}
const logged=[];
for(let i=0;i<lines.length-3;i++){
 const seq=lines[i].match(/flutter : 序号：(\d+)$/);
 const type=lines[i+1].match(/flutter : 类型：(\d+)$/);
 const len=lines[i+2].match(/flutter : 参数长度：(\d+)$/);
 const params=lines[i+3].match(/flutter : 参数：(\[[\d, ]*\])$/);
 if(seq&&type&&len&&params)logged.push({line:i+1,sequence:Number(seq[1]),type:Number(type[1]),parameterBytes:Number(len[1]),parameters:JSON.parse(params[1])});
}
assert.equal(logged.length,20);
for(let i=0;i<20;i++)for(const k of ['sequence','type','parameterBytes','parameters'])assert.deepEqual(blocks[i][k],logged[i][k],`Log mismatch record ${i+1}: ${k}`);
const name=payload.subarray(2,22).toString('utf8').split('\0')[0];
assert.equal(name,'Cali MK3 Lead','Only this already-reviewed factory-name fixture is approved for export');
let windowsComparison='unavailable';
try {
 const live=JSON.parse(await readFile('analysis/private/windows-current-preset.json'));
 assert.equal(live.current.RawStatePayloadHex.toLowerCase(),Buffer.from(state.hex,'hex').subarray(4,-2).toString('hex'));
 assert.equal(live.current.RawDetailPayloadHex.toLowerCase(),payload.toString('hex'));
 windowsComparison='Both payloads equal retained Android capture byte-for-byte';
} catch(e) {if(e.code!=='ENOENT')throw e;}
const fixture={description:'Retained initial F15i V1.1.2 state/preset replies. Generic device name and matching factory preset only; no addresses/accounts/timestamps.',sourceCaptureSha256:report.sha256,sourceLogSha256:createHash('sha256').update(logBytes).digest('hex'),
 state:{frameHex:state.hex,records:state.records,payloadLength:92,observedPresetIndex:16,deviceNameOffset:9,deviceNameBytes:20},
 detail:{frameHex:detail.hex,records:detail.records,payloadLength:payload.length,presetIndex:payload[1],name,nameOffset:2,nameBytes:20,blocks,trailingPayloadHex:payload.subarray(offset).toString('hex'),logLines:logged.map(x=>x.line)}};
await writeFile('tests/fixtures/f15-observed-preset.json',JSON.stringify(fixture,null,2)+'\n');
console.log(JSON.stringify({stateRecords:state.records,detailRecords:detail.records,payloadLength:payload.length,name,blocksMatchingLog:blocks.length,logLines:logged.map(x=>x.line),trailingPayloadHex:fixture.detail.trailingPayloadHex,windowsComparison},null,2));
