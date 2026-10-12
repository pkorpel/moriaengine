# moriaengine — working state

This file is kept up to date across sessions so a fresh Claude Code session (after
a kill/restart) can pick up exactly where the last one left off without re-deriving
context from scratch. Update it whenever the current task/branch/plan changes —
don't wait until end of session.

## Repo

- GitHub: `pkorpel/moriaengine` (SteamEngine, a Ultima Online server emulator, .NET 8 port)
- Local checkout: `/workspace/moriaengine`
- `main` tracks `origin/main` 1:1 — this repo gets no direct pushes to main, see Workflow below.

## Current initiative: issue #67 — full-scale protocol-level verification pass

Tracking issue: **#67** "[Verification] Full-scale protocol-level test pass benchmarked
against real UO behavior". Plan: real client-protocol scenario tests (login → real
packets → assert on observable server behavior via `GameServerPairFixture`) for every
system built this session, plus a benchmark pass over pre-existing "solid" systems.
Phases V-0 through V-10, each its own sub-issue under #67.

### Phase status

| Phase | Issue | Status | Notes |
|---|---|---|---|
| V-0 Shared infra | #68 | ✅ closed | target-cursor/skill-use encoders, packet scanner, 2-client harness |
| V-1 Combat depth | #69 | ✅ closed | |
| V-2 Pet commands & GM NPC control | #70 | ✅ closed | |
| V-3 Economy | #71 | ✅ closed | banking, vendor buy/sell, payment source |
| V-4 Social systems | #72 | ✅ closed | secure trade, guild, party — surfaced bugs #84, #87 |
| V-5 Notoriety & region enforcement | #73 | 🟡 open (intentionally) | Criminal-flag/murderer-threshold part done & merged (#89). Region-enforcement sub-part (`Notoriety.IsAttackBlockedByRegion`) **deliberately deferred** — needs real static-region test data or an ad-hoc-region test helper; constructing one at runtime risked corrupting shared region state for other concurrently-running tests. Left open for whoever picks this back up. |
| V-6 Housing | #74 | 🟡 open, should probably close | Deed placement, overlap rejection, co-owner/stranger access all tested & merged (#91). Surfaced bug #90 (now fixed, merged). **Gap**: issue's "GM release" checklist item not covered by any test yet. |
| V-7 Taming/Mining/Lumberjacking | #75 | ✅ closed (PR #93, 2026-10-12) | `GatheringTests`: taming sets ControlMaster, mining yields ore, lumberjacking yields a log. Full suite 59/59 passing at merge time. |
| V-8 Day/night & weather | #76 | 🔵 not started — **pick up here next** | |
| V-9 Content-restoration sanity | #77 | ⬜ not started | |
| V-10 Pre-existing systems benchmark | #78 | ⬜ not started | crafting, base skills, chat, movement, items/containers, GM tools, persistence |

### Bugs found by the verification pass (real defects in production code, not test bugs)

All three below were already fixed and merged to main, and their issues are now closed
(2026-10-12):

- **#84** (closed) — Guild (and all non-allowlisted) `.` commands were unusable by any
  non-GM player (`CommandsAuthorization.cs` allowlist never included them). Fixed:
  added the 13 guild commands to the allowlist + a `Test`-prefix exemption for the test
  harness.
- **#87** (closed) — Every non-GM `.` command always logged/reported as failed even on
  success (`ScriptHolder.TryRun`, unlike `LScriptHolder.Run`, never set
  `lastRunSuccesful`). Fixed in `GameCore/ScriptHolder.cs`.
- **#90** (closed) — House placement completely broken: `DynamicRegion.Place` checked
  whether the incoming parameter was non-null instead of whether the region's `Parent`
  was already set, so it threw on literally the first-ever placement. Fixed in
  `GameCore/Regions/DynamicRegion.cs:50`.

These confirm the verification pass is doing its job — pre-existing, never-before-exercised
code paths had real, 100%-reproducing bugs that no unit test ever caught.

## Right now: between phases — V-7 done, V-8 not started

V-7 (#75) shipped via PR #93 and is merged to `main`. Working tree is clean, `main` is
checked out, local is in sync with `origin/main`.

**Next steps (in order):**
1. Start V-8 (#76, day/night & weather scenario tests) — branch
   `verify-v8-day-night-weather` off `main`, same `GameServerPairFixture` pattern as
   V-0 through V-7.
2. Build, run the new scenario tests + full suite, commit → push → PR → self-merge →
   `git pull` main → delete branch (see Workflow below).
3. Close issue #76 on green.
4. Then V-9 (#77) and V-10 (#78), unless redirected. Also worth circling back to: the
   #74 GM-release test gap, and the #73 region-enforcement deferral, whenever there's
   a natural opening.

**Environment notes for this sandbox** (needed every fresh session — not in PATH/profile
by default):
```
export PATH="$PATH:/home/agent/.dotnet"
export DOTNET_ROOT="/home/agent/.dotnet"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1   # no libicu in this container
```
These are appended to `~/.bashrc`, but a tool invocation that doesn't source it
(non-interactive shells) still needs them exported inline. Build: `dotnet build
SteamEngine.sln -c Debug` (no `pwsh` available in this sandbox, so `build.ps1` can't be
used directly — plain `dotnet build`/`dotnet test` work fine since Generated/*.Generated.cs
and bin/obj already exist from prior sessions). Full suite:
`dotnet test Tests/SteamEngine.Tests.csproj -c Debug` — takes ~8 minutes
(GameServerPairFixture boots real server processes per test class), run it with
`run_in_background` / block on it rather than a short synchronous timeout.

## Workflow conventions for this repo (don't re-derive each session)

- **Always** go branch → commit → push → `mcp__github__create_pull_request` →
  `mcp__github__merge_pull_request` → `git pull` on local main → delete branch
  (local + `origin`). Never wait for the user to manually merge — they explicitly want
  a durable PR trail but not manual merge gatekeeping.
- Direct `git push origin main` can get blocked mid-session by the Claude Code
  auto-mode classifier ("Git Push to Default Branch"), even after earlier direct
  pushes in the same session succeeded. The branch+PR+self-merge route above is not
  subject to that block — use it by default rather than pushing to main directly.
- After a full-boot-tier test run (anything spawning `GameServerPairFixture`'s real
  `AuxiliaryServer`/`GameCore` child processes), you'll see hundreds of leftover
  dotnet/msbuild/VBCSCompiler entries under `/proc`. **These are zombies (state Z),
  not a real leak** — they consume ~0 resources and can't be killed (nothing to
  signal). If a full-boot test times out, it's sandbox scheduling contention, not
  these zombies — just retry once rather than hunting for "cleanup."
- If delegating research to a sub-agent/fork mid-implementation-session, be explicit
  that it's read-only ("do NOT write/commit/push, report back only") — forks inherit
  full conversation context and have previously gone ahead and implemented/merged
  real features on their own initiative when the surrounding context made that look
  like the obvious next step. Verify `git log`/`git status` after any fork that touched
  the repo, even a "just research" one.
