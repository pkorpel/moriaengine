#!/usr/bin/env bash
# Build and run the full suite, including the full-boot tier (SteamEngine.Tests.Protocol,
# real AuxiliaryServer + GameCore per test class).
#
#   scripts/test-full.sh                # fast tier, then each full-boot class on its own
#   scripts/test-full.sh <filter>       # one dotnet test run with --filter,
#                                       #   e.g. "FullyQualifiedName~LevelingTests"
#   scripts/test-full.sh --kill-stale   # kill leftovers of an earlier run (servers on the
#                                       #   ports, orphaned dotnet test/testhost), then exit
#
# You always see what's going on (#785): every test result is printed the moment it's in,
# a test running for a while gets a "still running (Ns)" line every 15 s, the server pair's
# boot/ready/unresponsive events are echoed, and a heartbeat line comes at least every
# HEARTBEAT_SECONDS (20). Nothing waits silently:
#   - a test running longer than STUCK_SECONDS (120) is killed together with its whole
#     process tree and reported as STUCK with its name;
#   - once the server pair dies or stops answering logins for 30 s, the remaining tests of
#     that class fail within seconds (ServerHealth in the test project) instead of retrying
#     for minutes each;
#   - the servers exit by themselves if the test process dies (ParentProcessWatchdog), and
#     stopping this script (Ctrl+C, kill, or its parent going away) kills the whole run.
#
# The full-boot fixture listens on fixed ports 2593/2595, so only one run may go at a
# time on this machine: a lock in /tmp serializes runs across all worktrees (waits up
# to LOCK_WAIT seconds, default 1800); the fixture itself also refuses to boot while
# another run holds the ports. Without a filter every full-boot class runs in its own
# dotnet test; a class whose run crashed is retried once, a STUCK or failing one is not.
# Ends with a per-class summary; exit 1 if anything failed, got stuck or crashed twice.
# See docs/wiki/Process/Dev-Process.md, "Flaky full-boot tests".
set -u

root=$(cd "$(dirname "$0")/.." && pwd)
LOCK=/tmp/moria-fullboot.lock
WAIT="${LOCK_WAIT:-1800}"
STUCK="${STUCK_SECONDS:-120}"
HEARTBEAT="${HEARTBEAT_SECONDS:-20}"
HANG="${HANG_TIMEOUT:-3min}"   # dotnet test's own backstop, in case this script's watch misses something

port_holders() { lsof -t -nP -iTCP:2593 -iTCP:2595 -sTCP:LISTEN 2>/dev/null; }

# Linux only: dotnet test / testhost / server processes left behind by an earlier run
stray_processes() {
  [ -d /proc ] || return 0
  for p in /proc/[0-9]*; do
    pid=${p#/proc/}
    [ "$pid" = "$$" ] && continue
    cmd=$(tr '\0' ' ' < "$p/cmdline" 2>/dev/null) || continue
    case "${cmd%% *}" in   # only real dotnet/testhost processes, not shells that merely mention these names
      *dotnet|*testhost*) ;;
      *) continue ;;
    esac
    case "$cmd" in
      *SteamEngine.Tests*|*SteamEngine.GameCore.dll*|*SteamEngine.AuxiliaryServer.dll*) echo "$pid $cmd" ;;
    esac
  done
}

kill_stale() {
  holders=$(port_holders)
  strays=$(stray_processes)
  if [ -z "$holders" ] && [ -z "$strays" ]; then
    echo "test-full: nothing left over"; return 0
  fi
  [ -n "$strays" ] && echo "test-full: killing leftover processes:" && echo "$strays" | cut -c1-160 | sed 's/^/  /'
  # shellcheck disable=SC2086
  kill $holders $(echo "$strays" | cut -d' ' -f1) 2>/dev/null
  sleep 2
  # shellcheck disable=SC2086
  kill -9 $(port_holders) $(stray_processes | cut -d' ' -f1) 2>/dev/null
  rm -rf "$LOCK"
  echo "test-full: done"
}

if [ "${1:-}" = "--kill-stale" ]; then kill_stale; exit 0; fi

waited=0
until mkdir "$LOCK" 2>/dev/null; do
  holder=$(cat "$LOCK/pid" 2>/dev/null || echo "")
  if [ -n "$holder" ] && ! kill -0 "$holder" 2>/dev/null; then
    echo "test-full: removing stale lock of dead pid $holder" >&2
    rm -rf "$LOCK"; continue
  fi
  [ "$waited" -ge "$WAIT" ] && { echo "test-full: lock held by pid $holder for ${WAIT}s, giving up" >&2; exit 1; }
  [ "$waited" -eq 0 ] && echo "test-full: another full-boot run holds the lock (pid $holder), waiting - status every 30s" >&2
  [ "$waited" -gt 0 ] && [ $((waited % 30)) -eq 0 ] && echo "test-full: still waiting for pid $holder (${waited}s)" >&2
  sleep 10; waited=$((waited + 10))
