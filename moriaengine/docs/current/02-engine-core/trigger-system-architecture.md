# SteamEngine trigger system architecture

This guide explains the extended polymorphism (tags+triggers) model: concepts, semantics, and usage patterns.

## 1) Core pattern: `TagHolder` -> `PluginHolder`

`TagHolder` is the base extension object:
- keyed tags (`TagKey -> value`)
- keyed timers (`TimerKey -> BoundTimer`)

`PluginHolder` extends `TagHolder` and adds:
- trigger groups
- plugins
- trigger dispatch (`Trigger`, `TryTrigger`, `CancellableTrigger`, `TryCancellableTrigger`)

In practice:
- **TagHolder** = extension storage
- **PluginHolder** = extension storage + behavior dispatch

---

## 2) What a trigger is

A trigger is **not one method**. A trigger is the whole event pipeline:

- **Trigger key**: logical event identity (@ prefix by convention: `@startup`, `@dclick`, `@denyPutInItem`)
- **Trigger point**: place where the engine raises the event
- **Trigger family**: all handlers attached for that key on relevant participant(s)
- **Handler implementations**: `On_*` methods and/or dynamic handlers

One trigger key can have many implementations.

---

## 3) Trigger chains

Many operations are multi-phase and/or multi-participant ("trigger chains").

Example: put item into container:

1. **Deny phase**
   - on source char: `@denyPutItemInItem` / `On_DenyPutItemInItem`
   - on dragged item: `@denyPutInItem` / `On_DenyPutInItem`
   - on target container: `@denyPutItemIn` / `On_DenyPutItemIn`
2. **Commit phase** (if allowed)
   - on moved item: `@enterItem` / `On_EnterItem`
   - on target container: `@itemEnter` / `On_ItemEnter`

The order is not random, and is part of the API contract for scripters.

### Who hears one step, in what order

One step of a chain is the participant's dynamic handlers, then its compiled `On_*` method. For a thing,
`TryCancellableTrigger` (`Thing.cs`, `AbstractItem.cs`) runs, in this order:

1. the server-wide registered trigger groups (items', then all things');
2. the thing's own trigger groups (`EVENTS`);
3. its plugins, **newest first** (`AddPlugin` puts a plugin at the front; save/load and dupe keep the order);
4. its def's trigger groups: the def section's own `on=@x` lines and its `TEVENTS`;
5. for an item, its `TYPE`.

Then the core calls the virtual `On_*` method. So a script handler always comes before the class's own default, and
in a deny chain a handler can only be skipped by an earlier cancel, never by the `On_*` method. Plugin order is when
each plugin was added, not a contract: where the order of two changes matters (a multiplier before an addition), give
them separate steps or call one at a fixed point (the spell slayers have their own step, `@beforeDoEffect`, for that).
Plugin details: [plugins.md](plugins.md).

---

## 4) Trigger semantic taxonomy

The vocabulary used in this guide is:

1. **Notification**
   - "Something happened" signal, to which trigger handler can react, but doesn't really implement the base behavior nor can it stop it.
   - Usually regular (non-cancellable).
   - Example: `@splitFromStack`, '@startup'

2. **Deny**
   - Permission gate.
   - Default action exists and proceeds unless denied (by setting `DenyTriggerArgs.Result` / `argv[0]` in LScript to a refusing `DenyResult`, see section 10).
   - Example: `@denyPickup*`, `@denyPut*`, `@denyEquip*`.

3. **Implementation**
   - Behavior ownership/consumption point.
   - Not mainly permission; handler typically performs behavior.
   - Typically should be cancellable so handler can consume flow (similar to override not calling base).
   - Example: `@dclick`, `@putOnItem`, `@putItemOn`.

4. **Calculation**
   - Return value is meaningful payload/decision, not only side effects.
   - Example: `@command` authorization-style handling; speech result flows.

These are conceptual labels for documentation and for script authors. They are not a formal engine-enforced naming scheme for every generated method or key.

---

## 5) Trigger shape taxonomy (execution shape)

Shape is separate from semantics:

