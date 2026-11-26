# Tasks: issues, labels, done

Last reviewed: 2026-11-26

Every piece of work is a GitHub issue in `pkorpel/moriaengine`, raised before the work starts (#1559).

## Labels

Set when the issue is raised.

| Group | Labels | Rule |
|---|---|---|
| type | `type:bug`, `type:feature`, `type:chore`, `type:docs`, `type:research` | exactly one |
| prio | `prio:1` urgent, `prio:2` next, `prio:3` some day | exactly one |
| area | `area:combat`, `area:skills`, `area:magic`, `area:economy`, `area:npc`, `area:world`, `area:quests`, `area:admin`, `area:login`, `area:infra` | every one that applies |
| flags | `needs-core-review` (waiting for janslav's approval), `needs-in-game-check` (shipped, not yet tried in a real client) | as they apply |

`area:quests` covers quests, errands and the living world. `area:admin` covers GM commands and dialogs,
moderation, world setup and settings. `area:infra` covers build, CI, tooling, test infrastructure and the dev
setup. The GitHub default labels (`bug`, `enhancement`, …) aren't used.

There is no status label. The maintainer's go-ahead (in chat, or on the issue) makes an issue ready, and the
issue thread shows where it stands.

## Lifecycle

1. **Raised**, labelled, from a template (bug, feature, chore/docs/research).
2. **Picking this up:** the first comment when work starts, with the plan and the branch. Findings and
   decisions follow as comments.
3. **Before building**, depending on the type:

   | Type | Extra step |
   |---|---|
   | `bug` | Reproduce at the reported spot; a test fails with the reported symptom before the fix |
   | `feature` | An "Existing system" comment: how the engine already does it (its `docs/current/` page, the code, the original `2dbc90b`) and how Erebor does it. The feature plugs into that system. A broken existing piece is fixed first in its own issue, never replaced. A new system, or one touching more than one existing system, also gets a design note the maintainer OKs. |
   | `chore` | none |
   | `docs` | none (the review checks the stated facts against the code) |
   | `research` | The output is a docs page, no code, until the maintainer picks items |

4. **Done**, in this order:
   1. code and its docs in the same PR (`moriaengine/docs/current/` for a changed system, `docs/wiki/` for
      scripts, CI, hooks or process);
   2. an independent review agent has checked it and every finding is addressed (`## Review` in the PR);
   3. tested: a new test red-checked (A/B), the fast tier, the area's protocol classes, with the PR listing
      only what ran;
   4. engine core approved by janslav (see [Dev process](Dev-Process.md#the-merge-gate));
   5. CI green, merged with a merge commit, branch and worktree removed;
   6. the issue closed with a summary comment (what shipped, the PR, follow-ups, and whether it was tried in
      a real client; if not, `needs-in-game-check`).

PRs say `Closes #<n>`.
