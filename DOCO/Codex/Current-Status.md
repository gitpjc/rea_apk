# Current development status

## Next task

Resume **MOOER F4 Wireless Footswitch support on 18 October 2026**, as agreed with the user on 17 October. Establish how mobile iAMP connects/configures the user's F4, then implement supported desktop parity and verify preset/effect synchronization. Do not assume direct Windows pairing. Only the footswitch portion of P6.5 is authorized; factory tone-library and other Phase 6 work remain deferred. No F4 testing or implementation has started.

The user reports successful installation and uninstall, including Start menu shortcuts. Further upgrade/license/clean-PC checks are outside the current requested scope and are not claimed as tested.

Reconciled 17 October 2026. Installer package: **1.41.8**; the usual desktop launch folder remains **1.41.5**. Windows x64, tested with F15i firmware **V1.1.9** and Android iAMP **1.6.1**. This page summarizes implemented behavior; [Plan.md](Plan.md) tracks tasks and [Feature-Matrix.md](Feature-Matrix.md) separates implementation from acceptance evidence.

## Implemented

| Area | Current behavior | Details |
| --- | --- | --- |
| Connection | Windows Bluetooth LE discovery, Connect/Disconnect/Refresh, retained session and approximately three-second current-state polling. One controller at a time. | [Preset-Browser.md](Preset-Browser.md) |
| Presets | All 80 bank labels/names, 0A–19D; recall with unsaved-discard protection; text and effect filtering, including disabled effects; persistent effect index with manual refresh/reset. | [Preset-Effect-Filter.md](Preset-Effect-Filter.md), [Effect-Cache.md](Effect-Cache.md) |
| Effects | Add/remove/reorder, insertion marker, in-place model replacement and repeated categories/models. All 124 requested built-in models have verified parameter/on-off controls and ordered JSON. | [Effect-Chain-Editing.md](Effect-Chain-Editing.md) |
| Saving | Same-slot save/rename and Save As to all 80 destinations; 1–20 printable ASCII characters; fresh source/destination checks, themed Save/Cancel confirmation, single save and complete readback. Both dialogs say “Current amplifier settings”. | [Preset-Save-Rename.md](Preset-Save-Rename.md) |
| Save failures | Discard the failed session and, where appropriate, attempt one read-only reconnect. Show the outcome beside Save; never replay a save. Transfer-state changes or unsuccessful recovery require an explicit fresh connection. | [Preset-Save-Rename.md](Preset-Save-Rename.md) |
| Mixer | Bluetooth, USB REC, Instrument and Master levels/mutes, including adjustment while muted. Physical MASTER0→10→0 leaves both mobile and desktop Mixer Master at+6dB in the observed test; desktop parity is confirmed. The physical knob mapping remains unestablished. | [Mixer-Editing.md](Mixer-Editing.md) |
| Information | Firmware/capacity cards and battery percentage/read time on Amplifier Information. Charging-state semantics remain opaque. | [Battery-Information.md](Battery-Information.md) |
| Local tools | Preset JSON view/copy/native Save As; versioned snapshot export/open/comparison for inspection; local redacted diagnostics; Version History. | [Preset-JSON.md](Preset-JSON.md), [Snapshot-Comparison.md](Snapshot-Comparison.md), [Diagnostics.md](Diagnostics.md) |

| Effect category | Verified built-in models | Specification and milestone evidence |
| --- | --- | --- |
| Dyna | 4 | [Dyna-Model-Editing.md](Dyna-Model-Editing.md) |
| Filter | 3 | [Filter-Model-Editing.md](Filter-Model-Editing.md) |
| Preset EQ | 2 | [EQ-Model-Editing.md](EQ-Model-Editing.md) |
| Amp | 55 | [Amp-Model-Editing.md](Amp-Model-Editing.md) |
| OD | 22 | [OD-Model-Editing.md](OD-Model-Editing.md) |
| Delay | 12 | [Delay-Model-Editing.md](Delay-Model-Editing.md) |
| Reverb | 9 | [Reverb-Model-Editing.md](Reverb-Model-Editing.md) |
| Mod | 17 | [Mod-Model-Editing.md](Mod-Model-Editing.md) |

