# Local connection diagnostics

This reference remains applicable to application 1.41.2. Version labels and test counts below identify the original implementation/evidence runs; they do not claim a new test run. See [Current-Status.md](Current-Status.md) for full delivered scope and open acceptance gates.


Updated 14 October 2026; introduced in desktop version 1.3.0.

Use **Export diagnostics** in the sidebar after a connection problem, before closing the desktop app. Choose a destination for the `.iamp-diagnostics.json` file. Export does not connect to the amplifier or change presets. A new file is staged beside the destination and moved into place only after a successful write; the save dialog confirms overwriting an existing file. Export is disabled while another operation is active.

The app keeps the latest 256 events in a bounded in-memory journal. Closing the app discards them. There is no background file writing or automatic upload. Exported files are ignored by Git. If the mobile app alone exhibits the transfer-screen loop while the desktop is closed, this journal cannot observe that event; report the circumstances separately.

The JSON includes application version, export time, capacity, dropped-event count and events with sequence number, UTC timestamp, allowlisted operation/outcome and elapsed milliseconds. Categories identify connection attempts, identity/name/state/detail queries, live updates, preset recall/save and session failures. The production dispatcher and Windows connection adapter supply these events. Completion describes transport command completion, not proof of an entire musician workflow or saved-preset durability.

Only typed categories enter the journal. It accepts no arbitrary message text, raw packet bytes, Bluetooth addresses, device names, preset names/values, paths or exception details. Command categories are derived solely from an allowlisted opcode; payloads are not retained. Unknown commands are labeled OtherCommand. UTC timestamps are intentional for correlating events across captures; no local username or computer name is included.

Started/Completed events describe normal activity. Cancelled and TimedOut distinguish an interrupted active command from its deadline expiring. TransferState records the terminal 9B guard. Rejected means a command was blocked before transport initiation after session failure. Disconnected is recorded by the Windows connection callback, and Disposed records normal session cleanup as well as explicit disposal. A completed connection followed by disposal is expected because the app releases BLE after each operation. No event automatically retries a write or save.

The initial scope covers connection establishment and command execution, not Bluetooth scanning, local file operations or every validation/whole-workflow outcome. No request IDs or raw replies are included. It is a compact troubleshooting aid, not a full protocol capture or a fix for the unresolved mobile transfer-screen trigger.

Validation: nine new .NET cases cover bounded retention/drop counts, exact allowlisted fields, concurrent recording, forbidden free-form metadata, privacy with injected sensitive payload/error text, dispatcher completion/transfer/rejection and timeout classification. All 109 .NET and five Node tests pass with zero Release warnings/errors. The actual offline export handler passed with a simulated writer; injected preset content and UI Bluetooth address were absent, application version and transfer event were present, and snapshot/pending state was preserved. Navigation and local snapshot regression checks passed. The sidebar layout was visually inspected at 1280×900.

Offline QA command, using the already-built package:

```bash
artifacts/Iamp.Desktop/Iamp.Desktop.exe --diagnostics-preview analysis/private/diagnostics-preview.png
```

This writes a PNG and an adjacent `.iamp-diagnostics.json` using simulated events only. Default startup does not inject those events. Evidence for this release is in `analysis/private/version-130-diagnostics.png` and its adjacent JSON. Physical Windows connection instrumentation remains untested in this increment; no phone or amplifier commands were sent.
