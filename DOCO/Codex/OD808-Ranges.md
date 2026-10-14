# OD808 Gain, Tone and Volume 0–100 — 1.17.0

14 October 2026, F15i V1.1.9, second-position type 1/model 3/five-word enabled OD808 profile. Phase 5 only; Phase 6 excluded.

ADB inspection establishes mobile Gain, Tone and Vol slider limits at **0–100**. Separate endpoint tests preserve the other parameters and restore Gain **39**, Tone **100**, Vol **100**. Exact complete-block commands corroborate direct integers at words 2/3/4. After closing phone iAMP, a fresh Windows connection matched every original state/detail byte. The first phone connection attempt returned to the home screen; retrying the same device row succeeded. No mobile stability check was requested.

The desktop allows all three 0–100 values together, preserving enabled/model flags. Unknown models, disabled profiles, invalid source values and unsupported firmware remain rejected. UI and protocol share source-profile validation. Pending Tone input now refreshes all dependent controls immediately; the focused check caught the prior stale Enable/Disable button state. Hardware action guards already reject pending-input toggles.

Forty-two focused protocol cases pass: 17 new OD range cases plus existing Tone/shared-parameter and unchanged saving-guard cases. Tests match independent endpoint/restoration frames, reject unsupported targets/profiles, cover every integer target and exercise a 216-source representative cube with 0/50/100 targets for all three encoders. They verify unrelated words, input immutability and frame decode. This is software coverage, not exhaustive physical testing. Offline UI checks cover input/pending/busy/firmware/import states and five sizes down to 620×320. The full test suite was not rerun.

Actual desktop Apply handlers verified **0, 1, 25, 50, 75, 99, 100** for each control, restoring the complete original baseline after each sequence. All eight endpoint combinations were then read back and the original configuration restored. A separate fresh connection verified every original byte. Private report: analysis/private/od-range-windows-report.json (passed=true, baselineRestored=true, failure=null). Sanitized frame fixture: tests/fixtures/f15-observed-od-ranges.json. No preset recalls or saves.

Same-slot saving remains limited to the original 0D profile with Noise Killer Threshold 25/26 and every other original value unchanged. This release does not broaden persistence, other algorithms or firmware. USB stay-awake remains enabled throughout ongoing testing/development; no computer screenshots.
