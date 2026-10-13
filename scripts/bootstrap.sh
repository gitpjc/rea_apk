#!/usr/bin/env bash
# Run in Git Bash. Never installs machine-wide tools or deletes directories.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
configure_codex=false
case "${1:-}" in
  '') ;;
  --configure-codex) configure_codex=true ;;
  *) printf 'Usage: bash scripts/bootstrap.sh [--configure-codex]\n' >&2; exit 2 ;;
esac
[[ $# -le 1 ]] || { printf 'Unexpected arguments\n' >&2; exit 2; }
source scripts/env.sh
command -v curl >/dev/null
command -v unzip >/dev/null
command -v sha256sum >/dev/null
command -v sha512sum >/dev/null
mkdir -p .tools/downloads analysis/generated

verify_sha256() {
  printf '%s  %s\n' "$1" "$2" | sha256sum --check --status || {
    printf 'SHA-256 mismatch; retained without overwrite: %s\n' "$2" >&2; exit 1;
  }
}
download_checked() {
  local url="$1" destination="$2" digest="$3"
  if [[ -f "$destination" ]]; then
    verify_sha256 "$digest" "$destination"
  else
    [[ ! -e "$destination.partial" ]] || { printf 'Inspect existing partial download: %s.partial\n' "$destination" >&2; exit 1; }
    curl --fail --location --show-error --output "$destination.partial" "$url"
    verify_sha256 "$digest" "$destination.partial"
    mv -- "$destination.partial" "$destination"
  fi
}

# Pin official distribution and cross-check its pinned digest against SHASUMS.
node_version='24.19.0'
node_archive="node-v$node_version-win-x64.zip"
node_sha256='57f71ab3652e797d84acddc79c81cc9ff1c6ddb2a1974cdb83f00fee9bff4c73'
if [[ -x .tools/node/node.exe ]]; then
  [[ "$(.tools/node/node.exe --version)" == "v$node_version" ]] || {
    printf 'Existing .tools/node version differs; inspect manually, not overwritten\n' >&2; exit 1;
  }
  [[ ! -f ".tools/downloads/$node_archive" ]] || verify_sha256 "$node_sha256" ".tools/downloads/$node_archive"
else
  [[ ! -e .tools/node ]] || { printf 'Existing .tools/node is incomplete; inspect manually\n' >&2; exit 1; }
  curl --fail --location --silent --show-error "https://nodejs.org/dist/v$node_version/SHASUMS256.txt" > .tools/downloads/node-shasums256.txt
  official_sha="$(awk -v name="$node_archive" '$2 == name {print $1}' .tools/downloads/node-shasums256.txt)"
  [[ "$official_sha" == "$node_sha256" ]] || { printf 'Official Node checksum differs from pin\n' >&2; exit 1; }
  download_checked "https://nodejs.org/dist/v$node_version/$node_archive" ".tools/downloads/$node_archive" "$node_sha256"
  [[ ! -e ".tools/downloads/node-v$node_version-win-x64" ]] || { printf 'Inspect existing Node extraction directory\n' >&2; exit 1; }
  unzip -q ".tools/downloads/$node_archive" -d .tools/downloads
  mv -- ".tools/downloads/node-v$node_version-win-x64" .tools/node
fi
source scripts/env.sh
[[ "$(node --version)" == "v$node_version" ]]
printf 'Selected Node %s; npm %s\n' "$(node --version)" "$(npm --version)"

# Use committed dependency integrity locks. Existing differing manifests stop.
mkdir -p .tools/rea
for manifest in package.json package-lock.json; do
  if [[ -f ".tools/rea/$manifest" ]]; then
    cmp -s "tools/rea/$manifest" ".tools/rea/$manifest" || {
      printf 'Existing REA manifest differs: %s; review before replacing\n' "$manifest" >&2; exit 1;
    }
  else
    cp -- "tools/rea/$manifest" ".tools/rea/$manifest"
  fi
done
if [[ -f .tools/rea/node_modules/rea-agents/package.json ]]; then
  node -e 'const fs=require("fs");const lock=require("./tools/rea/package-lock.json");for(const [key,value] of Object.entries(lock.packages)){if(!key)continue;const p=".tools/rea/"+key+"/package.json";if(!fs.existsSync(p)){if(value.optional)continue;throw Error("Missing locked dependency: "+key);}if(JSON.parse(fs.readFileSync(p)).version!==value.version)throw Error("Locked version differs: "+key);}console.log("Existing REA dependencies match locked versions")'
else
  [[ ! -d .tools/rea/node_modules || -z "$(ls -A .tools/rea/node_modules)" ]] || {
    printf 'Incomplete existing REA node_modules; inspect before npm ci\n' >&2; exit 1;
  }
  (cd .tools/rea && npm ci --no-audit --no-fund)
fi

[[ -x "$(cygpath -u "$JAVA_HOME")/bin/javac.exe" ]] || { printf 'Full JDK missing: %s\n' "$JAVA_HOME" >&2; exit 1; }
java --list-modules | grep -q '^jdk.compiler@'
java_feature="$(java -XshowSettings:properties -version 2>&1 | awk '$1 == "java.specification.version" {print $3}')"
[[ "$java_feature" =~ ^[0-9]+$ && "$java_feature" -ge 17 ]] || { printf 'JDK17+ required\n' >&2; exit 1; }
mkdir -p .tools/jadx
download_checked 'https://github.com/1013503897/jadx-headless-mcp/releases/download/v0.7.1/jadx-headless-mcp-0.7.1-all.jar' '.tools/jadx/jadx-headless-mcp-0.7.1-all.jar' '6e5eacf500b64292bfb73c49797c1958f6ee44646e43e868039ae7feb573ff75'
for executable in "$(cygpath -u "$ANDROID_SDK_ROOT")/platform-tools/adb.exe" "$(cygpath -u "$ANDROID_BUILD_TOOLS")/aapt.exe"; do
  [[ -x "$executable" ]] || { printf 'Existing Android tool missing: %s\n' "$executable" >&2; exit 1; }
done

sdk_version="$(node -p 'require("./global.json").sdk.version')"
if [[ -x .tools/dotnet/dotnet.exe ]]; then
  [[ "$(.tools/dotnet/dotnet.exe --version)" == "$sdk_version" ]] || { printf 'Existing local .NET SDK differs; inspect manually\n' >&2; exit 1; }
  if [[ -f .tools/dotnet-sdk.zip && -f .tools/dotnet-download.json ]]; then
    sdk_hash="$(node -p 'require("./.tools/dotnet-download.json").sha512')"
    printf '%s  %s\n' "$sdk_hash" '.tools/dotnet-sdk.zip' | sha512sum --check --status
  fi
else
  [[ ! -e .tools/dotnet || -z "$(ls -A .tools/dotnet)" ]] || { printf 'Local .NET directory is incomplete; inspect manually\n' >&2; exit 1; }
  node scripts/download-dotnet.mjs
  unzip -q .tools/dotnet-sdk.zip -d .tools/dotnet
fi
source scripts/env.sh
rea --version
dotnet --version

if "$configure_codex"; then
  rea setup --client codex --dry-run --json > analysis/generated/rea-setup-preview.json
  printf 'Applying explicitly requested Codex-only registration; preview retained.\n'
  rea setup --client codex --yes --json > analysis/generated/rea-setup-applied.json
  node scripts/configure-rea-env.mjs
  printf 'Restart/reconnect Codex to load the registered tools in the actual chat.\n'
fi
printf 'Bootstrap complete. Run bash scripts/check.sh for static/build/MCP verification.\n'
