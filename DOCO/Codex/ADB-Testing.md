# Authorized Android iAMP testing

User instructions recorded 14 October 2026:

- Use ADB to operate iAMP during tests instead of routinely asking the user to manipulate each control.
- The user explicitly permits screenshots of **iAMP on the phone**, for current and future iAMP tests. This does not permit screenshots of the computer screen or other apps.
- The user permits temporarily enabling Android's USB stay-awake setting during tests. Read and retain the original `stay_on_while_plugged_in` value before changing it; preserve other charging-source bits when enabling the USB bit. The user clarified to keep this enabled for the entire ongoing testing/development session, including work and packaging between tests. Restore and verify the original value when the whole session is finished or the user asks, not after individual increments.
- Do not request mobile-screen stability checks unless the user asks.
- Preserve the user's current starting preset and all parameter/effect values, including unsaved changes. Restore after each test and verify complete state/detail payloads independently. Do not substitute an older development baseline for the current one.
- Phase 6 remains excluded unless explicitly instructed. ADB permission alone does not authorize unrelated writes. General preset saving/renaming and its bounded restoration tests were separately authorized and implemented; see [Preset-Save-Rename.md](Preset-Save-Rename.md). Firmware/reset, cloud/account operations and Phase 6 remain outside current authorization.

The existing authorized phone is USB-connected and addressed explicitly during a test. Private captures/tools are under ignored `analysis/private/`. `adb-iamp.mjs` checks the focused package is `com.mooer.iamp` before screenshots or touch input; it refused a lock-screen capture during initial testing. If the phone locks, ask the user to unlock it; do not bypass authentication. iAMP's Flutter UI hierarchy currently exposes no useful control labels, so use a current allowed image to locate controls, never guessed coordinates. Images may be resized for viewing; map coordinates back to the actual image dimensions. Recheck after navigation. Multi-segment ADB touch input was successfully verified for the Mid slider, with pointer-up cleanup.

Git Bash can convert Android absolute paths. Set `MSYS_NO_PATHCONV=1` for commands using `/sdcard/...`; keep temporary dumps private and remove only known files created by the test. No Python installation is needed.

The private `phone-awake.mjs` helper records the original setting in `analysis/private/adb-testing-phone-settings.json`, enables USB stay-awake and verifies restoration. Its `restored` flag must be checked after an interruption before enabling a new test; do not overwrite an outstanding original-value record. A tool/usage interruption is not proof that cleanup ran.

Manual recovery: turn off **Stay awake** in Android **Developer options**. Unplugging USB stops the stay-awake behavior, but the setting itself persists and applies when USB is reconnected. The setting was restored and verified after the initial ADB Mid test.

Initial acceptance: ADB selected Amp, changed Mid 43→42→43, captured the phone app only, closed iAMP for exclusive Windows access, restored the phone's original setting, and verified all original amplifier bytes through an independent connection. No save or computer screenshot occurred.

14 October follow-up: the user explicitly authorized using ADB as needed within mobile iAMP throughout development and testing the desktop iAMP application as required. This does not broaden phone screenshots to other apps or computer screenshots to other windows. Continue restoring all tested amplifier settings and keeping USB stay-awake enabled across the ongoing session.

## Current session policy — 17 October 2026

Keep only one desktop instance running and one amplifier controller connected at a time. Operate iAMP through ADB when available; a lock screen requires the user to unlock it. Scroll every settings page to inspect all controls and groups. USB stay-awake remains enabled across this ongoing development/testing session; the retained original value is 0 and its outstanding restoration flag is false. The initial Mid-test restoration described above is historical, not proof that the current session setting has been restored. Restore and verify the retained original when the whole session ends or the user requests it. No ADB or phone settings were changed for this documentation pass.
