# PLX 100 six-control 0–100 ranges — 1.18.0

14 October 2026, F15i V1.1.9, third-position type 2/model 31/nine-word enabled PLX 100 profile. Phase 5 only; Phase 6 excluded.

ADB inspection establishes **Gain, Bass, Mid, Treble, Presence and Master: 0–100**. Each phone slider was moved to both endpoints and restored before testing the next. Original values remain **100,35,43,40,45,75**. Exact complete-block commands corroborate direct integer words 2–7 while preserving enabled/model/linked flags. After closing phone iAMP, a fresh independent Windows read matched every original state/detail byte. No recalls or saves.

All six desktop values can now coexist across 0–100. Source/layout/firmware guards remain strict and every write still compares the fresh full snapshot before sending, then verifies the complete expected readback. Unknown models, invalid source values and disabled/unfamiliar linked flags remain rejected. Range labels now match observed support.

Ninety-five affected protocol cases pass: 36 new range cases, 49 existing Amp cases and 10 strict saving cases. Exact independent endpoint/restoration frames, unsupported targets/profiles/flags, every integer target and all 64 extreme source combinations are covered in software, preserving all unrelated words/input collections and round-tripping checksummed frames. Historical exact nearby-value tests remain in place; rejection tests now target genuinely out-of-range values. The full suite was not rerun.

Offline UI checks validate endpoints/interior inputs, unchanged-value Apply state (including original Master 75), invalid input, pending effect-power protection, busy state, unsupported firmware, local imports and five sizes down to 620×320. The first offline test incorrectly expected unchanged Master 75 to enable Apply; its expectation was corrected. No application change was needed for that test issue.

Actual desktop handlers separately verified **0,1,25,50,75,99,100** for each control, restoring the full baseline after each sequence. Combined all-minimum, all-maximum and two alternating extreme profiles were read back and restored. Master was lowered first and raised last when building combined profiles. A separate fresh connection verified every original byte. Private report: analysis/private/amp-range-windows-report.json (passed=true, baselineRestored=true, failure=null). Sanitized fixture: tests/fixtures/f15-observed-amp-ranges.json. The 64 software combinations were not all physically exercised.

Saving remains restricted to the original 0D profile with Noise Killer Threshold 25/26 and every other original value unchanged. No persistence expansion, other models, physical parameter units or other firmware support is inferred. USB stay-awake remains enabled during ongoing testing/development; no computer screenshots or mobile-screen stability checks.
