# Documentation index

Reconciled 18 October 2026 against application **1.44.0**. Start with [Current-Status.md](Current-Status.md), then [Plan.md](Plan.md) for checked tasks and remaining acceptance. Current behavior takes precedence over earlier milestone limits. Dates/version numbers, captured values and test counts in reports identify the original evidence, not newly rerun tests. The original documentation reconciliation performed no hardware work. Subsequent physical-panel checks and the 1.41.3 dropdown correction are recorded separately below.

## Current guides and development status

- [Drum.md](Drum.md) — 1.44.0 style/pattern selector, compact Tools navigation, mobile observations, desktop controls and exact restoration evidence.

- [Tuner.md](Tuner.md) — 1.42.7 tuner correction, observed mobile mappings/colors, user-confirmed center agreement and exact original restoration.

- [Installer instructions](../../installers/README.md) — Inno Setup compilation and lifecycle validation boundary for 1.44.0; user installation/uninstall acceptance remains recorded at 1.41.8.
- [Current-Status.md](Current-Status.md) — Current development status
- [Plan.md](Plan.md) — Windows iAMP companion: implementation plan
- [Feature-Matrix.md](Feature-Matrix.md) — Windows iAMP feature and acceptance matrix
- [Progress.md](Progress.md) — Implementation progress
- [Decisions.md](Decisions.md) — Implementation decisions and unresolved evidence
- [Environment.md](Environment.md) — Development environment
- [Build-and-Release.md](Build-and-Release.md) — Windows build, focused checks and release packaging
- [User-Guide.md](User-Guide.md) — Running the current iAMP Desktop build
- [Musician-Workflow.md](Musician-Workflow.md) — End-to-end musician workflow acceptance
- [Preset-Save-Rename.md](Preset-Save-Rename.md) — Save, rename and save a copy
- [Preset-JSON.md](Preset-JSON.md) — Current preset JSON — application 1.41.2, schema 3.0.0
- [Effect-Cache.md](Effect-Cache.md) — Effect cache, explicit refresh and reset — 1.23.0
- [ADB-Testing.md](ADB-Testing.md) — Authorized Android iAMP testing
- [Backup-Baseline.md](Backup-Baseline.md) — F15i baseline backup and recovery route

- [Physical-Panel-Sync.md](Physical-Panel-Sync.md) — physical preset/conflict results, exact restoration, captured read race, 1.41.4 bounded retry, completed normal desktop acceptance, physical Master readback boundary/mobile parity and 1.41.3 dropdown regression.

## Current technical references with dated evidence

- [Battery-Information.md](Battery-Information.md) — Read-only battery information — 1.9.0
- [Diagnostics.md](Diagnostics.md) — Local connection diagnostics
- [Snapshot-Comparison.md](Snapshot-Comparison.md) — Read-only snapshot comparison
- [Mixer-Editing.md](Mixer-Editing.md) — Mixer
- [Read-Consistency.md](Read-Consistency.md) — Current-preset read consistency
- [Workflow-Serialization.md](Workflow-Serialization.md) — Complete workflow serialization
- [Command-Lifecycle.md](Command-Lifecycle.md) — Shared command lifecycle
- [Protocol.md](Protocol.md) — F15i Li protocol evidence ledger

## Complete model specifications and implementation milestones

These settings/order/range tables remain the specifications. Introductory milestone versions and “next” statements record chronology; the full requested set is now implemented.

- [Dyna-Model-Editing.md](Dyna-Model-Editing.md) — Dyna model editing — 1.28.0
- [Filter-Model-Editing.md](Filter-Model-Editing.md) — Filter model editing — 1.29.0
- [EQ-Model-Editing.md](EQ-Model-Editing.md) — Preset EQ editing — 1.30.0
- [Amp-Model-Editing.md](Amp-Model-Editing.md) — Amp model editing — 1.35.0
- [OD-Model-Editing.md](OD-Model-Editing.md) — OD model editing — 1.36.0
- [Delay-Model-Editing.md](Delay-Model-Editing.md) — Delay model editing — 1.37.0
- [Reverb-Model-Editing.md](Reverb-Model-Editing.md) — Reverb model editing — 1.38.0
- [Mod-Model-Editing.md](Mod-Model-Editing.md) — Mod model editing — 1.39.0

## Historical discovery, bounded proofs and UI milestones

Each document has a current scope note. Preserve the original observations, hashes and procedures; do not reuse their fixed presets, baselines, unexecuted checklists or earlier restrictions as current instructions. Representative tests do not establish every combination/fault case.

