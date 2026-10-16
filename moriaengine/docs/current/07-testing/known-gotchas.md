# Known gotchas

Sharp edges found while building protocol tests against this engine, accumulated across
many features. Several of these have independently cost real debugging time more than
once - read this before writing a new test in an unfamiliar area of the codebase.

## CanReach runs after the deny-trigger chain

A correctly-**allowed** action and one correctly-**denied** for an unrelated reason (too
far away, line of sight blocked, container not open) produce the **identical** "nothing
happened" observable symptom from outside. `AbstractCharacter.CanPickup` (and the
equivalent checks for other actions) runs `CanReach` strictly *after* the full
deny-trigger chain already passed - see
[Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md). A test character
that never actually stands at the right place, or never opens the right container
first, can silently validate the *wrong* thing while still reporting "denied" as if the
feature under test did its job.

Two concrete, independently-discovered instances of this:
1. Every fresh test character spawns at its own far-away default position - reuse
   `MovementSpeedTests.TestTeleportToOpenGround` to actually stand at the relevant
   location before testing a pickup/interaction denial.
2. Taking an item *out* of a container (even one the picker legitimately has access to)
   requires that connection to have already explicitly double-clicked/opened the
   container first (`OpenedContainers.HasContainerOpenFromAt`) - a void deny-hook that
   doesn't itself set `args.Result` doesn't short-circuit this check for an **allowed**
   picker, only for an actually-denied one. See
   [House lockdown & security](../04-gameplay-systems/house-lockdown.md) for where this
   was hit.

## AssertSeen vs AssertSeenSince

`AssertSeen("MARKER:", ...)` is unsafe for **any** marker string a Fact/Theory sends more
than once within a shared-fixture collection - even the *first* time it's introduced, if
that same Fact/Theory will reuse the marker later. A later occurrence can satisfy an
earlier, unrelated wait instantly, because `AssertSeen` just checks "does this string
appear anywhere in the log so far," not "does it appear *after* the command I just sent."

Fix: capture `GameOutputSnapshot().Length` before sending the command, then use
`AssertSeenSince` with that captured position. **General rule**: any marker a test will
send more than once in a shared-fixture collection needs `AssertSeenSince` from its very
first use, not just its second - the bug doesn't announce itself until the second usage
actually happens, by which point the first usage's own assertion already "passed" for
the wrong reason.

## The first account is permanently GM-elevated

`AbstractAccount.HandleLoginAttempt`'s `accounts.Count == 1` check (equivalently,
`accounts.Count == 0` just before creation) fires once, ever, at the very first account
created against a given `saves/` state - and the elevation is **account-wide and
permanent**, not per-character and not per-session. Every character ever created under
that one lucky first account is already a GM, with zero code anywhere checking or
granting it per-session.

Consequences:
- Any test relying on `"protocoltest"` being that lucky first account (the conventional
  choice, see [Test infrastructure](test-infrastructure.md)) silently gets full GM
  authority for free - including bypassing any `IsGM`-gated check the test might actually
  be trying to verify works correctly for a *non*-GM player. A test specifically
  targeting non-GM behavior needs a dedicated, deliberately-non-first account.
- `TestSetGmOn`'s `PLevel = MaxPLevel` only reaches **real** GM authority on that one
  literal first account - setting it on any other account raises the `IsGM`-flag-based
  check but does **not** pass a genuine `CommandsAuthorization` check (a different,
  stricter gate used by real GM-only commands). `SocialTests.TestPromoteToGM`
  (reflection-based plevel promotion, account-order-independent) is the correct fix
  whenever a non-first-account test character needs to pass a *real*
  `CommandsAuthorization` check, not `TestSetGmOn`.

## Give a test that needs real isolation its own account

Several unrelated-looking test failures across this codebase's history turned out to
share one root cause: two Facts in the same `GameServerPairFixture` collection, sharing
one account name, interacting in a way neither Fact's own logic explains. Concrete
instances:
- A Fact that deliberately triggers a real `Globals.Save()` leaves a persisted character
  in the shared `saves/` directory - a *later-booting* fixture in the same full-suite run
  can auto-load that leftover character and break an unrelated Fact's "fresh account, no
  characters yet" assumption.
- A second character created on an account that already has one actively connected can
  make lookups that depend on exactly-one-active-connection-per-account behave
  unpredictably (confirmed for `ProfessionDef.GetByDefname` returning null in exactly
  this situation; never root-caused further than "account sharing is the trigger").
