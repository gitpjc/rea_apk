#!/usr/bin/env bash
# Software/static smoke checks only; no amplifier writes or live phone capture.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
[[ $# -eq 0 ]] || { printf 'Usage: bash scripts/check.sh\n' >&2; exit 2; }
source scripts/env.sh
python_bin="$(cygpath -u "$IAMP_PYTHON")"
[[ -x "$python_bin" ]] || { printf 'Static binary inventory needs the existing bundled interpreter; do not install Python. Build/test C# separately if unavailable.\n' >&2; exit 1; }
mkdir -p analysis/generated
node --version
npm --version
java --list-modules | grep '^jdk.compiler@'
dotnet --version
rea --version
if [[ "${SKIP_CODEX_DOCTOR:-0}" == 1 ]]; then
  printf 'SKIPPED Codex registration readiness explicitly (SKIP_CODEX_DOCTOR=1); independent checks only.\n' >&2
else
  rea doctor --client codex --json > analysis/generated/rea-doctor.json || {
    printf 'REA Codex readiness FAILED; inspect analysis/generated/rea-doctor.json.\n' >&2
    exit 1
  }
fi
"$python_bin" scripts/apk-inventory.py
"$python_bin" scripts/flutter-triage.py
node scripts/rea-mcp-smoke.mjs
node --test tests/protocol-capture.test.mjs
dotnet build Iamp.slnx -c Release
dotnet test Iamp.slnx -c Release --no-build --verbosity minimal
git diff --check
printf 'Static APK/signing, Flutter-token, separate MCP-client and Windows build checks passed.\n'
printf 'Live amplifier control, backup and actual-chat MCP integration remain separate acceptance gates.\n'
if [[ "${SKIP_CODEX_DOCTOR:-0}" == 1 ]]; then
  printf 'Codex registration readiness was skipped; no configured-chat readiness claim.\n' >&2
fi
