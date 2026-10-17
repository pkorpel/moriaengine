# Agent Kit Trial — running log

Back-filled 2026-10-17: the install (phase 2, commits `348a1c7`..`618ba12`) ran without
this file ever being created, despite SETUP.md requiring it from phase 0 onward. This
reconstructs phase 0/1 from the installed diff plus a fresh re-check of the phase 0
facts, then continues the log live from here. Phase 4 needs this file complete, so
don't let it drift again.

## Phase 0 — facts

| Check | Result |
|---|---|
| Repo | `pkorpel/moriaengine`, default branch `main`, private |
| Working tree / branch state | clean at install time; `agent-kit-trial` did not exist yet |
| OS | Linux |
| `dotnet` | 8.0.425 |
| `pwsh` | 7.4.6, present |
| `git` | 2.39.5 |
| `gh` | 2.102.0, logged in as `pkorpel` (token-based) |
| `docker` | missing / not running — using `github-mcp-server` (`scripts/mcp-github.sh`) instead, per SETUP's documented alternative |
| `pre-commit` | 3.0.4, present |
| `gitleaks` | 8.21.2, present |
| `python3` | 3.11.2 |
| `lsof` | present |
| `CLAUDE.md` | large (3000+ lines), a running session log — not a short doc |
| `.claude/` | did not exist before install |
| `.github/workflows/dotnet.yml` | triggered on `main` only, no `workflow_dispatch.tier` input, before install |
| existing labels | only GitHub's defaults (`bug`, `documentation`, `enhancement`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`, `duplicate`, `accessibility`) plus a bare `verification-pass` — no `type:`/`prio:`/`area:`/`status:` labels existed |
| GitHub Project board | none accessible to this token (GraphQL `FORBIDDEN` on `projectsV2`) — treated as "no board" |
| **Save isolation** | **missing.** `moriaengine/Tests/Protocol/GameServerPairFixture.cs` shares one on-disk `saves/` dir across all full-boot test classes — no per-class env var. Its own comment describes clearing stale snapshot dirs by date, not isolating by class. Confirmed real: a full-boot run writes into the tracked working tree. |
| `saves/`, `logs/`, `Tests/TestResults/` ignored? | `logs/` and `Tests/TestResults/` **are** ignored (`moriaengine/.gitignore`). `saves/` is **not** ignored, and `moriaengine/saves/2000-01-01/00_00_00/*.sav` (4 files) is the **tracked baseline bootstrap skeleton** `GameServerPairFixture` restores from — SETUP.md's warning ("ask before ignoring saves") applies for real here, this isn't hypothetical. Not touched by this trial; flagged below as the open item. |
| `global.json` | does not exist — SDK version isn't pinned anywhere; only one 8.0.x SDK is actually installed here so it hasn't bitten yet |
| Baseline build/test | not re-run for this backfill (would require stopping anything on ports 2593/2595 first); the 4 install commits landed and CI (`dotnet.yml`) already covers `agent-kit-trial`, treating that as sufficient evidence the fast tier still builds |

## Phase 1 — decisions (reconstructed from the installed diff)

1. **CLAUDE.md**: option **(a)**, recommended — kept as-is, added the kit's "Workflow
   (agent kit)" section near the top (now at `CLAUDE.md` after the repo/local-checkout
   bullets). `.claude/kit-claude-section.md` was deleted after, per step 3.
2. **Labels vs project board**: no board exists/is reachable, so this is moot — `status:`
   labels only, nothing added to a board.
3. **Area labels**: kept the kit's own list as-is — `combat`, `skills`, `magic`,
   `economy`, `npc`, `world`, `login`, `infra` (`docs/wiki/Process/Tasks.md`,
   `.claude/commands/task-new.md`).
4. **Commit rules**: kept **both** — the 72-char subject rule and the attribution-trailer
   ban (`scripts/commit-msg-check.sh` has both checks; `.claude/settings.json`'s
   `attribution` block is all empty/`false`).
5. **Telegram**: left off/later, as recommended — `scripts/telegram-session.sh` is
   installed but has no bot token configured.
6. **Pilot issue**: not chosen before this backfill. See "Open items" below — the
   missing save isolation is a strong alternative-pilot candidate per SETUP.md's own
   suggestion, and is now confirmed real rather than hypothetical.
7. **Who may mark `status:ready` / default assignee**: `pkorpel` (the maintainer), per
   `docs/wiki/Process/Tasks.md`'s lifecycle table and the kit defaults — no different
   login was given at install time.

## Phase 2 — install, what actually landed (2026-10-17)

Commits on `agent-kit-trial`:
- `348a1c7` agents: add role agents and task commands
- `6478be2` scripts: add test, run and worktree helpers
- `258ce78` docs: add process wiki
- `618ba12` ci: run on the agent-kit-trial branch, add issue templates and MCP config

Pushed to `origin/agent-kit-trial` (step 12). `.mcp.json` added (`github` server via
`scripts/mcp-github.sh`). `.claude/settings.json` has the deny rules for force-push,
push-to-main, push-to-`agent-kit-trial`, and `--no-verify`, plus the `protect_tests.py`
PreToolUse hook and the `motd.sh` status line/SessionStart hook.

**Gaps found during this backfill, now closed:**
- Step 11 (labels) had never run — no `type:`/`prio:`/`area:`/`status:` labels existed
  on the repo. Ran `kit-labels.sh pkorpel/moriaengine --dry-run` (confirmed all 20 were
  new, nothing pre-existing to collide with), then `--record
  docs/wiki/Process/agent-kit-labels.txt` to create them and record the exact list the
  revert path deletes.
