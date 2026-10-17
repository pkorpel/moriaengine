---
description: Claim a ready issue, comment the plan and make its worktree from agent-kit-trial
argument-hint: [issue number]
---

Start work on issue $ARGUMENTS in `pkorpel/moriaengine`.

**No number given:** list open issues labelled `status:ready` with no assignee,
sorted by `prio:` then age, and ask which one. Stop there.

**Claim rule.** Work may start only on an issue that is open, labelled
`status:ready` and **unassigned**. If it is assigned (yourself included, from an
earlier session), has another `status:`, or is closed: stop, say what you found,
and ask.

1. Read the issue and its comments (`issue_read`, methods `get` and
   `get_comments`).
2. `git fetch origin`. The main checkout stays on `agent-kit-trial`; work happens in a
   worktree.
3. Read the code the issue touches and write a short plan: numbered steps, each
   small enough for one developer run; which files; which test covers each
   acceptance criterion (fast tier or full-boot); anything unclear. If something
   blocks the plan, ask now, before claiming.
4. Claim, in this order (`issue_write`, method `update`, then `add_issue_comment`):
   - assignee `pkorpel`;
   - labels: remove `status:ready`, add `status:doing`; keep every other label
     (`update` replaces the whole label list — send all of them);
   - comment the plan, starting with "Plan:".
5. `scripts/worktree-add.sh <n>-<slug>` → `../moria-<n>` on branch `<n>-<slug>`
   from `origin/agent-kit-trial` (sparse: no Cruncher/OldUpdater). Slug: lowercase ASCII, words
   joined by `-`, from the title, at most ~40 characters (`12-poison-tick-damage`).
   Don't push yet; `/task-ship` does that.

Then do the work in the worktree. Commit in small steps with `area: what changed`
subjects. The pre-commit and commit-msg hooks run; fix what they report, never
`--no-verify`. New behaviour gets a test. Stay inside the issue's scope: anything
else you notice becomes a note for `/task-new`, not part of this branch.
