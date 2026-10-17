#!/bin/sh
# Build and run the full suite, including the full-boot tier (SteamEngine.Tests.Protocol,
# real AuxiliaryServer + GameCore per test class, ~15 min).
#
#   scripts/test-full.sh                # fast tier, then each full-boot class on its own
#   scripts/test-full.sh <filter>       # one dotnet test run with --filter,
#                                       #   e.g. "FullyQualifiedName~LevelingTests"
#
# The full-boot fixture listens on fixed ports 2593/2595, so only one run may go at a
# time on this machine: a lock in /tmp serializes runs across all worktrees (waits up
# to LOCK_WAIT seconds, default 1800).
#
# Hangs: a run that goes quiet is cut by --blame-hang after HANG_TIMEOUT (3min), without
# a dump (a full dump of testhost + servers is many GB). Whole-suite runs hang often
# (inherited flake, issue #100: a different class each time), so without a filter
# every full-boot class runs in its own dotnet test, and a class that hung or crashed
# is retried once. A plain test failure is not retried. Ends with a per-class summary;
# exit 1 if anything failed or hung twice. See docs/wiki/Process/Dev-Process.md,
# "Flaky full-boot tests".
set -eu

root=$(cd "$(dirname "$0")/.." && pwd)
LOCK=/tmp/moria-fullboot.lock
WAIT="${LOCK_WAIT:-1800}"

waited=0
until mkdir "$LOCK" 2>/dev/null; do
  holder=$(cat "$LOCK/pid" 2>/dev/null || echo "")
  if [ -n "$holder" ] && ! kill -0 "$holder" 2>/dev/null; then
    echo "test-full: removing stale lock of dead pid $holder" >&2
    rm -rf "$LOCK"; continue
  fi
  [ "$waited" -ge "$WAIT" ] && { echo "test-full: lock held by pid $holder for ${WAIT}s, giving up" >&2; exit 1; }
  [ "$waited" -eq 0 ] && echo "test-full: another full-boot run holds the lock (pid $holder), waiting" >&2
  sleep 10; waited=$((waited + 10))
done
echo $$ > "$LOCK/pid"
trap 'rm -rf "$LOCK"' EXIT INT TERM

if lsof -nP -iTCP:2593 -iTCP:2595 -sTCP:LISTEN >/dev/null 2>&1; then
  echo "test-full: ports 2593/2595 are busy (a local server from run-local.sh?). Stop it first:" >&2
  lsof -nP -iTCP:2593 -iTCP:2595 -sTCP:LISTEN >&2
  exit 1
fi

cd "$root/moriaengine"
pwsh ./build.ps1 -Configuration Debug >/dev/null 2>&1 || { echo "test-full: build failed, run scripts/test-fast.sh for the log" >&2; exit 1; }
cd "$root"
HANG="${HANG_TIMEOUT:-3min}"
run() {  # filter → exit 0 pass, 1 fail, 2 hang/crash; output to $out
  dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug \
    --blame-hang --blame-hang-timeout "$HANG" --blame-hang-dump-type none \
    --filter "$1" >"$out" 2>&1 && return 0
  if grep -q "test run was aborted" "$out"; then
    # a hung run can leave the fixture's servers behind on the ports
    lsof -t -nP -iTCP:2593 -iTCP:2595 -sTCP:LISTEN 2>/dev/null | xargs kill 2>/dev/null
    sleep 2; return 2
  fi
  return 1
}
out=$(mktemp)
trap 'rm -rf "$LOCK" "$out" moriaengine/Tests/TestResults' EXIT INT TERM

if [ $# -ge 1 ]; then
  set +e; run "$1"; rc=$?; set -e
  cat "$out"
  [ "$rc" -eq 2 ] && echo "test-full: run hung or crashed (known flake, issue #100) — retry once" >&2
  exit $(( rc > 0 ))
fi

summary=""; bad=0
add() { summary="$summary
  $1"; }

set +e
run "FullyQualifiedName!~SteamEngine.Tests.Protocol"; rc=$?
[ "$rc" -eq 0 ] && add "pass  fast tier" || { add "FAIL  fast tier"; cat "$out"; bad=1; }

classes=$(dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug --list-tests 2>/dev/null \
  | sed -nE 's/^ +(SteamEngine\.Tests\.Protocol\.[A-Za-z0-9_.]+)\.[A-Za-z0-9_]+(\(.*)?$/\1/p' | sort -u)
for c in $classes; do
  short=${c#SteamEngine.Tests.Protocol.}
  tries=1
  run "FullyQualifiedName~$c."; rc=$?
  if [ "$rc" -eq 2 ]; then tries=2; run "FullyQualifiedName~$c."; rc=$?; fi
  n=$(sed -nE 's/.*Passed: +([0-9]+).*/\1/p' "$out" | tail -1)
  case "$rc" in
    0) [ "$tries" -eq 2 ] && add "pass  $short (${n:-?} tests, after a hang)" || add "pass  $short (${n:-?} tests)" ;;
    1) add "FAIL  $short"; bad=1; grep -A2 -E "^ +Failed " "$out" | head -30 ;;
    2) add "HANG  $short (twice)"; bad=1 ;;
  esac
done
set -e
echo "test-full: summary$summary"
exit "$bad"
