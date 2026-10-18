# Tuner

Implemented and verified for F15i firmware **V1.1.9**, **1.42.7**, 18 October 2026. The user reported the amplifier in tune while 1.42.5 said Low. Fresh desktop/mobile captures disproved the raw-as-segment assumption. The corrected normal desktop 1.42.6 received 68 updates, displayed A/Perfect and explicit No signal, and the user confirmed agreement with the amplifier. Subsequent mobile reference sweeps and a device-timed upper-center capture establish the corrected 1.42.7 segment spacing, green neighbors and colors. Independent cleanup verified every original state/detail byte after each comparison. The user authorized P6.2 ahead of F4. Other Phase 6 work remains deferred; GIR&IR remains excluded.

## Desktop behavior

Open **Tuner** or press **Ctrl+4** while connected. Entry pauses normal preset polling and competing edits, verifies the current preset, waits a three-second quiet interval, then performs the observed mobile Tools-to-Tuner handshake. The page shows the detected note and a 17-segment meter: **High!**, **Low!** or green **Perfect!**. Low means flat; High means sharp. No cents or measured frequency are inferred from these discrete positions.

Reference pitch supports **430–445 Hz**, in one-Hz steps. **Mute / Bypass** changes tuner output mode. Settings use serialized whole-state writes with complete readback and preserve unrelated values. No preset is saved or recalled. Stop, navigation away and window closing attempt verified exit before resuming normal operation. An amplifier-reported exit or connection failure clears the display and blocks misleading editing.

Readings are change-driven: retain a valid note until explicit no-signal, invalid input, stop or connection loss. A two-second silence timer was removed because captured valid readings can be more than two seconds apart. The user normally turns guitar volume off after a requested test; request volume on explicitly when another signal is needed.

## Validation

- The earlier transport integration passed **132 affected protocol/session/tuner/queue/diagnostic tests**. This established transport behavior, not correct physical tuning classification. No complete-suite rerun is claimed.
- The latest 1.42.7 correction passes **39 affected tuner decoding/captured-frame tests** and focused WPF tuner checks. The sanitized fixture now retains **80 captured frames and seven independent display observations**, including source digests, expected labels and lit segments. Tests cover the reported raw 15/16 regression, the observed upper pair, both directions and measured meter colors.
- Focused WPF tuner checks passed at five window sizes, including steady-note retention beyond the former timeout, explicit no-signal, connection loss, reported inactive mode, accessibility labels, stale generations and competing-edit guards. Preset-sync and Mixer checks also passed during integration.
- Windows settings checks covered all 16 reference values and both output modes, with exact complete original state/detail restoration.
- The actual normal desktop 1.42.5 trace received **115 live updates**. The user confirmed the amplifier tuner and desktop note/meter both responded. The trace subsequently showed **No signal** after playing stopped; no inactive-mode report occurred during the run of over six minutes.
- Actual desktop controls verified **440 → 441 → 440 Hz** and **Mute → Bypass → Mute**, observing continued active mode beyond the previously failing interval after each change.
- Normal desktop Stop/cleanup succeeded. A fresh independent Bluetooth connection confirmed **every original state and detail byte matched the baseline**, including active preset 0A. No saves or recalls were performed.
- Physical tuner activation and Windows exit were also observed earlier. Mute/Bypass command and state behavior is verified; calibrated acoustic output measurements, other firmware and interruption/sleep acceptance are not claimed.

## Protocol observations

Mobile iAMP 1.6.1 uses `41 01` for Tools entry, answers the exact incoming `49 00` request with `4A 01`, then selects Tuner with `41 02`. Mode commands acknowledge `42 01`; `41 00` exits. The responder also handles the observed initial connection request. The wider meaning of `49 00` is not established. `43 00` reports inactive and `43 02` active.

The 92-byte current-state payload stores reference pitch at zero-based byte 44 (0–15 maps to 430–445 Hz) and output at byte 45 (1 Mute, 2 Bypass). Settings writes use opcode `13`, acknowledgement `14 01`, followed by complete state/detail verification.

Live FFF2 notifications contain opcode `45`, note and raw deviation bytes. Note codes 1–12 map to B, C, C#, D, D#, E, F, F#, G, G#, A, A#. `45 00 00` means no signal. **Raw deviation is not a visible segment index, and a simple raw/2 projection is also incorrect.** Raw 16 is centered. For nonzero deviation from 16, reserve the center region, then group outward pairs: distance is `1 + abs(raw - 16) / 2` using integer division; subtract from visible center 8 for High and add for Low, clamping rendering to 0–16. Raw 15–17 uses green Perfect: raw 15 lights segments 8/9, raw 16 the whole meter, raw 17 segments 7/8. Raw bytes remain preserved. No calibrated cents meaning is established.

Fresh mobile screenshots directly show A/raw 15 with green Perfect and segments 8/9, A/raw 16 with the full green meter, and the upper raw 17 pair7/8. The upper capture records device timestamps around the actual screenshot; packets16/17 occur in its window, and its upper pair differs from the separately established raw 16 full-meter display. Reference438 Hz shows raw 18 High at segment 6;430 Hz shows raw 28/29 High at1;441 Hz shows raw 13/14 Low at10. Retained E/raw 9 observations show lower segment 12. These observations establish representative mapping correspondence on both sides; an exhaustive physical sweep of every raw value has not been performed. Transition-ambiguous frames are not claimed as exact single-packet matches. Guitar fret/pressure/intonation variation was not treated as calibrated pitch evidence.

Dominant flat-fill colors measured from the original mobile screenshots are green `#5CE600`, yellow `#FFC001`, orange `#ED7D31` and red `#E90000`. The desktop uses these observed colors. The user's mobile-first verification rule is recorded in Plan.md: inspect actual behavior before reproducing it, retain evidence gaps and do not equate live delivery alone with functional parity.

## Entry investigation and final correction

Earlier builds acknowledged entry but the amplifier emitted `43 00` about 2.45 seconds later, without a desktop exit command. Battery polling alone, the captured `03` setup follow-up and cached identity alone did not resolve normal desktop entry. These are historical failures. The unsuccessful extra setup request was removed.

In bounded comparisons, a minimal sequence remained active; adding the normal identity request reproduced the delayed exit. Quiet startup intervals of 3, 5 and 8 seconds prevented the exit during each 5.5-second observation window. Production therefore keeps its same-session verified firmware identity, freshly checks the full current state and waits three seconds before the ordered Tools/Tuner transition, with normal polling paused. The subsequent normal desktop live run passed. This is a tested guard interval, not a decoded firmware timing constant or proof that identity alone causes the firmware behavior.

The explicit developer `--tuner-check <address> <private-output-prefix>` mode uses normal connection/navigation handlers in one visible instance. It records at most 2048 private readings/display transitions plus allowlisted diagnostics, stops after ten minutes or a `<prefix>.stop` marker, exits and independently compares the full baseline. Captures, addresses and private results stay excluded from Git. Normal diagnostics contain event categories and timings, not raw packets or preset contents.

## Evidence limits

The supplied APK is 1.6.1/code 42; the installed phone app reports code 43. REA APK identity evidence: `ev_d395beb36dce25aee008f8a641306f8fda60e19d035c384ddb206b0614b87a69`. A bounded Java-class search found no Tuner match (`ev_a93335281b3e6154f2697e20e7ccacc98e10ccf18a156c3f8f3f4df9c4d351ba`); this is not a search of all native Dart code. Sanitized observations are retained in `tests/fixtures/f15-tuner-observations.json`.
