# F15i Li protocol evidence ledger

Reconciled 17 October 2026 for desktop 1.41.4 and F15i V1.1.9. This is a bounded implementation/evidence ledger, not a complete vendor specification. [Current-Status.md](Current-Status.md) states delivered scope; per-feature documents and sanitized fixtures contain the detailed commands and tests. The original V1.1.2 capture is retained in the historical section below.

## Implemented command families

All opcodes below are hexadecimal payload bytes, excluding the AA55 envelope. Wire preset indices 1–80 map to 0A–19D. Current state is 92 bytes; detail includes a 20-byte name and 20 positional records with variable-length values plus preserved trailer bytes. Outgoing effect records encode values little-endian; incoming preset detail words are parsed big-endian. Do not infer wire identifiers from manual serial numbers.

| Request → observed reply | Implemented use | Evidence and boundary |
| --- | --- | --- |
| 01 01 → 02 | Identity, firmware and reported capacities | Windows reads corroborated Android capture. Current writes require V1.1.9 and appropriate supported capability/layout. |
| 29 start end → 2A | Names 1–40 and 41–80 | All 80 names and bank labels verified; names are not an effect snapshot. |
| 11 →12, 21 index →22 | Current state and indexed effect detail | Read state/detail/state as one serialized workflow; require matching state and detail index. From 1.41.4, retry only fully received inconsistent reads, at most three attempts with 100 ms settling; never retry transport failures or mutations. A live snapshot alone does not prove saved storage. |
| 25 + full effect record → observed 26 01 | Parameter and enabled-state changes at an explicit position | All 124 requested model layouts have evidence. Complete readback, not ATT delivery or the observed acknowledgement alone, establishes success. See the eight model documents. |
| 2D token operation ... | Add/remove/move/replace effects | Operations 1/3/4/2 respectively, with captured positional layouts and process-wide token bands. See [Effect-Chain-Editing.md](Effect-Chain-Editing.md). No unknown-operation fuzzing. |
| 13 + full state →14 01 | Preset recall and the separately verified Mixer fields | Preserve all unrelated bytes; check full state/detail after the operation. All 80 recall destinations and four Mixer channels have bounded hardware evidence. |
| 2F destination name[20] →30 01 | Save current live content, rename, or Save As | Name is 1–20 printable ASCII characters, padded to 20 bytes. Source/destination freshly checked; one save, post-save 13/14 recall, exact expected state/detail. No automatic save replay. |
| 91 →92 | Battery percentage | Percentage byte is corroborated on V1.1.9; power-state/charging semantics stay opaque. |
| unsolicited 9B | Defensive transfer-state detection | Invalidate the session, inspect complete notification batches and block later/queued writes. The historical mobile route-loop trigger is unresolved; no root-cause fix is claimed. |

The current-state Mixer offsets/conversions are documented in [Mixer-Editing.md](Mixer-Editing.md). They do not establish global EQ/input gain or other Phase 6 settings. Parameter families are Dyna (type 4), Filter(5), OD(1), Amp(2), preset EQ(9), Mod(6), Delay(7) and Reverb(8); together 124 models and 126 shared parameter types. Opaque/reserved words are retained, not guessed. The observed Boolean Amp ninth-word side effect has only the bounded allowances described in the chain/on-off documents.

## State, persistence and failure policy

The frame envelope uses AA55, a little-endian payload length and big-endian CRC-16/GSM over length plus payload. Fragmentation and checksums are covered by retained independent fixtures and bounded parsers. Original capture counts below are historical, not the entire current fixture corpus.

The production command dispatcher serializes requests; WorkflowQueue serializes complete identity/read/compare/write/readback transactions. Active cancellation, timeout, disconnect, disposal, transfer status and workflow failure stop that session. Queued cancellation is isolated. A complete-read consistency mismatch may settle within three read-only attempts before leaving the workflow. Exhaustion and genuine command failures remain terminal. A new session is required after terminal failure; cross-process coordination is a user/testing policy, not a claimed system-wide lock.

The desktop polls current state about every 3 seconds and battery about every 30 seconds while connected and eligible. Pending inputs are separate from confirmed values. External changes that conflict with pending input preserve that input and stop editing until refresh. Unsolicited physical-panel-originated synchronization remains an acceptance task.

