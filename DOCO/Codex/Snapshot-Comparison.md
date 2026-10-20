# Read-only snapshot comparison

Updated for **1.47.0**, 20 October 2026. The primary comparison now uses two independently scrollable columns, each showing its preset code/name, firmware, capture time, effect count and actual effect chain order. Cards show category, model, enabled state and all available mobile-verified named settings. Custom EQ retains its Low, Low Mid, High Mid and High sections; values retain units, signed decimals and Off labels.

The left column identifies the selected file. The right says **Current preset · last read**, or **Displayed snapshot** when viewing a local file. Matching models are paired by category/model and occurrence order for duplicate instances, including reordered chains. Differing comparable values are blue and bold. Moved models identify their other position; unmatched models and unavailable named layouts are explicitly explained. Duplicate models are paired by occurrence rather than an inferred persistent identity.

The former raw table is retained under a collapsed **Technical details** section. The main view shows readable musical settings rather than raw field numbers. Export filenames default to `Preset-0A-British-J800-Clean-snapshot.json`, using the actual preset code/name with whitespace and Windows-invalid characters replaced by hyphens. Legacy `.iamp-snapshot.json` files remain supported. Archive schema, integrity checks and read-only behavior are unchanged.

Validation for 1.47.0: focused offline WPF checks pass for independent chains, duplicate/reordered models, highlighted values, grouped EQ, units/decimals/Off, scrolling, collapsed technical fields, two viewport sizes and safe filenames. All 27 affected snapshot protocol cases pass. The real comparison handler detects fixture differences, rejects malformed input and preserves displayed state, pending input and read-only guards. The application's own redesigned dialog was rendered and visually inspected. No phone/amplifier changes were made. Run `dotnet run --project tools/Iamp.Desktop.UiChecks -- --snapshot-comparison` for focused UI checks.

## Original archive behavior and evidence

The details below describe the original raw comparison and its evidence. In 1.47.0 that table is available within Technical details; the readable view described above is the default.


Updated 14 October 2026; introduced in version 1.4.0.

Read the amplifier or open a local snapshot, then choose **Compare snapshot** beside **Open snapshot**. Select a previously exported `.iamp-snapshot.json` file. The dialog lists differences with the chosen file on the left and the currently displayed snapshot on the right. Close or Escape returns to the existing editor state. Comparison does not refresh the amplifier, apply the file, recall a preset or save anything.

Both inputs pass through the existing version-1 archive validator, including structural, firmware-text and SHA-256 checks. File input uses a bounded 64 KiB buffer, strict UTF-8 and optional UTF-8 BOM handling. Invalid or oversized files leave the displayed snapshot unchanged. Comparison is disabled before a snapshot exists or while another operation is active.

The comparison covers firmware, preset index/name, all 20 effect blocks and every value, complete whole-state data, stored name padding and the unknown preset trailer. It compares raw state/detail payloads byte for byte separately from firmware. Capture timestamps are deliberately ignored when comparing values. A firmware-only change therefore reports matching raw payloads with different firmware; missing or added effect fields are shown as Absent. An altered name can produce both a decoded-name row and a stored-name-byte row.

Unknown state bytes are shown with zero-based payload offsets; effect positions and field numbers are one-based. Effect fields are reported as stored integer values, with no assumed units, parameter names or interchangeability between different models. A display row is a difference, not necessarily an independent audible parameter. The table is read-only, scrollable and supports normal selection/copying; it has no Apply action.

Matching snapshots prove equality of those captured payloads only. They do not establish the amplifier's current live state, saved-preset durability, vendor-backup compatibility or validity of an imported file for hardware writes. Imported snapshots continue to disable all hardware edits until a fresh amplifier read.

Validation: seven new protocol cases cover identical values with different timestamps, every opaque state byte and comparison direction, unknown effect/type/trailer values, stored name padding, differing parameter counts, firmware-only differences and malformed/tampered inputs. The actual offline UI handler detected synthetic state/parameter differences, rejected a malformed file, preserved the displayed snapshot and pending input, and kept all writes disabled. The comparison dialog was visually inspected at 860×600 after fixing compressed columns. All 116 .NET/five Node tests pass; build/publication have zero warnings/errors. Existing snapshot, version-history and diagnostics regressions pass.

Offline QA:

```bash
artifacts/Iamp.Desktop/Iamp.Desktop.exe --comparison-preview tests/fixtures/f15-snapshot-v1.example.json analysis/private/comparison-preview.png
```

The preview uses a local fixture and temporary synthetic comparison file only; the temporary file is removed afterward. Release evidence: `analysis/private/version-140-comparison.png`. No phone/amplifier commands or parameter changes were performed for this feature.
