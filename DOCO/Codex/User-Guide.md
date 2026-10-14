# Running the current iAMP Desktop build

Updated 14 October 2026. This is an experimental companion for the MOOER F15i Li. The complete editor is still in development.

Current published version: **1.5.2**. The connection code keeps each complete read/edit/save workflow together; concurrent read-only hardware checks preserved all original amplifier values. The window adapts to smaller work areas, with wrapping controls/cards and a scrollable sidebar. **Ctrl+1** opens Amplifier library, **Ctrl+2** opens Version History and **Ctrl+F** focuses preset search. Tab/Shift+Tab move between controls; keyboard-focused buttons have a blue border. These shortcuts do not operate the amplifier. See [Desktop-Usability.md](Desktop-Usability.md).

**Compare snapshot** and **Export diagnostics** remain available. Supported editing ranges are unchanged. Developers can run `bash scripts/build-desktop.sh` without a connected phone or amplifier; see [Build-and-Release.md](Build-and-Release.md).

To compare values, first read the amplifier or open a snapshot, choose **Compare snapshot**, and select an exported `.iamp-snapshot.json`. The read-only dialog shows **FILE** and **DISPLAYED** values. Your displayed snapshot and pending inputs stay unchanged; nothing is sent to the amplifier. Matching snapshots do not prove current live or saved state. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

If a connection problem occurs, choose **Export diagnostics** before closing the desktop app. It retains the latest 256 events in memory and excludes Bluetooth addresses, preset contents and raw packets. Nothing is uploaded automatically. The log is cleared when the app closes; a mobile-only problem while the desktop is closed cannot appear in this log. See [Diagnostics.md](Diagnostics.md).

## Start and read the amplifier

