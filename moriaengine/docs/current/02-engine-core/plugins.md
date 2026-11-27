# Plugins

A plugin is an object attached to a thing (any `PluginHolder`: characters, items, and
`Globals`) under a key. It gets every trigger its holder gets, can keep its own data and
its own timer, and is saved and loaded with its holder. This makes it the engine's way to
give things a behaviour without touching their classes: a system written as a plugin is
one class (plus its def), installed in one place, and everything else reaches it through
triggers it already raises.

This is SteamEngine's own design, not Sphere's. Sphere's nearest equivalents are memory
items and `EVENTS`.

## The pieces

| Piece | File | What it is |
|---|---|---|
| `Plugin` | `GameCore/Plugin.cs` | The attached object. A `TagHolder`, so it has tags and timers of its own. `Cont` is the holder, `Def` its def. |
| `PluginDef` | `GameCore/PluginDef.cs` | A def that makes plugins (`Create()`). Every plugin class `XPlugin` has a def class `XPluginDef`. |
| `PluginKey` | `GameCore/PluginKey.cs` | Where on the holder the plugin sits, written `@@name` in scripts. One plugin per key. |
| `PluginHolder` | `GameCore/PluginHolder.cs` | `AddNewPlugin(key, def)`, `AddPlugin`, `GetPlugin(key)`, `HasPlugin`, `RemovePlugin`, `DeletePlugin(key)`, `GetAllPlugins()`. |
| `TimerPlugin` | `scripts/Basic/TimerPlugin.cs` | A plugin with a `Timer` property: setting it (seconds) arms a timer that fires the plugin's `@timer`. |

Adding a plugin under a key that already holds another deletes the old one
(`PluginHolder.AddPluginImpl`). `@assign` runs when a plugin is added, `@unAssign` when it's
removed, `@destroy` when it's deleted. Deleting the holder deletes its plugins.

