# Completed edited-value phone save, recall and restoration test

14 October 2026. **Explicitly approved by the user with “proceed”; the bounded phone save/recall experiment and original-value restoration are complete.** Threshold 26 remained present after the phone saved slot 4 and recalled 5 → 4. Threshold 25 was then restored and saved back, with exact complete final state/detail equality. The final original-value recall used the existing guarded Windows selection workflow rather than the proposed phone recall; that deviation is recorded below. No Windows save packet, other-slot save, rename or power cycle was performed.

## Preconditions and scope

- Amp at quiet volume, with no backing track, drums or looper running; retain the user's prior quiet-test condition and stop if that condition changes.
- Firmware V1.1.9, original current slot **0D / wire 4 / British Lead Solo**.
- Independently read and retain complete state/detail immediately before testing; require the exact baseline in [Preset-Saving.md](Preset-Saving.md): state SHA-256 `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5`, detail SHA-256 `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`.
- Verified vendor backup retained as described in [Backup-Baseline.md](Backup-Baseline.md). Its restore remains untested and is not an automatic fallback.
- Only Noise Killer Threshold **25 → 26 → 25** may be edited. Name, destination, every other parameter and unknown byte must remain unchanged. Temporary recall may use only the already observed slot 5 / US 65 Deluxe Clean and return to slot 4. No save to slot 5.
- Use the phone app's observed save UI, inspect exact name/destination before each Done, and capture passive app/HCI evidence. Release competing Windows/phone BLE sessions before independent reads.

## Approved bounded sequence

1. Record the complete starting state and phone setting baseline. Require user approval for this proposal before the first edit intended for saving.
2. Set Threshold to **26** using the existing observed live control. Independently read and require the complete detail to equal the original with only that field changed, and complete state to remain identical. If the phone gesture lands on another value, do not save it; restore 25 and verify the baseline.
3. In iAMP, open Save once; require unchanged name **British Lead Solo** and destination **0D British Lead Solo**. Confirm Done once. No automatic save retry after timeout or ambiguous outcome.
4. Release/reconnect and independently read slot 4. Recall already observed slot **5**, then **4**, and read complete state/detail again. Require exactly the Threshold-26 expected detail and original full state. This tests recall persistence, not power-cycle persistence. The current production Windows recall guard rejects edited profiles; use the phone for this controlled recall and do not loosen that guard for the test.
5. Restore Threshold **25** and independently verify every original byte before saving. Inspect the same unchanged name/destination and perform one original-value save to **0D**. This restorative save is an essential part of the approval scope.
6. Independently read, recall **5 → 4** once more through the phone, and re-read. Require both complete payloads to match the original hashes above. Original slot 4 must be selected at the end. Release iAMP and restore temporary phone settings.

If a save response or recall differs, stop further experiments and assess the actual state through read-only queries. Restore the known original Threshold and slot using demonstrated controls where the target and resulting state can be verified; the proposal authorizes that cleanup and original-value save, not repeated ambiguous saves. If complete restoration cannot be verified, report the exact unresolved state and stop additional writes. Never substitute a reset, firmware update, guessed packet or untested backup restore.

## Deliverables and boundaries

- [x] Concrete edit/save/recall/restore sequence prepared.
- [x] Offline exact-baseline save envelope guard and rejection tests implemented; no transport/UI hardware save exposed.
- [x] Explicit approval covering the edited-value save, temporary recall and original-value restorative save obtained on 14 October 2026.
- [x] Passive changed-value save sequence captured and independently compared with the unchanged observation.
- [x] Recall retention verified with Threshold 26 after the phone save and phone 5 → 4 selection.
- [x] Original Threshold 25 saved back; original full state/detail and selected slot independently verified after the final guarded Windows recall and a fresh read.
- [x] Observed response candidates and remaining acknowledgement/failure/unknown-state limits documented; no Windows save transport/UI implemented by this test.

## Executed evidence and deviation

The sanitized [f15-observed-persistence.json](../../tests/fixtures/f15-observed-persistence.json) retains exact save/recall frame ordinals, directions, checksums, source/log hashes and Windows readback summaries. Raw logs and full private preset payloads remain under `analysis/private`.

