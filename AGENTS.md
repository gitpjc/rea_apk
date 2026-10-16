
## Overview

The purpose of this project is to use this library:
[REA: Reverse Engineer Anything (GitHub Repo)](https://github.com/morluto/rea)  

to create a Windows Desktop version of this Mobile application:

.\apk\MOOER_iAMP_V1.6.1_2026.06.26.apk

This is some documentation about this Mobile application

.\DOCO\MOOER\F15i Li_Manual_EN_V01_2025.06.19.pdf

## Documentation References

[REA: Reverse Engineer Anything (GitHub Repo)](https://github.com/morluto/rea)     
[REA: Official Website](https://rea.tools/)   
[Set up REA](https://rea.tools/get-started/)   
[What you can analyze](https://github.com/morluto/rea#what-you-can-analyze)    
[Static Android APK analysis](https://github.com/morluto/rea/blob/main/docs/android-analysis.md)

## Security Issue

- Codex may take screenshots of its own application window including the preview browser and applications it is developing for me.  
- Codex must never take screenshots of my computer screen or any other windows (besides the Codex application window and applications it is developing for me) without my explicit permission.   

## Command usage

Please use the Bash shell to run commands (Not PowerShell)

## Python preference

Do not install Python for this project. The existing Codex-bundled Python runtime
may be used for offline analysis only when it provides a clear advantage, such as
established PDF libraries or a tested binary parser. Prefer JavaScript or C# for
routine project tooling. The Windows application must not require Python.


## Python

Do not install Python on this computer without my explicit permission. You may use Python libraries built into Codex if there is a clear advantage to using it in that way for this project.    


## Code Changes

Whenever you make any code changes please do this:

- Bump the version number of this application appropriately in any code or configuration files where this version number is being used.  

### Version Numbering

Use the Semantic Versioning numbering standard for all version numbering  

[Semantic Versioning](https://semver.org/)    

Reason:    

- This is the version numbering standard I always use.   

### Version History file

Every time you make any change write the relevant details to the assets/version_history.json file.    

Refer to assets/version_history_example.json file for the format required














