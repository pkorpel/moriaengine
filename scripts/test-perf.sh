#!/usr/bin/env bash
# The perf harness (#1161, #1179): builds, boots a server pair and runs a scenario that populates a world in phases and
# measures each one (game thread busy, timer lateness, globalLock holders and waits, client step times). Not part of
# either tier: the scenarios skip themselves unless PERF_RUN=1, which this sets. Results are appended to PERF_OUT
# (default /tmp/perf-out.txt). How to read them: docs/current/02-engine-core/performance-review.md, section 9.
#
#   scripts/test-perf.sh                  # PerfProbeTests: the populated-world scenario
#   scripts/test-perf.sh PerfLoadTests    # boot on the world PerfProbeTests saved last, timing the load
#
# Knobs (environment): PERF_SCALE (1.0 = the design world: 30k NPCs, 626k items; 0.1 for a quick smoke run),
# PERF_PLAYERS (50), PERF_SECONDS (30 per measuring window), PERF_PHASES (all, or a list of players,save,stress),
# PERF_RECTS (default moriaengine/tools/perf/dungeon-rects.txt), PERF_CONFIG (Debug; Release once it builds, plan step B2).
# PERF_OUT is read by the test process, whose working folder is its bin folder: give an absolute path.
# A full-scale run takes about 8 minutes in Release (longer in Debug) and several GB: run nothing else beside it.
set -u

root=$(cd "$(dirname "$0")/.." && pwd)
# test-full builds Debug; another configuration is built here, or the fixture would boot whatever was built last
config="${PERF_CONFIG:-Debug}"
if [ "$config" != "Debug" ]; then
  log="${TMPDIR:-/tmp}/moria-perf-build.$$.log"
  echo "test-perf: building $config"
  (cd "$root/moriaengine" && pwsh ./build.ps1 -Configuration "$config") >"$log" 2>&1 || {
    tail -30 "$log" >&2
    echo "test-perf: the $config build failed (full log $log)" >&2
    exit 1
  }
fi
export PERF_RUN=1
export STUCK_SECONDS="${STUCK_SECONDS:-7200}"
export HANG_TIMEOUT="${HANG_TIMEOUT:-130min}"
export MORIA_BOOT_SECONDS="${MORIA_BOOT_SECONDS:-150}"
exec "$root/scripts/test-full.sh" "FullyQualifiedName~SteamEngine.Tests.Perf.${1:-PerfProbeTests}."
