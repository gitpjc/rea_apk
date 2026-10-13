# Development environment

Verified on 13 October 2026. Run commands in Git Bash from the repository root:

```bash
cd /d/A_A_Codex/rea_apk
source scripts/env.sh
```

`/d/A_A_Codex/rea_apk` is the Git Bash spelling of `D:\A_A_Codex\rea_apk`. Native tools accept Windows absolute paths; `cygpath -m` converts Bash paths when needed. The managed execution sandbox currently prevents Git Bash from starting; authorized commands were run outside that sandbox through the normal approval mechanism, without changing policy.

## Verified tooling

| Component | Selected version/location | Status |
| --- | --- | --- |
| Windows | x64, build 26200 reported by ADB; Windows 11 | Running |
| System Node/npm | Node 22.16.0, npm 10.9.2, `C:/Program Files/nodejs` | Node too old for REA; left unchanged |
| Selected Node | 24.19.0, portable `.tools/node` | Compatible and checksum verified; system installation unchanged |
| REA | 6.4.0, `.tools/rea/node_modules/rea-agents` | CLI and separate MCP client smoke test passed |
| Java | Full OpenJDK 21.0.1, `D:/JAVA_STUFF/OpenJDK/jdk-21.0.1` | `jdk.compiler` present |
| JADX engine | 0.7.1, `.tools/jadx/jadx-headless-mcp-0.7.1-all.jar` | Official release SHA-256 verified |
| ADB | 36.0.2-14143358, `D:/JAVA_STUFF/Android/Sdk/platform-tools` | Wireless phone connection established; endpoint changes observed |
| Android build tools | 36.0.0, `D:/JAVA_STUFF/Android/Sdk/build-tools/36.0.0` | AAPT metadata and apksigner verification passed |
| .NET SDK | 10.0.401, `.tools/dotnet` | Official Windows ZIP SHA-512 verified; probe builds without warnings |
| Visual Studio | Community 18 installed | Not modified; installed workloads not fully audited |
| Python | Codex bundled Python runtime | Static inventory scripts work |
| Bluetooth | Intel Wireless Bluetooth, USB VID 8087 / PID 0033, driver `oem81.inf` | Windows enumeration succeeds; 15-second BLE scan ran |

## Connected hardware

- Amplifier: Windows USB product `F15i`, VID `34DB`, PID `000B`.
- Interfaces: USB Audio (`MI_00`) and HID (`MI_03`). Probe measured 65-byte input and output reports, no feature reports, usage page 1, usage 0. This establishes report metadata, not an application packet format or firmware version.
- No named F15i/MOOER BLE advertisement was observed in the initial 15-second scan. This is a bounded negative observation, not proof that the amplifier lacks BLE.
- Phone: model `M2012K11AG`, Android 13; USB ADB now works reliably. Prior Wi-Fi ports changed repeatedly and one connection went offline. The phone's iAMP app was stopped to release its BLE connection for Windows testing; it can be reopened when another reference capture is needed.
- Amplifier firmware: independent Windows BLE information queries matched the phone's V1.1.2 report and returned all 80 preset names. Only the captured identity and preset-name queries have been sent by the Windows probe; no preset/settings changes, saves or maintenance commands are implemented. Baseline backup/recovery verification remains pending.

## Emulator

An isolated `Iamp_Analysis` virtual device was created under `.tools/avd` using the already-installed Android 34 Google APIs x86-64 image. It has approximately 5 GiB free after initialization. The existing Pixel 8 virtual device was left intact.

The standalone APK initially crashed on x86-64 because `libhtdemucs_native.so` is missing from that ABI in the supplied package. Reinstalling with `--abi arm64-v8a` uses the emulator's installed ARM translation and successfully starts iAMP (home screen visually verified). The emulator is running without a window for ADB-based inspection and does not replace the physical amplifier's Bluetooth connection.

## Python policy

No Python installation was performed. Early offline APK/PE/PDF analysis and ZIP extraction used the preexisting Codex-bundled interpreter. It may be used only where an established library or tested parser provides a clear advantage; routine application work uses Bash, Node and C#. The packet-capture parser/tests now use Node. Python is not a Windows application dependency.

## REA integration

Setup changed only the Codex registration and installed its matching REA skill. It preserved other configuration and created `C:/Users/Paul/.codex/config.toml.rea.backup`. Android environment keys were subsequently added under `[mcp_servers.rea.env]`, with a second backup at `config.toml.iamp-env.backup`.

The server now launches portable `.tools/node/node.exe` and the project-local public REA launcher with `mcp`. `JAVA_HOME` and `REA_JADX_MCP_JAR` are configured in the server environment, not just in a terminal. A scoped setup reapplied the registration after portable Node installation and the Codex doctor check passed.

