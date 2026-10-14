# Bounded Hall Reverb editing — 1.15.0

14 October 2026, F15i V1.1.9 / 0D British Lead Solo, fifth-position type 8/model 1/ten-word block. Phase 5 only; Phase 6 remains excluded.

Pre Delay **18–20 ms**, Level **23–24**, Decay **34–35** and Tone **0–1** can be applied explicitly, individually or together. The enabled/model flags and final four zero words must match the observed profile. Full ranges, other models and physical units for Decay/Tone are not inferred.

ADB operated the authorized iAMP phone app. Tone 0→1→0 isolated word 5 from the five previously indistinguishable trailing zeros. Separate Pre Delay 20→18→20, Level 24→23→24 and Decay 35→34→35 captures established words 2/3/4. Slider gestures can pass through intermediate nearby values; every final restoration was visually checked. A fresh independent Windows read after closing mobile iAMP matched all original state/detail bytes. No preset was recalled or saved.

The desktop now displays the resolved Tone value and offers four guarded editors, using existing serialized fresh-state/full-readback workflows. Forty-eight focused protocol cases pass: 21 new Hall cases plus Delay and shared parameter regressions. Exact independent phone frames, target/source/layout/flag rejection and all 24 bounded source combinations are covered. Every unrelated word and input collection is preserved. Offline UI checks verify all four summary labels/values, pending/invalid/busy/unsupported-firmware/local-import guards and five logical window sizes down to 620×320. No full-suite rerun or computer screenshots.

Actual Windows Apply handlers changed/restored each Hall control, separately verified Pre Delay 19→20, then changed/restored all four controls together. Complete baseline checks followed each individual restoration. A separate fresh connection verified all original state/detail bytes after testing. Private report: analysis/private/reverb-windows-report.json (passed=true, baselineRestored=true, failure=null). Sanitized command provenance: tests/fixtures/f15-observed-reverb.json; raw logs/screenshots/snapshots remain ignored and private.

USB stay-awake remains enabled throughout ongoing development/testing and packaging. Its original value is retained for restoration when the whole session ends or the user asks. Do not request mobile-screen stability checks unless the user asks. Testing permission covers mobile iAMP via ADB and the desktop application; phone screenshots remain limited to iAMP.
