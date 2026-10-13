#!/usr/bin/env node
/** Offline btsnoop/Android logcat evidence extraction. Never sends data. */
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';

export function crc16(data, poly, init, reflected = false, xorout = 0) {
  let crc = init;
  for (const byte of data) {
    crc ^= reflected ? byte : byte << 8;
    for (let bit = 0; bit < 8; bit++) {
      crc = reflected ? (crc >>> 1) ^ ((crc & 1) ? poly : 0) : (crc << 1) ^ ((crc & 0x8000) ? poly : 0);
      crc &= 0xffff;
    }
  }
  return crc ^ xorout;
}

export function checksumCandidates(frames) {
  const models = [
    ['CCITT-FALSE',0x1021,0xffff,false,0],['XMODEM',0x1021,0,false,0],
    ['KERMIT',0x8408,0,true,0],['X25',0x8408,0xffff,true,0xffff],
    ['MODBUS',0xa001,0xffff,true,0],['ARC',0xa001,0,true,0],
    ['USB',0xa001,0xffff,true,0xffff],['AUG-CCITT',0x1021,0x1d0f,false,0],
    ['T10-DIF',0x8bb7,0,false,0],['DECT-X',0x0589,0,false,0],
    ['DNP',0xa6bc,0,true,0xffff],['EN13757',0x3d65,0,false,0xffff],
    ['BUYPASS',0x8005,0,false,0],['CMS',0x8005,0xffff,false,0],
    ['GENIBUS',0x1021,0xffff,false,0xffff],['GSM',0x1021,0,false,0xffff],
  ];
  const result = [];
  for (let start = 0; start < 5; start++) for (const [model,poly,init,reflected,xorout] of models) {
    for (const endian of ['little','big']) {
      const matches = frames.filter(f => crc16(f.subarray(start,-2),poly,init,reflected,xorout) ===
        (endian === 'little' ? f.readUInt16LE(f.length-2) : f.readUInt16BE(f.length-2))).length;
      if (matches === frames.length && frames.length) result.push({model,polynomial:`0x${poly.toString(16)}`,
        init:`0x${init.toString(16)}`,reflected,xorout:`0x${xorout.toString(16)}`,start_offset:start,trailer_endian:endian,matched:matches});
    }
  }
  return result;
}

export function logFrames(text) {
  const frames = new Map();
  for (const [index,line] of text.split(/\r?\n/).entries()) {
    if (!line.includes('写入成功')) continue;
    const match = line.match(/\[([\d,\s]+)\]/);
    if (!match) continue;
    const values = match[1].split(',').map(Number);
    if (values.some(x => !Number.isInteger(x) || x < 0 || x > 255)) throw Error(`Invalid byte at log line ${index+1}`);
    const frame = Buffer.from(values);
    if (frame.length < 6 || frame.readUInt16BE(0) !== 0xaa55) throw Error(`Malformed frame at log line ${index+1}`);
    const hex = frame.toString('hex'), length = frame.readUInt16LE(2), payload = frame.subarray(4,-2).toString('hex');
    if (!frames.has(hex)) frames.set(hex,{hex,source:'iamp-logcat.txt',line_numbers:[],semantics:'unknown',direction:'outgoing',
      payload_hex:payload,payloadHex:payload,length_field_le:length,length,total_bytes:frame.length,length_consistent:length+6===frame.length});
    frames.get(hex).line_numbers.push(index+1);
  }
  return [...frames.values()];
}

