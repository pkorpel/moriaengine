#!/bin/sh
# Build the solution and run the fast test tier (no server processes, ~30 s).
# Same steps as CI's build-and-unit-test job; the pre-push hook runs it.
#
#   scripts/test-fast.sh                # build + fast tier
#   scripts/test-fast.sh <filter>       # extra dotnet test --filter, ANDed with the tier filter
set -eu

root=$(cd "$(dirname "$0")/.." && pwd)
command -v pwsh >/dev/null || { echo "test-fast: pwsh missing (build.ps1 needs PowerShell 7: brew install powershell, or see learn.microsoft.com/powershell for Linux)" >&2; exit 1; }

cd "$root/moriaengine"
pwsh ./build.ps1 -Configuration Debug >"${TMPDIR:-/tmp}/moria-build.$$.log" 2>&1 || {
  tail -40 "${TMPDIR:-/tmp}/moria-build.$$.log" >&2
  echo "test-fast: build failed (full log ${TMPDIR:-/tmp}/moria-build.$$.log)" >&2
  exit 1
}
rm -f "${TMPDIR:-/tmp}/moria-build.$$.log"

filter="FullyQualifiedName!~SteamEngine.Tests.Protocol"
[ $# -ge 1 ] && filter="$filter&($1)"
cd "$root"
exec dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug --filter "$filter"
