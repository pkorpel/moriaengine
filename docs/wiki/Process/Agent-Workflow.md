# Agent workflow

Last reviewed: 2026-10-17

Claude Code drives issues from idea to closed with slash commands
(`.claude/commands/`) and role agents (`.claude/agents/`). Installed from the
moria-agent-kit; "dev" is a local boot of `main`.

## Flow

```
/task-new ──► triage ──/task-refine──► (user) status:ready
                                              │
                                   /task-run <n>  (or /task-pick + work + /task-ship)
                                              │
 claim → worktree → red tests → build → full suite → review → PR → green CI → merge
                                              │
                    verify on a local boot of main → closed (or left in review)
```

## Commands

| Command | Does | Stops before |
|---|---|---|
| `/task-new <idea>` | issue from a template, labels, analyst pass | creating it without your OK |
| `/task-refine <n>` | analyst rewrites a triage issue into criteria | marking it ready (only you do) |
| `/task-pick [n]` | claims a ready issue, plan comment, worktree | pushing |
| `/task-ship` | push, full suite locally, PR, waits for CI, squash-merges on green | opening a PR without a full-suite run (standing rule, 2026-10-17 - skip only if the user says so for that one case) / merging on red / no CI run |
| `/task-run <n>` | all of the above with agents, plus local verify and close | any stop rule |
| `/status-weekly` | status page in `docs/wiki/Status/Weekly/` via a docs PR | — |

Running `/task-run` or `/task-ship` is the go-ahead to push and merge for that issue.

## Role agents

| Agent | Model | Can | Can't |
|---|---|---|---|
| analyst | opus / medium | read code, read + edit the issue body, comment | Bash, files, labels |
| tester | opus / high | write tests (`Tests/`, `scripts/Tests/`), run them, verify on local boot | production code |
| developer | sonnet / medium | edit code, build, run tests, commit in its worktree | GitHub, `Tests/` (hook), push |
| reviewer | opus / high | read the diff and code, read the issue | edit anything |

- The developer's ban on test files is enforced by `scripts/hooks/protect_tests.py`
  (PreToolUse hook): any path with a `tests` segment, any case.
- Every code change gets the reviewer (or another independent, read-only review
  agent) before merge, inside `/task-run` or not (standing rule since 2026-11-07,
  #1230). `scripts/hooks/require_review.py` (PreToolUse on `gh pr merge` and
  `merge_pull_request`) denies merging a PR that changes anything but `*.md` when
  its body has no `## Review` heading with text under it (the template's empty one
  doesn't count). It fails open when `gh` can't look the PR up.
  Its tests: `python3 scripts/hooks/test_require_review.py`.
- The analyst and reviewer are kept read-only by their tool lists: GitHub goes
  through the MCP server, so they get only the issue tools they need.
- Agents don't see the conversation: the orchestrator passes worktree path, issue,
  plan and step in each prompt and checks the branch each agent reports.

## Stop rule (`/task-run`)

On any stop: comment on the issue what happened and what's needed, set
`status:blocked` if it can't continue without you, notify Telegram, end. No retrying
a stopped step another way. Limits: two review rounds, one fix run for a red suite,
two red CI runs.

## GitHub access

- MCP server `github` (`.mcp.json` → `scripts/mcp-github.sh`): official
  `github-mcp-server` (local binary if on PATH, else v2.0.2 in Docker pinned by
  digest); toolsets issues, pull_requests, actions, repos, labels; tools that write
  files/branches/repos are excluded. Token from `scripts/gh-token.sh`:
  `$AGENT_GITHUB_TOKEN`, the macOS Keychain item `github-pkorpel`, or `gh auth token`.
- Read-only tools are allowed without a prompt in `.claude/settings.json`; writing
  tools (issue_write, create/merge PR) ask unless your local settings allow them.
- Git itself uses your normal git credentials (SSH or HTTPS).

## Session helpers

- SessionStart hook: `scripts/motd.sh --hook` — branch, issue, board counts, next step.
- Status line: `scripts/motd.sh --line`.
- Notifications: `scripts/telegram-session.sh notify "#<n> …"` (see
  [Telegram session](../Runbooks/Telegram-Session.md)).
