# Windows iAMP companion: implementation plan

Prepared: 13 October 2026; updated 14 October. Status: implementation in progress; Windows discovery/state reads, restricted Noise Killer Threshold and 808 Tone controls, and guarded preset 4 -> 5 -> 4 recall/restore are working. Full amplifier control is not yet proven.

The latest Tone-enabled executable is published at the normal launch path. Its actual Apply Tone workflow passed 100 -> 98 -> 100 with complete original state/detail independently restored. See [808-Tone-Editing.md](808-Tone-Editing.md), [Progress.md](Progress.md) and [User-Guide.md](User-Guide.md).

Implementation evidence is recorded in [Environment.md](Environment.md), [Artifact-Inventory.json](Artifact-Inventory.json), [Feature-Matrix.md](Feature-Matrix.md), [Decisions.md](Decisions.md) and [Vendor-Utility.md](Vendor-Utility.md). Completed checkboxes denote the stated task only, not completion of its phase gate.

Runnable build: `artifacts/Iamp.Desktop/Iamp.Desktop.exe` (keep its accompanying files), a Windows BLE companion for firmware/capacities, all 80 names and the current preset. Published desktop handlers passed a real V1.1.9 sequence: Read -> Select 5 -> Select 4 -> Apply Threshold 26 -> Apply Threshold 25. Independent cleanup through a fresh session verified full state and detail payloads equal the original baseline; nothing was saved. Evidence: `analysis/private/desktop-ui-hardware.roundtrip.json` and `desktop-ui-hardware.png`. Recall remains restricted to exact observed slot-4/5 states and threshold editing to demonstrated Noise Killer values 25-28. Arbitrary selection, saving, mixer and general editing remain unsupported. See [Preset-Selection.md](Preset-Selection.md), [Parameter-Editing.md](Parameter-Editing.md), [Protocol.md](Protocol.md), [Preset-Format.md](Preset-Format.md), [Native-Findings.md](Native-Findings.md) and [Effect-Catalog.md](Effect-Catalog.md) for the boundaries.

Latest validation is recorded in [Progress.md](Progress.md). The user-created MOOER Studio baseline backup and private copy have matching verified SHA-256 hashes; the vendor recovery route is documented in [Backup-Baseline.md](Backup-Baseline.md). Backup restore remains untested. Fresh phone and independent Windows state now identify firmware V1.1.9 and British Lead Solo (phone slot 0D, current wire index 4), distinct from the earlier V1.1.2 baseline. The successful live threshold experiment did not save the preset or change output gain.

Latest increment on 14 October: combined edits for Noise Killer Threshold 25-28 / Level 47-50 and 808 Gain 36-39 / Tone and Volume 97-100 passed Windows API and actual desktop-handler hardware tests. Individual edits and all five changed values together were read back and restored; independent fresh-session verification matched complete original state/detail. The tested build is published at `artifacts/Iamp.Desktop`; executable and Desktop/Transport/Protocol DLL SHA-256 hashes match tested staging. Thirty .NET and five Node tests pass. These ranges remain experimental, not full device limits. See P5.5c and `Expanded-Controls.md`.

## 1. Objective and approach

Build a Windows desktop companion for the MOOER F15i Li amplifier that reproduces the useful device-management and tone-editing workflows of iAMP 1.6.1. Preserve the mobile app's familiar effect colors, labels, signal-chain presentation and parameter controls, while using a desktop layout with a persistent preset browser and larger editing area.

The amplifier remains responsible for its sound processing. The first release controls the physical amplifier; it does not implement an amp simulator or require an Android emulator. REA is a development investigation tool, not a dependency shipped with the finished application.

Develop an independent Windows implementation from documented behavior, protocol evidence and hardware experiments. Decompiled output is supporting evidence, not a directly buildable Windows application. Prioritize a reliable offline editor before cloud services or advanced media processing.

**Critical path:** prepare tools -> identify implementation and transport -> recover protocol -> demonstrate real Windows control -> implement editor -> validate and package. Do not invest in a complete UI before the transport proof of concept succeeds.

Each task has a checkbox. Check a task only after its deliverable and validation are recorded. Record blocked tasks and their missing evidence in `DOCO/Codex/Decisions.md`; an unchecked task is not a completed feature.

## 2. Inputs and findings established during planning

Paths below are relative to the repository root, `D:\A_A_Codex\rea_apk`.

| Input | Purpose and current finding |
| --- | --- |
| `AGENTS.md` | Project instructions; requires Bash for commands. |
| `apk/MOOER_iAMP_V1.6.1_2026.06.26.apk` | Primary supplied APK, 247,145,267 bytes. Contains two DEX files, Flutter assets, `libflutter.so` and `libapp.so` for ARM64, ARMv7 and x86-64. |
| `apk/extracted/base.apk` and three `split_config.*.apk` files | Separate phone-extracted set. The ARM64 split contains native libraries absent from this base APK. Do not treat these files as byte-identical to the primary APK or merge them without verifying identity. |
| `DOCO/MOOER/F15i Li_Manual_EN_V01_2025.06.19.pdf` | 90 PDF pages. Printed pages 37-63 describe iAMP; pages 64-65 describe the existing PC utility; pages 66-81 describe effects. PDF page numbers are printed page numbers plus three in these sections. |
| `DOCO/MOOER/MOOER AMP.md` | User notes and links, including sample presets and the existing Windows utility. |

