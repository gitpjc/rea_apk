# PLX 100 Mid editing — 1.11.0

Validated 14 October 2026 on V1.1.9 / 0D British Lead Solo. Phase 5 only; Phase 6 remains excluded.

The independent baseline has enabled PLX 100 `[1,31,100,35,43,40,45,75,1]`. Phone command logs confirm Mid is word 4 (zero-based), using the existing complete-block opcode 25. Exact Mid 42 and restored 43 frames are regression-tested. The user's slider briefly traversed 44–46; those values remain unsupported in Windows.

Added explicit Mid Apply, limited to 42–43. Bass 34–35 and Mid 42–43 may coexist in confirmed live state; every other Amp word still must match the captured profile. Firmware, fresh complete snapshot, command serialization and complete state/detail readback guards are unchanged. No saving or automatic replay was added.

Thirty-eight focused protocol cases pass: new Mid exact-frame/field/range/combination cases plus affected Bass and existing-parameter regressions. Offline UI checks pass valid/invalid input, pending-effect protection, busy, unsupported firmware and local-import guards. The full suite was not rerun.

Actual Windows handlers verified Mid 43→42, Bass 35→34 while Mid stayed 42, Bass restored 35 while preserving Mid 42, then Mid restored 43. A fresh independent connection matched every original state/detail byte. Private report `analysis/private/amp-mid-windows-report.json` has passed/baselineRestored true and no failure. This validates the two bounded controls together, not all device ranges or persisted storage.

The user then authorized ADB phone testing and iAMP-only phone screenshots. ADB selected the Amp category and performed a separate Mid 43→42→43 gesture test, with visual and command-log evidence. iAMP exposes no useful labelled controls in the UI hierarchy. A small swipe stayed below touch slop and changed nothing; a multi-segment touch gesture reached 42 and restored 43. Intermediate gesture movement is not an expansion of supported Windows values. Private phone images/logs remain ignored. No computer screenshots were taken.

After closing phone iAMP through ADB, an independent Windows read again matched the full original baseline, Mid 43 and Bass 35. `analysis/private/amp-mid-adb-report.json` records passed/baselineRestored true and saved false. The user-authorized temporary USB stay-awake setting was restored to its original value and verified. See `ADB-Testing.md` for ongoing testing preferences. No mobile-screen stability check was requested; no preset was saved.
