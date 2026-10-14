# Implementation progress

Updated 14 October 2026. The full editor is not complete.

Scope update, 14 October: the user explicitly excluded all Phase 6 work unless separately instructed. No Phase 6 investigation, implementation or hardware test is authorized by a general request to continue. Remaining core MVP, connection reliability and release verification work stays in scope. That scope restriction remains in effect for subsequent versions.

## Latest: inline preset dropdown in 1.6.3

Version 1.6.3 places the compact preset dropdown on the same row as the Presets heading/count, between the heading and search. The dropdown is capped at 440 pixels. Search wraps below at smaller widths, and very narrow layouts also wrap the dropdown to keep controls usable. Focused offline checks cover header geometry at window widths 1280, 1050, 800 and 620, dropdown checkmark placement, selected-field contents, filtering, clearing and focus. The full suite was not rerun for this layout-only change. No screenshots or amplifier/phone access; Phase 6 remains excluded.

## Earlier: preset checkmark and search clearing in 1.6.2

Version 1.6.2 shows the active blue checkmark only inside the open preset dropdown; the selected-item field shows bank/name without a checkmark. Search now has a right-side × button that appears only when text is present, clears the filter and returns focus to the input. Focused offline UI checks cover popup check color/position, absence of a check in the selected field while open/closed, empty/text/whitespace visibility, clearing/restoring all presets, no-match filtering and focus. The full test suite was not rerun for these isolated UI changes. No screenshots, phone or amplifier access, or Phase 6 work.

## Earlier: compact preset dropdown in 1.6.1

Version 1.6.1 moves the four amplifier information cards above Presets and replaces the fixed-height table with a compact dropdown. Each entry shows its bank label/name, with a #4385ff checkmark to the left of the active amplifier preset. Existing guarded recall and unsaved-discard confirmation remain in use; Escape cancels browsing. Offline rendering verified the real popup, exactly one active checkmark and its color/left position. All 137 .NET/five Node tests and responsive, snapshot and history checks passed. No phone or amplifier commands were sent; Phase 6 remains excluded.

## Earlier: mobile-style preset browser and retained connection in 1.6.0

Version 1.6.0 adds mobile-style 0A–19D preset labels, device-reported names, current-row highlighting and click/Enter recall across all 80 slots. Scan/select automatically connects and loads the library; the retained connection refreshes current state every three seconds. All 79 alternatives were recalled through Windows handlers and original 0D was restored after each, with complete raw payload equality. Automatic refresh, pending-input preservation and automatic scan/connect passed hardware checks; final independent readback matched every original byte. The user then reconnected phone iAMP and reported a stable normal effects screen. No parameter edits or saves were sent. All 137 .NET and five Node tests pass with zero build warnings/errors; offline snapshot/history/comparison/diagnostics/layout/save guards pass. Saving and parameter ranges remain restricted, the mobile loop cause remains unresolved, and Phase 6 remains excluded. See [Preset-Browser.md](Preset-Browser.md).

## Earlier: physical power-off exposed cleanup bug; fix verified in 1.5.3

The approved read-only power-off test first failed with ObjectDisposedException while closing the disconnected session. A fresh read after restart still matched the complete original baseline. Added shared teardown handling for closed/cancelled native operations with guaranteed attempts to release both resources, plus late-callback guards. The repeat power-off test passed: Windows reported Disconnected, cleanup finished, and the follow-up identity call was blocked without another transport request (23→23). After restart the actual desktop Read and an independent fresh read matched every original state/detail byte, index 4 and Threshold 25. No edits, recalls, saves or phone setting changes occurred. See `Physical-Disconnect-Test.md` and P3.5g.

All 129 .NET/five Node tests pass with zero warnings/errors. Five cleanup cases cover expected and unexpected failures; the final cancelled-native-operation variant was tested offline after the successful physical run. Local history/snapshot/comparison/diagnostics regressions pass. Version 1.5.3 is published to the usual folder with binary/history verification. Connections are closed and phone iAMP may reconnect. Broader Bluetooth/sleep/interrupted-mutation cases and the mobile loop cause remain unresolved.

## Latest: complete workflow serialization in 1.5.2

Each Windows session now serializes whole public read/edit/recall/save workflows around the existing command dispatcher. Private nested read methods avoid re-entrant queue deadlock. Queued cancellation is isolated, while active failure stops later workflows. Eight new offline concurrency cases pass; all 124 .NET/five Node tests pass with zero build warnings/errors.

