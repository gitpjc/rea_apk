# Implementation progress

Updated 14 October 2026. The full editor is not complete.

## 14 October: Tone control published and tested

The regular executable now includes **Apply Tone**, restricted to 808 Tone 97–100 in the captured V1.1.9 configuration with Gain 39 and Vol 100 preserved. The staged executable passed actual UI handlers for preset 4 -> 5 -> 4, Threshold 25 -> 26 -> 25 and Tone 100 -> 98 -> 100 before publication to `artifacts/Iamp.Desktop`.

`analysis/private/desktop-ui-tone-oct14.roundtrip.json` reports `passed: true`, `baselineRestored: true`, `failure: null`, with final stage **UI OD808 Tone 98 to 100**. Complete state/detail equality was independently verified through a fresh connection. The phone was not connected to ADB and was not needed for this test. The user confirmed quiet volume/no playback for the controlled Tone investigation. Nothing was saved.

Nineteen .NET tests and five Node capture-parser tests pass; build/publish completed with zero warnings/errors. This supersedes the earlier pending-recheck status below. The viewer also shows bounded labels for 808, PLX 100, Analog delay and Hall reverb; ambiguous mappings remain omitted. See [808-Tone-Editing.md](808-Tone-Editing.md), [Runtime-Effect-Mappings.md](Runtime-Effect-Mappings.md) and [User-Guide.md](User-Guide.md).

## Working build

`artifacts/Iamp.Desktop/Iamp.Desktop.exe` is a self-contained Windows x64 build.
Keep all files in its directory together. The current published compact UI provides snapshot reads and limited experimental Apply/Select controls. Real desktop handlers passed V1.1.9 recall 4 -> 5 -> 4 and Threshold 25 -> 26 -> 25, followed by independent complete-baseline verification. The full editor remains incomplete.
Use `bash scripts/run-desktop.sh` to build/run from source.

Implemented:

- Bluetooth discovery and manual device-address selection.
- Asynchronous information reads, cancellation, error/status handling and a timestamped disconnected snapshot.
- Firmware/capacity display and searchable preset names.
- Current preset name/index and structural decoding of its effect records.
- Read-only effect-chain presentation; the independently corroborated D-Drive fields are Gain, Tone and Volume. Other parameter mappings remain incomplete.
- Shared `NoiseKillerCommand`, `SetNoiseKillerThresholdAsync` and DeviceProbe `threshold-roundtrip`: firmware V1.1.9, position 1 / type 4 / model 2, Threshold restricted to 25-28 while retaining Level 50 and the other observed fields. Both probe and published desktop Apply handler passed hardware round trips.
- Shared `SelectObservedPresetAsync` and DeviceProbe `selection-roundtrip`: V1.1.9 slots 4/5 only, exact current/target preset-detail SHA-256 guards, fresh complete state/detail comparison, preserved fields in the whole-state `13` request and independent read-back. Unknown unsaved edits are rejected. Automatic test restoration uses a fresh BLE session.

General parameter editing, arbitrary preset switching, preset saving, mixer changes, factory reset and firmware updates remain unimplemented. The limited threshold write is an unsaved live edit; guarded recall of two observed presets does not establish arbitrary recall, save behavior or power-cycle persistence.

## Actual validation

