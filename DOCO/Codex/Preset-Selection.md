# Preset-selection evidence

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


13 October 2026. A phone-driven selection round trip on firmware **V1.1.9** establishes the observed request structure for **wire index 4 → 5 → 4**. A separate Windows API round trip now confirms selection and complete observed restoration for the same pair. This document's investigation decoded retained evidence only and did not send hardware commands. Desktop selection UI writes have not been separately tested on hardware.

The user requires **every test change to be restored**. Independent Windows reads confirm that the phone round trip returned the complete observed state and preset-detail payload to baseline. Selecting a different preset can discard unsaved current-preset edits; a test must not assume that recalling the original index alone restores those edits.

## Sources and reproduction

Private sources are `analysis/private/phone-preset-switch-logcat.txt`, `phone-preset-switch-bluetooth.txt`, decoded `phone-preset-switch-btsnoop.log`, and target-attributed `phone-preset-switch-capture.json`. Android btsnooz was decoded with the existing official `.tools/android/btsnooz.py` and the already available bundled Python runtime; no runtime was installed. Application/HCI parsing used the existing Node script:

```bash
source scripts/env.sh
node scripts/protocol-capture.mjs \
  --logcat analysis/private/phone-preset-switch-logcat.txt \
  --btsnoop analysis/private/phone-preset-switch-btsnoop.log \
  --output analysis/private/phone-preset-switch-capture.json \
  --fixture analysis/private/phone-preset-switch-all-frames.json
```

The sanitized [f15-observed-selection.json](../../tests/fixtures/f15-observed-selection.json) retains exact state/selection frames, source ordinals, CRC results, preset-detail hashes, screenshot observations and restoration comparisons. A separate Windows section records `analysis/private/windows-selection-roundtrip.json`, its source digest and baseline/changed/restored payload hashes. It excludes device addresses and complete preset libraries. Source SHA-256 digests identify each input.

The capture contains **2,644 HCI records, 436 target ATT PDUs and 134 reconstructed application frames**, all CRC-valid. Two successful LE enhanced connection-complete events attribute the target to handle 2 at record 936 and handle 3 at record 2210. The selection comparison uses the latest preceding state response, records **2426–2427**, in the latter connection. No record truncation or unfinished HCI/L2CAP/application reassembly was detected. Source ordinals refer only to this decoded capture.

## Observed phone workflow

| Step | Phone log evidence | HCI evidence | Corroboration |
| --- | --- | --- | --- |
| Initial current state, index 4 | Current-state/preset reads around 23:06:49 | `12 04 …` state at 2426–2427; `22 04 …` detail at 2431–2433 | `phone-preset-ready.png`: **0D British Lead Solo**, Noise Killer Threshold 25 / Level 50 |
| Select index 5 | 23:07:52.971, line 562 | 92-byte payload `13 05 …` at 2498 | `phone-preset-changed.png`: **1A US 65 Deluxe Clean** |
| Read selected preset | 23:07:53.061, line 563 | Query `21 05` at 2501; response `22 05 …` at 2505–2507 | 144-byte preset-detail payload and changed phone screen |
| Return to index 4 | 23:09:25.521, line 668 | 92-byte payload `13 04 …` at 2630 | Return screenshot shows **0D British Lead Solo** with a loading spinner |
| Read returned preset | 23:09:25.611, line 669 | Query `21 04` at 2633; response `22 04 …` at 2637–2639 | Returned 160-byte detail equals initial detail byte for byte |

The time labels above come from the phone log, without a claim of cross-device clock calibration. The visible bank labels establish **only the two observed associations**, index 4 → 0D and index 5 → 1A. They do not validate a complete bank/letter numbering rule.

## Request structure and exact byte comparison

The request payload is the full **92-byte current-state payload**, with opcode byte 0 changed from `12` to `13` and byte 1 changed to the target wire index. The phone preserves all remaining **90 bytes**, including fields whose meanings are unknown.

| Comparison against initial `12 04 …` state | Changed payload byte offsets | Other differences |
| --- | --- | --- |
| Outgoing `13 05 …`, record 2498 | 0 (`12 → 13`), 1 (`04 → 05`) | None |
| Outgoing `13 04 …`, record 2630 | 0 (`12 → 13`) | None |

The complete outgoing requests are retained in fixture `frames[1]` and `frames[3]`; the exact baseline state is `frames[0]`. Their framing is `AA55 5C00 [92-byte payload] [CRC]`. CRC-16/GSM covers the little-endian length bytes plus payload, with the CRC stored big endian. The observed index-5 request ends in CRC `0A51`; index-4 return ends in `21EB`.

This shows what the phone transmitted. It does **not** establish meanings or safe arbitrary values for the other 90 state bytes, or prove that every one of those state fields behaves identically when another preset is selected. A Windows encoder should copy a fresh, validated current-state snapshot rather than constructing those fields from guessed defaults.

