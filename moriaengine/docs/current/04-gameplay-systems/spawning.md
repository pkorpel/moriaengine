# Spawning

Spawn gems (`spawn.scp`/`SpawnInfo.cs`) are placed world objects that periodically create
NPCs, cap the live population they've created, and reschedule themselves as those NPCs
die. Built out across several passes (#191, #206, #208, #210) from an initial state that
existed as data model + dialog but had never actually worked end to end.

## Data model

`SpawnInfo` (`scripts/Basic/Spawning/SpawnInfo.cs`) holds a list of `SpawnEntry` (one
possible monster type each, with a relative `Probability` weight - weights don't need to
sum to anything, `RollSpawnDef` normalizes at roll time), a population cap (`Amount`,
shared across all configured entry types - not per-type), and a respawn delay range
(`MinTime`/`MaxTime`, now `double` minutes, not `short` - see "Decimal respawn delay"
below).

`SpawnEntry.HasValidCharDef` (`SpawnInfo.cs:69-74`) checks whether a configured chardef
actually resolved, in plain C# rather than LScript's own `||` - LScript's logical-or
evaluates both operands unconditionally, so `SpawnDef == 0 || !SpawnDef.IsCharDef` would
throw a `NullReferenceException` on the second operand whenever the first was already
null.

## Fixing the original dialog→gem→spawn flow (#191)

The `.spawn` command and dialog already existed as a data model + gump, but the flow had
never actually worked end to end - 4 separate bugs found via real protocol tests: wrong
`ImprovedDialog` API calls, wrong argv indexing (raw-LScript dialogs only get a
2-element `ScriptArgs`; custom `DialogArgs` live on `argv(0).InputArgs[i]`, not a 3rd
argv slot), `AbstractDef.Get` where `GetByDefname` was needed, and a
`NullReferenceException` on invalid-chardef redisplay (the `||` short-circuit issue
above). All fixed; the full dialog→gem→spawn flow now has permanent protocol-test
coverage (`Tests/Protocol/SpawnCommandTests.cs`).

## Homedist/`WanderLeash` (#206)

`SpawnInfo.Homedist` was stored on the spawn gem but **never read anywhere** - the
actual "stay near home" mechanism already existed as `NPCAiTimer.WanderLeash` (see
[Pets & NPC AI](pets-and-npc-ai.md)) but was a hardcoded constant for every NPC,
completely ignoring the configured value. `spawn.scp`'s `@spawnNow` now sets
`NPCAiTimer.GetFor(char).WanderLeash = <tag.spawnInfo.Homedist>` immediately after
creating each spawned character.

## Respawn-timer units bug (#206)

The respawn timer was computing `{MinTime,MaxTime} * 10` instead of `* 60` -
`AddTimer`'s delay argument is seconds, but the dialog labels Min/MaxTime as "[min]"
(minutes), so a configured "1-2 minute" respawn was actually happening in 10-20 seconds.
Fixed by using the correct multiplier.

## Decimal respawn delay (#210)

`SpawnInfo.MinTime`/`MaxTime` are `double`, not `short` - a GM can configure sub-minute
delays (e.g. `0.5` = 30 seconds). This needed the actual roll to move **out of** LScript's
`{a,b}` random-range operator and into C# (`SpawnInfo.RollRespawnDelaySeconds()`) -
that operator's "Final" node hardcodes `Convert.ToInt32` on both ends before rolling, so
it can never produce a genuinely fractional result regardless of the operand types. This
was confirmed directly (not assumed) by testing `{1,2}` 2000 times via a reused compiled
AST - a clean 50/50 split between exactly 1 and 2, proving the mechanism is unbiased on a
2-outcome range (an earlier user report of "always picks min or max" was genuine,
unbiased behavior on a tiny range, not a bug) but also confirming decimals genuinely need
their own path outside that operator.

## Weighted multi-monster spawn list (#210)

A spawn gem can configure multiple possible monster types (`SpawnEntry`/
`SpawnInfo.Entries`, replacing an earlier single-chardef field), each with its own
relative probability. `RollSpawnDef()` picks one per spawn event, weighted among entries
with a valid chardef.

The spawn dialog gained one row per entry (Baseid/Probability/remove-button, a fixed "+"
button to add a blank row) - each row is its own 1-row `GUTATable` rather than one table
with an unknown-in-advance row count, since a `GUTATable` can't grow after construction.
See [Dialog framework](../03-admin-tooling/dialogs-framework.md) for `GUTATable` and the
button-ID-range response-matching (`ON=300 399`) this dialog uses.

## Double-click-to-edit (#208)

Double-clicking an already-placed spawn gem reopens the `.spawn` dialog pre-filled with
that gem's own current settings, GM-only, letting settings be edited without deleting and
recreating the gem. Editing only affects the gem's stored settings going forward - no
retroactive change to an already-spawned NPC's `WanderLeash` or an already-pending
respawn timer's already-rolled delay.

## A real engine-level bug found via spawn testing (#206)

Killing a spawned NPC sometimes never produced a replacement at all, even with the above
fixes in place. Root-caused to a general engine bug in `Thing.InternalDeleteNoRFV()`:
`base.Delete()` (which clears all per-instance plugins/trigger-groups/timers) ran
**before** `Trigger_Destroy()` (which fires `@destroy`) - so any per-instance trigger
group relying on `@destroy` was already wiped out by the time the trigger fired, and
silently never ran. This specifically broke `spawn.scp`'s dynamically-attached
`e_spawned_char` trigger group, whose `@destroy` handler decrements the gem's spawned
count and reschedules the next timer. Confirmed to be a general engine bug, not
spawn-specific, via grep showing this was the only dynamic per-instance
`AddTriggerGroup` call anywhere in the codebase - every other `@destroy` handler is
defined at the Def level, unaffected by this ordering. Fixed by swapping the order in
`InternalDeleteNoRFV()`.

## See also

[Pets & NPC AI](pets-and-npc-ai.md) for what happens to an NPC once it's spawned.

A spawn can be guarded by other spawns: see [guard bosses](guard-boss.md) (#1234).