1. Open `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**, select the amplifier, then **Read amplifier**. Alternatively, expand **Manual Bluetooth address** and enter its address.
4. Browse/search the 80 preset names and select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful read produces a timestamped snapshot and releases Bluetooth. The displayed **Disconnected** status after an operation is intentional: each read or edit reconnects when needed.

## Supported controls

The sidebar displays the current desktop application version beneath **DESKTOP COMPANION**. Choose **Version History** to see dated release summaries, then select an entry to open its full details. Choose **Amplifier library** to return to your existing snapshot and pending inputs. History is loaded from the accompanying `assets/version_history.json` file; keep it with the published application.

The current build at `artifacts/Iamp.Desktop` supports the five intervals below in combination through explicit desktop Apply controls. API and actual UI-handler hardware tests passed individual edits and all five values changed together, followed by complete original-state restoration. Published executable and Desktop/Transport/Protocol DLL hashes match the tested staging build.

| Feature | Current support |
| --- | --- |
| Information | Firmware, capacity counts, preset names and current-preset details |
| Noise Killer Threshold | Explicit Apply, values 25–28, matching first-position configuration |
| Noise Killer Level | Explicit Apply, values 47–50; API/UI hardware verified |
| 808 Gain | Explicit Apply, values 36–39; API/UI hardware verified |
| 808 Tone | Explicit Apply, values 97–100; API/UI hardware verified, including supported combinations |
| 808 Volume | Explicit Apply, values 97–100; API/UI hardware verified |
| Preset selection | Explicit selection between captured index 4 / British Lead Solo and index 5 / US 65 Deluxe Clean configurations |
| Other effect parameters | Corroborated fields are displayed read-only; unsupported fields are omitted |
| Local snapshots | Export/open `.iamp-snapshot.json` files for offline inspection |
| Save current preset | Same-slot save at index 4 / British Lead Solo, Threshold 25 or 26 only, all other original values unchanged |
| Arbitrary recall, other-slot save, rename, mixer, chain editing, applying imports | Still in development |

Hardware writes are currently validated only on firmware **V1.1.9** and the captured first-position Noise Killer / second-position 808 models. The five intervals can vary together in the shared API; other fields must still match the supported layout. These are nearby experimental integer intervals, not documented full device ranges or physical units. Configuration checks disable unfamiliar layouts. Preset recall additionally requires the exact captured original slot-4/5 state; restoring only some changed values does not satisfy that guard.

## Apply a supported parameter

Select **Dyna / Noise Killer** or **OD / 808**, enter a supported whole-number value, then click its Apply button. Typing changes a pending value only. The app reconnects, checks the actual preset against its snapshot, writes the captured command and independently reads the result. Values on screen update only when that result is confirmed.

Apply controls make **live, unsaved edits**. Returning all changed values to their original snapshot values clears the snapshot-difference indicator. Preset selection stays disabled while that difference remains. Matching a read snapshot alone does not prove that the preset is saved.

## Save the supported preset

Version **1.2.0** adds **Save current preset** in the current-preset panel. It enables only for a live edit to index **4 / British Lead Solo** on firmware **V1.1.9**, with Noise Killer Threshold **25 or 26** and every other original value unchanged. Apply the supported Threshold change first; then choose Save and review the unchanged name, destination **0D / index 4** and Threshold in the overwrite confirmation. **No** is the default choice.

On confirmation, the app checks a fresh complete snapshot, sends the observed save once, recalls the same slot and verifies the full state/detail. The Windows test saved 26, saved original 25 back and independently verified every original byte after recall. Other saved values, renaming, other destinations and power-cycle persistence are not validated. The existing recall controls remain restricted to their original captured configurations; saving Threshold 26 does not expand those recall guards.

An unconfirmed save may have changed storage. The app disables further writes and does not automatically repeat it. Read the amplifier to inspect its actual state before another action. Local snapshot files cannot enable saving.

If an edit is interrupted or not confirmed, its outcome may be unknown. Click **Read amplifier** before another edit and inspect the actual values; the app does not automatically replay an ambiguous write.

If reading fails with service discovery **Unreachable** while Android iAMP is connected, disconnect/close iAMP and retry **Read amplifier**. That contention case was reproduced and the subsequent Windows read succeeded; broader automatic recovery is still being tested. After PC restart, scanning may be needed before a manually entered address can be opened. A bounded exact-address scan fallback handles null address lookup before commands; it does not retry ambiguous writes or verify identity from an advertisement alone.

## Save and open local snapshots

After reading the amplifier, choose **Export snapshot** to preserve the displayed preset and its raw state in a `.iamp-snapshot.json` file on this PC. Choose **Open snapshot** to inspect an exported file without connecting to the amplifier. Exporting records the last confirmed snapshot; pending typed values are not included.

An opened file is marked **LOCAL SNAPSHOT / OFFLINE** and all amplifier editing controls are disabled until a fresh **Read amplifier** succeeds. Only that snapshot's preset is listed; capacity counts are unknown. Files are versioned and integrity-checked, with a 64 KiB limit. Invalid files leave the previous displayed snapshot intact. These files cannot currently be applied to the amplifier and are separate from MOOER Studio backups.

## Backups and testing

One mobile compatibility incident is under investigation: iAMP repeatedly showed its PC/USB transfer screen after Windows testing. In that incident, closing iAMP and restarting the amplifier with its USB unplugged restored normal phone operation; original preset data matched exactly afterward. The cause is unresolved. See [Mobile-Transfer-Loop.md](Mobile-Transfer-Loop.md). Avoid simultaneous phone/Windows control and report recurrence before further save tests.

Version **1.2.1** stops a Windows connection session if the amp reports the status notifications observed during that loop. It does not automatically resume or replay a write after another notification. A failed save can have an unknown stored outcome; wait for a stable amp and inspect a fresh read. This defensive check does not fix the mobile loop's underlying cause. Read-only reconnect checks passed both with USB attached and unplugged; see [Read-Only-Handoff.md](Read-Only-Handoff.md).

Use the vendor MOOER Studio utility for whole-device backups; the verified development baseline and documented recovery route are in [Backup-Baseline.md](Backup-Baseline.md). The desktop app does not restore vendor backups.

Development tests record the original state, restore every changed parameter and the originally selected preset, then compare complete state/detail payloads. The opt-in hardware test command in [Environment.md](Environment.md) performs temporary changes; normal app startup and preview commands do not run that test.

## Build from source

Use Git Bash from the repository root:

```bash
bash scripts/run-desktop.sh
```

See [Progress.md](Progress.md) for validation results and [Plan.md](Plan.md) for the remaining tasks.
