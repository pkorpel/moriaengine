---
name: debug-hang
description: Find the cause of a hung or unresponsive moriaengine GameCore (a protocol test times out with no output, "server unresponsive", suspected deadlock) with a managed stack dump via dotnet-stack. Use before adding checkpoint prints.
---

# Debugging a hung GameCore

Checkpoint prints narrow a hang down to a call; only a managed stack dump shows the cause. One dump found
the #798 timer lock-order deadlock at once, after two wrong guesses.

1. `dotnet tool install -g dotnet-stack` if it's missing (it ends up in `~/.dotnet/tools`).
2. The diagnostics socket lives in the **target's** `TMPDIR`, and the scratchpad path is over the
   108-character socket limit ("ServerNotAvailableException"). Use a short path:
   ```bash
   ln -s <scratchpad> /tmp/dh && TMPDIR=/tmp/dh scripts/test-full.sh "FullyQualifiedName~TheClass"
   ```
3. Watch `moriaengine/logs/SteamEngine.GameServer.<date>.log` for the hang marker.
4. Find the pid. There's no `pgrep`, so scan `/proc/*/cmdline` for `GameCore`. Then:
   ```bash
   TMPDIR=/tmp/dh dotnet-stack report -p <pid>
   ```
5. Look for two threads waiting on each other's locks. The lock order is `globalLock`, then the timer
   lock, and `Timer.changesLock` is a leaf lock. Also check Logger vs `Console.Out` (#815).
6. Remove the symlink afterwards. Stop the run with `scripts/test-full.sh --kill-stale`.

Profiling (`dotnet-trace`) needs the same short `TMPDIR`, for both the run and the tool. `dotnet-trace`
10 has no `cpu-sampling` profile, so pass the providers explicitly.

Old "server unresponsive" flakes from before #798/#815 (fixed 2026-10-29) may have been these deadlocks.
Retest them before treating them as sandbox noise.
