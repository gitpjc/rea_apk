# F15i Li protocol evidence ledger

Updated 13 October 2026. This is a partial reconstruction. The initial V1.1.2 capture analysis below was offline; subsequent V1.1.9 phone captures and bounded Windows hardware tests established two restricted control operations. This is not a complete protocol specification.

## Subsequent V1.1.9 control proofs

- [Parameter-Editing.md](Parameter-Editing.md): payload `25` followed by a complete effect record changed the first-position Noise Killer threshold. Windows verified 25 -> 26 -> 25 and restored the complete preset-detail payload. The initial implementation supports only the captured model/configuration and values 25-28. Response `26 01` is a candidate acknowledgement; its semantic meaning remains unknown. Read-back establishes the actual result.
- [Preset-Selection.md](Preset-Selection.md): a 92-byte state copy with discriminator `13` and the desired preset index recalled slots 4/5. Windows verified 4 -> 5 -> 4, preserving the other state fields and restoring both full state and preset-detail payloads exactly. Exact current-preset detail hashes guard against discarding unfamiliar edits. Response `14 01` semantics remain unknown.
- No preset save, arbitrary global-state write, firmware update or backup-restore command has been established. All test changes must return to the recorded baseline and originally selected preset, with read-back verification.

The following sections retain the initial capture's provenance and unresolved boundaries; their V1.1.2 measurements are historical rather than the current amplifier baseline.

## Sources and reproduction

- `analysis/private/iamp-logcat.txt`: app-specific log, SHA-256 `b10011b2822d2e005bfe78d698b1a2b52b6804c16590cfcb40aac826e59bcaf1`.
- `analysis/private/phone-btsnoop.log`: previously decoded Android btsnooz capture; the output records its SHA-256. Source raw phone logs remain private and untracked.
- `scripts/protocol-capture.mjs`: offline Node.js parser using built-in modules. Run `node scripts/protocol-capture.mjs` from the repository root with the retained inputs present. Run `node --test tests/protocol-capture.test.mjs` to verify parsing and fixtures. The active implementation requires no Python runtime.
- `analysis/generated/protocol-capture.json`: regenerated target ATT packets, contributing record ordinals, reassembled application frames, integrity statistics, and source digests. Generated evidence remains untracked.
- `tests/fixtures/f15-observed-frames.json`: sanitized 17 unique outgoing AA55 frames, log line provenance and matching target-write record ordinals. Semantic labels deliberately remain unknown; these are observed bytes, not an approved replay list.

## Confirmed transport observations

The capture contains 2,095 HCI records. LE enhanced connection-complete event record 328 attributes connection handle 2 to the amp address identified by Android's iAMP GATT logs. The parser retains target ATT, explicitly marks unknown-handle ATT as unattributed, and excludes other devices' ATT contents and addresses. It does not infer identity from a connection handle alone.

There are 233 target ATT PDUs, including 37 application write requests, one CCC subscription request and 139 notifications. No record/ACL truncation, unfinished L2CAP PDU, or unfinished application stream was detected in this particular capture. Original and captured btsnoop lengths, HCI-declared lengths and L2CAP-declared lengths are handled separately. ACL and application notification fragments are reassembled with record provenance.

Service discovery record 343 exposes service UUID `FFF0`, service range handles 3–11. Characteristic discovery record 353 establishes:

| UUID | Value handle in this session | Properties | Observed role |
| --- | --- | --- | --- |
| FFF1 | 5 | `0x18` (write, notify) | Not used by the captured application exchange |
| FFF2 | 8 | `0x12` (read, notify) | Notifications carrying protocol responses |
| FFF3 | 11 | `0x08` (write) | Application write requests |

FFF2's CCC descriptor is handle 9 (record 362). The phone enables notifications by writing `0100` (record 370). These are Bluetooth-base 16-bit UUIDs, e.g. `0000fff2-0000-1000-8000-00805f9b34fb`. Rediscover characteristics and descriptors on each connection; do not hardcode session-specific handles. ATT MTU response is 508 (records 367 and 382). Even with that MTU, some application responses span many notifications.

