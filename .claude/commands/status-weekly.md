---
description: Write this week's status page into docs/wiki from the issues and ship it
---

Write `docs/wiki/Status/Weekly/<YYYY-Www>.md` (ISO week, today's date) for
`pkorpel/moriaengine` from GitHub. Generated page: overwrite it if it exists,
never merge with hand edits.

Content, each item as `#n title`:

- First line: `Updated by: /status-weekly · Last reviewed: YYYY-MM-DD`.
- **Done**: issues closed in the last 7 days; PRs merged in that time.
- **In progress**: `status:doing`, `status:review`, with the open PR.
- **Blocked**: `status:blocked`, with the reason from the latest comment.
- **Next**: `status:ready`, by `prio:`.
- **Triage**: open issues with no `status:` label, count and the three oldest.
- **CI**: last `.NET Build` run on `agent-kit-trial` (result, date); Actions minutes used this
  month if the API shows them.

Facts only; no commentary. Show the page to the user, then ship it as a docs change:
a worktree from `origin/agent-kit-trial` on branch `status-<YYYY-Www>`, commit
`docs: weekly status <YYYY-Www>`, PR, merge when CI is green (task-ship's step 8
rule). Link it from `docs/wiki/Home.md` under Status (keep the four newest weeks).

On the first Monday of a month, also propose a `type:docs` issue "Docs review
YYYY-MM" listing wiki pages whose `Last reviewed` date is more than 90 days old (ask
before creating it; skip if none).