export function parseBtsnoop(raw, target) {
  if (raw.length < 16 || !raw.subarray(0,8).equals(Buffer.from('btsnoop\0'))) throw Error('Not a btsnoop capture');
  const version = raw.readUInt32BE(8), linktype = raw.readUInt32BE(12);
  if (version !== 1 || linktype !== 1002) throw Error(`Only btsnoop v1 HCI UART/H4 supported; got ${version}/${linktype}`);
  let offset=16, ordinal=0;
  const peers=new Map(), partial=new Map(), att=[], stats={}, target_connections=[];
  const count = key => stats[key]=(stats[key]??0)+1;
  while (offset < raw.length) {
    if (raw.length-offset < 24) { count('truncated_record_header'); break; }
    const original=raw.readUInt32BE(offset), captured=raw.readUInt32BE(offset+4), flags=raw.readUInt32BE(offset+8);
    offset+=24; ordinal++;
    if (captured > raw.length-offset) { count('truncated_record_data'); break; }
    const packet=raw.subarray(offset,offset+captured); offset+=captured; count('records');
    if (captured<original) count('snaplen_truncated_records');
    if (!packet.length) continue;
    const kind=packet[0], data=packet.subarray(1);
    if (kind===4 && data.length>=2) {
      const event=data[0], declared=data[1], params=data.subarray(2,2+declared);
      if (params.length<declared) { count('truncated_events'); continue; }
      if (event===0x3e && params.length>=12 && [1,0x0a,0x29].includes(params[0]) && params[1]===0) {
        const handle=params.readUInt16LE(2)&0xfff;
        const peer=[...params.subarray(6,12)].reverse().map(x=>x.toString(16).padStart(2,'0')).join(':').toUpperCase();
        peers.set(handle,peer);
        if (peer===target.toUpperCase()) target_connections.push({record:ordinal,connection_handle:handle,event_subevent:params[0]});
      } else if (event===5 && params.length>=4) {
        const handle=params.readUInt16LE(1)&0xfff; peers.delete(handle);
        for (const [key, entry] of partial) if (entry.handle===handle) partial.delete(key);
      }
      continue;
    }
    if (kind!==2 || data.length<4) continue;
    const word=data.readUInt16LE(0), declared=data.readUInt16LE(2), handle=word&0xfff, pb=(word>>>12)&3;
    const direction=(flags&1)?'incoming':'outgoing', key=`${handle}:${direction}`, payload=data.subarray(4,4+declared);
    if (payload.length<declared) { count('truncated_acl_records'); partial.delete(key); continue; }
    if ([0,2,3].includes(pb)) {
      if (payload.length<4) { count('short_l2cap_headers'); continue; }
      partial.set(key,{handle,length:payload.readUInt16LE(0),cid:payload.readUInt16LE(2),
        assembled:payload.subarray(4),records:[ordinal],peer:peers.get(handle)});
    } else if (pb===1) {
      if (!partial.has(key)) { count('orphan_acl_continuations'); continue; }
      const entry=partial.get(key); entry.assembled=Buffer.concat([entry.assembled,payload]); entry.records.push(ordinal);
    }
    const entry=partial.get(key);
    if (!entry || entry.assembled.length<entry.length) continue;
    partial.delete(key);
    if (entry.assembled.length!==entry.length) { count('l2cap_length_mismatch'); continue; }
    if (entry.cid!==4 || !entry.assembled.length) continue;
    const attribution=entry.peer===target.toUpperCase()?'target':entry.peer===undefined?'unattributed':'other';
    count(`att_${attribution}`);
    if (attribution==='other') continue;
    const value=entry.assembled, item={records:entry.records,connection_handle:handle,attribution,direction,
      opcode:value[0].toString(16).padStart(2,'0'),att_hex:value.toString('hex')};
    if ([0x0a,0x12,0x1b,0x1d,0x52].includes(value[0]) && value.length>=3) {
      item.attribute_handle=value.readUInt16LE(1); item.value_hex=value.subarray(3).toString('hex');
    } else if (value[0]===0x0b) item.value_hex=value.subarray(1).toString('hex');
    att.push(item);
  }
  stats.unfinished_l2cap_pdus=partial.size;
  return {source:'phone-btsnoop.log',sha256:crypto.createHash('sha256').update(raw).digest('hex'),stats,target_connections,att};
}