- A Fact that triggers a real player/NPC death, sharing a fixture with a later, unrelated
  Fact that just needs a clean dialog/packet check, produced deterministic (not flaky)
  cross-Fact failures that retries/timeouts/corpse-cleanup never fully fixed. The actual
  fix was giving that one Fact its own dedicated
  `[CollectionDefinition]`/`ICollectionFixture<GameServerPairFixture>`, not more
  defensive cleanup.

**Pattern worth internalizing**: when two Facts in the same fixture show a *deterministic*
(not transient) interaction that neither Fact's own logic explains, suspect account- or
fixture-sharing first, before chasing the specific symptom further.

## `ScriptArgs`'s several shapes

A command argument reaching a `[SteamFunction]` through `Commands.InvokeCommand` can
arrive in genuinely different shapes depending on which parser handled it - gated purely
on `commandSrc.MaxPlevel >= Globals.PlevelForLscriptCommands` (default 2), which in
practice means "is this the one lucky first/GM-elevated account or not" (see above):

1. **`SimpleCommandParser`** (non-elevated accounts) - one combined, un-split string
   argument in `Argv[0]`, regardless of how many space-separated tokens it logically
   contains. Also does **not** strip quotes - a quoted literal arrives still containing
   the literal quote characters.
2. **Full LScript parser, multi-token** (elevated accounts) - genuinely split into
   separate `Argv[i]` entries.
3. **Full LScript parser, single numeric token** (elevated accounts) - a boxed `int` (or
   other value type) sitting directly in `Argv[0]`, **not** a string at all. Code that
   assumes `Argv.Length == 1` always means "an un-split string to manually parse" will
   crash trying to cast that boxed value to `string`.

Any test `[SteamFunction]` taking 2+ arguments needs to tolerate shapes 1 and 2
(`sa.Argv.Length >= 2` vs. manually splitting a single combined string) unless it's
certain to always run from the one elevated account; one taking a single numeric
argument needs to tolerate shape 3 too (check `Argv[0] is string` before treating it as
something to split).

## A bare word matching a registered defname gets resolved, not kept literal

On an elevated (GM-plevel/first) account, a command argument that happens to match *any*
registered `AbstractScript` defname - an item, a skill, anything - gets resolved by the
full LScript parser into that live object's own string form, not kept as the literal
text that was typed. E.g. `.TestGrantItem i_sword_viking_copper` silently becomes
`TestGrantItem("copper viking sword: i_sword_viking_copper// (5049)")` server-side, with
no error. This **only** affects elevated-account connections (full LScript parser); the
same argument reaches `SimpleCommandParser` completely literally for ordinary accounts.

Fix: quote the argument (`.TestGrantItem "i_sword_viking_copper"`) for elevated-account
test commands specifically - but do **not** quote it for `SimpleCommandParser`-routed
commands, since that parser doesn't strip quotes at all (see the `ScriptArgs` shapes
above) and a quoted literal would arrive containing the quote characters themselves.
Whether a given test command needs quoting depends on which parser its caller's account
routes through, not on the command itself.

## A command string colliding with a property/method name silently no-ops

A `[SteamFunction("Name")]` command string that happens to match an instance property or
method name on the type it's invoked against gets silently resolved by LScript's own
member-resolution path as a property *read* on `self`, instead of ever invoking the
command method - no exception, nothing happens, the command just logs as having run
successfully. Concrete instance: `[SteamFunction("Race")]` colliding with
`Player.Race` (see [Professions](../04-gameplay-systems/professions.md)). The fix each
time has been renaming the *command string*, not the property - the underlying
LScript resolution-order footgun itself is unfixed at the engine level.

## Never run two full-boot test invocations concurrently

`GameServerPairFixture` uses fixed ports (`2593`/`2595`). Two `dotnet test` invocations
against full-boot-tier tests running at the same time don't error out cleanly on the
port conflict - both just **hang indefinitely**, with near-zero CPU, no output, and no
timeout that fires on its own. This has happened more than once from a stale/empty
background-task notification making an earlier run look finished when it hadn't been -
always confirm a run has genuinely completed before starting another one against the
same fixture.

A related trap: a run that was believed abandoned or moved past (killed, or assumed
finished) can still be alive in the background indefinitely once its own hang triggers,
silently holding the fixed ports and sabotaging a later, completely unrelated test run
with zero output for many minutes. Check `/proc` for stray `dotnet test`/`testhost`
processes before trusting "no output yet" at face value on anything that normally
finishes in 1-3 minutes.

