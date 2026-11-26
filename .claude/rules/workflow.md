# Workflow

The lifecycle every piece of work follows (#1559). The full process, for people too, is in
`docs/wiki/Process/Dev-Process.md` and `Tasks.md`.

## 1. Issue, with labels

- Raise the issue **before** any investigation or code, for bugs you find yourself too. Don't ask first.
- Labels when raising it: exactly one `type:` (`bug`, `feature`, `chore`, `docs`, `research`), exactly one
  `prio:` (1 urgent, 2 next, 3 some day), and every `area:` that applies (`combat`, `skills`, `magic`,
  `economy`, `npc`, `world`, `quests`, `admin`, `login`, `infra`). Use the issue templates' sections.
- The maintainer's go-ahead in chat makes it ready. There is no ready label.

## 2. Picking it up

- The first comment when work starts: **"Picking this up"**, with the plan and the branch/worktree. That
  comment is the resume point after a crash.
- Comment as things land: root cause, options and the one chosen, why something was or wasn't ported from
  Erebor, scope cuts. The thread should read as the story of the work.
- Stay inside the issue's scope. A bug found along the way gets its own issue (labelled) and PR.

## 3. Before building

- **Feature or port: investigate the existing system first.** Post an "Existing system" comment: how the
  engine already does it or something close (its `docs/current/` page, the code, the original in `2dbc90b`),
  and how Erebor does it. Then plug into that system and never reinvent it. **If the existing piece is broken,
  file it and fix it first, never replace it.** The review agent blocks a second mechanism.
- A feature that adds a new system or touches more than one existing one: a short design note on the issue,
  OKed by the maintainer, before building.
- **Bug:** reproduce it at the reported spot. The test must fail with the reported symptom. Fix by
  category, not instance: a tiledata/LOS/movement bug is fixed for the UO flag it carries, never one model
  number; if a fix touches one half of a symmetric check, check the other half.
- **Research:** the output is a docs page, no code, until the maintainer picks items.
- **Verify claims in code.** "Shipped" in an old note, an audit's "X doesn't work", "already exists": read the
  current code. When the maintainer says something exists that a search missed, read the whole directory.
- Don't invent tuning numbers: where the source gives none, put the value on `.settings` and say it's a guess.

## 4. Definition of done

In this order:

1. **Code and its docs are done, in the same PR.**
   - A changed system updates its `moriaengine/docs/current/` page; a new page is linked from
     `docs/current/README.md`.
   - Scripts, hooks, CI or process update `docs/wiki/`.
   - A durable decision goes to `docs/current/08-history/design-decisions-log.md`.
   - A plain bugfix with no lasting "why" needs no doc.
2. **The review agent has checked it, and every finding is addressed** (fixed, or answered in the PR). A
   substantial fix gets reviewed again. The PR body's `## Review` says what was found. Docs get reviewed too
   (#1559): every stated fact checked against the code, plus clarity and contradictions with other pages.
3. **Tested.**
   - A new test, red-checked: A/B with only the fix reverted, failing with the reported symptom.
   - The fast tier.
   - The protocol classes for the area, chosen generously (`protocol-tests` skill).
   - The PR lists only what ran on the final code; run again after any later fix.
4. **Core approval:** a PR touching `moriaengine/GameCore/`, `Common/`, `Communication/`, `AuxiliaryServer/`
   or `Tools/SteamEngine.Generators*` gets the `needs-core-review` label and a review request to janslav
   (`gh pr edit --add-reviewer janslav`), and waits for his approval. A ruleset on `main` and the merge hook
   enforce it. Carry on with other work meanwhile.
5. **Merged:** CI green, then `gh pr merge --merge` (a merge commit, never squash or rebase). Every branch
   commit lands on `main` as it is, so each needs the subject `area: what changed (#n)` and a real step of
   the change, not "fix typo"; never push `fixup!`/`squash!` commits. The one exception is the
   "Merge branch 'main' into …" commit that updating a branch behind `main` adds. Delete the branch, and
   remove the worktree when done.
6. **Issue closed with a summary comment**: what shipped, the PR, follow-up issues, and what isn't tried in a
   real client (then also the `needs-in-game-check` label). The PR says `Closes #n`. Close with a separate
   update call where needed: `issue_write create` with `state: closed` leaves an issue open.

## Batches

When the maintainer gives a list and says to carry on, go from item to item without asking. Park questions
that are truly theirs, and ask them together at the end. Never decide on your own a design call that would
silently change behaviour project-wide. Work they deferred or parked isn't started until they pick it up.

The full protocol suite (~3 h) runs once at the end of a batch, or when the maintainer asks, not per PR.
Post its result on the batch's issue.
