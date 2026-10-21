# Running the current iAMP Desktop build

Updated 21 October 2026. The core F15i editor is implemented; broader connection, usability and deployment acceptance remains open. See [Current-Status.md](Current-Status.md) for scope and [Musician-Workflow.md](Musician-Workflow.md) for the completed clean/rhythm/lead checks.

Current release: **1.51.1**. Settings includes Preserve window size; the Presets experimental badge is removed. The shared amplifier header is visible on every amplifier-related page; Presets replaces Amplifier Settings and Snapshots has its own fixed viewing state. Snapshot comparisons show each preset’s named effects/settings in separate columns, highlighting differing comparable values. Exports default to `Snapshot-0A-British-J800-Clean_2026-10-20_1909.json`. Report headings are bold/larger and Copy report exports Markdown. Previous release: **1.46.0**. There are 126 parameter control types across 124 verified V1.1.9 models, including all Dyna, Filter, preset EQ, Amp, OD, Delay, Reverb and Mod models. Most use **0–100**; Mod also includes signed decimal Pitch and Detune cents. EQ supports signed dB, decimal Q, stepped Hz and Off cut values. Reverb Pre Delay uses **0–200 ms** and all Delay Time controls use **40–2500 ms**; Sweep Range uses **300–4000**. Choose **Change** to replace the selected effect in its current chain position using amplifier defaults.

Use **Amplifier Settings** to connect, choose presets and edit effects. Scan/select loads the library and current preset automatically; a live connection refreshes current state about every three seconds. Disconnect phone iAMP before connecting from Windows. **Amplifier Information**, directly below Settings, shows firmware, capacity cards, battery percentage and read time. Local snapshot imports clear live battery data; navigation preserves the connection and pending inputs.