Primary APK SHA-256:

```text
7ea003baf4dcb5073174c4f253a25deeb460dff2cdec379ae0ece7c88b931273
```

ZIP inventory establishes Flutter packaging, but does not establish the transport, protocol, firmware compatibility or whether individual features run locally or in the cloud. Libraries with audio/ML-related names are also present; names alone do not establish their behavior.

The manual documents Bluetooth connection from iAMP. It does not establish whether app control uses BLE GATT, Bluetooth Classic, or another arrangement. Bluetooth music playback and USB audio must not be mistaken for the control protocol.

The manual also documents MOOER Studio for Future Series on PC for USB firmware updates, third-party IR import and whole-device backup/restore. The current vendor download page lists a Windows utility and additional release notes. Evaluate this existing software early; it may provide a backup path, behavioral reference or an easier route for selected functions. Do not assume that it exposes an SDK or all mobile features.

The 2025 manual predates the supplied 2026 APK. Treat it as a baseline, then reconcile against the installed app and actual amplifier firmware. For example, printed pages 43 and 48 disagree on the sound-library count (30 versus 32); do not hard-code either count without checking.

## 3. Recommended Windows technology

**Recommended baseline: C# + WPF/XAML + .NET 10 LTS, using MVVM and asynchronous device I/O.** Initially support Windows 11 x64; evaluate other Windows versions and ARM64 explicitly rather than promising them automatically. Pin a supported SDK patch and a Windows-specific target framework once the Bluetooth spike is verified.

| Option | Assessment for this project |
| --- | --- |
| C# / WPF | Preferred for a Windows-only controller: mature custom styling, vector drawing, binding, desktop tooling and access to Windows device APIs. Suitable for this workload without a browser runtime. |
| C# / WinUI 3 | Viable for a modern Windows appearance. Custom mobile-style controls remain necessary; evaluate deployment and device API requirements if choosing it over WPF. |
| Dart / Flutter for Windows | Strong alternative because the reference UI is Flutter. However, the APK does not supply reusable Dart source; a new implementation and verified Windows transport/plugin support would still be needed. Reconsider if source access or future cross-platform requirements change the balance. |
| C++ / Qt | Strong device and custom-UI capabilities, but additional native complexity and licensing/deployment considerations are unlikely to benefit the initial controller. |
| TypeScript / Electron | Capable UI stack, but a browser runtime and native device bridge add complexity for a Windows-only hardware application. |

This is an engineering recommendation, not a measured performance comparison. .NET 10 is the current LTS baseline checked for this plan. WPF supports custom controls and hardware-accelerated rendering; Windows Runtime APIs can be called from appropriately targeted desktop .NET projects. Validate the actual Bluetooth API behavior, packaging permissions and adapter support in the spike before finalizing the stack. [R5-R8]

Proposed projects:

```text
src/Iamp.Desktop/              WPF views, styling, view models
src/Iamp.Application/          Use cases, connection coordinator, state synchronization
src/Iamp.Domain/               Presets, effects, parameters, capabilities
src/Iamp.Protocol/             Packet encoders/decoders and command definitions
src/Iamp.Transport.Windows/    Verified Windows Bluetooth or USB implementation
src/Iamp.Persistence/          Versioned local presets/settings and migrations
tools/Iamp.DeviceProbe/        Small console probe for protocol experiments
tests/                        Protocol fixtures, state tests, integration checks
analysis/                     Local evidence and captures; large/private files ignored
DOCO/Codex/                   Specifications, decisions, validation and task tracking
```

Use a transport interface and simulator so protocol and UI work can run without an amplifier. Keep command parsing independent of Windows APIs. Store local presets in a versioned JSON format initially; introduce a database only if library scale or search needs justify it. Keep cloud integration behind a separate interface.

## 4. Release scope and desktop experience

| Priority | Features | Evidence baseline |
| --- | --- | --- |
| MVP | Discover/connect/disconnect; identify device/firmware; read and select presets; edit supported effects and parameters; save with read-back; local export/import of verified preset data; mixer and connection status | Manual pp. 38-49; confirm protocol support |
| Next | Global EQ/input gain/bypass, lighting/brightness, device naming, power settings; tuner; drum/metronome; looper; footswitch pairing/status | Manual pp. 47, 55-63; validate each command |
| Conditional | Factory tone library; GNR/GIR/IR management, supported device backup/restore | Manual pp. 48, 54, 64-65; determine format and transport |
| Separate investigation | Accounts, cloud presets, uploads, previews, any newer AI/stem/media features found in 1.6.1 | Not required for an offline controller; assess supported service access and compute needs |
| Deferred from initial release | Firmware flashing and factory reset | Preserve vendor utility as the initial maintenance path |

