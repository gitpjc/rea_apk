# Windows build, focused checks and release packaging

Updated 18 October 2026. Installer and usual desktop launch folder: **1.44.0**. Run commands from the repository root in **Git Bash**. The selected local .NET SDK is pinned in global.json and packages use locked restores. No APK, REA, ADB, Python, phone or amplifier is needed for software-only builds.

## Routine changes

Run the affected tests and build the changed projects. A documentation-only update needs link/consistency/version-history checks; it does not require a software-suite or hardware rerun. For an affected save UI change, for example:

~~~bash
source scripts/env.sh
dotnet build tools/Iamp.Desktop.UiChecks/Iamp.Desktop.UiChecks.csproj -c Release --no-restore
dotnet tools/Iamp.Desktop.UiChecks/bin/Release/net10.0-windows10.0.19041.0/Iamp.Desktop.UiChecks.dll --save
node scripts/check-version.mjs
~~~

Use an appropriate dotnet test filter for changed protocol behavior. Existing UI groups include --save, --mixer, --eq, --amp, --od, --delay, --reverb, --mod --tuner and --drum. Source identifies the exact scope of each group. Do not open a second desktop instance for UI QA while the user's app is still running.

## Complete release check

~~~bash
bash scripts/build-desktop.sh
~~~

This is an explicit **complete-suite** build/test/package command, not the default for every small edit. Use it when requested or when the scope justifies broad release verification. The script validates semantic version/history, runs the capture-parser tests, restores locked dependencies, builds the solution, runs all discovered .NET tests and publishes a self-contained Windows x64 folder to artifacts/Iamp.Desktop.next. Test counts grow; use the command's actual output rather than the historical 71-test count from 1.2.2. It does not run the separate WPF UI-check executable or hardware acceptance automatically.

The script stops on failure. It neither promotes the package to the launch folder nor uploads, commits or saves amplifier presets. Keep the complete output directory together; the executable alone is insufficient. First-time setup/restore may need network access; warmed local checks use existing dependencies.

## Staging and promotion

After relevant checks and any required restoration, close the user's desktop before opening a staging preview. Run one desktop process at a time. The Version History preview exits when complete and uses demo data only:

~~~bash
artifacts/Iamp.Desktop.next/Iamp.Desktop.exe --version-history-preview analysis/private/version-history-staging.png
node scripts/check-version.mjs artifacts/Iamp.Desktop.next
~~~

Review the app-only preview when needed, then promote:

~~~bash
set -euo pipefail
cp -a artifacts/Iamp.Desktop.next/. artifacts/Iamp.Desktop/
node scripts/check-version.mjs artifacts/Iamp.Desktop
cmp artifacts/Iamp.Desktop.next/Iamp.Desktop.dll artifacts/Iamp.Desktop/Iamp.Desktop.dll
cmp artifacts/Iamp.Desktop.next/Iamp.Transport.Windows.dll artifacts/Iamp.Desktop/Iamp.Transport.Windows.dll
cmp artifacts/Iamp.Desktop.next/Iamp.Protocol.dll artifacts/Iamp.Desktop/Iamp.Protocol.dll
~~~

Do not replace locked running binaries. A documentation-only history change may synchronize just assets/version_history.json into staging/published assets without rebuilding production code; rerun the version checker. Application code/config changes require an appropriate semantic version bump. Record all changes in the example history schema. Readable JSON schema 3.0.0 and snapshot/cache schema 1 are independent format versions.

## Automation and acceptance

The GitHub workflow is disabled at the user's request. Its reference definition is .github/workflows/windows-build.yml.disabled; GitHub Actions does not load that filename. Do not re-enable it. This documentation pass does not push, change hosted settings or verify remote workflow state.

The 1.41.2 folder and its 66 history entries have recorded source/package consistency checks. P5.8 production-code-host musician workflows pass; [Musician-Workflow.md](Musician-Workflow.md) documents their exact scope and different build revision metadata. Those checks are not a byte-identity or clean-PC test of that host against the published executable.

Historical 1.2.2 validation passed 76 then-existing tests and preview/binary comparisons at that milestone; those figures are not current suite counts or a new run. No full suite is claimed for 1.41.2's wording-only change or this documentation pass. Installer/MSIX selection, signing, upgrade/uninstall/user-data preservation, licensing and clean-machine actual-amplifier acceptance remain P7.5–P7.8. No final distributed-release acceptance is implied.

