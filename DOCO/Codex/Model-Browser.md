# Read-only model browser investigation

14 October 2026, current desktop release 1.21.1, connected F15i V1.1.9. Authorized ADB screenshots inspect the mobile effect-model browser without selecting any model row. Only category navigation and list scrolling are performed. A fresh independent Windows read matches every original state/detail byte afterward. No saves, recalls, effect changes or parameter changes.

The picker has a category column and numbered model rows. The Amp category also has an Amp dropdown header; its alternate options were not opened. Captured categories are Dyna, OD, Amp, Delay and Reverb. This is a **bounded partial UI catalog**, not a protocol catalog or proof of all available models. Names and displayed ordinals are preserved in tests/fixtures/f15-model-browser-observations.json with capture digests. Raw phone images/logs remain private.

| Existing verified model | Displayed ordinal | Independently known wire model |
| --- | --- | --- |
| Dyna / Noise Killer | 03 | 2 |
| OD / 808 | 04 | 3 |
| Amp / PLX 100 | 32 | 31 |
| Delay / Analog | 02 | 1 |
| Reverb / Hall | 02 | 1 |

For these five existing models, the wire value is one below the displayed ordinal. That is a bounded correspondence, **not permission to assume the formula for other entries**. Reverb Shimmer (06) is visibly greyed out; the reason and device support are unverified. The desktop must not present it as an enabled supported choice based on its name alone.

The next model-selection implementation must first capture one alternate-model change and exact restoration, determine the resulting complete block/defaults, verify source/target layouts and all preserved fields, then repeat through the Windows control with independent original restoration. Existing verified parameter controls must remain guarded on unknown algorithms. Start with a small category such as Dyna or Reverb; do not combine model replacement with saving or Phase 6 work. This investigation does not enable new model writes.