export function applicationFrames(packets) {
  const streams=new Map(), result=[], stats={};
  for (const packet of packets) {
    if (packet.attribution!=='target' || !['12','52','1b','1d'].includes(packet.opcode)) continue;
    const value=Buffer.from(packet.value_hex??'','hex');
    if (!value.length || packet.attribute_handle===9) continue;
    const key=`${packet.connection_handle}:${packet.direction}:${packet.attribute_handle}`;
    if (!streams.has(key)) streams.set(key,[]);
    const stream=streams.get(key);
    for (const byte of value) stream.push({byte,records:packet.records});
    while (stream.length>=4) {
      if (stream[0].byte!==0xaa || stream[1].byte!==0x55) {
        stats.unframed_bytes_skipped=(stats.unframed_bytes_skipped??0)+1; stream.shift(); continue;
      }
      const length=stream[2].byte+(stream[3].byte<<8)+6;
      if (stream.length<length) break;
      const chunk=stream.splice(0,length), frame=Buffer.from(chunk.map(x=>x.byte));
      const records=[...new Set(chunk.flatMap(x=>x.records))].sort((a,b)=>a-b);
      result.push({hex:frame.toString('hex'),records,direction:packet.direction,attribute_handle:packet.attribute_handle,
        length_field_le:length-6,crc16_gsm_valid:frame.readUInt16BE(frame.length-2)===crc16(frame.subarray(2,-2),0x1021,0,false,0xffff)});
    }
  }
  stats.unfinished_application_bytes=[...streams.values()].reduce((n,s)=>n+s.length,0);
  stats.frames=result.length; stats.crc16_gsm_valid=result.filter(x=>x.crc16_gsm_valid).length;
  return [result,stats];
}

export function main(argv=process.argv.slice(2)) {
  const args={logcat:'analysis/private/iamp-logcat.txt',btsnoop:'analysis/private/phone-btsnoop.log',
    output:'analysis/generated/protocol-capture.json',fixture:'tests/fixtures/f15-observed-frames.json'};
  for (let i=0;i<argv.length;i+=2) {
    const key=argv[i].replace(/^--/,'');
    if (!['logcat','btsnoop','target','output','fixture'].includes(key)||!argv[i+1]) throw Error(`Unknown/incomplete option ${argv[i]}`);
    args[key]=argv[i+1];
  }
  if (!args.target) {
    // Runtime target comes from the private log; no device identifier is embedded in source.
    const addresses=[...fs.readFileSync(args.logcat,'utf8').matchAll(/(?:device:|Device=)\s*([\dA-F]{2}(?::[\dA-F]{2}){5})/gi)].map(x=>x[1].toUpperCase());
    const unique=[...new Set(addresses)];
    if (unique.length!==1) throw Error('Provide --target: private log does not identify exactly one GATT peer');
    args.target=unique[0];
  }
  const log=fs.readFileSync(args.logcat), frames=logFrames(log.toString('utf8'));
  const candidates=checksumCandidates(frames.map(x=>Buffer.from(x.hex,'hex'))), capture=parseBtsnoop(fs.readFileSync(args.btsnoop),args.target);
  [capture.application_frames,capture.application_stats]=applicationFrames(capture.att);
  for (const frame of frames) {
    frame.btsnoop_write_records=capture.att.filter(p=>p.attribution==='target'&&p.direction==='outgoing'&&p.value_hex===frame.hex).map(p=>p.records);
    frame.source_records=[...new Set(frame.btsnoop_write_records.flat())].sort((a,b)=>a-b);
    const value=Buffer.from(frame.hex,'hex');
    frame.crc16_gsm_valid=crc16(value.subarray(2,-2),0x1021,0,false,0xffff)===value.readUInt16BE(value.length-2);
  }
  const responses=capture.application_frames.filter(x=>x.records.length===1&&x.records[0]===389).map(frame=>({hex:frame.hex,
    payloadHex:Buffer.from(frame.hex,'hex').subarray(4,-2).toString('hex'),length:frame.length_field_le,direction:'incoming',
    source:'phone-btsnoop.log',source_records:frame.records,semantics:'identity/capability response candidate; firmware ASCII corroborated by app log',crc16_gsm_valid:frame.crc16_gsm_valid}));
  const fixture={schema_version:1,evidence_type:'passive Android application log; no command safety classification',checksum_candidates:candidates,frames,responses};
  capture.logcat={source:path.basename(args.logcat),sha256:crypto.createHash('sha256').update(log).digest('hex'),unique_frames:frames.length,
    logged_writes:frames.reduce((n,f)=>n+f.line_numbers.length,0),checksum_candidates:candidates};
  for (const [file,value] of [[args.output,capture],[args.fixture,fixture]]) {
    fs.mkdirSync(path.dirname(file),{recursive:true}); fs.writeFileSync(file,JSON.stringify(value,null,2)+'\n');
  }
  console.log(JSON.stringify({stats:capture.stats,application_stats:capture.application_stats,target_connections:capture.target_connections,logcat:capture.logcat},null,2));
}
if (process.argv[1] && import.meta.url===pathToFileURL(path.resolve(process.argv[1])).href) main();