The phone sends a separate preset-detail query after each selection. The evidence does not establish whether that query is necessary to apply the selection or merely refreshes the phone UI. It supplies useful independent verification and should not be confused with the selection command.

## Response candidate

The amp notifies `AA5502001401CD01` at records **2503 and 2635**, payload `14 01`, after the selection and its follow-up detail query. The opcode pairing and timing make it a strong candidate for a selection acknowledgement. The meaning of `01` is unproven; it is not an established success code, selected-index echo or persistence confirmation.

For application success, independently read current state and preset details. Do not treat ATT write acceptance or `14 01` alone as proof that selection completed.

## Restoration and captured-baseline gates

The restored phone screenshot contains a loading spinner, so it alone does not establish completed restoration. The returned phone detail at 2637–2639 equals the initial phone detail at 2431–2433. Separately, `analysis/private/windows-preset-return.json`, read after the phone app was stopped, equals the earlier `windows-v119-restored.json` for **both full `RawStatePayloadHex` and full `RawDetailPayloadHex`**, at wire index 4.

The full decoded payload SHA-256 hashes are:

| Payload | Bytes | SHA-256 |
| --- | --- | --- |
| Current state, baseline and restored | 92 | `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` |
| Preset-4 detail, initial and restored | 160 | `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce` |
| Preset-5 detail after the captured selection | 144 | `56b8474e5064a7d6f20635a13fb5e725ed2d7f5e613b5ea88ab52ea496474714` |

These hashes cover decoded payload bytes, including the response opcode, wire index and all remaining content; they exclude the AA55 frame, length field and CRC. They identify the **captured recall results**, not every factory version of these presets.

A first Windows selection implementation should reject an unknown or modified current-preset detail payload before writing, and restrict targets to the captured index-4/index-5 pair on verified firmware. Otherwise an unsaved change may be discarded and cannot be recovered just by returning to the original index. Matching the current-state payload alone does not detect changed effect parameters: those are in the separate preset-detail payload.

For each authorized hardware test, retain the exact original index, state and detail; perform the bounded test; return to the original index; and compare both full state and full detail against that baseline. If verification fails or a write times out, the operation remains unresolved until fresh reads establish the device's actual state. No automatic replay or success claim should mask an ambiguous result.

## Implementation checklist and limits

### Separate Windows API proof

`F15ReadSession.SelectObservedPresetAsync` and `PresetSelectionCommand` have been implemented and exercised against the amplifier. The API requires firmware `V1.1.9`, a fresh current state/detail that matches the caller's expected snapshot, a 92-byte state payload, and current/target indices restricted to **4 or 5**. Before writing it requires the current full preset-detail hash to match the captured baseline for that index, rejecting unknown or unsaved current edits rather than risking their loss.

The encoder copies the entire fresh current-state payload and changes only opcode/index. After ATT write acceptance, the API independently reads current state and detail. It requires the exact requested index, all 90 preserved state bytes and the captured target-detail hash to match. The target hash is checked on the resulting readback, not treated as an acknowledgement from `14 01`. No preset-save command is sent.

The retained `windows-selection-roundtrip.json` contains a baseline at index 4, changed snapshot at index 5, and restored snapshot at index 4. The changed 92-byte state differs from baseline **only at byte offset 1**, the current index. The selected index-5 detail matches the captured 144-byte detail hash. Both the entire 92-byte state and entire 160-byte detail after returning to index 4 match baseline exactly. The source record reports `baselineRestored: true` and `saved: false`; power-cycle persistence remains untested.

The fixture's `windowsRoundtrip` section records all three state/detail hashes. Fourteen protocol tests pass for the current implementation, including restrictions and captured selection encoding. This validates the narrow API path and its observed round trip, not the desktop UI's actual button workflow, other presets, other firmware, or lossless restoration of unknown unsaved edits. Those remain outside the gate.

### Checklist

- [x] Attribute selection packets to the amplifier and verify all frame CRCs.
- [x] Compare the full 92-byte selection payload against the preceding current state.
- [x] Identify the follow-up detail queries and `14 01` response candidate.
- [x] Confirm that the phone test returned full observed state and detail to baseline through an independent Windows read.
- [x] Implement the restricted Windows encoder/API with fresh-state copying, firmware/index checks and captured-detail hash gates.
- [x] Validate Windows selection **4 → 5** and restoration **5 → 4** through independent full state/detail reads; full observed baseline restored.
- [ ] Exercise the desktop selection UI on hardware and verify its stale-snapshot/error behavior.
- [ ] Determine acknowledgement, rejection and disconnect semantics.
- [ ] Validate other indices/firmwares and complete bank numbering before broadening support.
- [ ] Establish unsaved-edit preservation and preset-save/power-cycle behavior separately.

No preset save, preset replacement, generalized state editing or power-cycle persistence is established. The current evidence supports the tightly scoped Windows selection API with complete observed restoration checks. Desktop UI write validation remains pending.
