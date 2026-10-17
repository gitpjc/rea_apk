# Reverb model editing — 1.38.0

## Scope at 17 October 2026 — current application 1.41.2

The model settings/order/ranges and dated device results below remain the category specification. Application 1.41.2 completes all eight requested categories (124 models total), including Mod, and general save/rename/Save As. Any “next category”, narrower save guard or other-models-pending wording records this milestone’s chronology, not current backlog. All values/groups must be checked across the entire scrolled mobile page. Broader fault/combination and deployment acceptance remains open; GIR&IR is excluded and Phase 6 deferred.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


All nine built-in Reverb models are verified on firmware V1.1.9. Mod follows Reverb in the requested order. GIR&IR and Phase 6 remain excluded.

## Observed controls

| Model | Settings in mobile and desktop order |
| --- | --- |
| Room | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Hall Reverb | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Plate | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Spring | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Mod | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Shimmer | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Fl-Reverb | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100; Rate: 0–100; Feedback: 0–100; Mod Delay: 0–100; Mod Level: 0–100 |
| Reverse Reverb | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100 |
| Dist Reverb | Pre Delay: 0–200 ms; Level: 0–100; Decay: 0–100; Tone: 0–100; Gain: 0–100; Dist Level: 0–100 |

Each model has ten words: enabled, model and eight parameter slots. Unused slots remain zero. Controls, names, order, ranges and units come from each model's actual phone UI and successful commands. Pre Delay has an explicit ms unit; no unit is invented for Decay or other plain-number settings. The existing Hall Reverb title is retained.

An earlier browser capture recorded Shimmer greyed out without establishing the reason. Fresh phone selection, returned model 5, endpoint/power captures and actual Windows tests now establish its availability in the tested chain on V1.1.9. Static catalog names alone did not establish support.

Retain 102 independent full-block endpoint/power frames, top and scrolled-bottom screenshots, visible endpoint values/units, defaults and restoration proof hashes in tests/fixtures/f15-reverb-model-observations.json. Windows OCR reads a visually confirmed zero glyph as Oms in some 0ms captures; the validator accepts only that exact ambiguity at a matching setting row and independently captured numeric-zero frame. Other endpoint text and units match exactly.

## Implementation and restoration

ReverbParameters groups only independently verified identical layouts. Stable profiles remain separate from physical chain position. Parameter writes reject foreign profiles, disabled effects, invalid values and nonzero unused fields. Power writes preserve every other word. Duplicate instances retain separate pending inputs across selection and unchanged refresh. JSON follows the same ordered settings as the phone and desktop, with separate units.

Every ADB interaction and screenshot is guarded to foreground iAMP. Inspect the top and scroll to the bottom for every model; locate each slider safely before testing it. Mandatory cleanup attempts Windows restoration even if stopping phone iAMP fails. It refuses unrelated/global differences, restores only the owned Reverb to original Hall at position 5 with every original value and enabled flag, then independently reconnects and compares all original state/detail bytes. Nothing is saved or recalled.

## Verification

295 affected protocol cases pass: independent frames, every observed integer at all twenty positions, source/layout/unused/profile/disabled guards, duplicate targeting, earlier Hall observations, bypass and unchanged save restrictions. Focused WPF checks cover every model and control, mobile order and category color, input lengths, pending isolation/refresh, disabled/re-enable, invalid/busy/firmware/preview states and five viewport sizes. Ordered JSON, clipboard and native Save As checks also pass. No full-suite rerun was required.

Actual Windows handlers test minimum, maximum and default for every setting, off/on, move 5→4, edit and restore Level at the new position, then move back. Level is muted while testing other settings, except during its own boundary checks. Every model finishes with complete independent original restoration, followed by another final independent readback. The hidden WPF tests take no computer screenshots.