`AddNewPluginAsSimple` / `AddPluginAsSimple` store a plugin without putting it in the
holder's trigger list, so it hears none of the holder's triggers. It still gets `@assign`,
`@unAssign`, `@destroy`, and triggers run on the plugin itself, such as `TimerPlugin`'s
`@timer` (Discordance's effect is a simple timer plugin, `Discordance.cs`).

## How triggers reach a plugin

`PluginHolder.Trigger`, `TryTrigger`, `CancellableTrigger` and `TryCancellableTrigger` run
the holder's own trigger groups (its `EVENTS`), then each plugin in the trigger list, newest
first (`AddPlugin` puts a plugin at the front; a save and load and a dupe keep the order,
#1483). For each plugin, `Plugin.Run` runs:

1. the scripted triggers of its def: the `on=@x` lines in the def's script section;
2. the compiled triggers of its class: every public method named `On_x`. These are collected
   at scripts compile time by the Plugin trigger-dispatch source generator
   (`Tools/SteamEngine.Generators/PluginTriggerDispatch/*`), which emits partial overrides of
   `Plugin.Run(TriggerKey, ScriptArgs)`. The method's parameters are filled from the trigger's
   arguments, the same way as for compiled trigger groups. See
   [plugin-trigger-dispatch-generator.md](code generators/plugin-trigger-dispatch-generator.md).

In a cancellable trigger, a plugin returning `1`/`true` (or `TriggerResult.Cancel`) cancels
the action, exactly as a trigger group would.

A handler may remove plugins of its holder, its own and others, during the trigger (#1387, #1468). A removed plugin
doesn't run; the trigger goes on to the next plugin still on the holder, as a removed plugin keeps the link it had to
the one after it. A plugin added during the trigger goes in at the front, behind the walk, so it hears the next trigger;
but one removed and added again during the trigger runs when the walk reaches it, and the walk then goes on from the
front, so the plugins newer than it hear the trigger twice (as before #1468).

For a thing the full order of a trigger is (`Thing.TryCancellableTrigger`, `Thing.cs:542`;
`AbstractItem.TryCancellableTrigger`, `AbstractItem.cs:517`):

1. the server-wide registered trigger groups (items', then all things');
2. the thing's own trigger groups (`EVENTS`);
3. its plugins;
4. its def's trigger groups: the def section's own `on=@x` lines and its `TEVENTS`
   (`ThingDef.TryCancellableTrigger`);
5. for an item, its `TYPE`.

So a plugin on a character hears whatever the character hears without anyone calling it: the skill chain, the
swing, the spell cast, the step, the deny triggers, `@death`, and so on. Which triggers exist, on which participant, in
what order, with what args, and which plugins answer them, is in [trigger-catalog.md](trigger-catalog.md); the naming
convention (`@denyX` / `@X` / `@afterX`) is in [trigger-system-architecture.md](trigger-system-architecture.md)
section 9. Plugins hear a trigger newest first, so where the order of two changes matters (Erebor's damage_final:
energy shield, then mana shield) the first is called at a fixed point instead (`EnergyShieldPlugin.Absorb`), or gets
its own step (the spell slayers' `@beforeDoEffect`).

A profession's behaviour lives on its profession plugin. This is the original code's own pattern: each profession .def
names its `professionPluginDef`, and `ProfessionPlugin.InstallProfessionPlugin` puts that plugin on the player under
`_profession_`. Mage's `p_profession_mage` adds LScript `@SkillSelect`/`@SkillStart` to the shared `ProfessionPlugin`
class. A C# subclass works the same way: `[PluginTemplate WarriorProfessionPlugin : ProfessionPlugin]`, whose def
`[WarriorProfessionPluginDef p_profession_warrior]` warrior.def names (#1429). The base class keeps what every class
shares (spells and gear by profession, the shield block); a subclass overrides it and calls `base`.

## Declaring a plugin

### The class: template attributes or fully hand-written

Most plugins use template attributes directly in C#: `[PluginTemplate]` on the plugin class
and `[PluginDefTemplate]` on the matching def class. The build-time generator emits the
generated pairing/constructors/registration glue, while behavior stays in the hand-written
class body (`RegenerationPlugin.cs`: `On_Assign`, `On_Timer`).
See [ThingAndPluginClasses `Generator`](code%20generators/thing-and-plugin-classes-generator.md)
for generation details.

A plugin with no fields can be written by hand instead: a `PluginDef` subclass with
`CreateImpl` and a `Bootstrap` calling `RegisterPluginDef`, and a `[SaveableClass]`,
`[DeepCopyableClass]` plugin class with a copy constructor and a `[LoadingInitializer]`
one (`scripts/Basic/TimerPlugin.cs` is the original example).

### The def instance: a script section, or one made in C#

A plugin def is a def like any other and must exist before anything uses it, and before a
save that holds such a plugin loads. There are two ways to make one:

1. **A section in a script file**, naming the def class:

   ```
   [ActivableAbilityDef a_kick]
   ...
   PluginKey = @@kick
   PluginDef = p_kick

   [EffectDurationPluginDef p_kick]
   on=@beforeswing //params: attacker, defender, attack, piercing, armorClass, damageAfterAC
   	if (a_kick.CheckSuccess(this.Cont))
   		argv[5] = argv[5] * a_kick.EffectPower //reduce damage
   		...
   ```

   (`scripts/Basic/AbilitiesAndTalents/Abilities/kick.scp`). The section can also set the
   def's `Def:` fields and add scripted triggers to that def. One C# class can serve many
   defs this way: `EffectDurationPluginDef` alone backs `p_kick`, `p_silence_effect`,
   `p_protection_bonus` and others.

2. **A static field `instance` of the def's own class**, registered under its defname:

   ```csharp
   public static CraftingProcessPluginDef instance = (CraftingProcessPluginDef)
   	new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1).Register();
   ```

   (`scripts/Basic/skills/Crafting/CraftingProcessPlugin.cs`, on the def's partial class).
   The original `RegenerationPlugin.cs` comment names both ways: "it must be here as below or
   somewhere in the LScript as simple `[RegenerationPluginDef p_regenerations]`".

   It exists before the world loads because `ClassManager.InitClasses` runs the def class's
   static constructor before calling its `Bootstrap()`. A template-generated def gets its
   `Bootstrap()` generated; a hand-written one declares `public new static void Bootstrap()` itself, calling
   `RegisterPluginDef` (`TimerPluginDef`, `MagicChargePluginDef`) (#1369, see
   [persistence-and-saving.md](persistence-and-saving.md#defs-made-in-code-and-what-a-save-refers-to-by-name)).
   Use it as `CraftingProcessPluginDef.instance`. A def made in a static field of any other
   class, such as the plugin class, would exist only once that field is first read, after a
   save holding such a plugin has already failed to load; the boot logs an error for one held
   by a def or plugin class.

### Installing it

Install a plugin in one place: when the thing is made, when a skill or ability starts, at
login. `ActivableAbilityDef` (`scripts/Basic/AbilitiesAndTalents/ActivableAbilityDef.cs`)
shows the usual shape: the def's `PluginDef` and `PluginKey` fields name what to install;
activation makes the plugin with `def.Create()`, initialises it, then `AddPlugin(key, plugin)`
(so its data is set before `@assign`), and deactivation is `DeletePlugin(key)`. A plugin that
needs no setup is added in one call, `AddNewPlugin(key, def)`:

```csharp
if (!futureCont.HasPlugin(regenerationsPluginKey)) {
	...
	futureCont.AddNewPlugin(regenerationsPluginKey, SingletonScript<RegenerationPluginDef>.Instance);
}
```

(`RegenerationPlugin.TryInstallPlugin`). A plugin may delete itself when its work is done:
`RegenerationPlugin.On_Timer` does at full stats, and `TryInstallPlugin` puts it back when a
stat drops. `Character.On_Death` deletes it.

Deleting a holder's plugins (`PluginHolder.DeletePlugins`) works in rounds (#1479): each round takes the plugins on
the holder at that moment and deletes each one still there under its current key. So an `@unAssign` may remove,
move, re-add or add other plugins while this runs. A plugin another one's `@unAssign` already took away is left to
it. After 100 rounds that still find plugins, it logs an error and stops.

## Saving

A holder saves its plugins with itself (`PluginHolder.Save`, `Plugin.Save`): a line on the
holder referring to the plugin (`@@key*=` for a simple plugin), and the plugin as its own
section with its def and fields:

```
@@_regenerations_=(RegenerationPlugin)1759
...
[RegenerationPlugin 1759]
def=#p_regenerations
...
```

The list's plugins are saved head first and loaded back by the line each was read from
(`InsertByListOrder`), so after a restart they hear triggers in the order they did before (#1483: the save used to
follow the tags Hashtable and the load put each at the front, an arbitrary order). A dupe keeps the order the same way.

On load, a plugin whose def can't be found is deleted with an error
(`Plugin.LoadingFinished`). So a plugin def must be registered before the world loads,
whichever way it's made.

## When to use a plugin

- Behaviour that belongs to some things and not others, and reacts to what happens to them:
  an effect, a state, a running process (crafting, tracking, regeneration).
- Anything that would otherwise need calls added in other systems' code. If several
  unrelated files call into a system to tell it something happened, that event is a trigger,
  and the system is a plugin listening to it.
- Data that lives and dies with the thing: plugin fields save with the holder.

A plain `BoundTimer` (see [tags-and-timers.md](tags-and-timers.md)) is enough for a delayed
action with no reaction to triggers.

Two examples moved onto this pattern (the original developer's feedback, 2026-11):

- **Standcast** (#1386): `StandcastPlugin` answers the caster's `@spellCast`,
  `@monsterSpellPower` and `@activateAbility` instead of being looked up by the spell formula
  and switched off by Magery; its freeze is `CastFreezePlugin`, refusing `@denyStep`.
- **Poison** (#1422): a running poison is `DamagingPoisonEffectPlugin` (`p_poison`, the original code's dead plugin
  rebuilt on `EffectDurationPlugin`), one per kind under its kind's key, instead of `Poison`'s per-kind timers, its own
  `.boosts` source and a green-bar flag set by hand. `Poison` keeps the rules and its API (Apply, IsPoisoned, RemoveAll,
  the hits). A death ends it through `EffectDurationPlugin.On_Death`, so `Character`'s death no longer calls
  `Poison.RemoveAll`. Callers that poison (traps, monsters, the thief, the spell, the skill) are unchanged.
- **Polymorph and Incognito** (#1424): the spells are `DurableCharEffectSpellDef`s with `p_polymorph`/`p_incognito`
  holding what a timer and tags held by hand. The plugins restore on unassign, and `EffectDurationPlugin.On_Death`
  ends them, so `Character`'s death no longer calls their `End`. `DurableCharEffectSpellDef` used to throw for a source that
  was neither a potion, a book nor a scroll; an ability or a rune now uses the spell's key.
- **Player hunger** (#1396): `HungerPlugin` (a `TimerPlugin` with a saved `food`) goes on
  every player at login. Its timer is the food tick, and it answers `@skillSuccess` (a hungry
  player's success fails), as Erebor's player event `e_allplayers` did.
  - **#1558: a starving attempt hurts and fails, on the skill chain.** `HungerPlugin` also answers `@skillStart` and a
    swing's `@skillStroke` with Erebor's hungryhurts (`PlayerHunger.HurtIfStarving`). A starving player (no food) takes 10
    hits with "Umiras hlady..." **and the attempt fails** (pkorpel's call; on Erebor it only hurt): the handler cancels, so
    the skill's `On_Start`/`On_Stroke` never runs. Never a GM. It charges only an attempt Erebor charged, at Erebor's point,
    so each skill's own checks come first:
    - **a swing**: only a stroke `WeaponSkillDef.IsSwingAttempt` passes, a swing being drawn with the target there, in sight,
      not immune, in reach, standing still if the weapon needs it (Erebor's `@hittry`, before `@denySwing`). Any other
      stroke goes to `On_Stroke` uncharged, which ends the attack at a lost target as before. A starving swing stays drawn
      and is tried again a weapon delay later: once a swing, as Erebor's;
    - **a cast**: only one past Sphere's own checks (sight, mana, reagents: `MagerySkillDef.PassesOwnChecks`), so before the
      casting rules (#1401, where `@spellCastTry` was). Not a custom scroll (Erebor's customspellcast never ran
      `@spellcast`);
    - **mining, lumberjacking, fishing**: the wait, the saddle and the dark come before the start (`Gathering.Prepare`, on the
      target), so hunger is after the dark (miningfix);
    - **taming**: at the start; the targeting reveals first (f_tamingstart);
    - **a craft-menu order**: once, at its first item's start (`CraftingProcessPlugin.IsOrdersFirstItem`); the order's
      light and list come after, in that item's `On_Start` (`BeginOrder`), as makestring_start. A starving order stops.
      Cooking on a fire is no order and costs nothing;
    - **a lockpick try** is the one exception: it isn't a skill start (it runs by its own timer, and a skill start would
      end tracking, taming or a crafting order), so `Targ_Lockpick` asks `PlayerHunger` itself.
  - A starving attempt cancelled at `@skillStart` doesn't reach `Character.On_SkillStart`, so it doesn't stop the skills
    and the crafting order a real start stops. `@spellCastTry` is gone.
  - A custom spell scroll's cast doesn't hurt (Erebor's customspellcast never ran `@spellcast`). The bard skills,
    healing, the shaman's pacts and the custom scrolls' casting rule ask `PlayerHunger` about being hungry, which reads
    the plugin.
- **Hiding's step reveal** (#1478): `HiddenPlugin` (`p_hidden`) is on a character while it is hidden or
  invisible by magic; `Character.Trigger_VisibilityChange` adds and removes it. Its `@step` (after the move, #1558) runs
  `HidingSkillDef.OnStep` (Stealth's roll, the reveal), which `Character.On_StepTaken` used to call for every step of
  every character.
- **The labour camp's convict** (#1480): `ConvictPlugin` (`p_convict`) holds what six character tags held (Erebor's
  `tag.trestanec` and the tags beside it), from `Camp.Send` to `Camp.Release`. Its `@skillSuccess` is the convict's chop
  rule, which `LumberjackingSkillDef.On_Success` used to call (`Camp.AllowsChop`). The jail (account state, redirected in
  `Character.Go`, no `@go` trigger) and the camp's no-harm rule (the area's, not a character's) stay as they are.

## Related

- [scripting-pipeline.md](scripting-pipeline.md): script compilation and code generation.
- [trigger-system-architecture.md](trigger-system-architecture.md): trigger dispatch, deny results, the naming convention.
- [trigger-catalog.md](trigger-catalog.md): every chain and which plugins answer it.
- [persistence-and-saving.md](persistence-and-saving.md): def values and saves.