Desktop layout: left preset/library pane; central horizontal signal chain and selected-effect editor; optional right mixer/device inspector; bottom connection, firmware, battery and pending-change status. Use resizable panes and sensible collapse behavior on smaller displays. The phone's current 1.6.1 app uses dark charcoal surfaces, blue navigation selection and colored effect cards/sliders; this observed theme supersedes the older manual's light-theme screenshot as the initial visual reference. Keep light-theme support optional.

Support mouse wheel, keyboard entry, focus indication, shortcuts, accessible names, scalable text, and explicit numeric values alongside sliders. Changes must distinguish local edits, acknowledged device values and saved device presets. A disconnected window must not imply that controls are operating live hardware.

## 5. Phase 0 - Establish the baseline

- [x] P0.1 Re-read `AGENTS.md`, inspect repository state and preserve existing user files. Use Git Bash for project commands and document Windows versus Git Bash path conventions. Evidence: `Environment.md`; pre-existing documentation relocations and `.gitignore` edits preserved.
- [x] P0.2 Create `DOCO/Codex/Environment.md`, `Decisions.md` and `Feature-Matrix.md`; record machine architecture, Windows build, Bluetooth adapter/driver, amplifier model/firmware and installed phone app version. Initial V1.1.2 and current V1.1.9 were independently queried by Windows. Preserve both session baselines.
- [x] P0.3 Record hashes, package identifiers, version codes, signing-certificate fingerprints and provenance for the primary APK and extracted split set. Verify package/version/certificate agreement within the split set before comparing implementations. Evidence: `Artifact-Inventory.json`; all five APKs pass apksigner; primary code 42 versus phone code 43, same publisher certificate.
- [x] P0.4 Inventory existing Node/npm, full JDK, ADB, .NET SDK and Windows development tools. Evidence: `Environment.md`; existing Java/ADB retained, compatible portable Node and local .NET installed.
- [x] P0.5 Obtain access to the F15i Li, the working Android app, a data-capable USB cable and a Windows Bluetooth adapter. User created the vendor baseline backup; original and private copy are 294,658 bytes with SHA-256 `75d3ee78e313025571dc56e913397f09d00f23b4d099461b6fd67dc1e6e086a8`. Manual pp.64-65 documents recovery. See `Backup-Baseline.md`; restore was not performed.
- [ ] P0.6 Evaluate the existing MOOER Windows utility on the actual amplifier; record which requested workflows it already provides and which remain missing. Avoid automatic firmware updates during this baseline exercise.
- [ ] P0.7 Create a prioritized feature matrix from the manual and current app, recording local/device/cloud execution, screenshots, expected behavior and test cases. Include bank/slot addressing (80 presets; confirm 20 x 4 mapping), effect categories, units and limits.

**Exit gate:** input identity, hardware baseline, backup and an agreed MVP feature list are documented. No hardware-control writes are required to complete source triage.

## 6. Phase 1 - Set up REA for Codex on this PC

REA connects Codex to analysis engines through MCP. Its setup can register Codex and install matching workflow instructions. Pin the tested package version; repository `main` may document tools absent from npm. Use the connected server's actual schemas. [R1-R3]

- [x] P1.1 Install or select a supported Node runtime (current REA documentation lists Node 22 >=22.19, 24 >=24.11, or 26+) and npm. Record exact versions and executable paths. Evidence: portable Node 24.19.0 in `.tools/node`; existing system Node left unchanged.
- [x] P1.2 Resolve the published REA version, review the release's Windows support, and pin that exact version in the development setup/runbook. Evidence: REA 6.4.0 pinned in `tools/rea/package.json` and dependency lock.
- [x] P1.3 Run a Codex-only REA setup preview; review its configuration and skill destinations, preserve unrelated entries and backups, then apply that scoped setup. Evidence: scoped setup/doctor reports, original `.rea.backup`, Android environment backup; registration aligned with portable Node.
- [ ] P1.4 Restart/reconnect Codex; verify the live REA tools are actually available. Configuration readiness alone does not prove this chat has a working MCP connection. Document how to restart and repeat the smoke test. [R4]
- [x] P1.5 Select the existing full JDK 21.0.1 and install the documented `jadx-headless-mcp` 0.7.1 JAR; verify official provenance and hashes. Configure `JAVA_HOME` and `REA_JADX_MCP_JAR` in the MCP server environment as well as the shell. Evidence: `Environment.md`, successful Android inspection. [R2]
- [x] P1.6 Verify Windows x64 native controls and inspect the supplied APKs through CLI and the registered server's MCP workflow. Primary CLI inspection and phone-base MCP inspection succeeded; a separate MCP client verified 170 tools. Actual chat reconnection remains P1.4. Evidence retained in `analysis/generated/`; primary Evidence `ev_96a1e60d7c71e56ad0b11537736a34911c86260a439eef531812ce1e1ee98ead`. [R2]
- [ ] P1.7 Add Apktool for resources and Android Platform Tools/ADB for phone observation. Add signing inspection tools if needed for P0.3. Record versions and acquisition sources.
- [x] P1.8 Check the installed server's Flutter/AOT capability and exercise it if advertised. Neither tool is advertised by 6.4.0 (170-tool MCP catalog and CLI checks); no successful REA Flutter/AOT inspection is claimed. Independent bounded string/ELF triage is in `scripts/flutter-triage.py`; deeper investigation remains P2.3. [R3]
- [ ] P1.9 If protocol logic is native/AOT, choose a verified ELF analysis route. REA's documented Windows Ghidra P0 scope is x86/x64 PE, so it must not be assumed to handle Android ELF. Evaluate REA on a supported Linux environment, or standalone Ghidra with the appropriate ELF/CPU loader; test on an extracted library before relying on it. [R1]
- [x] P1.10 Save a repeatable Bash setup/smoke-test script and `Environment.md` with versions, paths, configuration backup location, expected outputs and troubleshooting. Keep downloaded tools, APK copies and generated evidence out of release artifacts.

