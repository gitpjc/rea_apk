# Read-only battery information — 1.9.0

Phase 5 P5.3, validated 14 October 2026 on F15i firmware V1.1.9. Phase 6 remains excluded.

## Protocol evidence

Retained target-attributed phone captures show repeated request `AA550100915B57` (payload `91`) and CRC-valid three-byte replies `92 percentage powerState`. Examples `924D01`, `924E01`, `924F01`, `925001` correspond to 77–80; the phone's own log explicitly reports battery 77 and 78 in the associated capture. Sources: private `phone-threshold-capture.json`, `phone-threshold-roundtrip-logcat.txt`, with further replies in `phone-tone-capture.json`. This supersedes the earlier unresolved battery interpretation in `Protocol.md` for this specific query. Identity request `0101` remains unrelated.

The parser requires exactly three bytes, opcode 92 and percentage 0–100. The final power-state byte is retained opaque. No charging/discharging labels or independent charging control are inferred.

## Application behavior

Amplifier Information shows battery percentage and the read time beneath snapshot status. Connect/Refresh reads it for V1.1.9; other firmware is explicitly marked unverified. While connected, the existing timer performs a serialized read approximately every thirty seconds, subject to other operations. It does not introduce another connection or replay failed commands. Current-preset polling stays at three seconds.

Disconnected values retain their read timestamp alongside the existing disconnected snapshot status. Local snapshot imports clear battery information and say it is not included in that file; the archive format is unchanged. No new device writes, recalls, saves, global audio controls or Phase 6 features are introduced.

## Acceptance

- Ten focused protocol cases pass for captured responses, malformed lengths/opcodes, out-of-range values, percentage endpoints and opaque power-state retention.
- Focused offline UI checks pass for percentage/time display and local-import clearing.
- Actual desktop Connect loaded `100%`; a due refresh through the production polling method succeeded. The check made its timer due to exercise the real thirty-second branch without waiting. It does not constitute a long-duration timing test.
- Fresh independent connections before and after matched complete original state and preset-detail payloads. No preset values or phone settings changed.
- Navigation/dropdown responsive regression checks cover the additional information text without screenshots or hardware.

Private hardware report: `analysis/private/battery-read-report.json` (`passed: true`, before/after raw state/detail equality, no failure). Full-suite testing was not required for this isolated increment. No further phone stability observation was requested, respecting the user's preference.
