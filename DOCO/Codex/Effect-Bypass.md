# Effect enabled/bypassed state — 1.7.0

Validated 14 October 2026 on F15i firmware V1.1.9. Phase 5 only; Phase 6 remains excluded.

## Presentation update — 1.7.1

Supported effect buttons now show only the icon and category name. Grey/category color indicates bypassed/enabled state; On/Off text has been removed. The details panel still shows confirmed status and Enable/Disable controls. Focused offline UI checks pass; no hardware or screenshot access was needed. The 1.7.0 presentation description below records that earlier milestone.

## Evidence and supported scope

The earlier desktop painted effect buttons using category colors without interpreting enabled state. Phone logs retained the user's Noise Killer off/on actions. A fresh independent Windows baseline confirmed 0D British Lead Solo with Noise Killer `[0,2,25,50,0,0]`, and enabled OD808, PLX 100, Analog Delay and Hall Reverb.

At the user's confirmed quiet output setting, the user switched the other four effects off/back on individually in phone iAMP, without saving. Captured phone frames use the existing complete-block `25 position type byteCount valuesLE16` command. The first value is 0 for bypassed and 1 for enabled. Only these five model/layout combinations are accepted; unknown states, models or lengths remain unsupported. Phone-frame tests cover exact full frames/CRCs for both states of every supported model.

Windows testing exposed an additional behavior: PLX 100 mirrors on/off into its ninth word on readback. The first strict single-field check stopped at this unexpected change. A restoration command and an independent fresh connection verified every original byte restored. The implementation now expects exactly the two observed Amp flags to change, preserving all tone parameters. The meaning of the ninth word beyond this linked on/off behavior is not established; do not extrapolate it into an independent cabinet control.

## Implementation

- Disabled effects have grey mobile category icons, grey labels and an explicit Off indicator; enabled effects use category colors and On. Unknown model states are labelled unknown.
- Selecting an effect shows its confirmed Enabled/Disabled status and Enable effect or Disable effect control. Viewing a bypassed effect remains possible.
- On/off controls require a fresh supported live V1.1.9 snapshot. Offline/preview, invalid snapshots, active operations and conflicting pending parameter inputs block switching.
- The API serializes the complete identity/read/compare/write/readback workflow. It rejects stale complete snapshots before writing and compares all returned state/detail bytes with the exact expected change. PLX 100 includes its observed linked flag; every other effect changes only its first word.
- These are live unsaved changes. No save command, preset recall, automatic replay or expansion of parameter ranges is added. Existing dirty-state confirmation applies before recalling another preset.

Five PNG icon assets are the matching category symbols from the user-supplied phone APK, not screenshots. See `assets/effects/README.md` for paths and hashes. The desktop uses the PNG alpha masks to apply category/disabled colors. No computer or phone screenshots were taken for this increment.

## Validation

- Thirteen focused protocol cases passed: ten independent phone frames, unknown layouts/states, opaque word preservation and the Amp linked readback rule. The full suite was not rerun.
- Focused offline UI checks passed for five icon assets, grey Noise Killer/Off presentation, Enable/Disable labels and preview write blocking.
- Actual desktop toggle handlers passed individually for all five effects. Each change was read back and restored before the next effect; complete original state/detail equality was required after each restoration.
- Final independent connection matched every baseline state/detail byte. Original 0D stayed selected, Noise Killer finished disabled, and all four other effects finished enabled. No tone parameters or saved presets were changed.
- The user reconnected phone iAMP and reported a stable normal effects screen with Noise Killer still disabled. This does not resolve the historical transfer-screen loop cause. **User preference: do not request further mobile-screen stability checks unless the user explicitly asks.**

Private evidence: `analysis/private/effect-toggle-baseline.json`, `effect-toggle-flutter.log`, `effect-toggle-phone-roundtrip.log`, `effect-toggle-frames.json`, `effect-toggle-windows-initial-failure.json`, `effect-toggle-after-failure.json`, `effect-toggle-amp-recovery-independent.json`, and final `effect-toggle-windows-report.json` (`passed: true`, `baselineRestored: true`, five restored results, no failure). These remain excluded from Git. Testing did not change phone settings.

## Remaining limits

Other models, firmware versions, independent linked-flag control and bypass persistence across save/power-cycle are not validated. Existing saving remains restricted to the earlier exact 0D profile; the user's disabled Noise Killer baseline does not broaden that saving guard. Phase 6 advanced functions remain deferred.
