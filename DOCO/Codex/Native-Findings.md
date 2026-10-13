# Vendor utility native protocol findings

Inspected 13 October 2026 with REA MCP and Ghidra 12.1.4. Target: the read-only local copy `analysis/generated/vendor/MooerStudioForF40i.exe`, SHA-256 `56e2cafe196e6f0da378066af5a4c38ec38da1691c5874ade6a696b42a120462`. No vendor executable was run and no USB request was sent during this analysis. Addresses below are Ghidra virtual addresses, not file offsets. Decompiled names beginning `FUN_` are generated analysis labels.

## Established framing

`FUN_0042a880` reads a little-endian 16-bit payload length from frame offset 2, computes CRC over the two length bytes plus payload, writes the CRC high byte at `length + 4` and low byte at `length + 5`, and sends `length + 6` bytes. The request construction sites inspected use `AA 55` at offsets 0–1. Evidence: `ev_bdf6ffae741a88dc560d0c5730ba2e6a04c5de343f3acbea429a663f797942fe`, retained in `analysis/generated/native/usb-serializer-pseudo.json`.

`FUN_00412510` initializes the CRC accumulator to zero, shifts left eight bits per input byte using the 256-entry ushort table at `0x483000`, and returns its complement. All 256 table entries were compared with independently generated non-reflected polynomial `0x1021` entries and matched. Thus the 16-bit result is CRC-16/GSM: polynomial `0x1021`, init `0x0000`, xorout `0xFFFF`, refin/refout false. Evidence: `ev_748114825ebd565067a7b9a10b5fcdcad24c8be9f7c8bda4ab94b4ecdd1bd030`; table in `usb-crc-table.json`; reproducible comparison in `scripts/native-metadata.mjs`.

This matches the mobile BLE framing independently established in `Protocol.md`. It establishes a shared frame envelope, not complete USB/BLE command compatibility.

## USB report wrapping

`FUN_004127f0` splits a complete frame into chunks of at most 63 bytes. It submits a 64-byte buffer for each chunk: byte 0 is chunk length, followed by chunk data. The last partial chunk is still submitted as a 64-byte buffer; the recovered code does not clear unused trailing bytes between chunks. A new implementation should zero padding. Evidence: `ev_a29d3129d0474c9ced9a41a0ae0b6b2e328ee8a5e9d7f5a8df3e88dcf76d86c5`, `usb-report-send-pseudo.json`.

The embedded HID backend `FUN_00449110` adds a zero report-ID byte when the device uses unnumbered output reports. That explains how the vendor's 64-byte transfer can correspond to a Windows 65-byte HID report, but this specific amplifier path still needs runtime correlation. Evidence: `ev_fbce24fad38047fb5957d365d93eb917176247aaea196420fd744b2426345414`. The function is embedded libusb backend code; its presence alone is not proof that every app connection uses that backend.

## Commands and identity response

Qt method strings and actual dispatch `FUN_0042f010` distinguish methods that would otherwise be easy to misidentify. Evidence: `ev_7cce845732660ed33fcb35000773b1e68eb19b3866d722b529936f1725f66753`; names retained in `main-string-table.json`.

| Native location | Observed behavior | Boundary |
| --- | --- | --- |
| `0x41f7e0` | `StartConnectSync` selects worker mode 1 and starts QThread. The virtual run entry `0x420780` routes mode 1 to `0x41dfc0`. | This synchronizes file catalogs after device identity is already known. |
| `0x41efd0` | `RequestFileInfoData` constructs payload `65 type startIndexLE16 endIndexLE16`, length 6. Synchronization requests types 1 and 2 in batches of ten. | Static metadata-read candidate; not sent or validated on hardware. |
| `0x41f0c0` | `RestartDeviceToUpdate` constructs length-1 payload `C1`. | **Do not treat C1 as an identity/read command. It requests update-mode restart.** |
| `0x41e210` | `DealData` dispatches incoming opcode `C4` to `0x41feb0` (`receiveDeviceInfo`); `C2` is associated with update restart acknowledgement. | Static response classification, not a captured USB response. |
| `0x41feb0` | Copies device name from full frame offset `0x05` (32 bytes), state from `0x25`, firmware type from `0x26`, app version from `0x27` (7 bytes). Reads additional counts at `0x2e`–`0x33`. Debug labels explicitly name device name, app version, state, current firmware type, GIR, GNR, drum and looper counts. | String termination, numeric semantics and actual device values need runtime validation. |

Identity parser evidence: `ev_4abd71117f73aaf747fa634e7f5649d928d5f59ab106b0c2667764b9f68c5d7d`; opcode dispatch: `ev_451ec98399136274807efd19e3ed68b0f7c5d15e8be8972bffad1383e57b0c50`; file request: `ev_e75347ef80a0c07e3b54b7e5f5530b107fef80a2db7e293428ae86f9af61af18`; confirmed virtual worker: `ev_0fbf1a557d35fb0741dc6434bbfba0106f5e0d2b7823594e25d1601a65b079ea`.

The imported `qChecksum` is called by `0x43dcf0` while deriving an application/process session identifier from a path and current session. It is not the transport CRC implementation. Evidence: `ev_5a99e9622025394fe45a5879bad49c13f0712d0ff26b7a04b62568a8e79db09f`.

## Remaining work

- [x] Run actual REA/Ghidra native analysis of the vendor executable.
- [x] Establish frame length encoding, CRC parameters, covered range and trailer byte order.
- [x] Establish static USB chunk length wrapping and identify Windows report-ID handling.
- [x] Locate device-info response parser and distinguish update restart from reads.
- [ ] Locate and validate the actual device-info request and initial product-selection/handshake sequence. A generic lower-level `HIDRequestVersion_PC2MCU` path exists at `0x412550`, but its product-specific mode is not yet established.
- [ ] Capture a controlled vendor utility USB read-only session and correlate framing/backend against this amplifier.
- [ ] Confirm HID input reassembly and connection/disconnection sequencing before using the USB implementation.
- [ ] Establish whether USB exposes mobile editing operations, beyond maintenance and catalog reads.

Generated evidence and binary copies remain ignored local analysis inputs. Use `scripts/rea-native-session.mjs` with its explicit queued jobs file to reproduce a persistent session; when absent, it creates a minimal set of framing/identity inspection jobs. Append jobs before the final STOP entry for subsequent static investigation. The script closes the native target in `finally`. No Python is required.

The completed session exported 71 evidence records to `analysis/generated/native/vendor-evidence.json` (1,640,812 bytes), and `close_binary` successfully wrote `vendor-snapshot.json` (1,649,022 bytes). The snapshot reports zero primitive entries, three workflow entries and 71 evidence records: it preserves evidence, but does not imply a reusable full Ghidra database or that arbitrary new queries can avoid fresh analysis.