- **Regular**: `Trigger` / `TryTrigger` - doesn't short-circuit chain on return value
- **Cancellable**: `CancellableTrigger` / `TryCancellableTrigger` - short-circuits chain on return value 1 / cancel
- **Deny**: Cancellable trigger with `DenyTriggerArgs` object (`Result` = `argv[0]` in LScript), set to a refusing `DenyResult` to deny 
  (all deny triggers are cancellable; not all cancellable triggers are deny)

---

## 6) TriggerGroups vs Plugins

Both TG and Plugins contain 1 to n `On_*` handlers.

### TriggerGroup
- shared reusable behavior bundle attached by reference
- good for broad policy/cross-cutting event logic
- supports auto-scope conventions:
  - `*_all<types>` auto-register by class
  - `*_global` auto-register to `Globals`

**Examples from this repo**

- **Global policy behavior via naming convention**: `scripts/Basic/PeriodicSave.scp` defines
  `[triggergroup e_periodicsave_global]` with `on=@startup`, `on=@beforeSave`, `on=@afterSave`.
  This is exactly the kind of server-wide cross-cutting logic TGs are good at.
- **Reusable script behavior attached to holders**: `scripts/Basic/Spawning/spawn.scp` uses
  `char.AddTriggerGroup(e_spawned_char)` to attach shared behavior (`on=@destroy`) to spawned NPCs.
- **Reusable combat policy attached on demand**: `scripts/Basic/skills/Poisoning/PoisonedItemPlugin.cs`
  installs triggergroup `E_Poisoned_Weapon_User` on the wielder so shared combat callbacks can run
  while poisoned weapons/projectiles are in play.

### Plugin
- per-holder behavior object instance
- own state/lifecycle (`Plugin : TagHolder`)
- good for buffs/debuffs/effects/cooldowns/temporary mechanics
- attach/remove by `PluginKey` (convention: `@@key`)
- "simple plugin" variant exists for keyed behavior/state without full iteration role

**Examples from this repo**

- **Ability effect with holder-bound lifecycle**: `scripts/Basic/AbilitiesAndTalents/Talents/ClearCast/ClearCastPlugin.cs`
  sets duration in `On_Assign()` and reacts in `On_SkillStart(...)` through `this.Cont`.
- **Script-side add/get/update workflow**: `scripts/Basic/AbilitiesAndTalents/Talents/silence.scp`
  reads existing plugin by key (`GetPlugin(@@silence_effect)`), prolongs timer if present, otherwise creates and adds new plugin.
- **Ability activation creates/removes plugin state**: `scripts/Basic/AbilitiesAndTalents/ActivableAbilityDef.cs`
  adds plugin instances on activation and deletes them on deactivation/unassign.
- **Plugin state follows item stack/equip mechanics**: `scripts/Basic/skills/Poisoning/PoisonedItemPlugin.cs`
  handles `On_StackOnItem`, `On_ItemStackOn`, `On_Equip`, and `On_Unassign` in one holder-bound object.

---

## 7) Practical guidance

- Use **TriggerGroup** when one behavior set should be reused broadly.
- Use **Plugin** when behavior needs per-instance state/lifecycle.
- Use **Deny** triggers for policy/permission gates.
- Use **Implementation** triggers for consumed/owned behavior.
- Keep trigger-chain participant order stable; order is contract-level behavior.

### Concrete choices (good patterns)

- **Use auto-scoped TGs for server-wide policy**: `e_periodicsave_global` is a clean example of “one behavior pack for the whole server”.
- **Use TGs for reusable reaction logic**: `e_spawned_char` centralizes spawned-NPC cleanup without per-NPC plugin state.
- **Use plugins for holder-owned state machines**: silence/clearcast/effect plugins encapsulate duration, assignment, refresh, and cleanup on each target.
- **Use hybrid composition when needed**: a plugin can attach a TG to another participant (`PoisonedItemPlugin` -> `E_Poisoned_Weapon_User`) when you need both per-instance state and shared callbacks.

### Signals that your design may need refactoring

- **If your TriggerGroup starts reading/writing holder tags for state, consider a Plugin instead.**
  TriggerGroups are best as shared behavior packs; persistent per-holder state usually fits plugin ownership better.
