# Parameter sliders — 1.21.0

All 18 verified Noise Killer, OD808, PLX 100, Analog Delay and Hall Reverb parameters now have integer sliders. The range is the already verified model-specific range; Time supports 40–2500 ms and Hall Pre Delay 0–200 ms. Numeric entry remains available.

Slider movement updates pending input only. Existing explicit Apply handlers perform fresh snapshot validation, write and complete readback. Sliders never create a Bluetooth session or update confirmed state. Numeric input updates the slider only when valid; blank, malformed and out-of-range entries remain visible for correction and disable Apply. Accessibility names state parameter/range. Arrows step by one; Page Up/Page Down by ten; Home/End select limits. Sliders inherit input availability for busy, invalid, unsupported, preview and bypassed states.

Run the focused offline check from the repository root with:

```bash
source scripts/env.sh
dotnet run --project tools/Iamp.Desktop.UiChecks -c Release
```

This check instantiates actual WPF controls with a synthetic offline snapshot, checks all integer targets for all 18 controls, numeric/invalid/fractional input handling, pending/no-write guards, busy/firmware/invalid/disabled/preview states and geometry at 1366×768, 911×512, 683×384, 620×320 and 1920×1080. It neither shows/captures a computer window nor accesses phone/amplifier hardware. No full protocol-suite rerun was needed for this presentation change.

Authorized hardware acceptance records a fresh original baseline and uses the actual Windows slider ValueChanged and Apply handlers for every supported parameter. Each value changes by one within its supported range and immediately returns to its original value, with complete baseline verification after each pair. A separate fresh connection verifies complete original state/detail equality at the end. Private report: analysis/private/slider-windows-report.json, passed=true, baselineRestored=true, 18 restored controls, failure=null. No preset recalls or saves.

Other models, broader persistence and Phase 6 remain outside this change. USB stay-awake remains enabled during ongoing development/testing.

## Mobile interaction follow-up — 1.24.0

The original explicit-Apply-only slider behavior above is superseded by `Mobile-Style-Sliders.md`: half-width rounded coloured adjusters now commit the final tap/drag value on release, or keyboard adjustment on key release. Typed values still require Apply. All 18 actual commit/restore paths and focused half-width/colour/live-value/keyboard/cancellation checks pass.