With phone iAMP disconnected by the user, real concurrent current-preset/name/current-preset workflows completed 12 read-only commands without splitting their groups and loaded 80 names. The actual desktop Read then completed six commands in a fresh connection; a final independent read matched every original state/detail byte. Diagnostics showed no mutation categories or transfer events and recorded disposal. Original slot 4/Threshold 25 remains unchanged. See `Workflow-Serialization.md` and P4.4b. Windows connections are closed; the user was told phone iAMP could reconnect. No new phone-handoff observation was taken for this version.

Version 1.5.2 is published to the usual folder with binary/history verification and offline local-feature checks. No parameters, recalls, saves or phone settings were changed. Physical fault/write regression and the original mobile loop cause remain open.

## Latest: shared dispatcher passed read-only hardware regression in 1.5.1

Follow-up completed: the user reconnected phone iAMP after the Windows read and reported its normal effects screen stable. P3.5f is complete for this observed read-only handoff; there was no measured passive phone capture or further hardware action. This supersedes the pending observation below and does not resolve the original loop trigger. Documentation/history updated without a code or application version change.

The user confirmed hardware readiness and disconnected phone iAMP/closed MOOER Studio. Independent current-state read → actual desktop Read → fresh independent read all matched the complete original 92-byte state and 160-byte British Lead Solo detail. Original index 4, Threshold 25 and every other field remain unchanged. The current dispatcher completed all six desktop read commands and disposed normally; exported diagnostics contain no mutation categories or transfer-status events. See `Read-Only-Regression.md` and P3.5e. This is normal-path acceptance only; forced fault paths and the loop trigger remain open. Phone reconnect usability has been requested and is pending the user's observation.

Added the explicit read-only evidence helper in 1.5.1, using actual Read and local export handlers and refusing existing output paths before connecting. Source/package checks, 116 .NET/five Node tests and local export regressions passed with zero warnings/errors. The tested package is published to the usual folder with binary/history verification. No parameters, preset recall/save operations or phone settings were changed. The feature matrix was updated to distinguish implemented local tools/narrow save evidence from remaining parent gates.

## Latest: compact desktop layout and keyboard navigation in 1.5.0

The main window now fits its initial work area and permits compact resizing. Header/search reflow, input sizing, adaptive card columns and a scrollable sidebar keep controls available in smaller windows; preset-name width adapts while index alignment remains intact. Ctrl+1/Ctrl+2 navigate library/history and Ctrl+F reveals/selects preset search. Buttons show keyboard focus and key controls have accessible names. No hardware actions are assigned to these shortcuts.

Offline routed-command/state-preservation and horizontal-bound checks passed at 1366×768, 911×512, 683×384 and 1920×1080 logical pixels. Compact/normal render images were inspected. These approximate content space at common scaling levels and are not physical DPI-switch acceptance. All 116 .NET/five Node tests pass with zero build warnings/errors; existing local snapshot/comparison/diagnostics/history regressions pass. Version 1.5.0 is published to the usual folder with binary/history verification. See `Desktop-Usability.md` and P5.2b; actual DPI/Narrator/keyboard and full dialog checks remain open. No phone/amplifier commands were sent and no settings changed.

## Latest: read-only snapshot comparison in 1.4.0

Added Compare snapshot beside Open snapshot. It validates a local file and compares it with the displayed snapshot without connecting, applying data or replacing editor state. Complete raw state/detail equality is reported separately from firmware; differences include opaque fields, name padding, absent effect fields and unknown trailer data. Capture time is excluded from value differences. Seven new protocol cases and actual offline UI comparison/error/preservation checks pass. All 116 .NET/five Node tests pass with zero build warnings/errors; existing snapshot/history/diagnostics checks pass. The dialog's initially compressed columns were corrected and visually verified. See `Snapshot-Comparison.md` and P5.7b.

Version 1.4.0 is published to the usual folder with source/package history verification and staging/published binary comparisons. The published comparison workflow passes. No phone/amplifier commands were sent and no presets or settings changed. Applying imports, broader control coverage and physical lifecycle acceptance remain open.

## Latest: local connection diagnostics in 1.3.0

