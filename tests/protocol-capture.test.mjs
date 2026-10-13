import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { crc16, parseBtsnoop, applicationFrames } from '../scripts/protocol-capture.mjs';

function record(packet,flags=1,original=packet.length) {
  const header=Buffer.alloc(24); header.writeUInt32BE(original,0); header.writeUInt32BE(packet.length,4); header.writeUInt32BE(flags,8);
  return Buffer.concat([header,packet]);
}
function acl(payload,pb=2) {
  const header=Buffer.alloc(5); header[0]=2; header.writeUInt16LE(2|(pb<<12),1); header.writeUInt16LE(payload.length,3);
  return Buffer.concat([header,payload]);
}
function parse(records) {
  const header=Buffer.alloc(16); header.write('btsnoop\0'); header.writeUInt32BE(1,8); header.writeUInt32BE(1002,12);
  // Synthetic device address, unrelated to any actual amp.
  return parseBtsnoop(Buffer.concat([header,...records]),'01:02:03:04:05:06');
}
test('all observed fixture checksums and lengths',()=>{
  const fixture=JSON.parse(fs.readFileSync(new URL('./fixtures/f15-observed-frames.json',import.meta.url)));
  assert.equal(fixture.frames.length,17);
  for(const item of fixture.frames) {
    const frame=Buffer.from(item.hex,'hex');
    assert.equal(frame.length,frame.readUInt16LE(2)+6);
    assert.equal(crc16(frame.subarray(2,-2),0x1021,0,false,0xffff),frame.readUInt16BE(frame.length-2));
  }
});
test('unknown handle stays unattributed',()=>{
  const result=parse([record(acl(Buffer.from('040004001b0800ff','hex')))]);
  assert.equal(result.att[0].attribution,'unattributed');
});
test('connection attribution and ACL fragmentation',()=>{
  const params=Buffer.concat([Buffer.from('0a0002000000060504030201','hex'),Buffer.alloc(19)]);
  const event=Buffer.concat([Buffer.from([4,0x3e,params.length]),params]);
  const pdu=Buffer.from('050004001b08000102','hex');
  const result=parse([record(event),record(acl(pdu.subarray(0,6))),record(acl(pdu.subarray(6),1))]);
  assert.equal(result.att[0].attribution,'target'); assert.equal(result.att[0].att_hex,'1b08000102');
  assert.deepEqual(result.att[0].records,[2,3]);
});
test('declared ACL truncation is not decoded',()=>{
  const result=parse([record(Buffer.from('0202201400040004001b','hex'),1,25)]);
  assert.deepEqual(result.att,[]); assert.equal(result.stats.truncated_acl_records,1);
});
test('application notification fragmentation',()=>{
  const frame=Buffer.from('aa55100002410156312e312e320005500a0a460af221','hex');
  const packets=[frame.subarray(0,7),frame.subarray(7)].map((part,i)=>({attribution:'target',opcode:'1b',
    connection_handle:2,direction:'incoming',attribute_handle:8,records:[i+1],value_hex:part.toString('hex')}));
  const [frames,stats]=applicationFrames(packets);
  assert.equal(frames[0].hex,frame.toString('hex')); assert.deepEqual(frames[0].records,[1,2]); assert.equal(stats.crc16_gsm_valid,1);
});
