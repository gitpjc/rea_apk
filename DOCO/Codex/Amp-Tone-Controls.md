# Bounded PLX 100 tone controls — 1.12.0

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026, V1.1.9 / 0D British Lead Solo. Phase 5 only; Phase 6 remains excluded.

Added Treble **39–40** and Presence **44–45**, alongside Bass **34–35** and Mid **42–43**. All four verified intervals may coexist; Gain 100, Master 75, enabled/model/linked flags and the third-position PLX 100 layout remain required. These are verified experimental intervals, not full device ranges.

ADB phone tests identified Treble at word 5 and Presence at word 6 (zero-based) in the complete-block opcode 25 command. The phone showed Treble 40→39→40 and Presence 45→44→45. One restore gesture rounded to 39; the image check caught it and a one-pixel correction restored exactly 40 before moving on. Independent Windows readback after closing phone iAMP matched every original state/detail byte. No preset was saved.

The desktop uses the existing full fresh-snapshot guard, serialized write/readback workflow and exact expected whole-payload comparison. Forty-nine relevant protocol tests pass, including exact ADB-captured frames, unsupported targets/layouts/fixed fields, and all 16 bounded four-control combinations. Offline UI checks cover valid/invalid input, pending-effect protection, busy, unsupported firmware and local-import guards. The full suite was not rerun.

Actual Windows handlers tested Treble and Presence individually with complete baseline restoration after each, then changed all four supported Amp tone controls together and restored them in reverse order. A fresh independent connection verified complete original equality afterward. `analysis/private/amp-eq-windows-report.json` records passed/baselineRestored true and no failure. Phone evidence is retained privately in `amp-eq-adb.log`, `amp-eq-phone-frames.json`, `amp-eq-phone-restored.json` and the authorized `adb-*.png` captures.

USB stay-awake remains enabled across the ongoing testing/development session, including between hardware tests and packaging, as the user clarified. Its original value is backed up privately; restore it when the entire session is finished or the user asks, not after each increment. No computer screenshots or mobile-screen stability checks were taken/requested. Gain/Master editing and full parameter ranges remain separate work.
