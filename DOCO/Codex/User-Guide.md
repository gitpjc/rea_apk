# Running the current iAMP Desktop build

Updated 14 October 2026. This is an experimental companion for the MOOER F15i Li. The complete editor is still in development.

Current release: **1.24.2**. All 18 currently supported parameters now have verified mobile ranges on their captured V1.1.9 model profiles. Most use **0–100**; Hall Pre Delay uses **0–200 ms** and Analog Delay Time uses **40–2500 ms**.

Use **Amplifier Settings** to connect, choose presets and edit effects. Scan/select loads the library and current preset automatically; a live connection refreshes current state about every three seconds. Disconnect phone iAMP before connecting from Windows. **Amplifier Information**, directly below Settings, shows firmware, capacity cards, battery percentage and read time. Local snapshot imports clear live battery data; navigation preserves the connection and pending inputs.

The compact preset dropdown sits beside Presets, with search on the right and responsive wrapping. Entries show amplifier labels **0A–19D** and actual names. A blue left-side checkmark (**#4385ff**) identifies the active preset inside the open list; the selected-item field has no check. Search shows a right-side × only when it contains text. See [Preset-Browser.md](Preset-Browser.md).

Select an effect icon to inspect it. Category labels are centered, grey icons indicate disabled effects, and details show **Enabled** or **Disabled**. Use **Enable effect** / **Disable effect** or a supported parameter's explicit **Apply** button for a verified live change; nothing is saved automatically. Ctrl+1 opens Settings, Ctrl+2 opens Version History and Ctrl+F focuses search.

**Compare snapshot** and **Export diagnostics** remain available. Supported editing ranges are unchanged. Developers can run `bash scripts/build-desktop.sh` without a connected phone or amplifier; see [Build-and-Release.md](Build-and-Release.md).

To compare values, first read the amplifier or open a snapshot, choose **Compare snapshot**, and select an exported `.iamp-snapshot.json`. The read-only dialog shows **FILE** and **DISPLAYED** values. Your displayed snapshot and pending inputs stay unchanged; nothing is sent to the amplifier. Matching snapshots do not prove current live or saved state. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

If a connection problem occurs, choose **Export diagnostics** before closing the desktop app. It retains the latest 256 events in memory and excludes Bluetooth addresses, preset contents and raw packets. Nothing is uploaded automatically. The log is cleared when the app closes; a mobile-only problem while the desktop is closed cannot appear in this log. See [Diagnostics.md](Diagnostics.md).

## Start and read the amplifier

1. Open `artifacts/Iamp.Desktop/Iamp.Desktop.exe`. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**. If one amplifier is found it connects automatically; otherwise select the amplifier. Alternatively, enter a valid **Manual Bluetooth address** and choose **Connect amplifier**.
4. Browse/search all 80 bank-labelled presets. Choose an entry from the dropdown to recall it; the active preset has a blue checkmark on its left. Escape closes the dropdown without recalling a browsed entry. Select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful connection stays open and shows Connected. Current state refreshes every three seconds; **Refresh amplifier** performs an explicit reread. Choose **Disconnect** before reconnecting phone iAMP. Disconnection retains the displayed snapshot but disables edits. Use one control app at a time. Connect is disabled until a valid target is available.

## Filter presets by effect

After connecting, choose an effect from the **Effect** dropdown beside the preset search. It lists all 124 built-in models using their mobile names, prefixed by category to distinguish similar names. Type a model name while the dropdown is focused to jump to it. Presets match even when that effect is disabled.

The effect choice and name/bank/index search work together. Click **Reset** to select **All effects**; Reset is disabled while All effects is already selected. Reset leaves the text search unchanged, and the search-field × clears only text. The preset count reflects both filters. Filtering sends no recall, edit or save commands.

The first connection builds a local effect cache. Later connections and normal **Refresh amplifier** reuse it, including after restarting the desktop app. The active preset is still read live. Click **Refresh effect names** to update the full preset-effect cache when you want, for example after changing effect models in the phone app. This requires a connected supported amplifier and offers progress/Cancel; pending parameter inputs are preserved. Cached results remain searchable after disconnecting. A local snapshot searches only its one loaded preset. Built-in effect names are already bundled with the app.

## Supported controls

The sidebar displays the current desktop application version beneath **DESKTOP COMPANION**. Choose **Version History** to see dated release summaries, then select an entry to open its full details. Choose **Amplifier Settings** to return to your existing snapshot and pending inputs. History is loaded from the accompanying `assets/version_history.json` file; keep it with the published application.

The current build supports the verified Noise Killer/OD808/PLX 100/Hall Reverb/Analog Delay ranges below through explicit Apply controls. Device writes use a fresh full-snapshot check and complete readback. All tested changes were restored through an independent connection. Other models and ranges remain unavailable.

| Feature | Current support |
| --- | --- |
| Information | Firmware, capacity counts, preset names and current-preset details |
| Noise Killer Threshold | Explicit Apply, values 0–100, matching verified first-position configuration |
| Noise Killer Level | Explicit Apply, values 0–100; may be combined with Threshold 0–100 |
| 808 Gain | Explicit Apply, values 0–100; verified OD808 profile only |
| 808 Tone | Explicit Apply, values 0–100; all three OD808 values may coexist |
| PLX 100 Bass | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| Hall Reverb Pre Delay | Explicit Apply, values 0–200 ms |
| Hall Reverb Level | Explicit Apply, values 0–100 |
| Hall Reverb Decay | Explicit Apply, values 0–100; no time unit inferred |
| Hall Reverb Tone | Explicit Apply, values 0–100; all four supported Hall values may coexist |
| Analog Delay Level | Explicit Apply, values 0–100 |
| Analog Delay Feedback | Explicit Apply, values 0–100 |
| Analog Delay Time | Explicit Apply, values 40–2500 ms; all three supported Delay values may coexist |
| PLX 100 Gain | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| PLX 100 Master | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| PLX 100 Treble | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| PLX 100 Presence | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| PLX 100 Mid | Explicit Apply, values 0–100; all six supported Amp values may coexist |
| 808 Volume | Explicit Apply, values 0–100; verified OD808 profile only |
| Preset selection | Dropdown recall for all 80 presets, 0A–19D, with a left-side blue checkmark on the active preset |
| Effect on/off | Noise Killer, OD808, PLX 100, Analog Delay and Hall Reverb on V1.1.9; grey bypassed icons and Enable/Disable controls |
| Other effect parameters | Corroborated fields are displayed read-only; unsupported fields are omitted |
| Local snapshots | Export/open `.iamp-snapshot.json` files for offline inspection |
| Save current preset | Same-slot save at 0D / British Lead Solo, Threshold 25 or 26 only, all other original values unchanged |
| Other-slot save, rename, mixer, chain editing, applying imports | Still in development |

Hardware writes are validated on **V1.1.9** for the captured Noise Killer, OD808, PLX 100, Analog Delay and Hall Reverb profiles and the table’s supported ranges. All listed limits match the observed mobile sliders for those captured profiles. Physical units are shown only where established. Fresh snapshot/layout checks reject unfamiliar configurations, and all-slot recall does not broaden parameter or saving support.

## Apply a supported parameter

Pending values remain while you browse other effect cards or apply another parameter. Hidden pending values still protect against accidental recall or effect toggling. An unchanged amplifier refresh retains them; loading another preset/import or refreshing changed hardware data starts from that new snapshot.

Each setting has a larger, extra-bold white label above its slider, without a duplicate summary row. The half-width slider shows the value on its right, with the filled track matching the effect icon. Tap or drag anywhere on the track, then release to apply the final value to the amplifier. The value display follows movement immediately; the edit is confirmed only after checked readback. Escape cancels an active pointer drag. Arrow keys adjust a focused slider by one; Page Up/Page Down by ten; Home/End select its limits. Keyboard adjustments apply on key release. Slider and numeric entry stay synchronized. Disabled effects, offline/preview snapshots, busy operations and unsupported configurations cannot be edited.

Numeric entry remains available for precise changes: type a whole number and choose Apply. Typing alone changes a pending value and does not send a command. Both slider release and Apply use the same fresh complete-snapshot check, verified command and full readback. Successful readback confirms the value; pending or unconfirmed edits remain marked.

Apply controls make **live, unsaved edits**. Returning all changed values to their original snapshot values clears the snapshot-difference indicator. Recalling another preset with known live unsaved edits or pending input asks whether to discard them; No is the default. If an external change conflicts with typed input, the app preserves that input, stops automatic refresh and disables editing until you explicitly refresh. Matching a read snapshot alone does not prove that the preset is saved.

## Rearrange, add or remove effects

On F15i V1.1.9, drag an effect icon to the blue insertion line between icons, or use the selected effect’s Move left/Move right buttons. The line shows the exact placement before you drop. Select + to browse the mobile-style category list and choose a model; search the model list if needed, then choose Add effect. Multiple effects from the same category and repeated identical models can be added, as in mobile iAMP. Each instance has its own chain position and pending parameter values. GIR&IR is listed with an explanation that library access is not yet supported. The amplifier supplies the new effect’s default values. Select Remove effect to delete the selected module.

Moves preserve the effect’s values. Previously supported parameter editors continue working at the new position. Newly added models show their names, with parameter editing available only for verified layouts. Apply or clear pending parameter inputs before changing the chain. Every change is checked against a fresh preset and verified by readback. Chain changes are live and unsaved; the restricted save workflow below still applies.

## Save the supported preset

### View, copy or save preset JSON

Choose **Show preset JSON** beside Save current preset. The dialog shows verified current-snapshot effect settings in chain order, with settings ordered like the mobile/desktop editor. Disabled and repeated effects are included. Setting names omit units, which have their own `unit` property; internal IDs and raw/unmapped values are excluded. Scroll vertically or horizontally, use the **Copy JSON** icon/button to copy the complete document, or **Save JSON…** to choose a file location with Windows Save As. The default filename is the preset prefix and name, such as `0D British Lead Solo.json`.

The dialog captures the displayed snapshot when opened. Unapplied input values are excluded, and the dialog identifies them if present. Copy and Save contain exactly the JSON shown. These actions do not change the amplifier. See [Preset-JSON.md](Preset-JSON.md) for the consistent schema.

### Save changes to the amplifier

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