Historical example below describes the initial setup approach. The implemented repeatable setup is `bash scripts/bootstrap.sh`; use `--configure-codex` to explicitly apply registration and environment changes. Run `bash scripts/check.sh` for verification. Exact tools/dependencies are pinned in tracked manifests and hashes are checked before use.

Initial setup example:

```bash
cd /d/A_A_Codex/rea_apk
node --version
npm --version
java -version
java --list-modules
npm view rea-agents dist-tags.latest

# Replace this placeholder with the reviewed exact published version.
REA_VERSION='<reviewed-version>'
npx "rea-agents@$REA_VERSION" setup --client codex --dry-run --json
npx "rea-agents@$REA_VERSION" setup --client codex
npx "rea-agents@$REA_VERSION" doctor --client codex --json

# Example paths only; use actual installed locations.
export JAVA_HOME='C:/tools/jdk-21'
export REA_JADX_MCP_JAR='D:/tools/jadx-headless-mcp-0.7.1-all.jar'
npx "rea-agents@$REA_VERSION" inspect-android-package \
  'D:/A_A_Codex/rea_apk/apk/MOOER_iAMP_V1.6.1_2026.06.26.apk'
```

Configure those environment variables for the registered server before the Codex smoke test; exports in an unrelated terminal do not configure an already-running desktop process. Use `--help` and installed schemas to verify version-specific arguments and timeouts. No universal manual MCP launch command is assumed here.

**Exit gate:** Codex can inspect the supplied APK and retain a reproducible result; the Flutter/native analysis limitations and chosen fallback are recorded.

## 7. Phase 2 - Recover application behavior and transport

- [ ] P2.1 Inventory manifest, resources, Flutter assets, DEX/plugin classes and native libraries. Map each feature to Java/Kotlin plugins, Dart AOT, native code, device firmware or remote services. Analyze one ABI at a time and retain hashes.
- [ ] P2.2 Use REA/JADX class searches and reference tracing to locate Bluetooth/USB plugins, platform channels, UUIDs, packet serializers, file formats and connection lifecycle. Follow actual plugin calls rather than assuming class names prove protocol behavior.
- [ ] P2.3 Identify the Dart/Flutter build and investigate `libapp.so` only as needed. Match any AOT tooling to the observed SDK/snapshot layout; preserve uncertain findings and fallback to observed traffic when source recovery is incomplete.
- [ ] P2.4 Record current app screens and workflows on the phone: discovery, preset changes, effect edits, save, mixer, tools and reconnection. Use ADB screenshots/logs where supported; record app and firmware versions with each observation.
- [x] P2.5 Establish the actual mobile control transport and endpoints. BLE GATT FFF0 with FFF2 notifications / FFF3 writes is confirmed by attributed phone traffic and independent Windows discovery/reads. USB HID metadata and static USB framing are documented separately; live USB control remains unverified.
- [x] P2.6 Acquire and decode a retained phone-to-amplifier session with action/log timestamps and coverage checks. The attributed HCI log contains 233 target ATT PDUs and 79 CRC-valid reassembled frames, with no truncation detected. Controlled edit comparisons remain P2.7; this initial session does not establish every command.
- [ ] P2.7 Repeat one action at a time across multiple values: initial handshake/state read, preset selection, parameter change, bypass, save and mixer. Compare outgoing requests and incoming notifications/responses; corroborate against code evidence where possible.
- [x] P2.8 Write `DOCO/Codex/Protocol.md` with confirmed, inferred and unknown fields. Supplement with `Preset-Format.md` and `Native-Findings.md`. The partial ledger explicitly leaves mutation semantics, error responses and most parameter scales unknown; later discoveries must update it.
- [ ] P2.9 Create sanitized request/response fixtures with action, version, expected state and provenance. Define notification-driven synchronization, timeout/retry policy and whether each write is safe to repeat.

**Exit gate:** the handshake, a reliable state read, preset addressing and one reversible parameter command are understood well enough to implement. If blocked, continue targeted capture/native analysis or assess USB against the vendor utility before proceeding to full UI work.