## Stale `saves/` snapshot causes a misleading "NoAccount" cascade

`GameServerPairFixture` intentionally shares the real repo's `saves/` directory across
every full-boot-tier run (not isolated per fixture). `BackupsManager` always auto-loads
the single most-recently-written `saves/<date>/<time>/` snapshot on boot. If *any* test
run leaves behind a snapshot whose only account is something other than
`"protocoltest"` (a dedicated-account test that deliberately triggered a real save, see
[Test infrastructure](test-infrastructure.md)), every subsequent `"protocoltest"`-reliant
test - in any later collection, even in a future session - fails with a login denial
(`NoAccount`, reason 0), since `accounts.Count == 0` (the only auto-create trigger) never
fires again once *any* account already exists in the loaded snapshot.

**Fix, same every time this has recurred**: delete the stale `saves/<date>/` directory
(gitignored-in-spirit scratch data, zero product value) so the next boot falls back to
the clean committed baseline. **Diagnostic tip**: if a NoAccount-style cascade suddenly
affects many unrelated tests after a heavy local testing session, check for a stale
non-`protocoltest` snapshot *before* suspecting the actual code change under test - this
exact mechanism has produced a misleading "my change broke everything" signal multiple
times.

## `dotnet`/`msbuild` zombie processes after a full-boot run

After any full-boot-tier run, you'll see hundreds of leftover `dotnet`/`msbuild`/
`VBCSCompiler` entries under `/proc` in state `Z` (zombie). These consume ~0 resources
and can't be signaled/killed (there's nothing left to kill) - they are not a real leak
and not the cause of a slow or hung run. If a run times out, it's sandbox scheduling
contention, not these zombies.

## Character names can't contain underscores

The server's real name validator rejects anything but letters and spaces ("Illegal name
- Contains something other than letters and spaces"). A test name built by string
concatenation with an underscore separator (`"AttrMove_" + mode`) fails outright at
character creation - use plain concatenation instead (`"AttrMove" + mode`).

## A test-helper that "creates a character" may not be idempotent

`EnsureAutoAccountCreationEnabled()`-style helpers that create a throwaway character to
flip a setting are not necessarily safe to call more than once per test process - each
call can consume one of the account's 5 character slots (see
[Test infrastructure](test-infrastructure.md)) even on a "no-op" repeat call, silently
exhausting the cap well before a naive reading of the test file would suggest. Guard with
a `static bool` "already done" flag if a helper like this gets called from more than one
Fact/Theory case sharing a process.

## Lazy `.def` expression evaluation inside a command's own call stack gets denied

A `.def`-authored lazy expression field (e.g. an animal's `DeathSound = snd_X + N`) is
evaluated on first access via `OpNode_Lazy_Expression.Run`, which calls
`Commands.AuthorizeCommandThrow` for that evaluation - harmless in real gameplay (a real
monster death from combat damage is always asynchronous, via a timer), but if the
triggering code runs **synchronously inside a chat command's own call stack**
(`Commands.commandRunning == true`), the expression's own symbol name gets
authorization-checked as if *it* were a command - and obviously isn't allowlisted, so
it's denied. This has surfaced more than once via test code calling something like
`CauseDeath` directly from a `[SteamFunction]` invoked as a command. Fix in the test (not
the engine): defer the actual triggering call to a near-immediate `BoundTimer`, so it
runs after the command handler itself has already finished - this is also a more
accurate model of how the real, async trigger actually fires.

## `Character.CauseDeath` is a no-op once already dead

Calling `CauseDeath` a second time on an already-`Flag_Dead` character does nothing at
all, silently - working as designed, but a real trap for a test that re-kills the same
character without resurrecting first. The second call's own assertions can appear to
"pass" for a completely unrelated reason (nothing changed because nothing ran, not
because the feature under test correctly produced no change). See
[Corpses & death](../04-gameplay-systems/corpses-and-death.md).

## `[Flags]` enum `ToString()` order is declaration order, not set order

A `[Flags]` enum's default `ToString()` lists set flags in the order they're **declared
in the enum**, not the order they were OR'd together or appeared in a `.def` line. A test
asserting an exact string against a multi-flag value needs to match declaration order,
not authoring order - see [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md).