Added sidebar Export diagnostics, a 256-event in-memory journal and typed instrumentation in Windows connection establishment/disconnect/cleanup and the production command dispatcher. Export contains application version, UTC times, operation/outcome categories, elapsed milliseconds and dropped-event count. It excludes addresses, device/preset names, values, raw packets, paths and exception text; no automatic file write/upload occurs. Nine new tests cover privacy, bounded retention, concurrency, validation and failure classification. All 109 .NET/five Node tests pass, with zero Release warnings/errors. See `Diagnostics.md` and P4.6b.

Actual offline export using simulated events passed privacy and snapshot-preservation checks; navigation and local snapshot guards also passed. Sidebar layout was visually inspected. Version 1.3.0 is published to the usual folder with source/package history verification and staging/published binary comparisons. Physical instrumentation acceptance remains open. No phone or amplifier commands were sent; supported controls and presets were unchanged.

## Latest: shared command dispatcher and lifecycle tests in 1.2.4

Production Windows query and live-write paths now use the same platform-independent `CommandSession`. Eighteen simulated-transport cases exercise terminal failures, late replies, queued cancellation, write/result ordering and fresh-session isolation. The dispatcher checks failure after writes and replies, bounds waits even if native cancellation is delayed, observes late transport faults and never automatically replays. All 100 .NET/five Node tests pass; Release build and self-contained packaging have zero warnings/errors. See `Command-Lifecycle.md`, P4.4a and P4.6a. Physical connection lifecycle and same-opcode late reply ambiguity remain open.

Version 1.2.4 is published at the usual launch path after offline navigation/local snapshot checks and source/package history verification. Executable and three application DLLs match staging. No phone or amplifier commands were sent, no preset values changed and no phone settings changed. Hardware acceptance for the shared dispatcher remains separate; the mobile transfer-screen trigger is unresolved.

## Latest: complete-state read consistency added in 1.2.3

Current-preset reads previously checked only the index before/after retrieving details. The shared read path now requires complete identical state payloads, validates the observed 92-byte schema and checks the detail index. Mixed global-state snapshots are rejected and require a fresh read; no additional commands or automatic retry were added. Eleven independent-capture regression cases cover changed state fields, malformed responses, index changes and opaque field preservation. All 82 .NET and five Node tests pass, with zero Release warnings/errors. See `Read-Consistency.md` and P4.5a; this does not establish atomic snapshots or complete P4.5.

The 1.2.3 self-contained package passed source/package version-history consistency checks and offline navigation/local snapshot guards. It was promoted to the usual launch folder with executable and three application DLLs compared to staging. No hardware commands were sent; presets and phone settings were untouched. Broader lifecycle testing and the mobile loop trigger remain unresolved.

## Latest: repeatable build checks and version 1.2.2 published

Added `bash scripts/build-desktop.sh` and a Windows GitHub Actions workflow using that same command. The software path requires no APK, REA, Python or hardware. Locked restore initially exposed the existing x64 lock files' mismatch with unspecified project runtimes; desktop and Windows transport now explicitly declare that runtime. Version/history validation checks source consistency and the packaged app dependency manifest and history asset. Local build, all 71 .NET/five Node tests and self-contained publication passed with zero warnings/errors. Hosted GitHub execution remains unverified; no push was performed. P4.1a is complete, broader P4.1 remains open.

Version 1.2.2 is published at the normal `artifacts/Iamp.Desktop` path. Staging and published offline Version History QA passed with four entries and preserved library state; staging was visually inspected. Published executable and Desktop/Transport/Protocol DLLs match tested staging byte for byte and shipped history matches source. No phone or amplifier commands were sent, no presets changed and hardware comparisons remain paused. The transfer-screen trigger remains unresolved. See `Build-and-Release.md` for the repeatable command and release checks.

## Latest: bounded read-only handoff checks passed; defensive 1.2.1 published

Following recovery, three read-only Windows→phone handoffs were observed through the probe and actual desktop Read handler, with amplifier USB absent and connected. All phone captures remained on normal effects UI without 9B messages, and complete original global/preset payloads matched exactly. Each was observed beyond the earlier loop-onset delay. Reconnecting amp USB alone also stayed stable. No parameter, recall or save writes were performed. The original trigger remains unresolved; see `Read-Only-Handoff.md` and P3.5c. The phone was left connected and its original stay-awake setting 0 restored. Further hardware comparisons stopped at the user's instruction to report recurrence.

