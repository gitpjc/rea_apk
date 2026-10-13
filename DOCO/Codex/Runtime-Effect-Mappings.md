# Runtime effect labels and bounded field correspondences

13 October 2026. Five phone screenshots were independently inspected and correlated with the retained Windows preset-4 snapshot on firmware **V1.1.9**. This label inspection performed no parameter writes, model changes or hardware operations. The capture operator reported that the baseline was unchanged while switching the selected effect view.

The phone visibly identifies the selected preset as **0D British Lead Solo**. The retained Windows snapshot identifies wire index **4**. This document preserves observed labels and unique baseline value correspondences. A subsequent controlled phone test resolves **808 Tone = Values[3]**, with evidence in [808-Tone-Editing.md](808-Tone-Editing.md); Vol remains inferred by elimination and Hall Tone remains unresolved. This is not a complete effect catalog.

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
| Gain | 39 | Values[2] = 39 | Unique baseline value; inferred, not controlled |
| Tone | 100 at baseline; 97 during later controlled test | Values[3] | Controlled phone change; [808-Tone-Editing.md](808-Tone-Editing.md) |
| Vol | 100 | Values[4] | Inferred by elimination after resolving Tone; not independently varied |

The initial baseline alone could not resolve Tone versus Vol, because both were 100. The later controlled Tone capture resolves Values[3]; Vol's remaining Values[4] association is inference by elimination. No Vol write has been independently controlled. Values[0] is unlabeled; no enabled-state meaning is established.

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

All six distinct values support the listed associations, but they remain baseline inferences rather than controlled offset/scale/range validation. Values[0] and Values[8], both 1, remain unknown. In particular, the trailing 1 must not be labeled cabinet enabled, bypass, polarity, mode or another guessed flag. The displayed model name is **PLX 100**; the manual's serial number does not establish wire model value 31.

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

The `ms` suffix is directly visible in the phone UI at this baseline. One matching raw/display point does not establish the full time encoding, allowed range, scaling, tap behavior or tempo subdivision. Values[0] and Values[5–7] remain unlabeled. No Analog delay write was performed.

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

The displayed `ms` suffix belongs to Pre Delay only. Decay 35 does not establish milliseconds, seconds or another unit. Tone's wire offset must not be inferred from screen order or the first trailing zero. Values[0] and other trailing-zero field meanings remain unknown. No Hall reverb write was performed.

## Implementation boundary and next checks

- [x] Inspect every supplied screenshot and retain its digest.
- [x] Correlate the four selected models with the preserved baseline's numeric blocks.
- [x] Retain unique baseline correspondences separately from equal-value ambiguities.
- [x] Resolve 808 Tone through a single-parameter phone capture and complete observed restoration.
- [ ] Independently vary 808 Vol to validate its inferred offset.
- [ ] Resolve Hall Tone among the trailing zeros through a single-parameter capture and exact restoration.
- [ ] Independently validate each new parameter offset, encoding, limits and restoration before enabling writes.
- [ ] Compare additional models before generalizing any category layout.

The curated fixture now marks 808 Tone `controlledChangeVerified: true` for the phone mapping, with the separate controlled fixture linked. Windows Tone writes remain unverified at this stage, and other new mappings remain baseline inferences. Scale/range limits remain unverified. These distinctions must survive future UI/catalog implementation; unknown fields and ambiguous labels remain unavailable for editing.
