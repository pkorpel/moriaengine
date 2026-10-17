---
description: Turn an idea into a GitHub issue in triage, from the matching template
argument-hint: <what you want, in a sentence>
---

Create one issue in `pkorpel/moriaengine` from this request: $ARGUMENTS

1. Pick the template from `.github/ISSUE_TEMPLATE/`: feature, bug or chore. A spike
   or docs task uses chore with `type:spike` / `type:docs`.
2. Fill it in, in English. Acceptance criteria must be things someone can check on a
   local boot of `agent-kit-trial` (a test or the console log); write "Done when" items the
   same way for a chore.
3. Labels: exactly one `type:`, one `prio:` (`prio:1` urgent, `prio:2` next,
   `prio:3` some day) and the `area:` labels that apply (`combat`, `skills`,
   `magic`, `economy`, `npc`, `world`, `login`, `infra`). **No `status:` label**: a
   new issue is in triage, and only the user moves it to `status:ready`.
4. Look for duplicates first (`search_issues` / `list_issues`, open and closed). If
   one exists, show it and ask instead of creating.
5. Show the title, labels and body to the user and create the issue only after they
   agree (`issue_write`, method `create`). Title short, no prefix.
6. Feature and bug: run the `analyst` agent on the new number (as `/task-refine`
   does) and show its criteria and open questions. Chores, spikes and docs only when
   the "Done when" list was hard to write.

End with the issue link and number. Don't branch or start work: that's `/task-pick`
or `/task-run`, after the user marks it `status:ready`.
