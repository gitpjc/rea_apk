# Observed phone same-slot save and edited-value recall retention

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


Current implementation: version1.41.0 adds verified general save/rename/save-as; see [Preset-Save-Rename.md](Preset-Save-Rename.md). The narrow historical observations and limitations below describe the original1.2.0 stage.

14 October 2026. The user explicitly approved one unchanged phone-app save of **0D / wire index 4 / British Lead Solo** on firmware **V1.1.9**. The actual Save dialog displayed that exact name and destination; neither field was edited. Save was opened once and Done was confirmed once. No other slot, parameter, name or firmware was changed.

A later explicitly approved experiment now verifies **Threshold 26 retained after phone save and 5 → 4 recall**, followed by an original-value Threshold-25 save and complete observed restoration. The original unchanged observation below remains a separate stage. No Windows save packet or power-cycle test was performed. Full evidence and the final-recall method deviation are recorded in [Preset-Persistence-Test.md](Preset-Persistence-Test.md).

## Passive sequence

The app log identifies destination 4 and British Lead Solo at 10:43:41.741. Bluetooth capture independently attributes the following sequence to the already identified amplifier connection (connection event record 2937, handle 5). Record numbers are one-based capture ordinals, not timestamps.

| Records | Direction | Payload | Observation |
| --- | --- | --- | --- |
| 3279 | Phone to amplifier | `2F 04` followed by 20 bytes containing the unchanged ASCII name, zero-padded | Request associated with Done in this controlled save workflow |
| 3282 | Amplifier to phone | `30 01` | Response candidate; success/error enumeration remains unknown |
| 3283 | Phone to amplifier | `13 04 …`, 92 bytes | Same-slot state/recall request; every byte after opcode matches the original full state response |
| 3286 | Phone to amplifier | `21 04` | Preset-detail query |
| 3288 | Amplifier to phone | `14 01` | State/recall response candidate |
| 3290–3292 | Amplifier to phone | `22 04 …`, 160 bytes | Reassembled detail response exactly matches the original preset |

The complete save-associated request is `AA5516002F0442726974697368204C65616420536F6C6F0000008BD8`. All six sequence frames have valid existing AA55 length/CRC framing. The retained whole rolling capture contains 3,346 HCI records and 196 reassembled application frames, all CRC-valid; those totals include earlier activity and are not all save operations. The phone app log contains the same outgoing request at line 564. Private raw logs remain excluded from Git.

## Independent verification and cleanup

Fresh Windows reads before and after the phone action match the recorded baseline in both complete raw payloads:

- State, 92 bytes: SHA-256 `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5`.
- Detail, 160 bytes: SHA-256 `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`.

Preset remains British Lead Solo / wire index 4, Noise Killer Threshold 25 / Level 50, 808 Gain 39 / Tone 100 / Volume 100; all 20 block records and unknown/trailing bytes match. No compensating write was needed. iAMP was stopped to release Bluetooth, and the phone's original stay-awake setting was restored to **0** and read back.

Private evidence: `windows-save-approved-before.json`, `windows-save-approved-after.json`, `phone-save-approved-dialog-stable.png`, `phone-save-approved-logcat.txt`, `phone-save-approved-bluetooth.txt`, `phone-save-approved-btsnoop.log`, `phone-save-approved-capture.json`. The bounded proposal and approval history are in [Preset-Save-Test.md](Preset-Save-Test.md).

## Edited-value phone proof and complete restoration

Independent Windows reads in `persistence-live26.json`, `persistence-after-save26.json` and `persistence-recalled26.json` have identical complete Threshold-26 detail, SHA-256 `6d343ac67a0e9175632d00bbc4a89cf3342c170d32d4965e6367f0e9635a259c`. Only detail byte offset 29 differs from the original; full state is unchanged. Phone HCI confirms the save-associated request at **4172**, response `30 01` at **4175**, subsequent phone slot-5 selection at **4563** and slot-4 return at **4635**. Returned detail **4642–4644** still contains the same complete Threshold-26 payload.

The original Threshold 25 was restored live, independently checked, and saved back through the phone. That distinct save request is record **5749**, with `30 01` at **5752** and original detail at **5760–5762**. Both requests use the exact unchanged slot/name envelope shown above; the value to retain resides in the separately established current preset state, not those request bytes.

`persistence-after-save25.json`, the restored stage in `persistence-final-recall25.json`, and `persistence-final-original.json` exactly match the original **92-byte state and 160-byte detail hashes**. Name, slot 4, all 20 block records, companion values and unknown/trailing bytes are restored. The final original-value recall was **existing guarded Windows 4 → 5 → 4**, not the phone recall specified in proposed step 6. It ran only after the exact original profile was restored and saved; the guard was not loosened, and it sent no save packet. The changed-26 recall used the phone as proposed.

The sanitized [f15-observed-persistence.json](../../tests/fixtures/f15-observed-persistence.json) separates save26, phone recall26, restorative save25 and final Windows recall/readback provenance. It includes source hashes and CRC results without full private preset payloads or device addresses. `30 01` remains a response candidate rather than a proven success-code/storage-completion contract.

## Windows implementation boundary and remaining work

Published version **1.2.0** adds `ObservedPresetSaveCommand.CreateForObservedThreshold`, requiring the exact V1.1.9 full state and original complete detail after normalizing only Threshold 25/26. All other changes are rejected. `SaveObservedThresholdAsync` first matches a fresh complete snapshot, sends the observed envelope once, requires the observed response, recalls the same slot and compares every returned byte. The desktop Save button requires a supported live edit and explicit same-slot overwrite confirmation. Failures invalidate further writes; there is no automatic save replay. The unchanged-only constructor remains separately tested.

Sixty protocol tests pass; offline UI checks verify supported enablement and rejection of other parameters, Threshold 27, unsupported firmware, local archives, invalid targets and busy states. The initially rejected Windows test did not execute. After explicit Windows-path approval, the actual UI handlers saved 26 and saved original 25 back, with independent recall and full original baseline verification. The report has passed=true, baselineRestored=true, originalSaveConfirmed=true and failure=null. The regular published app and history now use **1.2.0** and match tested staging. See [Windows-Save-Test.md](Windows-Save-Test.md).

The evidence establishes **phone and Windows same-slot Threshold save/recall round trips with complete observed original restoration**. It does not prove power-cycle durability, that `2F` alone causes retention rather than possible cached live state, other destinations, name encoding beyond this ASCII example, failure responses or a general storage contract. `30 01` alone remains insufficient proof. Further validation must preserve explicit destination confirmation, fresh baseline guards, independent readback and the user's requirement to restore all test changes.
