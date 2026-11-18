---
description: Push the issue branch, run the area's protocol classes locally, open the PR into main, wait for CI and merge it
---

Ship the current issue branch as a pull request into `main` of
`pkorpel/moriaengine`. Running this command is the user's go-ahead to push this
branch and merge its PR once CI is green.

This command is the merge gate — it merges only on a green CI run of the PR's head
commit (a private repo on GitHub Free has no branch protection or required checks).

1. Branch must be `<n>-<slug>`; take the issue number from it. On `main`: stop.
2. Working tree clean, at least one commit ahead of `origin/main`. Check the commit
   subjects: imperative, `area: what changed`, ≤ 72 characters, no attribution.
3. `git fetch origin`. If behind `origin/main` and it matters (conflicts, or main
   changed what the branch touches): make a backup branch, then `git reset --hard
   origin/main` and cherry-pick the branch's commits (`git rebase` misbehaves in this
   container). Resolve conflicts or ask.
4. `git push -u origin HEAD` (a rewritten, already pushed branch needs
   `--force-with-lease`, which is denied for the agent, so ask the user to push it). The
   pre-push hook builds and runs the fast tier (~30 s). If it fails, fix and push
   again; never skip it.
5. Run the protocol classes for the area the branch touches, chosen generously:
   `scripts/test-full.sh "FullyQualifiedName~A|FullyQualifiedName~B"` (see the
   `protocol-tests` skill). The whole suite (~3 h) is not run per PR: it runs once at
   the end of a batch of work, or when the user asks (user, 2026-11-05). A missing
   result for the area's classes is not a silent default. If a wave of unrelated classes
   suddenly fails, check `moriaengine/saves/` for a stale dated snapshot FIRST (wiki
   `Process/Dev-Process.md`, "Flaky full-boot tests") before suspecting this branch -
   a stale non-baseline snapshot causes a misleading login-denial cascade, a known
   mechanism predating this kit. A `FAIL` in a class the branch didn't touch that
   passes on a rerun of that class alone (after ruling out a stale snapshot) is a
   known flake - note it in the PR, don't block on it. A real failure: fix and rerun
   before opening the PR.
6. If a PR for this branch is already open, go to step 9. Otherwise open one
   (`create_pull_request`): base `main`; title = what the squashed commit should
   say, in the commit style; body from `.github/pull_request_template.md`:
   `Part of #<n>` (not "Closes": the issue closes only after it's verified on a
   local boot of main), what and why, how it was checked, the review, boxes ticked
   only where true.
   **Review (standing rule, #1230):** a branch that changes anything but `*.md`
   needs an independent review before merge. Coming from `/task-run`, its step 5 did
   it. Otherwise run the `reviewer` agent now (worktree path, issue number), verify
   its findings, fix the blocking ones (then rerun what they touch) and review again
   after a substantial fix. The PR body's `## Review` section says what it found and
   what changed ("no defects" counts); a review that ends after the PR is open goes
   in with `update_pull_request` (or `gh pr edit --body-file` in its own call, not
   chained before the merge). `scripts/hooks/require_review.py` denies the merge
   while that section is missing or empty.
7. Wiki: if the branch changes `scripts/`, `.github/`, `.pre-commit-config.yaml`,
   `.claude/`, the labels or a component's structure, the matching page in
   `docs/wiki/` changes in the same branch — `Runbooks/<Op>.md` for a script,
   `Process/Dev-Process.md` for CI, hooks or branching, `Process/Agent-Workflow.md`
   for commands and agents, `Process/Tasks.md` for labels, `Architecture/*.md` for a
   component. Name the pages in the PR body.
8. On the issue: remove `status:doing`, add `status:review` (send the full label list).
9. Wait for CI: poll `actions_list` (workflow runs for the branch's head SHA) until
   the `.NET Build` run completes — at most 20 minutes, every ~60 s. Then:
   - **success** on the current head SHA, PR not a draft, the user didn't say "no
     merge": `merge_pull_request` with method `squash`, then delete the remote
     branch (`git push origin --delete <branch>`);
   - **failure**: read the failed job (`get_job_logs`), fix on the branch, push,
     wait again. Red twice: stop and report;
   - no run for the head SHA after 3 minutes: dispatch one —
     `actions_run_trigger` (`run_workflow`, workflow `dotnet.yml`, ref = the branch,
     inputs `{"tier": "fast"}`; only if the workflow has that `tier` input, otherwise
     stop and report) — and wait for that run instead. It counts only if its
     `head_sha` is the PR's current head. Still none after 5 more minutes: stop and
     report;
   - runs that fail in seconds with no runner allocated (Actions minutes used up):
     merge with `gh pr merge --squash --admin` only after the local checks above all
     passed, and say so in the PR. Never merge a run that actually ran and went red.
10. Report: PR link, CI result, merged or not (and why), and the next step — verify
   on a local boot of main (`/task-run` does it; by hand: the tester agent, mode
   `verify`).
