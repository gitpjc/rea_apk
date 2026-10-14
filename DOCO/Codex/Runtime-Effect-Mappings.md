# Runtime effect labels and bounded field correspondences

Original baseline inspection: 13 October 2026. Updated 14 October 2026 with separate controlled Level/Gain/Vol evidence. Five original screenshots were independently inspected and correlated with the retained Windows preset-4 snapshot on firmware **V1.1.9**. That original label inspection performed no parameter writes or model changes. Subsequent controlled experiments are identified separately below.

The phone visibly identifies the selected preset as **0D British Lead Solo**, wire index **4** in the Windows snapshot. Controlled phone experiments now establish **808 Gain/Tone/Vol = Values[2]/[3]/[4]** and **Noise Killer Level = Values[3]**, with restoration evidence in [Expanded-Controls.md](Expanded-Controls.md) and [808-Tone-Editing.md](808-Tone-Editing.md). Hall Tone is now resolved through controlled ADB/Windows tests in `Reverb-Editing.md`. This is not a complete effect catalog.

## Evidence and confidence

The curated [f15-effect-labels.json](../../tests/fixtures/f15-effect-labels.json) contains SHA-256 digests of all five screenshot files and `analysis/private/windows-v119-restored.json`, plus the four relevant numeric blocks. The original screenshots and complete preset snapshot remain private. The full preset-detail payload digest is `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce`, matching the restored baseline used in the threshold and selection investigations.

Three evidence levels must remain distinct:

1. **Observed UI facts:** a screenshot establishes the displayed category, model, parameter label, value and any visible unit suffix.
2. **Inferred baseline correspondence:** a distinct screenshot value uniquely matches one numeric value in the corresponding captured block. This supports a read-only label association for this configuration, but has not been tested by independently changing that parameter.
3. **Controlled mapping:** a single parameter is changed, captured and independently corroborated, then restored. The original read-only label inspection did not reach this level for the new models. A subsequent phone **808 Tone 100 → 97 → 100** test resolves Tone's offset, with complete observed Windows restoration; no intermediate Windows read of 97 is claimed. The earlier Noise Killer threshold experiment is documented separately in [Parameter-Editing.md](Parameter-Editing.md).

All `Values[n]` positions below are **zero based within the parsed block's 16-bit value array**, not byte offsets, manual serial numbers, global parameter IDs or chain positions.

## OD / 808

Source: `analysis/private/phone-od-baseline.png`. The screenshot displays model **808**, Gain **39**, Tone **100**, Vol **100**. Correlated block position 2 is type 1, model value 3, five values:

```text
[1, 3, 39, 100, 100]
```

| Label | Displayed value | Supported correspondence | Confidence |
| --- | --- | --- | --- |
| Gain | 39 at baseline; 36 during controlled test | Values[2] | Controlled phone change; [Expanded-Controls.md](Expanded-Controls.md) |
| Tone | 100 at baseline; 97 during later controlled test | Values[3] | Controlled phone change; [808-Tone-Editing.md](808-Tone-Editing.md) |
| Vol | 100 at baseline; 97 during controlled test | Values[4] | Now independently controlled on the phone; [Expanded-Controls.md](Expanded-Controls.md) |

The initial baseline could not resolve Tone versus Vol because both were 100. The later Tone capture resolved Values[3], and the 14 October Vol capture independently validates Values[4]. Gain's earlier baseline inference is also now controlled. Separate Windows API and desktop-handler tests now validate the bounded Gain/Tone/Vol edits, including combined dirty-state changes and full baseline restoration; details are in [Expanded-Controls.md](Expanded-Controls.md). Values[0] remains unlabeled.

## Dyna / Noise Killer follow-up

The position-1/type-4/model-2 block `[1,2,25,50,0,0]` now has controlled phone mappings for Threshold **Values[2]** and Level **Values[3]**. Level **50 → 47 → 50** preserves Threshold 25 and the remaining fields. [Expanded-Controls.md](Expanded-Controls.md) retains exact occurrences and independent final restoration. Other flag/trailing-field meanings remain unknown.

## Amp / PLX 100

Sources: `analysis/private/phone-amp-baseline.png` and `phone-amp-lower-baseline.png`. The lower screenshot shows **Master 75** clearly, while the first screenshot is clipped at that row. Correlated block position 3 is type 2, model value 31, nine values:

```text
[1, 31, 100, 35, 43, 40, 45, 75, 1]
```

| Label | Displayed value | Unique baseline correspondence |
| --- | --- | --- |
| Gain | 100 | Values[2] |
| Bass | 35 | Values[3] |
| Mid | 43 | Values[4] |
| Treble | 40 | Values[5] |
| Presence | 45 | Values[6] |
| Master | 75 | Values[7] |

Subsequent controlled ADB and Windows tests establish all six offsets within the bounded intervals documented in `Amp-Gain-Master.md`. Full scales/ranges remain unverified. Values[0] is on/off and Values[8] mirrors it in the observed bypass workflow; no cabinet/polarity semantics are inferred. The displayed model name is **PLX 100**; the manual's serial number does not establish wire model value 31.

## Delay / Analog

Source: `analysis/private/phone-delay-baseline.png`. Correlated block position 4 is type 7, model value 1, eight values:

```text
[1, 1, 17, 25, 500, 0, 0, 0]
```

| Label | Displayed value | Unique baseline correspondence |
| --- | --- | --- |
| Level | 17 | Values[2] |
| Feedback | 25 | Values[3] |
| Time | 500ms | Values[4] = 500 |

