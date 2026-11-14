# Trigger catalog (core + scripts)

This is a reference for the current trigger system, including core and script-defined triggers. May not be complete as scripts can define their own triggers.
Documents the state as of 2026, before AI changes. 

For theoretic/architectural background look at `docs/triggers/trigger-system-architecture.md`

Table columns:
- **Step**: trigger order within a chain (`-` when not an ordered chain)
- **Trigger key**: script-facing key (`@...` or On_... method)
- **Class**: the object that receives the trigger
- **Category**: notification / deny / implementation / calculation
- **Shape**: regular / cancellable
- **Meaning**: what the trigger represents
- **Example script usage**: what this is / could be used for in scripts

---

## 1) Core chains (ordered)

## 1.1 Chain: Item pickup

This is the full pickup flow: deny gates first, then transfer notifications.

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyPickupItem` | character (picker) | deny | cancellable | Picker decides whether pickup is allowed | Block pickup while stunned or overloaded |
| 2 | `@denyPickup` | item (picked item) | deny | cancellable | Item decides whether it can be picked | Make item immovable unless unlocked |
| 3 | `@denyPickupItemFrom` | source holder (item/char/region hierarchy) | deny | cancellable | Source holder/region may veto removing the item | Protected container/region denies taking items |
| 4* | `@splitFromStack` | item (picked stack part) | implementation/notification | regular | Stack split happened before transfer | Recompute per-stack metadata (e.g., poison doses) |
| 5a | `@leaveItem` | item | notification | regular | Item leaves source container item | Cleanup when leaving container |
| 5b | `@itemLeave` | source container item | notification | regular | Source container sees item leaving | Update UI, counters, bookkeeping |
| 5c | `@leaveChar` or `@unEquip` | item | notification/implementation | regular | Item leaves source character (equipped path uses `@unEquip`) | Remove effects from worn/held item |
| 5d | `@itemLeave` or `@itemUnEquip` | source character | notification/implementation | regular | Source character sees item leave/unequip | Recalculate stats/weapon state |
| 5e | `@leaveRegion` + region `@itemLeave` | item + region hierarchy | notification | regular | Ground/region source sees item removed | Zone bookkeeping on item removal |
| 6 | `@enterChar` | item | notification | regular | Picked item enters picker (dragging layer) | Adjust temporary item state while dragging |
| 7 | `@itemEnter` | picker character | notification | regular | Picker receives dragged item | Refresh character carry state/UI |

\* Step 4 only occurs when picking part of a stack.

## 1.2 Chain: Put item on ground

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyPutItemOnGround` | character (actor) | deny | cancellable | Actor decides whether ground drop is allowed | Prevent dropping in restricted states |
| 2 | `@denyPutOnGround` | item (dragged) | deny | cancellable | Item decides whether it may be dropped on ground | Bound item refuses ground placement |
| 3 | `@denyPutItemOn` | target region hierarchy | deny | cancellable | Region(s) may veto placement at target point | Safe-zone or house floor restrictions |
| 4 | `@enterRegion` | item | notification | regular | Item is placed on ground | Initialize on-ground behavior |
| 5 | `@itemEnter` (child -> parent) | region hierarchy | notification | regular | Region chain observes ground entry | Region effects, zoning counters |

## 1.3 Chain: Put item into container

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyPutItemInItem` | character (actor) | deny | cancellable | Actor decides whether put-in-container is allowed | Disallow while action-locked |
| 2 | `@denyPutInItem` | item (dragged) | deny | cancellable | Dragged item may refuse being put inside | Item-only-in-special-container rules |
| 3 | `@denyPutItemIn` | target container item | deny | cancellable | Target container may deny receiving item | Capacity/type/content restrictions |
| 4 | `@enterItem` | item | notification/implementation | regular | Item enters target container | Normalize tags/position on insert |
| 5 | `@itemEnter` | target container item | notification/implementation | regular | Container sees the arriving item | Auto-sort, bookkeeping, validation |
| 6a* | `@stackOnItem` | dragged item | implementation | cancellable | Dragged item attempts merge with waiting stack | Merge custom per-stack payload |
| 6b* | `@itemStackOn` | waiting stack item | implementation | cancellable | Waiting stack accepts/adjusts merge | Compute combined stack state |

\* Step 6 occurs only when stack merge is attempted.

## 1.4 Chain: Put item on item (use/drop-on behavior)

This is the implementation chain used when dropping one item onto another item.

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@putOnItem` | dragged item | implementation | cancellable | Item handles being put on target item | Consume action for special “use-on” logic |
| 2 | `@putItemOn` | target item | implementation | cancellable | Target item handles receiving another item on it | Crafting/combining/container-specific behavior |

