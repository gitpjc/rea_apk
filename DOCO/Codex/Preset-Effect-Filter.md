# Preset effect filter — 1.22.0

15 October 2026. The Effect dropdown combines with the existing name/bank/index search. All effects clears only the effect choice; search × clears only text. Disabled effects count as present: matching uses category/type and model identity, never the enable flag or a fixed chain position. Duplicate display names are disambiguated by category. All 124 captured built-in models are listed, including models that the desktop does not edit. Filtering never enables new hardware writes.

## Catalog evidence

The complete built-in catalog is in assets/effect_catalog.json and embedded in Iamp.Protocol, so published filtering does not depend on external downloads or a Python runtime. It includes Dyna 4, Filter 3, OD 22, Amp 55, EQ 2, Mod 17, Delay 12 and Reverb 9. Labels/ordinals come from authorized iAMP category browsing and list scrolling, including retained first-page captures. Every label is also corroborated by a literal in the extracted installed-phone APK libapp.so. Categories use their observed record types; model keys use category-local zero-based ordinals, corroborated by retained runtime pairs and indexed preset records. This read-only catalog does not establish editing support/defaults/ranges for every model. Shimmer remains available as a search choice even though its phone selection row is greyed out. The inspected GIR/IR and GNR user-library lists are Empty; these empty sample slots are not built-in effect models.

The remaining Filter category was verified with a temporary mobile Noise Killer → Touch Wah → Noise Killer replacement. The captured replacement request identifies Touch Wah as type 5/model 0 (not type 3). Reselecting Noise Killer reset Threshold to its default 15. Fresh Windows comparison identified that as the sole difference, and the already verified guarded Threshold API restored the original 25. Every original state/detail byte then matched. Nothing was saved. Private proof: analysis/private/filter-complete-restoration.json. Full labels/evidence digests: analysis/private/effect-catalog-evidence.json. Raw screenshots/logs/device identifiers remain ignored.

## Read-only indexing

The existing 0x21/index → 0x22/index detail query was first exercised on a non-current slot, then all 80 slots. Fresh current state/detail comparisons after each ten reads and at the end confirmed no recall or edit. ReadPresetEffectsAsync runs as one serialized V1.1.9 workflow, verifies complete expected current state/detail before and after, validates every requested slot/layout and returns only a complete result. No partial index is committed after cancellation/failure. This is a read snapshot, not an atomic transaction across all stored presets.

Desktop Connect/Refresh loads the complete index with 1/80…80/80 progress and existing Cancel/timeout behavior. Changing filters uses only memory. Retained data remains searchable when disconnected; opening a local snapshot replaces the index with that single preset. Unsupported firmware has no effect index and resets the effect choice to All effects, preventing a disabled control from stranding a hidden filter. Confirmed live-preset updates refresh that preset's indexed chain. Refresh rereads all stored chains.

## Validation

29 affected protocol cases pass: PresetEffectSearchTests, CurrentPresetReadGuardTests and WorkflowQueueTests. They cover disabled/enabled inclusion, exact category/model matching, absent/truncated records, all 124 catalog keys/counts, text/bank/index matching and invalid/duplicate slot rejection, alongside read/workflow guard regressions. The full suite was not rerun.

The offline WPF check (tools/Iamp.Desktop.UiChecks) exercises the actual dropdown/search handlers: 125 choices including All effects, disabled-effect results, combined name/bank search, empty counts, independent clears, active marker, busy/empty-index states and no device work. Header/control geometry passes at 1366×768, 911×512, 683×384, 620×320 and 1920×1080, along with the affected slider/pending-state layout checks. No desktop screenshots were taken.

Actual Windows ReadAsync loads all 80 chains. Each of the 124 dropdown choices is checked against independently captured preset records, alone and combined with text search; All effects returns all 80. The capture contains a disabled effect, which remains included. A separate fresh BLE connection verifies every original state/detail byte unchanged. Private report: analysis/private/effect-filter-windows-report.json, passed=true, baselineRestored=true, 124 checked choices, failure=null.

This is preset filtering only. No new model editing, sample transfer, saving or Phase 6 functionality is added.

## Cache follow-up — 1.23.0

The initial always-read connection behavior above is superseded by `Effect-Cache.md`: library effects are cached per device/firmware and reused across restarts/reconnect/normal refresh. Only a first cache miss or explicit Refresh effect names rereads all 80 chains. Reset selects All effects, is disabled there and preserves text search. The current preset remains live.
