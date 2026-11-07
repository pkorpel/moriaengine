---
description: Take a ready issue from claim to closed with the role agents (tester, developer, reviewer)
argument-hint: <issue number>
---

Run issue $ARGUMENTS of `pkorpel/moriaengine` end to end. Running this command
is the user's go-ahead for every step below up to "closed": claim, commits, push,
PR, merge on green CI, local verify, closing the issue.

You orchestrate; the role agents in `.claude/agents/` do the work. Give each agent
everything it needs in its prompt (worktree path, issue number, plan, step) — they
don't see this conversation. Notify with `scripts/telegram-session.sh notify "<text>"`
(start each message with `#<n>`).

**Stop rule.** On any stop below: comment on the issue what happened and what's
needed, set `status:blocked` if the work can't go on without the user, notify, and
end. Don't retry a stopped step another way.

1. **Claim.** Follow `.claude/commands/task-pick.md` steps 1–5 (claim rule
   included). The plan comment lists numbered steps, each small enough for one
   developer run, and names the test each criterion gets. The worktree is
   `../moria-<n>`; give every agent its absolute path and check the branch each one
   reports back. An agent that worked on `main`: stop.
2. **Red.** `tester` agent, mode `red`. It commits failing tests. No criterion
   testable at all and the issue isn't a chore/docs: stop.
3. **Build.** For each plan step, a fresh `developer` agent with: the issue text,
   the full plan, its step, the failing tests for that step. One step at a time, in
   order. A developer that returns a question: answer it from the issue if the
   issue answers it, otherwise stop.
4. **Suite.** In the worktree: `scripts/test-fast.sh`, then `scripts/test-full.sh`
   (fast tier + every full-boot class on its own, ~15 min, waits for the
   machine-wide lock; prints a per-class summary). A `FAIL` in a class the branch
   didn't touch that passes on a rerun of that class alone is a known flake (wiki
   Process/Dev-Process): note it in the PR. `HANG (twice)` in an untouched class:
   rerun that class once more, then treat it like a FAIL. Red for real: one
   `developer` run to fix it; red again: stop.
5. **Review.** `reviewer` agent. `changes` → a fresh `developer` with the blocking
   findings, then review again. Still `changes` after the second round: stop.
   The PR body's `## Review` section lists the rounds, what was fixed and the
   non-blocking findings (the merge hook requires the section, #1230).
6. **Ship.** Follow `.claude/commands/task-ship.md` steps 2–8 from the worktree.
   Not merged: stop.
7. **Verify.** A detached verify worktree at the merge commit:
   `scripts/worktree-add.sh --verify <n>` → `../moria-verify-<n>`. `tester` agent,
   mode `verify`, in it. Comment its
   table on the issue with the commit, then:
   - all `met`: remove `status:review`, close the issue (`issue_write`, state
     `closed`, reason `completed`);
   - anything else: leave it open with `status:review` (stop rule, no
     `status:blocked`).
8. **Clean up.** `git worktree remove ../moria-<n>` and `../moria-verify-<n>`;
   `git branch -d <n>-<slug>` once merged. Make sure `scripts/run-local.sh status`
   says not running.
9. **Report.** Notify: `#<n> <title> — closed` (or what's left), PR link, review
   rounds, tests added. End here with the same lines and a short table of the steps
   and their result.
