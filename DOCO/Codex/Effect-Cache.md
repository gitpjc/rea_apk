# Effect cache, explicit refresh and reset — 1.23.0

15 October 2026. The 124 built-in effect names were already embedded locally. The expensive connection step reread which effects every preset contains. This version persistently caches those identities and reads the full library only on the first cache miss or when the user chooses **Refresh effect names**. Normal reconnect and Refresh amplifier reuse the cache. The active preset still has a fresh complete live read, which updates its search entry.

**Reset** returns the Effect dropdown to All effects and is disabled whenever All effects is selected. It preserves text search. Refresh effect names is enabled only for a valid connected V1.1.9 amplifier, and disabled while busy, disconnected or viewing an imported local snapshot. It does not change pending parameter inputs, recall a preset or save anything. Cancellation/failure retains previous indexed data and releases the failed connection.

## Storage and lifecycle

Cache location: %LOCALAPPDATA%/Iamp.Desktop/EffectCache. Filenames use a SHA-256 key of the amplifier Bluetooth address and firmware; the raw address is not stored. Version-1 JSON contains 80 slot numbers, category/model identities and refresh time. Preset names, parameter values, raw packets and enable flags are omitted. This search-only representation cannot be applied to the amplifier. Disabled and enabled effects remain equally searchable.

Reads are bounded to 128 KiB and validate schema, firmware, complete unique 1–80 coverage, effect count and nonempty effect types. Missing, corrupt, invalid UTF-8, oversized or incompatible files become cache misses. Saves write a unique temporary file then atomically replace the device/firmware cache. If disk writing fails, the app keeps the successful result in memory for the current session and reports that it could not save the cache. Importing a local snapshot uses only that preset and does not overwrite the amplifier cache.

Data from other presets reflects the last cache refresh. After changing effects in another app, choose Refresh effect names to update it. No automatic age/name-change refresh is performed. Other firmware has no verified effect index. There is no shared cache between different amplifier addresses or firmware versions.

## Validation

28 affected cache/search protocol cases pass, covering round trips, disabled inclusion, catalog/search behavior, omission of private content, malformed JSON/schema/firmware/coverage/records and size limits. The full suite was not rerun. Actual persistent-store checks verify reload through a separate store instance, device/firmware separation, atomic temporary-file cleanup, corrupt/invalid-UTF-8/oversize rejection and in-memory retention after disk failure. WPF checks exercise Reset enablement/default/text-search preservation, disconnected refresh availability and both button positions at five viewport sizes down to 620×320, plus affected slider/pending guards. No desktop screenshots.

Actual read-only Windows checks use an isolated cache directory: a cold connection issues **83** detail queries (80 library entries plus initial/before/after live details); a newly created window/store reuses the saved file and issues **1** live detail query, with **zero** library queries; explicit Refresh effect names issues **82** detail queries (80 plus before/after). A pending Threshold input survives manual refresh and is returned locally to its confirmed value without Apply. Reset is disabled on All effects and preserves a British text search. After disconnect, cached filtering remains available while the refresh button disables. A separate fresh BLE read verifies every original state/detail byte unchanged. Private report: analysis/private/effect-cache-windows-report.json, passed=true, baselineRestored=true, failure=null.

This changes filtering/cache behavior only. No parameter changes, preset recalls, saves, sample transfers or Phase 6 work.

## General save integration — current 1.41.2

Successful save/rename updates the destination’s name and effect identities immediately. Save As also rereads the source slot’s stored identities: its abandoned unsaved chain must not remain indexed as stored content. Unconfirmed source data invalidates that entry and requires refresh. This schema-1 search cache stores identities, not parameter values or edit authority; the active preset always receives a fresh complete read.