## 8. Phase 3 - Prove Windows hardware control

- [x] P3.0 Build and run an initial read-only Windows discovery probe. `tools/Iamp.DeviceProbe` builds with zero warnings/errors and reads the F15i USB HID report metadata (65-byte input/output, no feature reports); bounded Windows BLE advertisement scan also ran. This does not satisfy the hardware-control exit gate.
- [x] P3.1 Build `Iamp.DeviceProbe` using verified Windows BLE discovery/GATT APIs and shared protocol code. Unpackaged .NET and self-contained WPF information reads work on this PC. Installer/clean-machine capability acceptance remains P7.5/P7.8. [R7-R8]
- [x] P3.2 Discover/identify the actual F15i Li; connect, initialize and read firmware/current state on Windows, with the phone disconnected. Initial V1.1.2 and current V1.1.9 state reads succeeded. Controlled contention with the phone connected produced `F15 service discovery: Unreachable` (`windows-contention-known-connected.json`); after stopping the phone app, Windows read succeeded and complete baseline was verified (`windows-controls-restored.json`). This records the tested contention outcome, not automatic contention recovery or the broader P3.5 lifecycle matrix.
- [x] P3.3 Apply one known reversible parameter change at low output volume; confirm the value on hardware and by read-back; restore its original value. The user confirmed quiet volume/no playback before the controlled 808 Tone investigation. Windows API Tone 100 -> 98 -> 100 and the expanded desktop test passed with complete state/detail restoration. Evidence: `windows-808-tone-roundtrip.json`, `desktop-ui-tone-oct14.roundtrip.json`, `808-Tone-Editing.md`. The earlier threshold-only test did not measure volume; no unknown-command fuzzing occurred.
- [x] P3.3a Demonstrate the narrow V1.1.9 Noise Killer threshold 25 -> 26 -> 25 through Windows, read back each value and restore the full preset-detail baseline byte for byte. Evidence: `analysis/private/windows-threshold-roundtrip.json`, `baselineRestored: true`, `saved: false`; restore used a new BLE connection. This small noise-gate threshold test changed no output-gain parameter; output level was not measured. See `Parameter-Editing.md`.
- [ ] P3.4 Switch presets and verify incoming state updates. Test physical-panel changes and determine how the Windows client learns about them.
- [x] P3.4a Prove guarded Windows preset 4 -> 5 -> 4 recall on V1.1.9 with independent read-back and automatic original-preset restoration through a fresh BLE session. `analysis/private/windows-selection-roundtrip.json` has `baselineRestored: true`, `saved: false`; both full raw state and detail payloads equal baseline. British Lead Solo / wire index 4 / Threshold 25 restored. Exact current/target detail hashes and fresh-state comparisons reject unknown unsaved edits. This two-slot proof does not complete physical-panel notifications, arbitrary slot recall or P3.4.
- [ ] P3.5 Test disconnection, amplifier power cycle, Windows Bluetooth toggle and reconnect. Dispose sessions/subscriptions and reject stale responses; resynchronize before enabling writes.
- [x] P3.5a Capture and recover the reported mobile transfer-screen loop after Windows testing. Incoming alternating 9B notifications correlate with phone transfer-route changes; closing iAMP and restarting the amp with USB unplugged restored a stable effects screen. Post-restart passive data matched complete original state/detail, with no 9B frames in the new connection. See `Mobile-Transfer-Loop.md`; recovery is observed, not a root-cause fix.
- [ ] P3.5b Investigate Windows-to-phone handoff and the transfer-status loop before expanding persistent writes. Compare read-only session disposal/reconnect with save/recall/reconnect; verify mobile usability as well as exact preset restoration. The causal trigger and broader lifecycle matrix remain unresolved.
- [x] P3.5c Record bounded read-only handoff checks through DeviceProbe and the actual desktop Read handler, with amp USB absent and attached. All three phone reconnect captures had zero 9B frames and exact original state/detail; each was observed beyond the earlier loop-onset delay. USB attachment alone also remained stable. See `Read-Only-Handoff.md`; this does not close P3.5b or prove general lifecycle compatibility.
- [x] P3.5d Publish defensive patch 1.2.1: checksum-valid 9B notifications permanently invalidate the current Windows session; inspect whole notification batches before accepting responses, block queued/subsequent writes, stop in-flight operations and never automatically replay. Seventy-one offline protocol tests and navigation/import/Save guard checks pass. The guard is not a root-cause fix; no real status notification was injected into the amp.
- [ ] P3.6 Decide and record the final UI stack and transport in `Decisions.md`, including tested OS/adapter/firmware combinations and any unsupported features.

**Exit gate:** a reproducible Windows session can read state, switch presets, change and restore a parameter, and reconnect. This is the feasibility milestone; a successful mock UI does not satisfy it.

## 9. Phase 4 - Build the application foundation

