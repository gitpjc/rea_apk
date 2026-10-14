# Mobile-style preset browser — 1.6.0

Validated on 14 October 2026 with F15i firmware V1.1.9. Phase 6 remains excluded unless separately authorized.

## Layout update — 1.6.1

The four amplifier information cards now appear above Presets. A compact dropdown replaces the fixed-height table. Its rows show a #4385ff checkmark to the left of the active preset, followed by the bank label and device-reported name. Other entries reserve the check space for alignment. Choosing an entry recalls it through the existing guarded workflow; Escape cancels browsing. Offline actual-popup rendering and existing responsive/snapshot/history checks passed without hardware access. The 1.6.0 table-specific description below is historical evidence.

## Normal workflow

Disconnect phone iAMP and close MOOER Studio before using Windows control. Scan Bluetooth automatically connects when exactly one amplifier is found; otherwise select a discovered amplifier. A manually entered valid address enables Connect amplifier. Successful connection loads all 80 device-reported names and the current preset, and stays open until Disconnect, closure or failure.

The browser appears above the information/effect panels. Wire indices 1–80 map to banks 0–19, with four slots A–D: 1 = 0A, 4 = 0D, 5 = 1A, 80 = 19D. The compact right-aligned preset column retains the requested alignment. Names come from the amplifier, and the current row is highlighted. Search accepts names, bank labels and wire indices. Clicking a row or pressing Enter recalls it; keyboard browsing alone does not.

Current state is polled every three seconds through the retained serialized session. Refresh amplifier is available for an explicit reread. This is polling, not proof of unsolicited physical-panel notifications. If an external change conflicts with pending typed input, the app preserves that input, stops automatic refresh and disables editing until an explicit refresh. Known live unsaved edits or pending input require discard confirmation before recall, with No as the default.

Recall verifies a fresh complete snapshot, preserves opaque global-state fields in the selection command and verifies the resulting index/details and global state. Failed or ambiguous operations are never automatically replayed. Disconnect retains the displayed snapshot but removes edit authority. Opening a local snapshot releases the live connection and remains read-only until a fresh connection succeeds.

## Hardware evidence

The user disconnected phone iAMP and confirmed a quiet amplifier. The actual Windows handlers recalled each of the 79 alternatives to original **0D British Lead Solo**, returning to 0D after every individual test. Every return matched the complete original state and detail payloads. All 80 bank labels/names were loaded. No parameter edits or saves were sent.

The retained-session test changed the selected slot through the API and verified that the production timer adopted 1A without Refresh. It also verified pending-input preservation and disabled editing after an external change. A separate actual Scan test automatically connected, loaded 80 presets and showed current 0D. Final independent fresh-session readback matched every original byte, including Threshold 25. Windows connections were closed, then the user reconnected phone iAMP and reported a stable normal effects screen. This observation does not establish the cause or resolution of the earlier transfer-screen loop.

Private evidence (excluded from Git):

- `analysis/private/version-160-browser.png.roundtrip.json`: passed, 79 alternatives, baselineRestored true, no failure.
- `analysis/private/version-160-sync.png.sync.json`: automatic refresh, pending preservation and baseline restoration passed.
- `analysis/private/version-160-scan.png.scan.json`: passed, 80 presets, current 0D, connected.
- `analysis/private/version-160-final.json`: independently matches browser baseline raw state/detail.
- Original raw SHA-256: state `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5`; detail `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`.

Explicit development-only modes `--preset-browser-check`, `--connection-sync-check` and `--scan-connect-check` produce local reports/screenshots. Normal startup never runs hardware tests. Offline verification passed 137 .NET and five Node tests, zero build warnings/errors, and snapshot/history/comparison/diagnostics/layout/save-guard checks.

## Remaining boundaries

All-slot recall does not broaden parameter editing or saving. Five previously demonstrated parameter intervals remain guarded by their supported layout. Save remains original-name same-slot 0D, Threshold 25/26 and all other original fields unchanged. Arbitrary save destinations, rename, full parameter ranges, physical-panel notification coverage and broad failure/power-cycle persistence acceptance remain unfinished. No Phase 6 work was performed.
