# Workflow

The process itself is in `docs/wiki/Process/Dev-Process.md` and `Agent-Workflow.md`. This file holds the
rules learned the hard way on top of it.

## Issues

- Raise the issue **before** any investigation or code, for bugs you find yourself too. Don't ask first.
- Comment on the issue as things land: root cause, options and the one chosen, why something was or
  wasn't ported from Erebor, scope cuts. The thread should read as the story of the work.
- Close it only when the PR has merged, and reference the PR. To close, use a separate update call:
  `issue_write create` with `state: closed` leaves the issue open. Check the real state afterwards.
- Stay inside the issue's scope. A bug found along the way gets its own issue and PR.
- Only the maintainer moves an issue to `status:ready`.

## Before you build

- **Verify claims in code.** "Shipped" in an old note, an audit's "X doesn't work", or "already exists"
  can each be wrong. Read the current code. When the maintainer says something exists that a search
  missed, read the whole relevant directory instead of grepping again.
- Before porting what changes a thing's state, check what that state actually does. One audit's
  "doesn't unlock" turned out to mean nothing was ever locked.
- Reusing an existing mechanism (a curve, a delay, a timer): check that its math really fits the source.
  Don't force a fit.
- Don't invent tuning numbers. Where the source gives none, put the value on `.settings` and say it's a
  guess. Configuration that isn't filled in defaults to permissive (an empty allow-list means
  unrestricted).

## Fixing

- **Fix by category, not by instance.** A tiledata, LOS or movement bug is fixed for the UO flag it
  carries (`Surface`, `Roof`, `Noshoot`...), never for one model number. If a fix touches only one half
  of a symmetric check, check the other half too.
- For a live bug report, reproduce it at the reported coordinates, with real tiledata (a throwaway
  diagnostic command is fine). A synthetic test that passes doesn't close the report.
- When the maintainer says "still broken", first prove the fix still changes the outcome (disable it and
  see the test fail) before blaming old data.
- A tuning change also updates the tests that hard-code the old number.

## Shipping

- Each PR has its own new test, red-checked: revert just the fix, see the test fail with the reported
  symptom, then restore it. It also gets the fast tier (`scripts/test-fast.sh`, which compiles Release
  too) and a generous set of protocol classes for the area (see the `protocol-tests` skill).
- The PR body lists only checks that ran on the final code. After a fix made since the last run, run
  again.
- Merge with `gh pr merge --squash` once local checks and CI are green. When CI can't run (no runners or
  minutes), `--admin` after full local verification is authorized.
- After the merge: close the issue with the PR reference, delete the branch, and remove the worktree
  when you're done with it.
- New player-facing commands go on the allowlist in the same PR (see `engine-patterns.md`). New tunables
  go on `.settings` in the same PR, not as a follow-up.
- Docs: a change to a documented system updates its `moriaengine/docs/current/` page in the same PR.
  A durable design decision also goes in `docs/current/08-history/design-decisions-log.md`, and a known
  gap in `known-gaps-and-tech-debt.md`. Docs explain how and why and link the issue; they don't tell the
  bug's story.

## Full protocol suite

The whole suite takes about 3 h. Don't run it per PR: run it once at the end of a planned batch, or when
the maintainer asks. Per PR, run the targeted classes.
