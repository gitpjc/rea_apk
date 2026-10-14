# Analog Delay observed ranges — 1.20.0

14 October 2026, F15i V1.1.9, enabled fourth-position type 7/model 1/eight-word Analog Delay profile. Phase 5 only; Phase 6 excluded.

Authorized ADB inspection establishes **Level 0–100**, **Feedback 0–100**, **Time 40–2500 ms**. Exact complete-block commands corroborate words 2–4; the app displays milliseconds for Time. Feedback endpoints were captured with Level held at zero, which the fixtures preserve. Original values **17,25,500** were restored exactly, then a separate Windows connection verified complete original state/detail payload equality. No saves or preset recalls.

Desktop source-profile guards preserve enabled/model flags and three trailing zeros, reject unknown layouts/firmware/out-of-range values and verify every write against a fresh complete snapshot and complete readback. Time entry now permits four digits; other input lengths derive from their supported maxima. The shared UI shows Supported range consistently for all verified profiles.

**43 affected protocol/save-guard cases pass**, including exact phone endpoint/restoration frames, every integer target, all eight extreme source profiles in software, invalid targets/source layouts, unrelated-word preservation and checksummed-frame decode. Earlier nearby fixtures remain intact. Offline input/pending/busy/firmware/import checks and five-size geometry checks down to 620×320 pass. The full suite was not rerun.

Actual Windows Apply handlers verify Level 0,1,17,50,75,99,100; Feedback 0,1,25,50,75,99,100 while Level is zero; and Time 40,41,500,999,1000,1250,2499,2500. Each sequence returns to the complete original baseline. Combined Level/Feedback/Time values [0,100,2500], [100,0,40] and [50,50,1250] also pass. A fresh independent session verifies every original state/detail byte after restoration. Private report: analysis/private/delay-range-windows-report.json; sanitized evidence: tests/fixtures/f15-observed-delay-ranges.json.

Saving remains limited to the separately verified original 0D configuration with Noise Killer Threshold 25/26. No persistence expansion, unknown trailing-field interpretation or other-algorithm support is claimed. USB stay-awake remains enabled throughout ongoing development/testing.
