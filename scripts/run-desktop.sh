#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/env.sh"
cd "$IAMP_ROOT"
dotnet run --project src/Iamp.Desktop/Iamp.Desktop.csproj --configuration Release -- "$@"
