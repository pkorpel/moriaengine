# Tags and timers

`TagHolder` provides optional keyed data and bound timers for Things, accounts, globals and
other engine objects. Use it for sparse extensions; use a dedicated field for stable,
class-owned state when that gives a clearer model.

## TagKey and the interning table

`TagKey.Acquire(name)` returns a case-insensitively interned key. Repeated acquisitions return
the same key; holder storage uses keys rather than repeatedly comparing strings. Cache fixed
keys in `static readonly` fields.

The process-wide interning dictionary never removes names. Dynamic names work, but building
a string and looking it up costs more than using a cached key, and unbounded names retain
memory. `_seenBy_` plus a viewer UID creates a distinct key per viewer name, shared across
holders; each item's value is stored separately. Removing a tag does not remove its key.

Prefer existing semantic names and check subclasses before adding base properties: `Owner`
already means different things for houses and corpses; item ownership uses `OwnedBy`.

## Timer and BoundTimer

`TimerThread` dispatches due work under `MainClass.globalLock`, acquiring it before a timer's
own lock. Preserve that order when scheduling/deleting timers from packet work.

| Property | Meaning |
|---|---|
| `DueInSeconds` | Delay to next firing; negative pauses without deleting. |
| `PeriodInSeconds` | Nonnegative repeats; negative is one-shot. |

Queue ordering uses the enqueue-time `queuedAt` value, separate from the mutable requested
`fireAt`. Dispatch rechecks timer state/generation so old-world work does not run after a
clear/reload. The round budget is checked between callbacks; it cannot preempt a slow callback.

Callbacks clear `Globals.Src` temporarily. Supply actors explicitly for damage credit,
permissions and messaging rather than relying on the most recent network sender.

`BoundTimer` holds a weak reference to its owner; the holder stores it strongly. A completed
one-shot with no next firing pending removes itself and calls `Delete` before invoking
`OnTimeout(TagHolder)`. Periodic timers stay attached. Use a new timer for another one-shot
or a periodic timer for repeated work; do not assume the expired timer remains usable.

<a id="timers-live-in-the-same-table-as-tags---which-means-theyre-saved-for-free"></a>

## Persistence

The holder table contains tag and timer entries distinguished by key type. Timer APIs maintain
both directions of the association; use `AddTimer`, `GetTimer`, `RemoveTimer` and `DeleteTimer`
rather than manipulating that table. Removal and deletion are different operations.

Holder saving writes `tag.` entries and `%` timer references. Referenced timers still need
a supported save/load implementation. A typical custom timer uses `[SaveableClass]` and a
parameterless `[LoadingInitializer]`, with its state covered by saving support.

On load, references are resolved and the timer is attached to its holder. Fire time and period
are stored in ticks on the engine's saved game-time basis, not as wall-clock deadlines.
Offline downtime is not automatically elapsed real-world time. See [persistence](persistence-and-saving.md).

## Scheduling patterns

| Need | Pattern |
|---|---|
| One world-wide periodic action | Recurring timer on `Globals.Instance`, such as the world clock. |
| One object's countdown | Bound timer on that object, created/cancelled as eligibility changes. |
| Script-trigger timeout | `AddTimer(%Key, delaySeconds, @triggerName)`, backed by `TriggerTimer`. |

Per-object timers avoid recurring full-world scans for sparse expirations, but still have
queue/storage costs. Choose by workload; persistence alone does not favour a global sweep
or per-object timers, since either can be attached to a saved holder.

## Sources

[TagHolder](../../../GameCore/TagHolder.cs), [AbstractKey](../../../GameCore/AbstractKey.cs),
[Timer](../../../GameCore/Timers/Timer.cs), [BoundTimer](../../../GameCore/Timers/BoundTimer.cs),
[TriggerTimer](../../../GameCore/Timers/TriggerTimer.cs),
[item decay](../04-gameplay-systems/item-decay.md).
