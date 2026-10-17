# Windows same-slot save test passed

## Scope at 17 October 2026 — current application 1.41.2

This is a retained early bounded experiment/procedure and its original results, not a current feature limit or instruction to rerun a device command. All 124 requested built-in models now have verified controls; all 80 destinations support general save/rename/Save As, with edited-copy power-cycle persistence and P5.8 workflow evidence. See Current-Status.md, Preset-Save-Rename.md and the category specifications. Earlier fixed 0D baselines, restricted intervals, test counts, next steps and unexecuted procedure checkboxes remain historical; always record a new baseline and preserve every original setting before authorized testing.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


14 October 2026. The user explicitly approved the Windows save-and-restore test. Version **1.2.0** passed the actual Windows Apply/Save handlers and is now published in `artifacts/Iamp.Desktop`. Original Threshold 25 and every original byte were independently restored and verified after recall.

Automatic approval review rejected the proposed Windows `--save-roundtrip-preview` command because the prior explicit approval covered the phone-app persistence experiment, not the new Windows save path. The entire rejected shell command did not execute, including its preliminary read. No Windows save or parameter edit occurred in that attempt. No workaround or alternative write path was attempted.

The later attempt followed the user's explicit statement **“I approve the Windows save-and-restore test.”** Its preliminary read first reported service discovery Unreachable; no edit occurred. Stopping the competing phone iAMP app allowed exact original baseline verification and the approved test to proceed.

## Result

`analysis/private/desktop-save-hardware.save-roundtrip.json` reports `passed: true`, `baselineRestored: true`, `originalSaveConfirmed: true`, `failure: null`. The actual Windows handlers applied/saved 26, a separate connection verified its full detail, the handlers applied/saved original 25, and an independent connection recalled 5→4 and verified complete original state/detail. Another fresh read in `windows-save-ui-final.json` also matched the original baseline hashes. `desktop-save-hardware.png` was visually inspected and shows the restored original preset and passed status.

The published executable and Desktop/Transport/Protocol DLLs match tested staging byte for byte, and the packaged history JSON matches source. Published Version History QA passed at 1.2.0. The app initially locked its loaded files during publication; the user closed it and the complete copy then succeeded.

## Exact proposed operation

Use the new actual Windows desktop Apply/Save handlers to perform only:

1. Read the amplifier and require firmware V1.1.9, current slot **4 / 0D / British Lead Solo**, and the complete original state/detail hashes from `Preset-Saving.md`. Record that baseline privately before the first edit. Retain the verified vendor backup and quiet-volume/no-playback condition.
2. Apply Noise Killer Threshold **25 → 26**, preserving all other original bytes.
3. Save **26** once to **the same slot 4**, with the unchanged original name. The opt-in helper provides the already approved test confirmation to the real Save handler; normal UI usage shows an explicit Yes/No overwrite confirmation with No selected by default.
4. Require the observed response, recall the same slot, and compare complete raw state/detail. A separate fresh session independently verifies the saved-26 snapshot.
5. Apply original Threshold **25**, save it back once to **slot 4**, and verify it.
6. In an independent session, use the already verified guarded recall **4 → 5 → 4** and compare complete original state/detail again. No save is sent to slot 5. Finish on original slot 4 with every original value restored.

The command, after explicit approval, is:

```bash
source scripts/env.sh
./artifacts/Iamp.Desktop.next/Iamp.Desktop.exe \
  --save-roundtrip-preview "$AMP_BLE_ADDRESS" \
  analysis/private/desktop-save-hardware.png
```

The helper writes baseline, steps, final restoration status and failure details to `desktop-save-hardware.save-roundtrip.json`. Neither normal startup nor offline preview commands invoke it.

## Restoration and failure boundaries

The test can overwrite the stored Threshold in the original slot. Its `finally` cleanup uses a fresh session, permits only the known Threshold 25/26 profiles, restores 25 and performs the original-value save if it has not yet been attempted. It then independently verifies original saved content through recall. An ambiguous original-value save is **not repeated automatically**; any unresolved stored baseline is reported and additional mutations stop. Unknown changes to any other parameter, name, slot or global byte block automated cleanup rather than being overwritten.

No renaming, other-slot saving, other parameter edits, raw arbitrary packets, power cycle, reset, firmware update or backup restore is included. Broader error codes and power-cycle persistence remain unverified.

- [x] Exact same-slot save/restore helper implemented.
- [x] Complete firmware/state/detail/content guards and offline UI checks implemented.
- [x] Sixty protocol tests pass; local snapshot import keeps Save disabled.
- [x] Explicit authorization for the new Windows-side stored-value writes obtained.
- [x] Windows hardware test passes with original stored baseline independently restored.
- [x] Publish tested version 1.2.0 to the regular app directory.
