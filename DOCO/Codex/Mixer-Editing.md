# Mixer

Version 1.40.0 adds the four mobile Mixer controls to a separate desktop page. Connect through **Amplifier Settings**, then choose **Mixer** or press **Ctrl+3**. The same live Bluetooth connection serves both pages.

| Channel | Level range | Step | Mobile color |
| --- | --- | --- | --- |
| Bluetooth | −36.0 to 0.0 dB | 0.5 dB | Blue |
| USB REC | −36.0 to 0.0 dB | 0.5 dB | Blue |
| Instrument | −36.0 to 0.0 dB | 0.5 dB | Blue |
| Master | −30.0 to +6.0 dB | 0.5 dB | Orange |

Drag a vertical fader and release to apply. Arrow keys move one step; releasing the key applies the value. You can also enter an exact dB value and press Enter or choose **Apply**. The speaker button mutes or unmutes its channel. Muted faders are grey and remain adjustable, matching the observed mobile behavior. Changing a muted level preserves its mute flag.

Mixer changes are live and do not save or recall a preset. Existing unsaved effect edits and locally pending effect inputs are preserved. The preset-effects JSON retains its existing scope; mixer levels are not effect parameters. Exported full snapshots already retain the raw mixer state.

## Evidence and protocol

The complete mobile page fits on the phone screen; all four channels, their limits, values and speaker controls were inspected. Guarded ADB captures recorded endpoints, middle values, mute/unmute and dragging while muted. Independent Windows reads confirmed the complete original current state and preset detail after each capture. The public fixture `tests/fixtures/f15-mixer-observations.json` retains successful mobile frames, preceding states, numeric observations and SHA-256 evidence references. Phone logs and images remain private and ignored by Git.

The 92-byte current-state response uses opcode `12`. A live Mixer write uses the complete state with opcode `13`, retaining the current preset index and every unrelated byte. The reply must be exactly `14 01`. Captured fields are:

| Channel | Mute byte | Level byte(s) | Physical conversion |
| --- | --- | --- | --- |
| Bluetooth | 29 | 30 | raw / 2 − 36 dB |
| USB REC | 31 | 32 | raw / 2 − 36 dB |
| Instrument | 33 | 34 | raw / 2 − 36 dB |
| Master | 35 | 37, 38 | raw / 2 − 30 dB |

Offsets are zero-based within the payload. Mute is 0/1 and levels are 0–72. Master writes update both level bytes together. Byte 36 and all other fields remain opaque and are preserved. Asymmetric Master levels can be displayed, but Master editing is unavailable until that state is independently understood.

Writes require a fresh full snapshot matching the displayed source, firmware V1.1.9 and 80 preset slots. After one write, the transport checks the exact whole-state result and unchanged complete preset details. A failed or interrupted operation disables editing until a fresh read; it is never retried automatically. Local snapshots and preview mode cannot write.

## Validation

Focused protocol tests cover the retained mobile frames, all 73 levels at all 80 slots, mute flags, physical formatting/parsing, malformed states, paired Master guards and preservation of every unrelated byte. Windows transport testing exercised boundaries, middle and half-step values, mute/unmute and muted level changes for each channel. An unsaved Noise Killer Threshold change from 25 to 26 survived Mixer changes across all four channels; it was then restored to 25. Every test finished with the complete original state and detail verified through an independent connection. No preset was saved or recalled.

All 176 affected protocol/read/queue/save cases and focused four-channel WPF checks pass, including five viewport sizes, navigation, local/preview guards, invalid inputs and asymmetric Master. Actual desktop fader-release, mute and muted-level handlers pass for all four channels while preserving an unsaved Threshold 26 and local pending Threshold 27 input. Final original Threshold 25 and every mixer/preset byte are independently restored. GIR&IR and Phase 6 remain excluded.

The self-contained Windows x64 release is published to `artifacts/Iamp.Desktop`. Source/package versions and all 62 history entries pass the version checker; Desktop, Transport and Protocol DLL SHA-256 hashes match staging. Release builds have no warnings or errors. The final polling fix is covered by the focused gesture-eligibility check and reviewed against serialized transport and poll-delivery guards.
