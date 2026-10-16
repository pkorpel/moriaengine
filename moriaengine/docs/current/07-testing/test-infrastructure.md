# Test infrastructure

The xUnit suite (`Tests/`) is split into two tiers with very different cost and
isolation characteristics - picking the wrong one for a new test is the most common
mistake to avoid.

## Two tiers

**Fast/unit tier** - plain xUnit tests at the top level of `Tests/`, no real server
process involved. Pure logic (`ConvertToolsTests.cs`), or an in-process `EngineTestFixture`
using an isolated temp directory. Runs in a fraction of a second for the whole tier.

**Full-boot tier** (`Tests/Protocol/`) - real end-to-end tests that spawn actual
`AuxiliaryServer.dll`/`GameCore.dll` child processes (the same binaries `build.ps1`
produces) and drive them with a real wire-protocol client, exercising the complete
two-hop login flow described in
[Architecture overview](../01-overview/architecture-overview.md). This is where almost
all gameplay-system verification in this codebase lives - a protocol test is the
standard of proof for "this actually works," not just a passing unit test against
isolated logic.

## `GameServerPairFixture`

`Tests/Protocol/GameServerPairFixture.cs` is the shared fixture every full-boot test
collection uses. Its constructor:

1. Resolves the real repo root from `AppContext.BaseDirectory` (not the fast tier's
   isolated temp dir - these are separate OS processes with their own independent static
   state, there's no isolation to inherit).
2. Starts `AuxiliaryServer.dll`, waits 2 seconds for it to bind its ports before starting
   `GameCore.dll` (mirrors `build.ps1`'s own "-Run Both" sequencing).
3. Both child processes get a dedicated stdin pipe held open for the fixture's whole
   lifetime - each one's `Main()` blocks on `Console.ReadLine()` in a background thread
   and shuts down the instant that returns, so an un-redirected stdin would make the
   child inherit whatever the *test process's* own stdin happens to be.
4. Blocks until `GameCore`'s stdout contains the literal marker `"Init done."` (60s
   timeout) before returning - a test that starts running before this returns would be
   racing the server's own boot sequence.

**Fixed ports** (`LoginPort = 2593`, `GamePort = 2595`) - this is the single most
important constraint to know about this fixture. See
[Known gotchas](known-gotchas.md#never-run-two-full-boot-test-invocations-concurrently)
for what happens when two full-boot runs overlap.

`GameOutputSnapshot()`/`WaitForGameOutput(marker, timeout)` let a test poll for a
specific log line the server-side diagnostic code printed - the standard way a protocol
test proves server-side state changed, when the wire protocol itself doesn't expose it
directly. `WaitForGameOutput` has a since-position overload for markers more than one
test method might print (see
[Known gotchas](known-gotchas.md#assertseen-vs-assertseensince)).

## `TestGameClient`/`TestLoginClient`

`TestLoginClient` drives the AuxiliaryServer login handshake; `TestGameClient` drives
the GameCore side (character list, enter world, and then whatever packets a specific
test needs - movement, target-cursor, gump responses, speech, etc). Both are real wire-
protocol clients, not mocks - they encode/decode the actual packet formats described in
[Networking & the protocol layer](../02-engine-core/networking-protocol.md).

New packet types get added to these classes incrementally, as tests need them - several
pages under [Gameplay systems](../README.md#4-gameplay-systems) mention a specific new packet
registration (`SendGumpResponse`'s switches-aware overload, `ReadSystemMessage`,
`ReadCharacterList`'s denial-reason decoding) added specifically to support one feature's
test. If a new test needs to decode or send a packet shape that doesn't exist yet, add
it here rather than hand-rolling bytes inline in the test itself.

## Account and character conventions

- `"protocoltest"` is the conventional account name most full-boot tests log in as -
  critically, it relies on being the server's **literal first-ever account**
  (`AbstractAccount.HandleLoginAttempt`'s `accounts.Count == 0` auto-create-and-elevate
  check fires once, ever, at the very first login any process-level `saves/` state has
  ever seen). See [Known gotchas](known-gotchas.md#the-first-account-is-permanently-gm-elevated)
  for the consequences of this and how fragile it is.
- A dedicated, differently-named account (e.g. `defpersistencetest`,
  `decaypersisttest`) is the right call whenever a test needs to trigger a **real**
  save, or needs guaranteed non-GM authority, or would otherwise interact unpredictably
  with other Facts sharing `"protocoltest"`. See
  [Known gotchas](known-gotchas.md#give-a-test-that-needs-real-isolation-its-own-account)
  for why "just reuse protocoltest" causes real, hard-to-diagnose cross-Fact failures.
- `AbstractAccount.maxCharactersPerGameAccount` caps every account at **5** character
  slots. A test file that accumulates more than 5 character creations under one shared
  account name will intermittently fail depending on xUnit's own execution order - give
  a later Fact in a crowded file its own dedicated account once this is a risk.
- Two **simultaneous** connections in one Fact (e.g. testing a denial between two
  live-connected characters at once) need two *different* accounts - a UO account only
  supports one active connection at a time, reusing one for both produces a login denial
  on the second connection attempt. This is a different constraint from the 5-character
  cap above (concurrent connections vs. cumulative character creation).

## See also

[Known gotchas](known-gotchas.md) for the accumulated list of sharp edges found while
building tests against this engine - read that page before writing a new protocol test
in an unfamiliar area, several of its entries have cost real debugging time more than
once.
