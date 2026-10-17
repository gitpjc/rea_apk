# Compact layout and keyboard navigation

## Scope at 17 October 2026 — current application 1.41.2

Current navigation is Amplifier Settings, Amplifier Information, Mixer and Version History. Presets use the compact dropdown/search/filter row, not the original fixed table; checkmarks appear only in the popup. Effects have centered icon labels, grey/color state, no redundant On/Off text and half-width mobile-style colored sliders with clearly bold titles. The duplicated value-summary row is removed. Current full-model and save dialogs are described in User-Guide.md. Earlier layout/test counts below are dated evidence; actual DPI, Narrator, long-session/performance and clean-PC acceptance remain open.

See [Current-Status.md](Current-Status.md) and [Documentation-Index.md](Documentation-Index.md) for current support and remaining gates.

## Retained specification and milestone evidence


Updated 14 October 2026; introduced in version 1.5.0.

The main window now starts within the available Windows work area and allows resizing down to 620×320 logical pixels. The library remains vertically scrollable, and the sidebar also scrolls when the window is short. At narrow widths the title badge and preset search move below their headings, connection inputs shrink and action buttons wrap. Information cards use four, two or one column according to available space. Preset names use the table's available width; the existing 64-pixel index column and index alignment are preserved.

Keyboard navigation:

| Shortcut | Action |
| --- | --- |
| Ctrl+1 | Show Amplifier library |
| Ctrl+2 | Show Version History |
| Ctrl+F | Show Amplifier library, bring preset search into view and select its text |
| Tab / Shift+Tab | Move through standard WPF focusable controls |

Buttons now have a blue border when keyboard-focused. Key inputs and the preset list have accessible names. These shortcuts operate navigation/search only and do not trigger scans, reads, parameter writes, recalls or saves. Navigation retains the displayed snapshot and pending input. Existing dialog Close/Escape behavior is unchanged.

Offline QA runs the actual routed navigation commands, checks state preservation, lays out connection/search/preset controls within horizontal bounds and exports connection/preset views at 1366×768, 911×512, 683×384 and 1920×1080 logical pixels. The middle two approximate the content space of a 1366×768 screen at 150% and 200% scaling. These are 96-DPI offscreen renders, not actual OS DPI switching, monitor migration or physical keyboard/screen-reader tests. Window chrome/taskbar overhead is not represented by those image sizes. Physical DPI, Narrator, tab order/focus visibility and smaller dialogs still require acceptance checks.

```bash
artifacts/Iamp.Desktop/Iamp.Desktop.exe --layout-preview analysis/private/layout-preview
```

This writes connection and preset-view PNGs for each size, using demo data only. It makes no hardware calls. Source release evidence is under `analysis/private/version-150-layout`. Compact and normal renders were visually inspected. All 116 .NET/five Node tests pass with zero Release build warnings/errors; existing offline history, snapshot, comparison and diagnostics checks pass. Published executable and Desktop/Transport/Protocol DLLs match tested staging and packaged history matches source.

This increment changes desktop presentation/navigation only. It does not expand hardware-control ranges or resolve the mobile transfer-screen incident.

Additional offline geometry checks confirm all six Amp inputs/actions remain within the horizontal viewport and scrollable content at 1920×1080, 1366×768, 911×512, 683×384 and 620×320 logical pixels. No screenshots or hardware commands were used for those checks; physical DPI/Narrator acceptance remains open.

## Parameter sliders — 1.21.0

All 18 verified parameter controls now expose a responsive integer slider alongside numeric entry and explicit Apply. Sliders change pending input only, with one-step arrows, ten-step Page Up/Page Down and Home/End. Labels expose parameter/range to accessibility tools. Offline WPF interaction checks cover every target, invalid numeric entry, no device work on slider movement, pending guards, disabled states and five viewport sizes. Every actual Windows slider/Apply path passes a one-step change and immediate original restoration, followed by independent full original readback. See `Parameter-Sliders.md`.

## Preset effect dropdown — 1.22.0

Effect search lists all 124 captured built-in models, includes disabled effects and intersects name/bank/index search. All effects and the search × reset their respective filters independently. The text/effect controls share a responsive row beside the preset selector where space allows, then wrap below it at smaller sizes. Actual offline WPF geometry/filter checks pass at five sizes down to 620×320; no desktop screenshots. See `Preset-Effect-Filter.md`.

## Effect cache controls — 1.23.0

Reset and Refresh effect names appear below the Effect dropdown. Reset disables on All effects and leaves text search intact. Refresh requires a supported live connection; cached filtering stays available offline. Cache status shows whether effects were reused or refreshed. Five-size WPF geometry checks pass for dropdown, search, preset selector and both buttons. See `Effect-Cache.md`.

## Mobile-style half-width adjusters — 1.24.0

Supported effect sliders now use half the effect-panel width, rounded grey/coloured tracks, white thumbs with matching centres and live right-aligned values. Delay uses mobile green. Pointer/key release commits through existing guarded handlers; numeric Apply remains. All-18/five-size WPF geometry and keyboard/no-replay checks plus actual hardware callback/restoration checks pass. See `Mobile-Style-Sliders.md`.

## Consistent setting headings — 1.24.1

The duplicate large-value summary row is removed across effect editors. All parameter labels above sliders are bold. Effects with read-only values use individual bold-labelled rows so their data remains available. Existing offline all-18-control and five-size layout checks pass without hardware commands or interaction changes.

## Stronger setting-label emphasis — 1.24.2

All setting labels now use 14-point extra-bold white Segoe UI rather than 12-point bold muted text. Shared slider headings and read-only rows receive the same treatment. Existing focused all-18/five-size WPF checks pass; no hardware access.

## User interaction policy — 17 October 2026

Ask for help or approval directly in Codex. Before initiating Windows UI automation that would require a new app-permission window, explain and ask here first. Do not repeatedly trigger confusing permission windows; use command-line or user-operated checks when automation access is unavailable. This does not imply that tool permission controls can be suppressed or bypassed.
