# Installed MOOER Studio: static inventory

Inspected 13 October 2026. This report inventories only `C:/Program Files (x86)/Mooer Audio Corp/Mooer Studio For Future Series`. The utility was not executed, installed, updated or connected to hardware during this inspection. Program Files was read only.

## Identity and framework

| Observed item | Result |
| --- | --- |
| Main executable | `C:/Program Files (x86)/Mooer Audio Corp/Mooer Studio For Future Series/MooerStudioForF40i.exe` |
| Size | 652,288 bytes |
| SHA-256 | `56e2cafe196e6f0da378066af5a4c38ec38da1691c5874ade6a696b42a120462` |
| PE header | PE32, machine 0x014c (x86); CLR data directory absent |
| Qt dependencies imported by executable | `Qt5Widgets.dll`, `Qt5Gui.dll`, `Qt5Xml.dll`, `Qt5Network.dll`, `Qt5Core.dll` |
| Other imported DLL families | Microsoft C++ runtime/CRT plus ADVAPI32, USER32, KERNEL32, SHELL32 |
| Classification | Native x86 Qt 5 application. No main-executable CLR header; no Electron/ASAR runtime in the 87-file installation inventory. This is a bounded installation finding, not a claim about every internally embedded component. |
| Local static-analysis copy | `analysis/generated/vendor/MooerStudioForF40i.exe` |
| Full file inventory | `analysis/generated/vendor/inventory.json`: 87 files, byte sizes, SHA-256, PE architecture and imported DLL names where applicable |

The filename references F40i, but the same installation contains explicit F15i resources. Do not infer that the actual amplifier is an F40i from the executable filename. No product version resource, Authenticode verification or license assessment was performed in this pass.

Selected installed DLL hashes:

| File | SHA-256 |
| --- | --- |
| `Qt5Core.dll` | `da9935a35a73105cdae78888b188a859c1ff7ea39527f7bbb548c84e802288ba` |
| `Qt5Gui.dll` | `ce5d9eb724480c2e5826af570cc5fe3321a33f16011c37d6051c49fd9a2ec0a2` |
| `Qt5Widgets.dll` | `37ed33aab70943891cc4657d28fc4d2dd86eb676b3a1185e6e687d25db81a4e9` |
| `Qt5Network.dll` | `bde4330a9a671dc643fd0282934c91a50984a3f44116ae18f82aae551875ae6c` |
| `Qt5Xml.dll` | `3ba18881d25e687184588868db44f8625e9017709d46da4079a33c60cbd48b41` |
| `psvince.dll` | `39d0a95460bbb9400b4cc757dba1eb7587dcf8c820039953b75e793ca5ed59c0` |

## Resource inventory and documented maintenance baseline

Observed resources include `platforms/qwindows.dll`, Qt image-format plugins, SourceSansPro fonts, `Language/Chinese.qm`, `Language/English.qm`, and `Language/Language.xml`. UI image names include `Device_F15i.png`, `Device_F40i.png`, backup/restore controls, third-party IR controls and update controls. Names identify packaged resources; they do not demonstrate working workflows.

`Firmware/F15i/firmware.xml` names F15i firmware V1.1.9 with files `F15i_EMMC_V1.1.9_20260227-1143.mr` and `F15i_FLASH_V1.1.9_20260227-1143.mr`, each 13,057,664 bytes. English release text describes an enhanced looper/drum interface with Drum Sync and real-time loop status. `Firmware/F40i/` separately contains V1.1.2 resources. These are **bundled release metadata**, not a measurement of connected-amplifier firmware, an update recommendation or a parsed firmware format. No firmware image was flashed or executed.

The supplied manual, printed pp.64-65, documents USB firmware update, third-party IR import and whole-device backup/restore using MOOER Studio. This static pass corroborates that relevant resources exist, but does not complete Plan P0.5/P0.6: backup, recovery and actual workflow evaluation remain pending.

## Focused native analysis candidates

All offsets below are **file offsets in the copied executable**, not function addresses. Identifier strings, including Qt metadata and C++ RTTI-style names, are search seeds for references; they do not recover USB commands by themselves.

| Candidate | Static observation | Next focused question |
| --- | --- | --- |
| `QUsbOperation`, `DealData`, `closeUsbConnect` | `QUsbOperation` string at file offset `0x5f460`; corresponding RTTI-style token also present. | Which functions open the selected device and serialize/write data? |
| `QUsbReceiveData`, `emitDealData`, `emitLostConnection` | `QUsbReceiveData` string at `0x5f5d8`. | How are reports read, buffered and passed to packet parsing? |
| `QMEMainWriteThread`, `StartConnectSync`, `RequestFileInfoData`, `receiveDeviceInfo`, `DealData` | Qt metadata identifier cluster around file offset `0x60600`; exact token offsets retained in inventory. | What initialization/state requests exist, and which replies identify firmware/device capabilities? |
| `StartBackupData`, `StartRestoreData`, `receiveBackup` | `StartBackupData` at `0x606be`; `StartRestoreData` at `0x606ce`; `receiveBackup` at `0x607be`. | What file framing, transfer sequencing, acknowledgement and integrity checks are used? Keep restore writes deferred. |
| `StartImportData`, `StartUpdateIrsNameDeleteIrs`, `receiveIrUpdateNameOrDelete`, `receiveFileInfo`, `emitTrafficControl` | Neighboring metadata identifies likely transfer-related entry points. | Which paths implement IR catalog/read operations versus mutations? |
| `HidD_*`, `HidP_*`, `WinUsb_*`, `SetupDi*`, `ReadFile`, `WriteFile`, `DeviceIoControl`, `LIBUSB_*`, `hid_*` | Allowlisted ASCII candidates retained in inventory. | Are these linked backend code, dynamically resolved APIs or actual app call paths? Which backend handles the connected amp? |
| `qChecksum` | String/import-name candidate. | Does application-level data use this function, and on which exact byte ranges? Never infer checksum algorithm from its name. |

The coordinator separately measured the connected amp as USB VID 34DB/PID 000B, HID interface MI03, 65-byte input/output report lengths and zero feature-report length. That runtime device fact is a useful correlation target; this inventory did not independently perform USB I/O. Report length alone does not establish report IDs, payload framing or read/write semantics. The presence of HID and WinUSB code in a library is not proof that both are supported control transports for this amplifier.

## Reproduction and remaining checks

Run from project root in Git Bash, using the bundled Python executable:

```bash
'/c/Users/Paul/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' scripts/vendor-inventory.py
```

The stdlib script hashes installed files, parses PE headers/import DLLs, collects allowlisted identifier candidates and copies only the main executable into ignored local analysis storage. It does not read unrelated installations or write to Program Files. No third-party PE parser was installed.

- [ ] Verify executable version resources and signature separately if required for provenance.
- [ ] Open the copied PE with a verified native analysis provider; locate references to the USB/Qt metadata candidates and follow actual serializer/parser paths.
- [ ] Correlate observed functions with a controlled vendor-utility USB session, retaining timestamped request/reply evidence.
- [ ] Establish read-only initialization and identity/state reads before considering writes.
- [ ] Evaluate vendor backup and recovery route on actual hardware; record backup separately from restore validation.
- [ ] Determine which mobile editing features, if any, the USB transport exposes. Maintenance support alone does not imply full editor support.

No protocol command bytes, checksums, handshake or successful hardware workflow are claimed by this document. Local executable copies and inventory evidence are development inputs and must not be included in the desktop application's distributable package.
