# Legacy SteamEngine trigger catalog

This page groups inherited trigger families by operation. It describes their current use in
moriaengine, not a frozen upstream snapshot. The [new catalog](new-trigger-catalog.md) covers
extension events introduced in the fork, even when they reuse Sphere names.

Read [trigger architecture](trigger-system-architecture.md) for dispatch and argument mapping,
and [deny chains](triggers-and-deny-chains.md) for the distinction between cancel and deny.
The tables identify extension points; they are not complete operation traces. New events,
virtual hooks, built-in checks and validity checks can intervene.

## Item manipulation

| Operation | Legacy events and receivers |
|---|---|
| Pickup permission | Picker `@denyPickupItem`, item `@denyPickup`, source holder/region `@denyPickupItemFrom`. |
| Ground-drop permission | Actor `@denyPutItemOnGround`, item `@denyPutOnGround`, destination region `@denyPutItemOn`. |
| Container permission | Actor `@denyPutItemInItem`, incoming item `@denyPutInItem`, destination container `@denyPutItemIn`. |
| Equip permission | Actor `@denyEquipOnChar`, target character `@denyEquip`, item `@denyEquip`. |
| Use/drop item on item | Incoming item `@putOnItem`, target `@putItemOn`; cancellable implementation. |
| Use/drop item on character | Actor `@putItemOnChar`, incoming item `@putOnChar`, target `@putItemOn`; cancellable implementation before fallback. |
| Stack merge | Moving stack `@stackOnItem`, waiting stack `@itemStackOn`; cancellable implementation. |
| Stack split | Split item's `@splitFromStack`; regular. |

Permission rows use an operation-specific result payload. Source hooks do not all propagate
cancel in the same way. Inspect the raising method before adding a gate.

Placement notifications are regular, separate from permission checks:

| Transition | Item event | Holder/region event |
|---|---|---|
| Leave container | `@leaveItem` | `@itemLeave` |
| Enter container | `@enterItem` | `@itemEnter` |
| Leave character | `@leaveChar` / `@unEquip` | `@itemLeave` / `@itemUnEquip` |
| Enter character | `@enterChar` / `@equip` | `@itemEnter` / `@itemEquip` |
| Leave ground | `@leaveRegion` | `@itemLeave` |
| Enter ground | `@enterRegion` | `@itemEnter` |

The dragging layer is also character placement. Direct placement and secure-trade completion
need not replay interactive permission checks. See [Thing placement](thing-model.md).

Source: [ThingManipulationTriggers](../../../GameCore/ThingManipulationTriggers.cs).

## Clicks and use

| Events | Contract |
|---|---|
| `@itemClick` / `@charClick`, then `@click` | Clicker-side target-kind handling, then target handling; cancellable replacement of defaults. |
| `@denyItemDClick`, `@denyDClick` | Actor-side item-use gate where applicable, then target gate. |
| `@itemDClick` / `@charDClick` | Regular clicker-side dispatch. |
| `@dClick` | Cancellable target-side implementation. |
| `@aosClick`, `@buildAosToolTips` | Tooltip click and tooltip construction respectively. |
| `@containerOpen` | Container opening behaviour/notification. |

See [item use](../04-gameplay-systems/item-use-and-dclick.md) for the complete current action
framework. Passing a gate does not prove the operation completed.

## Characters, regions and objects

| Event | Receiver and contract |
|---|---|
| `@login` | Character; cancellable login processing. |
| `@logout` | Character; regular logout notification. |
| `@step` | Character; cancellable pre-step processing with direction/running arguments. |
| `@newPosition` | Character; position-change notification, including paths beyond walking. |
| `@say` / `@hear` | Speaker/listener; cancellable speech processing. The hear path interprets a speech result. |
| `@enter` / `@exit` | Region; checked transitions can be cancelled; forced transitions use notification paths. |
| `@create`, `@destroy`, `@dupe` | Object lifecycle; check creation/deletion ordering before using other objects. |
| `@itemStep` | Item-side step interaction. |

