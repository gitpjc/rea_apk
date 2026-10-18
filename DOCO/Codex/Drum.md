# Drum

Implemented and checked for **F15i V1.1.9**, **1.44.0**, 18 October 2026. The user authorized Drum P6.3 ahead of F4 and requested **Tuner above Drum** in the desktop **Tools** section. F4 follows Drum; other Phase 6 work remains deferred and GIR&IR excluded.

## Desktop behavior

Choose **Tools → Drum** or **Ctrl+5** after connecting. Settings and playback status load automatically. The page has a mobile-style circular **40–260 BPM** dial, one-step arrows, center **TAP**, style dropdown and ten numbered pattern buttons, **Play/Pause** and speaker button opening a vertical volume popup. Choose Pop, Rock, Blues, Funk, Jazz, Metal or Metronome to browse its ten patterns; browsing sends no command. Click a numbered button to select immediately without stopping playback. The current pattern is highlighted and its full confirmed label appears below. This user-requested desktop selector replaces the mobile wheel; mobile protocol and original labels are unchanged. The observed mobile volume selections are **0–99**; no unobserved hardware maximum is claimed.

The amplifier calculates TAP tempo from intervals between presses. Desktop sends the captured interval command and rereads its result; it does not guess a rounding rule or idle threshold. The first tap establishes the desktop interval. Keyboard arrows adjust the focused dial; Enter/Space taps. Fast gestures coalesce pending values while writes and complete readbacks remain serialized.

Changes are live and do not save or recall a preset. Playback continues when leaving Drum, including a visit to Tuner, matching actual mobile observations. Pause explicitly when finished. Effect values and pending effect inputs are preserved. Connection loss disables controls; uncertain queued commands are discarded rather than replayed.

## Verified mobile behavior

The installed mobile iAMP 1.6.1/code 43 on F15i V1.1.9 shows a circular BPM dial, TAP control, one-step tempo arrows, pattern wheel, playback button and speaker button. The speaker opens a vertical volume popup; it is not a mute toggle. The initial values were pattern **01 POP01**, **100 BPM**, volume **60**, playback stopped.

All **70 pattern selections** were individually observed and their outgoing commands captured. Pattern selection applies immediately while the wheel stays open; outside dismissal closes it. The mobile tenth label is **10 POP010**, which is retained exactly. The seven groups contain ten patterns each: Pop, Rock, Blues, Funk, Jazz, Metal and Metronome. Selection writes the zero-based pattern identifier at current-state payload offset 43, preserving all unrelated bytes. Full original state and detail were independently verified after the catalogue test.

The catalogue is backed by `tests/fixtures/f15-drum-pattern-observations.json`: all 70 captured complete commands and screenshot digests, without private device identifiers. The control fixture `tests/fixtures/f15-drum-control-observations.json` retains seven state edits and ten playback/TAP exchanges.

Tempo arrows were observed at 100→101→100; actual stops are 40 and 260. State offsets 41–42 contain little-endian tempo; volume edits affect only offset 40. Popup screenshots show 0, 60 and top selection 99. Unrelated state/detail values are preserved.

Tools entry sends **41 01**, receives **42 01** and stopped/playing status **49 00/01**, then replies **4A 01**. Playback uses **47 flag 00 00** with **48 00** response. TAP uses the same command with a little-endian millisecond interval. Captures include accepted 593, 594, 621, 990, 1011 and 2510 ms intervals (**48 01**) and ignored long idle intervals 33266/38534 ms (**48 00**). These samples do not establish an exact idle cutoff. A Windows 1000 ms interval independently reads back 60 BPM.

Mobile playback remains running after leaving Tools and after Drum→Tuner→Drum. The desktop shared Tools responder acknowledges both observed statuses; tuner entry retains its established quiet interval and subsequent tuner-mode transition.

## Current restoration status

The dial test temporarily reached **260 BPM** and was interrupted by phone Bluetooth being off. After the user enabled it, original **100 BPM**, **01 POP01**, **volume 60**, stopped playback and every original preset state/detail byte were independently verified before subsequent tests. Final mobile cross-tool testing and desktop acceptance also restored the complete original baseline. No preset was saved or recalled.

## Selector update — 1.44.0

The user requested style/number selection and tighter sidebar spacing. Focused offline checks cover all seven groups and 70 button mappings, selected-state accessibility, browsing preservation without amplifier writes and compact rows at four viewport sizes. Tuner remains above Drum. This UI-only change does not require new hardware mutations or a complete software suite. The following hardware evidence belongs to 1.43.0.

## Validation

- **18 affected tests pass** across Drum catalogue/control frames, Tools entry and shared responder behavior.
- Focused offline WPF **Drum** checks cover menu order, labels/settings, vertical volume, accessibility, disconnected/connection-loss guards, pending input preservation and four viewport sizes. An own-app render was inspected. Affected **Tuner**, **Mixer** and **preset-sync** UI groups also pass.
- A Windows transport roundtrip verifies tempo, volume, pattern, playback, page exit/return and TAP, with independent complete restoration.
- Actual desktop normal handlers verify coalesced dial updates, volume, pattern selection, Play/Pause, page exit/return and the mobile-observed **Drum→Tuner→Drum** playback roundtrip. Complete original state/detail and stopped playback are independently restored afterward.
- The first desktop navigation harness checked visibility before the asynchronous navigation task completed. It now awaits the whole transition; cleanup independently restored the baseline on that failed run too.

No complete-suite rerun is claimed. Hardware evidence is specific to F15i V1.1.9. Calibrated acoustic output, every pattern's audible content, Windows sleep/interruption scenarios and other firmware are not claimed.

The explicit developer mode `--drum-check address private-prefix` uses one visible normal desktop window, bounded operations and independent restoration. It refuses to start alongside another running iAMP Desktop executable. Original phone captures and Bluetooth records remain private and excluded from Git.

## Evidence boundaries

REA APK identity: `ev_9077223b5f8eab52ec03da1ddec03019769510db50c92c12f4dd63bd1b752d81`. The supplied APK is 1.6.1/code 42; runtime observations use installed code 43. The bounded Java-class Drum search returned no matches (`ev_06fdeebece2bf7636d0ca2ebfac4b55f5ce8c32bbca3125c665cbf511212628a`); it does not search all native Dart implementation. Native strings corroborate labels and identify Drum source paths, but are not treated as execution or wire-protocol proof.
