# iAMP application icon

`iamp.png` is the unmodified 192×192 highest-density launcher artwork from the supplied mobile APK. `iamp.ico` contains Windows frames at16,20,24,32,40,48,64,96,128,192 and256 pixels. Frames are resampled from that artwork;256 is an upscale, not additional source detail. Original colors, background and transparency are preserved. No image generation or redesign was used.

- Source: `apk/MOOER_iAMP_V1.6.1_2026.06.26.apk`, package `com.mooer.iamp`, version1.6.1/code42.
- APK SHA-256: `7ea003baf4dcb5073174c4f253a25deeb460dff2cdec379ae0ece7c88b931273`.
- Manifest: `android:icon="@mipmap/ic_launcher"`, confirmed by REA/JADX evidence `ev_788a75bb8387bd99a0e9fd490af832cd6e27c5ab6f6b51a7c3dc3614010fc615`.
- AAPT resolves the density640 launcher entry to `res/o-.png`; this is the192×192 source. Lower-density declared resources are `res/9w.png`, `res/yn.png`, `res/FS.png` and `res/RJ.png`.
- PNG SHA-256: `05585176be9eedcec77ccc4d5f6e34bbb6c127f0b3c10d624fed75240e208f67`.
- ICO SHA-256: `91354d12cfb819056114ee4003b17c6686f43d2bd833a573d202d0cddcc2318a`.

`scripts/build-app-icon.mjs` rebuilds the ICO using the existing development runtime’s Sharp package. Example from Git Bash:

```bash
source scripts/env.sh
NODE_PATH=C:/Users/Paul/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules node scripts/build-app-icon.mjs
```

The application has no Sharp, Node, Python, APK or REA runtime dependency. MSBuild embeds the ICO as the executable icon and a WPF resource; MainWindow uses that resource explicitly. Future Inno Setup scripts can use `assets/app/iamp.ico` for their setup icon and the installed executable for shortcut icons. This provenance record does not replace the project’s existing redistribution/license review gate.

Validation for1.41.5 passed: all11 PNG-backed ICO entries have the expected dimensions, bounds and alpha; the192px frame preserves the source pixels exactly. REA native-resource evidence `ev_dc59f03addc36cdec0f2068d822c7046e6c289db80939dc6bdceba37ce99da55` reports one complete11-frame icon group in the published apphost, with every embedded frame hash matching the ICO. The WPF constructor/resource load and focused popup UI checks pass with zero build warnings/errors. The normal published desktop was visually inspected: the title bar displays the matching icon and version1.41.5. No amplifier or phone connection was opened for this change.
