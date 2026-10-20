# Desktop settings

Implemented in **1.49.0**, refined in **1.49.1**, 20 October 2026. Open **Settings** immediately below **Export diagnostics** in the sidebar. Settings and Version History hide the amplifier header because these pages do not require an amplifier connection. The Presets page no longer shows the EXPERIMENTAL EDIT badge.

**Preserve window size** uses a toggle on the right of the title/description, matching the reference launcher Material switch. It has a blue selected track and right-positioned thumb when enabled, or a grey outlined track and left-positioned thumb when disabled. Tab/Space and a visible focus outline support keyboard use. Preservation is enabled by default. It remembers the normal window width/height and maximized state, saves changes after a 350 ms pause, and flushes the final size when a real application window closes. Reopening restores the saved size/maximized state. Minimizing is not restored as a startup state; the previous normal/maximized state is retained. Window position is not saved.

Turning preservation off saves that preference immediately, keeps the current window unchanged, stops recording size changes, and uses the existing default window size on the next launch. Turning it on during a session records the current size rather than immediately resizing the window to an old value. Saved dimensions are constrained to the app's 620×320 minimum and available work area when restored; stored dimensions have an upper bound of 5000 logical pixels.

Preferences are local to the Windows user: `%LOCALAPPDATA%/Iamp.Desktop/settings.json`. Schema 1 stores PreserveWindowSize, WindowWidth, WindowHeight and WindowMaximized. Writes use a temporary file and replacement. Missing, malformed, oversized or unsupported settings fall back to defaults. A failed preference write produces an inline message on Settings; it does not block application use. Offline previews do not record window sizes, and demo controls cannot change preferences. The installer retains user settings on uninstall, consistent with the existing installer policy.

Source behavior was checked directly in `D:/A_A_Codex/flutter_web_launcher/lib/screens/settings_screen.dart`, `lib/platform/desktop_window_io.dart`, `lib/app.dart` and `lib/data/app_database.dart`: default-on setting, startup restoration, maximized state and 350 ms resize debounce. iAMP uses WPF and its own existing minimum size/work-area bounds; no Flutter dependency is added.

Validation: isolated settings-store and WPF checks verify default-on, close/reopen dimensions, maximized/minimized behavior, disabled default size/no geometry writes, size bounds, invalid-file fallback, save-failure feedback, sidebar placement, badge removal and three viewport sizes. All-eight-page workspace checks and affected save/cache checks pass. These are offline checks using temporary settings files, not an installed-app restart acceptance claim. No amplifier commands or phone operations were needed.

```bash
source scripts/env.sh
dotnet run --project tools/Iamp.Desktop.UiChecks -- --settings
```
