# Running the current iAMP Desktop build

Updated 14 October 2026. This is an experimental companion for the MOOER F15i Li. The complete editor is still in development.

## Start and read the amplifier

1. Open `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**, select the amplifier, then **Read amplifier**. Alternatively, expand **Manual Bluetooth address** and enter its address.
4. Browse/search the 80 preset names and select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful read produces a timestamped snapshot and releases Bluetooth. The displayed **Disconnected** status after an operation is intentional: each read or edit reconnects when needed.

## Supported controls

| Feature | Current support |
| --- | --- |
| Information | Firmware, capacity counts, preset names and current-preset details |
| Noise Killer Threshold | Explicit Apply, values 25–28, matching first-position configuration |
| 808 Tone | Explicit Apply Tone, values 97–100, matching second-position configuration with Gain 39 and Vol 100 |
| Preset selection | Explicit selection between captured index 4 / British Lead Solo and index 5 / US 65 Deluxe Clean configurations |
| Other effect parameters | Corroborated fields are displayed read-only; unsupported fields are omitted |
| Save, arbitrary recall, mixer, chain editing, imports | Still in development |

Hardware writes are currently validated only on firmware **V1.1.9**. Configuration checks deliberately disable controls on unfamiliar layouts or modified presets.

## Apply a supported parameter

Select **Dyna / Noise Killer** or **OD / 808**, enter a supported whole-number value, then click its Apply button. Typing changes a pending value only. The app reconnects, checks the actual preset against its snapshot, writes the captured command and independently reads the result. Values on screen update only when that result is confirmed.

These are **live, unsaved edits**. The application has no preset-save command. Returning all changed values to their original snapshot values clears the snapshot-difference indicator. Preset selection stays disabled while that difference remains, avoiding accidental loss of those edits. Matching the original snapshot does not independently prove that the preset is saved.

If an edit is interrupted or not confirmed, its outcome may be unknown. Click **Read amplifier** before another edit and inspect the actual values; the app does not automatically replay an ambiguous write.

## Backups and testing

Use the vendor MOOER Studio utility for whole-device backups; the verified development baseline and documented recovery route are in [Backup-Baseline.md](Backup-Baseline.md). The desktop app does not restore vendor backups.

Development tests record the original state, restore every changed parameter and the originally selected preset, then compare complete state/detail payloads. The opt-in hardware test command in [Environment.md](Environment.md) performs temporary changes; normal app startup and preview commands do not run that test.

## Build from source

Use Git Bash from the repository root:

```bash
bash scripts/run-desktop.sh
```

See [Progress.md](Progress.md) for validation results and [Plan.md](Plan.md) for the remaining tasks.
