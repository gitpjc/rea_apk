# Running the current iAMP Desktop build

Updated 17 October 2026. The core F15i editor is implemented; broader connection, usability and deployment acceptance remains open. See [Current-Status.md](Current-Status.md) for scope and [Musician-Workflow.md](Musician-Workflow.md) for the completed clean/rhythm/lead checks.

Current release: **1.41.2**. There are 126 parameter control types across 124 verified V1.1.9 models, including all Dyna, Filter, preset EQ, Amp, OD, Delay, Reverb and Mod models. Most use **0–100**; Mod also includes signed decimal Pitch and Detune cents. EQ supports signed dB, decimal Q, stepped Hz and Off cut values. Reverb Pre Delay uses **0–200 ms** and all Delay Time controls use **40–2500 ms**; Sweep Range uses **300–4000**. Choose **Change model…** to replace the selected effect in its current chain position using amplifier defaults.

Use **Amplifier Settings** to connect, choose presets and edit effects. Scan/select loads the library and current preset automatically; a live connection refreshes current state about every three seconds. Disconnect phone iAMP before connecting from Windows. **Amplifier Information**, directly below Settings, shows firmware, capacity cards, battery percentage and read time. Local snapshot imports clear live battery data; navigation preserves the connection and pending inputs.

The compact preset dropdown sits beside Presets, with search on the right and responsive wrapping. Entries show amplifier labels **0A–19D** and actual names. A blue left-side checkmark (**#4385ff**) identifies the active preset inside the open list; the selected-item field has no check. Search shows a right-side × only when it contains text. See [Preset-Browser.md](Preset-Browser.md).

Select an effect icon to inspect it. Category labels are centered, grey icons indicate disabled effects, and details show **Enabled** or **Disabled**. Use **Enable effect** / **Disable effect** or a supported parameter's explicit **Apply** button for a verified live change; nothing is saved automatically. Ctrl+1 opens Settings, Ctrl+2 opens Version History, Ctrl+3 opens Mixer and Ctrl+F focuses search.

**Compare snapshot** and **Export diagnostics** remain available. Supported edits remain explicit and verified. Developers can run `bash scripts/build-desktop.sh` without a connected phone or amplifier; see [Build-and-Release.md](Build-and-Release.md).

To compare values, first read the amplifier or open a snapshot, choose **Compare snapshot**, and select an exported `.iamp-snapshot.json`. The read-only dialog shows **FILE** and **DISPLAYED** values. Your displayed snapshot and pending inputs stay unchanged; nothing is sent to the amplifier. Matching snapshots do not prove current live or saved state. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

If a connection problem occurs, choose **Export diagnostics** before closing the desktop app. It retains the latest 256 events in memory and excludes Bluetooth addresses, preset contents and raw packets. Nothing is uploaded automatically. The log is cleared when the app closes; a mobile-only problem while the desktop is closed cannot appear in this log. See [Diagnostics.md](Diagnostics.md).

## Start and read the amplifier

1. Open artifacts/Iamp.Desktop/Iamp.Desktop.exe. Keep only one desktop instance running, including during development tests. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**. If one amplifier is found it connects automatically; otherwise select the amplifier. Alternatively, enter a valid **Manual Bluetooth address** and choose **Connect amplifier**.
4. Browse/search all 80 bank-labelled presets. Choose an entry from the dropdown to recall it; the active preset has a blue checkmark on its left. Escape closes the dropdown without recalling a browsed entry. Select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful connection stays open and shows Connected. Current state refreshes every three seconds; **Refresh amplifier** performs an explicit reread. Choose **Disconnect** before reconnecting phone iAMP. Disconnection retains the displayed snapshot but disables edits. Use one control app at a time. Connect is disabled until a valid target is available.

## Mixer

Choose **Mixer** in the sidebar after connecting through Amplifier Settings. The page has Bluetooth, USB REC, Instrument and Master vertical faders. The first three range from −36.0 to 0.0 dB; Master ranges from −30.0 to +6.0 dB, all in 0.5 dB steps. Release a dragged fader or an adjustment key to apply, or enter a value and choose Apply/press Enter.

Use the speaker icon to mute or unmute. Muted faders turn grey and remain adjustable, matching the phone app. Changes preserve unsaved effect edits and pending effect inputs; they do not save or recall a preset. Mixer is unavailable for local snapshots, unsupported firmware or an unconfirmed connection. See [Mixer-Editing.md](Mixer-Editing.md).

## Filter presets by effect

After connecting, choose an effect from the **Effect** dropdown beside the preset search. It lists all 124 built-in models using their mobile names, prefixed by category to distinguish similar names. Type a model name while the dropdown is focused to jump to it. Presets match even when that effect is disabled.

The effect choice and name/bank/index search work together. Click **Reset** to select **All effects**; Reset is disabled while All effects is already selected. Reset leaves the text search unchanged, and the search-field × clears only text. The preset count reflects both filters. Filtering sends no recall, edit or save commands.

The first connection builds a local effect cache. Later connections and normal **Refresh amplifier** reuse it, including after restarting the desktop app. The active preset is still read live. Click **Refresh effect names** to update the full preset-effect cache when you want, for example after changing effect models in the phone app. This requires a connected supported amplifier and offers progress/Cancel; pending parameter inputs are preserved. Cached results remain searchable after disconnecting. A local snapshot searches only its one loaded preset. Built-in effect names are already bundled with the app.

## Supported controls

The sidebar displays the current desktop application version beneath **DESKTOP COMPANION**. Choose **Version History** to see dated release summaries, then select an entry to open its full details. Choose **Amplifier Settings** to return to your existing snapshot and pending inputs. History is loaded from the accompanying `assets/version_history.json` file; keep it with the published application.

The current build supports all 124 built-in Dyna, Filter, preset EQ, Amp, OD, Delay, Reverb and Mod models, plus the Mixer. Device writes use a fresh full-snapshot check and complete readback. All tested changes were restored through an independent connection. GIR&IR is excluded.

| Feature | Current support |
| --- | --- |
| Information | Firmware, reported capacity counts, battery percentage/read time and confirmed current preset. Charging-state meaning remains unverified. |
| Dyna: 4 models | Red Comp Sensitivity/Level; Yellow Comp Attack/Ratio/Threshold/Level; Noise Killer Threshold/Level; Intel Reducer Threshold/Depth/Level. Each 0–100. |
| Filter: 3 models | Touch Wah, Auto Wah and Q-Filter with their verified 0–100 settings. See [Filter-Model-Editing.md](Filter-Model-Editing.md). |
| Preset EQ: 2 models | 5 Bands EQ and Custom EQ, signed dB, stepped frequencies, decimal Q and Off cuts. Four Custom EQ groups are retained in controls and JSON. See [EQ-Model-Editing.md](EQ-Model-Editing.md). |
| Amp: 55 models | Gain, Bass, Mid, Treble, Presence, Master; all 0–100. See [Amp-Model-Editing.md](Amp-Model-Editing.md). |
| OD: 22 models | Gain, Tone, Vol; all 0–100 in mobile order. See [OD-Model-Editing.md](OD-Model-Editing.md). |
| Delay: 12 models | Level/Feedback 0–100, Time 40–2500 ms and each model's extra controls. Sweep Range 300–4000 has no inferred unit. See [Delay-Model-Editing.md](Delay-Model-Editing.md). |
| Reverb: 9 models | Pre Delay 0–200 ms, Level/Decay/Tone 0–100 and each model's extra settings, including Shimmer. See [Reverb-Model-Editing.md](Reverb-Model-Editing.md). |
| Mod: 17 models | Model-specific settings, including signed decimal Poly Shift Pitch and signed Detune Cent values. See [Mod-Model-Editing.md](Mod-Model-Editing.md). |
| Effect on/off and chain | All 124 requested model profiles; grey disabled icons, Enable/Disable, reorder/add/remove/replace and repeated instances. Parameter writes require an enabled supported layout. |
| Presets | All 80 bank labels 0A–19D, recall with discard protection, effect/text filtering and persistent search index. |
| Save/rename/Save As | All 80 destinations, 1–20 printable ASCII names, fresh checks, themed confirmation and full readback. Unicode remains unsupported. |
| Mixer | Bluetooth, USB REC, Instrument and Master level/mute, including adjustment while muted. |
| Readable JSON | View, copy and save the current preset's effects in chain/editor order. No device writes or Mixer effect properties. |
| Local snapshot files | Raw export/open/comparison for offline inspection. Applying imports to the amplifier and vendor-backup restore are unsupported. |
| Unknown layouts | No guessed controls; unsupported fields/layouts remain omitted or read-only. |

All 124 model families and the indicated ranges have retained V1.1.9 parameter/on-off evidence. Fresh snapshot/layout checks still reject unfamiliar records. General save support is verified separately; readable exports and matching snapshots alone do not prove stored state.

## Apply a supported parameter

Pending values remain while you browse other effect cards or apply another parameter. Hidden pending values still protect against accidental recall or effect toggling. An unchanged amplifier refresh retains them; loading another preset/import or refreshing changed hardware data starts from that new snapshot.

Each setting has a larger, extra-bold white label above its slider, without a duplicate summary row. The half-width slider shows the value on its right, with the filled track matching the effect icon. Tap or drag anywhere on the track, then release to apply the final value to the amplifier. The value display follows movement immediately; the edit is confirmed only after checked readback. Escape cancels an active pointer drag. Arrow keys adjust a focused slider by one supported step; Page Up/Page Down by ten steps; Home/End select its limits. Keyboard adjustments apply on key release. Slider and numeric entry stay synchronized. Disabled effects, offline/preview snapshots, busy operations and unsupported configurations cannot be edited.

Numeric entry remains available for precise changes: enter a supported value and choose Apply. Most controls use integers; EQ/Mod also accept the verified signed/decimal forms, and EQ cuts accept Off. Typing alone changes a pending value and does not send a command. Both slider release and Apply use the same fresh complete-snapshot check, verified command and full readback. Successful readback confirms the value; pending or unconfirmed edits remain marked.

Apply controls make **live, unsaved edits**. Returning all changed values to their original snapshot values clears the snapshot-difference indicator. Recalling another preset with known live unsaved edits or pending input asks whether to discard them; No is the default. If an external change conflicts with typed input, the app preserves that input, stops automatic refresh and disables editing until you explicitly refresh. Matching a read snapshot alone does not prove that the preset is saved.

## Rearrange, add or remove effects

On F15i V1.1.9, drag an effect icon to the blue insertion line between icons, or use the selected effect’s Move left/Move right buttons. The line shows the exact placement before you drop. Select + to browse the mobile-style category list and choose a model; search the model list if needed, then choose Add effect. Multiple effects from the same category and repeated identical models can be added, as in mobile iAMP. Each instance has its own chain position and pending parameter values. GIR&IR may appear as an unavailable library category; it is excluded at your request and is not planned current work. The amplifier supplies the new effect’s default values. Select Remove effect to delete the selected module.

Moves preserve the effect’s values. Previously supported parameter editors continue working at the new position. Newly added models show their names, with parameter editing available only for verified layouts. Apply or clear pending parameter inputs before changing the chain. Every change is checked against a fresh preset and verified by readback. Chain changes affect the current amplifier settings and are unsaved until you explicitly use the general Save / rename preset workflow below.

Choose **Change model…** to replace the selected effect without changing its position. The picker starts on its current category/model and lets you choose another built-in model, including another category. Replacement loads the new model’s default values; changing back to the original model also loads defaults rather than recovering its previous settings. The currently selected model cannot be reapplied accidentally. See [Dyna-Model-Editing.md](Dyna-Model-Editing.md) for the newly supported Dyna settings.

## Preset JSON and saving

### View, copy or save preset JSON

Choose **Show preset JSON** beside **Save / rename preset…**. The dialog shows verified current-snapshot effect settings in chain order, with settings ordered like the mobile/desktop editor. Disabled and repeated effects are included. Setting names omit units, which have their own `unit` property; internal IDs and raw/unmapped values are excluded. Scroll vertically or horizontally, use the **Copy JSON** icon/button to copy the complete document, or **Save JSON…** to choose a file location with Windows Save As. The default filename is the preset prefix and name, such as `0D British Lead Solo.json`.

The dialog captures the displayed snapshot when opened. Unapplied input values are excluded, and the dialog identifies them if present. Copy and Save contain exactly the JSON shown. These actions do not change the amplifier. See [Preset-JSON.md](Preset-JSON.md) for the consistent schema.

### Save changes to the amplifier

Choose **Save / rename preset…** in the current-preset card. Apply or clear pending effect/Mixer inputs first. Edit the name and choose any bank-labelled destination from **0A–19D**. Keep the current destination to save or rename it; choose another to save a copy. Names currently support **1–20 printable ASCII characters**, including spaces and punctuation; Unicode is not supported yet.

Choose **Save**. A short Bluetooth check reads the current source and destination before the confirmation opens; this is not an intentional pause. Then review the source, fresh destination name, new name and overwrite warning in the matching dark confirmation dialog. Choose **Save** to confirm or **Cancel** to stop; Cancel is the default. On confirmation the app saves once, recalls the destination and verifies its complete content. The saved destination becomes active, its name/effect filter update immediately and the unsaved marker clears. Rename saves your current amplifier settings as well as the new name, matching mobile behavior. Save As preserves the source slot’s stored contents.

The clean/rhythm/lead workflows additionally passed both desktop dialogs, reconnect, scrolled mobile Amp comparisons and user-confirmed amplifier bank/name displays; see [Musician-Workflow.md](Musician-Workflow.md). Mobile and actual Windows same-slot rename and cross-slot edited-chain saves pass recall/reconnect checks; every original preset was restored after testing. The edited saved copy also survived a physical amplifier power cycle; all 80 original presets and global settings were subsequently restored. See [Preset-Save-Rename.md](Preset-Save-Rename.md).

An unconfirmed save may have changed storage. The app discards the failed connection and attempts one read-only reconnection to reload actual settings and restore controls. It shows the outcome beside Save and never repeats the save automatically. If recovery fails or the amplifier reports a transfer-state change, use Connect / Refresh amplifier before another action. Time spent reviewing the confirmation does not consume the device deadline. Local snapshot files cannot enable saving.

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

All 80 slots were recalled in the 1.6.0 Windows check, with original 0D restored and all original bytes verified after each test. Phone iAMP was stable when the user reconnected afterward. This does not resolve the earlier loop cause. Phase 6 advanced functions remain deferred unless separately requested; GIR&IR is excluded entirely. No further mobile-screen stability confirmation is requested unless you ask.

## Build from source

Use Git Bash from the repository root:

```bash
bash scripts/run-desktop.sh
```

See [Progress.md](Progress.md) for validation results and [Plan.md](Plan.md) for the remaining tasks.
