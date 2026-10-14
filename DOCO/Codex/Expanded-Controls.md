# Controlled mappings: Noise Killer Level, 808 Gain and Vol

14 October 2026. Three additional phone controls were changed individually and restored on firmware **V1.1.9**, preset wire index **4**. The evidence establishes **Noise Killer Level = Values[3]**, **808 Gain = Values[2]**, and **808 Vol = Values[4]**. Eight screenshots were independently inspected, the transmitted frames reconstructed from target-attributed HCI traffic, and separate Windows reads confirm complete final state/detail restoration.

This investigation analyzed retained evidence only. Subsequent separate **Windows API and actual desktop-handler round trips now pass**, with complete observed baseline restoration. Existing Threshold and Tone proofs are documented in [Parameter-Editing.md](Parameter-Editing.md) and [808-Tone-Editing.md](808-Tone-Editing.md).

## Sources and reconstruction

Private inputs are `analysis/private/phone-controls-roundtrip-logcat.txt` (app PID 21479), `phone-controls-bluetooth.txt`, and the eight screenshots below. The existing official `.tools/android/btsnooz.py` decoder used the already available bundled Python runtime; nothing was installed. The decoded file is `phone-controls-btsnoop.log`. The existing Node parser produced `phone-controls-capture.json`:

```bash
source scripts/env.sh
node scripts/protocol-capture.mjs \
  --logcat analysis/private/phone-controls-roundtrip-logcat.txt \
  --btsnoop analysis/private/phone-controls-btsnoop.log \
  --output analysis/private/phone-controls-capture.json \
  --fixture analysis/private/phone-controls-all-frames.json
```

The complete capture contains **1,312 HCI records, 392 target ATT PDUs and 106 application frames**, all CRC-valid. Two LE enhanced connection-complete events attribute the target at records 434 and 929, handles 2 and 3 respectively. The curated control writes come from the latter connection. No record truncation or unfinished L2CAP/application reassembly was detected.

The tracked [f15-observed-expanded-controls.json](../../tests/fixtures/f15-observed-expanded-controls.json) contains **nine ordered write occurrences**, source log lines/HCI ordinals, screenshot observations/digests and full-restoration hashes. Identical bytes remain separate occurrences. Device addresses and complete private preset contents are excluded.

## Phone observations and wire fields

| Control | Block configuration | Independently inspected phone sequence | Other displayed values |
| --- | --- | --- | --- |
| Noise Killer Level | Position 1, type 4, model value 2, six values | `phone-controls-connected.png`: 50 → `phone-level-changed.png`: 47 → `phone-level-restored.png`: 50 | Threshold remains 25 |
| 808 Gain | Position 2, type 1, model value 3, five values | `phone-gain-before.png`: 39 → `phone-gain-changed.png`: 36 → `phone-gain-restored.png`: 39 | Tone and Vol remain 100 |
| 808 Vol | Same OD block | Prior restored OD baseline: 100 → `phone-volume-changed.png`: 97 → `phone-volume-restored.png`: 100 | Gain remains 39; Tone remains 100 |

All screenshots show **0D British Lead Solo**. The writes use the full-block update payload: discriminator `25`, chain position, type, value-byte count, then all little-endian 16-bit values. Only the selected scalar differs within each sequence:

```text
Noise Killer Level: 25 01 04 0C [1, 2, 25, LEVEL, 0, 0]
808 Gain:          25 02 01 0A [1, 3, GAIN, 100, 100]
808 Vol:           25 02 01 0A [1, 3, 39, 100, VOL]
```

Bracketed numbers are decoded 16-bit values, not literal single bytes. `Values[n]` is zero based within the parsed block.

| Field | Values index | Update payload byte offsets | Full-frame offsets | This baseline's preset-detail offsets |
| --- | --- | --- | --- | --- |
| Noise Killer Level | 3 | 10–11 | 14–15 | 31–32 |
| 808 Gain | 2 | 8–9 | 12–13 | 44–45 |
| 808 Vol | 4 | 12–13 | 16–17 | 48–49 |

Preset-detail offsets depend on preceding block lengths and are not universal parameter IDs. Unknown initial/trailing fields remain unlabeled; no enabled flags, units or general model layout are established.

## Exact occurrence provenance

Phone times are source labels without cross-device clock calibration. Intermediate values occur while restoring the sliders:

| Experiment | Value | Phone log time / line | HCI record | CRC trailer |
| --- | --- | --- | --- | --- |
| Level change | 47 | 10:11:51.412 / 555 | 1215 | `7705` |
| Level restoration intermediate | 49 | 10:12:00.771 / 557 | 1219 | `ED22` |
| Level restored | 50 | 10:12:00.861 / 559 | 1222 | `23C2` |
| Gain change | 36 | 10:12:20.991 / 565 | 1230 | `7E10` |
| Gain restoration intermediate | 38 | 10:12:30.385 / 568 | 1235 | `F550` |
| Gain restored | 39 | 10:12:30.471 / 569 | 1238 | `B0F0` |
| Vol change | 97 | 10:12:41.122 / 571 | 1243 | `4F05` |
| Vol restoration intermediate | 99 | 10:12:51.501 / 576 | 1305 | `2967` |
| Vol restored | 100 | 10:12:51.592 / 578 | 1308 | `B0F0` |

