# Implementation decisions and unresolved evidence

Updated 17 October 2026 for **1.41.4**. [Current-Status.md](Current-Status.md) summarizes support; [Plan.md](Plan.md) records completion and [Feature-Matrix.md](Feature-Matrix.md) records bounded acceptance.

## Decision record

| ID | Status | Decision | Evidence / limit |
| --- | --- | --- | --- |
| D01 | Adopted | Independent Windows controller; the amplifier performs audio processing. REA is development-only. | No Windows amp simulator or Android emulator is required at runtime. |
| D02 | Adopted and implemented | C# WPF/.NET 10, asynchronous Windows BLE, Windows x64. Production projects are Desktop, Protocol and Transport.Windows, plus probe/tests/UI checks. | The UI uses partial MainWindow classes; separate Application/Domain/Persistence assemblies and full MVVM separation were initial proposals, not implemented structure. Clean-PC/installer acceptance remains open. |
| D03 | Feasibility demonstrated | Require real reads and reversible edit/restore proof before treating mock UI as hardware support. | All 80 recalls, all 124 model controls, Mixer and P5.8 workflows now have bounded evidence; broad fault/combination acceptance remains open. |
| D04 | Adopted and implemented for BLE | FFF0 service, FFF2 notifications and FFF3 writes; discover handles per connection. | BLE is the live control transport. USB Audio/HID metadata and vendor framing are separate evidence, not desktop USB-control support. |
| D05 | Implemented core scope | Offline preset/effect/Mixer editor with read-only local tools. | Phase 6 requires explicit authorization; GIR&IR is excluded. No implied expansion into cloud/media or maintenance operations. |
| D06 | Adopted | Keep pending, confirmed-current and saved state distinct; use independently versioned JSON formats. | Readable effects JSON 3.0.0; raw snapshot archive 1 and effect cache 1. Imports are inspection-only. General device save is verified; Unicode and vendor-format device apply are not. |
| D07 | Required | Fresh reads, quiet tests, exact restoration after each case, terminal failed sessions and no blind write/save replay. | A chain-token retry is permitted only after an exact successful read proves no change. Fully received inconsistent state/detail/state reads may settle within three complete attempts; transport failures and saves are never retried. One read-only save recovery is distinct from replay. |
| D08 | Required | Preserve APK/version/certificate provenance and firmware-specific evidence. | Supplied APK 1.6.1/code 42 and phone 1.6.1/code 43 share publisher identity but differ. V1.1.2 observations are historical; current control evidence targets V1.1.9. |
| D09 | Adopted | Use pinned REA 6.4.0 and actual available schemas. | REA Android and native vendor analysis succeeded. No Flutter/AOT operation is exposed by the recorded 170-tool release; bounded triage and runtime captures supplied the implemented protocol. Full Dart source recovery is not claimed. |
| D10 | Required | Keep APKs/private captures/toolchains outside runtime; redact identifiers/raw user data in public evidence. | Effect icon provenance is documented, but licensing/redistribution review and signing remain P7.6/P7.7. |
| D11 | User policy | Run only one desktop instance and one connected control app during tests. | Close an existing desktop before opening a test host; preserve/verify cleanup before the next instance. Do not request mobile stability checks unless asked. |
| D12 | User policy | Use semantic application versions for code/config changes; record changes in assets/version_history.json. | Documentation-only reconciliation stays on 1.41.2. Affected checks are the default; a complete suite needs an explicit request or reason. GitHub workflow remains disabled. |

## Tested platform and evidence gates

Windows 11 x64, build 26200 (current local version 10.0.26200.9711), recorded Intel Wireless Bluetooth VID 8087/PID 0033 and driver oem81.inf; F15i firmware V1.1.9; Android 13 phone running iAMP 1.6.1/code 43. Adapter/driver identity is the retained setup inventory, not a newly rerun driver audit. Version 1.41.2 is published as a self-contained folder. These observations do not promise other adapters, Windows versions, ARM64 or clean-machine deployment.

- [x] Actual REA Android/native analysis, attributed BLE transport and frame integrity evidence.
- [x] Independent V1.1.9 state/capacity/all-name reads and all 80 bank-labelled recalls.
- [x] All 124 model parameter/on-off controls and representative moved/duplicate/chain checks, with independent restoration.
- [x] Four-channel Mixer, general save/rename/save-as and edited-copy power-cycle persistence.
- [x] P5.8 clean/rhythm/lead control workflows, mobile parameter comparison and user-confirmed amplifier bank/name, with all original state restored.
- [x] User-created vendor backup hash baseline; later successful restore reported by the user.
- [ ] Complete physical-panel-originated change and connection fault matrix, including sleep/resume and adapter/permission failures.
- [ ] Exhaustive chain limits/rejection, actual DPI/Narrator/performance and long-session acceptance.
- [ ] Applying imports, custom backup-format round trip, installer/signing/license and clean-PC acceptance.

## Remaining issues and scope

