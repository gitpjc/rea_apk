# Read-only current-state and preset format

## Scope at 17 October 2026 — current application 1.41.2

The raw layout observations below remain evidence, not a universal vendor specification. Current live controls target V1.1.9 and all 124 requested built-in models; the earlier V1.1.2/unknown-model interpretations are historical. Readable preset JSON schema 3.0.0 is documented separately in Preset-JSON.md and excludes raw/internal fields. Raw snapshot archives use schema 1 and cannot be applied to the device.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


Updated 13 October 2026. This records one F15i V1.1.2 preset and supporting phone/UI evidence, not a complete preset or editing specification. No hardware commands were sent by this offline analysis. All offsets below are **payload offsets**, excluding the four-byte AA55/length header and two-byte CRC trailer.

## Sources and verification

- `Protocol.md` describes the attributed Android session and independently validated CRC envelope.
- Retained capture SHA-256: `b87933b64f3bf7fac4ce9f70967462a6430e23b6c765574f92cf0c9e9d08911b`. State request/response records are 601 / 604–605; preset-detail request/response records are 606 / 609–611.
- `analysis/private/iamp-logcat.txt`, lines 421–502, explicitly logs preset index/name and all 20 records' sequence, type, parameter byte length and parameter values.
- Retained phone screenshot `analysis/private/iamp-usb.png` shows `3D Cali MK3 Lead`, OD/D-Drive and Gain 0, Tone 73, Vol 99. Visible chain begins OD, Amp, EQ, Delay. User notes in `DOCO/MOOER/MOOER AMP.md` independently associate 3D with Cali MK3 Lead and 6B with Rock N Roll.
- `analysis/private/windows-current-preset.json` records actual Windows reads. Both raw state and detail payloads match the Android capture **byte for byte**; `scripts/preset-evidence.mjs` verifies this when that private file is present.
- `tests/fixtures/f15-observed-preset.json` retains the two original CRC-valid replies, source hashes, capture ordinals and decoded expected records. These contain the reviewed generic device name and factory preset; no addresses/accounts or raw log contents are included.

Run `node scripts/preset-evidence.mjs` from the project root. The offline Node helper validates all 20 records against the exact app diagnostic arrays before writing the fixture. It intentionally stops if capture coverage changes, so new preset data receives a privacy/provenance review.

## Current state: 11 → 12

Observed request payload is `11`; response payload is 92 bytes, beginning `12 10 00 18 18 18 18 09 FF ...`.

| Payload offset | Observation | Interpretation / confidence |
| --- | --- | --- |
| 0 | `12` | Response discriminator, observed. |
| 1 | `10` (decimal 16) | Current preset wire index, corroborated by the immediately following request `21 10`, app diagnostic index 16 and detail response. |
| 2–8 | `00 18 18 18 18 09 FF` | Raw fields, meaning unknown. Do not label mixer/battery/settings from numeric resemblance. |
| 9–28 | `F15iF15i` followed by zero bytes, within a 20-byte region | Device-name candidate supported by actual Windows DeviceName. General field length/termination is not yet proven across different names. |
| 29–91 | Remaining raw bytes | Settings/state fields unresolved. Retain raw evidence rather than inventing controls. |

The state reply does not itself contain the preset name or decoded effect records. Query detail using the observed index, then check the response index; do not assume preset 16 is always active.

## Preset detail: 21 index → 22 index

Observed request payload is `21 10`. The reply is 182 bytes: `22 10`, a 20-byte name field, 20 variable records, then two additional bytes `00 00`.

| Payload region | Observed encoding |
| --- | --- |
| 0 | Response discriminator `22`. |
| 1 | Preset index `10` (16), matching the request/current-state reply. |
| 2–21 | UTF-8/ASCII-compatible zero-padded name `Cali MK3 Lead`. Only ASCII is demonstrated by this name. |
| 22 onward | Twenty records, in sequence 1 through 20, encoded as below. |
| 180–181 | `00 00`; **unknown payload fields**, not the outer CRC. Preserve them; do not treat them as confirmed reserved bytes or volume. |

