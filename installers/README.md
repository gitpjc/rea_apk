# iAMP Desktop Windows installer

`iamp-desktop.iss` packages the self-contained Windows x64 application using the
existing Inno Setup 6 installation. No additional runtime installation is needed.
The installer version comes from the published application's assembly version;
there is no separate version number to update in the script.

Run from the repository root in Git Bash:

```bash
source scripts/env.sh
node scripts/check-version.mjs
dotnet publish src/Iamp.Desktop/Iamp.Desktop.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts/Iamp.Desktop.next
node scripts/check-version.mjs artifacts/Iamp.Desktop.next
"/c/Users/Paul/AppData/Local/Programs/Inno Setup 6/ISCC.exe" installers/iamp-desktop.iss
```

Alternatively open the script in the Inno Setup Compiler and select Compile.
Use the appropriate compiler path on other developer machines. To package a
different validated publish folder, pass `/DPublishDir="D:\absolute\publish"`
to ISCC (set `MSYS_NO_PATHCONV=1` when passing Windows-style switches in Bash).
Do not point it at a build folder or copy only the executable.

The output is `installers/iAMP-Desktop-<version>-Setup-x64.exe`, beside the `.iss` script.
Installer compilation does not install, uninstall, launch or connect the app.

## Installation behavior

- Windows x64, Windows 10 build 19041 or newer; ARM64 acceptance is not claimed.
- Per-user installation to `%LOCALAPPDATA%\Programs\iAMP Desktop`, without an
  administrator prompt. The directory can be changed in Setup.
- Start menu shortcut, optional desktop shortcut and Windows installed-apps entry.
- Original mobile iAMP artwork on Setup, the application and its shortcuts.
- Stable AppId and remembered install location support upgrades in place.
  Close the running app when Setup requests it; automatic restart is disabled.
- Uninstall removes installed application files and shortcuts. It preserves
  `%LOCALAPPDATA%\Iamp.Desktop`, user-created exports and backups. There is no
  recursive user-data cleanup or automatic amplifier operation.
- Launch after installation is optional and unchecked by default.

## Validation boundary

The script is compiled against the version-checked self-contained package.
Installation, upgrade from a prior installed release, uninstall, actual
user-data preservation and clean-PC operation require separate lifecycle
acceptance. No certificate/signing configuration is included. Compilation alone
does not establish those results. Do not silently run the installer against the
user's development installation to test them.
