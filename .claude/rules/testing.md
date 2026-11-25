---
paths:
  - "moriaengine/Tests/**"
  - "moriaengine/scripts/Tests/**"
---

# Tests

Read `moriaengine/docs/current/07-testing/test-infrastructure.md` and `known-gotchas.md` first: accounts and
their 5 character slots, the GM first account, argument shapes, `AssertSeenSince`, gump buttons and more. To run
and debug full-boot tests, use the `protocol-tests` skill.

## A test has to prove something

- **Red-check it:** revert just the fix (switch off only the line under test), see the test fail with the
  reported symptom, then restore. Confirm a server really booted, not a cached pass.
- **No vacuous passes.** Assert that the branch ran (the dialog's title, the marker, the state change).
  `a != null || b != null` never reads `b`. A direct helper call proves nothing about the `.def` line that
  calls it, so run the real trigger. Assert the invariant itself (`damage >= 0`), not a relative comparison.
  Test a refusal next to an allowed case.
- **Before calling a failure "pre-existing"**, A/B it against the unchanged code with the same filter, in
  isolation.
- Pin tunables and dates in the test. Poll instead of sleeping a fixed time. Statistical checks need enough
  samples (2000, not 500) and loose ratios.
- Player behaviour is tested from an auto-created account, not the GM first account. Count the logins per
  account before writing a new Fact.
- Fast-tier tests have no loaded content. Extract pure logic into directly testable functions where possible.
- Clean up the walls, items and NPCs a test places.
