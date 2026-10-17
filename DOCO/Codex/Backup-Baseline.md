# F15i baseline backup and recovery route

Recorded 13 October 2026. The user successfully created a backup using the installed MOOER Studio for Future Series utility. Codex verified the resulting file and its private project copy without modifying either. No restore, firmware update or factory reset was performed by Codex.

| Item | Verified value |
| --- | --- |
| Vendor-created source | `C:/Users/Paul/Documents/Mooer Studio For Future Series/BackupFiles/F15i/2026-10-13 18_34_47.backup` |
| Private project copy | `analysis/private/backups/F15i-baseline.backup` |
| Size of each file | 294,658 bytes |
| SHA-256 of each file | `75d3ee78e313025571dc56e913397f09d00f23b4d099461b6fd67dc1e6e086a8` |
| First 16 bytes | `46313569000000004800000000000000` |
| Integrity comparison | Source and copy hashes match. Header starts with ASCII `F15i`; remaining header fields and file format are not established here. |
| Storage boundary | Project copy is under ignored private analysis storage; do not ship it or publish user presets. |

Hash agreement proves the copy matches the source. It does not independently prove that the backup contains every device setting or can be restored successfully.

## Documented recovery route

The supplied `DOCO/MOOER/F15i Li_Manual_EN_V01_2025.06.19.pdf`, printed pp.64-65 (PDF pp.67-68), documents whole-device backup/restore using MOOER Studio for Future Series:

1. Connect the F15i Li to the PC with USB and connect it in MOOER Studio.
2. Open the utility's Backup section. The manual identifies the backup/restore file-management area beneath the backup control.
3. For an explicitly intended recovery, use the vendor utility's restore operation for the selected compatible backup, following its displayed instructions. A firmware-update prompt is a separate operation and is not part of this recovery baseline.

The initial 13 October baseline exercised backup creation only. Later, the user reported that the MOOER Studio restore succeeded after troubleshooting a failure that occurred after progress started. This is a user-reported successful vendor restore, not independent verification of completeness, interruption handling or the custom file/transfer format. Keep the original vendor file and verified copy. Do not introduce a custom restore encoder based on the observed header.

## Firmware and current-state boundary

Earlier independent Windows BLE reads reported firmware V1.1.2 and current preset wire index 16 / Cali MK3 Lead. These remain valid historical observations, not the current state.

Fresh `analysis/private/phone-reconnect-logcat.txt` reports firmware **V1.1.9** (line166), `British Lead Solo` (lines189 and408), with current slot **0D** reported by the coordinator's phone-state observation. This new session differs from the earlier baseline. Codex performed no firmware update; the cause/timing of the version change is not inferred from these logs. Subsequent independent Windows reads and core editing/save tests verified V1.1.9. Always record a fresh current baseline before another experiment; neither this original slot nor its values may be assumed current.

## Task status

- [x] F15i/phone/USB/Bluetooth access established for the development baseline.
- [x] User-created vendor backup source and private copy recorded with matching hashes.
- [x] Vendor-documented recovery route recorded.
- [x] User-reported successful vendor restore recorded.
- [ ] Independent exhaustive vendor-restore/completeness/interruption acceptance.
- [ ] Full vendor-utility workflow/feature evaluation completed (Plan P0.6).
- [ ] Custom backup/restore file format and transfer semantics validated (P6.6).
- [x] Bounded core editing, reconnect, general save and physical power-cycle persistence checks completed; P5.8 clean/rhythm/lead acceptance completed.
- [ ] Remaining broad fault, usability and deployment gates in Plan.md.

Plan P0.5 is complete because access, baseline backup and a documented recovery route exist. It does not imply independent exhaustive restore validation, full utility evaluation or USB control. Custom backup/restore remains deferred under Phase 6; GIR&IR is excluded.