- This file itself (the running log) — back-filled now.

**Gap found, deliberately NOT auto-fixed:**
- Save isolation (phase 0 above) is real and confirmed, but SETUP.md says to ask before
  touching `saves/`'s gitignore status since the baseline skeleton is tracked — not
  resolved here, flagged as the leading pilot candidate instead of silently patching
  `.gitignore`.

Step 13 (restart Claude Code on `agent-kit-trial` so `.mcp.json`/hooks/agents load) —
this session is operating from `agent-kit-trial` via `git switch`, not a literal Claude
Code restart. `/mcp` should still be checked for a connected `github` server at the
start of the next phase-3 session.

## Open items going into phase 3

- Pick and run the pilot (`/task-new`). Leading candidate: fix `GameServerPairFixture`'s
  save isolation (`STEAMENGINE_SAVEPATH`-style per-class dir) — real, small, fast-tier-
  provable? No — it's inherently a full-boot-tier concern, which cuts against SETUP's
  "not on the full-boot path" pilot criterion. Needs the maintainer's call: fix it anyway
  as the explicitly-offered alternative pilot (it unblocks every later full-boot
  `/task-ship`), or pick a small fast-tier bug instead and leave this as a follow-up
  issue.
- `global.json` pinning is still missing — low-risk today (one SDK installed), worth an
  issue if a second SDK ever lands in this environment.
- `saves/` gitignore status is still unresolved — don't touch without the maintainer's
  answer.

## Phase 2 blocker resolved, backfill shipped (2026-10-17)

The sandbox's `pre-commit` was rebuilt to a newer pip-installed version (4.0.1, confirmed
via `pre-commit --version`) in the Dockerfile — `pre-commit run --all-files` no longer hits
`InvalidConfigError` on the kit's `stages: [pre-push]` hook. One extra wrinkle found on top
of that: the previously-installed git hook scripts (`.git/hooks/pre-commit` etc.) hardcode
`INSTALL_PYTHON=/usr/bin/python3` from when the OLD apt-installed pre-commit wrote them —
the new pip/pipx install lives in a separate venv, so the stale hook script failed with
`No module named pre_commit` even though `pre-commit` itself worked fine from the shell.
Fixed with `pre-commit install -f` (regenerates the hook scripts pointing at the correct
interpreter) — needed once per worktree/checkout whose hooks predate the Dockerfile rebuild.

With that confirmed working, committed and shipped the back-filled trial log + label
record: branch `backfill-agent-kit-trial-log` → PR #357 → merged into `agent-kit-trial`
(squash, green CI, `don't commit to branch` hook observed passing on a non-main branch).

## Phase 3 — pilot decision and first pilot issue (2026-10-17)

Asked the maintainer explicitly (per the open item above): use the save-isolation gap as a
sanctioned full-boot-tier exception, or pick a different small fast-tier bug and leave
save-isolation as a regular follow-up. **Decision: pick a different bug.** Save isolation's
re-confirmation + the tracked-baseline-skeleton wrinkle (`saves/2000-01-01/00_00_00/*.sav`)
was added as a comment on the existing #197 (same root cause, filed earlier this project
under a different symptom) rather than a new duplicate issue — not fixed.

**Pilot chosen: #358** — `StaticMemberSaver.TryRegisterMember` guards properties for
read/write-ability before registering them but had no equivalent guard for fields, so the
readonly `CraftmenuContents.mainCategories` field (marked `[SavedMember]`) got registered,
saved fine, then threw `FieldAccessException` on every subsequent boot's load — the exact
boot-log noise from #64 item 4, split off into its own issue since it's a self-contained
engine bug, not a content/data judgment call like #64's other 3 items. Fits the pilot
criteria well: small (one guard clause + one dead-attribute comment), pure reflection logic
with zero full-boot dependency, confirmed by direct code reading before touching anything.

Ran the full kit workflow end to end: issue #358 (labels `type:bug`/`prio:2`/`area:infra`,
`status:doing` — see the issue's own comment for why `status:ready` was skipped, this
session had the maintainer's live go-ahead already) → worktree `../moria-358` via
`scripts/worktree-add.sh` → fix + new fast-tier test (`Tests/StaticMemberSaverTests.cs`,
reflects into the real `StaticMemberSaver`/`CraftmenuContents` to prove the readonly guard
works — manually confirmed it fails without the fix, passes with it) → PR #359 into
`agent-kit-trial`.

**Real build-pipeline snag found while building the pilot**: the new test needed a
`ProjectReference` from `Tests.csproj` to `scripts/SteamEngine.Scripts.csproj` (to reach
the real `CraftmenuContents`/`StaticMemberSaver` types) — but `build.ps1`'s
`$StandaloneProjects` list built `Tests.csproj` standalone *before*
`Invoke-ScriptGeneration` ran, which fails once Tests depends on Scripts (Scripts needs the
not-yet-generated `Generated/*.Generated.cs` partial classes). Fixed by dropping
`Tests/SteamEngine.Tests.csproj` from that early list — the final "Building full solution"
step (which runs after script generation) already builds it. Confirmed by a full
`pwsh ./build.ps1` run end to end plus `scripts/test-fast.sh` (28/28 fast-tier tests green).