**The Gain-restored and Vol-restored frames are byte-for-byte identical:**

```text
AA550E002502010A01000300270064006400B0F0
```

Record **1238 / log line 569** restores Gain after its change; record **1308 / line 578** restores Vol after its change. A deduplicated byte list cannot preserve that distinction. The same full-block baseline bytes also occur in the earlier Tone restoration; the packet has no demonstrated separate Gain/Vol/Tone action identifier.

All nine writes validate CRC-16/GSM over length bytes plus payload, with a big-endian trailer. Incoming `AA5502002601AEF6` (`26 01`) occurs at **1218, 1224, 1226, 1234, 1240, 1242, 1246, 1310, 1312**. It remains an acknowledgement candidate with unproven `01` semantics. Rapid paired restoration writes precede their identical responses, making individual pairing ambiguous. These responses are not independent success confirmation.

## Restoration and limits

`analysis/private/windows-controls-baseline.json` and `windows-controls-restored.json` show wire index **4** and identical **complete 92-byte state and 160-byte preset-detail payloads**. This independently confirms final combined observed restoration. The screenshots show each displayed control restored; no Windows read after each individual phone experiment is claimed.

| Payload | Baseline and restored SHA-256 of decoded bytes |
| --- | --- |
| Current state | `bc565805361362423342fdd441fac9fdb6b2cdb1b2b55f9617a30fa7f12ed5c5` |
| Preset detail | `644dee6d3ad07f3e5c2f6ed7670e37220b28d71892b2605763c820b5e19c30ce` |

Nearby implementation intervals are **Level 47–50, Gain 36–39 and Vol 97–100**. Phone wire evidence observes Level **47/49/50**, Gain **36/38/39**, and Vol **97/99/100**. Separate Windows API reads now observe the missing nearby values **Level 48, Gain 37 and Vol 98**. Together the sources cover all four integers in each bounded interval, without establishing complete device ranges. Existing Threshold 25–28 and Tone 97–100 evidence remains separate.

## Separate Windows API and desktop proof

`analysis/private/windows-controls-combined-roundtrip.json` records **ten API steps** plus baseline and restored snapshots. SHA-256 of the complete source file is `c431e05dce8cfb66e14acb9f01ab7f93c4b76b7b74ac485d43febee46d063387`. The sequence changes Level to 48, changes/restores Threshold while Level remains changed, restores Level, then changes Gain to 37, Tone to 98 and Vol to 98 before restoring those OD values individually. Intermediate snapshots retain the simultaneous pending changes rather than resetting companion fields to factory values.

The final API snapshot exactly matches baseline for the entire **92-byte state and 160-byte detail**, and the source reports `baselineRestored: true`, `saved: false`. The fixture's separate `windowsApiValidation` section stores per-step numeric control summaries and state/detail hashes, not raw private presets or device addresses. This independently observes the nearby mid-values missing from the phone capture.

`analysis/private/desktop-five-controls-hardware.roundtrip.json` reports actual desktop-handler validation with `passed: true`, `baselineRestored: true`, and `failure: null`. Its final recorded stage is **UI combined five-parameter dirty-state update and restore**; the validation also exercised the three added controls individually. Source SHA-256 is `54ae20925ca0dc697f3cde078489b3046ae5540629c38707a4430ed978736974`. The retained baseline/final snapshots have exactly equal full state/detail hashes. A separate fresh verification also confirmed the amplifier's full baseline after these tests.

The curated fixture separates this desktop proof from both phone HCI records and API-step evidence. Neither successful proof interprets `26 01` as a proven success code. The tested implementation remains restricted to the captured firmware/models/configurations and nearby values; arbitrary ranges, other models, rejection responses and persistent preset saving remain unverified.

- [x] Identify the controls through single-control phone changes and restoration.
- [x] Preserve exact occurrence provenance for repeated baseline frames.
- [x] Verify checksums and unchanged companion fields.
- [x] Confirm final full state/detail restoration through an independent Windows read.
- [x] Validate Windows Level, Gain and Vol changes and restoration through fresh full-detail reads, including all five controls changed in combination.
- [x] Validate actual desktop handlers for these controls individually and in a combined five-control hardware round trip; complete observed baseline restored.
- [ ] Determine full ranges, units, rejection behavior and other model/firmware compatibility.
- [ ] Investigate preset-save and power-cycle persistence separately.

Windows edits should remain gated to the captured model/position/configuration, check a fresh expected snapshot, preserve every other byte and establish success through independent readback. **Every test change must be restored** against its exact original state/detail baseline. No automatic ambiguous-write replay, guessed flag editing or acknowledgement-only success is justified.
