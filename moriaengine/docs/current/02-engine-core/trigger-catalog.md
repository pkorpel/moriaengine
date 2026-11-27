# Trigger catalog (core + scripts)

This is a reference for the current trigger system, including core and script-defined triggers. May not be complete as scripts can define their own triggers.
It documents the code after the #1558 rework: the chains follow the naming convention in
[Trigger system architecture](trigger-system-architecture.md) section 9, and section 5 below lists the old names and
Sphere's/Erebor's.

For architectural background (who hears a step in what order, deny results, GMs), see
[Trigger system architecture](trigger-system-architecture.md).

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

This is the full pickup flow: deny gates first, then transfer notifications. The engine's default check, `CanReach`,
comes after the deny gates unless one of them cancelled while allowing (architecture section 10).

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyPickupItem` | character (picker) | deny | cancellable | Picker decides whether pickup is allowed | Block pickup while stunned or overloaded |
| 2 | `@denyPickup` | item (picked item) | deny | cancellable | Item decides whether it can be picked | Make item immovable unless unlocked |
| 3 | `@denyPickupItemFrom` | source holder (item/char, or on the ground every region up to the world region, #1579) | deny | cancellable | Source holder/region may veto removing the item | Protected container/region denies taking items |
| 3a | `@pickingUp` | character (picker) | notification | regular | The pickup is certain; the item still lies where it was (#1426, `Trigger_PickingUp`, argv: item, amount) | Weekly quests count a monster's loot |
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
| 3 | `@denyPutItemOn` | target region hierarchy (every region up to the world region, #1579) | deny | cancellable | Region(s) may veto placement at target point; the default check after them is `CanReachCoordinates` | Safe-zone or house floor restrictions |
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

## 1.7 Chain: Step (character), with the region transition

Since #1558 (until then: `CheckCanMove` with `@denyMove`, `CheckCharacterObstacles`, a cancellable `@step` before the
move, cancellable region `@exit`/`@enter`, and `@stepTaken` after the move, not for GMs).

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyStep` (`DenyStepArgs`) | moving character | deny | cancellable | Holds refuse the step before anything is worked out | Paralysis, freeze, a vine |
| 2 | `On_DenyStep` | moving character | deny | (method) | Core: terrain, finds `args.Destination`; scripts: stamina, doorstep, collision | Override for movement rules |
| 3 | `@denyExit` (old branch, child -> parent) | region hierarchy | deny | cancellable | Old regions may refuse leaving (not for GM-plevel players) | Keep player inside event area until finished |
| 4 | `@denyEnter` (new branch, parent -> child) | region hierarchy | deny | cancellable | New regions may refuse entry (not for GM-plevel players) | Entry checks by zone hierarchy |
| 5 | `@exit` / `@enter` (`argv[1]`: 1 teleport, 0 step) | region hierarchy | notification | regular | Committed transition, also for teleports | Arrival/departure announcements |
| 6 | `@newPosition`, then the items' `@itemStep`/`@step` | character, items underfoot | notification | regular | The character is on the new tile | Traps, teleport pads |
| 7 | `@step` (direction, running, point left) | character | notification | regular | A step was taken (not a teleport); also for GMs | Step-based effects, stamina drain |

