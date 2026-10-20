# Read-only snapshot comparison

## Snapshot workspace — 1.48.0

Choose **Snapshots** in the sidebar. The amplifier header stays visible above each amplifier-related page, showing Bluetooth connection status and the amplifier's last confirmed preset. That header is independent of the snapshot being viewed.

The Snapshots page provides **Capture current preset**, **Open snapshot…**, **Export this snapshot…** and **Compare with file…**. Capture requires an available connection and performs one read of the amplifier's current preset, waiting for any in-flight poll to finish. It excludes unapplied editor inputs. If the read reveals changed amplifier data, the editor is marked for refresh while preserving pending values. A captured snapshot is fixed until explicitly replaced by another capture or file.

Opening a file now changes only this page's viewing state. It does **not** disconnect the amplifier, replace the preset editor, alter the names/filter cache, clear battery information or change the editor's target/pending inputs. Source filename or Bluetooth address, preset code/name and capture time identify what is being viewed. A notice identifies when the observed amplifier preset has changed since an amplifier capture; retained disconnected header data is labelled **Last read preset**. Comparison uses the fixed viewed snapshot, not the editor or a newly polled preset. Export preserves its original bytes and timestamp. Both work offline after a file has been opened or a capture retained.

Version 1.48.1 hides the header on Version History without leaving a gap and restores it on all other screens. Connection and viewed snapshot state are preserved.

The shared header has the amplifier selector followed by **Scan Bluetooth**, **Disconnect**, **Connect amplifier / Refresh amplifier** and a Cancel button shown during operations. Manual address is expandable. Controls wrap on smaller windows, and the header can scroll without consuming all page space. **Amplifier Settings** is now named **Presets**. Snapshots remain inspection/comparison data, not a restore-to-amplifier feature.

Validation for 1.48.0: offline workspace checks cover all seven pages at 1280×900, 911×512 and 620×320; shared control ordering, disabled-state guards, independent file opening, malformed-file preservation, fixed captures and change notices, exact byte/timestamp export, comparison and pending-editor/target preservation. Focused comparison, preset-sync, Mixer, Tuner, Drum, save/rename/cache and report checks pass. Production snapshot/comparison preview paths pass. Only the app's own offscreen render was inspected; no amplifier writes or phone actions were needed. Run `dotnet run --project tools/Iamp.Desktop.UiChecks -- --snapshot-workspace` for these workspace checks.

## Readable comparison — 1.47.0

Introduced in **1.47.0**, 20 October 2026. The primary comparison uses two independently scrollable columns, each showing its preset code/name, firmware, capture time, effect count and actual effect chain order. Cards show category, model, enabled state and all available mobile-verified named settings. Custom EQ retains its Low, Low Mid, High Mid and High sections; values retain units, signed decimals and Off labels.

The left column identifies the **Comparison file**. The right identifies the **Snapshot being viewed** on the Snapshots page. Matching models are paired by category/model and occurrence order for duplicate instances, including reordered chains. Differing comparable values are blue and bold. Moved models identify their other position; unmatched models and unavailable named layouts are explicitly explained. Duplicate models are paired by occurrence rather than an inferred persistent identity.

The former raw table is retained under a collapsed **Technical details** section. The main view shows readable musical settings rather than raw field numbers. Export filenames default to `Snapshot-0A-British-J800-Clean_2026-10-20_1909.json`, using the actual preset code/name with whitespace and Windows-invalid characters replaced by hyphens. Legacy `.iamp-snapshot.json` files remain supported. Archive schema, integrity checks and read-only behavior are unchanged.

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