| ID | Current status | Next authorized work / boundary |
| --- | --- | --- |
| B01 | Core BLE feasibility resolved | Keep USB control distinct and unclaimed; no new USB/transfer investigation is implied. |
| B02 | USB ADB reference access established | Honor iAMP-only foreground/overlay guards and retained stay-awake cleanup policy. |
| B03 | Core command/action evidence complete for implemented scope | Preserve fixtures; physical-panel-originated and broad failure cases still require acceptance. |
| B04 | REA Flutter/AOT tooling limitation remains | Do not repeat unavailable commands or claim original Dart source recovery. |
| B05 | Vendor backup baseline and user-reported restore established | Full utility evaluation and independently proven custom restore remain open/deferred; never use a backup restore as routine test cleanup. |
| B06 | V1.1.9 reconciled through repeated independent Windows checks | Preserve old V1.1.2 evidence as historical. No Codex firmware update or explanation of the earlier version change is claimed. |
| B07 | Core model/Mixer scales established | Exhaustive chain constraints remain open. Global input gain/library tools belong to deferred Phase 6; GIR&IR excluded. |
| B08 | Some lifecycle evidence passed | Contention, physical amp power-off/reconnect and failed-save recovery are bounded proofs. The historical mobile transfer-loop trigger, broader fault matrix and deployment acceptance remain unresolved. |
| B09 | Advanced scope restricted | Phase 6 is not authorized by a general continuation or documentation request. |

## Historical setup findings — 13 October 2026

The following table retains the original artifact/setup observations. A row's “does not establish” column states the limit of that evidence at that time, not a claim that later features remain unimplemented.

| Finding | Type / source | What it establishes | What it does not establish |
| --- | --- | --- | --- |
| Primary APK reports package `com.mooer.iamp`, version 1.6.1/code 42. | Static badging: `analysis/generated/MOOER_iAMP_V1.6.1_2026.06.26.badging.txt`. | Declared identity of that supplied artifact. | Installed-phone identity, signature agreement or live behavior. |
| APK includes Flutter assets and `libapp.so`/`libflutter.so`. | ZIP inventory in Plan; `analysis/generated/apk-inventory.json`. | Flutter packaging; Dart AOT/native analysis is relevant. | Complete Dart recovery, location of each protocol function or working Windows transport. |
| REA package is pinned to 6.4.0. | Local package manifest. | Reproducible package dependency selection. | Live Codex MCP availability, provider readiness or every tool described by upstream main. |
| Flutter/Dart AOT operations are absent from the installed REA release. | CLI attempts plus `analysis/generated/rea-mcp-catalog.json`: actual connected MCP catalog contains 170 tools, with no Flutter/Dart AOT tools. | These operations cannot be used in the installed 6.4.0 release. | Absence of other native/AOT routes or impossibility of protocol reconstruction from captures. |
| Saved Dart AOT CLI attempt returned command-not-found. | `analysis/generated/dart-aot.json`. | That command is absent in selected CLI; no decompilation was produced by it. | Absence of other native/AOT routes; route through actual advertised tools or a separately verified analysis engine. |
| Authorized phone USB ADB and app-specific runtime capture succeeded. | Coordinator's retained phone identity/runtime evidence and `Protocol.md` capture sources. | Actual phone/app access and a retained Android BLE session are available. | An emulator substituting for Bluetooth hardware, or comprehensive action coverage. |
| Bluetooth connection and USB maintenance are documented. | Manual printed pp.38-39,64-65; text extraction of app/PC sections. | Intended user workflows. | Exact wire protocol or independent desktop compatibility. |
| REA Android inspection succeeded. | Coordinator retained Evidence ID `ev_96a1e60d7c71e56ad0b11537736a34911c86260a439eef531812ce1e1ee98ead`; `analysis/generated/android-package.json`. | A successful Android static-inspection route is available. | Complete Dart analysis or live phone/amp control. |
| Primary APK version code 42 and phone split set code 43 both identify 1.6.1 and share publisher certificate SHA-256 `f07ffbe874f48f2e5a23fec3336799b0e96171ade2b7249e1f2407282af61ee1`. | Coordinator verified `analysis/generated/*signing.txt` and `*badging.txt`; split set also includes source stamp. | Different builds with shared publisher identity; comparison must preserve this distinction. | Byte identity or identical behavior between builds. |
| Windows enumerates amp VID 34DB/PID 000B with Audio MI00 and HID MI03; Intel Wireless Bluetooth is present. | Coordinator reviewed `analysis/generated/connected-devices.txt`. | Candidate USB interfaces and a local Bluetooth adapter exist. | HID control framing, BLE reachability, correct initialization or support for all mobile features. |
| .NET SDK 10.0.401 is installed; protocol checks and Windows read probe succeed. | `analysis/generated/dotnet-info.txt`; five .NET protocol tests and five Node capture tests passed; private `windows-read-info.json`. | Real Windows GATT integration can read firmware V1.1.2, capabilities 80/10/10/70/10 and all 80 preset names. | Effect editing, preset selection/save, battery semantics, reconnect reliability or release packaging. |
| Retained Android capture contains 233 target ATT PDUs and 79 complete AA55 frames with valid CRC. | `Protocol.md`, generated `protocol-capture.json`, sanitized fixture and tests. | Initial-session FFF0/FFF2/FFF3 transport, fragmentation and CRC-16/GSM envelope are established. | Every action, firmware version, error response or reconnect sequence. |
| Native vendor utility analysis corroborates AA55 framing and CRC, and exposes USB chunking. | `Native-Findings.md`; REA/Ghidra 12.1.4 evidence IDs and 71-record local evidence export. | Independent static framing evidence and device-info parser; C1 is update-mode restart and must not be mistaken for a read. | A working USB session or equivalent mobile/USB command sets. |

## Drum mobile parity — 18 October 2026, 1.43.0

The amplifier computes TAP BPM from mobile interval commands; desktop does not guess its rounding or idle threshold. Playback is separate from preset state and continues across Tools navigation, including Tuner, as actually observed. All 70 pattern names/IDs are mobile-captured, not inferred from manual counts. Reconnect discards pending writes; only confirmed complete state is retained. The user requested Tuner above Drum in the desktop Tools section.
