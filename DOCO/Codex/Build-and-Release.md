# Windows build and release checks

Updated 14 October 2026; introduced in desktop version 1.2.2.

From the repository root in Git Bash, run:

```bash
bash scripts/build-desktop.sh
```

This uses the existing local Node/.NET runtimes when present, or tools already on PATH. A Windows machine with Git Bash, Node and the SDK selected by `global.json` is required. NuGet access is needed on the first restore. No APK, Python, Java, ADB, REA, phone or amplifier is required. The script sources the shared environment but does not execute its analysis tools or access hardware.

The command checks the semantic version in `Directory.Build.props` and the latest entry in `assets/version_history.json`, validates every history entry, runs the five capture-parser and 71 .NET tests, and builds Release with warnings treated as errors. It restores with locked NuGet dependencies and publishes a self-contained Windows x64 package to `artifacts/Iamp.Desktop.next`. Windows desktop/transport projects explicitly declare the x64 runtime already present in their lock files. The package check verifies the application version in its dependency manifest and exact equality of the shipped history asset.

The script stops on failure. It does not replace the normal launch folder, run device probes, save presets, upload a release or commit files. Keep all files in the output directory together. When preparing a distributable ZIP, archive the complete output directory; the executable alone is insufficient.

The GitHub workflow is disabled at the user's request (repository version 1.40.1, included with desktop 1.41.0). Its definition is retained as `.github/workflows/windows-build.yml.disabled`, which GitHub Actions does not load. Commit and push this change to remove its triggers from the hosted repository. No hosted disable action or push was performed. The local Bash build command remains available. Do not re-enable GitHub automation unless the user requests it.

After successful software checks, close the desktop app before promoting a package. Run the actual offline Version History preview and inspect its output:

```bash
artifacts/Iamp.Desktop.next/Iamp.Desktop.exe --version-history-preview analysis/private/version-history-staging.png
```

Promote the package only after checking the result:

```bash
set -euo pipefail
cp -a artifacts/Iamp.Desktop.next/. artifacts/Iamp.Desktop/
node scripts/check-version.mjs artifacts/Iamp.Desktop
cmp artifacts/Iamp.Desktop.next/Iamp.Desktop.exe artifacts/Iamp.Desktop/Iamp.Desktop.exe
cmp artifacts/Iamp.Desktop.next/Iamp.Desktop.dll artifacts/Iamp.Desktop/Iamp.Desktop.dll
cmp artifacts/Iamp.Desktop.next/Iamp.Transport.Windows.dll artifacts/Iamp.Desktop/Iamp.Transport.Windows.dll
cmp artifacts/Iamp.Desktop.next/Iamp.Protocol.dll artifacts/Iamp.Desktop/Iamp.Protocol.dll
artifacts/Iamp.Desktop/Iamp.Desktop.exe --version-history-preview analysis/private/version-history-published.png
```

These steps do not establish hardware acceptance. For changes to device behavior, follow the relevant hardware test and exact restoration procedure separately. No hardware behavior changed in 1.2.2, and no phone or amplifier command was sent during this increment.

Validation: the complete build command passed locally, with zero build warnings/errors, all 76 existing tests passing and package/history consistency confirmed. Staging and published Version History checks passed at 1.2.2 with four entries and preserved library state. The staging view was visually inspected. Published executable and Desktop/Transport/Protocol DLLs match staging byte for byte. Evidence images: `analysis/private/version-122-staging.png` and `version-122-published.png`.
