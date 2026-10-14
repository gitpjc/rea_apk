# Noise Killer Threshold and Level 0–100 — 1.16.0

14 October 2026, F15i V1.1.9, first-position type 4/model 2/six-word enabled Noise Killer profile. Phase 5 only; Phase 6 excluded.

The mobile sliders clamp at **0** and **100** for both Threshold and Level. ADB operated both endpoints and restored the original Threshold **25**, Level **50**. Inspected phone screenshots and exact complete-block frames corroborate the limits and direct integer encoding at words 2/3. A fresh independent Windows connection matched every original state/detail byte after phone testing. No preset was recalled or saved.

The desktop supports integers 0–100 for both values together. It preserves the model/enabled flags and trailing zero words, rejects unfamiliar profiles and still uses serialized fresh-snapshot/full-readback guards. Threshold accepts three digits and its accessible name/range message now match 0–100. UI and protocol use the same source-profile validation.

Thirty-eight focused protocol cases pass, including captured endpoint/restoration frames, invalid targets/sources/profiles, legacy parameter regressions and the unchanged strict saving guard. Software checks enumerate all 10,201 Threshold/Level integer source combinations, exercise both encoders with complementary targets, verify every unrelated word/input collection, and decode the resulting checksummed frame. This is exhaustive source-encoding coverage, not a claim that all 10,201 combinations were physically tested.

Actual desktop Apply handlers separately verified **0, 1, 25, 50, 75, 99, 100** for each control, restoring the full original baseline after each control's sequence. They also verified all four endpoint combinations **(0,0), (0,100), (100,100), (100,0)** and restored **(25,50)**. A separate fresh connection verified all original bytes. Private report: analysis/private/noise-range-windows-report.json (passed=true, baselineRestored=true, failure=null). Sanitized fixture: tests/fixtures/f15-observed-noise-ranges.json. The full test suite was not rerun.

Offline UI checks cover endpoints/interior inputs, negative/out-of-range/malformed text, pending-edit protection, busy state, unsupported firmware, local snapshot imports and five window sizes down to 620×320. No computer screenshots or mobile stability checks. USB stay-awake remains enabled across ongoing development/testing.

Saving remains restricted to the original 0D/British Lead Solo full profile with Threshold 25 or 26 and all other original content unchanged. Expanding live ranges does not authorize or enable broader persistence, other algorithms or other firmware.
