@AGENTS.md

# Claude Code in this repo

Everything in `AGENTS.md` above applies. This file adds what is specific to Claude Code and to the dev
container it runs in. Keep it short: **no status, no history, no work log here**. Work state lives on
GitHub issues; history is git, PRs and `docs/history/`.

## Where the rules are

- `.claude/rules/*.md` load automatically. `workflow.md` and `erebor-port.md` load in every session. The
  area rules (`engine-patterns.md` for `moriaengine/**/*.cs`, testing, converter, LScript, localization,
  world setup) load only when you touch matching files. To change a rule, edit its file in a PR.
- Skills (`.claude/skills/`) are loaded on demand:
  `protocol-tests` (running and debugging full-boot tests), `converter-regen`, `debug-hang`, `erebor-reference`.
- Agent definition: `.claude/agents/reviewer.md`, the read-only review agent run before every code merge.
- What a rule must never stop doing goes in a hook (`.claude/settings.json`, `scripts/hooks/`), not in prose.
- GitHub: the `github` MCP server (`.mcp.json` → `scripts/mcp-github.sh`) or `gh`.

## Session continuity

Sessions die (OOM, mount drops), so the trail must exist before the work does:

1. **First action on any task:** the GitHub issue exists, labelled, and a **"Picking this up"** comment says
   what was asked, the plan and the branch. This replaces the old rule of writing an IN PROGRESS entry into
   CLAUDE.md.
2. While working, comment on the issue when something real lands: root cause, a decision, a scope cut,
   "done in PR, review said X". Those comments are the resume point.
3. **Resuming after a crash:** run `git worktree list`, then `git status` in each `../moria-*` worktree.
   Unfinished work is usually uncommitted in a worktree, not in the main checkout. Read the open issue's
   latest comments. If files suddenly look deleted, check `ls /workspace` and `/proc/mounts` first: the
   host mount sometimes drops. Never `git checkout`/`restore`/`reset`/`clean` a worktree that shows mass
   deletions.
4. A user decision that outlives one issue goes into the matching `.claude/rules/` file, or into
   `moriaengine/docs/current/08-history/design-decisions-log.md`, in a PR. Never into CLAUDE.md.

## Working with the maintainer

- Blanket approval for routine work: branches, commits, PRs, self-merge after the gates, test runs,
  issue triage. Destructive or unusual actions still need judgment.
- When a batch of items is planned, go from one to the next without asking for a go-ahead. Park questions
  that are truly the maintainer's call, and ask them all together in one report at the end.
- Work the maintainer deferred or parked ("raise an issue, we'll look later") isn't started until they
  pick it up. Ideas parked on an issue wait until they have tested the shipped work.
- Never decide on your own a design call that would silently change behaviour project-wide.
- Reports: plain language, short. Say what ran and what didn't.

## The dev container (memory and processes)

- The Docker VM has about 5 GB, and `claude` is PID 1: an OOM kill ends the session. Run **one build or
  test run at a time**, and don't let several agents build in parallel.
- Use the repo scripts (they source `scripts/lean-build-env.sh`). A bare `dotnet build`/`dotnet test`
  leaves idle MSBuild and compiler processes holding memory.
- `pwsh`, `dotnet` (`~/.dotnet`) and `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` are set. There is no ICU, so
  `CultureInfo("cs-CZ")` throws. There is no `pgrep` or `free`: scan `/proc/*/cmdline`. `kill -0` reports
  zombies as alive, so read the state in `/proc/<pid>/stat`. Leftover dotnet zombies with PPID 1 are
  harmless; don't try to kill them.
- TaskStop kills only the wrapper shell. Stop a full-boot run with `scripts/test-full.sh --kill-stale`.
- Long background output can lose its start. Compare the summary's counts with what the file shows.
- Tools that use a diagnostics socket (`dotnet-stack`, `dotnet-trace`) need a short `TMPDIR`
  (108-character socket path limit). See the `debug-hang` skill.

## Editing files

- **Check line endings before any scripted edit:** `open(p,'rb')`, then count `b'\r\n'`. A text-mode
  Python read hides CRLF, and a write turns the whole file into LF (`Player.cs`, `ConvertedCharDef.cs`
  and others are CRLF; most files are LF). Edit as bytes, and look at `git diff --stat` before
  committing.
- About 20 sources aren't UTF-8 (`GameCore/Gump.cs`, `GameCore/DenyResult.cs`,
  `scripts/Basic/Roles/Role.cs`; `Common/Tools.cs` is UTF-16). Grep skips them silently, so search with
  `grep -a`, and keep each file's encoding when editing.
- A retried scripted edit applies twice if its replacement contains the search text.

## Git hazards here

- Commit in its own step, then check `git log -1` and `git status --short` before pushing or merging. A
  hook-rejected commit inside a chained `commit; push; merge` once shipped a PR without its fix.
- Gate on the test exit code before writing a PR body or merging. `rc=124` means it didn't finish, not
  that it passed.
- A direct push to `main` is denied (settings and classifier). Always go through branch, PR, merge.
- The stash stack is shared by every worktree and session: don't use bare `git stash`/`pop`.
- A worktree made right after a merge needs `git fetch` first, or it starts from the old main.
  `scripts/worktree-add.sh` fetches for you.
- `git rebase` misbehaves here (it stops with "Could not apply" on clean picks). Bring a branch up to date
  with a backup branch, then `reset --hard origin/main` plus cherry-picks. An already pushed branch then
  needs a force push, which is denied for Claude, so ask the maintainer.

## Subagents

- **Review before merge** (every change, docs included): the `reviewer` agent (or another fresh read-only agent)
  gets the issue, the worktree and `git diff origin/main...HEAD`, **not your conclusions**. Verify each finding yourself, fix
  the real ones, re-run what they touch, and write what it found under `## Review` in the PR body (the
  `require_review.py` hook checks for it). For docs, the review checks every stated fact against the code.
- Briefs for research agents and forks say explicitly "read-only: do not edit, commit or push". Forks
  inherit the whole conversation and have implemented and merged work on their own. Check `git log` and
  `git status` after one returns.
