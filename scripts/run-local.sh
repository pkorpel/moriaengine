#!/bin/sh
# Run the local shard in the background: AuxiliaryServer (login 2593, console 2594)
# and GameCore (game 2595), from this checkout's Debug build.
#
#   scripts/run-local.sh start [--build]  # start both, wait until 2593 and 2595 listen
#   scripts/run-local.sh status           # pids, listening ports, last log lines
#   scripts/run-local.sh logs             # follow both console logs
#   scripts/run-local.sh stop
#
# Both servers shut down when their stdin closes, so each gets `tail -f /dev/null`
# as stdin. Console logs: moriaengine/logs/{aux,gamecore}.console.log; pids in
# moriaengine/logs/run-local.pids. On a save with no accounts the first login creates
# its account (admin). The full-boot tests use the
# same ports: stop this before scripts/test-full.sh.
#
# Env: START_TIMEOUT (seconds to wait for the ports, default 180).
set -eu

root=$(cd "$(dirname "$0")/.." && pwd)
me="$root/moriaengine"
logs="$me/logs"
pids="$logs/run-local.pids"
TIMEOUT="${START_TIMEOUT:-180}"

die() { echo "run-local: $1" >&2; exit 1; }
listening() { lsof -nP -iTCP:"$1" -sTCP:LISTEN >/dev/null 2>&1; }
alive() { [ -f "$pids" ] && while read -r p; do kill -0 "$p" 2>/dev/null && return 0; done < "$pids"; return 1; }

launch() {  # name dll
  # detached from our stdout/stderr, or a caller reading them would wait forever
  ( tail -f /dev/null | dotnet "$2" >"$logs/$1.console.log" 2>&1 ) </dev/null >/dev/null 2>&1 &
  echo $! >> "$pids"
}

cmd="${1:-status}"
case "$cmd" in
  start)
    alive && die "already running (status / stop)"
    listening 2593 || listening 2595 && die "port 2593 or 2595 is busy (a test run?)"
    if [ "${2:-}" = "--build" ] || [ ! -f "$me/GameCore/bin/Debug/net8.0/SteamEngine.GameCore.dll" ]; then
      (cd "$me" && pwsh ./build.ps1 -Configuration Debug >/dev/null) || die "build failed, run scripts/test-fast.sh for the log"
    fi
    mkdir -p "$logs"; : > "$pids"
    cd "$me"
    launch aux AuxiliaryServer/bin/Debug/net8.0/SteamEngine.AuxiliaryServer.dll
    sleep 3
    launch gamecore GameCore/bin/Debug/net8.0/SteamEngine.GameCore.dll
    waited=0
    until listening 2593 && listening 2595; do
      [ "$waited" -ge "$TIMEOUT" ] && { tail -20 "$logs/gamecore.console.log" >&2; "$0" stop; die "ports not up after ${TIMEOUT}s"; }
      sleep 2; waited=$((waited + 2))
    done
    echo "run-local: up after ${waited}s — login 127.0.0.1:2593, game 2595 (logs: $logs/*.console.log)"
    ;;
  status)
    if alive; then echo "run-local: running"; else echo "run-local: not running"; fi
    for p in 2593 2594 2595; do listening $p && echo "  :$p listening" || echo "  :$p -"; done
    for f in aux gamecore; do
      [ -f "$logs/$f.console.log" ] && { echo "  -- $f"; tail -3 "$logs/$f.console.log" | sed 's/^/  | /'; }
    done
    exit 0
    ;;
  logs)
    exec tail -f "$logs/aux.console.log" "$logs/gamecore.console.log"
    ;;
  stop)
    [ -f "$pids" ] || listening 2593 || listening 2595 || die "not running"
    # Kill by the port's real listening pid, not the launch() subshell pid - that
    # subshell's own children (tail/dotnet) just get reparented to init if the
    # subshell itself dies without them, and pkill -P (the previous approach) is
    # not guaranteed to be installed. Poll + escalate to -9, then verify both
    # ports are actually free before claiming success.
    ok=1
    for p in 2593 2595; do
      pid=$(lsof -t -nP -iTCP:"$p" -sTCP:LISTEN 2>/dev/null) || true
      [ -n "${pid:-}" ] || continue
      kill $pid 2>/dev/null || true
      waited=0
      while listening "$p" && [ "$waited" -lt 10 ]; do sleep 1; waited=$((waited + 1)); done
      if listening "$p"; then
        pid=$(lsof -t -nP -iTCP:"$p" -sTCP:LISTEN 2>/dev/null) || true
        [ -n "${pid:-}" ] && kill -9 $pid 2>/dev/null || true
        sleep 1
      fi
      listening "$p" && ok=0
    done
    rm -f "$pids"
    if [ "$ok" -eq 1 ]; then
      echo "run-local: stopped"
    else
      die "port(s) still listening after SIGKILL - check manually (lsof -iTCP:2593,2595)"
    fi
    ;;
  *) sed -n '2,9p' "$0"; exit 2 ;;
esac
