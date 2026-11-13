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

---

## 4) Trigger semantic taxonomy

The vocabulary used in this guide is:

1. **Notification**
   - "Something happened" signal, to which trigger handler can react, but doesn't really implement the base behavior nor can it stop it.
   - Usually regular (non-cancellable).
   - Example: `@splitFromStack`, '@startup'

2. **Deny**
   - Permission gate.
   - Default action exists and proceeds unless denied (by setting `Allow = false` on the `DenyTriggerArgs.Result` object / `argv[0]` in LScript).
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
- **Deny**: Cancellable trigger with `DenyTriggerArgs` object (`Result` = `argv[0]` in LScript) to set `Allow = false` 
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
} catch (FatalException) { throw; }
catch (TransException) { throw; }
catch (Exception e) { Logger.WriteError(e); }
```

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


