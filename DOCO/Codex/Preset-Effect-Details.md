# Preset/Effect Details

Implemented in **1.45.0**, extended with offline names in **1.46.0**, 18 October 2026.

The **Preset/Effect Details** button sits immediately to the right of the dropdown labelled **Preset**. It opens a themed, scrollable read-only report. Presets are grouped numerically by bank (0A–0D, 1A–1D, … 19A–19D), with each slot code and loaded name. Effects are grouped in the existing desktop catalogue order: Dyna, Filter, OD, Amp, EQ, Mod, Delay and Reverb, covering all 124 available built-in models. GIR&IR library access remains excluded.

The report uses all loaded preset names, independent of search/effect filters. Starting with1.46.0, the last complete80-slot name library is saved locally and loaded at startup for offline dropdown browsing and reports. Cached names are identified as last-read data; they do not claim a live connection or active preset. A successful connection refreshes them and confirmed save/rename updates persist. Connect once after upgrading to populate this new cache. It does not fetch or change amplifier data. If names have never been loaded into this cache, the report explains this and still lists the built-in effects. Imported snapshots may provide only one preset; absent names are not invented. The open report is a fixed snapshot of the loaded data when opened.

The **copy icon / Copy report** button copies the entire report as Markdown, including entries outside the visible scroll area. Clipboard contention produces a retry message. Close or Escape dismisses the dialog.

Focused offline checks pass for numeric grouping, unfiltered names, every catalogue model/category, scrolling/read-only behavior, complete copy/busy handling and adjacent responsive placement at four window sizes. Affected preset-sync checks pass. No hardware, user clipboard or preset mutations were needed.

Offline names are stored in `%LOCALAPPDATA%/Iamp.Desktop/preset-names.json`, schema1. Reads are bounded and validated; writes atomically replace a complete80-slot library. Incomplete, duplicate, malformed, unknown-schema or oversized data is rejected. Demo/imported snapshot names do not overwrite it. Installer updates/uninstall retain this user data under the existing policy. Focused persistence/startup, report/search, cache validation, save and preset-sync checks pass without hardware changes.

## Report styling and Markdown — 1.46.1

Presets/Effects headings are bold16px; bank and effect-category headings are bold15px; entries remain13px normal. Copy report exports equivalent Markdown: level-two section headings, level-three bank/category headings and standard bullet lists. Names are escaped so Markdown punctuation remains literal. The complete report, including offline-cache notes, is copied regardless of scrolling. Focused checks cover formatting, all entries, name escaping and clipboard-busy feedback.
