# Read-only Windows-to-phone handoff checks

14 October 2026. These bounded checks followed recovery of the [mobile transfer-screen loop](Mobile-Transfer-Loop.md). They used existing version **1.2.0** production components before the defensive 1.2.1 patch. No preset selection, parameter edit or save was performed. Windows information requests and notification subscription/disposal were exercised; phone initialization issued its usual information/catalog requests.

| Windows operation / amp USB condition | Phone BLE connection / effects screen time | Final passive connection / frames | Result |
| --- | --- | --- | --- |
| DeviceProbe `read-current`; amp USB absent | 12:56:38.248 / 12:56:50.861; observed through 12:59:23 | Event 9567; 45 application frames | Zero 9B frames; complete original state/detail match |
| Published desktop `--read-preview` actual Read handler; amp USB absent | 13:01:03.026 / 13:01:15.489; observed through 13:03:05 | Event 9545; 44 application frames | Zero 9B frames; complete original state/detail match |
| Published desktop `--read-preview`; amp USB attached to PC | 13:07:09.555 / 13:07:22.060; observed through 13:09:43 | Event 9497; 45 application frames | Zero 9B frames; complete original state/detail match |

Times are recorded local phone/host labels; no independently calibrated timing precision is claimed. Each observation lasted longer than the earlier roughly 47-second loop onset. No transfer-route push/pop was present in these reconnect logs. The user separately reported that reconnecting USB alone left the phone stable, with Windows amplifier apps closed; USB metadata confirmed the target interface was present. That is a bounded negative result, not proof that USB can never contribute.

All three phone captures match the original 92-byte global state and 160-byte British Lead Solo / wire-4 detail. Detail SHA-256 remains `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`; Threshold 25 / Level 50 and the other original values remain intact. No Windows connection remained open. The phone's temporary USB stay-awake setting was restored to its original **0** and read back; iAMP was left connected. Further hardware comparisons stopped when the user said they would report a recurrence.

Private evidence prefixes: `handoff-phone-after`, `handoff-ui-phone-after`, `handoff-usb-phone-after`, each with logcat/dumpsys/btsnoop/parsed-capture/verification JSON. Windows evidence includes `handoff-windows-readonly.json`, `handoff-desktop-read.png`, `handoff-usb-desktop-read.png`; USB checks are `handoff-usb-before.json` and `handoff-usb-connected.json`.

## Defensive patch 1.2.1

The Windows transport now uses `NotificationSessionGuard` to inspect complete decoded notification batches before resolving a pending response. Any checksum-valid frame with discriminator `9B` permanently invalidates that session, regardless of the status byte. A subsequent `9B 01`, ordinary acknowledgement or decoder reset cannot re-enable it. Queued and in-flight operations fail through `TransferStateChangedException`; write initiation shares the notification gate, and there is no automatic replay. Read and Save status text explains the transfer-state interruption; a failed save remains potentially ambiguous.

Independent captured `9B 00`, `9B 01` and acknowledgement frames are retained in `tests/fixtures/f15-observed-transfer-status.json`. Offline tests cover arbitrary fragmentation, both coalesced response/status orders, sticky invalidation, unknown status values and corrupted frames rejected before the guard. **71 protocol tests pass.** Release build/publish has zero warnings/errors; navigation, local import/export and offline Save enablement checks pass. The published executable/DLLs and history asset match tested staging/source. No real transfer-status event was injected into hardware to test the patch, and no further save test was performed after it.

These checks did not reproduce the original loop. The guard is a protective response to observed notifications, not a fix for the amp/mobile state or proof of its original cause. Broader save-to-phone handoff, failure/recovery and lifecycle acceptance remain open.
