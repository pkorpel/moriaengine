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
`Tests/xunit.runner.json` (`diagnosticMessages: true`, `longRunningTestSeconds: 15` - was 30
before #785) fixes this with xUnit's own built-in feature - any test still running past 15 seconds gets a
periodic `[Long Running Test] '<full test name>', Elapsed: <time>` line printed to the
normal console output, repeated while it keeps running. Confirmed: a real 40-second test
printed exactly one clean heartbeat line, no other noise added. The file must stay at
`Tests/xunit.runner.json` and ship with `CopyToOutputDirectory` in the `.csproj` - xUnit
reads it from next to the compiled test assembly at runtime, not from the source tree.

## Knowing right away when a run is stuck (#785)

The user's requirement: never wait on a test run without knowing what it's doing. Five pieces:

1. **Live progress** - `scripts/test-full.sh` streams every test result the moment it's in
   (`pass`/`FAIL` lines), a `still running (Ns)` line from the long-running heartbeat above, the
   server pair's own events (`srv BOOT`/`READY`/`UNRESPONSIVE`/`STOPPED` - written by the fixture to
   the file in `STEAMENGINE_TEST_PROGRESS_FILE`), and a heartbeat at least every
   `HEARTBEAT_SECONDS` (20).
2. **Stuck = killed and named** - a test running longer than `STUCK_SECONDS` (120) makes the
   script kill the run's whole process group and report `STUCK <test>`; the class is not retried.
   The full log of anything that didn't pass stays in `$TMPDIR/moria-test-full/<class>.log`.
3. **Dead server = fail fast** - `ServerHealth` (Tests/Protocol/ServerHealth.cs): every
   `TestLoginClient`/`TestGameClient` checks in before connecting and a completed login reports
   back. Once GameCore has exited, or logins have failed for 30 s across more than 3 attempts, the
   pair is declared dead for the rest of the fixture: every later client throws
   `ServerUnresponsiveException`, which (unlike the `InvalidOperationException`s the per-test
   retry loops catch) ends the test at once. Before, each test of a broken class retried 40 times
   with 15 s read timeouts - ~2m15s per test, 20+ minutes per class, all silent. GameCore's
   **stderr is captured** into the game output too, so a crash shows its real cause (that's how
   #786, a stack overflow behind the old "CombatDepthTests flake", was found).
4. **No collisions** - `xunit.runner.json` has `parallelizeTestCollections: false` (xUnit used to
   boot several collections' server pairs at once on the same ports and `saves/` - a run raced
   against itself), and the fixture holds a machine-wide lock
   (`$TMPDIR/moria-protocol-servers.lock`, released by the OS if the process dies) for its whole
   life: a second concurrent run fails within 15 s with "Another protocol test run is using the
   server ports" instead of tangling with the first.
5. **No orphans** - the fixture passes its PID in `STEAMENGINE_PARENT_PID`;
   `ParentProcessWatchdog` (Common) in both GameCore and AuxiliaryServer kills the server within
   ~2 s once that process is gone. The fixture's own teardown kills each server's whole process
   tree (`Kill(true)`, catching nested script-compile builds). `test-full.sh` runs each
   `dotnet test` in its own process group (`set -m`), kills it on Ctrl+C/kill, and also when its
   own parent disappears (e.g. a background-task wrapper shell was killed - the case that left a
   full run alive and colliding with the next one). `scripts/test-full.sh --kill-stale` cleans up
   anything left anyway, and every run refuses to start while leftovers exist.

Checking for leftovers: `kill -0` is not enough in this sandbox - dead processes stay as unreaped
zombies (state `Z` in `/proc/<pid>/stat`), which `kill -0` still reports as alive.

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

**Both clients' sockets have a 15-second default `ReadTimeout`**, set once in each
constructor. Real bug found and fixed here: this was missing entirely until it was - every
read in `TestGameClient.ReadPacketPayload` computes its own per-call deadline and sets
`ReadTimeout` accordingly, but the *earlier* login/character-list phase
(`TestGameClient.ReadOneCompressedFrame`/`ReadCharacterList`, and every read helper in
`TestLoginClient`) called `stream.Read()`/`ReadByte()` directly with no timeout at all,
relying on `NetworkStream`'s own default of `Timeout.Infinite`. Since
`ConnectLoginAndCreateCharacter`'s retry loop creates a fresh client (and so a fresh
stream) on every attempt, this unbounded wait was re-armed on every single retry too - if
the server was ever slow/unresponsive during exactly that window, the read blocked
forever, not just a long time. This caused two separate multi-minute test "hangs" in one
session that had no explanation anywhere else, since every *other* wait in the suite was
already correctly bounded. The resulting `IOException` on timeout is converted to
`InvalidOperationException` at the call sites the existing `ConnectLoginAndCreateCharacter`/
`WaitForServerListingAndGetRedirectPort` retry loops already catch - deliberately **not**
inside the shared `ReadOneCompressedFrame`/`FillQueueFromOneFrame` primitive itself, since
that's also used by `ReadPacketPayload` deep inside real test assertions, which already has
its own `IOException` -> `TimeoutException` conversion that some tests explicitly
`Assert.Throws` against (converting inside the shared primitive shadowed that and broke
those assertions - a real regression caught by a full-suite run before this shipped).

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