## 1.5 Chain: Put item on character

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@putItemOnChar` | character (actor) | implementation | cancellable | Actor initiates drop-on-character behavior | Guard or redirect allowed target actions |
| 2 | `@putOnChar` | dragged item | implementation | cancellable | Item handles being put on character | Custom item effect on target character |
| 3 | `@putItemOn` | target character | implementation | cancellable | Target character handles incoming item-on-char | Accept/reject custom interaction flow |
| 4 | *equip / container* | — | — | — | If not consumed, flow continues via `put in container` (inventory) or 'equip' chain | Fallback to normal item transfer |

## 1.6 Chain: Equip item on character

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 0* | `@unEquip` / `@itemUnEquip` | currently equipped item + character | implementation | regular | Existing equipped item may be unequipped first | Free slot by removing previous equipment |
| 1 | `@denyEquipOnChar` | character (actor) | deny | cancellable | Actor may veto equipping on target | Disallow equipping while stunned/disarmed |
| 2 | `@denyEquip` | target character | deny | cancellable | Target may deny incoming equipment | Class/race/profession restrictions |
| 3 | `@denyEquip` | item (dragged) | deny | cancellable | Item may deny being equipped | Minimum stats/requirements checks |
| 4 | `@equip` | item | implementation | regular | Item applies equip behavior | Start item-driven modifiers/effects |
| 5 | `@itemEquip` | target character | implementation | regular | Character reacts to equipped item | Recalculate derived combat stats |

\* Step 0 is conditional (only when slot replacement is required).

## 1.7 Chain: Region transition (character)

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@exit` (old branch, child -> parent) | region hierarchy | deny/implementation | cancellable | Old regions are asked to allow leaving | Keep player inside event area until finished |
| 2 | `@enter` (new branch, parent -> child) | region hierarchy | deny/implementation | cancellable | New regions are asked to allow entry | Entry checks by zone hierarchy |
| 3 | `@exit` / `@enter` forced notifications | region hierarchy | notification | regular | Committed transition notifications | Arrival/departure announcements |

## 1.8 Chain: Item stack merge

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@stackOnItem` | manipulated stack | implementation | cancellable | Moving stack attempts to merge | Validate whether stack payloads are compatible |
| 2 | `@itemStackOn` | waiting stack | implementation | cancellable | Waiting stack applies final merge logic | Combine doses/power/metadata |

## 1.9 Chain: Click (single click)

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@itemClick` or `@charClick` | clicking character | implementation | cancellable | Clicker-specific click handling for target kind | Intercept click for custom inspect rules |
| 2 | `@click` | clicked thing | implementation/notification | cancellable | Generic click handling on clicked object | Replace default label behavior |

## 1.10 Chain: DClick (double click / use)

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyItemDClick` (character-side) | dclicking character | deny | cancellable | Character-side gate before target dclick logic | Block use while frozen / restricted |
| 2 | `@denyDClick` | clicked thing | deny | cancellable | Target object gate for dclick | Item/target denies activation |
| 3 | `@itemDClick` or `@charDClick` | dclicking character | implementation | regular | Character-side dclick handling by target kind | Route use action by target type |
| 4 | `@dClick` | clicked thing | implementation | regular | Target object executes/consumes use behavior | Open/equip/activate object behavior |

## 1.11 Chain: Skill flow

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@skillSelect` | skill handlers | calculation/implementation | regular | Skill target/selection phase | Validate selected target |
| 2 | `@skillStart` | skill handlers | implementation/notification | regular | Skill starts | Allocate temporary skill state |
| 3 | `@skillStroke` | skill handlers | notification/implementation | regular | Skill tick/pulse phase | Progress long-running skill |
| 4a | `@skillSuccess` | skill handlers | notification | regular | Successful completion | Apply final rewards/effects |
| 4b | `@skillFail` | skill handlers | notification | regular | Failed completion | Apply failure consequences |
| 4c | `@skillAbort` | skill handlers | notification/deny | regular | Interrupted/cancelled completion | Roll back temporary state |