## Confirmed frame integrity

All 40 logged writes (17 distinct frames) have this layout:

| Byte offset | Meaning supported by evidence |
| --- | --- |
| 0–1 | `AA 55` marker |
| 2–3 | Little-endian payload byte count N |
| 4 through 4+N−1 | Payload; first byte acts as a command/response discriminator in this session |
| 4+N through 5+N | Big-endian CRC trailer |

Total frame size is N+6. A bounded search of 16 common CRC models, five coverage start offsets and both trailer byte orders finds CRC-16/GSM covering **the length bytes and payload**, excluding `AA55` and the trailer: polynomial `0x1021`, initial value `0x0000`, no input/output reflection, final XOR `0xFFFF`, checksum stored most-significant byte first. This candidate matches all 17 distinct logged writes and all 79 reassembled incoming/outgoing application frames in the HCI capture. It is strongly corroborated for this firmware/session; it is not proof of every model or firmware's behavior.

Example observed request: `AA55 0200 0101 3187`. Its response at record 389 is `AA55 1000 02410156312E312E320005500A0A460A F221`, containing the ASCII string `V1.1.2`. Unit tests check all fixture checksums plus synthetic capture truncation, connection attribution, ACL fragmentation and application fragmentation.

## Inferred feature associations

These associations are grounded in outgoing requests followed by coherent responses in the capture. They require a controlled comparison before production code treats them as documented commands. They are not classifications of side effects or authorization to replay arbitrary frames.

| Observed outgoing frame | Evidence and provisional interpretation |
| --- | --- |
| `AA55020001013187` | Record 386; response 389 carries firmware/version and capability values also decoded by iAMP log lines 175–183. Candidate identity/capability query. The preceding log says “get battery”; the actual response does not justify calling this a battery query. |
| `AA5503002901289F21` | Record 394; reassembled response 402–451 starts payload `2A0128` and contains 40 preset names. Candidate names query, indices 1–40. |
| `AA550300292950EFF1` | Record 452; response 504–515 starts `2A2950` and contains the remaining preset names. Candidate names query, indices 41–80. |
| `AA55010011CADF` | Record 601; response 604–605 starts `1210` and contains device/model text. Candidate current-state query; field layout remains unresolved. |
| `AA55020021103571` | Record 606; response 609–611 starts `2210` and contains “Cali MK3 Lead”. Candidate preset-detail query for preset 16; numbering and parameter layout need verification. |
| `AA550100915B57` | Repeated at roughly 30-second intervals in logcat; responses start `92` with varying values. Polling behavior observed; battery/other field semantics unconfirmed. |

iAMP logs report firmware `V1.1.2`, 80 presets, 10 GIR slots, 10 GNR slots, 70 drums and 10 looper files. These counts are firmware-reported/session observations, not hardcoded product-wide guarantees. Responses beginning `66` follow requests beginning `65` and include “Empty” or drum names; record layouts and category meanings remain partially inferred.

## Remaining work and limits

- [x] Decode the retained capture with connection attribution and length checks.
- [x] Extract sanitized outgoing fixtures and establish the checksum across all observed logged writes.
- [x] Reassemble captured notification frames and verify all 79 checksums.
- [ ] Independently compare the candidate queries against controlled single-action captures and document response schemas.
- [ ] Identify every parameter, index convention, error response, sequence/timing requirement and maximum frame size.
- [ ] Complete mappings for preset changes, effect edits, drum/looper control, imports and backups. Only the two narrow V1.1.9 operations linked above have Windows control proofs.
- [ ] Establish reconnect behavior, concurrent-client behavior, and compatibility across firmware versions.
- [ ] Investigate USB separately: this Bluetooth capture establishes no USB control protocol.

App “write successful” logging and ATT write responses only establish transmission/GATT acceptance. They do not prove a command has no side effects, was semantically accepted, or completed a device operation. No complete protocol, firmware update procedure, or safe restore workflow is claimed.