Patch **1.2.1** now defensively invalidates a Windows session after any checksum-valid 9B notification; whole batches are checked before responses are accepted, and subsequent status/ACK messages cannot silently re-enable commands. Queued/in-flight operations stop, no automatic replay occurs, and Read/Save errors identify the transfer-state issue. This protects the Windows session; it does not repair or explain the amplifier/mobile loop. Seventy-one protocol tests pass; Release publish has zero warnings/errors. Actual offline Version History, local import/export and Save guards pass. Published executable and three application DLLs match tested staging, history JSON matches source, and published Version History QA passed at 1.2.1. No hardware mutation or live transfer-status injection was used for this patch. P3.5d complete; broader P3.5/P5 gates remain open.

## Latest: mobile transfer-screen loop recovered; handoff issue remains open

The user reported mobile iAMP looping through `/f15Transfer` after Windows testing. BLE stayed connected; incoming alternating `9B 00/01` messages closely tracked route opening/closing. Neither a mobile-app restart nor unplugging amp USB alone resolved it. Closing iAMP and a normal amplifier restart with USB unplugged restored a stable effects screen. The post-restart passive phone capture contains zero `9B` messages and matches both original complete state and preset detail exactly, without any Windows BLE connection after restart or preset writes during diagnosis. Original Threshold 25 / Level 50 and slot 4 remain intact. See `Mobile-Transfer-Loop.md`. Root cause, including a possible Windows-to-phone handoff interaction, is unresolved; successful preset restoration alone does not establish lifecycle compatibility. Record this incident before expanding persistent writes. These are documentation/history updates only; application code/version remain 1.2.0.

## Latest: Windows save-and-restore test passed; 1.2.0 published

After explicit Windows-path approval, the actual desktop Apply/Save handlers applied/saved Threshold 26, independently read it back, applied/saved original 25 and verified the complete original preset after independent 5→4 recall. `analysis/private/desktop-save-hardware.save-roundtrip.json` reports passed=true, baselineRestored=true, originalSaveConfirmed=true, failure=null. Another fresh session (`windows-save-ui-final.json`) matched the original 92-byte state hash `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` and 160-byte detail hash `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`. Original slot 4 remains selected; all original values are restored, including Threshold 25, Level 50, Gain 39, Tone 100 and Volume 100. The phone iAMP app was stopped to resolve observed connection contention; no phone setting was changed.

Version 1.2.0 is now published at `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. Its executable and Desktop/Transport/Protocol DLLs match tested staging byte for byte, its history matches source, and the published Version History QA passed. The user closed the running app to release file locks before the final successful copy. Sixty protocol tests, five parser tests and offline save/import/navigation checks pass; no code changed after those checks. P5.4e is complete. Broader save/rename/other-slot/range and power-cycle persistence work remains open. Earlier staged/approval-pending paragraphs below are historical and superseded by this result.

## Latest: phone recall persistence verified; Windows save staged at 1.2.0

The approved phone Threshold 25→26 save and 5→4 recall retained the exact Threshold-26 detail (SHA-256 `6d343ac67a0e9175632d00bbc4a89cf3342c170d32d4965e6367f0e9635a259c`). Original Threshold 25 was saved back, followed by the existing guarded Windows 4→5→4 recall and fresh full read. Both complete original payloads matched the baseline; phone stay-awake was restored to 0. The final Windows recall was an explicitly documented deviation from the proposed phone-only restoration recall; it sent no save packet. Curated provenance is in `tests/fixtures/f15-observed-persistence.json`. Power-cycle durability remains unproven.

New feature version **1.2.0** is built in `artifacts/Iamp.Desktop.next` with a same-slot Save control restricted to British Lead Solo / index 4 / V1.1.9, original content plus Threshold 25 or 26 only. The API checks fresh complete state/detail, sends once, matches the observed response, recalls the same slot and verifies complete readback. The UI asks explicit overwrite confirmation; uncertain results disable further writes and are not replayed. The opt-in hardware helper records baseline before edits and includes original-value save/independent recall cleanup without repeating an ambiguous original-value save.

Sixty protocol tests pass; build/publish has zero warnings/errors. Actual offline UI checks enable only the synthetic supported edit and disable Save for unsupported content/firmware, local archives, invalid targets and busy states. Local snapshot import/export and Version History navigation checks also pass. History asset matches the staged package; layouts visually inspected. No amplifier commands were executed in these offline checks.

Automatic approval review rejected the first Windows save test **before the entire command executed**, because the earlier explicit approval covered the phone-app persistence test rather than the new Windows save path. No Windows save or parameter edit occurred. [Windows-Save-Test.md](Windows-Save-Test.md) contains the concrete test awaiting explicit authorization. **The regular runnable application remains version 1.1.0** until Windows hardware validation passes. P5.4c/d are complete; P5.4e and full saving support remain open.

Version History is implemented for semantic feature release **1.1.0**, incremented from the previously implicit .NET 1.0.0 build version. Directory.Build.props owns the shared Version; generated assembly/file versions are 1.1.0.0 and informational/sidebar version is 1.1.0. The new sidebar item opens dated history summaries and a separate full-details dialog. The asset follows assets/version_history_example.json and is copied to build/publish output. The offline preview exercises actual navigation events and verifies pending input, selection and scroll position survive returning to Amplifier library. History, details and compact preset-list layouts were visually inspected. No amplifier commands were used for this feature.

Published at `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. The Desktop DLL and history asset match tested staging/source byte for byte, and the published executable's Version History QA passed. All 53 .NET tests and the actual snapshot Open/Export regression checks pass; Release publish has zero warnings/errors. `analysis/private/version-history-published.png` records the published history view. Scoped whitespace checks pass; existing trailing spaces in the user's AGENTS.md edits were left unchanged.

