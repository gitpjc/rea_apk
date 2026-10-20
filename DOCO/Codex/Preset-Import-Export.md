# Preset import and export

Export contains the complete preset detail: its name, original slot as provenance, all twenty ordered records, every 16-bit effect word (including on/off and opaque companion words), and the two opaque trailing bytes. Amplifier-wide state is excluded. Import targets the currently active slot, regardless of the original slot stored in the file, and confirms overwriting that destination. An exported 0A preset imported while 19C is active replaces 19C's name and settings; 0A is untouched.

The readable JSON and lossless restoration record represent the same data and are checked for consistency before import. Earlier display-only JSON is not a complete import file. The Preset JSON dialog displays the new format; Export replaces its former Save JSON action.

## Mobile evidence and implementation boundary

This feature composes previously captured mobile commands rather than inventing a whole-preset opcode:

- `Effect-Chain-Editing.md`, `tests/fixtures/f15-model-replacement-observations.json` and `f15-amp-chain-readback-observations.json`: mobile `2D` add/remove/replace operations and full readback, including repeated instances and bounded Boolean Amp companion-word changes.
- `EffectBypassCommand.cs`, `ObservedParameterCommands.cs` and the category model fixtures: mobile `25 position type byteLength values...` writes carry the complete selected effect record. Words are little-endian 16-bit values; complete-record restoration retains all exported words rather than reconstructing only visible controls.
- `Preset-Save-Rename.md` and `tests/fixtures/f15-preset-save-observations.json`: mobile `2F destination name[20]`, exact `30 01`, whole-state `13` recall, exact `14 01`, followed by independent complete state/detail readback. Saving changes the name and commits already-live effect content.
- `Preset-Format.md` / `PresetDataParser.cs`: twenty contiguous records plus two unknown trailing bytes; the current-state response is 92 bytes and is separately preserved.

The supplied mobile APK SHA-256 is `7ea003baf4dcb5073174c4f253a25deeb460dff2cdec379ae0ece7c88b931273`. REA/JADX class search for `Preset` (`ev_eee378ad7aad55186f86f35b0b1a5f8c9fe337f684abe049ba89ce1856a984a9`) found no DEX classes, consistent with retained Flutter AOT packaging evidence. Mobile `libapp.so` contains `writePresetData` and `receivePresetData` literals, but literals establish neither command encoding nor runtime behavior. No new opcode is inferred from those names. This pass did not execute the mobile app or mutate hardware.

Import supports the verified F15i V1.1.9 built-in model layouts and printable ASCII names of 1–20 characters. It validates the complete file and destination before mutation, and rejects a differing opaque trailer before writing because the captured commands do not establish how to set it. GIR/IR libraries and unverified layouts are rejected rather than silently dropped.

The coordinator reads the complete current preset before every mutation. It removes excess trailing effects, replaces mismatching identities, appends missing effects, then restores every desired complete record. Each mutation must preserve the full global state and match expected complete detail. A final exact live comparison precedes the single same-slot save. Import succeeds only after recall returns every imported detail byte (retargeted to the active slot/name) and unchanged global state. Device normalization that prevents an opaque word from being restored exactly fails verification before save; the application must never claim a partial restoration was complete.

Writes and saves are not automatically retried. Failure after a live write reports that live settings may be partially changed; a failure once saving begins reports an unknown stored outcome. Refresh reads actual amplifier settings. Transport failures do not trigger automatic rollback or save replay. Explicit user cancellation follows the restoration workflow below. Fake-transport protocol checks exercise cross-slot retargeting, repeated and disabled effects, complete words, empty chains, stale state, trailer/model rejection, cancellation, partial writes, global-state preservation, save timeout and recall failure. These checks verify coordinator behavior; they do not claim new hardware roundtrip evidence for every exported combination.

## Import progress and cancellation (1.50.1)

The import confirmation stays open while importing, with a visible progress indicator and stage messages in a fixed footer. Cancel, Escape, dialog Close and app Close request cancellation without interrupting a Bluetooth command. After complete command readback, restore and verify the captured pre-import live preset. If the import already saved, finish verifying that transaction and save the captured previous settings back; before saving, restoration preserves the existing unsaved state and does not save. Keep the dialog open throughout restoration and show the final result until Close. Connection, deadline, external-state or readback failures explicitly report that restoration was not verified; uncertain writes are not replayed.

Cancellation restores the captured active preset, including its name and complete live settings. If a completed import save must be undone, that captured previous live configuration is saved back. This differs from preserving an older stored configuration when the captured configuration contained unsaved edits. Tests cover safe-boundary cancellation before and after chain/record writes, during save/recall, rollback conflicts/failures, dialog-close interception, visible progress and completion retention. All new checks are offline; no new physical amplifier roundtrip is claimed.

## Failed-import recovery (1.50.2)

A failed import now shows Import stopped and an explicit Reconnect and inspect action inside the dialog. Retain a visible Import did not complete panel on Presets with a read-only recovery action, the underlying failure details and a notice that no import is running. After reconnecting, identify the actual preset and keep the incomplete-import warning until the user reviews and dismisses it. A successful reread restores ordinary controls without claiming import success or retrying writes. Include effect-position and returned-value differences in future verification failures instead of discarding the reason. Preserve strict readback guards and existing cancellation restoration.

A user-reported 1.50.1 failure was inspected read-only: all final command/state/detail reads completed, no preset save was recorded, and the application discarded its connection after failed verification. Moving the dialog has not been established as the cause. Reconnecting reread the actual active 18D live preset and restored controls without saving, selecting another preset or repeating import. The original detailed mismatch was not retained in 1.50.1; the new failure details preserve it. Recovery UI does not claim to correct that underlying device readback mismatch.

## Export filenames (1.50.3)

Preset exports default to `Preset-0D British Lead Solo_2026-10-20_1909.json`. Snapshot exports default to `Snapshot-0A-British-J800-Clean_2026-10-20_1909.json`. The timestamp uses local export time in `_yyyy-MM-dd_HHmm` format (24-hour clock), rather than the original capture time. Presets retain spaces in their names; snapshots replace spaces with hyphens. Existing filename sanitization, overwrite confirmation and older import/open filenames remain supported.

## Import opening and file errors (1.50.4)

Show preset-file validation errors in a modal dialog naming the file, explaining the reason and offering Choose another file or Cancel; never rely only on a small notice behind the picker. Identify Snapshot files explicitly and explain that Preset Import needs a Preset export. Default the file picker to Preset-prefixed JSON, retaining All JSON for earlier valid filenames. Open the overwrite confirmation immediately after validation and perform the fresh amplifier/destination checks under its visible progress indicator after explicit confirmation. Clear stale error notices when starting a new selection. Verify the actual Import button opens confirmation without Bluetooth work, cancellation is unchanged, and the user-reported exported preset validates. The running 1.50.3 app also accepted that exact file in a no-overwrite check; its earlier generic rejection was not reproduced.

The supplied `Preset-0A British J800 Clean_2026-10-20_2028.json` passed complete validation and opened the running application’s overwrite confirmation for 17A New Patch to 17A British J800 Clean (six effects). That confirmation was cancelled; no import or save was sent. This establishes the export is valid, not the cause of the earlier generic rejection.

## Hyphenated preset filenames (1.50.5)

Preset export names now replace spaces with hyphens, including the separator after the preset slot: `Preset-0A-British-J800-Clean_2026-10-20_2041.json`. The local timestamp, saved preset name/content and existing-file import compatibility are unchanged. This supersedes the space-preserving filename convention above.
