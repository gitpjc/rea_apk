# Effect catalog investigation

13 October 2026. **No trusted wire parameter-ID/range/unit catalog was recovered from the bounded packaged-resource search.** The manual provides useful effect names and presentation ordering, but its serial numbers must not be used as protocol identifiers. This result leaves captured preset blocks uninterpreted until code or controlled runtime evidence supplies the mapping.

## Sources and search coverage

`scripts/catalog-inventory.mjs` uses existing Node and Java `jar` to list archive members, extract only selected metadata and hash bytes. Generated evidence is `analysis/generated/catalog/inventory.json`; copies are under that ignored directory. No Python was used or installed for this investigation. Earlier separate inventory work used an existing bundled Python runtime; this report does not imply it was never used elsewhere.

| Source | SHA-256 | Bounded coverage |
| --- | --- | --- |
| `apk/MOOER_iAMP_V1.6.1_2026.06.26.apk` | `7ea003baf4dcb5073174c4f253a25deeb460dff2cdec379ae0ece7c88b931273` | All 787 archive member names; 308 Flutter asset members; selected metadata content. |
| `apk/extracted/base.apk` | `7082248d8eff291748f7296107ba5ee1fdcfad1def9a9b4e1d5bf1a626b0dc68` | All 572 archive member names; 308 Flutter asset members; selected metadata content. |
| Installed MOOER utility | Per-file hashes in `analysis/generated/vendor/inventory.json` and `Vendor-Utility.md` | All 87 external file paths; contents of all three XML/JSON/CSV/text/YAML/database-extension candidates (all were XML). Compiled PE resources and firmware bodies not decoded. |
| `DOCO/MOOER/F15i Li_Manual_EN_V01_2025.06.19.pdf` | `4c2f24cbcb7147e24b9d5c010259e241146db5d5be51dcdeab883beb1a420117` | Text extraction of printed pp.66-81 (PDF pp.69-84); prior app workflow extraction pp.37-65. Text normalization is not a pixel-perfect transcription. |

Both APKs have identical Flutter asset **extension counts**, not necessarily identical complete asset contents: 289 PNG, 8 GIF, 2 JSON, 2 HTML, 2 fragment shaders, 1 binary manifest, 1 compressed notices file, 1 OTF, 1 TTF and 1 JavaScript. Selected metadata hashes below are identical between builds. The different APK builds remain separate inputs.

The complete non-media Flutter member list is:

- `assets/flutter_assets/AssetManifest.bin`
- `assets/flutter_assets/FontManifest.json`
- `assets/flutter_assets/NativeAssetsManifest.json`
- `assets/flutter_assets/NOTICES.Z`
- `assets/flutter_assets/assets/f40/files/user_disclaimer_en.html`
- `assets/flutter_assets/assets/f40/files/user_disclaimer_zh.html`
- `assets/flutter_assets/packages/wakelock_plus/assets/no_sleep.js`

“Non-media” here excludes common image/audio/video/font/shader extensions for triage. This is a filename-based inventory, not proof that every excluded file can contain only media. Notices/disclaimers/wakelock source were not needed for the parameter-catalog question and were not extracted. The binary Flutter asset manifest was retained but not semantically decoded.

## Packaged catalog candidates

| Candidate | Size / SHA-256 | Observed content and usefulness | IDs/ranges/units |
| --- | --- | --- | --- |
| Flutter `FontManifest.json` (both builds) | 208 B / `cd7e03645bc44b2dd47b7cb626f51c4ecbf55a197ab77241628b47ac165fbe21` | MaterialIcons and CupertinoIcons font references. | None found in parsed JSON. |
| Flutter `NativeAssetsManifest.json` (both builds) | 45 B / `9548a31e4a048135c1d94f919328bfb62ae2c7bb3cab96557c7941daa97776cb` | Format version `[1,0,0]`, empty `native-assets` object. | None found in parsed JSON. |
| Flutter `AssetManifest.bin` (both builds) | 26,527 B / `826e0a3c30a86b97231106a74bfcad1cbb19b58ac56354ac97b0a1e2f8f8e4fc` | Binary asset manifest; file inventory already identifies packaged resources. No parameter schema asserted. | Not decoded; no mapping established. |
| Vendor `Firmware/F15i/firmware.xml` | 662 B / `521cfb6047386f51aa27949d72340f3b949a39d75356cc86850cf2ecdf87f3f9` | V1.1.9 release notes, image and EMMC/FLASH firmware filenames. | No effect/parameter definitions found in XML. Bundled version is not live-device version. |
| Vendor `Firmware/F40i/firmware.xml` | 651 B / `880251d5cb885a51ef545312abbba091ff8a90717ecb6bfcd63b588f58557ae7` | F40i V1.1.2 release notes and filenames. | No effect/parameter definitions found in XML; not F15i configuration. |
| Vendor `Language/Language.xml` | 222 B / `4106b817e05101f64b07e6cc83538e30839d09ccbb59599e1c0e340ba9d6c1b9` | Chinese/English translation-file selection. | No effect/parameter definitions found in XML. Translation binaries not decoded. |
| Flutter effect-category PNG filenames | Recorded paths in generated catalog inventory | `assets/flutter_assets/assets/f40/images/f40_fx_` prefixes include `amp`, `cab`, `delay`, `dyna`, `eq`, `filter`, `mod`, `od`, `reverb`. | Presentation candidates only. Filename ordering is not a category-ID mapping. |