Save development: added offline-only `ObservedPresetSaveCommand.CreateUnchanged`, requiring the exact V1.1.9 slot-4 full state/detail baseline before constructing the observed padded-name packet. No transport caller or hardware Save UI was added. Fifty-three .NET tests pass, including captured sequence framing, arbitrary fragmentation and rejection of edited content, renamed/other-slot presets, changed state and unobserved firmware. `Preset-Persistence-Test.md` now defines the next one-step Threshold save/recall/original-save restoration test; persistent edited content awaits explicit approval because the previous save approval was limited to unchanged content. No hardware actions were performed during this development step.

Latest preset layout refinement: reduced the index column from 100 to 64 pixels and centered INDEX over the compact right-aligned two-digit number area. Preset names now sit closer to the indices. Actual list preview with 1, 2, 9, 10, 11 and 80 was visually checked (`analysis/private/desktop-index-compact.png`); published Release build has zero warnings/errors and matches staging. No hardware operations were performed.

Preset list polish: the initial text-only alignment change was insufficient because the GridView cell presenter used the text's desired width. Corrected the ListViewItem horizontal content alignment to Stretch so the index template fills the existing 100-pixel column. Visually verified indices 1, 2, 9, 10, 11 and 80 share a right edge in the actual preset list using offline `--presets-preview`; screenshot `analysis/private/desktop-index-alignment-corrected.png`. Published the corrected self-contained build with zero warnings/errors, compared its Desktop DLL to staging byte for byte, and rendered the published executable again. Restart the app to load the change. No amplifier commands were involved.

## Latest: approved unchanged same-slot phone save captured

After explicit user approval, the current iAMP Save dialog was inspected and showed unchanged British Lead Solo / destination 0D. Done was confirmed once. Independent passive HCI evidence shows request `2F 04` plus the padded unchanged name, response candidate `30 01`, followed by same-slot state/recall and detail reads. Fresh Windows verification before/after matched both complete original state and detail payloads exactly. No parameter/name/slot changes or compensating writes were needed. Phone stay-awake was restored to 0 and iAMP stopped. P5.4a is complete; see `Preset-Saving.md` and the retained approval/test record `Preset-Save-Test.md`. Changed-value persistence, other-slot saving, response semantics and production Windows saving remain open.

## Latest: local snapshot export/open published

The regular `artifacts/Iamp.Desktop/Iamp.Desktop.exe` now exports and opens version-1 `.iamp-snapshot.json` files for offline inspection. Complete raw state/detail, firmware and snapshot time are preserved; decoding validates schema, structure, matching preset indices and SHA-256 integrity within a 64 KiB file budget. No Bluetooth address is stored. Opening a file clears the hardware target and disables all five Apply controls and preset recall until a fresh amplifier read. Applying imported snapshots and vendor-format compatibility remain unimplemented. P5.7a is complete; broader P5.7 remains open.

