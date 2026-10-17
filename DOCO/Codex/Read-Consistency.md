# Current-preset read consistency

This reference remains applicable to application 1.41.2. Version labels and test counts below identify the original implementation/evidence runs; they do not claim a new test run. See [Current-Status.md](Current-Status.md) for full delivered scope and open acceptance gates. Current mutation workflows include general save/rename: a failed terminal session is disposed, one read-only recovery may reload actual state, human confirmation time is excluded from the device deadline, and saves are never replayed. See [Preset-Save-Rename.md](Preset-Save-Rename.md).


Updated 14 October 2026; implemented in version 1.2.3.

The production read sequence remains: query current state (`11` → `12`), query the selected preset's details (`21 index` → `22 index`), then query current state again. Previously, only the selected index was compared between state replies. A global setting could therefore change while the preset index stayed the same, allowing an inconsistent snapshot to be accepted.

`CurrentPresetReadGuard` now validates the observed 92-byte state structure, opcode and index before requesting details. After the final state query, it parses details, checks their index and requires byte-for-byte equality of both complete state responses. Unknown state fields remain opaque and are compared without interpretation. A disagreement throws an error requiring a fresh read. No automatic retry or additional device command was added.

`F15ReadSession.ReadCurrentPresetAsync` uses this guard for every caller, including reads before edits, saves and recall verification. The desktop already disables editing when a fresh read begins and only enables it after a successful complete read. An unsuccessful read therefore cannot enable editing from the rejected snapshot. Existing error handling also disables edits after an unconfirmed mutation.

Eleven regression test cases use independently captured state/detail frames. They accept the unchanged capture, reject changes at every state offset after the opcode/index, reject preset switches and mismatched details, reject short/long states and invalid discriminators/indices, and preserve unknown stable fields and effect types. Combined validation passed: 82 .NET tests, five Node tests, Release build with zero warnings/errors and self-contained package/version/history checks.

This is a consistency check, not an atomic snapshot guarantee. An effect-only change that is not reflected in whole-state replies, or a setting changed and changed back between queries, can still escape detection. No protocol transaction identifier or revision counter has been established. General external-change reconciliation, cancellation/disconnect simulation, stale reply correlation and the mobile transfer-screen cause remain open tasks. No hardware comparison, parameter edit, recall or save was performed for this increment.

## Physical-switch race — 1.41.4

A hardware monitor captured successfully completed state/detail/state requests whose states differed during physical preset selection. CurrentPresetChangedException now identifies this condition; the full read restarts at most twice (three attempts total), with 100 ms settling, inside the serialized workflow. No partial snapshot is adopted. Transport/format/transfer/cancellation errors are not retried; mutation preconditions remain fresh comparisons and no mutation/save is replayed. Exhaustion remains terminal with specific desktop guidance. See [Physical-Panel-Sync.md](Physical-Panel-Sync.md) for tests, exact restoration and completed normal published-desktop UI acceptance.
