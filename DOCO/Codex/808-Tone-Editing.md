# 808 Tone controlled mapping and restoration

13 October 2026. The phone's **808 Tone** control was changed **100 → 97 → 100** on preset wire index **4**, firmware **V1.1.9**. The user confirmed that the amplifier was quiet and nothing was playing before this controlled test. The observed writes establish **Tone = Values[3]** in the type-1/model-3 five-value block at chain position 2. Windows independently read back the complete restored state and preset detail.

The phone capture establishes the mapping; subsequent Windows API and actual desktop-handler tests establish the restricted control implementation. No preset save or power-cycle persistence test is claimed.

## Sources and capture scope

Private inputs are `analysis/private/phone-tone-roundtrip-logcat.txt` (app PID 10152), `phone-tone-bluetooth.txt`, decoded `phone-tone-btsnoop.log`, and `phone-tone-capture.json`. The official existing `.tools/android/btsnooz.py` decoder used the already available bundled Python runtime without installing anything. The existing Node parser reconstructed the application frames:

```bash
source scripts/env.sh
node scripts/protocol-capture.mjs \
  --logcat analysis/private/phone-tone-roundtrip-logcat.txt \
  --btsnoop analysis/private/phone-tone-btsnoop.log \
  --output analysis/private/phone-tone-capture.json \
  --fixture analysis/private/phone-tone-all-frames.json
```

The decoded retained ring contains **5,443 HCI records, 846 target ATT PDUs and 253 application frames**, all CRC-valid, including history from earlier sessions. This Tone analysis uses only the three matching type-1/position-2 edit writes in the final target-attributed connection, established at record **5027**, connection handle **5**. No truncation or unfinished reassembly was detected. Earlier threshold/selection packets are not counted as Tone edits.

The sanitized [f15-observed-808-tone.json](../../tests/fixtures/f15-observed-808-tone.json) retains exact edit frames, source ordinals, log lines, screenshot observations and source/payload hashes. It excludes device addresses and complete private presets.

## Controlled observation

The independently inspected phone screenshots show:

| Screenshot | Gain | Tone | Vol |
| --- | --- | --- | --- |
| `phone-tone-before.png` | 39 | 100 | 100 |
| `phone-tone-changed.png` | 39 | 97 | 100 |
| `phone-tone-restored.png` | 39 | 100 | 100 |

All three show **OD / 808** and the same preset display, **0D British Lead Solo**. The phone sends the complete five-value block using the previously observed `25` update discriminator:

| Phone log time and line | HCI record | Observed block values | Complete frame |
| --- | --- | --- | --- |
| 23:41:34.551, line 559 | 5423 | `[1,3,39,97,100]` | `AA550E002502010A010003002700610064000CB5` |
| 23:41:53.511, line 574 | 5431 | `[1,3,39,99,100]` | `AA550E002502010A01000300270063006400E1DD` |
| 23:41:53.601, line 576 | 5434 | `[1,3,39,100,100]` | `AA550E002502010A01000300270064006400B0F0` |

The intermediate 99 occurred while restoring the slider; the final screenshot shows 100. Phone log times are source labels without cross-device clock calibration.

Payload structure is:

```text
25 02 01 0A  0100 0300 2700 TT00 6400
│  │  │  │    └──── five little-endian16-bit values ───┘
│  │  │  └─ 10 value bytes
│  │  └──── type1 (OD in this captured configuration)
│  └─────── chain position2
└────────── observed block-update discriminator
```

Tone is **Values[3]**, payload byte offsets **10–11**, complete-frame offsets **14–15**. In this particular baseline's preset-detail payload, the second block starts at offset 37 and Tone occupies offsets **46–47**. That detail byte offset is specific to the preceding block's serialized length; it is not a universal parameter ID.

Only the Tone value changes among the three outgoing payloads. Every other block value is preserved. Gain remains 39 and Vol remains 100 in the screenshots. This resolves the earlier equal-value Tone/Vol ambiguity for Tone. **Vol = Values[4] remains an inference by elimination**, not an independently varied Vol mapping. Values[0] is still an unknown field and must not be called an enabled flag.

All three frames validate CRC-16/GSM over the length bytes plus payload, excluding AA55 and the big-endian trailer. Incoming `AA5502002601AEF6`, payload `26 01`, appears at **5426, 5436 and 5438**. The isolated Tone-97 write has a following response; both rapid restoration writes occur before their two identical responses. Per-write response attribution during restoration is ambiguous. The meaning of response byte `01` remains unknown and must not replace readback verification.

## Complete observed restoration

The independent Windows snapshots `analysis/private/windows-before-tone.json` and `windows-after-phone-tone.json` show wire index **4** before and after, with **full current-state and full preset-detail payload equality**. The restored OD block is `[1,3,39,100,100]`.

| Payload | Bytes | Baseline and restored SHA-256 |
| --- | --- | --- |
| Current state | 92 | `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` |
| Preset detail | 160 | `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce` |

Hashes cover decoded payload bytes, excluding framing/CRC. The phone screenshot independently establishes the changed UI value 97; the retained Windows evidence independently establishes complete observed restoration to 100. No intermediate Windows read of the phone-changed 97 is claimed.

## Implementation boundary

- [x] Resolve 808 Tone's offset through a controlled phone change and restoration.
- [x] Verify exact frame checksums and preserved block fields.
- [x] Confirm complete observed restoration using independent full Windows state/detail reads.
- [x] Implement `Od808ToneCommand` and `Set808ToneAsync`, restricted to V1.1.9, second-position type 1/model 3, Gain 39, Vol 100 and Tone 97–100. Fresh state/detail comparison rejects stale snapshots; full expected read-back determines success.
- [x] Validate Windows Tone 100 -> 98 -> 100, preserving and restoring complete state/detail. Evidence: `analysis/private/windows-808-tone-roundtrip.json`, `baselineRestored: true`, `saved: false`.
- [x] Exercise actual desktop Tone handlers on 14 October 2026. `analysis/private/desktop-ui-tone-oct14.roundtrip.json` reports `passed: true`, `baselineRestored: true`, `failure: null`; final stage is Tone 98 -> 100. A separate fresh connection verified complete baseline equality after preset/threshold/Tone tests.
- [ ] Independently vary Vol before calling its offset controlled.
- [ ] Establish ranges, scales, rejection responses, other models and persistence separately.

The implementation restricts Tone to **97–100**. Phone values **97, 99 and 100** and Windows value **98** are now observed; this does not establish a device-wide range. Every test change must be restored and verified against its exact original full state/detail baseline. No automatic replay or acknowledgement-only success path is justified.
