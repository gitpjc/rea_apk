# Proposed unchanged same-slot preset save test

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026. **The explicitly approved bounded phone test is now complete.** Save was opened once and Done confirmed once with the existing unchanged **0D / wire index 4 / British Lead Solo**, on firmware **V1.1.9**. Complete original state/detail were independently verified afterward. See [Preset-Saving.md](Preset-Saving.md). Approval did not authorize another destination, renaming, parameter changes, raw Windows save packets, firmware operations or backup restoration.

## Approval boundary and rejected action

The coordinator reported that automatic approval review **rejected the proposed ADB save-icon tap at (950, 345)** because the action could immediately persist or overwrite an amplifier preset. **That tap was not executed.** The current application's behavior on its first Save tap has not been established; the old manual is insufficient to guarantee it is only navigation.

The user explicitly approved this bounded test on 14 October 2026 by answering **yes** to the request to save British Lead Solo unchanged to its existing slot 0D and verify the complete original state. That approval covers the proposed initial Save tap and unchanged same-slot confirmation. It does not broaden the test. Automatic review still applies; another rejection stops the operation and must be reported.

## What the existing evidence supports

| Evidence | Supported observation | Boundary |
| --- | --- | --- |
| Supplied manual, printed p.46 / PDF p.49 | The illustrated Save icon opens a dialog with a name field, destination row and Cancel/Done buttons. The text describes choosing a name/location and completing the save with Done. | This is the June 2025 manual, not runtime proof of the extracted 2026 app's first-tap behavior. |
| Manual illustration on that page | Current preset **1A US 65 Deluxe Clean** and the shown dialog destination are both **1A US 65 Deluxe Clean**. | An example of the same target, not a verified rule or default for the current app. |
| `analysis/private/phone-save-before.png`, independently inspected | Current phone UI shows **0D British Lead Solo**, Noise Killer Threshold 25 / Level 50 and the Save icon. No save dialog is shown. | The destination/default/confirmation behavior remains unknown because the rejected tap was not run. |
| Retained Flutter triage, phone code43 | The packaged AOT library contains F15 save logic/view Dart route strings. | String inventory only: no recovered handler, control flow, wire command, success code or destination default is established. |
| `analysis/private/windows-save-baseline.json` | Firmware V1.1.9, wire index 4, British Lead Solo, 20 parsed blocks, complete baseline state/detail hashes below. | Read-only current state; not proof of save completion or power-cycle persistence. |
| [Backup-Baseline.md](Backup-Baseline.md) | Vendor backup source and private copy were verified to match. | Restore completeness, compatibility with the current firmware/state and successful recovery remain untested. |

The manual was text-extracted using the existing bundled `pypdf` library and visually inspected after rendering PDF p.49 with bundled Poppler. No Python or other runtime was installed. The manual SHA-256 is `4c2f24cbcb7147e24b9d5c010259e241146db5d5be51dcdeab883beb1a420117`.

Retained save route provenance from `analysis/generated/flutter-triage.json`:

| Artifact | Route-string file offsets within `libapp.so` | Library SHA-256 |
| --- | --- | --- |
| Phone code43, `lib/arm64-v8a/libapp.so` | `.../ui/home/save/f15_save_logic.dart`: `0x3BFEE`; `.../f15_save_view.dart`: `0xC4EDD` | `c9cf17fdc2ef2c281fe8df82734e065c59193d962a730052b50b6805fba6befb` |
| Primary code42, `lib/x86_64/libapp.so` | Logic: `0x3C04B`; view: `0xC4F5C` | `71a49fbc35e1dc54d97dd57f643c0fe25c35e87c7b4471470a5e21cb79c69006` |

The complete route prefix is `package:flutter_iamp_app/device/future_series/f15i/`. These are file offsets, not recovered function addresses. No source/decompilation result or new REA native finding is claimed. Generated supporting inventory and rendered manual evidence are under `analysis/generated/preset-save-proposal-evidence.json`, `manual-save-evidence.json` and `manual-save-page49.png`.

## Exact unchanged baseline

| Item | Required value |
| --- | --- |
| Firmware | **V1.1.9** |
| Current and proposed destination | **0D / wire index 4** |
| Name, including spelling and spaces | **British Lead Solo** |
| Noise Killer | Threshold **25**, Level **50** |
| 808 | Gain **39**, Tone **100**, Vol **100** |
| Parsed preset blocks | **20**, including all populated/empty slots; every field and unknown/trailing byte must remain unchanged |
| Complete current-state payload | **92 bytes**, SHA-256 `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` |
| Complete preset-detail payload | **160 bytes**, SHA-256 `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce` |
| Baseline source JSON | `analysis/private/windows-save-baseline.json`, SHA-256 `a93f4bf58d7f7c50ad88d8d23ff69fce48620b62eda577de8606603141524110` |

