---
name: protocol-tests
description: Run and debug moriaengine's full-boot protocol tests (Tests/Protocol, GameServerPairFixture) safely in the dev container — filters, one run at a time, reading logs, telling flakes from regressions, known failing classes. Use before running scripts/test-full.sh or when a protocol test fails, hangs or looks flaky.
---

# Running full-boot protocol tests

Each class boots a real AuxiliaryServer + GameCore pair on fixed ports 2593/2595. Every boot also
compiles `moriaengine/scripts/` **from disk**.

## Run

```bash
scripts/test-full.sh "FullyQualifiedName~ClassA|FullyQualifiedName~ClassB"   # targeted, one dotnet test
scripts/test-full.sh                                                       # everything, ~3 h, once per batch
scripts/test-full.sh --kill-stale                                          # clean up leftovers, then exit
```

- **Only through `test-full.sh`**, never a raw `dotnet test` on `Tests/Protocol`. Run it in the
  background and relay its progress. A lock serializes runs across worktrees.
- **One build or test run at a time** (the 5 GB VM; `claude` is PID 1, see `CLAUDE.md`).
- **Don't edit anything under `moriaengine/scripts/` in that worktree while a run is going.** Each boot
  compiles from disk, so mid-run edits produce fake mass failures. If you must edit: stop the run, use
  `--kill-stale`, rebuild, and rerun from scratch. A burst of failures right after an edit means
  contamination, not bugs.
- Filters are substrings of the **class name** (`Q009Tests`, not the file name `Q009Q018Tests`). A
  filter that matches nothing passes without running anything. A short filter like `SettingsTests` pulls
  in many classes and can run past a timeout.
- Pick related classes generously: the area you touched plus its neighbours (after touching guards, run
  `CombatDepth`; after converter or speech changes, run `BootLogTests` and `SpeechStubTests`).
- `BootLogTests` fails on any boot ERROR/WARNING. Under load, boots take 90-130 s, so run it alone.
  A clean boot log proves nothing about lazily resolved things such as character models. Before trusting
  a boot log's error count, check that the log reached `Init done`.
- A two-boot restart test needs `STUCK_SECONDS=300`. The fixture's boot limit is `MORIA_BOOT_SECONDS`
  (90).

## Gate

```bash
scripts/test-full.sh "..."; rc=$?; [ $rc -eq 0 ] || exit 1   # before writing the PR body or merging
```

`rc=124` means it timed out, not that it passed. Never chain `gh pr merge` after a run.

## Reading results

- Live output cuts long messages short. The full log of every class that didn't pass is in
  `${TMPDIR:-/tmp}/moria-test-full/`. Read the `run.log` it names before suspecting the test. Find
  compile errors with `grep "rror CS"` in the log the script names.
- A CRITICAL log line fails no test, so watch the live output for it.
- Very long background runs can lose the start of their output. Compare the summary's FAIL count with the
  `[FAIL]` blocks you can see, and rerun suspect classes alone.
- To see what the server said, temporarily write `fixture.GameOutputSnapshot()` to a file from the test
  (xUnit truncates long assertion messages), rerun the one test, and grep the file for `ERROR`/`WARNING`
  or your marker. Remove the dump before committing. To read a probe's numbers, use a temporary failing
  assert that includes the game output.

## Flake or regression?

1. Check the boot log for ERRORs first; a duplicate-def error made `CombatDepth` flakes much worse.
2. `LoginDeniedReason=1`, "Game server never appeared", "GameCore did not reach Init done", or a broken
   pipe right after login: count logins per account (5 characters per account) before blaming
   contention.
3. Otherwise rerun the class **alone**. Under load (many server-booting classes, idle build processes),
   boots time out. Retry once.
4. Still red? A/B against unchanged code (a scratch worktree from `origin/main`, never `git stash`),
   same filter.
5. The full suite runs each class in its own `dotnet test`, so a green suite says nothing about failures
   seen only in mixed runs (#890 BankerDialog, #1313 ThingErrand).

Known failing classes on main (last full suite, 2026-11-07): AttrFlag, ContentRestoration,
LevelingIsolated, LocalizationVerification, Notoriety (Healer since fixed). Update this list when a full
suite says otherwise.

## Hangs

A test that goes silent mid-command means a hang in GameCore. Use the `debug-hang` skill (managed stack
dump) before adding more prints.
