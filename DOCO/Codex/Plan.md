# Windows iAMP companion: implementation plan

Prepared: 13 October 2026. Status: implementation in progress; environment and a read-only Windows discovery probe are working. Full amplifier control is not yet proven.

Implementation evidence is recorded in [Environment.md](Environment.md), [Artifact-Inventory.json](Artifact-Inventory.json), [Feature-Matrix.md](Feature-Matrix.md), [Decisions.md](Decisions.md) and [Vendor-Utility.md](Vendor-Utility.md). Completed checkboxes denote the stated task only, not completion of its phase gate.

Current runnable build: `artifacts/Iamp.Desktop/Iamp.Desktop.exe` (keep its accompanying files). It is a read-only Windows BLE snapshot viewer: firmware/capacities, all 80 names and the current preset. A published-build integration test successfully read the actual F15i. Preset switching, editing, saving, mixer and other mutations are still unimplemented. See [Protocol.md](Protocol.md), [Preset-Format.md](Preset-Format.md), [Native-Findings.md](Native-Findings.md) and [Effect-Catalog.md](Effect-Catalog.md) for the confirmed boundaries.

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
- [x] P0.2 Create `DOCO/Codex/Environment.md`, `Decisions.md` and `Feature-Matrix.md`; record machine architecture, Windows build, Bluetooth adapter/driver, amplifier model/firmware and installed phone app version. Firmware V1.1.2 is app-reported in the observed connection log, not yet independently queried by Windows.
- [x] P0.3 Record hashes, package identifiers, version codes, signing-certificate fingerprints and provenance for the primary APK and extracted split set. Verify package/version/certificate agreement within the split set before comparing implementations. Evidence: `Artifact-Inventory.json`; all five APKs pass apksigner; primary code 42 versus phone code 43, same publisher certificate.
- [x] P0.4 Inventory existing Node/npm, full JDK, ADB, .NET SDK and Windows development tools. Evidence: `Environment.md`; existing Java/ADB retained, compatible portable Node and local .NET installed.
- [ ] P0.5 Obtain access to the F15i Li, the working Android app, a data-capable USB cable and a Windows Bluetooth adapter. Make a baseline backup using the vendor-supported utility and confirm a documented recovery route.
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
- [ ] P3.2 Discover and identify the actual F15i Li; connect, complete session initialization and read firmware/current state. **Read portion complete:** Windows independently read V1.1.2, 80 names and current preset index 16 / Cali MK3 Lead; state/detail bytes match Android. Phone-app contention still needs a controlled test before ticking the whole task.
- [ ] P3.3 Apply one known reversible parameter change at low output volume; confirm the value on hardware and by read-back; restore its original value. Do not fuzz unknown commands on the amplifier.
- [ ] P3.4 Switch presets and verify incoming state updates. Test physical-panel changes and determine how the Windows client learns about them.
- [ ] P3.5 Test disconnection, amplifier power cycle, Windows Bluetooth toggle and reconnect. Dispose sessions/subscriptions and reject stale responses; resynchronize before enabling writes.
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
- [ ] P5.3 Add discovery, connection progress/errors, device identity, supported capabilities, battery where exposed and an obvious live/offline indicator.
- [ ] P5.4 Implement preset read/select/rename/save-to-slot with dirty-state handling, explicit overwrite confirmation and read-back verification. Confirm slot mappings with the hardware.
- [ ] P5.5 Implement effect add/remove/reorder/bypass, algorithm selection and parameter editing within demonstrated device constraints. Revert rejected edits and show the reason.
- [ ] P5.6 Implement Bluetooth/USB/instrument channel mute and level plus master volume where exposed. Verify dB-to-wire conversion; the manual's mixer range is a starting reference, not a protocol specification.
- [ ] P5.7 Implement local preset export/import with schema versioning, validation and explicit separation from vendor formats. Add vendor-format compatibility only after a proven round trip.
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