Region transitions traverse the old branch toward ancestors and the new branch toward the
child. Forced movement is not a second identical permission round. See [regions](regions.md).
For an early movement veto or completed step, see new `@denyMove` / `@stepTaken`.

Sources: [AbstractCharacter](../../../GameCore/AbstractCharacter.cs),
[Thing](../../../GameCore/Thing.cs), [Region](../../../GameCore/Regions/Region.cs).

## Skills

These names identify different receivers, not merely old/new aliases:

| Phase | Character | SkillDef |
|---|---|---|
| Selection | `@skillSelect` | `@select` |
| Start | `@skillStart` | `@start` |
| Stroke | `@skillStroke` | `@stroke` |
| Success | `@skillSuccess` | `@success` |
| Failure | `@skillFail` | `@fail` |
| Abort | `@skillAbort` | `@abort` |

Select/start/stroke/success/fail call character cancellable handlers, a virtual hook and
SkillDef handlers while preceding stages continue. Abort uses cancellable dispatch within
each receiver but ignores those returns when moving to subsequent abort stages. Events use
the skill sequence's `scriptArgs`.

Crafting adds `@makeItem` / `@skillMakeItem`; skill-value changes use `@skillChange`.
See [skills](../04-gameplay-systems/skills-overview.md) and
[SkillDef](../../../scripts/Basic/skills/SkillDef.cs).

## Combat, abilities and effects

| Family | Legacy events | Purpose |
|---|---|---|
| Swing gates | `@beforeSwing`, `@beforeGetSwing` | Cancellable attacker/defender pre-swing work. |
| Damage | `@causeDamage`, `@damage` | Regular calculation with mutable `DamageArgs`. |
| Swing aftermath | `@afterSwing`, `@afterGetSwing` | Regular completion, not proof damage landed. |
| Combat state | `@coupledWithWeapon`, `@warModeChange`, `@visibilityChange`, `@disruption`, `@hostileAction` | Weapon/state/action reactions. |
| Death | `@death` | Victim death; distinct from new death gates and kill/credit events. |
| Ability activation | `@denyActivateAbility` / `@denyActivate`, `@activateAbility` / `@activate` | Holder/ability gates and activation. |
| Ability values | `@valueChanged`, `@abilityValueChanged` | State changes. |
| Spell effects | `@causespelleffect`, `@spelleffect`, `@effectchar`, `@effectitem`, `@effectground` | Effect pipeline and target specialisation. |
| Removal | `@dispell` | Legacy spelling for effect removal. |
| Plugin timeout | `@timer` | Plugin-owned scheduled work. |

See [combat](../04-gameplay-systems/combat.md), [magery](../04-gameplay-systems/magery-spells.md)
and the [new catalog](new-trigger-catalog.md) for additional phases. Swing, hit and generic
damage are different contracts; infer neither argument shape nor cancellability from a name.

## Globals and other script families

| Family | Events | Reference / caveat |
|---|---|---|
| Lifecycle | `@startup`, `@shutdown` | Includes recompilation; see [runtime lifecycle](runtime-lifecycle.md). |
| Saving | `@beforeSave`, `@openSaveStream`, `@afterSave` | [Persistence](persistence-and-saving.md) defines stream ownership and completion. |
| Loading | `@beforeLoad`, `@openLoadStream`, `@afterLoad` | After-load precedes deferred object/timer completion. |
| Commands/connections | `@command`, `@clientAttach` | [GM commands](../03-admin-tooling/gm-commands.md), [networking](networking-protocol.md). |
| Membership | `@MemberAdded`, `@MemberRemoved`, `@DenyAddMember`, `@DenyRemoveMember` | Organization/role notifications and gates. |
| Doors | `@open`, `@close`, `@denyOpen`, `@denyOpenDoor`, `@denyClose`, `@denyCloseDoor` | [Door guide](../04-gameplay-systems/doors.md); inspect the raising method for the relevant receiver. |

Scripts may define further events. Add new public extension points to the new catalog and
keep this page focused on inherited families.
