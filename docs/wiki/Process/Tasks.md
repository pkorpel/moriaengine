# Tasks: issues and labels

Last reviewed: 2026-10-16

Every piece of work is a GitHub issue in `pkorpel/moriaengine`. Every bug gets an
issue before work on it starts.

## Labels

| Group | Labels | Rule |
|---|---|---|
| type | `type:feature`, `type:bug`, `type:chore`, `type:spike`, `type:docs` | exactly one |
| prio | `prio:1` urgent, `prio:2` next, `prio:3` some day | exactly one |
| area | `area:combat`, `area:skills`, `area:magic`, `area:economy`, `area:npc`, `area:world`, `area:login`, `area:infra` | any that apply |
| status | `status:ready`, `status:doing`, `status:review`, `status:blocked` | at most one; none = triage |

`area:infra` covers build, CI, tooling, tests infrastructure and the dev setup.

## Lifecycle

| State | Label | Who moves it |
|---|---|---|
| triage | — | `/task-new` creates it here |
| ready | `status:ready` | **only the user** |
| doing | `status:doing` | `/task-pick` / `/task-run` (claim: assign + label + plan comment) |
| review | `status:review` | `/task-ship` when the PR is open; stays here until verified |
| blocked | `status:blocked` | stop rule of `/task-run`, with a comment saying what's needed |
| closed | — | `/task-run` after a local verify with every criterion `met` |

PRs say `Part of #<n>`, not `Closes`: the issue closes only after the merged `agent-kit-trial`
is verified locally.