- [ ] P4.1 Create the solution/projects from section 3, pin SDK/package versions and add a Windows build/test pipeline. Keep setup instructions in Bash even where tools execute native Windows binaries.
- [ ] P4.2 Define domain types for banks/slots, effect-chain order, algorithm identifiers, parameter units/ranges, mixer values and device capabilities. Derive numerical encodings from protocol evidence, not displayed labels alone.
- [x] P4.3 Implement bounded framing/parser and checksum encoding, retaining unknown command payloads and fields as opaque data. Eight .NET tests cover independently captured frames/preset values, fragmentation, malformed lengths/sequences and checksums; five Node tests cover capture attribution/reassembly. Complete command interpretation remains separate.
- [ ] P4.4 Implement a connection state machine, serialized command queue, cancellation and response correlation. Coalesce slider updates, preserve final values and avoid retrying non-idempotent save/transfer commands blindly.
- [ ] P4.5 Implement state reconciliation: local pending edits, confirmed live values and persisted device values; define conflict handling for panel/app changes and interrupted saves.
- [ ] P4.6 Implement simulator/replay transport, diagnostic logging with sensitive values redacted, and versioned local persistence with migration/rollback handling.

**Exit gate:** protocol/state behavior is testable without hardware and the probe uses the same production transport/protocol components.

## 10. Phase 5 - Deliver the desktop MVP

- [ ] P5.1 Produce a desktop mockup and design tokens from current app screenshots and manual references. Validate color, hierarchy and control behavior at 1366x768 and 1920x1080, and at 100%, 150% and 200% DPI.
- [ ] P5.2 Build the responsive shell, preset browser, effect-chain area, editor and device/mixer inspector, with keyboard navigation and screen-reader labels.
- [x] P5.2a Add Version History navigation, current semantic version beneath DESKTOP COMPANION, and selectable history entries with dates/summaries/full details. Version 1.1.0 is shared through Directory.Build.props; assets/version_history.json follows the required example schema and ships with the application. Offline navigation checks preserve pending inputs, selection and scroll position; history/detail/list layouts visually inspected. Broader P5.2 remains open.
- [ ] P5.3 Add discovery, connection progress/errors, device identity, supported capabilities, battery where exposed and an obvious live/offline indicator.
- [ ] P5.4 Implement preset read/select/rename/save-to-slot with dirty-state handling, explicit overwrite confirmation and read-back verification. Confirm slot mappings with the hardware.
- [x] P5.4a Capture the current phone app's unchanged same-slot save workflow for British Lead Solo (0D / wire index 4), after explicit user approval. The actual dialog name/destination matched and Done was confirmed once. Passive evidence shows `2F 04` plus the unchanged padded name, response candidate `30 01`, then same-slot state/detail queries. Independent Windows reads match complete original state/detail byte for byte; phone settings restored. See `Preset-Saving.md`. The earlier rejected tap did not execute; this later attempt had explicit approval. Changed-value persistence, other slots and the Windows save implementation remain open.
- [x] P5.4b Add an offline reconstruction of the observed unchanged save envelope with exact firmware/full-state/full-detail guards. Independent captured bytes match; changes to destination, name, parameter, global state or firmware are rejected. No transport/UI save operation is exposed. Fifty-three .NET tests pass, including save-frame fragmentation and rejection cases.
- [x] P5.4c Execute the explicitly approved phone edited-value save/recall/restoration test in `Preset-Persistence-Test.md`. Threshold 26 survived the phone save and 5-to-4 recall; original 25 was saved back and independently verified in both complete payloads after guarded Windows recall. The final restoration recall used the existing Windows API, explicitly documented. Power-cycle durability and Windows save execution remain unverified.
- [x] P5.4d Implement version 1.2.0 Windows save API/UI for exact V1.1.9 slot-4 original profiles with Threshold 25/26 only. Fresh full-snapshot guards, explicit Yes/No overwrite confirmation, observed-response checks, same-slot recall/readback and no automatic retry are implemented. Sixty protocol tests and offline UI checks pass; local imports, unsupported content/firmware and invalid/busy snapshots disable Save. Hardware validation/publication completed in P5.4e.
- [x] P5.4e Obtain explicit Windows-path authorization and pass `Windows-Save-Test.md`, including original-value save and independent full baseline verification after recall; publish 1.2.0. Actual Windows handlers saved 26 then original 25; independent 5→4 recall and another fresh read matched complete original state/detail. Report: passed=true, baselineRestored=true, originalSaveConfirmed=true, failure=null. Published binaries/history match tested staging. The earlier rejected test did not execute; the later test had explicit authorization. Broader P5.4 remains open.
- [ ] P5.5 Implement effect add/remove/reorder/bypass, algorithm selection and parameter editing within demonstrated device constraints. Revert rejected edits and show the reason.
- [x] P5.5a Implement and hardware-verify published desktop Apply/Select handlers for V1.1.9 slots 4 -> 5 -> 4 and Noise Killer Threshold 25 -> 26 -> 25. Independent fresh-session verification restored complete original state/detail. Opt-in `--hardware-roundtrip-preview` exercised real handlers; `desktop-ui-hardware.roundtrip.json` reports `passed: true`, `baselineRestored: true`, `failure: null`. Returning Threshold to 25 clears the original-snapshot difference and re-enables guarded recall. Default startup, `--preview` and `--read-preview` do not invoke this write test. Full P5.5/P3 acceptance remains open.
- [x] P5.5b Implement and hardware-verify desktop 808 Apply Tone, restricted to Tone 97–100 in the captured V1.1.9 configuration. Tone 100 -> 98 -> 100 passed on 14 October with complete original state/detail independently restored. The executable is published; 19 .NET tests and five Node tests pass. Broader model/range/save support remains open.
- [x] P5.5c Implement, hardware-verify and publish combined Windows API and desktop-handler edits for Noise Killer Threshold 25-28 / Level 47-50 and 808 Gain 36-39 / Tone 97-100 / Volume 97-100. Ten-step API proof and `desktop-five-controls-hardware.roundtrip.json` passed individual Level 50 -> 48 -> 50, Gain 39 -> 37 -> 39, Volume 100 -> 98 -> 100 plus all five values dirty together/restored. Fresh-session verification matched complete original state/detail; UI report has `passed: true`, `baselineRestored: true`, `failure: null`. Thirty .NET/five Node tests pass. Regular artifact executable and Desktop/Transport/Protocol DLL hashes match tested staging; no save/full-range/persistence claim. Full P5.5 remains open.
- [ ] P5.6 Implement Bluetooth/USB/instrument channel mute and level plus master volume where exposed. Verify dB-to-wire conversion; the manual's mixer range is a starting reference, not a protocol specification.
- [ ] P5.7 Implement local preset export/import with schema versioning, validation and explicit separation from vendor formats. Add vendor-format compatibility only after a proven round trip.
- [x] P5.7a Implement version-1 local snapshot export/open for offline inspection, preserving complete raw state/detail with SHA-256 integrity checks, structural validation and a 64 KiB file limit. Opening a snapshot disables every hardware edit until a fresh amplifier read. This is a local inspection format; applying imported data to the amplifier and vendor-format compatibility remain open.
- [ ] P5.8 Test an end-to-end musician workflow: connect, select a clean/riff/lead preset, edit an effect, save to a chosen slot, reconnect and confirm persistence. Compare with the Android app and amplifier panel.

