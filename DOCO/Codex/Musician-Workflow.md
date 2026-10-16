# End-to-end musician workflow acceptance

Completed 17 October 2026 against F15i firmware V1.1.9 and Android iAMP 1.6.1, using desktop 1.41.2 production controls and handlers in a private WPF test host. This is a functional control/persistence check at quiet output with no instrument or playback, not an audio listening evaluation.

| Workflow | Source | Amp editor / Bass change | Temporary saved destination | Desktop reconnect | Mobile and amplifier display | Restoration |
| --- | --- | --- | --- | --- | --- | --- |
| Clean | 0A · British J800 Clean | J800, position 1; 47→46 | 19A · QA Clean | Pass | Pass | Pass |
| Rhythm | 0C · British Rhythm | PLX 100, position 2; 47→46 | 19B · QA Rhythm | Pass | Pass | Pass |
| Lead | 0D · British Lead Solo | PLX 100, position 3; 35→34 | 19C · QA Lead | Pass | Pass | Pass |

## Procedure and verification

1. Disconnect the existing desktop controller, record a fresh complete current state and all 80 preset records without mutations, and retain the starting selected preset. The user's current 19D name/settings and custom clean/rhythm chains were preserved; no older development baseline was substituted.
2. Preflight each scratch destination against exact previously proven 65 US DLX default records. Distinguish original active live values from its stored record through a guarded recall and inactive read; they matched in this session.
3. Use the production desktop Read, preset selection, Bass input/Apply and Save handlers. Exercise the actual Save or rename preset dialog and themed overwrite confirmation, including their Save actions; do not bypass either with the confirmed-test flag. The selected Amp positions 1/2/3 verify positional targeting across the three different source chains.
4. Check exact saved state/detail bytes, immediate destination name, cleared unsaved marker, usable Save button and unchanged stored source. Perform a real desktop reconnect and verify the complete saved copy again. Retain readable JSON from that confirmed snapshot.
5. Release Windows Bluetooth, open mobile iAMP with guarded ADB access and compare the saved bank/name, model and all six Amp settings. Scroll to see Master fully. The user separately confirmed the physical amplifier's bank/name for each temporary copy; no panel controls were changed.
6. Close phone iAMP before Windows cleanup. Rebuild each owned scratch slot to its exact original default chain, compare the entire record before one restorative save, and verify its original name/content. Restore the original active preset and global state after each case. Compare all 80 original records and obtain an independent fresh-session full current read before the next case.

All three cases pass every stage. The complete original active state, all saved presets and global settings are restored. No save was automatically replayed. An independent cleanup watchdog was present during phone/display comparison. No production application code changed and no complete software-suite rerun was needed for this hardware acceptance work.

## Evidence and limits

Private baseline, original-storage distinction, three checkpoints/results, readable JSON exports, comparison records, phone screenshots and their hashes are retained under analysis/private/musician-*. The combined musician-acceptance-result.json validates each desktop stage, six mobile Amp values, user display confirmation and restoration. Device identifiers and raw user data remain private.

The test host compiled the current production 1.41.2 code; it was not a clean-PC installation test of the distributed executable. The published and test assemblies carry different Git revision metadata because the source was committed after publishing. This check does not close P7.8.

The original idle desktop window was initially left open but disconnected. The user closed both windows during the clean case; its cleanup still completed. At the user's request, rhythm and lead used only one desktop instance at a time, and it closed after restoration. Keep one desktop instance during future tests.

P5.8 is complete for these representative clean/rhythm/lead control workflows. Physical-panel-originated changes, exhaustive chain limits/rejections, the complete connection fault matrix, actual DPI/Narrator/performance, installer/signing and clean-machine acceptance remain separate open tasks. Previous physical power-cycle persistence evidence is retained in [Preset-Save-Rename.md](Preset-Save-Rename.md); no additional power cycle was performed here. Phase 6 remains deferred and GIR&IR excluded.
