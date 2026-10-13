#!/usr/bin/env bash
# Optional native-analysis engine; no application runtime dependency or global install.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
source scripts/env.sh
archive='.tools/ghidra.zip'
digest='ddac49f903da9d5bac833e5cc79395098b9c33cfd3279be5f31bd00387d2d4db'
url='https://github.com/NationalSecurityAgency/ghidra/releases/download/Ghidra_12.1.4_build/ghidra_12.1.4_PUBLIC_20260921.zip'
mkdir -p .tools/ghidra analysis/generated
if [[ ! -f "$archive" ]]; then
  [[ ! -e "$archive.partial" ]] || { printf 'Inspect existing partial Ghidra download\n' >&2; exit 1; }
  curl --fail --location --show-error "$url" -o "$archive.partial"
  printf '%s  %s\n' "$digest" "$archive.partial" | sha256sum --check --status
  mv -- "$archive.partial" "$archive"
fi
printf '%s  %s\n' "$digest" "$archive" | sha256sum --check --status
if [[ ! -d .tools/ghidra/ghidra_12.1.4_PUBLIC ]]; then
  unzip -q "$archive" -d .tools/ghidra
fi
source scripts/env.sh
rea doctor --provider ghidra --json > analysis/generated/ghidra-doctor.json
printf 'Verified local Ghidra 12.1.4. Windows REA support is PE-only; Android ELF is not established.\n'
