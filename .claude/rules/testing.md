---
paths:
  - "moriaengine/Tests/**"
  - "moriaengine/scripts/Tests/**"
---

# Writing tests that prove something

Running full-boot tests (filters, logs, hangs, known failures) is covered by the `protocol-tests` skill.

## Make it fail first

- Red-check every test: revert just the fix (switch off only the line under test), see the test fail
  with the reported symptom, then restore. Confirm a server really booted (timing), not a cached pass.
- Watch for vacuous passes. Assert that the branch ran (the dialog's title, the marker, the state
  change). `a != null || b != null` never reads `b`. A direct helper call proves nothing about the
  `.def` line that calls it, so run the real trigger. Assert the invariant itself (`damage >= 0`), not a
  relative comparison. Blocked-step tests need a control that isn't blocked.
- Before calling a failure "pre-existing", A/B it against the unchanged code with the same filter, in
  isolation.
- Pin tunables and dates in the test. Don't depend on live defaults or the real calendar.
- Poll instead of sleeping a fixed time. Reading a timer right after setting it can fail.
- Statistical checks need enough samples (2000, not 500) and loose ratios. To force a failure at
  difficulty 0 (50%), try 100 times.
- Fast-tier tests have no loaded content (`GetByDefname` is unavailable), so read `.def` files directly.
  Extract pure logic into directly testable functions where possible.

## Accounts and characters (full-boot tests)

- **An account holds 5 characters, and each login creates one.** A 6th breaks a later login with an
  unrelated-looking error (broken pipe, "server never appeared"). Count logins per account with grep,
  chain new checks onto an existing Fact, and give new Facts their own account. A failure that grows with
  the fact count is this, not a race.
- One active connection per account: two characters connected at once need two accounts. A collection
  auto-creates only its first account; others need `.TestEnableAutoAccountCreation` (guard it with a
  static bool, because each call uses a slot).
- Character names: letters and spaces only. A digit or underscore fails silently.
- The server's first-ever account is a GM and goes through the LScript parser. GMs bypass cooldowns,
  costs and equip rules. Test player behaviour from an auto-created account. A "plain player" test must
  lower **MaxPLevel**, not PLevel, or it passes without the allowlist line.
- Command arguments: on an elevated account, quote word args (bare words resolve to defs; `in` is a
  keyword). On a plain account, `Argv` is one literal string that keeps its quotes. Helpers must handle
  both. Rapid commands trip the hammering rule: call `ForgetAttempts`.

## The world around the test

- The default spawn (4763,1363) is a boxed-in platform at z 10 over land z 0, with a static to its east.
  Use `TestTeleportToOpenGround` (about 7-9 tiles clear) before ground casts, placing items or walking.
  Keep distances around 6 tiles and assert LOS. Probe `Map.CheckMovement` before multi-tile walks; the
  first step in a new direction only turns.
- The client only reaches what it can see. Open the pack, chest or corpse before dragging or targeting
  inside it (or use `Trigger_DClickFromAfar`). A target far away is outside update range.
- `Go`/`P` don't fire step triggers (`t_telepad`), so take real steps. `P()` keeps the plane; `Go()`
  changes it. `P()` doesn't set `Flag_Moving`.
- Read a dead NPC's X/Y/Name before `CauseDeath` (it's deleted). `CauseDeath` on a dead character does
  nothing. Hitting your own pet makes it rebel. A bare test character's blow does 0 damage through a
  rabbit's armor. A test smith needs Tinkering too: a missing skill is a silent refusal.
- Long runs reach night (darkness refuses casts), so give night sight. Clean up placed walls, items and
  NPCs; leftovers break later tests.
- Use `AssertSeenSince(snapshotLength)`/`WaitForGameOutputSince` for a marker sent more than once.
- A test that closes `Test*` commands closes them for its whole server: run it last or in its own
  collection. Give Facts that kill players or NPCs their own collection.
- Test-only `.def` hex headers must not collide with other test defs. Changing `.SetupWorld` pieces
  means updating the counts in `WorldSetupTests` and `WorldSetupRestartTests`.

## Gumps

- The server accepts any button id, so assert that the layout draws a new button before pressing it:
  `Assert.Matches(@"<up>\s+<down>\s+1\s+0\s+<id>\b", dialog.LayoutText)`. Wait on titles unique to the
  expected page, and assert each intermediate dialog. Label text is in the strings list
  (`ReadGumpDialogWithStrings`). A gump the server re-sends must be read, or later reads lag one behind.
- The test client clicks through `On_Click`. Test the AOS path server-side with `Trigger_AosClick`.
  A green test says nothing about 3.0.6m or TazUO, and gump art can't be checked from tests.

## Persistence and LScript in tests

- Persistence bugs need a real save, restart and reload (`BootPreservingSaves`). Save with timers running.
- `RunSnippet` takes no multi-line `foreach`: put the LScript in a `.scp` `[function]` and call
  `ScriptHolder.GetFunction(name).Run`.
- An `ICollectionFixture` type needs exactly one public constructor.
