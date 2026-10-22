# MOOER F4 footswitch

Desktop support is scoped to F15i V1.1.9 and mobile iAMP 1.6.1/code 43. The F4 connects to the amplifier. Windows is not its receiver.

## Desktop behavior

Choose **Tools → Footswitch**, below Drum. The shared amplifier header remains visible. The page distinguishes amplifier disconnected, waiting for footswitch status, F4 not connected and F4 connected. A disconnected F4 does not prove that its saved pairing has been deleted. Status comes from amplifier notifications; command acknowledgement alone never establishes a connection.

**Pair F4…** opens an app-styled confirmation/instruction dialog. Hold A+C together on the F4 for about three seconds until its LEDs flash, then choose **Start pairing**. The amplifier starts pairing. Progress remains in the dialog while it waits for the actual connected notification. Check that the footswitch displays a preset number; the user's physical display remains an important confirmation.

**Forget F4…** requires explicit confirmation that footswitch control will stop and re-pairing will be required. It does not recall, edit or save a preset. Pairing actions are unavailable without a supported amplifier session or during conflicting operations.

The dialog cannot close during an unacknowledged command. Once acknowledged, **Stop waiting** stops only the desktop wait; it does not invent a hardware cancellation or unpair command. A 30-second desktop wait limit is a UI policy, not a measured amplifier pairing timeout. Unconfirmed transport failures require reconnection and are never replayed automatically.

The existing stable preset polling already follows F4 patch selection. The user confirmed the behavior in both desktop and mobile apps before this feature was implemented. Browsing a bank on the F4 takes effect when A/B/C/D is selected. This feature does not introduce a second F4-to-Windows connection.

## Mobile evidence

On 22 October 2026, guarded captures of the actual foreground mobile iAMP app showed its connected wireless icon, disconnect confirmation, unpaired icon and reconnect. The user explicitly authorized a controlled unpair/re-pair cycle and confirmed restoration to a preset number. Holding A+C after the first quick reconnection put the F4 back into pairing mode (two dots); another explicit amplifier pairing request restored it. This demonstrated why ACK alone is insufficient and why the UI should guide the user to prepare the footswitch before starting amplifier pairing.

The user also powered the F4 off and back on without deleting its pairing. The amplifier reported `97 00` while disconnected and `97 01` on reconnection. Duplicate notifications occur.

Captured commands:

| Operation | Request payload | Acknowledgement | Actual connection notification |
|---|---|---|---|
| Pair | `95 01` | `96 01` | `97 01` when connected |
| Forget | `95 00` | `96 01` | `97 00` when disconnected |

Forgetting produced status notifications before its ACK; pairing produced them after its ACK. Both orders must work. Strict parsers accept only the observed lengths/opcodes/values. Unknown or malformed status messages do not alter the last valid observation.

`tests/fixtures/f15-footswitch-observations.json` retains sanitized complete frames and source record numbers. Full phone Bluetooth dumps and mobile screenshots remain private. Static native strings from both retained code42/code43 Flutter binaries corroborate the pairing/deletion instructions but are not treated as executable protocol proof. No F15i-specific button-assignment/configuration interface was verified; generic shared-app strings about other products are not sufficient to implement one. External F4 tuner/drum mode integration and desktop looper control are not claimed by this pairing/status delivery.

REA identity evidence: supplied APK `ev_55738d815a459b61ea50a5deb2831608be939d4074d60d86a919199ba4afc632` (SHA-256 `7ea003baf4dcb5073174c4f253a25deeb460dff2cdec379ae0ece7c88b931273`, version 1.6.1/code42); installed mobile identity `ev_7badaba40fda276f452644603e2776cae5a15e7e1ce84cd14009984d0e5d1d6e` (code43). Bounded Java-class inventory `ev_397bc5f72e297012dbaaa485e33bca3b6c094362954a562421297b77e75d2fdb` locates the mobile shell/plugins; it does not recover native Dart behavior. The actual pairing wire evidence comes from the preserved Bluetooth capture and guarded mobile actions.

## Verification

Focused protocol checks cover captured commands, ACK/status ordering, strict rejection, duplicate notifications, consumer isolation and absence of automatic cleanup/retry writes. Offline WPF checks cover Tools navigation, status distinctions, disconnected guards, styled confirmations, safe default cancellation and compact layouts. Existing preset synchronization and stable read behavior are retained.

The F4 diagram shows A/B/C/D preset assignments and names, and a blue highlight for the active patch. Click a switch to recall its preset using the same verified workflow and unsaved-change confirmation as Presets. Previous bank and Next bank browse banks 0–19; browsing changes no amplifier settings until a switch is chosen. Selected bank distinguishes browsing from the current bank, while the active patch remains labelled below. Normal unchanged polling preserves browsing; a new actual patch synchronizes the bank. Presets search/effect filters do not restrict these switches. Retained offline information says Last read; missing names are explicit. Recall requires a supported, valid amplifier session and loaded destination name; unavailable or current slots cannot be recalled. No footswitch radio connection is inferred from this diagram.

Mobile startup also sends `03`, receiving a six-byte `04` global reply. Desktop uses that observed read sequence; only `97` notifications establish footswitch status. An initial disconnected notification can precede a delayed connected notification. The Windows live watch received connected status, followed the user's physical B/A presses from 0A to 0B and back, and verified every original preset state/detail byte unchanged. The user confirmed the amplifier changed accordingly. A final normal desktop read with the diagram again confirmed connected status and exact preset preservation.

The developer-only **--footswitch-check address private-prefix** runs normal desktop connection/navigation, reads complete preset state/detail twice and renders only the app. It never pairs/unpairs, recalls or saves. Its bounded settling wait observes actual notifications rather than inventing connected status. Windows pairing/unpairing hardware roundtrip acceptance remains separate from the captured mobile cycle and offline desktop command/dialog checks.