## 1.12 Chain: Combat resolution

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1a | `@beforeSwing` | attacker | deny/implementation | cancellable | Pre-hit phase on attacker | Alter/deny pending hit |
| 1b | `@beforeGetSwing` | defender | deny/implementation | cancellable | Pre-hit phase on defender | Defensive pre-hit logic |
| 2a | `@causeDamage` | attacker | implementation/calculation | regular | Outgoing damage adjustment | Scale or transform outgoing damage |
| 2b | `@damage` | defender | implementation/calculation | regular | Incoming damage adjustment | Absorb/redirect/modify incoming damage |
| 3a | `@afterSwing` | attacker | notification | regular | Post-hit phase attacker side | Trigger on-hit aftermath |
| 3b | `@afterGetSwing` | defender | notification | regular | Post-hit phase defender side | Trigger post-hit defensive effects |

---

## 2) Core groups (non-chain tables)

## 2.1 Global lifecycle and command group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@startup` | globals | notification | regular | Server startup lifecycle point | Start periodic timers |
| - | `@shutdown` | globals | notification | regular | Server shutdown lifecycle point | Graceful cleanup |
| - | `@beforeSave` | globals | notification | regular | Runs before persistence write | Pause or remove transient timers |
| - | `@openSaveStream` | globals | notification | regular | Custom stream preparation for save | Bind custom save stream |
| - | `@afterSave` | globals | notification | regular | Runs after save finishes | Re-arm periodic save timer |
| - | `@beforeLoad` | globals | notification | regular | Runs before data load | Pre-load setup |
| - | `@openLoadStream` | globals | notification | regular | Custom stream preparation for load | Bind custom load stream |
| - | `@afterLoad` | globals | notification | regular | Runs after load completes | Restore runtime-only systems |
| - | `@command` | globals/command handlers | calculation/implementation | regular | Command routing/authorization/handling | Handle custom admin command |
| - | `@clientAttach` | globals | notification | regular | Client attached | Session init hooks |

## 2.2 Character lifecycle and communication group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@login` | character | notification | regular | Character login event | Initialize per-character systems |
| - | `@logout` | character | notification | regular | Character logout event | Persist temporary state |
| - | `@step` | character | notification | regular | Character stepped | Step-based effects |
| - | `@newPosition` | character | notification | regular | Position changed | Re-evaluate positional mechanics |
| - | `@say` | character | notification | regular | Character says text | Speech command parsing |
| - | `@hear` | character | notification | regular | Character hears text | NPC/reactive conversation logic |
| - | `@aosClick` | thing | implementation/notification | cancellable | AOS tooltip-style click path | Custom tooltip handling |

## 2.3 Thing lifecycle group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@create` | item/thing | notification | regular | Object created | Initialize default tags |
| - | `@destroy` | item/thing | notification | regular | Object deleted | Cleanup cross-links/counters |
| - | `@dupe` | item/thing | notification | regular | Object duplicated | Reinitialize copy-specific metadata |

## 2.4 Item utility group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@itemStep` | item | notification | regular | Item-related step hook | Floor trap/item step effects |
| - | `@containerOpen` | item/container | implementation/notification | regular | Container opening action | Audit/open restrictions UI behavior |
| - | `@buildAosToolTips` | item | calculation/notification | regular | Tooltip content construction | Add custom tooltip lines |

---

## 3) Script-defined groups (C# + LScript)

## 3.1 Skill extension group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@start` | skill handlers | implementation/notification | regular | Legacy/alternate skill-start naming | Start custom skill flow |
| - | `@select` | skill handlers | calculation/implementation | regular | Legacy/alternate skill-select naming | Validate selected target |
| - | `@stroke` | skill handlers | notification/implementation | regular | Legacy/alternate skill-stroke naming | Tick long-running skill |
| - | `@success` / `@fail` / `@abort` | skill handlers | mixed | regular | Legacy/alternate outcome naming | Handle custom outcomes |
| - | `@makeItem` / `@skillMakeItem` | crafting/skill handlers | implementation | regular | Item creation sub-step | Customize crafted output |
| - | `@skillChange` | character/skill handlers | notification | regular | Skill value changed | Recompute dependent systems |

