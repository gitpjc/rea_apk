# Delay model editing — 1.37.0

## Scope at 17 October 2026 — current application 1.41.2

The model settings/order/ranges and dated device results below remain the category specification. Application 1.41.2 completes all eight requested categories (124 models total), including Mod, and general save/rename/Save As. Any “next category”, narrower save guard or other-models-pending wording records this milestone’s chronology, not current backlog. All values/groups must be checked across the entire scrolled mobile page. Broader fault/combination and deployment acceptance remains open; GIR&IR is excluded and Phase 6 deferred.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


All twelve built-in Delay models are verified on firmware V1.1.9. Reverb follows Delay in the requested implementation order. GIR&IR and Phase 6 remain excluded.

## Observed controls

| Model | Settings in mobile and desktop order |
| --- | --- |
| Digital | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Analog Delay | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Real Echo | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Tape | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Mod | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Reverse | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| PingPong | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms |
| Crystal | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms; Depth: 0–100; Rate: 0–100 |
| Rainbow | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms; Depth: 0–100; Rate: 0–100 |
| Vintage Delay | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms; Bit: 0–100; S-Rate: 0–100 |
| Galaxy Delay | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms; Mod Rate: 0–100; Mod Depth: 0–100 |
| Sweep | Level: 0–100; Feedback: 0–100; Time: 40–2500 ms; Rate: 0–100; Range: 300–4000 |

Every model has eight words: enabled, model and six parameter slots. Unused slots remain zero. The first three settings are Level, Feedback and Time. Additional settings differ by model; their names, order and ranges come from actual per-model phone captures. Sweep Range is displayed without a unit, so none is invented. Time has an explicit ms unit in JSON and the desktop value display. The existing Analog Delay title is retained.

Independent captures include the top and scrolled bottom of each effect, visible endpoint values/units, defaults and 116 successful full-block endpoint/power frames. Their hashes and restoration proofs are retained in tests/fixtures/f15-delay-model-observations.json. Feedback endpoints are captured with Level at zero. Intermediate Sweep Range frames include individual integer values rather than an assumed stepped scale.

## Implementation and restoration

DelayParameters groups only independently verified identical layouts. Profile 4 covers the three-control models; additional model-specific profiles cover Crystal/Rainbow, Vintage Delay, Galaxy Delay and Sweep. Every profile remains independent of physical chain position. Parameter writes reject foreign profiles, disabled effects, invalid values and nonzero unused words. Power writes preserve every other word. Duplicate instances retain separate pending values across selection and unchanged refresh.

Guard every ADB screenshot and interaction to foreground iAMP. A notification shade or lock screen stops capture. Mandatory cleanup attempts complete Windows restoration even if stopping phone iAMP fails. Only the owned Delay module may be changed; unrelated or global differences stop blind restoration. Restore original Analog at position 4, its original enabled state and all original parameters, then reconnect independently and compare all original state/detail bytes. Nothing is saved or recalled.

## Verification

289 affected protocol cases pass, including independent frames, every integer at all twenty positions, malformed/unknown/disabled/foreign-profile guards, duplicate targeting, earlier Analog observations, bypass and unchanged save restrictions. Focused WPF checks cover all twelve models, every control, mobile order and category color, numeric input, pending isolation, disabled/re-enable, invalid/busy/firmware/preview states and five viewport sizes. Ordered JSON, clipboard and native Save As checks also pass. No full-suite rerun was needed.

Actual Windows handlers test each minimum, maximum and default, with Level muted for Feedback and subsequent settings. They verify off/on, move 4→5, edit and restore Level at the new position and move back. Every model finishes with complete independent original restoration, followed by another final independent readback. Tests use hidden WPF controls without taking computer screenshots.
