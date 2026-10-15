# Effect-chain editing — 1.25.0

## Second-Amp readback correction — 1.25.4

Adding 65 US DLX at position 6 succeeds on the amplifier, but also changes the original position-3 PLX100 ninth word from 1 to 0. Removal restores that word to 1. All original parameter words and the complete 92-byte global state stay unchanged. Its meaning remains opaque; it is not treated as a user-facing enable/disable value. The previous exact surviving-record comparison rejected this valid transition, invalidated editing and left the old UI visible. Restarting read the actual chain, which explained the reported symptoms.

Amp add/remove/move verification now permits only Boolean changes in that ninth word of matching nine-word Amp records. Every other detail byte, model, record sequence, opaque trailer and complete global-state byte remains checked. Non-Amp chain operations do not receive this allowance. PLX100 parameter writes preserve either observed flag value. A genuinely unexpected complete readback is displayed immediately with an unconfirmed status and editing disabled until refresh, rather than leaving a stale chain.

Actual desktop handlers tested adding/removing both 65 US DLX and another PLX100, checking immediate icon counts/selection and enabled Remove controls without restarting. Each test also edited original PLX100 Bass 35→34→35 with its flag at 0, moved/reversed the new Amp and independently restored the complete original preset. Focused WPF/protocol cases cover observed flag changes, rejection of unrelated parameter/flag differences and authoritative add/remove views on an unconfirmed outcome. No saves or recalls occurred. Sanitized evidence: `tests/fixtures/f15-amp-chain-readback-observations.json`.

## Mobile parity correction — 1.25.3

**The earlier one-category-per-chain inference was wrong.** Direct ADB testing successfully added Red Comp while Noise Killer was present, and then a second identical Noise Killer. The mobile payloads appended new modules at position 6 (`2D 00 01 06 04 00` for Red Comp and `2D 00 01 06 04 02` for Noise Killer). Both temporary additions were removed immediately and independent Windows reads matched the fresh original complete state/detail byte for byte. No saves or recalls occurred.

Remove the category/model occupancy restriction from the picker and chain codec. Built-in models remain white and addable even when their category or identical model is present. Repeated instances are addressed by their explicit chain position. The 18 supported parameter controls retain independent pending values for each instance; switching, refresh, applying and moving cannot redirect values to another identical model. Legacy transport methods still refuse ambiguous implicit targets; desktop writes use the explicit guarded positional API.

Actual desktop tests added second identical Noise Killer and OD808 instances, edited and restored each, moved each 6→2, edited/restored at the new position, reversed the move and removed the addition. The full original preset was independently restored. Focused protocol and WPF checks cover repeated records, exact transformations, all model names being addable, per-instance pending values and prior editor regressions. GIR/IR library support remains outside this correction. Earlier occupancy descriptions below record the superseded implementation, not a mobile restriction.

## Background refresh correction — 1.25.2

The three-second background read incorrectly disabled/re-enabled Add, Move and Remove, causing ongoing flashing even after a temporary effect was removed. Background reads now leave their availability stable; a valid user edit uses the existing serialized workflow queue and its fresh complete-snapshot guards. Real user operations, invalid snapshots, offline/unsupported states and pending values still disable edits. A background read already underway cannot replace the snapshot of an active drag or picker. Twenty offline refresh cycles verify stable controls and drag feedback, and eight focused workflow-queue tests pass. No hardware was accessed for this correction.

## Usability correction — 1.25.1

Drag to a gap before or after an icon. A vertical blue (`#4385ff`) insertion line shows the exact destination, including wrapped rows and row ends; whitespace between cards accepts drops. The source index is adjusted once when moving forward, so the line and final order agree. Leaving/cancelling the drag clears the line. Automatic polling pauses during the gesture and while the model picker is open; the usual fresh complete-snapshot check still runs before a write.

The picker now has a mobile-style left category list containing Dyna, Filter, OD, Amp, GIR&IR, EQ, Mod, Delay and Reverb. All 124 built-in model names remain visible, including categories already present. An occupied category explains why Add is disabled; disabled modules still occupy their category. GIR&IR is visibly listed with an unavailable-library explanation; no GIR/IR access, transfer or Phase 6 work was added. The earlier three-category-only picker hid occupied categories, which caused the reported mismatch.