Payload hashes cover decoded `RawStatePayloadHex`/`RawDetailPayloadHex` bytes, excluding AA55 framing, length and CRC. The detail matches the pristine captured recall profile already verified during index-4 restoration. These hashes compare the whole observed configuration, not merely the five editable values.

The vendor-created backup source recorded in [Backup-Baseline.md](Backup-Baseline.md) is `C:/Users/Paul/Documents/Mooer Studio For Future Series/BackupFiles/F15i/2026-10-13 18_34_47.backup`. Its verified private copy is `analysis/private/backups/F15i-baseline.backup`, **294,658 bytes**, SHA-256 `75d3ee78e313025571dc56e913397f09d00f23b4d099461b6fd67dc1e6e086a8`. The copy's hash was rechecked during this offline preparation. Its existence does not justify an automatic restore or guarantee recovery.

## Bounded action proposed for approval

1. **Require explicit approval before the first Save tap.** Approval must cover the possibility that the initial tap itself performs an unchanged save, as well as one same-slot confirmation if the documented dialog appears. Passive evidence capture and read-only verification are distinct from a save command.
2. Immediately before the approved attempt, verify the complete baseline above through read-only state/detail reads and the phone's visible current slot/name. Stop if any firmware, index, name, parameter or full-payload hash differs. Release competing Bluetooth sessions before the phone workflow. Make no parameter, name or selection change to prepare the test.
3. Capture the initial phone screen and passive application/HCI evidence. If the initial Save tap passes review and is executed, inspect its result before any next action. **Do not assume that opening Save is side-effect free.** If no recognizable reviewable dialog appears, do not tap again or retry; obtain read-only state and report the outcome.
4. If a dialog appears, confirm that its destination is already **0D / British Lead Solo**, and that its name field is exactly **British Lead Solo**. Keep both unchanged. **If the destination/name differs, the dialog is ambiguous, or an unexpected prompt appears, do not confirm or select another slot; stop and report.** Do not navigate through other preset destinations under this proposal.
5. Only if all checks match, the explicit approval covers confirmation and automatic review permits it, perform **one** unchanged same-slot save confirmation. Never retry an ambiguous or timed-out save. No rename, changed tone, other-slot overwrite or raw Windows/BLE save packet is part of this test.
6. Inspect passive evidence and take fresh independent read-only Windows state/detail snapshots after the phone releases its connection. Require the exact original name, index and both full payload hashes. A normal reconnect/read may be recorded; **no power cycle or broad preset cycling is included**. Treat a UI success message or response packet as evidence to analyze, not sufficient proof of storage semantics.

This attempts to observe one unchanged save, not to persist an experimental sound. Because all name/parameter/block values remain identical, even a successful unchanged save cannot demonstrate that a changed value survives a power cycle or prove full save semantics.

## Stop, restoration and reporting policy

The intended final configuration is exactly the starting baseline, so no compensating parameter or preset write should be necessary. If any readback differs, stop further write operations, retain the evidence and report **baseline restoration unresolved**. Do not guess a repair packet, re-save, recall another preset, restore the vendor backup, reset the amp or update firmware automatically. Any recovery action must be separately concrete and authorized; the existing backup's restore route is documented but untested.

Status:

- [x] Manual Save page text/render inspected; historical workflow/default example distinguished from current-app unknowns.
- [x] Retained save route strings and artifact provenance recorded without inventing handler semantics.
- [x] Exact unchanged baseline and verified backup copy identified.
- [x] Rejected action and the approval boundary recorded.
- [x] Explicit approval for the bounded unchanged same-slot attempt obtained on 14 October 2026.
- [x] Initial Save action executed once; actual current-app dialog showed the unchanged name and same 0D destination. Absence of every possible first-tap side effect is not claimed.
- [x] Same-slot unchanged confirmation executed once with explicit approval and permitted review.
- [x] Passive save evidence analyzed and independent full baseline readback confirmed.
- [ ] Changed-value saving, save acknowledgement semantics and power-cycle persistence validated separately.

**Current result: approved unchanged same-slot phone workflow captured; independent complete baseline equality confirmed before/after. iAMP stopped and phone stay-awake restored to 0. Changed-value persistence and production Windows saving remain unverified.**