done
echo $$ > "$LOCK/pid"

if [ -n "$(port_holders)" ]; then
  echo "test-full: ports 2593/2595 are busy (a local server from run-local.sh, or a leftover run). Holders:" >&2
  lsof -nP -iTCP:2593 -iTCP:2595 -sTCP:LISTEN >&2
  echo "test-full: stop it, or run scripts/test-full.sh --kill-stale" >&2
  rm -rf "$LOCK"; exit 1
fi
if [ -n "$(stray_processes)" ]; then
  echo "test-full: an earlier test run is still alive:" >&2
  stray_processes | cut -c1-160 | sed 's/^/  /' >&2
  echo "test-full: run scripts/test-full.sh --kill-stale first" >&2
  rm -rf "$LOCK"; exit 1
fi

out=$(mktemp)
progress=$(mktemp)
export STEAMENGINE_TEST_PROGRESS_FILE="$progress"
current=""   # pid (= process group, see set -m) of the dotnet test being watched

stop_current() {
  [ -n "$current" ] || return 0
  kill -TERM -- "-$current" 2>/dev/null
  for _ in 1 2 3; do kill -0 "$current" 2>/dev/null || break; sleep 1; done
  kill -KILL -- "-$current" 2>/dev/null
  wait "$current" 2>/dev/null
  current=""
  # shellcheck disable=SC2046
  kill $(port_holders) 2>/dev/null
}
cleanup() { stop_current; rm -rf "$LOCK" "$out" "$progress" "$root/moriaengine/Tests/TestResults"; }
trap cleanup EXIT
trap 'echo "test-full: stopped - killing the run" >&2; exit 130' INT TERM HUP
set -m   # every background job gets its own process group: one kill takes dotnet test, testhost and both servers

cd "$root/moriaengine"
echo "test-full: building..."
pwsh ./build.ps1 -Configuration Debug >/dev/null 2>&1 || { echo "test-full: build failed, run scripts/test-fast.sh for the log" >&2; exit 1; }
cd "$root"

now() { date +%s; }
short() { echo "${1#SteamEngine.Tests.}"; }
say() { echo "$(date +%H:%M:%S)  $*"; last_said=$(now); }

