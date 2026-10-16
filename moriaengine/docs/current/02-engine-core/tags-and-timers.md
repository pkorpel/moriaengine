# Tags & timers

`TagHolder` (`GameCore/TagHolder.cs`) is the base class that gives every `Thing` (items,
characters), `GameAccount`, and `Globals.Instance` a generic, indexed-by-name value bag
without needing a dedicated field for every piece of per-instance data a script might ever
want to attach. Its own doc comment calls this "lightweight polymorphism" - any piece of
code can stash arbitrary data on any `TagHolder` without the class itself needing to know
about it ahead of time.

## TagKey and the interning table

A tag is addressed by a `TagKey`, not a raw string. `TagKey.Acquire(name)`
(`GameCore/TagKey.cs`, built on `AbstractKey<T>` in the same folder) is a case-insensitive,
thread-safe `ConcurrentDictionary<string, T>.GetOrAdd` - the first call for a given name
creates the key and assigns it a sequential `uid`; every later call for the same name
returns the *same* key instance. `Equals`/`GetHashCode` are sealed to reference equality on
that `uid`, which is why tag lookups are a plain hashtable operation rather than a string
comparison.

This interning table only ever grows (nothing ever removes an entry from `AbstractKey`'s own
`byName` dictionary) - fine for the normal case of a bounded number of distinct tag *names*
used throughout the codebase, but worth knowing if something were to generate a very large
number of genuinely unique key strings over a server's lifetime.

**Dynamically-built key names work fine and need nothing special.** The item ATTR flag
system's `attr_invis` per-viewer visibility exception list (`Item.IsSeenBy`/
`GrantVisibilityTo`/`RevokeVisibilityFrom`, `scripts/Basic/ItemClasses/Item.cs`) keys each
grant as `TagKey.Acquire("_seenBy_" + viewerUid)` - one distinct tag per (item, viewer) pair,
computed at the point of use rather than declared as a static field. Since `Acquire` is just
a lookup/insert, this is exactly as cheap as any other tag access. This is the pattern to
reach for any time a flag needs to be "true for this object, with respect to that other
specific object" - a per-viewer, per-owner, or per-grantee exception, rather than inventing
a new storage mechanism.

**The more common pattern** is a single `private static readonly TagKey` field backing one
semantically-named C# property, e.g. `House.Owner` (`scripts/Basic/ItemClasses/Housing/
House.cs`) or `Item.OwnedBy` (`scripts/Basic/ItemClasses/Item.cs`) - a plain get/set wrapper
around `GetTag`/`SetTag` on one fixed key. This is preferred over a real instance field
whenever the value is occasional (most instances never set it) or the class's own `.ct`
template doesn't already declare a field for it - adding a tag costs nothing for instances
that never use it, where a declared `Vars:` field would need regenerating the class and (for
a value type) always occupies space.

One real naming trap, found while adding `Item.OwnedBy`: `House` and `Corpse` (both `Item`
subclasses) already each had their own `Owner` property with an unrelated meaning (who owns
the house / whose body this is). A naively-named `Item.Owner` would have silently *shadowed*
both (caught only via a `CS0108` compiler warning on a first attempt) rather than conflicted
outright - C# property hiding doesn't error, it just quietly resolves differently depending
on the static type you're holding. When adding a same-shaped property to a widely-subclassed
base class, check for an existing same-named property on any subclass first.

## Timer and BoundTimer

`Timer` (`GameCore/Timers/Timer.cs`) is the engine's scheduling primitive: a background
thread (`TimerThread`) drains a priority queue ordered by absolute fire time (`fireAt`, a
`TimeSpan` since server start) and calls `OnTimeout()` under `MainClass.globalLock` when due.
Two knobs control it:

- `DueInSeconds` - delay before the *next* firing. Negative pauses the timer (removes it from
  the active queue without deleting it); this is why `ScheduleOrCancel`-style helpers often
  re-arm a timer by setting `DueInSeconds` directly rather than recreating it.
- `PeriodInSeconds` - if `>= 0`, the timer re-arms itself to `fireAt + period` *before*
  `OnTimeout` runs, making it recurring. Negative (the default) means one-shot.

`BoundTimer` (`GameCore/Timers/BoundTimer.cs`) is the subclass everything in this codebase
actually uses - it attaches to a `TagHolder` via a weak reference (`contRef`) and adds the
one-shot auto-cleanup behavior: in its own `OnTimeout()` override, if the timer is *not*
periodic and has no future `DueInSeconds` pending (i.e. it just fired once and isn't set to
repeat), it removes itself from the owning `TagHolder` before calling the subclass's
`protected abstract void OnTimeout(TagHolder cont)`. A periodic timer skips this because its
`fireAt` was already bumped into the future by the time that check runs.

### Timers live in the same table as tags - which means they're saved for free

`TagHolder.AddTimer`/`RemoveTimer`/`HasTimer`/`DeleteTimer`/`GetTimer` all operate on the
*exact same* `Hashtable` (`TagHolder.tags`) that `SetTag`/`GetTag` use - a `BoundTimer` is
stored under its `TimerKey` exactly like a tag is stored under a `TagKey`, just distinguished
by key type when the table is iterated. `TagHolder.Save` (same file) walks that table once and
writes `"tag." + key` for `TagKey` entries and `"%" + key` for `TimerKey` entries - meaning a
`BoundTimer` attached to any `Item`/`Character`/etc. is serialized automatically, with no
special-cased save/load code anywhere, by virtue of being in that one table.

This was verified directly (not assumed) during the #219/#236 item-decay redesign: triggering
a real `Globals.Save()` on an item with a pending `ItemDecayTimer` produced

```
[Item i_rock_plain]
attributes=2
uid=8
%_itemdecay_=(ItemDecayTimer)29

