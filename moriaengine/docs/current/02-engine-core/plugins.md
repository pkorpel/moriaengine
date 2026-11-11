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

`PluginHolder.Trigger`, `TryTrigger`, `CancellableTrigger` and `TryCancellableTrigger`
(`PluginHolder.cs:225-308`) run the holder's own trigger groups (its `EVENTS`), then each
plugin in the trigger list, newest first (`AddPlugin` puts a plugin at the front). For each
plugin, `Plugin.Run` (`Plugin.cs:60`) runs:

1. the scripted triggers of its def: the `on=@x` lines in the def's script section;
2. the compiled triggers of its class: every public method named `On_x`. These are collected
   at boot into `Generated/PluginTriggerGroups.Generated.cs`
   (`GameCore/PluginTriggerGroupGenerator.cs`) and registered for the def class named
   `<plugin class>Def`. The method's parameters are filled from the trigger's arguments, the
   same way as for compiled trigger groups.

In a cancellable trigger, a plugin returning `1`/`true` (or `TriggerResult.Cancel`) cancels
the action, exactly as a trigger group would.

For a thing the full order of a trigger is (`Thing.TryCancellableTrigger`, `Thing.cs:542`;
`AbstractItem.TryCancellableTrigger`, `AbstractItem.cs:517`):

1. the server-wide registered trigger groups (items', then all things');
2. the thing's own trigger groups (`EVENTS`);
3. its plugins;
4. its def's trigger groups: the def section's own `on=@x` lines and its `TEVENTS`
   (`ThingDef.TryCancellableTrigger`);
5. for an item, its `TYPE`.