The `ms` suffix is directly visible in the phone UI at this baseline. One matching raw/display point does not establish the full time encoding, allowed range, scaling, tap behavior or tempo subdivision. Values[0] and Values[5–7] remain unlabeled. The initial read-only inspection performed no Analog delay write. Subsequent ADB/Windows tests independently verify Level Values[2] 16–17, Feedback Values[3] 24–25 and Time Values[4] 499–500 ms, including all three together with full restoration; see `Delay-Editing.md`.

## Reverb / Hall

Source: `analysis/private/phone-reverb-baseline.png`. Correlated block position 5 is type 8, model value 1, ten values:

```text
[1, 1, 20, 24, 35, 0, 0, 0, 0, 0]
```

| Label | Displayed value | Supported correspondence | Confidence |
| --- | --- | --- | --- |
| Pre Delay | 20ms | Values[2] = 20 | Unique baseline value; inferred |
| Level | 24 | Values[3] = 24 | Unique baseline value; inferred |
| Decay | 35 | Values[4] = 35 | Unique baseline value; inferred |
| Tone | 0 | One of Values[5], [6], [7], [8], [9] | Unresolved: five zeros |

The displayed `ms` suffix belongs to Pre Delay only. Decay 35 does not establish milliseconds, seconds or another unit. Tone's wire offset must not be inferred from screen order or the first trailing zero. Values[0] and other trailing-zero field meanings remain unknown. The original label inspection performed no Hall write. Subsequent ADB captures and actual Windows handlers verify Pre Delay Values[2] 18–20 ms, Level Values[3] 23–24, Decay Values[4] 34–35 and Tone Values[5] 0–1. Individual, midpoint and combined tests restore every original state/detail byte independently; see `Reverb-Editing.md`. The historical baseline ambiguity in the table above is resolved by that later controlled experiment, not by screen ordering.

## Implementation boundary and next checks

- [x] Inspect every supplied screenshot and retain its digest.
- [x] Correlate the four selected models with the preserved baseline's numeric blocks.
- [x] Retain unique baseline correspondences separately from equal-value ambiguities.
- [x] Resolve 808 Tone through a single-parameter phone capture and complete observed restoration.
- [x] Independently vary 808 Vol and Gain to validate their offsets, with complete observed restoration.
- [x] Independently vary Noise Killer Level and restore baseline.
- [x] Resolve Hall Tone among the trailing zeros through ADB 0→1→0 capture and exact independent restoration; actual Windows handlers also verify 0→1→0 and combined Hall edits.
- [ ] Independently validate each new parameter offset, encoding, limits and restoration before enabling writes.
- [ ] Compare additional models before generalizing any category layout.

808 Gain/Tone/Vol and Noise Killer Level have controlled phone mappings and separate bounded Windows/API/desktop-handler proofs, including complete baseline restoration. [Expanded-Controls.md](Expanded-Controls.md) preserves the separate provenance and nearby values; [808-Tone-Editing.md](808-Tone-Editing.md) retains the earlier Tone proof. Amp and Analog Delay now have controlled bounded mappings in `Amp-Gain-Master.md` and `Delay-Editing.md`; Hall now has controlled bounded mappings, including Tone at word 5, in `Reverb-Editing.md`. Full scales/ranges remain unverified. These distinctions must survive future catalog implementation; unknown fields and ambiguous labels remain unavailable for editing.

## Noise Killer range follow-up — 1.16.0

ADB endpoint inspection establishes the phone Threshold/Level limits at 0 and 100, with exact word-2/word-3 frames. Actual Windows handlers verify representative boundary/interior values and all four extreme combinations, restoring every original byte independently. The 0–100 ranges supersede the original nearby intervals; see `Noise-Killer-Ranges.md`. This does not generalize to other Dyna algorithms.

## OD808 range follow-up — 1.17.0

ADB endpoint inspection establishes Gain/Tone/Volume limits at 0–100 with exact word-2/3/4 frames. Actual desktop handlers verify representative boundary/interior values and all eight extreme combinations, followed by complete independent restoration. See `OD808-Ranges.md`. These observed limits supersede the earlier nearby intervals for this captured model only.

## PLX 100 range follow-up — 1.18.0

ADB endpoint inspection establishes all six PLX 100 sliders at 0–100, matching direct integers at words 2–7. Actual desktop handlers verify representative boundary/interior values and combined extremes, restoring every original byte independently. See `PLX100-Ranges.md`. These limits supersede the earlier nearby intervals for this captured model only; no physical tone/gain units or other-model ranges are inferred.

## Hall Reverb range follow-up — 1.19.0

ADB endpoint inspection establishes Pre Delay at 0–200 ms and Level/Decay/Tone at 0–100, directly encoded in words 2–5. Actual desktop handlers verify representative boundary/interior values and all 16 endpoint combinations with complete independent restoration. See `Hall-Reverb-Ranges.md`. These limits supersede the earlier nearby Hall intervals only; trailing-field meanings and physical units for Decay/Tone remain unknown.

## Analog Delay range follow-up — 1.20.0

ADB endpoint inspection establishes Level/Feedback 0–100 and Time 40–2500 ms, directly encoded in words 2–4. Feedback endpoint frames retain Level 0 during the muted test. Actual Windows controls verify boundaries, interior values and representative combinations with complete independent restoration. See `Analog-Delay-Ranges.md`. These limits supersede earlier nearby Delay intervals only; trailing-word meanings and other-model support remain unknown.
