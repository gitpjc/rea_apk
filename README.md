# rea_apk

## Windows desktop development

Run `bash scripts/build-desktop.sh` from Git Bash for the offline build, tests and self-contained Windows x64 package. Output is staged at `artifacts/Iamp.Desktop.next`; the usual app launch folder is `artifacts/Iamp.Desktop`. This software check needs no phone, amplifier, APK, REA or Python. See [build and release instructions](DOCO/Codex/Build-and-Release.md), [implementation plan](DOCO/Codex/Plan.md) and [user guide](DOCO/Codex/User-Guide.md).

## Pay Store `iAMP` App

[iAMP](https://play.google.com/store/apps/details?id=com.mooer.iamp)  

Current version: `1.6.1 `(Last update: 26 Jun 20026)
Required OS: Android 7.0 and up    

## Codex Prompts

Can REA: https://github.com/morluto/rea/ be used to reverse engineer an Android APK?

**Yes—REA explicitly supports reverse engineering Android APKs.** It uses JADX to inspect the manifest and permissions, search classes, decompile methods into Java or smali, and trace static references. See its [Android analysis guide](https://github.com/morluto/rea/blob/main/docs/android-analysis.md).

Its usefulness depends on your goal:

| Goal | REA support |
|---|---|
| Understand app logic | Class and method inspection through JADX |
| Inspect resources | Separate Apktool integration |
| Investigate a running app | Separate ADB tools for devices, logs and captures |
| Analyze native `.so` libraries | Separate native analysis through Ghidra, IDA or Hopper |

These capabilities are listed in the [repository’s supported targets](https://github.com/morluto/rea#what-you-can-analyze).

**It won’t automatically recover the original Android Studio project.** Decompiled output is a derived representation; even complete output doesn’t guarantee complete semantics or an identical reconstruction. The APK analysis workflow also excludes split APK/AAB handling. [Documented limitations](https://github.com/morluto/rea/blob/main/docs/android-analysis.md#interpret-the-result)

For your Windows environment, the documented requirements are a **full JDK 17+**, the **`jadx-headless-mcp` JAR**, and a compatible REA Windows x64 package. Windows support has a package-version caveat, so we’d check the installed release before setting it up. [Windows setup details](https://github.com/morluto/rea/blob/main/docs/android-analysis.md#supply-tools)

## Documentation References

[REA: Reverse Engineer Anything (GitHub Repo)](https://github.com/morluto/rea)     
[REA: Official Website](https://rea.tools/)   
[Set up REA](https://rea.tools/get-started/)   
[What you can analyze](https://github.com/morluto/rea#what-you-can-analyze)    
[Static Android APK analysis](https://github.com/morluto/rea/blob/main/docs/android-analysis.md)         


## Bash commands to extract apk's

```bash
# Connect your phone then execute the following commands:

# Confirm the phone is connected
adb devices

# Find the package name; replace keyword with an app/developer name
# adb shell pm list packages keyword
adb shell pm list packages iamp

# Get APK paths; replace com.example.app with the package name
adb shell pm path com.mooer.iamp
# Output
# package:/data/app/~~D0wlNIN0DnDbJsB6ETpttA==/com.mooer.iamp-fYY3SxSbtLuZ2GECCG-1Vw==/base.apk
# package:/data/app/~~D0wlNIN0DnDbJsB6ETpttA==/com.mooer.iamp-fYY3SxSbtLuZ2GECCG-1Vw==/split_config.arm64_v8a.apk
# package:/data/app/~~D0wlNIN0DnDbJsB6ETpttA==/com.mooer.iamp-fYY3SxSbtLuZ2GECCG-1Vw==/split_config.en.apk
# package:/data/app/~~D0wlNIN0DnDbJsB6ETpttA==/com.mooer.iamp-fYY3SxSbtLuZ2GECCG-1Vw==/split_config.xxhdpi.apk

# Copy all APKs, including splits, into your project folder
# mkdir -p /d/A_A_Codex/rea_apk/extracted
mkdir -p extracted

while IFS= read -r apk; do
  apk="${apk//$'\r'/}"
  apk="${apk#package:}"
  # [[ -n "$apk" ]] && ./adb.exe pull "$apk" /d/A_A_Codex/rea_apk/extracted/
  [[ -n "$apk" ]] && adb pull "$apk" extracted/
done < <(adb shell pm path com.mooer.iamp)
```

## Setting up REA (TODO)  

## iAMP Desktop App

The app being developed is available here:   

```bash
D:\A_A_Codex\rea_apk\artifacts\Iamp.Desktop\Iamp.Desktop.exe

```