So a plugin on a character hears whatever the character hears without anyone calling it:
`@skillSelect`, `@skillStart`, `@skillStroke`, `@skillSuccess` and `@skillFail` (raised on the
character by `SkillDef`'s `Trigger_Select`, `Trigger_Start`, `Trigger_Stroke`,
`Trigger_Success` and `Trigger_Fail` before the skill's own code, `SkillDef.cs:233-321`),
`@beforeSwing`, `@death`, `@logout`, the deny triggers, and so on. `@damage` (`DamageManager`) carries a
`DamageArgs` whose `damage` a handler may lower (the mana shield does, #1401) and whose `isSwing`/`isSpell` say where
it came from. Plugins hear a trigger newest first, so where the order of two changes matters (Erebor's damage_final:
energy shield, then mana shield) the first is called at a fixed point instead (`EnergyShieldPlugin.Absorb`). Magery
adds four of its own: `@spellCastTry` (#1401) after mana and reagents and before the casting rules
(`SpellCastRules`), the cancellable `@denySpellCast` (#1401) right after the rules (the energy shield refuses Recall
there), and (#1386) `@spellCast` once a player's cast is under way (`MagerySkillDef.On_Start`, after mana,
reagents, the casting rules and the spell's own `@start`; custom spell scrolls raise it too, NPC
casts don't, they go through `NPCAiTimer.TryCastSpell`), and `@monsterSpellPower` while a player's
spell power on a monster is worked out (`SpellDef.MonsterLevelBonus`, a `MonsterSpellPowerArgs`
whose `LevelBonus` handlers add to). `StandcastPlugin` answers both. A character's
`@denyMove` (`Character.CheckCanMove`, #1386) asks whether it may move at all, before the step
is worked out: a plugin returning Cancel holds it still (`CastFreezePlugin`, the paralysis
plugin, and since #1409 `GmFreezePlugin`, `ImmobilePlugin`, `LootHoldPlugin`, `EntangledPlugin`
and `VineHoldPlugin`, which were tags read by a hard-coded chain). Refusing in `@step` instead comes too late: #767's collision with other characters
(a shove, its stamina) runs before `@step`. After the step, `@stepTaken` (#1414, `AbstractCharacter.Trigger_StepTaken`,
with `running`) tells it a step was really taken. It comes before `On_StepTaken`, only for a step (`@newPosition`
fires for teleports too and has no `running`), and never for a GM.

Experience has two (#1416, [leveling-experience.md](../04-gameplay-systems/leveling-experience.md)): `@gainExperience`,
cancellable, in `LevelingSystem.AddExperience`, whose `GainExperienceArgs.Amount` a handler may change (`BaseAmount`
is what bonuses take their percent of); and `@killCredit` (`KillCreditArgs`: victim, share) on every fighter credited
with a death, player or NPC.

A pick-up raises `@pickingUp` on the picker (#1426, GameCore's `PickupImpl`, args: the item, the amount), after the deny
checks and while the item still lies where it was, beside the virtual `On_PickingUp`. `WeeklyQuestPlugin` counts a
monster's loot there.

Spells have two more (#1419, [magery-spells.md](../04-gameplay-systems/magery-spells.md)): `@spellPower` on the caster
(`SpellPowerArgs`, a mutable `SpellPower`), before the target's defense and after the inline pre-defense modifiers;
and `@spellSuccess` (`SpellSuccessArgs`) after a successful Magery cast. The pre-defense modifiers (charge,
Koncentrace, a stolen talent) stay at a fixed point because they multiply in order and plugins run newest first.

`@hit` on the attacker and then `@getHit` on the defender (#1411, `DamageManager.Trigger_Hit`) are raised for a blow
that landed, before its damage: a `WeaponSwingArgs` whose `DamageAfterAC` a handler may change (it's what
`CauseDamage` gets), and both are cancellable: a cancel ends the blow as a dodged one (no damage, `@afterSwing` still
raised, and the boss after-swing reactions a dodge runs) and `@getHit` isn't raised after a cancelled `@hit`. They
differ from Sphere's: raised after the dodge and the blow's own modifiers (armour, monster shields, class abilities),
with the damage stage (resists, shields, minimum damage, reactive armour) still to come in `CauseDamage`; only for
swings (a mystic's strike too, not spells nor ability blows such as backstab or split shot); and SRC isn't set. `VineHoldPlugin` counts the blows on a holding vine there.

Before the dodge, a swing raises `@swingLanding` on the attacker and `@getSwingLanding` on the defender (#1429,
`DamageManager.Trigger_SwingLanding`/`Trigger_GetSwingLanding`). They are Erebor's player `@AfterSwing` and
`@aftergetswing`: the blow is landing, armour and the monsters' own modifiers are applied, and the dodge is still to
come. Handlers change `DamageAfterAC`. They can't cancel the blow; `@hit` can. The profession plugins answer them.

A death raises `@kill` on the killer (#1431, Sphere's `@Kill`, args: the victim), in `Character.CauseDeath` after
`@death`, unless there's no killer, it killed itself or it's deleted. The ranger's plugin charges its killing spree there.

A profession's behaviour lives on its profession plugin. This is the original code's own pattern: each profession .def
names its `professionPluginDef`, and `ProfessionPlugin.InstallProfessionPlugin` puts that plugin on the player under
`_profession_`. Mage's `p_profession_mage` adds LScript `@SkillSelect`/`@SkillStart` to the shared `ProfessionPlugin`
class. A C# subclass works the same way: `[PluginTemplate WarriorProfessionPlugin : ProfessionPlugin]`, whose def
`[WarriorProfessionPluginDef p_profession_warrior]` warrior.def names (#1429). The base class keeps what every class
shares (spells and gear by profession, the shield block); a subclass overrides it and calls `base`.

## Declaring a plugin

### The class: `.ct` template or by hand

Most plugins are written as a `[PluginTemplate]` in a `.ct` file plus a partial class in a
`.cs` beside it. `ClassTemplateParser` turns each `.ct` file into one `<file name>.Generated.cs`
beside it, holding all of its templates, at every boot (`Main.cs:171`), before the scripts
compile:

```
[PluginTemplate RegenerationPlugin : TimerPlugin]
Vars:
	private double residuumHits = 0.0
	...
```

(`scripts/Basic/Regeneration/RegenerationPlugin.ct`). The generated file holds the
`RegenerationPluginDef` class (with `CreateImpl` and a `Bootstrap` that pairs the two
types), the plugin's fields, their save/load and deep-copy code, and the constructors.
A `Def:` section adds fields to the def instead, settable from its script section
(`FadingEffectDurationPlugin.ct`: `tickInterval`, `maxTicks`, `maxPower`, `pluginKey`).
The behaviour goes in the hand-written partial class (`RegenerationPlugin.cs`: `On_Assign`,
`On_Timer`).

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
   static constructor before calling its `Bootstrap()`. A `.ct` def class gets its `Bootstrap()`
   generated; a hand-written one declares `public new static void Bootstrap()` itself, calling
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
  and switched off by Magery; its freeze is `CastFreezePlugin`, refusing `@denyMove`.
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
  player's success fails), `@spellCastTry` and `@hitTry` (starving hurts), as Erebor's player
  event `e_allplayers` did. `@hitTry` is raised by `WeaponSkillDef` on a player (not a GM) as a
  swing is drawn. Before, those were calls from `Character.On_SkillSuccess`, `Magery.On_Start`
  and `WeaponSkillDef`, the food a tag and the tick a separate timer; and a landed blow hurt a
  second time from `DamageManager.ProcessSwing`, a leftover since #995. A custom spell scroll's
  cast doesn't hurt (Erebor's customspellcast never ran `@spellcast`), and a cast whose caster
  dies of it ends there. The skills whose own Erebor scripts ask about hunger
  (lockpicking, crafting, gathering, taming, the bard skills, healing, the shaman's pacts, the
  custom scrolls' casting rule) still call `PlayerHunger`, which reads the plugin. A cast the
  casting rules refuse still hurts (#1401): Erebor's hungryhurts was its `@spellcast`'s first line.

## Related

- [scripting-pipeline.md](scripting-pipeline.md): `.ct` templates and `Generated/`.
- [triggers-and-deny-chains.md](triggers-and-deny-chains.md): trigger dispatch.
- [persistence-and-saving.md](persistence-and-saving.md): def values and saves.
