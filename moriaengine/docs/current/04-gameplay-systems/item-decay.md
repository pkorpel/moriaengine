# Item decay

Tracking issues: #219 (original build, part of #215), #236 (redesign to per-item
timers). The system described below is the **current** (#236) design - the original
global periodic sweep it replaced is mentioned only for context, it no longer exists in
the codebase.

## Why a per-item timer, not a periodic sweep

The first version of this system (#219) was a single recurring `BoundTimer` that swept
every `Thing` each tick looking for eligible ground items. The user asked for a redesign
away from that, for two explicit reasons: the sweep's CPU cost scaled with total world
item count every tick regardless of how many were actually decaying, and "we are aiming
for full performant game, not just alfa/beta testing." The current design instead gives
each eligible item (or corpse) its own ordinary per-instance `BoundTimer` - exactly like
every other per-instance timer in this engine (`NPCAiTimer`'s `WanderLeash`,
`SpawnTimer`), created the moment the item becomes eligible and cancelled the moment it
stops being eligible. Steady-state cost is zero for everything that isn't currently
decaying.

Persistence comes for free as a consequence: a `BoundTimer` lives in the same
`TagHolder.tags` hashtable as ordinary tags (see
[Tags & timers](../02-engine-core/tags-and-timers.md)), which the generic item-save
pipeline already serializes - no explicit save/load code was needed for this system at
all. Verified directly, not assumed: a real save was triggered during #236 and the
resulting file read back, showing the timer serialize as `%_itemdecay_=(ItemDecayTimer)N`
plus a separate `[ItemDecayTimer N]` block with its own absolute `fireAt=` tick - the
same shape already proven for `NPCAiTimer`/`SpawnTimer`. A permanent test
(`Tests/Protocol/ItemDecayPersistenceTests.cs`) asserts this shape stays true.

## `ItemDecaySystem.ScheduleOrCancel` - the one function everything calls

`scripts/Basic/ItemClasses/ItemDecaySystem.cs:50-58`:

```csharp
public static void ScheduleOrCancel(Item item) {
    item.DeleteTimer(decayTimerTK);
    double? minutes = GetDecayMinutesIfEligible(item);
    if (minutes.HasValue) {
        ItemDecayTimer timer = new ItemDecayTimer();
        item.AddTimer(decayTimerTK, timer);
        timer.DueInSeconds = minutes.Value * 60;
    }
}
```

Always clears any existing timer first and re-derives eligibility from scratch, so
calling it more than once in a row (or before all relevant state is known - see
`Corpse.InitFromChar` below) is always safe and self-correcting, never additive.

`Cancel(Item item)` (`ItemDecaySystem.cs:66-68`) is an unconditional variant used from
`Item.On_LeaveRegion`, which fires exactly when an item stops being on the ground but
**before** its own `Cont` is actually updated (`Trigger_LeaveGround`'s own precondition
is `Cont` still being null) - so `ScheduleOrCancel`'s own "is this on the ground" check
can't be trusted at that exact moment; this skips straight to "cancel it, no matter
what."

## Eligibility

`GetDecayMinutesIfEligible` (`ItemDecaySystem.cs:79-101`):

```csharp
private static double? GetDecayMinutesIfEligible(Item item) {
    if (item.Cont != null) {
        return null; //not on the ground
    }
    if (IsInNoDecayRegion(item)) {
        return null;
    }
    Corpse corpse = item as Corpse;
    if (corpse != null) {
        return (corpse.Owner != null)
            ? DecaySettings.instance.playerCorpseDecayMinutes
            : DecaySettings.instance.npcCorpseDecayMinutes;
    }
    if (!item.HasAttribute(ItemAttributes.attr_decay)) {
        return null;
    }
    if (item.HasAttribute(ItemAttributes.attr_questitem)) {
        return null; //exempt, no clock at all
    }
    return DecaySettings.instance.itemDecayMinutes;
}
```

Corpses decay **unconditionally** once on the ground - no flag gates corpse decay at
all, matching real UO (nothing has to opt a corpse into decaying). Non-corpse items
default to `attr_decay` **off** unless a `.def` line or a GM/script explicitly sets it -
matches this engine's existing "incomplete config defaults permissive" convention.
`attr_questitem` fully exempts a non-corpse item (quest items don't decay).

`IsInNoDecayRegion` (`ItemDecaySystem.cs:103-106`) checks the item's current
`FlaggedRegion.Flag_NoDecay` - see [Regions](../02-engine-core/regions.md). This is
re-checked fresh every time eligibility is (re-)evaluated, so removing an exemption
later starts the clock cleanly from that point - it never "owes" elapsed decay time
retroactively.

## Hooks - where eligibility gets re-evaluated

- **`AbstractItem.On_EnterRegion`/`On_LeaveRegion`** (`Item.cs:283-296`) - the genuine
  "item placed on/removed from the ground" events, confirmed via direct reading of
  `ThingDef.Create`/`AbstractItem.Cont`/`MakeLimbo` to fire for every ground-landing
  path: creation directly onto the ground, drops, pickup, monster-death loot.
- **`SetAttribute`** (`Item.cs:107-109`) - re-evaluates when `attr_decay` or
  `attr_questitem` toggle. Real gap found and fixed during #236: the original code only
  hooked `attr_decay`, so toggling `attr_questitem` alone (after `attr_decay` was
  already set) wouldn't cancel the running timer.
- **`Item.On_Create`** (`Item.cs:263-276`) - needs its own re-evaluate call because
  `On_EnterRegion` fires **before** `On_Create` during `ThingDef.Create`'s own call
  order. The first attempt (from `On_EnterRegion`) always sees unseeded/zero attributes;
  this second call is what actually catches a `.def`-configured `attr_decay` default for
  an item created directly on the ground.
- **`Corpse.InitFromChar`** (`Corpse.cs:74-117`, re-evaluate call at line 116) - same
  ordering issue, specific to corpses: a corpse's own `Create()` call fires
  `On_EnterRegion` before `InitFromChar` ever runs, so the clock always starts once at
  the NPC rate first (since `Owner` isn't set yet), then this call corrects it to the
  player rate if `dieingChar.IsPlayer` turns out to be true. See
  [Corpses & death](corpses-and-death.md) for `InitFromChar` in full.

## `DecaySettings` - three independent rates

`scripts/Basic/ItemClasses/DecaySettings.cs`, a `.settings`-dialog category ("Item
Decay" - see [The `.settings` dialog framework](../03-admin-tooling/settings-dialog.md)):

```csharp
[SaveableData] public double playerCorpseDecayMinutes = 10;
[SaveableData] public double npcCorpseDecayMinutes = 5;
[SaveableData] public double itemDecayMinutes = 5;
```

Three rates, not one generic interval, per explicit user request - a player's corpse, an
NPC's corpse, and a loose `attr_decay` item are three different real-world defaults.
`Corpse.Owner != null` (set only for player deaths, see
[Corpses & death](corpses-and-death.md)) is the exact discriminator between the first two
rates.

## House exemption

All 256 real player-house regions on this map (`a_dum_1`..`a_dum_258`, the "dum N"
regions from the original Sphere conversion) already had `Flag_NoDecay` set, confirmed
via a direct bitwise check across every one of them during #236 - nothing needed
changing there; the data was correct from the original conversion, it just wasn't
readable as a property until #219 added `FlaggedRegion.Flag_NoDecay`.

## Test coverage

`PlayerAndNpcCorpsesDecayAtDifferentRates` proves the right rate gets picked per corpse
type (deliberately different tiny rates, checked at an intermediate checkpoint between
the two), not just that corpses decay at all. `ItemDecayPersistenceTests.cs` proves the
timer survives a real save/reload.