A query of the active preset can describe live unsaved values. Save proof and restoration must distinguish active/live from stored content, preserving the user's fresh original state. General saving uses PresetSaveCommand and F15ReadSession.SavePresetAsync, defined in F15ReadSession.PresetSave.cs; the older ObservedPresetSaveCommand/SaveObservedThresholdAsync are retained legacy narrow APIs, not current desktop save restrictions. A full save request contains destination/name, not reconstructed effect bytes: the amplifier stores its current chain.

Save failure recovery may create one new read-only session to reload actual state and restore controls. It never repeats a save. Transfer-state changes and unsuccessful recovery require explicit reconnect. Human time in the overwrite dialog does not consume the device-operation deadline. Chain commands alone permit one fresh-token attempt after a successful exact read proves **no change**; no failed or ambiguous read allows that retry.

## Current verification and remaining scope

All 80 recalls, all 124 per-model parameter/on-off profiles, representative repeated/moved/replaced chains, four-channel Mixer, general save/rename/save-as, saved edited-copy physical power-cycle persistence and P5.8 clean/rhythm/lead workflows have retained evidence with exact restoration. P5.8 used both actual desktop save dialogs and compares mobile parameters plus user-confirmed amplifier bank/name; see [Musician-Workflow.md](Musician-Workflow.md).

Remaining limits include reserved fields, broad fault/late-reply behavior, physical-panel-originated changes, exhaustive chain rejection/limits, other firmware and USB control. GIR&IR is excluded; Phase 6 requires explicit authorization. Drum/looper/global settings/cloud/backup transfer commands are not implemented or authorized by this ledger. User-reported vendor restore is not a custom-format implementation proof.

## Historical V1.1.2 capture — 13October 2026

The sections below preserve the original single-session interpretation and open questions at that date. Their “candidate”, “unknown” and unchecked items describe that evidence only; later core mappings are summarized above. They are not current task authorization or a replay list.

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
| `AA550100915B57` | Repeated at roughly 30-second intervals in logcat; responses start `92` with varying values. Battery percentage in the second payload byte is now corroborated by phone logs and Windows V1.1.9 readback; the third power-state byte stays opaque. See `Battery-Information.md`. |

iAMP logs report firmware `V1.1.2`, 80 presets, 10 GIR slots, 10 GNR slots, 70 drums and 10 looper files. These counts are firmware-reported/session observations, not hardcoded product-wide guarantees. Responses beginning `66` follow requests beginning `65` and include “Empty” or drum names; record layouts and category meanings remain partially inferred.

## Historical remaining work at the initial capture

- [x] Decode the retained capture with connection attribution and length checks.
- [x] Extract sanitized outgoing fixtures and establish the checksum across all observed logged writes.
- [x] Reassemble captured notification frames and verify all 79 checksums.
- [ ] Independently compare the candidate queries against controlled single-action captures and document response schemas.
- [ ] Identify every parameter, index convention, error response, sequence/timing requirement and maximum frame size.
- [ ] Complete mappings for preset changes, effect edits, drum/looper control, imports and backups. Only the two narrow V1.1.9 operations linked above have Windows control proofs.
- [ ] Establish reconnect behavior, concurrent-client behavior, and compatibility across firmware versions.
- [ ] Investigate USB separately: this Bluetooth capture establishes no USB control protocol.

App “write successful” logging and ATT write responses only establish transmission/GATT acceptance. They do not prove a command has no side effects, was semantically accepted, or completed a device operation. No complete protocol, firmware update procedure, or safe restore workflow is claimed.

## Tuner — verified 18 October 2026

See [Tuner.md](Tuner.md) for observed Tools entry `4101`, request/reply `4900/4A01`, tuner entry `4102`, exit `4100`, acknowledgement `4201`, reported modes `4300/4302`, note/meter notifications `45` and whole-state settings offsets 44/45. Production uses a tested three-second quiet guard before entry; this is not a decoded firmware timing constant. Readings are change-driven and explicit `450000` clears the display. No cents mapping is established.