The compact preset dropdown sits beside Presets, with search on the right and responsive wrapping. Entries show amplifier labels **0A–19D** and actual names. A blue left-side checkmark (**#4385ff**) identifies the active preset inside the open list; the selected-item field has no check. Search shows a right-side × only when it contains text. See [Preset-Browser.md](Preset-Browser.md).

Select an effect icon to inspect it. Category labels are centered, grey icons indicate disabled effects, and details show **Enabled** or **Disabled**. Use **Enable** / **Disable** or adjust a parameter slider for a verified live change. Effect parameters show a slider and current value without manual entry or Apply buttons. Release the mouse or adjustment key to apply; arrow keys move one supported step. The **Unsaved changes exist** warning indicates differences from the preset entry state; use **Save / rename preset…** to save permanently. Ctrl+1 opens Settings, Ctrl+2 opens Version History, Ctrl+3 opens Mixer, Ctrl+4 opens Tuner, Ctrl+5 opens Drum and Ctrl+F focuses search. Tools lists Tuner above Drum.

**Compare snapshot** and **Export diagnostics** remain available. Supported edits remain explicit and verified. Developers can run `bash scripts/build-desktop.sh` without a connected phone or amplifier; see [Build-and-Release.md](Build-and-Release.md).

To compare values, first read the amplifier or open a snapshot, choose **Compare snapshot**, and select an exported `.iamp-snapshot.json`. The read-only dialog shows **FILE** and **DISPLAYED** values. Your displayed snapshot and pending inputs stay unchanged; nothing is sent to the amplifier. Matching snapshots do not prove current live or saved state. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

If a connection problem occurs, choose **Export diagnostics** before closing the desktop app. It retains the latest 256 events in memory and excludes Bluetooth addresses, preset contents and raw packets. Nothing is uploaded automatically. The log is cleared when the app closes; a mobile-only problem while the desktop is closed cannot appear in this log. See [Diagnostics.md](Diagnostics.md).

## Start and read the amplifier

1. Open artifacts/Iamp.Desktop/Iamp.Desktop.exe. Keep only one desktop instance running, including during development tests. Keep all accompanying files together; the published Windows x64 build does not require a separate .NET installation.
2. Switch on the amplifier and disconnect the Android iAMP app from it.
3. Choose **Scan Bluetooth**. If one amplifier is found it connects automatically; otherwise select the amplifier. Alternatively, enter a valid **Manual Bluetooth address** and choose **Connect amplifier**.
4. Browse/search all 80 bank-labelled presets. Choose an entry from the dropdown to recall it; the active preset has a blue checkmark on its left. Escape closes the dropdown without recalling a browsed entry. Select effect cards to inspect supported details.

Control uses Windows Bluetooth LE. USB charging or the vendor utility's USB connection is a separate connection. A successful connection stays open and shows Connected. Current state refreshes every three seconds; **Refresh amplifier** performs an explicit reread. Choose **Disconnect** before reconnecting phone iAMP. Disconnection retains the displayed snapshot but disables edits. Use one control app at a time. Connect is disabled until a valid target is available.

## Drum

Choose **Tools → Drum** (Ctrl+5) after connecting through Amplifier Settings. Settings and playback status load automatically. Drag the circular tempo dial or use the arrows for **40–260 BPM**. Click its **TAP** center repeatedly to set the beat; the amplifier calculates tempo from tap intervals. Keyboard arrows adjust the focused dial; Enter/Space taps.

Choose a **Style**: Pop, Rock, Blues, Funk, Jazz, Metal or Metronome. Its ten numbered buttons appear below; click **01–10** to select a pattern immediately. Browsing styles does not change the playing rhythm. The selected pattern is highlighted, with its full confirmed name underneath. The mobile tenth label remains **10 POP010** in the current-rhythm label and tooltip. **Play / Pause** controls playback. The speaker opens a vertical volume popup; it does not mute. The observed mobile volume selections run from **0 to 99**.

Changes are live and do not save or recall a preset. Playback continues when you leave Drum, including a visit to Tuner. Pause it explicitly when finished. Pending effect inputs are preserved. Reconnect after an unconfirmed operation; queued edits are discarded rather than replayed. See [Drum.md](Drum.md).

## Preset/Effect Details

Click **Preset/Effect Details** beside the **Preset** dropdown for a scrollable report of all loaded preset names grouped by bank and all available built-in effect models grouped by category. Filters do not restrict the report. Use the copy icon to copy the entire report as Markdown. It uses loaded data and does not read, recall, edit or save amplifier settings. Connect once after upgrading to populate the preset-name cache. Later launches show last-read names offline in the dropdown and report; connect to refresh them. Confirmed saves and renames update the cache. Cached names never enable amplifier editing. See [Preset-Effect-Details.md](Preset-Effect-Details.md).

## Mixer

Choose **Mixer** in the sidebar after connecting through Amplifier Settings. The page has Bluetooth, USB REC, Instrument and Master vertical faders. The first three range from −36.0 to 0.0 dB; Master ranges from −30.0 to +6.0 dB, all in 0.5 dB steps. Release a dragged fader or an adjustment key to apply, or enter a value and choose Apply/press Enter.

Use the speaker icon to mute or unmute. Muted faders turn grey and remain adjustable, matching the phone app. Changes preserve unsaved effect edits and pending effect inputs; they do not save or recall a preset. Mixer is unavailable for local snapshots, unsupported firmware or an unconfirmed connection. See [Mixer-Editing.md](Mixer-Editing.md).

## Filter presets by effect

After connecting, choose an effect from the **Effect** dropdown beside the preset search. It lists all 124 built-in models using their mobile names, prefixed by category to distinguish similar names. Type a model name while the dropdown is focused to jump to it. Presets match even when that effect is disabled.

The effect choice and name/bank/index search work together. Click **Reset** to select **Any effect**; Reset is disabled while Any effect is already selected. Reset leaves the text search unchanged, and the search-field × clears only text. The preset count reflects both filters. Filtering sends no recall, edit or save commands.

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

On F15i V1.1.9, drag an effect icon to the blue insertion line between icons, or use the selected effect’s Move left/Move right buttons. The line shows the exact placement before you drop. Select + to browse the mobile-style category list and choose a model; search the model list if needed, then choose Add effect. Multiple effects from the same category and repeated identical models can be added, as in mobile iAMP. Each instance has its own chain position and pending parameter values. GIR&IR may appear as an unavailable library category; it is excluded at your request and is not planned current work. The amplifier supplies the new effect’s default values. Select Remove to delete the selected module.

Moves preserve the effect’s values. Previously supported parameter editors continue working at the new position. Newly added models show their names, with parameter editing available only for verified layouts. Apply or clear pending parameter inputs before changing the chain. Every change is checked against a fresh preset and verified by readback. Chain changes affect the current amplifier settings and are unsaved until you explicitly use the general Save / rename preset workflow below.

Choose **Change** to replace the selected effect without changing its position. The picker starts on its current category/model and lets you choose another built-in model, including another category. Replacement loads the new model’s default values; changing back to the original model also loads defaults rather than recovering its previous settings. The currently selected model cannot be reapplied accidentally. See [Dyna-Model-Editing.md](Dyna-Model-Editing.md) for the newly supported Dyna settings.

## Preset JSON and saving

### View, copy or save preset JSON

Choose **Show preset JSON** beside **Save / rename preset…**. The dialog shows verified current-snapshot effect settings in chain order, with settings ordered like the mobile/desktop editor. Disabled and repeated effects are included. Setting names omit units, which have their own `unit` property; the readable effects list is accompanied by a lossless restore payload with an integrity checksum. Scroll vertically or horizontally, use the **Copy JSON** icon/button to copy the complete document, and close the dialog as before. Use **Export** on the Presets screen to save the complete JSON file. The default filename is the preset prefix and name, such as `Preset-0D-British-Lead-Solo_2026-10-20_1909.json`.

The dialog captures the displayed snapshot when opened. Unapplied input values are excluded, and the dialog identifies them if present. Copy and Save contain exactly the JSON shown. These actions do not change the amplifier. See [Preset-JSON.md](Preset-JSON.md) for the consistent schema.

### Save changes to the amplifier

Choose **Save / rename preset…** in the current-preset card. Apply or clear pending effect/Mixer inputs first. Edit the name and choose any bank-labelled destination from **0A–19D**. Keep the current destination to save or rename it; choose another to save a copy. Names currently support **1–20 printable ASCII characters**, including spaces and punctuation; Unicode is not supported yet.

Choose **Save**. A short Bluetooth check reads the current source and destination before the confirmation opens; this is not an intentional pause. Then review the source, fresh destination name, new name and overwrite warning in the matching dark confirmation dialog. Choose **Save** to confirm or **Cancel** to stop; Cancel is the default. On confirmation the app saves once, recalls the destination and verifies its complete content. The saved destination becomes active, its name/effect filter update immediately and the unsaved marker clears. Rename saves your current amplifier settings as well as the new name, matching mobile behavior. Save As preserves the source slot’s stored contents.

The clean/rhythm/lead workflows additionally passed both desktop dialogs, reconnect, scrolled mobile Amp comparisons and user-confirmed amplifier bank/name displays; see [Musician-Workflow.md](Musician-Workflow.md). Mobile and actual Windows same-slot rename and cross-slot edited-chain saves pass recall/reconnect checks; every original preset was restored after testing. The edited saved copy also survived a physical amplifier power cycle; all 80 original presets and global settings were subsequently restored. See [Preset-Save-Rename.md](Preset-Save-Rename.md).

An unconfirmed save may have changed storage. The app discards the failed connection and attempts one read-only reconnection to reload actual settings and restore controls. It shows the outcome beside Save and never repeats the save automatically. If recovery fails or the amplifier reports a transfer-state change, use Connect / Refresh amplifier before another action. Time spent reviewing the confirmation does not consume the device deadline. Local snapshot files cannot enable saving.

If an edit is interrupted or not confirmed, its outcome may be unknown. Click **Connect amplifier** (or **Refresh amplifier** when connected) before another edit and inspect the actual values; the app does not automatically replay an ambiguous write.

If reading fails with service discovery **Unreachable** while Android iAMP is connected, disconnect/close iAMP and retry **Connect amplifier** (or **Refresh amplifier** when connected). That contention case was reproduced and the subsequent Windows read succeeded; broader automatic recovery is still being tested. After PC restart, scanning may be needed before a manually entered address can be opened. A bounded exact-address scan fallback handles null address lookup before commands; it does not retry ambiguous writes or verify identity from an advertisement alone.

## Save and open local snapshots

Choose **Snapshots** in the sidebar. **Capture current preset** reads the connected amplifier into a separate, fixed capture; pending editor inputs are excluded. **Open snapshot…** loads an exported file for viewing without changing the amplifier connection, preset editor, preset names or pending inputs. The viewing context identifies the preset, source and capture time; an amplifier capture remains unchanged when the amplifier switches presets, with a notice once that change is observed.

**Export this snapshot…** saves the viewed capture in a `Preset-<code>-<preset-name>-snapshot.json` file; legacy `.iamp-snapshot.json` files remain supported. **Compare with file…** compares another file against that fixed capture, showing named effects/settings in each chain's own order. Opening, exporting and comparing work offline. Files retain schema/integrity validation and a 64 KiB limit; invalid files preserve the previous view. There is no apply/restore action. To store amplifier settings in a slot, use **Save / rename preset** on **Presets**. See [Snapshot-Comparison.md](Snapshot-Comparison.md).

## Shared amplifier header

Version History and Settings hide the amplifier header and uses the full content height. Returning to another screen restores the header without changing the connection.

Every amplifier-related screen has the same amplifier header; Version History and Settings omit it. It shows the connection and last confirmed active preset; retained disconnected data says **Last read preset**. Use the selector, **Scan Bluetooth**, **Disconnect** and **Connect amplifier / Refresh amplifier** on the same wrapping row. **Manual Bluetooth address** expands when needed; Cancel appears during an operation. Keep phone iAMP disconnected while the desktop controls the amplifier. The former **Amplifier Settings** page is now **Presets**. The header describes the amplifier, while Snapshots separately describes the capture/file being viewed.

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

## Physical controls and preset display

The closed preset dropdown follows the confirmed active preset after polling or Refresh; an open popup keeps your browsing choice. Conflicting physical changes preserve unapplied input and block edits until Refresh. A physical preset switch can overlap a read. Version 1.41.4 retries a complete inconsistent read up to three times. If the amplifier keeps changing, leave the controls still and reconnect. Genuine transport failures still require reconnect; writes and saves are never replayed. See [Physical-Panel-Sync.md](Physical-Panel-Sync.md).

## Physical Master knob and Mixer Master

The Mixer Master value should not be treated as the amplifier’s physical knob display. In the verified physical0→10→0 test, both desktop and mobile iAMP kept Mixer Master at+6dB. The desktop matches that observed mobile behavior; no general physical-to-Mixer mapping is established. See [Physical-Panel-Sync.md](Physical-Panel-Sync.md).

## Tuner

Open **Tuner** or press **Ctrl+4** while connected. Entry includes a three-second quiet interval before activation. Play one note with guitar volume on. **Low!** means flat; **High!** means sharp; green **Perfect!** indicates in tune. At the exact center the whole meter turns green; near-center readings show two green segments. The meter matches observed mobile segment spacing and colors; it does not report cents. A steady note remains visible until the amplifier reports no signal; turning guitar volume off should clear it.

Use **− / +** to set reference pitch from **430 to 445 Hz**. **Mute / Bypass** selects tuner output behavior. **Stop tuner**, leaving the page or closing the app exits tuner mode and verifies state before normal editing resumes. No preset is saved automatically. Settings and entry support are verified for F15i V1.1.9. See [Tuner.md](Tuner.md).

## Desktop settings

Open **Settings** below **Export diagnostics**. Use the switch on the right of **Preserve window size**, enabled by default, and remembers normal size and maximized state between sessions. Turning it off leaves the current window unchanged and uses the default size on the next launch. Preferences save automatically on this computer; saved sizes fit the current work area and app minimum. See [Desktop-Settings.md](Desktop-Settings.md).

Version 1.49.7 places Reset and Refresh effect names beside the Effect dropdown (wrapping on narrow windows). The redundant manual recall/status row is removed; choose a preset through the dropdown to recall it.

The cached-effects timestamp sits to the right of Refresh effect names, centered on the button row; the group wraps on compact windows.

Preset search is beside the preset selector, before Preset/Effect Details. The redundant Preset heading/count has been removed; search wraps below on compact windows.

The selected effect’s enabled status and Enable/Disable button sit beside its name. Change, ← Move left, Move right → and Remove sit beside the power control, wrapping on narrow windows. The active effect’s power button says Disable; when the effect is off it says Enable and can be clicked to enable it again. Enabled status is blue and Disabled status is red. The effect name is vertically centred with these controls. All actions stay outside the parameter scroll area. Long parameter lists scroll independently, with mobile order and Custom EQ groups preserved; browsing effects retains pending values.

The effect parameter viewport fits the space left after the other page controls. Wheel scrolling inside it stays inside at both ends and never moves the outer page; its final setting and helper text are reachable without parameter overflow at normal window sizes. Other page areas can still scroll normally on compact windows. The application uses slim dark scrollbars with rounded thumbs; drag a thumb or click the track to scroll. Scrollbar hover and drag colors identify the active control.

## Export and import presets

Use **Export** beside Show preset JSON to save a complete preset, including its name, chain order, models, disabled effects and every stored effect word. Apply or clear pending inputs first. The JSON viewer displays the same schema 4 format and retains Copy JSON; its Save JSON button is replaced by Export.

Select the destination preset on the amplifier, then choose **Import** and an exported JSON file. Review the active destination and replacement name, then choose **Import and overwrite**. Import restores and saves into that active slot; the original file slot does not select the destination. For example, importing a 0A British J800 Clean export while 19C New Patch is active creates 19C British J800 Clean. The original 0A is unchanged. Mixer and other global settings are preserved.

Old display-only JSON is rejected because it lacks complete restore data. Import/export currently supports verified F15i V1.1.9 built-in models. Files with differing unsupported preset trailer fields are rejected before editing. On interruption, inspect the reported outcome and refresh; imports are never automatically repeated. See [Preset-Import-Export.md](Preset-Import-Export.md) for implementation and validation limits.

### Import progress and cancellation

The import confirmation stays open while importing, with a visible progress indicator and stage messages in a fixed footer. Cancel, Escape, dialog Close and app Close request cancellation without interrupting a Bluetooth command. After complete command readback, restore and verify the captured pre-import live preset. If the import already saved, finish verifying that transaction and save the captured previous settings back; before saving, restoration preserves the existing unsaved state and does not save. Keep the dialog open throughout restoration and show the final result until Close. Connection, deadline, external-state or readback failures explicitly report that restoration was not verified; uncertain writes are not replayed.

### If an import stops

Choose **Reconnect and inspect** in the Import stopped dialog or the Presets recovery panel. This reads the amplifier’s current live settings without saving, recalling a different preset or repeating import. Review the actual effects shown afterward. The notice continues to say the import was incomplete; dismiss it after review. If reconnecting fails, check amp power and disconnect phone iAMP before trying the read again. Expand Failure details to see why verification stopped.

## Unsaved preset warning

A bold amber **Unsaved changes exist** message flashes beside the Presets heading when the current preset has live unsaved edits or unapplied parameter inputs. It stays visible while browsing that preset’s effects. Saving the preset or switching to another preset clears the message; reverting pending values also clears it when no live edits remain. Mixer-only inputs do not mark a preset as unsaved.

### State-based unsaved warning (1.50.7)

The warning compares the current preset against a frozen copy taken when that preset was entered. It includes the name, ordered effect instances/models, enabled states and parameter values. Undoing all differences clears the warning automatically, including adding then removing an effect, restoring a deleted effect with its original settings, and moving effects back. Re-adding the same model with different default settings is still a change. Refreshing the same preset retains the entry baseline; saving/importing successfully establishes a new clean baseline, and entering a different preset/amplifier establishes its own baseline. Pending parameter values remain included; global mixer/Tools state and the amplifier’s derived Boolean Amp companion flag do not create false preset warnings. Discard prompts use the same comparison.