No standalone effect/model/parameter JSON, XML, CSV or database file was identified in the specified packaged Flutter assets or installed utility external file inventory. This does **not** establish that the application lacks a catalog: it may be encoded in Dart AOT objects, Qt/native resources, firmware or runtime messages. No cloud/account APIs were queried.

## Manual baseline: label candidates, not protocol identifiers

References use printed pages; PDF page = printed page + 3. Spaces are normalized for readability. “Serial” below means only the manual table's `Serial NO.` within that section; every category/model/parameter wire ID remains **unknown**. Parameter order, numeric range, scale and unit remain unknown unless separately stated.

| Manual section / pages | Printed serial span | Supported label examples | Limits and candidate interpretation |
| --- | --- | --- | --- |
| Dynamic / 66 | 1-4 | Red Comp; Yellow Comp; Noise Gate; Intelligent Noise Gate | Red Comp described as two-parameter compressor; Yellow Comp four parameters; names/parameter counts do not identify parameter fields or scales. |
| Filter / 67 | 1-3 | Touch Wah; Auto Wah; Filter | Effect names only; no cutoff/envelope/rate range or parameter order established. |
| Heading `AMP` / 68-69 | 1-22 | MOOER Pure Boost, Flex Boost; B.K. Butler Tube Drive; IBANEZ TS808; Klon Centaur Gold; ProCo Rat; BOSS Metal Zone; MOOER Solo | Descriptions are boost/drive/distortion models despite heading `AMP`. Preserve source ambiguity rather than infer mobile category IDs. |
| Preamp / 70-74 | 1-55 | Fender 65 Deluxe Reverb (1); Marshall JCM800 (30); Marshall JCM900 (31); Marshall Plexi100 (32); Peavey 5150DS (45); Markbass R50 (53); Ampeg SVT4Pr (54); Aguilar DB750CL (55) | Serial values are not zero/one-based wire IDs. Table has apparent label inconsistencies (e.g. entry11 name versus MARK V description); current app/device must settle actual labels. |
| EQ / 75 | 1-2 | 5-Band Equalizer; Custom Equalizer | Five-band guitar EQ versus four-band customizable-frequency EQ; actual frequencies/gain ranges/serialization unknown. Separate from global EQ. |
| Modulation / 76-77 | 1-17 | Classic Phaser (1), Tremolo (5), Harmonic Transposition (10), Analog Chorus (11), Triple Chorus (12), Pitch Shift (16), Octave (17) | Pitch-related names answer the existence of documented effects, not semitone/mix ranges or recommended chain order. |
| Delay / 78-79 | 1-12 | Digital (1), Analog (2), Echo (3), Tape (4), Modulated (5), Reverse (6), Ping Pong (7), Crystal Modulated (8), Rainbow Modulated (9), Vintage (10), Slow Attack (11), Filtered (12) | Time/feedback/mix limits, tap subdivisions and numeric encodings not specified here. |
| Reverb / 80-81 | 1-9 | Room (1), Hall (2), Plate (3), Spring (4), Modulated (5), Starlight (6), Flanged (7), Reverse (8), Distorted (9) | Decay/pre-delay/mix units and scales unverified. |

The category-local serial spans sum to 124 listed entries, but this is a manual-table count, not the actual installed app/device catalog size. No standalone cabinet table was identified in these effect-description pages, despite a cabinet UI image filename. GNR/GIR/IR sample slots are documented separately on printed p.54 and must not be silently added to this table as built-in models.

## Numerical facts safe to retain separately

These documented user-interface facts help define future experiments; they still do not identify packet offsets or scalar encodings:

| Source | Documented behavior | Wire uncertainty |
| --- | --- | --- |
| Manual p.49 | Mixer channels Bluetooth/USB/Instrument; stated -36 to 0 dB, default -6 dB; Master also present. | Individual/master applicability and dB-to-wire conversion unverified. |
| Manual p.60 | Tuner reference 430-445 Hz, Mute/Bypass. | Reference pitch encoding and mode IDs unknown. |
| Manual p.56 | Input gain upper bound +6 dB, default 0 dB; extracted text omits lower bound. | Do not assume symmetric range until visually/runtime confirmed. |
| Manual p.57 | Global equalizer has four preset frequency points and acts independently of presets. | Frequencies and gain encodings unknown. |

## Next evidence needed to interpret captured preset blocks

1. Trace `package:flutter_iamp_app/device/future_series/f15i/` model/state and BLE helper candidates already retained by Flutter triage; recover actual lookup objects/serialization, with function/data references rather than string adjacency.
2. Record a current-app screenshot of one selected model and all parameter labels/values; correlate the same preset state with a read-only capture. Do not assign IDs from the manual's serial numbers.
3. Vary one reversible parameter at a time only after the established hardware-test gate; capture before/after bytes, acknowledgement and read-back. Separate effect enable/model/parameter fields and verify units, range, defaults and scale.
4. Validate at least two different models in a category before generalizing layout. Unknown fields remain raw data; do not expose guessed sliders in the live editor.
5. Record an independently confirmed catalog with evidence per category/model/parameter and firmware/app build. Keep this document's label candidates separate from that eventual wire schema.

Reproduce the bounded resource inventory with `source scripts/env.sh` followed by `node scripts/catalog-inventory.mjs`. Generated copies are development evidence and are excluded from application release artifacts.