**Exit gate:** every MVP row in `Feature-Matrix.md` has hardware evidence and a pass/fail result; unsaved/pending states are visible and no operation silently overwrites another slot.

## 11. Phase 6 - Add supported advanced functions

- [ ] P6.1 Implement verified global input gain, EQ, bypass, stereo enhancement, brightness/lighting, auto-off and device-name settings; re-read values after writes and identify settings requiring reconnection.
- [ ] P6.2 Implement tuner mode, note/deviation updates, reference pitch and mute/bypass behavior; confirm how exiting restores prior audio state.
- [ ] P6.3 Implement drum/metronome pattern, tempo, tap tempo, level and play/stop; use a verified catalog rather than assuming manual counts remain fixed.
- [ ] P6.4 Implement looper recording/overdub/play/pause/undo/redo/clear, level and drum sync. Display hardware-reported state; do not infer recording completion from a UI timer alone.
- [ ] P6.5 Implement supported footswitch pairing/status and factory tone-library access. Provide confirmations for operations that interrupt connection or change pairing.
- [ ] P6.6 Investigate local IR/GIR/GNR and backup formats, transfer sequencing, checksums, capacity and recovery. Add imports/restores only after backed-up hardware round trips; retain the vendor utility for unsupported operations.
- [ ] P6.7 Assess cloud/account functionality independently: supported access, login flow, credential storage, service dependency and preset compatibility. Keep offline editing functional when services are unavailable; do not ship extracted credentials or APK-embedded secrets.
- [ ] P6.8 Inventory newer media/AI features against APK 1.6.1 and decide separately whether to implement, integrate or defer each. Do not expand the controller into an audio workstation by implication.
- [ ] P6.9 Record firmware update/factory-reset support as deferred unless a separate tested maintenance design, recovery process and explicit scope decision are completed.

**Exit gate:** advanced features are enabled only on firmware/transport combinations for which behavior has been validated. Unsupported controls explain their availability.

## 12. Phase 7 - Verify, package and maintain