- **If you are adding custom fields to timer callbacks, consider a Plugin instead.**
  Timers that need their own evolving state are usually cleaner as plugin lifecycle/state logic.
- **If your Plugin has no state at all, consider a TriggerGroup instead.**
  Stateless reaction logic is often simpler as reusable TG behavior.
- **If a class keeps one Plugin for almost its whole lifetime, consider moving that behavior into the class directly.**
  Long-lived, always-present behavior may be clearer and cheaper as first-class class behavior rather than an attached effect object.

---

## 8) Adding new trigger points in core code

When adding a new `Trigger_*` method in core code, decide the semantic type first (notification / deny / implementation / calculation), then use the matching execution pattern.

### Choose the dispatch method intentionally

- Use **`TryTrigger`** when script/compiled handler exceptions should be logged and execution should continue.
- Use **`Trigger`** when you want exceptions to propagate and terminate the chain.
- Use **`TryCancellableTrigger`** or **`CancellableTrigger`** when return value `1`/cancel should short-circuit behavior.

In practice, most gameplay trigger points in this codebase use `Try*` variants and then handle core-method exceptions explicitly.

### Standard exception-handling pattern for `On_*` calls

For core `On_*` method calls, follow the existing pattern:

```csharp
try {
	result = this.On_Something(args);
} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
```

(`FatalException` must stop the server; everything else is logged and the chain goes on. There is no other exception
type to let through.)

Use the same pattern for each participant in a chain.

### Pattern by semantic trigger type

#### Notification trigger
- Shape: regular (`TryTrigger`/`Trigger`), usually no cancel semantics.
- Pattern: run dynamic trigger, then `On_*`, continue regardless of return values.

```csharp
this.TryTrigger(TriggerKey.someEvent, args);
try { this.On_SomeEvent(args); } catch (...) { ... }
```

#### Deny trigger
- Shape: cancellable.
- Pattern: evaluate participants in strict order, stop on first cancel, then respect deny result before default action.
- Return propagation: if any stage cancels, skip default behavior.

```csharp
var result = actor.TryCancellableTrigger(TriggerKey.denySomething, args);
if (result != TriggerResult.Cancel) {
	try { result = actor.On_DenySomething(args); } catch (...) { ... }
}
if (result != TriggerResult.Cancel && args.Result.Allow) {
	// default action
}
```

#### Implementation trigger
- Shape: often cancellable.
- Pattern: let handlers consume/own behavior; cancel means “handled/stop.”
- Return propagation: if consumed (cancel), do not run fallback/default logic.

```csharp
var result = item.TryCancellableTrigger(TriggerKey.putOnItem, args);
if (result != TriggerResult.Cancel) {
	try { result = item.On_PutOnItem(args); } catch (...) { ... }
}
if (result == TriggerResult.Cancel) {
	return; // consumed by implementation
}
```

#### Calculation trigger
- Shape: regular or cancellable; return payload matters.
- Pattern: pass mutable args object (or returned value) through chain, then use final value in core logic.
- Return propagation: never ignore computed output (e.g., modified damage, selected target, command decision).

```csharp
this.TryTrigger(TriggerKey.damage, damageArgs);
try { this.On_Damage(damageArgs); } catch (...) { ... }
var finalDamage = damageArgs.damage;
```

### Chain safety rules (important)

- Keep participant order stable and documented; order is API contract.
- Validate that handlers did not illegally move objects mid-chain when required (existing `ReturnInto*IfNeeded` style guards).
- For deny/implementation chains, always gate default action with both:
  - cancel/continue result
  - semantic result (`args.Result.Allow`, computed value, etc.).


---

## 9) Naming convention (#1558)

Trigger names say the semantic type (section 4), so a scripter can tell from the key what a handler may do.

- **`@denyX`**: a deny, with a `DenyTriggerArgs` subclass (`argv[0]` is the `DenyResult`). It runs on every participant
  in the documented order; each participant's default check is its `On_DenyX`, so it can be overridden. Deny handlers
  only decide. Whatever a refusal leads to happens elsewhere (the hypnosis plugins note a refused death and act on their
  own timer).