In steps 1-4 a cancel always refuses the step, with or without a refusing `Result` (unlike the other deny chains, see
the architecture page's section 10): the holds answer with a bare cancel. Step 5 runs just before the character's
position changes, as before #1558; steps 6 and 7 after.

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
| 1 | `@denyItemDClick` / `@denyCharDClick` (character-side, by target kind; #1558 - a character target raised `@denyItemDClick` until then) | dclicking character | deny | cancellable | Character-side gate before target dclick logic; the reach test is `Character.On_DenyItemDClick`, skipped for `DenyClickArgs.SkipReachChecks` (Telekinesis) | Block use while frozen / restricted |
| 2 | `@denyDClick` | clicked thing | deny | cancellable | Target object gate for dclick | Item/target denies activation |
| 3 | `@itemDClick` or `@charDClick` | dclicking character | notification | regular | Character-side dclick handling by target kind (not cancellable, as in 2dbc90b) | Route use action by target type |
| 4 | `@dClick` | clicked thing | implementation | cancellable (#509) | Target object executes/consumes use behavior; a cancel skips `On_DClick` | Open/equip/activate object behavior; a banker's def trigger group replacing `On_DClick` |

A cancel in step 1 that leaves the result allowed skips step 2 too. `SkipReachChecks` is set by Telekinesis, the OpenDoor
macro and the potion macro (`Trigger_DClick(clicker, true)`).

A door is asked the same way (#1558): a locked one wants its key in `Door.On_DenyDClick`, a quest's `@denyDClick` handler on it can
refuse or allow and cancel (no key asked), and it swings in `Door.On_DClick`. The client's OpenDoor macro runs this chain on the
nearest door with only the reach test skipped.

## 1.11 Chain: Skill flow

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@skillSelect` | skill handlers | calculation/implementation | regular | Skill target/selection phase | Validate selected target |
| 2 | `@skillStart` | skill handlers | implementation/notification | cancellable | Skill starts | Allocate temporary skill state |
| 3 | `@skillStroke` | skill handlers | notification/implementation | cancellable | Skill tick/pulse phase | Progress long-running skill |
| 4a | `@skillSuccess` | skill handlers | notification | regular | Successful completion | Apply final rewards/effects |
| 4b | `@skillFail` | skill handlers | notification | regular | Failed completion | Apply failure consequences |
| 4c | `@skillAbort` | skill handlers | notification/deny | regular | Interrupted/cancelled completion | Roll back temporary state |

## 1.12 Chain: Combat resolution

#1558 reworked this chain (`DamageManager`, `WeaponSkillDef`, `Character`). Old names: `@hitTry` became `@denySwing`;
`@beforeSwing`/`@beforeGetSwing` became `@swing`/`@getSwing`; `@swingLanding`/`@getSwingLanding` merged into
`@hit`/`@getHit`; `@afterHit`/`@afterGetHit` merged into `@afterSwing`/`@afterGetSwing`; the hard-coded refusals at the
top of `CauseDamage` became `@denyDamage`. Each step is `@X` on the participant, then its `On_X`.

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 0 | `@denySwing` | attacker | deny | cancellable (`DenySwingArgs`) | A swing being drawn in reach (`WeaponSkillDef`), every attacker; a deny is a retry a second later (Sphere's `@HitTry` RETURN 1) | `Player.On_DenySwing`: too dark to fight (GMs pass) |
| 1a | `@swing` | attacker | calculation | cancellable | After armour, before the monsters' modifiers and the dodge; `DamageAfterAC` mutable; a cancel stops the whole swing | Whirlwind, weapon-skill bonus, `kick.scp` |
| 1b | `@getSwing` | defender | calculation | cancellable | Same stage on the defender | |
| 2a | `@hit` | attacker | calculation | regular | After the dodge; also on a dodged blow (`Dodged`), as Erebor's `@AfterSwing` | Class abilities, slayers, stolen talents |
| 2b | `@getHit` | defender | calculation | regular | After the attacker's `@hit`, a landed blow only (Erebor's `@aftergetswing`) | Stance, shield block, hypnosis breaking |
| 3 | `@denyDamage` | defender | deny | cancellable (`DenyDamageArgs`) | Every kind of damage, before the resists; refused damage does nothing | `Character.On_DenyDamage`: invulnerability, Rinald, guard spawns |
| 4a | `@causeDamage` | attacker | calculation | regular | After resists, energy shield, minimum damage, reactive armour; every kind of damage | Scale or transform outgoing damage |
| 4b | `@damage` | defender | calculation | regular | Same stage on the defender | Mana shield |
| 5a | `@afterSwing` | attacker | notification | regular | The blow is over: after a dodge, or once a landed blow's damage was dealt (`FinalDamage`, 0 when refused or absorbed); not after a cancelled `@swing` | Stun blow, freezing blow, bleeding strike, poisoned weapon |
| 5b | `@afterGetSwing` | defender | notification | regular | Same, on the defender; also when reactive armour killed the attacker | Blood pact, waking the NPC's AI |

Steps 0-2 and 5 are for swings only (a mystic's strike too), not for spells or ability blows such as backstab or split
shot; SRC isn't set. `@denyDamage`, `@causeDamage` and `@damage` come for every kind of damage. Handlers that need a
landed blow check `!Dodged`, not `FinalDamage > 0` (a fully absorbed blow still landed). `MagicWearerPlugin` is on a
character while anything magic is worn and answers `@hit` (slayers), `@afterSwing` (the freezing blow),
`@beforeDoEffect` and `@beforeGetEffect`.

## 1.13 Chain: Spell cast (Magery)

#1558 reworked the cast's start (`MagerySkillDef.On_Start`, `Character`). Old names: `@spellCastTry` is gone (hunger is a call
at the same point, `PlayerHunger.HurtIfStarving`, and a starving cast fails); the casting rules (`SpellCastRules`) became `Character.On_DenySpellCast`,
after the `@denySpellCast` handlers instead of before them. Each step is `@X` on the participant, then its `On_X`. A cast
starts with the skill chain (1.11): `@skillStart` comes first, then Magery's sight, mana and reagents, then hunger.

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denySpellCast` | caster | deny | cancellable (`DenySpellCastArgs`) | Before the cast gets under way; `On_DenySpellCast` is the casting rules; `Fizzle` says whether Magery shows a refusal as a fizzle | Energy shield and voodoo refusing Recall, shock refusing any cast |
| 2 | `@start` | spell def | implementation | cancellable | The spell's own start (summons pay, etc.) | |
| 3 | `@spellCast` | caster | notification | regular | The cast is under way (custom scrolls too; not NPC casts through `NPCAiTimer.TryCastSpell`) | Standcast's freeze and boost |
| 4 | `@monsterSpellPower` | caster | calculation | regular (`MonsterSpellPowerArgs`) | Per monster target: a term of the multiplier (`LevelBonus`), before there is a power | Standcast |
| 5 | `@spellPower` | caster | calculation | regular (`SpellPowerArgs`) | Per target, after the multipliers, charge and Koncentrace, before the target's defense | A mystic's stolen talent |
| 6 | `@beforeDoEffect` | caster | calculation | regular (`SpellPowerArgs`) | Per character target, after the defense: its own stage, so the slayers come before `@causeSpellEffect` whatever the plugins' order | Magic items' slayers |
| 7 | `@beforeGetEffect` | target | implementation | cancellable (`SpellEffectArgs`) | Before anything of the effect runs on that target; a cancel turns it back or away | Magic items' reflex and block |
| 8a | `@causeSpellEffect` | caster | calculation/implementation | cancellable | The effect leaves the caster; power changes and acts | Power talents, silence, ignition |
| 8b | `@spellEffect` | target | implementation | cancellable | The effect reaches the target | Magic reflection, spelldodge |
| 9 | `@spellSuccess` | caster | notification | regular (`SpellSuccessArgs`) | After a successful Magery cast, its effect and the after-cast work | The profession plugin's mana back |

## 1.14 Chain: Death

#1558 gave `@denyDeath` deny args and a defined outcome, and `@kill`/`@killCredit` their `Trigger_X` + `On_X`
(`Character.CauseDeath`).

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| 1 | `@denyDeath` | dying character | deny | cancellable (`DenyDeathArgs`: argv[0] result, argv[1] killer) | Before anything of the death, only for a living character (Sphere's `@Death` RETURN 1). A refusal leaves it alive at 1 hit point, or what a handler set; the blow counts only the hits really taken | A possessing mystic and the alter ego refuse, then come home on their plugin's timer |
| 2 | `@death` | dying character | notification | regular (argv: killer) | Partway through the death, after the murder count, `@hostileAction`, the disruption and the dismount | Cleanup of plugins and pacts |
| 2a | `@killCredit` | each credited fighter | notification | regular (`KillCreditArgs`: victim, share) | The experience share: from a monster's death inside its `NPC.On_Death`, from a player's death inside `Player.On_Death` (`LevelingSystem.ApplyDeathExperienceLoss`), and from a possessing mystic's refused death on `PossessionPlugin`'s timer; a death's comes before `@kill` | Weekly quests count kills |
| 3 | `@kill` | killer | notification | regular (argv: victim) | After `@death`; not when there's no killer, it killed itself, or it was deleted | The ranger's killing spree (Erebor's `@finalBlow`) |

The killer is `Globals.SrcCharacter` for a death through the `Hits` setter (every blow, spell, poison, bleed): on a
timer that is whoever last sent a packet, or null (#1464). Some callers set `Hits = 0` and call `CauseDeath` again with
the right killer, so a `@denyDeath` handler can be asked twice for one death and must answer the same. A handler must
not hurt or kill its own holder.

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
| - | `@step` | character | notification | regular | Character stepped (chain 1.7) | Step-based effects |
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
| - | `@itemStep` | character stepping onto an item | notification | regular | Raised on the character for each item underfoot, before the item's own `@step` (chain 1.7) | Arena, quest pads |
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
| - | `@causeSpellEffect` / `@spellEffect` | caster / target | calculation / implementation | cancellable | Spell/effect application (chain 1.13) | Power talents; magic reflection |
| - | `@effectchar` / `@effectitem` / `@effectground` | spell/effect handlers | implementation | regular | Effect target specialization | Target-type-specific behavior |
| - | `@dispell` | effect handlers | deny/implementation | regular | Dispel/removal flow | Allow/block dispel, then cleanup |
| - | `@timer` | plugin handlers | implementation | regular | Plugin timer callback | Periodic logic or self-expiration |

## 3.3 Combat extension group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@coupledWithWeapon` | combat/item handlers | notification/calculation | regular | Attack associated with weapon | Attach weapon-driven effects |
| - | `@warModeChange` | character handlers | notification/calculation | regular | War mode toggled | Start/stop combat plugins |
| - | `@visibilityChange` | character handlers | notification | regular | Visibility changed | React to hide/reveal |
| - | `@death` | character handlers | notification | regular | Death event (chain 1.14) | Cleanup/respawn flow |
| - | `@disruption` | character | notification | regular (`DisruptionArgs`: argv[0] `InterruptCasting`) | Action disrupted (`Character.Trigger_Disrupt`); a soft disruption breaks paralysis and the like but not casting (#860) | Abort cast/channel/action |
| - | `@hostileAction` | character | notification | regular (argv: enemy) | Hostile act happened; never with a null enemy (#1558) | PvP flagging/reaction logic |
| - | `@personalSpace` | character pushed past | notification | regular (argv: pusher) | Someone shoved past it (#767) | A strangler vine grabs them |
| - | `@contextMenuSelect` | thing | implementation | cancellable (argv: requester, index) | An entry of its context menu was picked; before the entry's action, which a cancel skips (`Thing.Trigger_ContextMenuSelect`) | Replace a menu entry's action |
| - | `@gainExperience` | player | calculation | cancellable (`GainExperienceArgs`: argv[0] base amount, argv[1] amount) | About to gain or lose experience (`Player.Trigger_GainExperience`); a handler may change `Amount` (LScript: `argv[1]`) or cancel | RP-rank and world-event bonuses |
| - | `@npcRefuseItem` | NPC | implementation | cancellable (argv: giver, item) | It turns down a given item; a cancel skips the default "They don't appear to want the item" (`NPC.Trigger_NpcRefuseItem`) | Erebor's guards: "you can't bribe me" |
| - | `@leaveSpawn` | NPC | notification | regular | It stops counting for its spawn (`NPC.Trigger_LeaveSpawn`) | `spawn.scp`'s `e_spawned_char` makes another |

## 3.4 Social / organization group

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@MemberAdded` / `@MemberRemoved` | social/guild/role handlers | notification | regular | Membership changed | Grant/revoke role effects |
| - | `@DenyAddMember` / `@DenyRemoveMember` | social/guild/role handlers | deny | cancellable | Membership change gate | Permission checks |

The original engine's door triggers (`@denyOpenDoor`, `@denyOpen`, `@open` and the close ones, on a `Door` class no def used) went
in #941/PR #942; since #1558 a door is the dclick chain (1.10), with no triggers of its own.

---

## 4) Quick index by class (core + scripts)

| Step | Trigger key | Class | Category | Shape | Meaning | Example script usage |
|---|---|---|---|---|---|---|
| - | `@startup`, `@shutdown`, `@beforeSave`, `@afterSave`, `@beforeLoad`, `@afterLoad`, `@command`, `@clientAttach` | globals | mixed | mixed | server lifecycle and command handling | startup policies, command routing |
| - | `@login`, `@logout`, `@step`, `@newPosition`, `@say`, `@hear`, `@enter`, `@exit`, `@warModeChange`, `@death` | character | mixed | mixed | character lifecycle, movement, social, combat state | state setup/cleanup, zone behavior |
| - | `@create`, `@destroy`, `@dupe`, `@dClick`, `@denyPickup`, `@denyPutInItem`, `@putOnItem`, `@equip`, `@splitFromStack`, `@timer` | item/plugin | mixed | mixed | item lifecycle and interaction/effect mechanics | item rules, effect ownership |
| - | `@itemEnter`, `@itemLeave`, `@denyPutItemIn`, `@denyPickupItemFrom`, `@containerOpen` | container item | mixed | mixed | receiving/removing items and containment policy | inventory/container rules |
| - | `@enter`, `@exit`, `@itemEnter`, `@itemLeave`, `@denyPutItemOn`, `@denyPickupItemFrom` | region | mixed | mixed | area entry/exit and ground item policy | zone restrictions/effects |

---

## 5) Renames and Sphere's names (#1558)

| Before #1558 | Now | Sphere 0.99z8 / Erebor |
|---|---|---|
| `@hitTry` (players only, not GMs) | `@denySwing` (`DenySwingArgs`, every attacker; a deny retries a second later) | Sphere's `@HitTry` (RETURN 1: stay ready, retry) |
| `@beforeSwing` / `@beforeGetSwing` | `@swing` / `@getSwing` | Erebor's custom `@BeforeSwing` / `@BeforeGetSwing` (`combat.scp`, `damage_final`) |
| `@swingLanding` / `@getSwingLanding` | merged into `@hit` / `@getHit` (after the dodge; the attacker's also on a dodged blow) | Erebor's `@AfterSwing` (also on a dodged blow) / `@AfterGetSwing` (landed only); Sphere's `@Hit`/`@GetHit` |
| `@afterHit` / `@afterGetHit` | merged into `@afterSwing` / `@afterGetSwing` (`FinalDamage`, `Dodged`) | none: Erebor's `@AfterSwing` is the earlier stage above |
| hard-coded refusals in `CauseDamage` | `@denyDamage` (`DenyDamageArgs`), defaults in `Character.On_DenyDamage` | Erebor's `@GetHit` RETURN 1 (invulnerable NPCs) |
| `@denyMove`, `CheckCanMove`, `CheckCharacterObstacles` | `@denyStep` (`DenyStepArgs`) and `On_DenyStep` | Sphere checks freeze, stone and immobile before terrain and the push |
| cancellable `@step` before the move, `@stepTaken` after it | `@step` after the move (direction, running, point left) | Sphere's `@Step`, after the move |
| region `@enter` / `@exit`, cancellable | region `@denyEnter` / `@denyExit`, then `@enter` / `@exit` as notifications | Sphere's region `@Enter` / `@Exit` (a GM is never refused) |
| `@spellCastTry` (hunger) | removed: hunger is `PlayerHunger.HurtIfStarving` at Erebor's points (hurts and fails) | Erebor's `e_allplayers` `@spellcast` / `@hittry` calling `hungryhurts` (hurt only) |
| `@denySpellCast` after the hard-coded casting rules | `@denySpellCast` (`DenySpellCastArgs`) before `Character.On_DenySpellCast` (the rules) | Sphere's `@SpellCast` RETURN 1, after its own checks |
| `@denyItemDClick` for a character target | `@denyCharDClick` | |
| `Trigger_DClickFromAfar` (skipped every deny) | `Trigger_DClick(clicker, true)`: `DenyClickArgs.SkipReachChecks` | Sphere's `Use_Obj` without the touch test |
| `@denyDeath` (`ScriptArgs`) | `@denyDeath` (`DenyDeathArgs`), refused = alive at 1 hit point | Sphere's `@Death` RETURN 1 (stays at 0 or below, fires again each tick) |
| `@kill`, `@killCredit` (bare raises) | the same keys, with `Trigger_X` + `On_X` | Erebor's `@finalBlow`; `@playerKill` / `@npckill` (`trigger_kill`) |
| `@disruption` with a private flag | `@disruption` with `DisruptionArgs` | |
| `@denyOpenDoor`, `@denyOpen`, `@open` (2dbc90b, unused) | none: a `Door` is the dclick chain (1.10) | Sphere's `Use_Item` for `t_door*` |

Kept as they were: `@spellPower`, `@monsterSpellPower`, `@beforeDoEffect` (Erebor's custom `@BeforeDoEffect`),
`@beforeGetEffect` (Erebor's custom `@BeforeGetEffect`), `@causeSpellEffect`, `@spellEffect`, `@spellCast`,
`@spellSuccess`, the skill chain.

## 6) How SE's triggers relate to Sphere's

- **0.99z8 has trigger tables:** 56 character, 20 item, 5 region and 7 skill triggers, and no spell triggers. Erebor's
  `@BeforeSwing`, `@AfterSwing`, `@finalBlow`, `@BeforeDoEffect`, `@BeforeGetEffect` and others are *custom* triggers
  that Erebor fires itself with `trigger(@X)`, mostly from `damage_final` in `combat.scp` and from `magie.scp`.
- **Erebor's swing order** (`damage_final`): the defender's `@beforeGetSwing`, the attacker's `@beforeSwing`; resists;
  the dodge roll; the attacker's `@afterSwing`, also on a dodged blow; the defender's `@afterGetSwing`, landed blows
  only; shields, armour, then the damage. SE's `@hit`/`@getHit` are that `@afterSwing`/`@afterGetSwing` stage; SE's
  `@afterSwing`/`@afterGetSwing` come at the very end.
- **Sphere's `@GetHit`** fires for every damage with a source, spells and scripts included. SE's `@getHit` is for swings
  only; the all-damage triggers are `@denyDamage` and `@damage`.
- **Death:** Sphere's `@Death` fires first and RETURN 1 keeps the character alive; that is SE's `@denyDeath`. 0.99z8 has
  no `@Kill`.
- **Casting:** Sphere fires `@SpellCast` after its own checks, and RETURN 1 cancels the cast. Erebor's `e_allplayers`
  charged hunger, then applied its casting rules, inside it (`sphereevents.scp`).
- **The converter maps no trigger names.** It keeps `@Create` (as fields), `@NPCRestock` (as loot), the `[SPEECH]` lines
  and a few hard-coded bodies (`@npcRefuseItem`, one `@dclick`). Every other `ON=` body was dropped and hand-ported to C#.