- [ ] P7.1 Run protocol fixture and state-machine tests, including corrupt/partial packets, delayed/duplicate replies, unsolicited notifications, timeouts and interrupted saves.
- [ ] P7.2 Run hardware regression against the Android reference: all supported preset slots, effect limits, parameter boundaries, mixer, tools, power cycles and external panel changes. Preserve before/after backups for persistence tests.
- [ ] P7.3 Exercise no adapter, Bluetooth disabled, permission denial, contention with the phone, device out of range, Windows sleep/resume and supported USB disconnects. Confirm no stale writes are replayed after reconnect.
- [ ] P7.4 Check accessibility and layout at target resolutions/DPI; profile startup, idle memory, slider interaction, command throughput and long sessions. Initial targets: interactive UI without blocking, local control feedback within 100 ms, and typical acknowledged device changes within 250 ms where hardware permits; record measured results and revise targets with evidence.
- [ ] P7.5 Choose installer/MSIX or another deployment format after testing device permissions and clean-machine operation. Package a self-contained x64 build if suitable; verify installation, upgrade, uninstall and user-data preservation.
- [ ] P7.6 Review third-party licenses and asset provenance; recreate visual assets where reuse is not established. Exclude APKs, captures, toolchains, account data and REA from the runtime distribution. Add product attribution without implying vendor endorsement.
- [ ] P7.7 Produce `User-Guide.md`, a supported-device/firmware matrix, troubleshooting, backup/recovery instructions, release notes and reproducible build instructions. Add signing for distributed releases and document runtime update responsibility.
- [ ] P7.8 Test the packaged application on a clean Windows PC with the actual amplifier. Record the release acceptance results and unresolved limitations before marking the release complete.

## 13. Risks, dependencies and completion rules

**User-required hardware test policy:** Before a test, record the originally selected preset and every value that may change. After each test, restore **all changed preset values and the originally selected preset**, then read back and verify restoration; retain raw state/detail comparisons where available. An intended test result is incomplete until this cleanup is verified. If restoration fails or cannot be verified, stop further mutations and report the unresolved state. Preserve the vendor baseline backup. Do not substitute factory reset, firmware update, implicit saving or an untested backup restore for exact test cleanup.

The phone preset-picker test 4 -> 5 -> 4 returned to British Lead Solo with Threshold 25. Independent Windows read-back in `analysis/private/windows-preset-return.json` matches both the full raw state and full raw detail payloads from `windows-v119-restored.json` byte for byte. This confirms cleanup of that phone test, not Windows preset-switch support or the complete P3.4 gate.

| Risk/dependency | Mitigation and decision point |
| --- | --- |
| Core logic is compiled Dart rather than DEX | P2 combines plugin inspection, Flutter/native analysis and controlled traffic captures; full source recovery is not a prerequisite if the protocol is proven. |
| REA main/npm or Windows provider mismatch | P1 pins and smoke-tests the real package and live schemas; use an explicitly validated ELF fallback. |
| Unknown proprietary transport, handshake or firmware differences | P2/P3 are feasibility gates. Keep the UI simulator useful while investigating; do not declare hardware support from static evidence. |
| Missing amp/phone/capture access | Static triage and mock UI can progress; hardware gates remain incomplete until exercised on the real device. |
| Disconnects, loud output changes or preset loss | Back up first, test at low volume, read before write, bound commands and verify persistence; defer destructive maintenance. |
| Vendor services or file formats unavailable | Deliver the offline controller independently and keep unsupported operations explicit. |

A task is done when its code/documentation exists, relevant checks pass, evidence is linked and limitations are recorded. Each feature-matrix row should include priority, source evidence, protocol status, implemented status, tested firmware and acceptance result. Use separate checkboxes for implementation and hardware verification where those occur at different times.

Do not estimate a fixed delivery date before P3: protocol discovery is the main uncertainty. After P3, estimate the confirmed backlog by feature group. The first demonstrable deliverable is Windows control of one reversible parameter; the first useful release is the offline desktop editor.

## 14. Sources and research boundaries

Local manual references above use printed page numbers, with explicit PDF offsets where useful. Planning included text extraction of the application/PC sections and visual inspection of the main UI reference on PDF page 44. The APK inspection was a ZIP/component inventory and hash calculation, not full decompilation, signature verification or execution. No REA installation, Codex configuration change, phone capture or amplifier experiment was performed while preparing this plan.

Online documentation checked 13 October 2026; recheck exact prerequisites when implementing:

- R1: [REA repository and setup guide](https://github.com/morluto/rea/blob/main/docs/installation.md) - setup, pinned registrations, readiness and provider/platform boundaries.
- R2: [REA Android analysis](https://github.com/morluto/rea/blob/main/docs/android-analysis.md) - JDK/JADX prerequisites, commands, Windows package requirements and static-analysis limits.
- R3: [REA Flutter analysis](https://github.com/morluto/rea/blob/main/docs/flutter-analysis.md) - Flutter identification, Dart AOT inspection and incomplete source recovery.
- R4: [Official Codex MCP documentation](https://learn.chatgpt.com/docs/extend/mcp) - server configuration and integration.
- R5: [Microsoft WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/) - styling, controls, binding and rendering.
- R6: [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) - supported LTS baseline.
- R7: [Windows Bluetooth GATT client](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/gatt-client) - BLE implementation reference if GATT is confirmed.
- R8: [WinRT APIs in desktop applications](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps) - Windows-specific .NET targeting and API constraints.
- R9: [Flutter Windows development](https://docs.flutter.dev/platform-integration/windows/building) - alternative implementation stack.
- R10: [MOOER Future Series downloads](https://www.mooeraudio.com/Downloads_xq/8.html) - existing Windows utility and newer feature release notes.
- R11: [MOOER F15i Li product information](https://www.mooeraudio.com/products/230.html) - hardware feature cross-check.
