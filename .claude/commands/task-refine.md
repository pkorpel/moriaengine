---
description: Have the analyst turn a triage issue into a buildable story with checkable criteria
argument-hint: <issue number>
---

Refine issue $ARGUMENTS of `pkorpel/moriaengine` with the `analyst` agent
(`.claude/agents/analyst.md`).

1. The issue must be open and have no `status:` label (triage). Anything else: say
   what you found and stop; a ready or started issue is not re-specified silently.
2. Run the `analyst` agent with the number.
3. Show the user the new criteria, the open questions and any proposed split. Open
   questions: ask them now and, with the answers, run the analyst once more.

The user moves the issue to `status:ready`; this command never does. End with the
issue link.