- [808-Tone-Editing.md](808-Tone-Editing.md) — 808 Tone controlled mapping and restoration
- [Amp-Bass-Editing.md](Amp-Bass-Editing.md) — PLX 100 Bass editing — 1.10.0 acceptance
- [Amp-Gain-Master.md](Amp-Gain-Master.md) — Bounded PLX 100 Gain and Master — 1.13.0
- [Amp-Mid-Editing.md](Amp-Mid-Editing.md) — PLX 100 Mid editing — 1.11.0
- [Amp-Tone-Controls.md](Amp-Tone-Controls.md) — Bounded PLX 100 tone controls — 1.12.0
- [Analog-Delay-Ranges.md](Analog-Delay-Ranges.md) — Analog Delay observed ranges — 1.20.0
- [Delay-Editing.md](Delay-Editing.md) — Bounded Analog Delay editing — 1.14.0
- [Desktop-Usability.md](Desktop-Usability.md) — Compact layout and keyboard navigation
- [Effect-Bypass.md](Effect-Bypass.md) — Effect enabled/bypassed state — 1.7.0
- [Effect-Catalog.md](Effect-Catalog.md) — Effect catalog investigation
- [Effect-Chain-Editing.md](Effect-Chain-Editing.md) — Effect-chain editing — 1.25.0
- [Expanded-Controls.md](Expanded-Controls.md) — Controlled mappings: Noise Killer Level, 808 Gain and Vol
- [Hall-Reverb-Ranges.md](Hall-Reverb-Ranges.md) — Hall Reverb observed ranges — 1.19.0
- [Mobile-Style-Sliders.md](Mobile-Style-Sliders.md) — Mobile-style effect sliders — 1.24.0
- [Mobile-Transfer-Loop.md](Mobile-Transfer-Loop.md) — Mobile transfer-screen loop: recovered, cause unresolved
- [Model-Browser.md](Model-Browser.md) — Read-only model browser investigation
- [Native-Findings.md](Native-Findings.md) — Vendor utility native protocol findings
- [Noise-Killer-Ranges.md](Noise-Killer-Ranges.md) — Noise Killer Threshold and Level 0–100 — 1.16.0
- [OD808-Ranges.md](OD808-Ranges.md) — OD808 Gain, Tone and Volume 0–100 — 1.17.0
- [PLX100-Ranges.md](PLX100-Ranges.md) — PLX 100 six-control 0–100 ranges — 1.18.0
- [Parameter-Editing.md](Parameter-Editing.md) — Parameter-editing evidence: Noise Killer threshold
- [Parameter-Sliders.md](Parameter-Sliders.md) — Parameter sliders — 1.21.0
- [Pending-Parameters.md](Pending-Parameters.md) — Pending parameter inputs — 1.21.1
- [Physical-Disconnect-Test.md](Physical-Disconnect-Test.md) — Physical power-off and reconnect check
- [Preset-Browser.md](Preset-Browser.md) — Mobile-style preset browser — 1.6.0
- [Preset-Effect-Filter.md](Preset-Effect-Filter.md) — Preset effect filter — 1.22.0
- [Preset-Format.md](Preset-Format.md) — Read-only current-state and preset format
- [Preset-Persistence-Test.md](Preset-Persistence-Test.md) — Completed edited-value phone save, recall and restoration test
- [Preset-Save-Test.md](Preset-Save-Test.md) — Proposed unchanged same-slot preset save test
- [Preset-Saving.md](Preset-Saving.md) — Observed phone same-slot save and edited-value recall retention
- [Preset-Selection.md](Preset-Selection.md) — Preset-selection evidence
- [Read-Only-Handoff.md](Read-Only-Handoff.md) — Read-only Windows-to-phone handoff checks
- [Read-Only-Regression.md](Read-Only-Regression.md) — Current Windows read-only regression
- [Reverb-Editing.md](Reverb-Editing.md) — Bounded Hall Reverb editing — 1.15.0
- [Runtime-Effect-Mappings.md](Runtime-Effect-Mappings.md) — Runtime effect labels and bounded field correspondences
- [Vendor-Utility.md](Vendor-Utility.md) — Installed MOOER Studio: static inventory
- [Windows-Save-Test.md](Windows-Save-Test.md) — Windows same-slot save test passed

## User instructions and source/provenance records

- [AGENTS.md](../../AGENTS.md) — user project instructions; preserved.
- [Initial_Prompt.md](Initial_Prompt.md) — original user request; preserved as source context.
- [Versioning.md](../Versioning.md) — user versioning instructions; preserved.
- [README.md](../../README.md) — project entry point and labelled original research notes.
- [Application icon provenance](../../assets/app/README.md) — exact APK launcher source, hashes, Windows frames and regeneration.
- [Effect icon provenance](../../assets/effects/README.md) — source members and hashes; licensing approval remains a separate release gate.
- [Artifact inventory](Artifact-Inventory.json) — recorded artifact metadata, not an assertion of current live device state.
- [Application version history](../../assets/version_history.json) — application changes and documentation audits; independent of readable JSON schema 3.0.0 and snapshot/cache schema 1.

The vendor manuals under DOCO/MOOER are source material, not generated development status. GIR&IR is excluded by the user. Phase 6 remains deferred unless explicitly authorized.