| Actual stage | Source and observation |
| --- | --- |
| Baseline / live Threshold 26 | `persistence-baseline.json` and `persistence-live26.json`: state unchanged; only preset-detail byte offset 29 changes from 25 to 26. All 20 block records and other bytes are preserved. |
| Phone save at 26 | PID 27217, `persistence-save26-logcat.txt`: save location/name line 555; outgoing save request line 558 at 11:04:52.041. HCI request `2F 04 …` record **4172**, response `30 01` **4175**. Returned detail records **4183/4186/4188** contain Threshold 26. |
| Independent post-save read | `persistence-after-save26.json`: complete Threshold-26 detail exactly matches the live-26 expected payload; full state remains original. |
| Phone 5 → 4 recall | PID 27500, `persistence-recall26-logcat.txt`: outgoing `13 05 …` line 560 / HCI **4563**, then `13 04 …` line 663 / **4635**. Returned slot-4 detail records **4642–4644** still match the complete Threshold-26 expected detail. `persistence-recalled26.json` independently confirms it through Windows. |
| Restore live value 25 | `persistence-restored25-live.json`: complete state/detail returned to original before the restorative save. |
| Phone restorative save at 25 | PID 31559, `persistence-restore25-logcat.txt`: save location/name line 552; request line 555 at 11:59:55.762. HCI save request **5749**, response `30 01` **5752**, returned original detail **5760–5762**. `persistence-after-save25.json` independently matches the original full baseline. |
| Final recall / fresh original verification | `persistence-final-recall25.json`: existing Windows **4 → 5 → 4** selection reports `baselineRestored: true`, `saved: false`; complete restored state/detail equal original. `persistence-final-original.json` independently confirms the same original baseline through a fresh read. |

**Deviation from approved sequence step 6:** the proposed final 5 → 4 recall was to use the phone. The actual final original-value check used the existing guarded Windows selection-roundtrip **only after the exact original Threshold-25 state/detail had already been restored and saved**. Its original-profile hash guard was not loosened. The edited-26 recall used the phone as proposed, and no Windows save packet was sent. This is a recorded method change, not a claim that the final recall was captured in phone HCI traffic.

The save-26 capture contains **4,192 HCI records, 951 target ATT PDUs and 243 CRC-valid application frames**. Its save connection is event **3778 / handle 6**. The later restore-25 capture contains **5,820 records, 1,341 target ATT PDUs and 344 CRC-valid application frames**; it retains the phone recall connection **4219 / handle 7** and restorative-save connection **5465 / handle 8**. Totals include earlier rolling history. No detected record truncation or unfinished reassembly occurred. Existing bundled Python was used only for the established Android btsnooz decoder; Node performed routine frame/fixture analysis; nothing was installed.

## Complete comparison hashes and limits

Hashes are SHA-256 of decoded payload bytes, excluding framing/CRC:

| Payload | Bytes | Hash |
| --- | --- | --- |
| Full state, original and all slot-4 verification stages | 92 | `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` |
| Threshold-26 detail, live / after save / after phone recall | 160 | `6d343ac67a0e9175632d00bbc4a89cf3342c170d32d4965e6367f0e9635a259c` |
| Original Threshold-25 detail, restored live / after restorative save / final recall / fresh final read | 160 | `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce` |

The two save requests are **byte-for-byte identical** to the earlier unchanged save: `AA5516002F0442726974697368204C65616420536F6C6F0000008BD8`. They encode destination/name, not Threshold 26 or 25. The active value must be established from the prior live state and subsequent full detail/readback, with distinct source ordinals preserving each save occurrence. Both observed responses are `AA55020030010723`; `30 01` is correlated with this working sequence but its success/error enumeration, flash completion and durability semantics remain unknown.

The result establishes **edited-value retention across the tested recall after a phone save**, with complete observed original restoration. It does not prove power-cycle durability or that `2F` alone causes retention: no unsaved-recall negative control distinguishes saving from possible retained live state. Other destinations, renaming, parameters/models, full ranges, cancellation/error/retry behavior and production Windows save transport/UI remain separate work. Every subsequent test must preserve explicit destination confirmation, independent reads and complete restoration.
