# Bounded Analog Delay editing — 1.14.0

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026, F15i V1.1.9 / 0D British Lead Solo, fourth-position type 7/model 1/eight-word block. Phase 5 only; Phase 6 remains excluded.

Level **16–17**, Feedback **24–25** and Time **499–500 ms** can be applied explicitly, individually or together. Enable/model flags and all trailing zero words must match the observed profile. These are verified experimental intervals; no full hardware range, tempo/tap semantics or other-model support is claimed.

ADB operated iAMP on the unlocked USB-connected phone. Level 17→16→17 and Feedback 25→24→25 each preserved the other values. The Time slider initially reached 474 ms; returning to the original thumb location rounded to 499 ms. An inspected subpixel ADB motion restored exactly 500 ms. Captured complete-block opcode 25 frames independently map Level/Feedback/Time to words 2/3/4; milliseconds are both displayed and directly represented at 499/500. Only 499–500 is enabled by this release. A fresh Windows read after closing phone iAMP matched every original state/detail byte.

The new editors use the existing serialized fresh-snapshot/write/full-readback workflow. Thirty-eight relevant protocol cases pass: 16 new Delay cases plus shared parameter and Amp regressions. They check exact independent phone frames, unsupported targets/layouts/flags/source values, all eight bounded source combinations, preservation of unrelated words and input immutability. Offline checks cover invalid/pending/busy/unsupported-firmware/import guards and all three input/action pairs at five logical window sizes down to 620×320. No full-suite rerun or computer screenshot was required.

Actual Windows Apply handlers changed/restored Level, Feedback and Time individually with complete baseline checks after each, then changed all three together and restored them in reverse order. A separate fresh connection matched the complete baseline. Private report: analysis/private/delay-windows-report.json, passed=true, baselineRestored=true, failure=null. No presets were recalled or saved. Sanitized frame provenance is in tests/fixtures/f15-observed-delay.json; raw logs, phone screenshots and snapshots remain ignored/private.

The user's ongoing permission covers ADB operation/testing in iAMP and testing the desktop application. iAMP-only phone screenshots remain permitted. USB stay-awake remains enabled throughout ongoing development/testing; its original setting is retained for restoration at the end of the session or on request. Do not request mobile stability checks unless asked.
