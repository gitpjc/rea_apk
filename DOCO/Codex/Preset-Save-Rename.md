# Save, rename and save a copy

Version 1.41.0 replaces the desktop's narrow Threshold-25/26 save gate with the verified general F15i V1.1.9 save workflow. The current preset's complete live effect chain is saved; unapplied typed or slider values are not included and must be applied or cleared first.

1. Choose **Save / rename preset…** in the current-preset card.
2. Edit the name and choose any bank-labelled destination from **0A–19D**. Keep the same destination to save or rename the current preset; choose another to save a copy.
3. Choose **Save**. The destination is read from the amplifier again.
4. Review the source, destination's existing name, new name and overwrite warning. The matching dark dialog uses **Save** and **Cancel**, with Cancel as the default. Confirming saves once, recalls that destination and checks the complete result.

After success, the saved destination becomes active, its displayed name and effect-filter cache update immediately, and the unsaved marker clears. Save As also rereads the source slot’s stored effects, so its abandoned unsaved chain is not incorrectly kept in the filter cache. An unverified cache is invalidated and a refresh is required. Save As leaves the original source's stored contents unchanged. Renaming is part of saving the live settings, matching the mobile Save dialog; it also commits any already-applied live edits.

Names support **1–20 printable ASCII characters**, including spaces and punctuation. The mobile input limit and a complete 20-character rename are observed. Unicode encoding is not yet established, so non-ASCII names are rejected rather than substituted or truncated. Imported snapshots, preview data, unsupported firmware/layouts, pending inputs, busy operations and stale source/destination data cannot enable a save.

## Protocol and guards

Successful mobile captures show `2F`, a destination byte (1–80), and 20 name bytes, zero-padded when shorter. Effect data is already in the live preset and is not reconstructed into that request. The expected saved detail is an exact clone of the source payload, changing only the destination byte and name field; every effect record and opaque trailer byte is preserved.

The transport verifies V1.1.9/80-slot identity, the exact current snapshot, and the exact destination shown in the overwrite confirmation. It rereads the source after the destination read. Changes before the write produce a distinct precondition error stating that nothing was saved.

After a single save request, the reply must be exactly `30 01`. The client follows the captured mobile workflow with a whole-state `13` recall of the chosen slot, requires `14 01`, and compares every state/detail byte against the expected result. Same-slot and cross-slot names, complete effect content and global preservation were independently checked after recall and reconnection.

A timeout, disconnect, unexpected acknowledgement or readback failure can leave an unknown stored outcome. The failed session is discarded. One read-only reconnection reloads actual settings and restores controls when successful; failure/transfer-state changes require Connect / Refresh. The outcome remains beside Save. Human confirmation time is excluded from the operation deadline. Saves are never automatically repeated. The software does not automatically restore a previous destination or substitute a factory reset/vendor backup restore.

## Evidence and tests

The private study began by reading the complete current preset and all 80 details without writes. An inactive read and controlled recall distinguished the active live values from stored content and confirmed that the original live and stored 0D matched. Guarded phone input/capture retained the Save dialog, bank-labelled destination picker, 20-character limit, same-slot rename and cross-slot save. Keyboard interaction is allowed only when metadata identifies iAMP as its input target; screenshots still reject any visible keyboard or foreign overlay.

The public fixture `tests/fixtures/f15-preset-save-observations.json` retains independently captured same-slot/cross-slot save and subsequent recall frames, full expected details and SHA-256 evidence references. Phone captures/logs and user preset baselines remain private.

- 44 focused protocol, legacy save, recall and workflow cases pass. They cover independent mobile frames, all 80 destination encodings, full byte preservation, reordering/disabled effects, name padding and invalid inputs.
- Focused WPF checks cover all 80 choices, unchanged rename/save-as, broader parameter edits, pending effect/Mixer inputs, imported/offline/busy/invalid states and three dialog sizes. The affected Mixer checks also pass. Normal and compact save-dialog renders were visually inspected.
- Actual Windows handlers renamed 0D and restored its name, with immediate library updates, full readback and independent recall verification.
- Actual Windows Save As retained Threshold 27, Tone 99, a disabled OD and an added duplicate Dyna module in 19D. The source slot remained unchanged. The destination's original default Amp chain and name were rebuilt and compared byte-for-byte before one restorative save.
- All 80 original preset details, the original selected 0D and complete global state were restored and independently verified after the cross-slot tests.
- Read-only Windows stale-source and stale-destination checks rejected the operation before any save command; diagnostics and independent baseline reads verify this.

**Power-cycle persistence passed.** After the user switched the amplifier off, waited five seconds and restarted it, an independent Windows read found 19D / Windows Save 80 with the exact complete saved detail: Threshold 27, Tone 99, disabled OD and duplicate Dyna. The original 19D was rebuilt, saved and recalled; all 80 original details, original 0D selection and complete global state then matched the baseline independently. The final source-filter-cache regression also passed with complete restoration. Evidence hashes and the actual post-restart state/detail are retained in the public fixture. GIR&IR and Phase 6 remain excluded.

The self-contained Windows x64 release is published to `artifacts/Iamp.Desktop`. Version 1.41.2 and all 66 history entries pass source/package checks. Release builds have zero warnings/errors; the published Desktop, Transport and Protocol DLL hashes match staging. The GitHub workflow remains disabled locally, with no commit or push performed.

## 1.41.1 save failure regression

The reported original failure before confirmation did not recur in direct Windows dialog checks. An intentionally stale source reproduced the failed-session/disabled-state path: it sent no save command, discarded the terminal session, reconnected read-only and enabled Save with the actual settings. An exact unchanged 0D British Lead Solo copy to19D succeeded and immediately updated the desktop name. All80 saved details and the fresh original active19D/global state were independently restored. Focused checks cover both themed confirmation actions, three sizes, Cancel default and review time exceeding a shortened device deadline.

## Current wording and workflow acceptance — 1.41.2

Both save dialogs describe **Current amplifier settings**. This means the amplifier’s currently applied chain and values, including unsaved edits; unapplied desktop inputs are excluded. The initial Save action rereads the destination and source before opening confirmation, so Bluetooth verification can take several seconds. It is not a fixed three-second confirmation timer. The themed confirmation defaults to Cancel; saves are never replayed.

The completed [Musician-Workflow.md](Musician-Workflow.md) P5.8 checks extend the earlier results above with clean/rhythm/lead copies, actual desktop dialogs, reconnect, scrolled mobile comparisons, physical display confirmation and independent full restoration after every case. They do not close the broader fault, accessibility or clean-PC gates.
