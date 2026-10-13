#!/usr/bin/env bash
# Source from Git Bash. This does not modify machine-wide installations.
IAMP_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export IAMP_ROOT
if [[ -x "$IAMP_ROOT/.tools/node/node.exe" ]]; then
  export PATH="$IAMP_ROOT/.tools/node:$PATH"
elif [[ -x '/c/Users/Paul/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe' ]]; then
  export PATH="/c/Users/Paul/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin:$PATH"
fi
if [[ -d "$IAMP_ROOT/.tools/dotnet" ]]; then
  export DOTNET_ROOT="$(cygpath -m "$IAMP_ROOT/.tools/dotnet")"
  export PATH="$IAMP_ROOT/.tools/dotnet:$PATH"
fi
export JAVA_HOME="${JAVA_HOME:-D:/JAVA_STUFF/OpenJDK/jdk-21.0.1}"
export ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-D:/JAVA_STUFF/Android/Sdk}"
export ANDROID_BUILD_TOOLS="${ANDROID_BUILD_TOOLS:-$ANDROID_SDK_ROOT/build-tools/36.0.0}"
export IAMP_PYTHON="${IAMP_PYTHON:-C:/Users/Paul/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe}"
export REA_JADX_MCP_JAR="$(cygpath -m "$IAMP_ROOT/.tools/jadx/jadx-headless-mcp-0.7.1-all.jar")"
if [[ -d "$IAMP_ROOT/.tools/ghidra/ghidra_12.1.4_PUBLIC" ]]; then
  export GHIDRA_INSTALL_DIR="$(cygpath -m "$IAMP_ROOT/.tools/ghidra/ghidra_12.1.4_PUBLIC")"
fi
export PATH="$(cygpath -u "$JAVA_HOME")/bin:$(cygpath -u "$ANDROID_SDK_ROOT")/platform-tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_HOME="$(cygpath -m "$IAMP_ROOT/.tools/dotnet-home")"
export NUGET_PACKAGES="$(cygpath -m "$IAMP_ROOT/.tools/nuget")"
rea() { node "$IAMP_ROOT/.tools/rea/node_modules/rea-agents/scripts/rea.mjs" "$@"; }
