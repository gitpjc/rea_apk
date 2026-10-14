# Pending parameter inputs — 1.21.1

Effect-card navigation previously reset inputs to their confirmed values, silently discarding un-applied changes. Selecting an already initialized effect now reuses its valid or invalid numeric text and slider position. Pending detection includes initialized hidden effect editors, preserving recall confirmation, effect-toggle blocking and polling conflict protection.

After a verified individual parameter write, all previously initialized input text is restored against the new confirmed snapshot. The applied value is now confirmed, while other pending changes remain. An explicit amplifier refresh preserves inputs only if device address, firmware and complete state/detail payloads are unchanged. New presets, imports and changed refresh snapshots initialize new inputs instead; ambiguous writes still invalidate editing and require a fresh read. This is session-local UI state, not persistence or automatic write replay.

The focused offline command in Parameter-Sliders.md covers all 18 controls, including valid/blank pending navigation, hidden effect-power guards, partial Apply/unchanged-refresh preservation and new-snapshot reset. Every integer target, numeric synchronization, disabled guards and five viewport sizes remain covered. No full protocol suite was needed for this UI-state change.

Actual Windows hardware acceptance records a fresh baseline, sets pending Noise Killer Threshold and OD808 Gain together, browses another effect, applies/restores Threshold while retaining pending Gain, applies/restores Gain, then verifies that an unchanged full amplifier refresh retains a new un-applied Gain value. That pending value is locally returned to its confirmed value. A fresh independent BLE session verifies complete original state/detail equality. Private report: analysis/private/pending-windows-report.json, passed=true, baselineRestored=true, failure=null. Nothing saved or recalled.

Broader conflict resolution and persistence remain open. Phase 6 remains excluded; USB stay-awake stays enabled during ongoing development/testing.