## 1.41.3 focused validation

The closed-preset synchronization fix uses `--preset-sync` in the WPF UI-check executable. It covers external adoption, marker consistency, filtering and an actually opened popup. Build and publish only the affected application/check projects; no full-suite rerun is required for this UI correction. Source/staging/published checks verify 67 history entries. Original physical-panel results and the unresolved disconnect are recorded separately in [Physical-Panel-Sync.md](Physical-Panel-Sync.md).

## 1.41.4 read-race validation

Run affected CurrentPresetReadGuardTests, StablePresetReadTests, WorkflowQueueTests, CommandSessionTests and PresetSaveTests (60 cases), plus WPF `--preset-sync`. Build the affected projects and publish the self-contained application; no complete-suite rerun was needed. Source/staging/published consistency checks verify 68 history entries. Hardware evidence uses the production reader; normal published-desktop physical-preset UI acceptance subsequently passes, recorded separately in Physical-Panel-Sync.md.

## Application icon — 1.41.5

Use assets/app/iamp.ico for ApplicationIcon and the embedded WPF window icon. The original mobile PNG, source hashes and developer-only regeneration command are in [icon provenance](../../assets/app/README.md). The future Inno Setup script can reuse the ICO; the user reports Inno Setup is installed, but no installer is created by this icon change. Packaging checks verify69 version-history entries. Remaining interruption/sleep reliability tests are deferred by the user and are not a prerequisite for preparing the installer script; unexecuted acceptance stays documented.

## Inno Setup installer — 1.41.8

The script is [installers/iamp-desktop.iss](../../installers/iamp-desktop.iss); see [installer instructions](../../installers/README.md) for publishing and compilation. It derives its version from the built assembly and packages the self-contained x64 folder, preserving user data during uninstall. Source/staging checks verify 72 history entries. Inno compilation passes; the installer has not been run and the usual desktop folder remains 1.41.5. Installation/upgrade/uninstall lifecycle, actual data preservation, signing and clean-PC acceptance remain open. No full software suite or hardware tests are needed for this packaging-only change.

## Tuner — 1.42.5

132 affected protocol/session/tuner/queue/diagnostic tests and focused WPF `--tuner` checks pass. The normal desktop hardware trace, reference/output controls, verified exit and independent complete baseline equality pass; see [Tuner.md](Tuner.md). No complete-suite rerun is claimed. Source, staging and launch-folder version/history checks pass; compile the current self-contained folder with the existing Inno script. Installation/uninstall acceptance remains the user’s 1.41.8 result.

## Mobile tuner parity correction — 1.42.7

The 1.42.5 raw-as-segment assumption was incorrect;1.42.6 fixes the center and the user confirms actual desktop/amplifier agreement. Direct mobile captures then establish non-linear center spacing, upper/lower Perfect pairs, representative High/Low positions and actual meter colors. Run only TunerReadingTests, TunerCapturedFrameTests (39 cases) and WPF `--tuner` for this display correction. Eighty captured frames and seven independent display observations are retained. Whole state/detail restoration passes after every live comparison; no saves or recalls. No complete-suite rerun or exhaustive physical sweep is claimed. Current source/staging/launch folder and installer use1.42.7.

## Drum and Tools — 1.43.0

Run the affected Drum, TunerEntry and ToolRequest protocol tests (18 cases) plus WPF --drum, --tuner, --mixer and --preset-sync groups. The explicit --drum-check address/private-prefix developer mode opens one normal desktop window, exercises real handlers, restores all temporary values and independently verifies original state/detail and playback. It never saves or recalls a preset. Actual acceptance includes coalesced dial updates and the mobile-observed Drum–Tuner–Drum playback roundtrip. Tests/builds pass; source and self-contained x64 package share version 1.43.0. No full-suite or installer lifecycle rerun is claimed.

## Drum style selector and compact navigation — 1.44.0

Run focused WPF --drum, --tuner and --preset-sync checks for the changed selector/navigation. All 70 button IDs are checked against the captured catalogue; browsing sends no commands. Protocol/transport behavior is unchanged, so no hardware mutation or complete-suite rerun is needed. Publish self-contained x64 and compile the existing Inno script beside its output.
