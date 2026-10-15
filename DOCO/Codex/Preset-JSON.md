# Current preset JSON — 1.30.0

Choose **Show preset JSON** beside Save current preset. A modal dialog displays pretty-printed JSON in a scrollable ListView. **Copy JSON** has a copy icon and copies the entire document; **Save JSON…** uses Windows' standard Save As dialog, including an overwrite confirmation. The default filename includes the full preset bank/letter label and name, for example `0D British Lead Solo.json` or `19D My Tone.json`. Invalid filename characters are replaced with underscores.

Opening the dialog captures the displayed preset snapshot. The view, copied text and saved UTF-8 file all use the same frozen JSON. Unapplied typed values are excluded and identified in the source metadata and dialog notice. Background snapshot reads do not change an already-open JSON document. Viewing, copying and saving do not send amplifier commands.

## Consistent schema

The schema version is `3.0.0`. This version adds a nullable `group` to every setting and supports the string `"Off"` for disabled EQ cuts alongside numeric physical values. Every preset uses the same top-level structure:

| Key | Contents |
| --- | --- |
| `schemaVersion` | Semantic version of the JSON format |
| `preset` | `slot`, `wireIndex`, `name` |
| `source` | `kind`, `firmware`, `readAt`, `confirmedSnapshot`, `hasUnappliedInputs` |
| `effects` | Ordered array of nonempty effect modules, preserving duplicate categories/models and disabled effects |

Each effect contains `position`, `category`, `name`, `enabled` and `settings`. Unknown enabled states use JSON null. Verified settings contain only `name`, `value`, `unit` and `group`. Values are JSON numbers in physical units, except an EQ cut at its Off position exports `"value": "Off", "unit": null`. Other units are null, `ms`, `dB` or `Hz`; Q values have no unit. Units appear only in `unit`: for example, `"name": "Pre Delay", "unit": "ms"`. Groups are null except for the Custom EQ band settings, which use `Low`, `Low Mid`, `High Mid` and `High`. The properties `unmappedValues`, `rawValues`, `rawValueIndex`, `typeCode` and `modelId` are excluded throughout. Unknown models retain their category/name and an empty settings array until their setting names have been verified.

`effects` follows the desktop/mobile chain order. `settings` follows the verified presentation order shared with desktop editor construction:

| Model | Settings order |
| --- | --- |
| Noise Killer | Threshold, Level |
| OD808 / D-Drive | Gain, Tone, Vol |
| PLX 100 | Gain, Bass, Mid, Treble, Presence, Master |
| Analog Delay | Level, Feedback, Time |
| Hall Reverb | Pre Delay, Level, Decay, Tone |
| 5 Bands EQ | 100Hz, 250Hz, 630Hz, 1.6kHz, 4kHz, Level |
| Custom EQ | Band 1 Gain/Freq/Q (Low), Band 2 Gain/Freq/Q (Low Mid), Band 3 Gain/Freq/Q (High Mid), Band 4 Gain/Freq/Q (High), Low Cut, High Cut, Level |

EQ `settings` remains one flat array. Each Custom EQ band has three consecutive settings with the same group; Low Cut, High Cut and Level have `"group": null`. Fixed-band gains and all non-EQ settings also have a null group. EQ gains export signed decimal dB, frequencies/cuts export Hz and Q exports its decimal ratio, using the same conversion as the desktop controls. Level remains a unitless number from 0 to 100. The `5 Bands EQ` and `Custom EQ` names identify recognized 17-value EQ blocks; unsupported EQ shapes/models retain an empty settings array.

The OD808 desktop editor previously put Tone first. Version 1.26.0 aligns it with the mobile Gain → Tone → Vol order. Version 1.26.1 removes unit suffixes from exported setting names while preserving the same order and separate units.

## Verification

Focused offline WPF checks cover schema consistency across all 80 slot labels, absence of the five removed properties at every nesting level, disabled/repeated/unknown effects, ordered setting values, units without name suffixes, fixed/custom EQ physical values and grouping, Off and active cuts, unsupported EQ shapes/models, Unicode/quoted names, actual OD808 slider ordering, scrollable list geometry, the copy icon and exact copy payload, clipboard-busy feedback, native Save As configuration and filename defaults, UTF-8 file content, overwrite/temporary-file cleanup and absence of device access. Tests use a clipboard delegate and temporary files; the user's clipboard is unchanged.
