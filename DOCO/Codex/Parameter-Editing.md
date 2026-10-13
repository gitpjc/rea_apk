# Parameter-editing evidence: Noise Killer threshold

13 October 2026. This document records a phone-driven threshold change/restoration and a separate **Windows API round trip, 25 → 26 → 25**, on firmware **V1.1.9**. This document's evidence analysis was offline; the Windows operation was performed separately and retained as a runtime record. The result establishes a narrowly scoped Windows write/readback path, with general editing, preset saving and power-cycle persistence still unresolved.

## Evidence retained

The source files are private and excluded from Git:

- `analysis/private/phone-threshold-roundtrip-logcat.txt`: phone application logs, including writes at lines 621, 623 and 625.
- `analysis/private/phone-threshold-bluetooth.txt`: Android Bluetooth dump containing btsnooz; decoded with the existing official `.tools/android/btsnooz.py` and the already available bundled Python runtime. No Python installation was performed.
- `analysis/private/phone-threshold-btsnoop.log`: decoded HCI capture.
- `analysis/private/phone-threshold-capture.json`: target-attributed ATT and reconstructed application frames, produced with the existing Node parser.
- `analysis/private/phone-edit-before.png`, `phone-edit-threshold3.png`, and `phone-threshold-restored.png`: visually inspected screenshots showing Threshold **25 → 28 → 25**, with Level **50** throughout, under **Dyna / Noise Killer**.
- `analysis/private/windows-v119-restored.json`: independent Windows readback after restoration, current wire index 4, block position 1, type 4, values `[1, 2, 25, 50, 0, 0]`.
- `analysis/private/windows-threshold-roundtrip.json`: separate Windows API baseline, changed and restored current-preset snapshots; Threshold **25 → 26 → 25**, Level **50**, wire index **4** throughout. The restored full preset-detail payload equals its baseline byte for byte.

The sanitized, tracked fixture [f15-observed-threshold.json](../../tests/fixtures/f15-observed-threshold.json) contains the three distinct phone edit frames, one response candidate with three occurrence ordinals, screenshot observations, restored numeric block and source SHA-256 digests. Its separate Windows section includes baseline/changed/restored full preset-detail payload hashes and changed byte offsets; it does not fabricate Windows HCI frame evidence. It contains no device address or preset library contents. Source ordinal numbering is specific to the phone capture.

Reproduce the packet analysis after decoding the retained btsnooz:

```bash
source scripts/env.sh
node scripts/protocol-capture.mjs \
  --logcat analysis/private/phone-threshold-roundtrip-logcat.txt \
  --btsnoop analysis/private/phone-threshold-btsnoop.log \
  --output analysis/private/phone-threshold-capture.json \
  --fixture analysis/private/phone-threshold-all-frames.json
```

## Confirmed observations

The decoded capture contains **1,782 HCI records**, **229 target ATT PDUs**, and **76 reconstructed application frames**, all with valid CRC-16/GSM checksums. Successful LE enhanced connection-complete event 936 attributes connection handle 2 to the amp identified in the app's GATT log. There were no detected truncated records, unfinished L2CAP PDUs or unfinished application frames. Discovery and notifications use the same FFF0 service / FFF3 write / FFF2 notify UUID pattern documented in [Protocol.md](Protocol.md). The session's write value handle is 11 and notification value handle is 8; production connections must discover these dynamically.

The phone transmitted these application frames:

| Action observed | Phone log time and line | HCI record | Complete frame, hexadecimal |
| --- | --- | --- | --- |
| Intermediate threshold 27 while adjusting | 22:57:08.332, line 621 | 1717 | `AA5510002501040C010002001B00320000000000AC64` |
| Final threshold 28, corroborated by screenshot | 22:57:08.421, line 623 | 1720 | `AA5510002501040C010002001C003200000000006B7C` |
| Restore threshold 25, corroborated by screenshot and Windows readback | 22:57:24.711, line 625 | 1779 | `AA5510002501040C01000200190032000000000023C2` |

Times above are labels from the phone log; no cross-device clock calibration is claimed. The payload length field is `1000` (16 bytes, little endian). CRC coverage remains the two length bytes plus payload; the two-byte CRC trailer is big endian. The CRC model is polynomial `0x1021`, initial value zero, no reflection, final XOR `0xFFFF`.

Only one 16-bit value changes among the three payloads. Their common structure is:

```text
25 01 04 0C  0100 0200 TT00 3200 0000 0000
│  │  │  │    └──────── six little-endian 16-bit values ────────┘
│  │  │  └─ 12 value bytes
│  │  └──── type code 4 in this snapshot
│  └─────── block position 1 in this snapshot
└────────── observed edit-command discriminator
```