- All five supplied APKs passed signature verification. Primary APK is version code 42; phone splits are code 43, both named 1.6.1 and sharing the publisher certificate.
- REA 6.4.0/JADX 0.7.1 Android inspection works on Windows. A separate MCP client verified 170 advertised tools and successfully inspected the phone base APK. The actual Codex chat still needs reconnection to load the new registration.
- Ghidra 12.1.4 through REA successfully analyzed the installed vendor utility. Static USB framing is corroborated; live USB application commands remain unverified.
- The attributed phone capture contains 233 target ATT packets and 79 reassembled CRC-valid application frames.
- Windows independently discovered FFF0/FFF2/FFF3 and read firmware V1.1.2, capacities, all 80 preset names and the current preset (wire index 16, Cali MK3 Lead).
- Android and Windows current-state/detail payloads match byte for byte. The current preset index was unchanged before and after the read.
- The published desktop UI's information-read workflow succeeded against the amplifier and produced `analysis/private/desktop-live.png`.
- Nineteen .NET protocol/preset/threshold/selection/Tone tests and five Node capture-parser tests pass. The 14 October build/publish has zero warnings/errors; the expanded real-handler hardware proof above passed.
- iAMP runs in the isolated Android analysis emulator using ARM64 translation. The x86-64 app package lacks a required native library; the original Pixel 8 virtual disk was preserved.
- User-created MOOER Studio baseline backup verified: source and `analysis/private/backups/F15i-baseline.backup` each contain 294,658 bytes and share SHA-256 `75d3ee78e313025571dc56e913397f09d00f23b4d099461b6fd67dc1e6e086a8`. `Backup-Baseline.md` records the vendor recovery route; restore has not been tested. P0.5 is complete; full vendor evaluation and custom restore remain open.
- Fresh phone reconnect log and independent Windows readback report firmware V1.1.9 and British Lead Solo (phone slot 0D, wire index 4); earlier Windows V1.1.2/Cali MK3 Lead results are historical. Codex performed no firmware update.
- Phone Threshold 25 -> 27 -> 28 -> 25 was corroborated against HCI evidence, with 76 reconstructed frames CRC-valid. Separate `analysis/private/windows-threshold-roundtrip.json` proves Windows 25 -> 26 -> 25; position 1 / type 4 values changed `[1,2,25,50,0,0]` -> `[1,2,26,50,0,0]` -> original. Complete raw preset-detail baseline restoration was independently checked byte for byte; no save occurred and restore used a fresh BLE connection. Only this small noise-gate threshold was changed; low output volume was not measured/confirmed and output gain was unchanged. P3.3a is complete; P3.3's exact low-volume condition and the full P3 gate remain open.
- Phone preset picker 4 -> 5 -> 4 was captured in `analysis/private/phone-preset-switch-logcat.txt` and its Bluetooth evidence. Subsequent independent Windows `windows-preset-return.json` matches `windows-v119-restored.json` in **both complete raw state and complete raw detail payloads**. This confirms the phone test's cleanup separately from the Windows proof below.
- Separate Windows `analysis/private/windows-selection-roundtrip.json` proves preset 4 / British Lead Solo -> 5 / US 65 Deluxe Clean -> 4 / British Lead Solo. `baselineRestored: true`, `saved: false`; **both full raw state and detail payloads match baseline exactly**. Current Threshold 25 / Level 50 and all baseline fields are restored. P3.4a is complete; arbitrary recall, physical-panel change notifications and full P3.4 remain open. See `Preset-Selection.md`.
- The latest `analysis/private/desktop-v119-editor.png` was visually inspected: live-read completion, firmware V1.1.9, 80 preset names reported, current index 4 / British Lead Solo and Noise Killer Threshold 25 / Level 50 are displayed. An earlier read attempt failed while the phone reconnected; this is observed connection contention, not a successful contention/lifecycle acceptance test. No UI Apply action is claimed by the screenshot.

## Required hardware test cleanup

Published executable opt-in `--hardware-roundtrip-preview` passed actual UI handlers Read -> Select 5 -> Select 4 -> Apply 26 -> Apply 25. `analysis/private/desktop-ui-hardware.roundtrip.json` reports `passed: true`, `baselineRestored: true`, `failure: null`; final complete raw state/detail match baseline byte for byte. Cleanup verification used a separate new BLE session. Current preset remains British Lead Solo / wire index 4, Threshold 25 / Level 50, with all baseline values restored and nothing saved. `desktop-ui-hardware.png` was visually inspected and shows passed/full-baseline-restored/nothing-saved status. The helper asserted that returning Threshold to 25 clears the original-snapshot difference and re-enables recall, fixing the normal UI latch. Default startup, `--preview` and `--read-preview` do not perform its writes. P5.5a is complete; broader editing and P3/P5 gates remain open.

The user's explicit policy is to restore **every changed preset value and the originally selected preset after every test**, and verify the result by read-back. Record the baseline first; use complete raw state/detail comparisons where available. If restoration fails or cannot be verified, stop additional mutations and report the unresolved state. Keep the verified vendor backup. No implicit factory reset, firmware update, save or untested backup restore may stand in for exact cleanup.

## Pending prerequisites and next actions

1. Preserve the verified vendor backup and private copy; complete the broader MOOER Studio workflow evaluation separately. Do not equate a documented restore route with a tested restore.
2. Reconnect the actual Codex chat to load the registered REA tools; the scoped setup and external-client readiness checks have passed.
3. Preserve the completed expanded UI hardware proof and verified baseline; continue broader parameter support, persistence/save protocol and connection lifecycle tests.
4. Retain completed threshold and phone/Windows two-slot recall/restore evidence; establish low-volume testing conditions before broader audio-affecting experiments. Expand recall only from independently demonstrated state/guard behavior.
5. Validate further commands only from demonstrated encoding/acknowledgement/read-back semantics and follow the restoration policy above. Arbitrary preset recall, physical-panel notifications, broader reconnect/lifecycle tests and the full P3 hardware gate remain unfinished.

## Tooling preferences

No Python installation was performed. Existing bundled Python is permitted only where an established library or tested binary parser offers a clear advantage. Specialized APK/ELF/PE helpers remain Python; routine setup/configuration, capture parsing and the application use Bash, Node and C#. Bootstrap now uses Git Bash's existing `unzip`; the Windows application has no Python dependency.

See `Plan.md` for task checkboxes and `Feature-Matrix.md` for separate implementation/hardware acceptance.
