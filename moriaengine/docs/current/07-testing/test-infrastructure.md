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

## Telling "slow" from "stuck": `xunit.runner.json`'s long-running-test heartbeat

A full-boot `dotnet test` run can go several minutes between visible console lines even
when perfectly healthy - a single AI-combat test can legitimately take 40-60+ seconds. With
no intermediate output, a long silent stretch is genuinely ambiguous: slow, or actually
hung (same fixed-port concurrent-invocation hang class covered in
[Known gotchas](known-gotchas.md#never-run-two-full-boot-test-invocations-concurrently))?
`Tests/xunit.runner.json` (`diagnosticMessages: true`, `longRunningTestSeconds: 30`) fixes
this with xUnit's own built-in feature - any test still running past 30 seconds gets a
periodic `[Long Running Test] '<full test name>', Elapsed: <time>` line printed to the
normal console output, repeated while it keeps running. Confirmed: a real 40-second test
printed exactly one clean heartbeat line, no other noise added. The file must stay at
`Tests/xunit.runner.json` and ship with `CopyToOutputDirectory` in the `.csproj` - xUnit
reads it from next to the compiled test assembly at runtime, not from the source tree.

## `GameServerPairFixture`

`Tests/Protocol/GameServerPairFixture.cs` is the shared fixture every full-boot test
collection uses. Its constructor:

1. Resolves the real repo root from `AppContext.BaseDirectory` (not the fast tier's
   isolated temp dir - these are separate OS processes with their own independent static
   state, there's no isolation to inherit).
2. **Deletes every `saves/` snapshot except the git-tracked `saves/2000-01-01/00_00_00`
   bootstrap skeleton**, before starting either child process. A process killed mid-run
   (a hung/duplicate `dotnet test` invocation, or an earlier collection's own fixture
   being torn down mid-save) can leave behind a snapshot whose only account isn't
   `"protocoltest"` - GameCore auto-loads the most recent snapshot on boot, and since the
   first-account auto-creation check above only fires when `accounts.Count == 0`, a stale
   leftover account silently breaks every `"protocoltest"`-dependent test in the run with
   `LoginDeniedReason=0` (NoAccount) cascades that have nothing to do with whatever's
   actually being tested. Wiping *only* the non-bootstrap snapshots (never the whole
   `saves/` tree - `WorldSaver.Load()` fails outright with nothing to load at all
   otherwise) guarantees every fixture boot starts from that known-clean world.
3. Starts `AuxiliaryServer.dll`, waits 2 seconds for it to bind its ports before starting
   `GameCore.dll` (mirrors `build.ps1`'s own "-Run Both" sequencing).
4. Both child processes get a dedicated stdin pipe held open for the fixture's whole
   lifetime - each one's `Main()` blocks on `Console.ReadLine()` in a background thread
   and shuts down the instant that returns, so an un-redirected stdin would make the
   child inherit whatever the *test process's* own stdin happens to be.
5. Blocks until `GameCore`'s stdout contains the literal marker `"Init done."` (60s
   timeout) before returning - a test that starts running before this returns would be
   racing the server's own boot sequence.

Steps 3-5 are wrapped in a `try`/`catch` that kills both child processes before
rethrowing on ANY failure (most commonly the step-5 boot timeout, when GameCore's own
nested script-compile step stalls under sandbox CPU contention). This isn't cosmetic -
without it, a failed constructor returns no object for xUnit to `Dispose()`, so the
already-spawned processes were silently abandoned, still running, still holding the
fixed ports. A real run was observed leaking 9 separate orphaned pairs this way over its
lifetime - each failed collection left its own pair behind while `dotnet test` moved on
to the next collection, whose own compile step then had to compete with an ever-growing
pile of already-stuck prior attempts for the same limited CPU, turning one transient
hiccup into a self-reinforcing whole-run cascade instead of an isolated failure.

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
