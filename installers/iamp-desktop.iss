; Build the self-contained win-x64 publish folder before compiling this script.
; See README.md in this directory. Keep AppId stable across all releases.
#ifndef PublishDir
  #define PublishDir SourcePath + "..\artifacts\Iamp.Desktop.next"
#endif
#define AppExe "Iamp.Desktop.exe"
#if !FileExists(PublishDir + "\" + AppExe) || !FileExists(PublishDir + "\Iamp.Desktop.dll")
  #error Publish the Windows x64 application before compiling the installer.
#endif
#if !FileExists(PublishDir + "\coreclr.dll") || !FileExists(PublishDir + "\assets\version_history.json")
  #error The publish folder must contain the self-contained runtime and version history.
#endif
; Read the built assembly, not a separately maintained installer version.
#define VersionQuad GetVersionNumbersString(PublishDir + "\Iamp.Desktop.dll")
#define AppVersion Copy(VersionQuad, 1, RPos(".", VersionQuad) - 1)

[Setup]
AppId={{7E1EA2D7-11FC-43AE-9689-2FE45E2C3129}
AppName=iAMP Desktop
AppVersion={#AppVersion}
AppVerName=iAMP Desktop {#AppVersion}
VersionInfoVersion={#VersionQuad}
DefaultDirName={localappdata}\Programs\iAMP Desktop
DefaultGroupName=iAMP Desktop
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.19041
OutputDir={#SourcePath}
OutputBaseFilename=iAMP-Desktop-{#AppVersion}-Setup-x64
SetupIconFile={#SourcePath}..\assets\app\iamp.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName=iAMP Desktop
Uninstallable=yes
CreateUninstallRegKey=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\iAMP Desktop"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"
Name: "{group}\Uninstall iAMP Desktop"; Filename: "{uninstallexe}"
Name: "{userdesktop}\iAMP Desktop"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon

[InstallDelete]
; Replace the single root shortcut created by releases before 1.41.8.
Type: files; Name: "{userprograms}\iAMP Desktop.lnk"

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch iAMP Desktop"; Flags: nowait postinstall skipifsilent unchecked

; Intentionally no [UninstallDelete] or user-data [Registry] entries.
; Inno removes files/shortcuts it installs. User settings under
; %LOCALAPPDATA%\Iamp.Desktop and user-created exports/backups are retained.
