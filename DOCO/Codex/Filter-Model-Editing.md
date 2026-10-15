# Filter model editing — 1.29.0

The desktop provides the verified settings and on/off controls for all three built-in Filter models on F15i V1.1.9. Settings follow the mobile editor and exported JSON order. Sliders use the Filter icon's purple colour and the existing half-width layout, with numeric Apply available for precise entry. Changes are live and unsaved.

| Model | Settings in mobile/desktop/JSON order | Observed defaults | Observed range |
| --- | --- | --- | --- |
| Touch Wah | Attack, Peak, Sensitivity, Level | 60, 45, 55, 70 | Each 0–100 |
| Auto Wah | Rate, Peak, Range, Level | 20, 45, 50, 70 | Each 0–100 |
| Q-Filter | Rate, Q, Mix | 12, 65, 60 | Each 0–100 |

These are mobile-displayed integers; physical units are not inferred from their names. A fresh baseline recorded Noise Killer enabled with Threshold 25 and Level 50 before the temporary Filter replacements. Returning to Noise Killer loads its default Threshold 15, so restoration explicitly re-applies original 25 rather than assuming a model change recovers previous values.

## Runtime evidence

Authorized ADB navigation selected each Filter model and captured each setting at 0 and 100, plus off/on. Requests use `25 position 05 0C` followed by six little-endian words. Word 0 is enabled state and word 1 is the category-local model identifier: Touch Wah 0, Auto Wah 1 and Q-Filter 2. Visible parameters occupy words starting at 2; Q-Filter's last unused zero word remains opaque and is preserved. Other words and the complete global state must remain unchanged.

Sanitized evidence in `tests/fixtures/f15-filter-model-observations.json` retains 28 exact independent frames with their preceding values and a SHA-256 of the private phone log. Each endpoint pair differs only in the setting being exercised. Device identifiers, raw logs and iAMP-only screenshots remain in ignored `analysis/private`. The mobile test restored the original Noise Killer model and exact starting values; an independent Windows read matched the complete starting state/detail. No saving or preset recall occurred.

## Implementation and verification

Stable model-profile identifiers are distinct from physical chain positions. Desktop writes address the selected instance explicitly and keep duplicate-instance pending values separate. A write requires the captured firmware, a matching fresh complete snapshot, enabled supported layout and demonstrated range. Checked readback accepts only the requested parameter or enabled-state change. Disabled models are grey with parameter editing blocked and Enable effect available. Existing pending-value, cancellation, offline/preview, conflict and save restrictions remain in force.

Focused protocol tests cover the independent captured frames, every integer at all 20 positions, other-word preservation, unsupported layouts/values, model-specific implicit resolution and existing bypass/save guards. Offline WPF checks cover all 11 sliders, ordering, duplicate-instance pending values, refresh preservation, grey/re-enable states, unsupported firmware, no device writes and ordered JSON output. Dyna and existing editor regressions remain covered by the affected checks.

Hardware acceptance passed actual desktop slider 0 → 100 → initial-default round trips for every setting, each model off/on, a moved-position parameter edit, reverse move, original model/value restoration and independent complete state/detail equality after each model and at the end. Each model moved 1 → 3; its first setting was changed by one and restored at position 3 before reversing the move. Noise Killer was restored with Threshold 25 after each model. The completed report and its hash are retained in the sanitized fixture. The 136 affected protocol cases and offline WPF checks pass; all new slider fills match the Filter icon colour. Phase 6 and broader model/save support remain excluded from this increment.