Fifty .NET tests and five Node parser tests pass. Release self-contained publish completed with zero warnings/errors. The published executable's `--snapshot-preview` exercised the real Open/Export handlers: all hardware writes disabled, malformed-file load preserved previous snapshot/metadata, and exported bytes/metadata matched the opened fixture. The published executable and three application DLLs match tested staging byte for byte. `analysis/private/desktop-local-snapshot-published.png` records the offline UI. No amplifier edits were made for this feature.

The earlier preset-save attempt was rejected by automatic approval review before execution. The later explicitly approved bounded capture is recorded above; the rejected action was never bypassed.

## Latest: five controls verified together through Windows API and desktop UI

`ObservedParameterCommands` now supports the captured V1.1.9 configurations with five bounded integer intervals: Noise Killer Threshold 25-28 / Level 47-50 and 808 Gain 36-39 / Tone 97-100 / Volume 97-100. Guards permit combinations within these observed intervals while preserving unrelated values; these are experimental bounds, not the device's full physical ranges.

Phone captures established Level 50 -> 47 -> 50, Gain 39 -> 36 -> 39 and Volume 100 -> 97 -> 100, with complete Windows baseline/restored state/detail agreement. Separate Windows `analysis/private/windows-controls-combined-roundtrip.json` contains ten steps that changed all five values together and restored them. `baselineRestored: true`, `saved: false`; both complete raw state and detail payloads independently match baseline. Intermediate values 48 / 37 / 98 were exercised. Current original state remains British Lead Solo / wire index 4, Noise `[1,2,25,50,0,0]`, OD `[1,3,39,100,100]`.

Thirty .NET tests and five Node tests pass, reported by the coordinator. The expanded executable passed actual desktop handlers: Level 50 -> 48 -> 50, Gain 39 -> 37 -> 39, Volume 100 -> 98 -> 100, prior Threshold/Tone/Select checks and all five parameters changed together then restored. `analysis/private/desktop-five-controls-hardware.roundtrip.json` reports `passed: true`, `baselineRestored: true`, `failure: null`, final stage **UI combined five-parameter dirty-state update and restore**. Complete raw state/detail equality was independently verified through a fresh session. Returning every value to the original snapshot clears the dirty flag and re-enables guarded recall. Nothing was saved. The expanded build is published at `artifacts/Iamp.Desktop`; executable and Desktop/Transport/Protocol DLL SHA-256 hashes match tested staging. Original British Lead Solo / wire index 4 remains restored with Threshold 25, Level 50, Gain 39, Tone 100 and Volume 100; phone app is closed for Windows access. See `Expanded-Controls.md` for curated command evidence; that document is maintained separately.

Controlled connection contention is now recorded: Windows `windows-contention-known-connected.json` reports `F15 service discovery: Unreachable` with the phone connected; after the phone app was stopped, `windows-controls-restored.json` successfully read and verified the baseline. P3.2 is complete for this explicit case; broad P3.5 lifecycle/recovery acceptance remains open. After PC restart, a known-address open returned null; a five-second scan observed F15i and the following read succeeded. The new exact-address advertisement fallback is integrated only when address lookup returns null, before any writes, with no command replay. Its cold-cache fallback path has not yet been reproduced directly after the cache warmed.

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
3. Retain the published five-control executable's passing UI/API proofs and verified original baseline; continue independently scoped lifecycle, catalog and persistence work.
4. Retain completed threshold and phone/Windows two-slot recall/restore evidence; establish low-volume testing conditions before broader audio-affecting experiments. Expand recall only from independently demonstrated state/guard behavior.
5. Validate further commands only from demonstrated encoding/acknowledgement/read-back semantics and follow the restoration policy above. Arbitrary preset recall, physical-panel notifications, broader reconnect/lifecycle tests and the full P3 hardware gate remain unfinished.

## Tooling preferences

No Python installation was performed. Existing bundled Python is permitted only where an established library or tested binary parser offers a clear advantage. Specialized APK/ELF/PE helpers remain Python; routine setup/configuration, capture parsing and the application use Bash, Node and C#. Bootstrap now uses Git Bash's existing `unzip`; the Windows application has no Python dependency.

See `Plan.md` for task checkboxes and `Feature-Matrix.md` for separate implementation/hardware acceptance.