Focused offline WPF checks cover every insertion gap in both move directions, actual marker geometry at two widths, wrapped rows, marker cleanup/colour, all nine category entries and all 124 model names, occupied/disabled-category guards, model search, GIR&IR's state and minimum picker sizing. No phone or amplifier was accessed for this UI-only correction.

On F15i firmware V1.1.9, drag an effect to an insertion gap to move it to that position. The selected effect also has Move left, Move right and Remove effect buttons. The + button opens a category/model picker with local search. Only categories absent from the chain can be added, even if an existing effect is disabled. New effects are appended with defaults returned by the amplifier. Existing effects retain their complete parameter records when reordered. These are live, unsaved changes; the existing restricted save workflow is unchanged.

The picker covers the 124 built-in catalog models. User GIR/IR/GNR libraries and their transfers are outside this release. All 18 previously supported parameter editors follow their effect's new position. New models display their catalog identity; parameter editing and enabled-state controls still require separately verified model layouts. Adding a model does not invent parameter semantics.

## Runtime evidence

The user authorized ADB operation of iAMP and phone-only screenshots. A fresh independent Windows read recorded the complete starting state and 20-record detail payload. ADB moved Noise Killer from position 1 to 3 and restored it through 3→2→1; the phone added Touch Wah at position 6 and removed it with the upward-drag gesture documented on printed manual page 43 (PDF page 46). A temporary Reverb bypass was also reversed. A new independent Windows read matched both original payloads byte for byte. No preset was saved or recalled.

Observed payloads (excluding the existing frame envelope):

| Action | Payload |
| --- | --- |
| Move 1→3 | `2D 97 04 01 03` |
| Move 3→2 | `2D 98 04 03 02` |
| Move 2→1 | `2D 99 04 02 01` |
| Add Touch Wah at 6 | `2D 00 01 06 05 00` |
| Remove position 6 | `2D 32 03 06` |

Byte 1 varies as a request token; operation bytes 1/3/4 select add/remove/move. The client allocates counters from separate bands based on the observed mobile starting tokens 0/50/150. Exact counter-band boundaries are not claimed as a firmware rule. Windows testing observed suppression when a temporary BLE session reused a previous move token. Counters now survive BLE session changes in the process. A fresh-token second attempt is permitted only after a successful complete read proves **every state and detail byte unchanged**. A failed, partial or unexpected read never permits this retry. Saves are never retried by this workflow.

## Verification and limits

Every mutation is serialized with other session workflows. Firmware, complete fresh state/detail and the expected starting snapshot must match before writing. Move/removal transforms renumber positions while preserving the original name/header, all surviving words and opaque trailer bytes. An addition is accepted only when removing its new record would recover every original byte, and its returned type/model/enabled prefix matches the request. Unknown layout, duplicate categories, holes and invalid positions are rejected. Busy/polling/offline/preview/unsupported-firmware/invalid-snapshot/pending-input states disable chain actions.

Focused protocol tests cover captured commands, every move/inverse pair, disabled flags, opaque fields, compaction, invalid layouts and unexpected addition differences. Offline WPF checks cover category availability, selection, all supported moved editors, pending input handling and disabled actions. Actual Windows transport tests cover three move/inverse pairs and Filter Touch Wah, EQ 5 Bands EQ and Mod Phaser add/remove pairs. Actual desktop handlers additionally exercise moved Noise Killer Threshold and moved OD808 Tone, restoring each value and the order. Final independent reads verify the complete original state and detail payloads.

The representative models above were physically tested; all 124 models were not individually exercised. Firmware rejection or any unexpected readback disables further editing until an explicit refresh. Phase 6 was not investigated or changed.

Sanitized evidence and hashes: `tests/fixtures/f15-effect-chain-observations.json`. Device identifiers, raw phone logs and screenshots remain in ignored `analysis/private`.
