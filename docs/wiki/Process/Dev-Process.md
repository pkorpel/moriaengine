# Dev process

Last reviewed: 2026-10-17

## Branches

- `agent-kit-trial` is the only long-lived branch. It takes changes only through pull requests.
- Issue branches: `<n>-<slug>` (e.g. `12-poison-tick-damage`), made from
  `origin/agent-kit-trial` in their own worktree `../moria-<n>` by `scripts/worktree-add.sh`
  (sparse: no `Cruncher/`, `OldUpdater/`, ~230 MB instead of ~660 MB).
- PRs are squash-merged; the remote branch is deleted after merge.

## The merge gate

`/task-ship` is the gate, whatever the plan: on GitHub Free a private repo has no
branch protection or required checks (if yours has them, they apply on top). So:

- `/task-ship` merges a PR only after the `.NET Build` run on the PR's head commit is
  green.
- Locally, `pre-commit` refuses commits on `agent-kit-trial`, and `.claude/settings.json`
  denies pushes to `main` and `agent-kit-trial`, force pushes and `--no-verify` for Claude.
- A person can still push to `agent-kit-trial` directly — don't.

## Hooks (`.pre-commit-config.yaml`)

Install once per clone: `pre-commit install`.

| Stage | Checks |
|---|---|
| pre-commit | merge conflicts, YAML/JSON syntax, files > 1 MB, private keys, gitleaks, no commit on `agent-kit-trial` |
| commit-msg | `scripts/commit-msg-check.sh`: no `Co-Authored-By`/session trailers, subject ≤ 72 |
| pre-push | `scripts/test-fast.sh`: build + fast test tier (~30 s) |

## CI (`.github/workflows/dotnet.yml`)

- Push / PR to `agent-kit-trial`: build (`build.ps1`) + fast tier on `ubuntu-latest`, ~2–3 min.
  A newer push to the same branch or PR cancels the older run (`concurrency`).
- Manual runs (`workflow_dispatch`, Actions → ".NET Build" → Run workflow) take a
  `tier`: `fast` (default, same job as push/PR) or `full` (entire suite incl.
  full-boot `Tests/Protocol`, ~10–25 min).
- If no push/PR run shows up for a PR's head commit within 3 minutes, `/task-ship`
  dispatches the fast tier on the branch and gates the merge on that run instead.
- Private repo on Free: 2000 Actions minutes / month, so the full suite runs locally
  in `/task-run` (`scripts/test-full.sh`) instead of on every push.

## Tests

| Tier | Where | Run | Time |
|---|---|---|---|
| Fast | `moriaengine/Tests/*.cs`, `Tests/Fixtures/` | `scripts/test-fast.sh [filter]` | seconds |
| Full-boot | `moriaengine/Tests/Protocol/*` (+ server-side helpers in `moriaengine/scripts/Tests/`) | `scripts/test-full.sh [filter]` | ~10 min all |

The full-boot fixture (`GameServerPairFixture`) starts real AuxiliaryServer +
GameCore processes on fixed ports 2593/2595. `scripts/test-full.sh` holds a
machine-wide lock (`/tmp/moria-fullboot.lock`) so runs from different worktrees
queue instead of colliding, and refuses to start while `scripts/run-local.sh` has the
ports.

### Flaky full-boot tests

Known since issues #100 and #121: a whole-suite `dotnet test` run goes quiet in a
random full-boot class and never finishes. `OwnerAggroTests` and
`LevelingIsolatedTests` fail intermittently, and connection races hit
Economy/Social/CombatScenario.

So `scripts/test-full.sh` without a filter runs the fast tier, then **each full-boot
class in its own `dotnet test`**. A class that hangs (`--blame-hang`, 3 min, no dump,
leftover servers killed) is retried once; plain failures are not. It ends with a
per-class summary (`pass` / `pass … after a hang` / `FAIL` / `HANG (twice)`).

Saves: unless the fixture gives each test class its own temp save directory,
full-boot runs write into `moriaengine/saves/`, and a dirty tree stops `/task-ship`.
Don't run full-boot tests with plain `dotnet test`.

Rule for a PR: a `FAIL` in a class the branch didn't touch that passes when rerun
alone (`scripts/test-full.sh "FullyQualifiedName~<Class>."`) is a flake; note it in
the PR. Fails again: it's real. Never let a dump run: a full hang dump of testhost +
servers is many GB.

**Before chasing a sudden wave of unrelated failures, check `moriaengine/saves/` for a
stale dated snapshot first.** `BackupsManager` always loads the single most-recently
written `saves/<date>/<time>/` snapshot, not the committed `saves/2000-01-01/00_00_00/`
baseline. Many full-boot test classes assume their login account (commonly
`"protocoltest"`) is literally the server's first-ever account
(`accounts.Count == 0` grants auto-creation + max plevel) - once any OTHER test's save
(a different account name, e.g. a persistence-regression test) becomes "latest", every
`protocoltest`-reliant class in every later run fails with a login denial
(`LoginDeniedReason=0`, NoAccount), looking exactly like a real regression. Recurred
several times before the kit existed, same mechanism every time. Fix: delete the stale
`saves/<date>/` directory (gitignored-in-spirit scratch, zero product value, never the
tracked `2000-01-01` baseline) and rerun - don't spend time on the test/code under
inspection until this is ruled out.

**After any full-boot run, `/proc` fills with leftover dotnet/msbuild/VBCSCompiler
entries in state `Z` - these are zombies, not a leak**, consume ~0 resources, and can't
be killed (nothing to signal). A genuinely hung/timed-out run is sandbox scheduling
contention, not these zombies - retry once, don't go hunting for "cleanup."

**A different, real class of failure found 2026-10-17 (#364): a LIVE (not zombie)
`AuxiliaryServer`/`GameCore` pair from one full-boot class can survive into the next
one** - `GameServerPairFixture`'s own teardown doesn't reliably kill its server pair on
every exit path (confirmed: not just the hang case `test-full.sh` already guarded
against, but plain non-hang failures and even ordinary passes). A lingering pair squats
ports 2593/2595, and every LATER class then fails with a misleading
`SocketException: Connection timed out` instead of its real result - once it starts,
it cascades across most of the rest of a run, not a one-off. `test-full.sh` now sweeps
(`lsof -t ... | xargs kill`) before every class, not just after a detected hang, so this
self-heals automatically - no manual intervention should be needed anymore. If it
somehow still is: check process start times (`stat -c '%y' /proc/<pid>`) against the
current class's own pair - anything meaningfully older is the leftover, not the zombies
note above (that one's about dead `Z`-state entries that can't interfere; this one is
about live listeners that genuinely do).

## Commits

`area: what changed`, imperative, ≤ 72 chars, no attribution of any kind. A body
only when the why isn't obvious. Areas follow the code: `combat:`, `skills:`,
`login:`, `tests:`, `ci:`, `docs:`, `scripts:` …

## Wiki

These pages change in the same branch as what they describe; `/task-ship` checks
it. Every page has a `Last reviewed:` date.
