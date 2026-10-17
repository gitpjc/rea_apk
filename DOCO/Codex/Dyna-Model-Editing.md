# Dyna model editing — 1.28.0

## Scope at 17 October 2026 — current application 1.41.2

The model settings/order/ranges and dated device results below remain the category specification. Application 1.41.2 completes all eight requested categories (124 models total), including Mod, and general save/rename/Save As. Any “next category”, narrower save guard or other-models-pending wording records this milestone’s chronology, not current backlog. All values/groups must be checked across the entire scrolled mobile page. Broader fault/combination and deployment acceptance remains open; GIR&IR is excluded and Phase 6 deferred.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


The desktop now provides parameter editing and confirmed on/off controls for all four built-in Dyna models. This extends the existing Noise Killer implementation with Red Comp, Yellow Comp and Intel Reducer, keeping the mobile parameter order, half-width orange sliders, numeric entry and explicit Apply behavior. Changes are live and unsaved on the verified F15i V1.1.9 profile.

## Observed mobile settings

| Model | Settings in mobile/desktop/JSON order | Observed defaults | Range |
| --- | --- | --- | --- |
| Red Comp | Sensitivity, Level | 20, 70 | Each 0–100 |
| Yellow Comp | Attack, Ratio, Threshold, Level | 30, 15, 45, 75 | Each 0–100 |
| Noise Killer | Threshold, Level | 15, 50 when replacing the model | Each 0–100, previously verified |
| Intel Reducer | Threshold, Depth, Level | 10, 15, 50 | Each 0–100 |

These are the displayed integer values. No physical units are inferred for Attack, Ratio, Threshold, Depth or the other normalized settings. User values are not replaced by defaults except when explicitly selecting another model. The fresh test baseline had Noise Killer enabled at Threshold 25 and Level 50; returning to its model therefore required explicitly restoring Threshold 25.

## Evidence and commands

Authorized ADB navigation selected each model, operated each displayed parameter to both endpoints, and toggled each effect off and on without saving. Captured full requests use `25 position 04 0C` followed by six little-endian words. The model word is 0 for Red Comp, 1 for Yellow Comp, 2 for Noise Killer and 3 for Intel Reducer. Word 0 is enabled state; the visible settings occupy successive words starting at 2. Red Comp has two trailing opaque zero words, Intel Reducer has one, and Yellow Comp uses all four remaining words. Opaque words remain untouched; unfamiliar parameter layouts stay disabled.

`tests/fixtures/f15-dyna-model-observations.json` retains 24 independently captured frames: both endpoints for nine settings and off/on for three models, with their preceding values and a SHA-256 of the private source log. Raw logs, device identifiers and phone screenshots remain under ignored `analysis/private`. The ADB coordinate-based gestures sometimes returned a temporary model to a nearby integer rather than its default; restoration does not rely on those gestures. The original Noise Killer model and exact original values were restored with verified commands and checked against the complete baseline independently.

## Guarding and state

Editor profiles identify the verified type/model/word-count layout independently of physical chain position. Pending values remain keyed by position and value index, so duplicate identical models do not share edits. Different Dyna models can coexist; an implicit legacy target must be unique for that model profile, while desktop writes always address the explicit physical position.

Every write requires the captured firmware, a fresh complete state/detail matching the displayed snapshot, a supported enabled layout and a value within its demonstrated range. Checked readback permits only the requested parameter word or enabled-state change. Slider release and numeric Apply share the same transport path. Disabled effects are grey, their parameter editors are disabled, and Enable effect remains available. Pending values block chain/model/on-off changes. No saving, preset recall or Phase 6 operation is introduced.

## Verification

Focused protocol checks exercise the captured frames, every integer at every chain position, preservation of other words, rejection of invalid/disabled/unknown layouts, model-specific resolution and existing save guards. Offline WPF checks exercise all nine new sliders, mobile ordering, independent duplicate-instance pending values, refresh preservation, disabled/re-enable indicators, unsupported firmware, absence of device writes and ordered JSON output.

Hardware acceptance passed each model's desktop slider endpoint/default round trips, off/on, a moved-position edit and complete original restoration, followed by a final independent read. Each of the nine settings ran 0 → 100 → its initial model default through the actual desktop commit handler. Each model was disabled/re-enabled, moved 1 → 3, edited/restored at position 3 and moved back. Returning to Noise Killer and restoring Threshold 25 recovered the complete original state/detail after every model. The final independent connection also matched. The completed result and evidence hash are recorded in the sanitized fixture and Plan.md. Other effect categories and persistent saving retain their existing limits.
