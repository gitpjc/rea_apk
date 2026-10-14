# Current-preset read consistency

Updated 14 October 2026; implemented in version 1.2.3.

The production read sequence remains: query current state (`11` → `12`), query the selected preset's details (`21 index` → `22 index`), then query current state again. Previously, only the selected index was compared between state replies. A global setting could therefore change while the preset index stayed the same, allowing an inconsistent snapshot to be accepted.

`CurrentPresetReadGuard` now validates the observed 92-byte state structure, opcode and index before requesting details. After the final state query, it parses details, checks their index and requires byte-for-byte equality of both complete state responses. Unknown state fields remain opaque and are compared without interpretation. A disagreement throws an error requiring a fresh read. No automatic retry or additional device command was added.

`F15ReadSession.ReadCurrentPresetAsync` uses this guard for every caller, including reads before edits, saves and recall verification. The desktop already disables editing when a fresh read begins and only enables it after a successful complete read. An unsuccessful read therefore cannot enable editing from the rejected snapshot. Existing error handling also disables edits after an unconfirmed mutation.

Eleven regression test cases use independently captured state/detail frames. They accept the unchanged capture, reject changes at every state offset after the opcode/index, reject preset switches and mismatched details, reject short/long states and invalid discriminators/indices, and preserve unknown stable fields and effect types. Combined validation passed: 82 .NET tests, five Node tests, Release build with zero warnings/errors and self-contained package/version/history checks.

This is a consistency check, not an atomic snapshot guarantee. An effect-only change that is not reflected in whole-state replies, or a setting changed and changed back between queries, can still escape detection. No protocol transaction identifier or revision counter has been established. General external-change reconciliation, cancellation/disconnect simulation, stale reply correlation and the mobile transfer-screen cause remain open tasks. No hardware comparison, parameter edit, recall or save was performed for this increment.
