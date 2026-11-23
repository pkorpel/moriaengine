# Triggers and deny chains

An action gate has two independent outputs: `TriggerResult` controls dispatch, while
`DenyResult` states whether the action is allowed and can carry a message. They are not
interchangeable. Read [trigger architecture](trigger-system-architecture.md) for handler order.

## Cancellation versus permission

Pickup is a useful concrete example. `CanPickup` runs the deny chain, then calls `CanReach`
only when dispatch did not cancel and `args.Result` allows the action.

| Trigger result | Permission result | Pickup behaviour |
|---|---|---|
| Continue | Allow | Run the default reach check. |
| Continue | Deny | Return the denial. |
| Cancel | Allow | Return allow without the default reach check. |
| Cancel | Deny | Return the denial without the default reach check. |

Thus returning cancel alone can bypass a default check without denying pickup.
`DenyResult.Allow` is read-only. To deny, assign a denial object to the payload's `Result`,
for example `args.Result = DenyResultMessages.Deny_YouCannotPickThatUp`, and choose the
trigger return according to the enclosing gate's contract.

Do not generalise this table to every `deny*` event. New `@denyMove` has no `DenyResult`:
cancel directly prevents movement. [The new catalog](new-trigger-catalog.md) specifies such cases.

## Participants and order

Interactive item operations commonly consult the actor, manipulated item and source or
destination. Each participant may have plugins, scripted handlers and virtual hooks.

| Operation | Main permission participants |
|---|---|
| Pickup | Picker, item, source holder/region. |
| Drop on ground | Actor, item, destination region. |
| Put in container | Actor, incoming item, destination container. |
| Equip | Actor, target character, equipment item. |
| Double click | Actor-side gate where applicable, target-side gate. |

The source holder/region stages of pickup do not propagate cancellation in exactly the same
way as the picker and item stages; some hooks only mutate the permission result. Follow
`Trigger_DenyPickupItem` and the source-specific method rather than assuming a uniform loop.

See [legacy trigger names](trigger-catalog.md) for each family. Placement notifications such
as `@itemEnter` describe transitions; they are not substitutes for permission checks.

## Interactive versus direct placement

An interactive request performs checks before transfer. Low-level placement methods also
serve loading and trusted server operations and do not necessarily invoke those checks.
Secure-trade completion directly inserts accepted items rather than replaying the ordinary
interactive put-in-container gate. Put essential invariants at the appropriate operation
boundary, not only in a UI-facing deny handler.

When a hook returns, participants may have been deleted or moved. Preserve existing
post-hook validity checks. A successful permission result does not prove transfer happened.

## Writing a gate handler

1. Identify the exact operation and receiver.
2. Check the raising method's payload and interpretation of cancel.
3. Set a suitable denial result when the API uses one; return the intended dispatch result.
4. Avoid relying on ambient sender state when the caller supplies an actor explicitly.
5. Verify allowed, denied and handled paths, including reach checks and participant deletion.

For item-use replacement, cancel often means “handled”, not “forbidden”. Avoid copying a
handler from a differently shaped event solely because its name also starts with `deny`.

## Sources

[ThingManipulationTriggers](../../../GameCore/ThingManipulationTriggers.cs),
[DenyResult](../../../GameCore/DenyResult.cs),
[secure trade guide](../04-gameplay-systems/trading-secure-trade.md).