`scripts/rea-mcp-smoke.mjs` connected a separate MCP client, discovered 170 tools and successfully inspected the phone base APK. This verifies the server's MCP workflow; the current Codex chat still needs a restart/reconnection to load the new registration. P1.4 remains open until tools are present in the actual chat.

Published 6.4.0 does not advertise `identify_flutter_build` or `inspect_dart_aot`; their CLI spellings also return `COMMAND_NOT_FOUND`. Do not use repository-main examples as though they were available in this release.

JADX JAR SHA-256:

```text
6e5eacf500b64292bfb73c49797c1958f6ee44646e43e868039ae7feb573ff75
```

## Repeatable checks

```bash
source scripts/env.sh
rea --version
java --list-modules | rg jdk.compiler
dotnet --version
rea doctor --client codex --json
node scripts/rea-mcp-smoke.mjs
dotnet build Iamp.slnx -c Release
dotnet run --no-build -c Release --project tools/Iamp.DeviceProbe -- usb
dotnet run --no-build -c Release --project tools/Iamp.DeviceProbe -- scan 15
adb devices -l
```

The probe only reads metadata and scans advertisements. `gatt <address>` connects to a specifically selected BLE device and discovers service/characteristic metadata without writing characteristics. Do not pick an unrelated nearby device; identify the amplifier first.

Complete generated evidence lives under ignored `analysis/generated/`; phone/device screenshots and captures belong under ignored `analysis/private/`. Source scripts and sanitized documentation are committed; downloaded APKs/JARs/SDKs and build output are excluded.

## Reproducible local setup scripts

`bash scripts/bootstrap.sh` prepares project-local tools and validates existing installations. It does not alter Codex registration by default, install global runtimes, delete directories or replace mismatching existing tool directories. The recorded first and repeat runs succeeded on 13 October 2026 and reused the existing REA/JADX/.NET installations.

Portable Node **24.19.0** with npm **11.17.0** is now installed under `.tools/node` and takes precedence in `scripts/env.sh`. The archive `node-v24.19.0-win-x64.zip` was downloaded from `https://nodejs.org/dist/v24.19.0/`; its pinned SHA-256 was checked against the official `SHASUMS256.txt` and verified before extraction:

```text
57f71ab3652e797d84acddc79c81cc9ff1c6ddb2a1974cdb83f00fee9bff4c73
```

The archive and official checksum list are retained under `.tools/downloads/`. The bootstrap copies the committed `tools/rea/package.json` and `package-lock.json` only when absent, checks existing files for equality, uses `npm ci` for a missing REA installation, and verifies existing dependency versions against the lock. Existing archive hashes and runtime versions are checked; this is a reproducibility check, not a security audit of every installed byte.

JADX 0.7.1 is downloaded only when absent from the official GitHub release and checked against the JADX SHA-256 in the REA integration section. Existing full JDK 17+ and Android SDK tools are required; set `JAVA_HOME`, `ANDROID_SDK_ROOT`, `ANDROID_BUILD_TOOLS` and `IAMP_PYTHON` before sourcing `scripts/env.sh` to override this PC's paths. Python 3.11+ handles ZIP extraction if needed; the setup does not install a global Python runtime.

.NET follows the SDK version in `global.json` (10.0.401). The download helper verifies the official Microsoft release-metadata SHA-512 and reuses a matching retained SDK archive instead of redownloading. An existing mismatching archive or installed version stops setup for review. The already installed SDK was reused during these checks; no second large SDK download occurred.

Only `bash scripts/bootstrap.sh --configure-codex` applies the Codex-only REA setup and Android environment. It first saves a dry-run preview, preserves unrelated client entries through REA's scoped setup, and uses the existing environment helper. This explicit mode is needed when intentionally refreshing registration after switching from bundled to portable Node; the default bootstrap does not silently repair global configuration. Restart/reconnect the actual chat afterward.

`bash scripts/check.sh` runs strict Codex registration doctor, APK hashes/signature inventory, bounded Flutter-token triage, a separate MCP-client Android inspection and Release solution build. It sends no amplifier commands. A failed Codex doctor fails the default check; optional provider diagnostics are retained in `analysis/generated/rea-doctor.json`. For a deliberately independent software check during registration repair, `SKIP_CODEX_DOCTOR=1 bash scripts/check.sh` explicitly skips that gate and labels it skipped; it does not establish registered-chat readiness. Actual hardware control, backup and current-chat tool availability remain separate plan tasks.

Validation on 13 October 2026: strict `bash scripts/check.sh` passed after the coordinator refreshed the Codex registration for portable Node. All five APK signatures verified; separate MCP smoke discovered 170 tools and inspected the phone base APK; Release build completed with zero warnings/errors. Final bootstrap repeat passed; invoking the .NET helper separately reused the retained 10.0.401 archive after checking the official Microsoft SHA-512, without another SDK download. Bash syntax checks and `git diff --check` passed.
