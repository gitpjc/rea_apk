# Hall Reverb observed ranges — 1.19.0

14 October 2026, F15i V1.1.9, fifth-position type 8/model 1/ten-word enabled Hall Reverb profile. Phase 5 only; Phase 6 excluded.

ADB endpoint inspection establishes **Pre Delay 0–200 ms**, **Level 0–100**, **Decay 0–100**, **Tone 0–100**. Each slider was moved to both endpoints and restored before the next control. Original values remain **20,24,35,0**. Exact complete-block frames corroborate direct integers at words 2–5, with the displayed millisecond suffix on Pre Delay. After closing phone iAMP, a separate fresh Windows connection matched every original state/detail byte. No recalls or saves.

The desktop supports all four observed ranges together. Its source-profile guard retains enabled/model flags and four trailing zeros, rejects unknown layouts/firmware/values and uses fresh full-snapshot/full-readback verification. Three-digit Pre Delay entry supports 200. Decay and Tone remain unitless; no physical time/frequency scale or other-model support is inferred.

Fifty-five affected protocol cases pass: 24 new range cases, 21 existing Hall cases and 10 unchanged saving cases. Exact independent endpoint/restoration frames, every integer target, all 16 extreme source combinations, unsupported targets/profiles/flags, unrelated-word preservation, input immutability and checksummed-frame decode are covered. Earlier nearby-value fixtures remain intact. The full suite was not rerun. Offline input/pending/busy/firmware/import and five-size layout checks pass down to 620×320.

Actual desktop Apply handlers verified Pre Delay **0,1,20,50,100,199,200** and **0,1,25,50,75,99,100** for the other controls, restoring the complete baseline after each sequence. All 16 endpoint combinations were read back in Gray-code order (one endpoint change between successive combinations), then original values were restored. A separate fresh connection verified every original byte. Private report: analysis/private/reverb-range-windows-report.json (passed=true, baselineRestored=true, failure=null). Sanitized fixture: tests/fixtures/f15-observed-reverb-ranges.json.

Saving remains limited to original 0D content with Noise Killer Threshold 25/26 and every other original value unchanged. No persistence expansion or other firmware/algorithm support is claimed. USB stay-awake remains enabled during ongoing testing/development; no computer screenshots or mobile-screen stability checks.