[ItemDecayTimer 29]
fireAt=1214134197
```

- a reference line inside the item's own block, pointing at a separate numbered block holding
the timer's own saved state (`Timer.Save`, same file, writes `fireAt`/`period` as raw
`TimeSpan.Ticks`). On load, `TagHolder.LoadLine` recognizes the `%`-prefixed key and defers
resolving the referenced `ItemDecayTimer` object before calling `AddTimer` again - the timer
reconnects to its owner and its absolute fire time is restored exactly, with the countdown
continuing correctly across a server restart. A `BoundTimer` subclass only needs
`[SaveableClass]` + a `[LoadingInitializer]` no-arg constructor (see `ItemDecayTimer`,
`scripts/Basic/ItemClasses/ItemDecaySystem.cs`) to participate in this - nothing about
persistence is decay-specific.

## Two idioms: one global timer vs one-per-instance

**A single recurring timer on `Globals.Instance`** is the right shape for anything that needs
exactly one world-wide periodic tick, independent of any specific object - `LightAndWeather.
WorldClockTimer` (`scripts/Basic/LightAndWeather.cs`) drives the day/night cycle and season
rolls this way: a lazily-self-installed (`EnsureClockRunning`), `PeriodInSeconds`-recurring
timer attached to the one `Globals.Instance` `TagHolder`. There's no natural "owner" for "is
it night" - it's a property of the whole world, so one shared timer is correct.

**One timer per eligible instance** is the right shape for anything that's really "this
specific object's own countdown" - `NPCAiTimer` (`scripts/Basic/AI/NPCAiTimer.cs`, attached
per-NPC) and the current `ItemDecaySystem.ItemDecayTimer` design (`scripts/Basic/ItemClasses/
ItemDecaySystem.cs`, attached per-eligible-item, created/cancelled the instant an item
lands on/leaves the ground) both use this. The item-decay system's own history is the clean
illustration of *why* to prefer this over a periodic global sweep when the problem is "some
subset of objects individually expire": the original #219 design was one global `BoundTimer`
on `Globals.Instance` scanning every `Item` in `Thing.AllThings` every 60 seconds to find
which ones were due - correct, but with a steady per-tick cost proportional to total world
item count regardless of how many items were actually decaying. The #236 redesign replaced
it with exactly this "one timer per eligible instance" idiom: zero steady-state cost when
nothing is decaying, and persistence is automatic rather than needing its own save-file scan
logic (which the sweep design also didn't need, since `Globals.Instance` is itself a
`TagHolder` either way - the real win is the removed per-tick scan, not persistence, which
both designs got for free).

**A third, script-facing idiom** exists for raw LScript content: the `AddTimer(%Key,
delaySeconds, @triggerName)` builtin (seen throughout `.scp` files, e.g. `spawn.scp`'s own
`AddTimer(%SpawnTimer, tag.spawnInfo.RollRespawnDelaySeconds(), @spawnNow)`) is sugar over the
exact same `TagHolder`/`BoundTimer` mechanism, backed by `TriggerTimer`
(`GameCore/Timers/TriggerTimer.cs`) - a generic `BoundTimer` subclass whose `OnTimeout`
doesn't call a dedicated C# method at all, just fires a named script trigger
(`PluginHolder.TryTrigger`) on the `TagHolder` it's attached to. Reach for a real `BoundTimer`
subclass (like the two above) when the timeout logic is non-trivial C# behavior; reach for
`AddTimer(%Key, seconds, @trigger)` when the response is better expressed as a script trigger
a `.scp`/`.def` file can hook.

## Related
- [Persistence & saving](persistence-and-saving.md) - the broader save/load pipeline this
  sits inside.
- [Item decay](../04-gameplay-systems/item-decay.md) - the concrete worked example used
  throughout this page.
