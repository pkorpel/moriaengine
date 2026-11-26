# Agent workflow

Last reviewed: 2026-11-18

How Claude Code works in this repo. The process itself (issues, labels, done) is the same for people and
agents: [Tasks](Tasks.md) and [Dev process](Dev-Process.md). The October agent kit (`/task-*` commands, role
agents, status labels) was dropped on 2026-11-18 (#1559); its trial notes are in `docs/history/agent-kit-trial.md`.

## Where agent instructions live (#1554)

| File | Read by | Holds |
|---|---|---|
| `AGENTS.md` | everyone: people, Claude Code, Copilot, other agents | repo shape, build/test, how changes land, rules that hold everywhere |
| `CLAUDE.md` | Claude Code (imports `AGENTS.md`) | Claude-only rules: session continuity via issues, the dev container's limits, git hazards, subagents |
| `.claude/rules/*.md` | Claude Code automatically (area rules only when matching files are touched); others by link | topic rules: workflow, Erebor porting, engine patterns and C# comments, testing, converter, LScript, localization, world setup |
| `.claude/skills/*/SKILL.md` | Claude Code, on demand | procedures: protocol tests, converter regen, hang debugging, Erebor lookups |
| `docs/history/claude-md-log.md` | nobody routinely | the old CLAUDE.md work log (2026-10-12 to 2026-11-13), archived |

None of these files holds status or history. Work state lives on the issue (plan comment first, then
findings as they land). A standing rule changes through a PR to the file that holds it.

## Review agent

- Every change, docs included (#1230, #1559), gets an independent, read-only review before merge: the
  `reviewer` agent (`.claude/agents/reviewer.md`) or another fresh agent. It gets the issue, the worktree and
  the diff, not the author's conclusions. Each finding is fixed or answered, and a substantial fix is reviewed
  again. The PR body's `## Review` section says what it found.
- `scripts/hooks/require_review.py` (PreToolUse on `gh pr merge` and `merge_pull_request`) denies the merge
  when that section is missing or empty, whatever the PR changes. It also denies it when the PR changes engine core without
  janslav's approval (#1559). It fails open when `gh` can't look the PR up. Its tests:
  `python3 scripts/hooks/test_require_review.py`.
- Agents don't see the conversation: the caller passes the worktree path, the issue and what to check, and
  checks the branch the agent reports.

## Existing patterns first (#1366, #1559)

Before work uses or changes an engine system or pattern (plugins, code-made defs, def fields, tags,
triggers, timers), its current form and use are written down from the existing code, with examples, in its
page under `moriaengine/docs/current/` (the original engine is commit `2dbc90b`). New code then follows that
pattern. A bug found in a pattern is fixed in the pattern, not worked around with a new mechanism. A feature
starts with an "Existing system" comment on its issue, and a broken existing piece is fixed first, never
replaced. The reviewer blocks a second way of doing what the engine already does.

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

- SessionStart hook: `scripts/motd.sh --hook` — branch, issue, open issues by type, next step.
- Status line: `scripts/motd.sh --line`.
- Notifications: `scripts/telegram-session.sh notify "#<n> …"` (see
  [Telegram session](../Runbooks/Telegram-Session.md)).
