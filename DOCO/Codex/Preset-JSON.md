# Current preset JSON — 1.26.1

Choose **Show preset JSON** beside Save current preset. A modal dialog displays pretty-printed JSON in a scrollable ListView. **Copy JSON** has a copy icon and copies the entire document; **Save JSON…** uses Windows' standard Save As dialog, including an overwrite confirmation. The default filename includes the full preset bank/letter label and name, for example `0D British Lead Solo.json` or `19D My Tone.json`. Invalid filename characters are replaced with underscores.

Opening the dialog captures the displayed preset snapshot. The view, copied text and saved UTF-8 file all use the same frozen JSON. Unapplied typed values are excluded and identified in the source metadata and dialog notice. Background snapshot reads do not change an already-open JSON document. Viewing, copying and saving do not send amplifier commands.

## Consistent schema

The schema version is `2.0.0`, identifying the requested removal of internal/raw properties from the original schema. Every preset uses the same top-level structure:

| Key | Contents |
| --- | --- |
| `schemaVersion` | Semantic version of the JSON format |
| `preset` | `slot`, `wireIndex`, `name` |
| `source` | `kind`, `firmware`, `readAt`, `confirmedSnapshot`, `hasUnappliedInputs` |
| `effects` | Ordered array of nonempty effect modules, preserving duplicate categories/models and disabled effects |

Each effect contains `position`, `category`, `name`, `enabled` and `settings`. Unknown enabled states use JSON null. Verified settings contain only `name`, numeric `value` and `unit` (null or `ms`). Units appear only in `unit`: for example, `"name": "Pre Delay", "unit": "ms"`. The properties `unmappedValues`, `rawValues`, `rawValueIndex`, `typeCode` and `modelId` are excluded throughout. Unknown models retain their category/name and an empty settings array until their setting names have been verified.

`effects` follows the desktop/mobile chain order. `settings` follows the verified presentation order shared with desktop editor construction:

| Model | Settings order |
| --- | --- |
| Noise Killer | Threshold, Level |
| OD808 / D-Drive | Gain, Tone, Vol |
| PLX 100 | Gain, Bass, Mid, Treble, Presence, Master |
| Analog Delay | Level, Feedback, Time |
| Hall Reverb | Pre Delay, Level, Decay, Tone |

The OD808 desktop editor previously put Tone first. Version 1.26.0 aligns it with the mobile Gain → Tone → Vol order. Version 1.26.1 removes unit suffixes from exported setting names while preserving the same order and separate units.

## Verification

Focused offline WPF checks validate schema consistency across all 80 slot labels, absence of the five removed properties at every nesting level, disabled/repeated/unknown effects, ordered setting values, units without name suffixes, Unicode/quoted names, actual OD808 slider ordering, scrollable list geometry, the copy icon and exact copy payload, clipboard-busy feedback, native Save As configuration and filename defaults, UTF-8 file content, overwrite/temporary-file cleanup and absence of device access. Existing 18-control, pending-input, duplicate-instance and responsive-layout checks also pass. Tests use a clipboard delegate and temporary files; the user's clipboard is unchanged. No full protocol-suite rerun or hardware tests are needed for this display/export feature.
