# Shared command lifecycle

This reference remains applicable to application 1.41.2. Version labels and test counts below identify the original implementation/evidence runs; they do not claim a new test run. See [Current-Status.md](Current-Status.md) for full delivered scope and open acceptance gates. Current mutation workflows include general save/rename: a failed terminal session is disposed, one read-only recovery may reload actual state, human confirmation time is excluded from the device deadline, and saves are never replayed. See [Preset-Save-Rename.md](Preset-Save-Rename.md).


Updated 14 October 2026; implemented in version 1.2.4.

Follow-up: version 1.5.2 adds per-session serialization of complete public workflows around this command dispatcher. See `Workflow-Serialization.md` for concurrency tests and read-only hardware acceptance. Later diagnostics and normal hardware read/handoff results are in `Diagnostics.md` and `Read-Only-Regression.md`; original scope/results below are historical.

`Iamp.Protocol.CommandSession` is now the shared dispatcher used by the real Windows `F15ReadSession`. Both response-bearing queries and write-only live updates pass through it. The Windows adapter retains GATT discovery, subscription, frame encoding and ATT Write With Response. No command bytes or supported hardware operations changed.

Each session has a serialized command queue, one active response predicate and a 15-second deadline after queue acquisition. Request registration and transport initiation share the notification gate. Complete notification batches pass through the existing transfer-status guard before a matching reply is delivered. A reply received while idle is discarded. A matching reply is insufficient on its own: the ATT write must also succeed and the session must still be valid.

Disconnect, active cancellation, timeout, disposal, write failure or a valid 9B status permanently stops that session. Active waits are interrupted and queued/future commands fail before transport initiation. Late replies and later status values cannot re-enable it. A new connection creates a new dispatcher and decoder. Cancellation while a request is still queued affects only that request; it does not invalidate a different active command.

Timeouts also bound a writer that does not promptly honor cancellation. Its late failure is observed without retrying the command. Underlying native cancellation is best effort: an already-issued write may still reach the device, so a failed mutation must still be treated as ambiguous. The application does not undo, repeat or save automatically in response to such an error.

`CommandSessionTests` uses an in-memory transport attached to this production component. Eighteen new cases exercise serialization/correlation, fragmented and corrupted frames, disconnect during a blocked write, disconnect after an early reply, queued/active/pre-cancelled requests, response and write timeouts, disposal, coalesced transfer statuses, failed writes with early replies, write-only status failure, new-session isolation, idle replies, early reply ordering and late native failure. Synchronization uses explicit write events; timeout cases use short deadlines with a five-second test bound. No amplifier or phone is involved.

Validation: all 100 .NET and five Node tests pass; Release build and self-contained publication have zero warnings/errors. Offline Version History and local import/export guards pass, including preserved navigation state and disabled hardware writes after opening a local snapshot. Version/history consistency checks pass. The published executable and Desktop/Transport/Protocol DLLs match tested staging byte for byte.

Remaining boundaries: the wire protocol has no established request identifier, so a delayed reply with the same opcode and matching content arriving during a later healthy request cannot be conclusively distinguished. Whole multi-command workflows still require caller serialization, as before. This dispatcher does not implement slider coalescing, diagnostic logging or general replay/simulation of an amplifier's behavior. Physical Windows disconnect, sleep/resume, Bluetooth-toggle and mobile handoff acceptance remain open. This increment did not access the phone or amp and does not explain the mobile transfer-screen incident.
