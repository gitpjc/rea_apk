#!/usr/bin/env bash
# Windows software checks only. Never connects to the phone or amplifier.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
[[ $# -eq 0 ]] || { printf 'Usage: bash scripts/build-desktop.sh\n' >&2; exit 2; }
source scripts/env.sh
node scripts/check-version.mjs
node --test tests/protocol-capture.test.mjs
dotnet restore Iamp.slnx --locked-mode
dotnet build Iamp.slnx -c Release --no-restore
dotnet test Iamp.slnx -c Release --no-build --no-restore --verbosity minimal
# A separate folder avoids overwriting a running or previously tested release.
dotnet publish src/Iamp.Desktop/Iamp.Desktop.csproj -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true -o artifacts/Iamp.Desktop.next
node scripts/check-version.mjs artifacts/Iamp.Desktop.next
printf 'Windows software checks passed. Package: artifacts/Iamp.Desktop.next\n'
printf 'Hardware acceptance and promotion to artifacts/Iamp.Desktop are separate steps.\n'
