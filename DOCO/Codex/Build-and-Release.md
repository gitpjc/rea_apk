# Windows build, focused checks and release packaging

Updated 17 October 2026. Published release: **1.41.2**. Run commands from the repository root in **Git Bash**. The selected local .NET SDK is pinned in global.json and packages use locked restores. No APK, REA, ADB, Python, phone or amplifier is needed for software-only builds.

## Routine changes

Run the affected tests and build the changed projects. A documentation-only update needs link/consistency/version-history checks; it does not require a software-suite or hardware rerun. For an affected save UI change, for example:

~~~bash
source scripts/env.sh
dotnet build tools/Iamp.Desktop.UiChecks/Iamp.Desktop.UiChecks.csproj -c Release --no-restore
dotnet tools/Iamp.Desktop.UiChecks/bin/Release/net10.0-windows10.0.19041.0/Iamp.Desktop.UiChecks.dll --save
node scripts/check-version.mjs
~~~

Use an appropriate dotnet test filter for changed protocol behavior. Existing UI groups include --save, --mixer, --eq, --amp, --od, --delay, --reverb and --mod. Source identifies the exact scope of each group. Do not open a second desktop instance for UI QA while the user's app is still running.

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
