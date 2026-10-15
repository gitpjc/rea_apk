# Preset EQ editing — 1.30.0

Both built-in preset-chain EQ models are supported on F15i firmware V1.1.9. This work does not access global EQ. GIR&IR is excluded at the user's request.

## Mobile evidence

ADB operations and screenshots were restricted to foreground iAMP. Each model was scrolled from top to bottom before its settings were considered complete. Custom EQ has four collapsible groups: **Low**, **Low Mid**, **High Mid** and **High**. Scrolling below them reveals **Low Cut**, **High Cut** and **Level**; these are not extra bands.

The sanitized independent fixture `tests/fixtures/f15-eq-model-observations.json` retains 46 successful mobile endpoint/power frames. Each includes the immediately preceding same-model frame, differing in exactly the named word. It records the SHA-256 of the private source log; device addresses, phone identifiers and screenshots remain private.

Both models use type 9 with 17 little-endian words. Word 0 is enabled; word 1 identifies the model. Parameter commands replace the complete effect block, retaining every other word. Physical chain position is independent of the stable editor profile.

| Model / group | UI order | Wire indices | Physical values |
| --- | --- | --- | --- |
| 5 Bands EQ | 100Hz, 250Hz, 630Hz, 1.6kHz, 4kHz | 2–6 | −16.0…16.0 dB, 0.5 dB steps |
| 5 Bands EQ | Level | 7 | 0–100, unitless |
| Custom EQ / Low | Band 1 Gain, Freq, Q | 2–4 | Gain as above; 40–600 Hz; Q 0.2–10.0 |
| Custom EQ / Low Mid | Band 2 Gain, Freq, Q | 5–7 | Gain as above; 200–2000 Hz; Q 0.2–10.0 |
| Custom EQ / High Mid | Band 3 Gain, Freq, Q | 8–10 | Gain as above; 600–7000 Hz; Q 0.2–10.0 |
| Custom EQ / High | Band 4 Gain, Freq, Q | 11–13 | Gain as above; 1500–20000 Hz; Q 0.2–10.0 |
| Custom EQ | Low Cut, High Cut, Level | 14–16 | Off or 20–600 Hz; Off or 2000–20000 Hz; unitless 0–100 |

Gain is encoded as `(physical dB × 2) + 32`. Q uses `physical Q × 10 − 2`, with 0.1 steps. Frequency words directly contain Hz. The slider's choice list follows observed decade-dependent spacing: 1 Hz below 100, 10 Hz below 1000, 100 Hz below 10000, then 1000 Hz. Each band uses its own bounds; cuts prepend Off (wire zero). Endpoints, interior/default positions and decade boundaries were corroborated through mobile observations and representative Windows readback. Every enumerated step is checked offline; the evidence does not claim a separate mobile capture for every step.

## Desktop behavior

The EQ icon and slider fill use teal `#20D5D2`. Sliders retain the half-width mobile style. The four Custom EQ sections expand/collapse without sending commands. Numeric inputs accept signed gains, decimal Q, supported Hz steps and case-insensitive Off for cuts. Invalid inputs remain visible but cannot be applied. Dragging does not write until release; keyboard adjustments commit on key release. Numeric Apply remains available.

Pending values remain isolated by physical effect position, including repeated identical EQ models. An unchanged refresh preserves them. Loading a different snapshot resets them. Disabled effects show the confirmed grey icon, retain readable settings and offer Enable effect; parameter writes remain blocked until enabled.

JSON schema **3.0.0** preserves the ordered flat settings array. Each setting includes nullable `group`; the twelve Custom EQ band settings use the four mobile group names. Cuts and Level use null. Values are physical numbers, except Off cuts use the string `"Off"` with null unit. See [Preset-JSON.md](Preset-JSON.md).

## Guards and validation

Writes require V1.1.9, a valid complete live snapshot, a supported enabled layout and no conflicting operation. A fresh complete read must match before writing. Complete readback must differ in only the selected word, with global state unchanged. Fixed EQ's opaque words 8–16 must remain zero. Unknown layouts, unsupported steps and ambiguous implicit model targets are rejected. The explicit position API handles duplicate models. Existing narrowly verified preset-saving limits remain unchanged.

Focused tests cover independent mobile frames, every supported choice at all twenty positions, physical-value round trips, unsupported layouts/steps, bypass and existing save guards. Offline WPF checks cover all 21 controls, grouping, five viewport sizes, duplicate pending values, refresh, signed/decimal/Off conversion, unsupported firmware and absence of hardware access. JSON tests retain the existing copy and native Save As behavior.

Actual desktop testing applies boundary/default values and representative interior/decade steps for every setting, toggles each model off/on and edits after moving it to another chain position. Each model is then replaced by the original Noise Killer and its original Threshold restored. Independent connections verify byte-for-byte equality of the complete initial state/detail after each model and at the end. No presets are saved or recalled during these tests.