Here `TT` is `1B`, `1C`, or `19` (27, 28, 25). The payload contains a complete block representation after discriminator `25`: position, type, byte count and all six values. It contains no separate preset index field established by this evidence. The surrounding session was on current preset wire index 4.

The screenshot labels and changed bytes support **type 4 / second value 2 / six-value block → Noise Killer**, with **Values[2] → Threshold** and **Values[3] → Level**, for this captured model and firmware. This association is corroborated by the restored Windows readback. `Values[0]`, `Values[4]` and `Values[5]` were preserved; their meanings remain unknown. In particular, this capture does not establish an enabled-state flag, parameter units, or permitted ranges.

## Response candidate and limits

The amp sent `AA5502002601AEF6` on the notification characteristic at records **1722, 1724 and 1782**. Its payload is `26 01`, with a valid CRC. These responses occur after the threshold writes and are a strong candidate for edit acknowledgements.

However, records 1717 and 1720 are both transmitted before either response 1722 or 1724. Both responses are identical and contain no demonstrated request identifier or echoed threshold. Consequently, assigning either response to one particular rapid edit is not proven. The isolated restore request at 1779 is followed by response 1782 about **141 ms** later using the HCI capture's relative timestamps.

The meaning of response byte `01` is unknown: it is **not yet an established success code**, block identifier, or persistent-save confirmation. ATT acceptance and an application response alone do not prove the requested value was applied. Restoration is separately supported by the phone screenshot and Windows readback of Threshold 25 / Level 50.

## Implementation boundary

### Separate Windows API validation

`F15ReadSession.SetNoiseKillerThresholdAsync` has been implemented and exercised against the amp through the Windows API round trip. It performs fresh firmware/current-preset reads, checks that the current wire index and entire preset-detail payload still match the supplied snapshot, and gates writes to firmware `V1.1.9` and the exact captured first block: position 1, type 4, six values `[1, 2, threshold, 50, 0, 0]`. Both the existing threshold and requested threshold must lie within the deliberately narrow **25–28** implementation range. This is an implementation restriction, not the device's demonstrated full range.

The encoder preserves every other block field and uses observed command discriminator `25`. The API waits for ATT write acceptance, then independently reads the current preset again and compares **every preset-detail byte** with the expected single-field change. It does not use the unproven `26 01` response as a success criterion and does not send a preset-save command.

The separate retained Windows snapshots show Threshold 25 at baseline, 26 after the Windows write, and 25 after restoration. Level stays 50. Only raw preset-detail byte offset **29** differs between baseline and changed snapshots; all **160** restored preset-detail payload bytes exactly match the baseline. The sanitized fixture stores SHA-256 hashes of these decoded payload bytes, rather than the preset name or full preset contents.

The phone's changed value **26 was not independently inspected** during the Windows round trip. The inspected phone screenshots establish the earlier 25 → 28 → 25 phone round trip and its restored value 25. This API validation is not a desktop UI hardware test, an audio-behavior test, an error-response test, or proof of persistent storage. Twelve protocol tests pass for the current implementation, including command restrictions and captured-frame behavior; those tests do not replace hardware or UI coverage.

### Checklist

- [x] Identify the phone's threshold-change frame and restoration frame with log/HCI provenance.
- [x] Validate CRCs and compare all preserved block fields.
- [x] Corroborate the final restored values with a separate Windows readback.
- [x] Implement a narrowly scoped Windows encoder/API that starts from a fresh current-preset/block snapshot and preserves every other field.
- [x] Confirm Windows edits using a fresh current-preset read and full expected preset-detail byte comparison, independently of the candidate acknowledgement.
- [x] Verify a Windows API-driven change **25 → 26** and restoration **26 → 25** against independent current-preset reads.
- [ ] Verify the desktop editor's apply workflow on hardware and independently inspect a Windows-changed value on the phone UI.
- [ ] Complete serialization/state handling across the entire editor transaction and distinguish transport completion, candidate acknowledgement and independent readback confirmation in the UI.
- [ ] Determine response `26` semantics, rejection behavior and timeout recovery without automatic ambiguous-write replay.
- [ ] Verify allowed ranges and model/firmware compatibility. Do not extrapolate the three observed values into a full range.
- [ ] Investigate preset-save behavior and power-cycle persistence separately. No save operation or persistence test is demonstrated here.

No general effect switch, model change, preset replacement, flash write, firmware update or save command is established by these round trips. The desktop editing feature should remain scoped to the specific validated block/model and should report the independently read value rather than treating the candidate response as proof of success.