- **`@X`**: the implementation, cancellable. Where there is nothing to cancel, `@X` is a plain notification instead,
  and there is no separate `@afterX` (a character's `@step` comes after the move).
- **`@afterX`**: a notification after an implementation that can be cancelled (`@afterSwing` after `@swing`).
- **Calculation triggers** carry the mutable value in their args and mostly have a noun name (`@hit`, `@damage`,
  `@spellPower`).

Examples, from the code:

| Chain | deny | implementation / calculation | notification |
|---|---|---|---|
| Double click | `@denyItemDClick`/`@denyCharDClick`, `@denyDClick` | `@dClick` | |
| Step | `@denyStep`, region `@denyExit`/`@denyEnter` | | `@step`, region `@exit`/`@enter` |
| Swing | `@denySwing`, `@denyDamage` | `@swing`/`@getSwing`, `@hit`/`@getHit`, `@causeDamage`/`@damage` | `@afterSwing`/`@afterGetSwing` |
| Death | `@denyDeath` | | `@death`, `@killCredit`, `@kill` |
| Spell cast | `@denySpellCast` | the spell's `@start`, `@spellPower`, `@causeSpellEffect`, `@spellEffect` | `@spellCast`, `@spellSuccess` |

**Exceptions, kept on purpose:**

- `@swing`/`@getSwing` are calculations with a verb name: they can still cancel the whole swing (`kick.scp`).
- `@causeSpellEffect` is an original key that already works as a calculation.
- `@monsterSpellPower`, `@beforeDoEffect` and `@beforeGetEffect` keep their separate stages:
  - `@monsterSpellPower` is a term of the multiplier, needed before there is any power to change;
  - `@beforeDoEffect` keeps the slayers in their own step, before `@causeSpellEffect`'s talents whatever the plugins'
    order (section 3);
  - `@beforeGetEffect` merged into `@spellEffect` would re-run the caster's `@causeSpellEffect` on a reflected spell.
- **The skill chain keeps its names** (`@skillSelect`, `@skillStart`, `@skillStroke`, `@skillSuccess`/`@skillFail`,
  `@skillAbort`). It is original and named by stage, and the other chains hang off it.
- **Single-stage triggers keep their names:** `@death`, `@kill`, `@killCredit`, `@disruption`, `@hostileAction`,
  `@npcRefuseItem`, `@gainExperience`, `@leaveSpawn`, `@pickingUp`, `@contextMenuSelect`.

Old names, and Sphere's and Erebor's, are in [trigger-catalog.md](trigger-catalog.md) section 5.

---

## 10) Deny results and the default check

`DenyResult` (`GameCore/DenyResult.cs`) has an `Allow` bool and `SendDenyMessage`. The subclasses say how the player
is told: `DenyResult_StringSysMessage` (a string), `DenyResult_ClilocSysMessage` (a cliloc the client renders),
`DenyResult_Allow` and `DenyResult_NoMessage`. `DenyResultMessages` (same file) holds the shared instances
(`Deny_ThatIsTooFarAway`, `Deny_ContainerClosed`, `Deny_ThatDoesNotBelongToYou`, `Deny_YouCannotPickThatUp`,
`Deny_ThatIsLocked`, ...): look there before adding a new message.

A step **denies** by setting `args.Result` to a refusing `DenyResult`, and usually cancels too. A step that **cancels
while leaving `args.Result` allowed** lets the action through without the later steps, including the default check.
The caller reads `args.Result` at the end.

**The exception is the step chain** (`AbstractCharacter.Trigger_DenyStep`, the regions' `@denyExit`/`@denyEnter`): there
a cancel always refuses the step, with or without a refusing `Result`, because the holds (paralysis, the vine, the GM
freeze) answer `@denyStep` with a bare cancel. A handler can't let someone through the later steps of a step by allowing
and cancelling.

**Permission first, then reach.** The deny chain decides whether this character may do it at all (ownership, locks,
flags, scripts). The engine's default check comes after it and decides whether they can from where they stand:

```csharp
public DenyResult CanPickup(AbstractItem item) {
	TriggerResult triggerResult;
	DenyResult denyResult = this.Trigger_DenyPickupItem(item, out triggerResult);
	//default implementation, can be skipped by returning true (cancelling)
	if (triggerResult != TriggerResult.Cancel && (denyResult.Allow)) {
		return this.CanReach(item);
	}
	return denyResult;
}
```

`CanReach` (`GameCore/CanSeeImplementation.cs`) checks sight, distance, line of sight, and for an item in a container
that the container is open. A ground drop's default is `CanReachCoordinates`. In a pickup, the cancels that count are
the picker's `@denyPickupItem`/`On_DenyPickupItem`, the item's `@denyPickup`/`On_DenyPickup`, the container's or
holder's `@denyPickupItemFrom` trigger, and each region's `@denyPickupItemFrom`/`On_DenyPickupItemFrom` (every region up
to the world region, #1579). The container's and holder's `On_DenyPickupItemFrom` return nothing, so they can only deny.
A double click's reach test is the clicker's `On_DenyItemDClick`, skipped when `DenyClickArgs.SkipReachChecks` is set
(Telekinesis, the OpenDoor macro, the potion macro); every other deny still applies. A macro's click is a whole double
click, so the clicker's `On_ItemDClick` runs too: like any item use it ends the clicker's current skill (a cast), as
Sphere's item use starts its idle skill for macro clicks too (`Cmd_Use_Item`, `Skill_Start(121)`). Its disruption also
breaks the clicker's own paralysis, which a real double click already did before #1558; the macros now do it as well.

A test of a deny rule checks both sides from the same spot: refused for the character the rule refuses, allowed for
one it allows. The allowed case proves the character is in reach, so the refusal is the rule's.

**The one path that skips the chain:** `SecureTrade.AddItem` (`scripts/Basic/Trading/SecureTrade.cs`) puts items into
the trade box with `PutItemInItem(box, 0, 0, false)`, as the trade parties were checked when the trade started. A rule
that must hold there (`attr_nodroptrade`) is an explicit check in `AddItem`. Taking items out of a trade box does go
through the chain.

---

## 11) GMs

**Core has no GM concept** beyond `AbstractCharacter.CanMoveEverywhere` (a player of `Globals.PlevelOfGM` or more): it
skips the terrain check, as it did in 2dbc90b, and the regions' step denies (#1558, as Sphere never refuses a GM).

**Scripts:**

- A GM's "allow all" sits in the script's `On_DenyX` override (allow, and skip or cancel the rest), never in whether
  the trigger is raised. Notifications fire for GMs too; a handler that must spare a GM checks itself
  (`Character.On_Step`'s after-step calls, the hiding, mystic and voodoo plugins).
- **Hostile effects never land on a GM** (pkorpel, #1558): `Character.IsImmuneToHostileEffects(source)` is true for a GM
  and any source but themselves, a missing source (a trap, lava) included. It is asked where hostile effects come
  through:
  - `Character.On_DenyDamage`: every kind of damage;
  - the swing: `WeaponSkillDef.On_Stroke` ends a swing drawn at a GM (no retry), and `DamageManager.ProcessSwing` lands no
    blow on one, so nothing a blow carries lands (a GM swinging at another GM included); NPC AI doesn't pick a GM as a
    target, as Sphere's NPCs give up on an invulnerable one;
  - `Character.On_SpellEffect`: no harmful spell;
  - `ParalyzeEffectPluginDef.Paralyze`: every paralysis or stun;
  - the effects applied directly: the vine hold, warcry, voodoo curses.

  So the holding plugins need no GM checks of their own. `.freeze` is a GM tool, not a hostile effect, and holds
  whoever a GM freezes.
- A GM drops things on the ground anywhere: `Character.On_DenyPutItemOnGround` allows and cancels for a GM, so no item,
  region or reach check follows. #1579 made that reach check real for everyone; before it, a region's cancel skipped it.
  A GM's pickups were reach-checked before and still are.
- Core's `CanMoveEverywhere` is plevel 4 or more, scripts' `IsGM` plevel above 1: the step's GM passes
  (`Character.On_DenyStep`, `Character.On_Step`) use the first, the immunity and the swing's work light the second, so a
  counselor gets some passes and not others until #1582.
- "GM" in scripts is `Character.IsGM` (account plevel above 1, toggled by `.gm`) until #1582 makes it a flag.
