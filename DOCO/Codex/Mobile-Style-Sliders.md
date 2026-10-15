# Mobile-style effect sliders — 1.24.0

15 October 2026. All 18 supported parameter sliders now measure exactly half the effect-details panel width, aligned left. Their grey track and coloured filled portion are rounded, and the white circular thumb has an effect-coloured centre. The colour comes from the same EffectPresentation colour as the effect icon; Delay uses mobile-style green (#52DC38). Disabled controls use the existing muted colour. Parameter labels appear above the track, with live values right-aligned and milliseconds shown for the two verified time controls.

## Interaction

Pointer capture permits tapping or dragging anywhere along the track and releasing outside its bounds; values clamp to the already verified integer range. The live label and fill follow movement. Releasing commits only the final value, using the same guarded Apply handler as numeric entry. This coalesces a whole drag into one checked write/readback instead of sending every mouse-move event. Arrow/Page/Home/End adjustments commit on key release. Pointer Escape/capture cancellation does not apply; keyboard focus loss cancels its pending automatic commit. Busy, preview, invalid, disabled and detached sliders cannot commit. Programmatic values and typed-input synchronization never auto-apply. Numeric entry keeps its explicit Apply button.

The amplifier command must still pass fresh identity/layout/complete-snapshot validation and full readback. There is no retry of an ambiguous write, no saving and no broadened model/range support. The separate confirmed-value summary remains distinct from the live pending label until readback succeeds. Pointer cancellation during an external-state conflict preserves pending input for inspection.

## Focused validation

tools/Iamp.Desktop.UiChecks measures all 18 sliders at 1366×768, 911×512, 683×384, 620×320 and 1920×1080. Checks verify half-width sizing, 28px white round thumbs with 8px coloured centres, exact icon/fill colour agreement, endpoint coordinate mapping and live value/unit bindings. Actual routed keyboard events verify one commit per release, no duplicate release commit and no replay after focus loss. Programmatic synchronization, busy and detached controls do not commit. Existing affected slider/pending/filter/cache/header checks also pass. No desktop screenshot was taken and no full protocol suite was rerun, since wire encoding did not change.

Hardware acceptance records a fresh baseline, changes each slider value by one within its supported range through CommitSliderValueAsync (the shared release callback), and immediately restores it through the same callback. Every pair must match the full original preset. All 18 callback paths pass; a separate fresh BLE session verifies complete original state/detail equality. Private report: analysis/private/mobile-slider-windows-report.json, passed=true, baselineRestored=true, 18 restored controls, failure=null. This verifies the shared commit path; it does not claim physical mouse injection or screenshot comparison. No recalls or saves.

Phase 6 remains excluded. Existing effect caching/reset behavior remains available.

## Heading simplification — 1.24.1

The separate large-value summary described above has been removed. Every setting label above a slider is now bold, consistently across all effects. Readback validation and pending/confirmed status remain; read-only values stay visible as individual bold-labelled rows. This presentation-only update passes existing offline all-18/five-size WPF checks.