There are **126 shared effect parameter types**. The readable preset-effects JSON schema is **3.0.0**; the separate raw snapshot archive and search-only effect cache each use schema **1**. These format versions are independent of application version 1.41.8. Opening a snapshot does not apply it to the amplifier. Mixer is not included as effect settings in the readable preset JSON.

## Verified milestones and remaining acceptance

All 79 alternative preset recalls, per-model parameter/on-off/moved-position tests, representative chain operations and four-channel Mixer checks have recorded restoration evidence. General save/rename/save-as and a saved edited copy surviving a physical amplifier power cycle passed. **P5.8 is complete:** clean/rhythm/lead workflows exercised both actual desktop save dialogs, reconnect/readback, scrolled mobile Amp comparison and user-confirmed amplifier bank/name displays. Every original saved preset, active preset and global setting was restored after each case. See [Musician-Workflow.md](Musician-Workflow.md).

Physical-panel testing on 17 October verified an initial 0D→0C→0D round trip and pending-input protection, with exact independent original restoration after each case. A later physical return reported connection loss. A subsequent production-reader monitor captured a complete-read consistency race; 1.41.4 adds at most three read-only attempts. The fixed reader’s physical round trip and exact restoration pass, and the normal published-desktop UI check subsequently passed: both fields followed the physical round trip without disconnect. P3.4 is complete for this bounded setup. Version 1.41.3 fixes a separate stale closed-dropdown selection after external changes/refresh. See [Physical-Panel-Sync.md](Physical-Panel-Sync.md).

This is not final product acceptance. Remaining work includes physical parameter/global-control-originated changes, the broader connection fault matrix and unresolved historical mobile transfer-loop trigger, exhaustive chain limits/rejections, actual DPI/Narrator/performance checks, installer/signing/license review and clean-PC operation. P5.8 used a host built from production code; it does not establish clean-machine acceptance of the distributed executable. Test counts in milestone reports describe those recorded runs, not a newly run complete suite.

**GIR&IR is excluded by the user. Phase 6 is deferred except for the explicitly authorized next F4 footswitch task above.** USB charging/vendor maintenance does not establish desktop USB control. Vendor-format device import/restore, cloud/accounts, tuner/drums/looper and other advanced operations are not implemented. The user reported a successful MOOER Studio restore; no independent custom backup-format or exhaustive vendor-restore validation is claimed.

The executable/title-bar icon now uses the supplied mobile APK’s launcher artwork; [icon provenance](../../assets/app/README.md) records the source and Windows sizes. The Inno Setup script and compilation instructions are available in [installers](../../installers/README.md). The 1.41.8 installer compiles; installation/upgrade/uninstall, user-data preservation, signing and clean-PC acceptance remain open.

## Development and testing policy

- Ask for user help/approval directly in Codex. Explain and ask here before initiating Windows UI automation that would require a new app-permission window; use non-UI or user-operated checks when access is unavailable.
- Keep only one desktop instance running during tests, and only one phone/Windows controller connected to the amplifier.
- Record a fresh baseline before hardware tests; restore every changed saved/live value and the original selected preset after each test, then independently verify complete state/detail. Preserve original unsaved values without silently saving them.
- Use authorized ADB access and iAMP-only phone captures; reject lock screens/foreign overlays. Keep USB stay-awake enabled across the ongoing testing session and restore its retained original value when the session ends or the user requests it. Do not request mobile-screen stability checks unless asked.
- The user deferred remaining Bluetooth-interruption/Windows-sleep reliability tests on17October2026. Keep unexecuted cases documented and run them only when requested or a related problem appears.
- Run affected checks for routine changes. Use the complete build/test script only when a full release check is requested or justified. Documentation-only work does not change the application version, but is recorded in version history.
- GitHub workflow automation is disabled at the user's request; do not re-enable it. REA, APKs, ADB, Python and capture tooling are development tools, not runtime requirements.

See [Documentation-Index.md](Documentation-Index.md) for current guides and explicitly historical evidence. Re-read the current baseline before another hardware experiment; a historical report is not an instruction to replay its commands or reuse its saved settings.
