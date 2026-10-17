# PLX 100 Bass editing — 1.10.0 acceptance

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026. The accepted source/package increment is 1.10.0. Phase 6 remains excluded.

Independent read-only baseline: V1.1.9, 0D British Lead Solo, enabled PLX 100 `[1,31,100,35,43,40,45,75,1]`. Noise Killer is now enabled in this user's latest baseline; preserve this current state rather than reverting to the earlier disabled baseline.

Prepared an explicit Bass Apply control for position 3/type 2/model 31, limited to 34–35 with every other captured Amp word unchanged. It uses the existing serialized fresh full-snapshot guard and complete state/detail readback comparison. Unknown layouts, other values, unsupported firmware, busy operations and local imports block editing. Pending Bass input blocks effect switching.

Twenty-five focused protocol tests pass (new Bass guards plus affected existing parameter regressions), and focused offline UI checks pass for valid/invalid inputs, busy, firmware, pending-input protection and local-import guards. The Release build has zero warnings/errors. These software checks do not establish the new command's mobile or hardware acceptance.

- [x] Record the current independent baseline (`analysis/private/amp-bass-baseline.json`).
- [x] Prepare the bounded API/UI and focused offline checks.
- [x] Capture phone Bass 35 → 34 → 35 without saving; independently confirm all baseline bytes restored.
- [x] Match the encoder against the exact captured phone frames.
- [x] Run actual Windows Apply 35 → 34 → 35; independently restore/verify every original state/detail byte.
- [x] Package the accepted increment and update the completed Plan checklist. Published binary/history equality is checked during promotion.

Phone logs captured exact Bass 34/35 commands and transient slider movement through 36; 36 remains unsupported in Windows. Actual Windows handlers passed 35→34→35 with `passed: true`, `baselineRestored: true`, no failure in `analysis/private/amp-bass-windows-report.json`. The complete independent pre-phone baseline matched before Windows writes and after restoration. No preset saves were sent. Private evidence includes `amp-bass-phone.log`, `amp-bass-phone-frames.json`, `amp-bass-windows.log` and the report. No screenshots or phone-setting changes were made. No mobile-screen stability check will be requested unless the user asks.