# drain <label>: echoes whatever the run and the server pair wrote since the last call;
# returns 3 if a test has been running longer than STUCK_SECONDS (after killing the run)
drain() {
  local label=$1 line n test elapsed h m s
  n=$(wc -l < "$progress")
  if [ "$n" -gt "$pseen" ]; then
    while IFS= read -r line; do say "  srv   ${line#* }"; done < <(tail -n "+$((pseen + 1))" "$progress" | head -n "$((n - pseen))")
    pseen=$n
  fi
  n=$(wc -l < "$out")
  [ "$n" -gt "$seen" ] || return 0
  while IFS= read -r line; do
    case "$line" in
      *"  Passed SteamEngine.Tests."*)
        passed=$((passed + 1)); line=${line#*Passed }; say "  pass  $(short "$line")" ;;
      *"  Failed SteamEngine.Tests."*)
        failed=$((failed + 1)); line=${line#*Failed }; say "  FAIL  $(short "$line")" ;;
      *"[Long Running Test] '"*)
        test=${line#*\[Long Running Test\] \'}; test=${test%%\'*}
        elapsed=${line##*Elapsed: }; h=${elapsed%%:*}; m=${elapsed#*:}; m=${m%%:*}; s=${elapsed##*:}; s=${s%%.*}
        elapsed=$((10#$h * 3600 + 10#$m * 60 + 10#$s))
        if [ "$elapsed" -ge "$STUCK" ]; then
          seen=$n
          say "  STUCK $(short "$test") - running ${elapsed}s (limit STUCK_SECONDS=$STUCK), killing $label"
          stop_current; return 3
        fi
        say "  ...   $(short "$test") still running (${elapsed}s)" ;;
    esac
  done < <(tail -n "+$((seen + 1))" "$out" | head -n "$((n - seen))")
  seen=$n
  return 0
}

# watch <pid> <label>: echoes progress while the run lives, with a heartbeat;
# returns 3 if it had to kill a stuck test
watch() {
  local pid=$1 label=$2 started
  seen=0; pseen=0; passed=0; failed=0
  started=$(now); last_said=$started
  while kill -0 "$pid" 2>/dev/null; do
    if ! kill -0 "$PPID" 2>/dev/null; then   # whoever started us is gone (e.g. a killed wrapper shell)
      echo "test-full: parent process gone - killing the run" >&2; exit 130
    fi
    drain "$label" || return 3
    if [ $(( $(now) - last_said )) -ge "$HEARTBEAT" ]; then
      say "  ...   $label: $(( $(now) - started ))s, $passed passed, $failed failed so far"
    fi
    sleep 1
  done
  drain "$label"   # whatever came in after the last look
  return 0
}

run() {  # filter label → 0 pass, 1 fail, 2 hang/crash, 3 stuck; output in $out
  # shellcheck disable=SC2046
  kill $(port_holders) 2>/dev/null   # leftovers of a previous class can't squat the ports
  : > "$out"; : > "$progress"
  dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug \
    --logger "console;verbosity=normal" \
    --blame-hang --blame-hang-timeout "$HANG" --blame-hang-dump-type none \
    --filter "$1" >"$out" 2>&1 &
  current=$!
  watch "$current" "$2"; local w=$?
  [ "$w" -eq 3 ] && return 3
  wait "$current"; local rc=$?
  current=""
  [ "$rc" -eq 0 ] && return 0
  if grep -q "test run was aborted" "$out"; then
    # shellcheck disable=SC2046
    kill $(port_holders) 2>/dev/null; sleep 2; return 2
  fi
  return 1
}

logs="${TMPDIR:-/tmp}/moria-test-full"   # full dotnet test output of every class that didn't pass
rm -rf "$logs"; mkdir -p "$logs"
keep_log() { cp "$out" "$logs/$1.log"; echo "$logs/$1.log"; }

failure_details() {  # first lines of each failure's message
  grep -A3 -E "^ +Failed SteamEngine" "$out" | grep -v -E "^ +Stack Trace|^--$" | cut -c1-200 | head -40
}

if [ $# -ge 1 ]; then
  say "test-full: $1"
  run "$1" "$1"; rc=$?
  case "$rc" in
    0) say "test-full: passed" ;;
    1) failure_details; say "test-full: FAILED - full log: $(keep_log run)" ;;
    2) say "test-full: run hung or crashed - retry once. Full log: $(keep_log run)" ;;
    3) say "test-full: STUCK - full log: $(keep_log run)" ;;
  esac
  exit $(( rc > 0 ))
fi

summary=""; bad=0
add() { summary="$summary
  $1"; }

say "test-full: fast tier"
run "FullyQualifiedName!~SteamEngine.Tests.Protocol" "fast tier"; rc=$?
if [ "$rc" -eq 0 ]; then add "pass  fast tier"; else add "FAIL  fast tier - $(keep_log fast-tier)"; failure_details; bad=1; fi

classes=$(dotnet test moriaengine/Tests/SteamEngine.Tests.csproj --no-build -c Debug --list-tests 2>/dev/null \
  | sed -nE 's/^ +(SteamEngine\.Tests\.Protocol\.[A-Za-z0-9_.]+)\.[A-Za-z0-9_]+(\(.*)?$/\1/p' | sort -u)
total=$(echo "$classes" | grep -c .)
i=0
for c in $classes; do
  i=$((i + 1))
  name=${c#SteamEngine.Tests.Protocol.}
  say "test-full: [$i/$total] $name"
  class_started=$(now)
  tries=1
  run "FullyQualifiedName~$c." "$name"; rc=$?
  if [ "$rc" -eq 2 ]; then tries=2; say "  ...   $name crashed - retrying once"; run "FullyQualifiedName~$c." "$name"; rc=$?; fi
  n=$(sed -nE 's/.*Passed: +([0-9]+).*/\1/p' "$out" | tail -1)
  took="$(( $(now) - class_started ))s"
  case "$rc" in
    0) [ "$tries" -eq 2 ] && add "pass  $name (${n:-?} tests, $took, after a crash)" || add "pass  $name (${n:-?} tests, $took)" ;;
    1) add "FAIL  $name ($took) - $(keep_log "$name")"; bad=1; failure_details ;;
    2) add "HANG  $name (twice) - $(keep_log "$name")"; bad=1 ;;
    3) add "STUCK $name ($took) - $(keep_log "$name")"; bad=1 ;;
  esac
done
echo "test-full: summary$summary"
exit "$bad"
