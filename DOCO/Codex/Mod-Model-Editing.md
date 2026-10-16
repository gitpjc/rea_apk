# Mod model editing — 1.39.0

All 17 built-in Mod models are verified on F15i firmware V1.1.9. GIR&IR and Phase 6 remain excluded.

| Model | Settings in phone, desktop and JSON order |
| --- | --- |
| Phaser | Rate: 0–100; Level: 0–100; Depth: 0–100 |
| Step Phaser | Rate: 0–100; Level: 0–100; Depth: 0–100 |
| Flanger | Rate: 0–100; Mix: 0–100; Feedback: 0–100 |
| Jet Flanger | Rate: 0–100; Mix: 0–100; Feedback: 0–100 |
| Tremolo | Rate: 0–100; Mix: 0–100; Tone: 0–100 |
| Stutter | Rate: 0–100; Mix: 0–100; Tone: 0–100 |
| Vibrato | Rate: 0–100; Mix: 0–100; Tone: 0–100 |
| Rotary | Rate: 0–100; Mix: 0–100; Tone: 0–100 |
| Ring | Rate: 0–100; Mix: 0–100; Tone: 0–100 |
| Poly Shift | Mix: 0–100; Tone: 0–100; Pitch: -12.0–12.0 |
| Ana Chorus | Rate: 0–100; Mix: 0–100; Tone: 0–100; Depth: 0–100 |
| Tri Chorus | Rate: 0–100; Mix: 0–100; Tone: 0–100; Depth: 0–100 |
| Lofi | Sample: 0–100; Mix: 0–100; Bit: 0–100 |
| Slow Gear | Rise: 0–100; Level: 0–100 |
| Panner | Rate: 0–100; Tone: 0–100; Depth: 0–100; Duty: 0–100 |
| Detune | Pitch: -100 Cent–100 Cent; Tone: 0–100; Mix: 0–100 |
| Octave | Sub: 0–100; Sub Tone: 0–100; Upper: 0–100; Upper Tone: 0–100; Dry: 0–100 |

Names, order, word layouts, ranges and units come from actual iAMP views and full-block command captures. Every model is scrolled until consecutive control views stabilize. Each control has distinct minimum/maximum/repeated-minimum images and a fresh interior capture. Changed endpoints require fresh commands; repeated minima and unrelated words must match. Linear physical mappings are corroborated at endpoints, defaults and interior values. Units are included only where the phone displays them. JSON uses the physical numbers and separate units, with no protocol identifiers or raw fields.

ModParameters groups only verified identical layouts and keeps editor profile identifiers separate from chain position. Controls preserve per-instance pending inputs, support signed decimals where observed, and reject unsupported layouts, disabled states, invalid values and unknown reserved fields. On/off remains available for disabled known models.

Verification retains 144 independent endpoint/power frames and full restoration proof hashes in tests/fixtures/f15-mod-model-observations.json. All 331 affected protocol cases pass, covering independent frames, every raw step at all twenty positions, physical formatting/parsing, duplicate targeting and source guards. Focused WPF checks cover all models, mobile-order/color controls, physical inputs and JSON, five viewport sizes, pending-input isolation and invalid/busy/firmware/preview guards. No routine full-suite rerun was required.

Actual Windows handlers test each control's minimum, maximum and default, off/on, moving position 5 to 4, editing the moved instance and restoring order. Full preset detail bytes are compared after parameter, power and move operations. Testing temporarily replaces only original Hall at position 5, then restores its original model, settings and enabled state. Every phone/Windows model finishes with an independent reconnect and complete original state/detail comparison; another independent comparison finishes the batch. No presets are saved or recalled.

Phone captures and interactions require foreground iAMP and reject visible foreign overlays. Capture state is checked before capture and again before saving; a failed second check discards the image. Ten focused guard tests pass. These checks do not take computer screenshots.
