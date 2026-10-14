# Running the current iAMP Desktop build

Updated 14 October 2026. This is an experimental companion for the MOOER F15i Li. The complete editor is still in development.

Current release: **1.8.0**. Use **Amplifier Settings** to connect, select presets and edit effects. **Amplifier Information**, directly below it, shows firmware and the three capacity cards from the latest read. Navigation preserves pending inputs and the connection. Disabled effects show the simple status label **Disabled**. Effect category names are centered beneath their icons. Supported effects now show mobile category icons, with grey icons when bypassed; redundant On/Off text is omitted from effect buttons. Select an effect and choose **Enable effect** or **Disable effect** to make a verified live change; nothing is saved automatically. The compact preset dropdown sits beside the Presets heading/count, with search on the right; controls wrap at smaller widths. The four information cards appear on the separate Amplifier Information screen. Choose presets from the compact dropdown; a blue checkmark (#4385ff) on the left identifies the active amplifier preset inside the open list only. The selected-item field shows its bank label and name without a check. The search field shows a right-side × only when it contains text; click it to clear the filter. Presets use amplifier labels **0A–19D** and their actual names. Scan/select loads the library and current preset automatically; the live connection refreshes current state every three seconds. **Ctrl+1** opens Amplifier Settings, **Ctrl+2** opens Version History and **Ctrl+F** focuses search. See [Preset-Browser.md](Preset-Browser.md).

**Compare snapshot** and **Export diagnostics** remain available. Supported editing ranges are unchanged. Developers can run `bash scripts/build-desktop.sh` without a connected phone or amplifier; see [Build-and-Release.md](Build-and-Release.md).

To compare values, first read the amplifier or open a snapshot, choose **Compare snapshot**, and select an exported `.iamp-snapshot.json`. The read-only dialog shows **FILE** and **DISPLAYED** values. Your displayed snapshot and pending inputs stay unchanged; nothing is sent to the amplifier. Matching snapshots do not prove current live or saved state. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

If a connection problem occurs, choose **Export diagnostics** before closing the desktop app. It retains the latest 256 events in memory and excludes Bluetooth addresses, preset contents and raw packets. Nothing is uploaded automatically. The log is cleared when the app closes; a mobile-only problem while the desktop is closed cannot appear in this log. See [Diagnostics.md](Diagnostics.md).

## Start and read the amplifier

1. Open `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**. If one amplifier is found it connects automatically; otherwise select the amplifier. Alternatively, enter a valid **Manual Bluetooth address** and choose **Connect amplifier**.
4. Browse/search all 80 bank-labelled presets. Choose an entry from the dropdown to recall it; the active preset has a blue checkmark on its left. Escape closes the dropdown without recalling a browsed entry. Select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful connection stays open and shows Connected. Current state refreshes every three seconds; **Refresh amplifier** performs an explicit reread. Choose **Disconnect** before reconnecting phone iAMP. Disconnection retains the displayed snapshot but disables edits. Use one control app at a time. Connect is disabled until a valid target is available.

## Supported controls

The sidebar displays the current desktop application version beneath **DESKTOP COMPANION**. Choose **Version History** to see dated release summaries, then select an entry to open its full details. Choose **Amplifier Settings** to return to your existing snapshot and pending inputs. History is loaded from the accompanying `assets/version_history.json` file; keep it with the published application.

The current build at `artifacts/Iamp.Desktop` supports the five intervals below in combination through explicit desktop Apply controls. API and actual UI-handler hardware tests passed individual edits and all five values changed together, followed by complete original-state restoration. Published executable and Desktop/Transport/Protocol DLL hashes match the tested staging build.

| Feature | Current support |
| --- | --- |
| Information | Firmware, capacity counts, preset names and current-preset details |
| Noise Killer Threshold | Explicit Apply, values 25–28, matching first-position configuration |
| Noise Killer Level | Explicit Apply, values 47–50; API/UI hardware verified |
| 808 Gain | Explicit Apply, values 36–39; API/UI hardware verified |
| 808 Tone | Explicit Apply, values 97–100; API/UI hardware verified, including supported combinations |
| 808 Volume | Explicit Apply, values 97–100; API/UI hardware verified |
| Preset selection | Dropdown recall for all 80 presets, 0A–19D, with a left-side blue checkmark on the active preset |
| Effect on/off | Noise Killer, OD808, PLX 100, Analog Delay and Hall Reverb on V1.1.9; grey bypassed icons and Enable/Disable controls |
| Other effect parameters | Corroborated fields are displayed read-only; unsupported fields are omitted |
| Local snapshots | Export/open `.iamp-snapshot.json` files for offline inspection |
| Save current preset | Same-slot save at 0D / British Lead Solo, Threshold 25 or 26 only, all other original values unchanged |
| Other-slot save, rename, mixer, chain editing, applying imports | Still in development |

Hardware writes are currently validated only on firmware **V1.1.9** and the captured first-position Noise Killer / second-position 808 models. The five intervals can vary together in the shared API; other fields must still match the supported layout. These are nearby experimental integer intervals, not documented full device ranges or physical units. Configuration checks disable unfamiliar layouts. All-slot recall uses fresh complete snapshot checks and post-recall global-state/detail verification. It does not broaden the supported parameter or saving ranges.

## Apply a supported parameter

Select **Dyna / Noise Killer** or **OD / 808**, enter a supported whole-number value, then click its Apply button. Typing changes a pending value only. The app uses its live connection, checks the actual preset against its snapshot, writes the captured command and independently reads the result. Values on screen update only when that result is confirmed.

Apply controls make **live, unsaved edits**. Returning all changed values to their original snapshot values clears the snapshot-difference indicator. Recalling another preset with known live unsaved edits or pending input asks whether to discard them; No is the default. If an external change conflicts with typed input, the app preserves that input, stops automatic refresh and disables editing until you explicitly refresh. Matching a read snapshot alone does not prove that the preset is saved.

## Save the supported preset

The supported save workflow provides **Save current preset** in the current-preset panel. It enables only for a live edit to **0D / British Lead Solo** on firmware **V1.1.9**, with Noise Killer Threshold **25 or 26** and every other original value unchanged. Apply the supported Threshold change first; then choose Save and review the unchanged name, destination **0D / index 4** and Threshold in the overwrite confirmation. **No** is the default choice.

On confirmation, the app checks a fresh complete snapshot, sends the observed save once, recalls the same slot and verifies the full state/detail. The Windows test saved 26, saved original 25 back and independently verified every original byte after recall. Other saved values, renaming, other destinations and power-cycle persistence are not validated. All-slot recall is available with explicit handling of unsaved edits; saving remains restricted to this supported 0D profile.

An unconfirmed save may have changed storage. The app disables further writes and does not automatically repeat it. Reconnect or refresh the amplifier to inspect its actual state before another action. Local snapshot files cannot enable saving.

If an edit is interrupted or not confirmed, its outcome may be unknown. Click **Connect amplifier** (or **Refresh amplifier** when connected) before another edit and inspect the actual values; the app does not automatically replay an ambiguous write.

If reading fails with service discovery **Unreachable** while Android iAMP is connected, disconnect/close iAMP and retry **Connect amplifier** (or **Refresh amplifier** when connected). That contention case was reproduced and the subsequent Windows read succeeded; broader automatic recovery is still being tested. After PC restart, scanning may be needed before a manually entered address can be opened. A bounded exact-address scan fallback handles null address lookup before commands; it does not retry ambiguous writes or verify identity from an advertisement alone.

## Save and open local snapshots

After reading the amplifier, choose **Export snapshot** to preserve the displayed preset and its raw state in a `.iamp-snapshot.json` file on this PC. Choose **Open snapshot** to inspect an exported file offline; opening it releases the live connection. Exporting records the last confirmed snapshot; pending typed values are not included.

An opened file is marked **LOCAL SNAPSHOT / OFFLINE** and all amplifier editing controls are disabled until a fresh **Connect amplifier** (or **Refresh amplifier** when connected) succeeds. Only that snapshot's preset is listed; capacity counts are unknown. Files are versioned and integrity-checked, with a 64 KiB limit. Invalid files leave the previous displayed snapshot intact. These files cannot currently be applied to the amplifier and are separate from MOOER Studio backups.

## Backups and testing

One mobile compatibility incident is under investigation: iAMP repeatedly showed its PC/USB transfer screen after Windows testing. In that incident, closing iAMP and restarting the amplifier with its USB unplugged restored normal phone operation; original preset data matched exactly afterward. The cause is unresolved. See [Mobile-Transfer-Loop.md](Mobile-Transfer-Loop.md). Avoid simultaneous phone/Windows control and report recurrence before further save tests.

Version **1.2.1** stops a Windows connection session if the amp reports the status notifications observed during that loop. It does not automatically resume or replay a write after another notification. A failed save can have an unknown stored outcome; wait for a stable amp and inspect a fresh read. This defensive check does not fix the mobile loop's underlying cause. Read-only reconnect checks passed both with USB attached and unplugged; see [Read-Only-Handoff.md](Read-Only-Handoff.md).

Use the vendor MOOER Studio utility for whole-device backups; the verified development baseline and documented recovery route are in [Backup-Baseline.md](Backup-Baseline.md). The desktop app does not restore vendor backups.

Development tests record the original state, restore every changed parameter and the originally selected preset, then compare complete state/detail payloads. The opt-in hardware test command in [Environment.md](Environment.md) performs temporary changes; normal app startup and preview commands do not run that test.

All 80 slots were recalled in the 1.6.0 Windows check, with original 0D restored and all original bytes verified after each test. Phone iAMP was stable when the user reconnected afterward. This does not resolve the earlier loop cause. Phase 6 advanced functions remain excluded unless separately requested.

## Build from source

Use Git Bash from the repository root:

```bash
bash scripts/run-desktop.sh
```

See [Progress.md](Progress.md) for validation results and [Plan.md](Plan.md) for the remaining tasks.