Each observed record is `[sequence:u8, type:u8, parameterByteLength:u8, parameters...]`. Parameters decode into little-endian 16-bit values; byte length must be even, and record data must remain within the payload. Larger values 260, 750, 2300, 6000 and 347 match the app's diagnostic output, ruling out a byte-only interpretation for these values. Unsigned interpretation reproduces this sample; signedness of every possible parameter is not established.

| Record / payload offset | Type | Parameter bytes | Decoded values |
| --- | --- | --- | --- |
| 1 / 22 | 1 | 10 | `1, 5, 0, 73, 99` |
| 2 / 35 | 2 | 18 | `1, 35, 40, 36, 42, 47, 48, 57, 1` |
| 3 / 56 | 9 | 34 | `1, 1, 36, 80, 8, 32, 260, 17, 32, 750, 9, 37, 2300, 10, 94, 6000, 62` |
| 4 / 93 | 7 | 16 | `1, 1, 13, 25, 347, 0, 0, 0` |
| 5 / 112 | 8 | 20 | `1, 1, 0, 15, 55, 22, 0, 0, 0, 0` |
| 6–20 / 135 through 177, step 3 | 0 | 0 | Empty arrays. |

Twenty record positions are observed in this reply. This is not proof that the amplifier supports 20 simultaneously active effects, or that all presets/firmware use this exact layout. Type-zero/zero-length records are empty in the app diagnostic; retain their sequence for reconstruction.

## Effect labels and parameter meaning

The screenshot's selected first module is OD/D-Drive, displaying Gain 0, Tone 73, Vol 99. Record 1 contains exactly those final three values. This supports the **sample-specific** mapping type 1 → OD, model candidate 5 → D-Drive, parameter indices 2/3/4 → Gain/Tone/Vol (zero-based parameter indices). Parameter 0 is 1 and the UI indicates an active module; enabled/bypass semantics still require a controlled toggle/readback.

The remaining visible chain order supports associations type 2 → Amp, type 9 → EQ and type 7 → Delay. Type 8 → Reverb is a plausible candidate from the fifth record and user notes, but the inspected screenshot does not show the fifth module. Model IDs, units, ranges, defaults, scaling and labels beyond the corroborated OD fields remain open. A read-only viewer can safely show sequence/type/raw parameter values, with inferred labels marked accordingly.

## Bank/letter labels

Windows name list wire index 16 is Cali MK3 Lead; the phone screenshot and user notes label it **3D**. Wire index 26 is Rock N Roll; user notes label it **6B**. Both anchors fit:

```text
bank = floor((wireIndex - 1) / 4)       // candidate display banks 0..19
position = "ABCD"[(wireIndex - 1) % 4]
```

This is a supported display inference, not a validated selection encoder or proof for every slot. It yields wire 1→0A, wire 5→1A, wire 16→3D and wire 26→6B. The reported manual 1A/US 65 Deluxe Clean example agrees with name-list index 5 but was not independently re-rendered in this pass. User notes say 5C “Ballad Sonic”, while wire 23 is “Ballad Song”; do not silently equate the differing names. Show wire index alongside any inferred label until all-slot mapping is accepted.

## Open acceptance work

- [x] Recover current-state/detail records with capture provenance and valid CRC.
- [x] Verify all 20 decoded records against Android diagnostics.
- [x] Compare actual Windows state/detail payloads with retained Android replies.
- [ ] Compare a second preset with different module order/types and non-ASCII name.
- [ ] Verify each type/model label and enabled/bypass semantics through controlled observation.
- [ ] Establish parameter units/ranges/signedness and trailing-field meaning.
- [ ] Validate every bank/letter mapping and distinguish active-state from saved-preset details.
- [ ] Establish modification, save, acknowledgements and restore separately after the backup baseline. No write encoder is justified by this read-only sample.