## 3.2 Ability and effect group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@activateAbility` / `@activate` | ability/talent handlers | implementation | regular | Ability activation action | Start ability effect plugin |
| - | `@denyActivateAbility` / `@denyActivate` | ability/talent handlers | deny | cancellable | Ability activation gate | Block on cooldown/resource checks |
| - | `@valueChanged` / `@abilityValueChanged` | ability/talent handlers | notification | regular | Ability-related value changed | Refresh dependent values/UI |
| - | `@causespelleffect` / `@spelleffect` | spell/effect handlers | implementation | regular | Spell/effect application pipeline | Inject effect modifiers |
| - | `@effectchar` / `@effectitem` / `@effectground` | spell/effect handlers | implementation | regular | Effect target specialization | Target-type-specific behavior |
| - | `@dispell` | effect handlers | deny/implementation | regular | Dispel/removal flow | Allow/block dispel, then cleanup |
| - | `@timer` | plugin handlers | implementation | regular | Plugin timer callback | Periodic logic or self-expiration |

## 3.3 Combat extension group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@coupledWithWeapon` | combat/item handlers | notification/calculation | regular | Attack associated with weapon | Attach weapon-driven effects |
| - | `@warModeChange` | character handlers | notification/calculation | regular | War mode toggled | Start/stop combat plugins |
| - | `@visibilityChange` | character handlers | notification | regular | Visibility changed | React to hide/reveal |
| - | `@death` | character handlers | notification | regular | Death event | Cleanup/respawn flow |
| - | `@disruption` | character handlers | notification | regular | Action disrupted | Abort cast/channel/action |
| - | `@hostileAction` | character handlers | notification/calculation | regular | Hostile act happened | PvP flagging/reaction logic |

## 3.4 Social / organization / door group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@MemberAdded` / `@MemberRemoved` | social/guild/role handlers | notification | regular | Membership changed | Grant/revoke role effects |
| - | `@DenyAddMember` / `@DenyRemoveMember` | social/guild/role handlers | deny | cancellable | Membership change gate | Permission checks |
| - | `@open` / `@close` | doors/containers | implementation | regular | Open/close action | Animate and state update |
| - | `@denyOpen` / `@denyOpenDoor` | doors/containers | deny | cancellable | Gate opening action | Lock/key/ownership checks |
| - | `@denyClose` / `@denyCloseDoor` | doors/containers | deny | cancellable | Gate closing action | Prevent close in blocked states |

---

## 4) Quick index by class (core + scripts)

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@startup`, `@shutdown`, `@beforeSave`, `@afterSave`, `@beforeLoad`, `@afterLoad`, `@command`, `@clientAttach` | globals | mixed | mixed | server lifecycle and command handling | startup policies, command routing |
| - | `@login`, `@logout`, `@step`, `@newPosition`, `@say`, `@hear`, `@enter`, `@exit`, `@warModeChange`, `@death` | character | mixed | mixed | character lifecycle, movement, social, combat state | state setup/cleanup, zone behavior |
| - | `@create`, `@destroy`, `@dupe`, `@dClick`, `@denyPickup`, `@denyPutInItem`, `@putOnItem`, `@equip`, `@splitFromStack`, `@timer` | item/plugin | mixed | mixed | item lifecycle and interaction/effect mechanics | item rules, effect ownership |
| - | `@itemEnter`, `@itemLeave`, `@denyPutItemIn`, `@denyPickupItemFrom`, `@containerOpen` | container item | mixed | mixed | receiving/removing items and containment policy | inventory/container rules |
| - | `@enter`, `@exit`, `@itemEnter`, `@itemLeave`, `@denyPutItemOn`, `@denyPickupItemFrom` | region | mixed | mixed | area entry/exit and ground item policy | zone restrictions/effects |

