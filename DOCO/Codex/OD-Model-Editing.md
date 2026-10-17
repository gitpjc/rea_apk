# OD model editing — 1.36.0

## Scope at 17 October 2026 — current application 1.41.2

The model settings/order/ranges and dated device results below remain the category specification. Application 1.41.2 completes all eight requested categories (124 models total), including Mod, and general save/rename/Save As. Any “next category”, narrower save guard or other-models-pending wording records this milestone’s chronology, not current backlog. All values/groups must be checked across the entire scrolled mobile page. Broader fault/combination and deployment acceptance remains open; GIR&IR is excluded and Phase 6 deferred.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


All 22 built-in OD models are verified on firmware V1.1.9: **Pure Boost, Flex Boost, Tube DR, OD808, Gold Clon, D-Drive, Jimmy OD, Full DR, Beebee Pre, Beebee+, Black Rat, Grey Faze, Muffy, Full DS, Shred, Riet, MTL Zone, MTL Master, Obsessive Dist, ODR 1, BE OD, Solo**. The category follows completed Dyna, Filter, preset EQ and all 55 Amp models. Delay is next; GIR&IR and Phase 6 remain excluded.

## Observed controls

Every model has five words: enabled, model, Gain, Tone and Vol. The three settings use integers 0–100 in that mobile order. Every model is inspected at the top and after scrolling; no further settings are hidden below Vol. Retain 176 independent full-block endpoint/on-off frames, screenshots/log hashes, defaults and per-model restoration proofs in `tests/fixtures/f15-od-model-observations.json`. Static catalogue names alone did not establish these layouts.

`OdParameters.Models` is built from completed evidence. Shared profile 2 remains independent of physical chain position. Gain and Vol use the existing shared editors; Tone uses the existing standalone editor consistently for every verified model. Status/accessibility text says OD rather than incorrectly identifying every algorithm as OD808. Same/different models retain independent pending inputs, unchanged refresh preserves them, and disabled effects can be enabled before editing. JSON schema 3.0.0 remains unchanged with ordered Gain/Tone/Vol values and separate null units/groups.

## Capture and restoration

Guard every ADB interaction/screenshot to foreground iAMP. Select a stable model row and verify its name, returned model and three-control layout before power or parameter tests. Where OCR omits a row number, exact model text plus adjacent ordinal geometry must match in two stable captures. Full-block model/count/range checks remain mandatory. A single replacement-token reselection is allowed only when returned values still match the untouched original. Unknown layouts stop the batch.

After each model, mandatory cleanup stops phone iAMP and independently reads Windows state. It refuses unrelated/global differences, restores only the owned OD block at position 2 to original 808 plus all original values and enabled state, then reconnects independently and compares every original preset/global byte. ADB cleanup failure cannot skip the Windows restoration attempt. Nothing is saved or recalled.

## Verification

337 affected protocol tests pass, including independent frames, every integer at all twenty positions, malformed/unknown/disabled guards, duplicate targeting, original 808 ranges, bypass and unchanged save guards. Focused 22-model WPF/JSON checks pass for all three controls, mobile order/color, pending isolation/refresh, invalid/busy/firmware/preview/disabled states and copy/save output. No full-suite rerun was required.

Actual Windows handlers pass 0/100/default for every setting, off/on, move 2→3, edit/restore Gain there and move back, for all 22 models. Each finishes with complete independent original restoration, followed by another final independent readback. Windows cleanup always attempts restoration even if releasing the previous connection throws. No desktop screenshots are taken.
