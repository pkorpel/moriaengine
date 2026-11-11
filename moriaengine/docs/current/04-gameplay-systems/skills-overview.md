# Skills (overview)

The skill-sequence state machine lives in `scripts/Basic/skills/SkillDef.cs`, with
per-character skill values stored by `scripts/Basic/skills/Skill.cs`. Individual skills
(Magery, weapon skills, Hiding, Taming, etc) are `SkillDef` subclasses overriding the
abstract phase methods described below. This page covers the shared machinery; see
[combat.md](combat.md) for the weapon-skill specialization and
[magery-spells.md](magery-spells.md) for Magery's.

## The five-phase sequence

Every skill use runs through `SkillSequenceArgs` (`SkillDef.cs:344`), a small state object
(self, skill def, up to two targets, two generic params, a tool item, a delay, and a success
flag) carrying one skill attempt through 5 named phases, each firing a double trigger pair
(a generic `@skill<Phase>` on the character, then a skill-specific `@<phase>` on the
`SkillDef` itself) before calling the matching abstract `On_*` method:

1. **Select** (`PhaseSelect`/`Trigger_Select`, line 214) - decide whether this attempt is
   valid at all (e.g. `WeaponSkillDef.On_Select` rejects a null or self target, line 49).
2. **Start** (`PhaseStart`/`Trigger_Start`, line 237) - set up the delay
   (`GetDelayForChar`, which returns 0 immediately for a GM, line 139) and begin.
3. **Stroke** (`PhaseStroke`/`Trigger_Stroke`, line 256) - the "important moment in the
   middle" - this is where a `CheckSuccess` roll usually happens, setting
   `SkillSequenceArgs.Success`.
4. **Success or Fail** (`PhaseSuccess`/`PhaseFail`, `Trigger_Success`/`Trigger_Fail`) -
   branches on the `Success` flag set during Stroke.
5. **Abort** (`PhaseAbort`/`Trigger_Abort`, line 311) - the "interrupted from outside, no
   skillgain" path, distinct from a normal Fail.

Each phase's trigger pair can be cancelled at either the character level or the `SkillDef`
level - cancelling at any point short-circuits the rest of that phase (see
[triggers-and-deny-chains.md](../02-engine-core/triggers-and-deny-chains.md) for the general
pattern `TryCancellableTrigger` implements).

## Delay/scheduling

`DelayStroke` (line 538) either calls `PhaseStroke` immediately (a negative delay, the
sentinel `Timer.negativeOneSecond` used as the "no delay" default) or schedules a
`SkillStrokeTimer` (`BoundTimer` subclass, line 608) keyed by a fixed `TimerKey`
(`"_skillTimer_"`) on the character. `AbortSkill(Character)` (line 582) is the standard way
to cancel an in-progress skill from outside - it removes that timer and runs the Abort
phase. `GetSkillSequenceArgs`/`GetSkillSequenceTimer` (lines 591-602) let other code inspect
or interrupt whatever skill a character is currently mid-sequence on.

`SkillSequenceArgs` is `[SaveableClass]` with most fields `[SaveableData]` (Target1/Target2/
Param1/Param2/Tool/DelaySpan/Success) - an in-progress skill attempt (including its pending
`SkillStrokeTimer`) survives a save/reload exactly like any other per-character timer (see
[tags-and-timers.md](../02-engine-core/tags-and-timers.md)).

## Skill values: `Skill`/`SkillDef`

`Skill` (`Skill.cs:25`) is the per-character instance - `RealValue` (the raw, persisted
value, 0-1000 internally, displayed /10 as a percentage) and `ModifiedValue` (RealValue plus
a temporary `modification`, e.g. from a buff/equipped item - never goes negative even if the
sum would). Both setters route through `CharSyncQueue.AboutToChangeSkill` and fire
`SkillDef.Trigger_ValueChanged` (`@skillChange`) when the modified value actually changes.
`Skill.Cap` (line 97) is the live profession-based cap - delegates to
`ProfessionDef.GetSkillCap`, or 0 if the character has no profession at all (see
[professions.md](professions.md)). **This cap is read-only decoration today** - see the gap
below.

`SkillDef` (`SkillDef.cs:27`) is the per-skill-TYPE definition (`AdvRate`/`Delay`/`Effect`
ranges, all `.def`-loaded `double[]` permille-interpolated via `ScriptUtil.EvalRangePermille`
- e.g. `GetEffectForChar` maps a character's raw skill value through that curve, with a GM
treated as having the maximum value, line 161-168). `CheckSuccess(Character, int difficulty)`
(line 175) is the generic pass/fail roll every skill's Stroke phase calls - a GM always
succeeds; everyone else goes through `SkillUtils.CheckSuccess(skillValue, difficulty)` (an
S-curve, not consulted further on this page - see the Hiding note below for one place this
shape itself was questioned).

## Known gap: skill caps are not enforced, by design decision (#140)

`Skill.Cap` is correctly computed but, as of this writing, is read in exactly one place:
`GameCore/Networking/OutgoingPackets.cs` (lines ~1069/1084), purely to decorate the client's
skill-list UI. **Nothing prevents a skill's real value from exceeding its profession cap.**
There is also no organic "use a skill to raise it" mechanic anywhere in the engine - the
obvious extension point (`SkillDef.cs`'s `tkSkillGain`/`tkGain` trigger keys, commented out
at lines 201-202) is dead code.

This is a deliberate, confirmed-with-the-user deferral, not an oversight: most skills are
meant to be raised through the Leveling system's skill-point pool (flat +5/level, spent
through `LevelingSystem.TrySpendSkillPoints` - see
[leveling-experience.md](leveling-experience.md)), which DOES validate against
`Profession.GetSkillCap` at spend time. Only a smaller list of skills (Mining,
Lumberjacking, Fishing, Cooking, Taming, Lockpicking, Blacksmithing, Carpentry, Bowcraft,
Alchemy, Tailoring, Tinkering, Inscription - mostly crafting/gathering) are meant to be
raised through actual use instead, and that mechanism is explicitly deferred until
crafting/gathering content becomes the active focus (#140, Backlog).

## Worth a second look, not a confirmed bug: Hiding's double-random roll

`HidingSkillDef.On_Stroke` feeds a *random* value (`Globals.dice.Next(700)`) into
`CheckSuccess` as the "difficulty" argument, rather than doing Erebor's own direct
`hiding_skill > rand(1025)` single comparison. The net effect is two layered random rolls
instead of one. This diverges from the Sphere reference for no documented reason, but
nothing confirms it's actually wrong (the generic `SkillDef.CheckSuccess` S-curve is used
this way by other skills too) - flagged as worth a second look if Hiding's success rate
ever gets reported as feeling off, not something to "fix" pre-emptively.

## Skill checks and gaining by use (#962)

Sphere's numbers. A check is an S-curve of skill minus difficulty with a variance of 10.0: equal is 50%, 10.0 over is 75%, 10.0 under is 25%. Before #962 the variance was 25.0, so skills are less forgiving both ways now.

Gaining by use is `Skill_Experience`, `SkillGainSystem.TryGainSkill`, with the attempt's difficulty, negative for a failure:

- Failing teaches only under 30.0, and then counts as half the skill less 0.8.
- The chance is 1 in `ADVRATE` successes, read at skill + (skill - difficulty), so a hard job teaches far more than an easy one.
- Over 40.0 below the skill it's halved, over 50.0 below it's nothing.

Only skills with a real `ADVRATE` on Erebor gain this way: crafting, Fishing, Lumberjacking, Mining. On Erebor the rest are `0fffffff`, here 0. Lockpicking and Taming have their own formula. Gathering's difficulty is the resource's own `SKILL` range. All curves are Erebor's live values, see `systems/skillvalues.def`.

Not ported: Sphere's skill-sum degrade, as `skillsum` is 30000 on every profession. Stat gains by use: every skill has `BONUSSTATS=0` on Erebor.

## Hunger (#924)

Sphere's food stat with Erebor's use of it, `scripts/Basic/Hunger`. A player holds up to 15 food and loses a point every 30 minutes in the world, on `.settings`. Erebor's `REGEN_4` line never reached Sphere, so this is its built-in rate. From nothing it goes up and down between 0 and 1. Under 40% the player is told how hungry they are.

Double clicking `t_food` or `t_fruit` eats one piece, adding its weight in stones, at least 1. The food line is always on `.boosts`, under Stav.

At 0 food hits don't regenerate, and a blow, a spell or starting to craft costs 10 hits with "Umiras hlady...". Under 7 food any skill's success can turn into a failure, 7 times in 10 at nothing; blows aren't skills here. GMs aren't troubled by that roll. Skills Erebor scripts on its own timers skip it, as their native skill is cancelled and its success never runs: Hiding among them (#1095), whose `i_hiding_timer` only rolls Hiding against `rand(1025)`.

Not ported: the food box (`systems/foodbox.scp`). Darkness stopping blows and spells (`nolightfizz` on `@hittry` and `@spellcast`) is ported (#995, `WorkLight`).

## Lockpicking (#962)

Erebor's `systems/skills/lockpicking.scp`, a script with its own timer, not a skill stroke. The skill list refuses it.

A lock is the container's type, `t_container_locked`. Without a key nobody but a GM opens one, which wasn't so before #962: the type did nothing. Tax chests answer their own click first. How hard a lock is lives in its `tag.lockpick`, as Erebor's treasure chests have it, 0 when unset.

Double click a pick, target the lock within 2 tiles and in sight, with light to work by. It reveals you, takes a use off the pick at once (a pick of 3 tries becomes one of 2, and so on), and 2 s later rolls 0 to 100.0 under skill minus the lock's difficulty. Opening makes it `t_container` for good and sets `tag.lockpicked`. Either way Lockpicking may rise by the `skillgain` formula. One job at a time. A ship's hold has its own roll, ready for when ships exist.

Not ported: Erebor's debug lines "wtf 1" and "wtf 2", printed as a worn pick is swapped.

## Snooping and Stealing (#962)

Stealing is off, by Erebor's own `on=@start action=-1`: the cursor comes up and nothing follows, so taking from a pack you've snooped is always refused.

Double clicking someone's pack within 2 tiles of them snoops it, with Sphere's rules: 2 s, against their Dexterity, or no roll at all for a higher plevel. A lower plevel gets nowhere. A player vendor can't be snooped. Failing while invisible makes you a criminal. Everyone around who sees you may notice, the mark most readily and your Snooping against theirs. Noticed, you lose 10 karma, down to -500, and each NPC witness has 1 chance in 60 of making it a crime.

A witness that takes it as a crime reacts as in Sphere: a player remembers, so a call for the guards sends them after you, an NPC flags you and calls the nearest guard (`CrimeWitness`). An NPC mark protests, and 1 time in 4 runs for 20 steps. Sphere also summons a guard if none is near; here one must be around. Karma falls by Sphere's scale, double while it's above 0.

## Healing and Veterinary (#962)

Erebor's `systems/skills/healing*.scp`, on timers rather than skill strokes. The skill list refuses both.

Double click a bandage and target someone within 6 tiles in sight, not a summon, hurt or bleeding. It takes 7 to 3 s by Healing and a bandage, and drops your weapon to the pack, but not a staff. Another bandage in between cuts the first short. People need Healing and anything else Veterinary, checked against a roll of 0 to 102.4, and a bloody bandage comes back. A bleed is stopped first, and that's all that treatment does. Otherwise it heals by skill + Anatomy: 0 to 15 per 100.0 of that sum for Healing, 5 to 50 for Veterinary.

Only shamans use bloody bandages on people. Their class ability `.improvement` can double a heal, and a priest's `.enlightment` can cut the time to 2 to 1 s. A priest who healed a player shares for 15 minutes in the experience of that player's fights.

A clean bandage on a tub of blood turns the stack bloody, a bloody one in `t_water_wash` turns it clean.

20 bandages on a body within 2 tiles try to raise it: a priest or shaman a person, from 80.0 Healing and Anatomy, the ghost within 4 tiles in war mode; a ranger a beast, from 80.0 Veterinary and Animal Lore. 4 s later a roll of 70.0 to 105.0 under the skill decides. A raised beast gives no experience or gold and returns 20 bloody bandages. Horses, llamas and spawns marked `i_nores` can't be raised.

## Hiding, Stealth and Detect Hidden (#962)

Erebor's `systems/skills/hiding.scp` and Sphere's reveal rules.

Hiding takes 3 to 1.5 s, then Hiding against a roll of 0 to 102.4. Starting any other skill in between spoils it. Stealth's button does the same; the skill does nothing else on its own.

A hidden step rolls Stealth, plus 10.0 per point of darkness, against 0 to 104. Running always reveals, and so does any step while magically invisible. You're also revealed by attacking, a physical hit, picking something up, speaking, teleporting, and pressing a skill button with nothing else going on.

Detect Hidden: 2 s of looking, then everyone unseen within 25 tiles is tried, not GMs, the invulnerable or the dead. It's Detect minus their Hiding, plus 100.0, minus 5.0 a tile, against a roll of their Hiding, plus twice what's over 100.0. A found person is simply revealed. The same person can't be tried again by you for 7 s, and you can't search again for 8 to 4 s.

## Meditation (#962)

Sphere's own skill, which Erebor keeps. An opening roll against up to 99.0, then a mana point a stroke, 1 to 0.3 s each by skill, until full. A step ends it with "Pohybem jsi ztratil soustredeni", and a mana shield refuses it. Weapons and low health don't matter. The Mage talent `a_meditation_bonus` no longer does anything: Erebor has no such thing.

## Taming (#962)

Erebor's script, systems/skills/taming.scp, not Sphere's skill. `TamingSkillDef` runs the checks in Erebor's order: sight, crook, can it be tamed at all, Taming against the animal's, Animal Lore against the animal's, already a pet, promised to someone (a summon rune in its pack), within 5 tiles. A creature's Taming is its rolled NPC skill (`AnimalTaming =` in the def). Nobody tames without a shepherd's crook in hand: spruce up to 40.0, then chestnut, oak, teak, mahagon, and eben past 120.0; the tiers are the converted `material_` and `crook_` constants. GMs need no crook.

`TamingTimer` talks for 3 to 7 strokes of 2 s, then rolls `0-1000 < tamer - animal`. Being hit, another skill or a spell ends it ("Ochocovani bylo preruseno"), `ScriptedSkills.Stop` is Erebor's stopscriptedskills. Using Taming again meanwhile only clears that mark and brings the next stroke forward, a quirk of Erebor's kept as is. A spawned animal led over 70 tiles from its spawn goes home. A pet leaves its spawn, which makes another. An animal raised by a healer (`i_poresu`) is tamed on the first stroke, whatever the roll. A pet loses 1 food and its karma.

Gains are `skillgain` with a bonus: 100 usually, 500-600 for a spawned animal of 30.0 taming or more when tamed, 300-400 when that fails on one above 30.0. Such a tame also adds to `MountKnowledge`: the 18 mounts of `def_taming_mount`, points up to each one's maximum, shown on .boosts with a button to Erebor's list and legend. What the knowledge gives is mount powers, #925, along with the ranger's ra_mountsKnowledge limit.

Not ported: "Zvire zemrelo hlady". Sphere's wild animals lose food and find their own, ours have no food until they are pets, so the check has nothing to read.

## Animal Lore (#962)

Erebor's f_alore, at once from the cursor, with no stroke, no delay and no gain. Anyone can be looked at; an item gets "To neni zvire", the ground nothing, as a skill's target isn't checked for reach. It tells whose the creature is, then, if a roll against the skill passes and it's an animal or knows taming, how fed it is and, with Animal Lore at least its own, what it eats. Yourself on a horse tells the horse's hunger. Wild animals always read as full, they have no food here. What riding an animal gives comes with mount powers, #925.

## Bard skills (#962)

Erebor's systems/skills/discordance.scp, peacemaking.scp and provocation.scp, with `Bard` holding what they share. All want an instrument in the pack, roll Musicianship against 0-1025 ("You play poorly."), then their own skill against the target's experience: first the skill's whole reach, which refuses outright, then a roll up to that reach. The result lands after the skill's delay. Nobody gains in them by use, on Erebor they have no skillgain call. Another skill, a hit or a spell spoils them, `ScriptedSkills.Stop`.

Discordance (Erebor's Enticement, reach up to 300.0) weakens a monster that has none of the skill and isn't a boss: `skill / 4` permille off stats, hits, mana, stamina, Magery, Eval Int, Resist and fighting skills, for 10 s plus a second per 2.0 of skill. Hits, mana and stamina keep their proportion. Peacemaking (reach up to 200.0) wants more skill than the creature's own and puts it to sleep for as long: it neither fights nor casts, being hit doesn't wake it, and it goes for the bard on waking. Its timer starts with the cursor, so an unanswered cursor blocks the skill for 60 s. Provocation (reach up to 100.0) is the command `.provo`, the button only says so: a creature, then whoever it should attack, each rolled on its own; a player can be the second target but is never provoked.

Musicianship is played by double clicking an instrument, against 0-90. Sounds are the instruments' TDATA1 and TDATA2, which the converter now keeps.

## Poison and Poisoning (#962)

`Poison` is Erebor's f_poison_doPoison and its poison manager, and the only way anything gets poisoned. Eight kinds: damage, percent of hits, paralysis, fizzling, stamina, and the three stat curses. One poison of each kind runs on a character; a stronger dose replaces the weaker, each hit takes a tenth off (at least 1), and it ends when nothing's left. A cure takes all of them. Poison resist of 1000 or a cure's immunity stops any new one. Hits are every 5 s, paralysis every 11 s (`PoisonEffectSettings` on .settings), and they credit the poisoner by `FightMemory`, as damage_noresponse.

A running poison is its plugin (#1422): `DamagingPoisonEffectPlugin` (`p_poison`), Erebor's i_memory_jed.
- It sits under its kind's key (`DamagingPoisonEffectPlugin.KeyOf`) and carries its kind, strength and dealer.
- Its own tick timer does the hits. It has no duration: it ends by weakening.
- `.boosts` lists it as "Jed" with its strength. It doesn't name the dealer (`ShowsSource`), as before.
- The green health bar goes through the character's poison counter, which bleeding shares. Before, a poison ending cleared the bar while the character still bled, and a bleed ending cleared it while a poison ran.
- A death ends it through `EffectDurationPlugin.On_Death`.
- It isn't a `FadingEffectDurationPlugin`: Erebor's poison doesn't fade over a set time, and doses don't add up.

A bottle is double clicked, the Poisoning button only says so; the cursor gives 20 s. It goes on a weapon with Erebor's `nastaveni_weappoison` numbers (difficulty, percent of the bottle, doses, 5000 being past anyone) or on up to 50 arrows with a poisoned kind (`<arrow>_poison`, at 75 percent). Skill must beat the weapon's difficulty plus 0-100, or 0-1000 for arrows. A blade's dose wears down with use. On a blade or an arrow the percent poison is plain damage, twelve times stronger on monsters. A failure can poison you, and a weapon's bottle can spoil. Nobody gains by use.

A monster with Poisoning has its skill in 1000 of poisoning on a blow, skill / 100 strong. The Poison spell is spell_poison_custom: spell power over 150, or 140 less up to 30 by level on a monster, a point more on a monster or under a curse, plus a point of damage at once.

Bottle strengths are `nastaveni_poison_i_potion_*`; the bottles themselves aren't in the archive, so `poisonPotions.def` stays hand-written. That the four damage bottles are the percent poison is a reading of Erebor's comment that poison_c can't be dealt from a weapon, not something the archive states.

Not ported: traps, poison fields, the dungeon and quest items that poison, the casting monsters' cure when poisoned, Sniper's poisoned knives.

## Information skills (#962)

Anatomy, Arms Lore, Item ID, Eval Int, Forensics and Taste ID are Sphere's OnSkill_Info in `InformationSkillDef`: a cursor with no reach check, a difficulty from the target (2 on yourself, otherwise 0-59), and the result after the skill's delay. No gain by use. Sphere's English texts stay, as they did on Erebor; the prompts and failures are Erebor's Czech. Item ID first says who made a thing and, sometimes, its durability; Taste ID says whether a thing is poisoned and by what. Nothing has a price yet, so Item ID always reports no resale value. Forensics reads the killer and time of death that `Corpse.NoteDeath` records; carved corpses aren't told apart.

## Begging, Spirit Speak, Herding, Camping (#962)

As Sphere, with no gain by use. Begging is a roll against the target's Int that gives nothing. Spirit Speak rolls against 0-89 for four minutes of contact, which opens nothing yet as ghosts' speech isn't garbled here. Herding starts by double clicking a crook: pick an animal, then a spot, and on a roll against about its Int it walks there; a GM's creature goes at once. Camping is used by double clicking kindling on the ground, which makes a fire for 4 minutes and one more per stick. Remove Trap waits for Erebor's traps, none are ported.

## Gathering stages (#962)

Mining, Lumberjacking and Fishing start with Erebor's f_tezba_pauza (`Gathering.Begin`): a try within 2 s of the last says "Jeste nemuzes pouzit skill." and pushes the next one to 4 s; a rider is dismounted; the dark can refuse; hunger hurts; hiding ends; the spot must be within 3 tiles and in sight. Above 100.0 the 2 s shrink by 0.03 s a percent. Mining and Lumberjacking then swing at the skill's 1.0 s, about 2 s a try, before the roll, and mining wears the tool once a try. Fishing has one stroke at 2.5 s with a splash, and the catch lands at your feet. A held tool is still required, a decision of ours (#370). Failures are Erebor's overhead lines.

A pick or hatchet try takes about 2 s (#1001, from the user's memory of Erebor): `leastStrokes` and `mostStrokes` default to 1 extra swing at the skill's 1.0 s. Sphere's own numbers would be 2 and 6, so 3 to 7 s. Both are on `.settings`; a save that already holds them keeps its values.

Ported since: the craftsman's vigour, a try at a fresh spot that can find the next metal or wood up (ra_vigour, #1017, `CraftsmanAbilities.Vigour`), the Elven wood rule (#427, `ElvenGrove`, see crafting-gathering.md) and the labour camp's carrying limit for convicts (#1107, see ../03-admin-tooling/moderation.md). Not ported: the crafting statistics.

## Magery's rules (#962)

Cast times, mana, durations and skill requirements come from Erebor's `nastaveni_` table through the converter (systems/spellvalues.def, `SpellValuesDef`), over what the spell's section says. Difficulty is the skill requirement, half from a scroll. Effects aren't overwritten, their meaning differs by spell. Gate Travel's duration stays a minute (#489): Erebor's number reads as 3-4 s. Erebor switched three spells off with numbers no one reaches: Paralyze Field costs 4044 mana, Poison Field wants 500.0 Magery, Earthquake 300.0.

Sphere pays at the end of a cast: mana and reagents are checked at the start and taken when it lands. A fizzle or an interrupted cast costs half the mana plus a random bit, and the reagents. A GM pays nothing and casts in 0.1 s.

`SpellCastRules` is Erebor's @spellcast, for players: darkness fizzles all but Night Sight, reactive armor by class, no blessing or cursing items, no teleporting onto items or NPCs, casting at others reveals you, no fields on what you hold, mages and necromancers need a good staff for summons, Mark doesn't work in dungeons, any spell but an arrow or harm on yourself drops a mana shield and half your Int of mana. Battlefield and conquest checks of recall and gate wait for those systems.

## Quick slots (#962)

Erebor's sixteen slots in `QuickSlots`: `.setequip0` to `15` remember an item or a character, `.equipN` uses the item if you carry it, `.healN` bandages the remembered character, `.samhealN` does it with a shaman's bloody bandage. The old `scripts/Optional/TargetMemory.cs` of ten slots is gone; `Player.targMem` stays, unused, as a saved field.

## Craft masters (#962)

Erebor's systems/mistri.scp in `CraftMasters`. A craftsman says hello to the Mistr Remesel for a list of what they made themselves and pays gold for experience, each thing once. The experience comes from the skills and materials of the recipe, the quality and `nastaveni_craftexp_*`; the price from your level, up to 56. The ship builders of Gondor and Mordor take logs, ore and ingots you have the skill for, for experience and no gold, and count the ships: a Gondor ship is 1500 teak, 300 mahagon and 50 eben, a Mordor ship 1200, 250 and 40. Items carry Erebor's values now (`nastaveni_cm_value`, `ItemDef.ItemValue`).

Not ported: bags as one row (top-level pack items only), the builders' spoken pages, statistics per player, the GM stats dialog, and closing the dialog on steps.

## Traps and the quiver (#962)

`i_past` is Erebor's player trap: double click it and aim a bomb potion, a poison bottle or another trap at it. It works once on someone stepping on it, and a second loading within a minute hurts. `i_toulec` holds up to 5000 arrows of one kind, poison and all, added and taken out by its dialog. It only stores them, a bow doesn't shoot from it. Erebor's older wooden quivers, its GM's monster traps and spike traps aren't ported.

## Darkness (#976)

`WorkLight` is Erebor's nolightfizz: with no night sight and no lit thing within 2 tiles or in the off hand, the area's light against 0-64 decides, so even daylight fails 1 time in 65. It stops crafting, gathering, taming, lockpicking, poisoning, spells other than Night Sight, and a player's blow, which tries again a second later. GMs are spared for blows and spells, which Erebor didn't do: casting and fighting as a GM isn't meant to be random.

## Spells' resists and curses (#962)

Poison, Paralyze and the poison and paralyze fields don't lose power to defense as damage spells do. Their live "effect" numbers (`Resist` in spellvalues.def) are a roll of the caster's spell power that must reach the target's defense, or "Cil kouzlu odolal". Paralysis resist cuts the power first. The Poison spell is Erebor's f_effect_a_poison: mind power times Poisoning and Magery, a plain damage poison every 5 s, weighted by `percentOnMonsters` or `percentOnPlayers`. A monster of 110 in a stat loses a share of it to clumsy, feeblemind, weaken and curse, not a number. Curse also takes 5% off a monster's fire resist.

Nine of the 64 spells aren't ported, none could be cast on Erebor: Create Food, Magic Trap, Magic Untrap, Magic Lock, Telekinesis, Unlock and Incognito need 500.0 Magery, Mana Vampire 250.0, Polymorph 700.0.

## Spell scrolls and the scroll case

A double click casts a `t_scroll`, as Sphere: half mana, half difficulty, the scroll goes whether it lands or not.

`t_customspell_scroll` is Erebor's scroll of a spell past Sphere's numbers (Mana Shield 104, Mass Bless 102). The scroll must be on you and not directly in the bank, and its def's `Tag.profession_*` lines say who may read it. Those spells skip `@spellcast`: full mana (half on a fizzle), no darkness check, the skill is checked before the cursor comes up, and the roll is `f_circle_fizzle`: skill capped at difficulty + 39.2 against a throw of difficulty to difficulty + 40.0. Mana Shield can only be aimed at yourself. Mass Bless has no `csflag_mass` on Erebor, so it blesses one target.

Mass Bless is an area spell of 8 tiles from its target, the user's call (#1001): Erebor's `a_mass_bless` has no `csflag_mass`, so it blessed one target. Who else gets it is `SpellDef`'s rule for beneficial area spells: nobody the caster stands worse with than the main target. Cast on yourself, that means allies only. Gate Travel lasts Erebor's live 3 to 4 s, not #489's 60 s.

Nine core spells live in Spells/CoreSpells with Erebor's live values (#1001): Create Food, Magic Trap, Magic Untrap, Magic Lock, Telekinesis, Unlock, Incognito, Mana Vampire, Polymorph. Skill 250.0 to 700.0 keeps them out of a book, and a scroll only halves that for the roll, so it fizzles; only GMs cast them. Sphere's wands have difficulty 1 and would work, but wands aren't ported. Magic Trap and Untrap have no effect in Sphere itself. Mana Vampire takes mana without giving any to the caster, as `f_effect_a_mana_vampire`, and is refused on a target without Magery. Unlock opens a door one time in 900, `MAGICUNLOCKDOOR`. Polymorph's `sm_polymorph` acts on the caster alone; Sphere's GM cursor for polymorphing others isn't ported. Incognito on an item isn't ported, there's no ATTR_IDENTIFIED here. Neither is dispellable yet.

Since #1424, Polymorph and Incognito keep their state in their effect plugins (`DurableCharEffectSpellDef`):
- **`p_polymorph`:** the body it replaced and the Str and Dex changes.
- **`p_incognito`:** the true name and skin.
- How they behave:
  - Their time running out, a death, or (for Polymorph) a new form gives everything back.
  - Both show in `.boosts` with the time left. An incognito doesn't name who disguised you.
  - A polymorphed character now dies in its form: the death sound is the form's. The old code gave the body back first.
  - Both still end silently and still aren't dispellable.
  - A second Incognito keeps the name and skin and only renews the time, as Sphere.

The menus of Tracking, Summon and Polymorph open as a dialog, as Sphere's picture menu (packet 0x7C with models) or both at once, `MenuStyles` (#1004). The choice stays with the players: `.menustyle` cycles it for the character; `defaultStyle` on `.settings` is for everyone else, 0 a dialog, 1 the picture menu, 2 both. With both open the first answer counts: the picture menu closes the dialog, and once the dialog is gone the picture menu is ignored, as a server can't close that one. Tracking has Sphere's five rows both ways, in English, and a pick of a name tracks, where 99z8's does nothing. Its "Anything that moves" lists NPCs and animals, not players, because our All does.

`i_scroll_case` (`t_svitkovac`) holds up to 999999 scrolls of each spell as counts on the item. Scrolls go in by dropping them on it, by its Pridat cursor, or a whole container by targeting it. Pages are the circles in pairs, necromancy and the rest. Vybrat takes one of everything.

## Necromancy

Spells 65-70 sit past the 64 a client's book can show, so `SpellBook.necroSpells` holds them, Erebor's `morex`. A scroll dropped on any book teaches them. A necromancer's double click on a book holding one opens `D_NecroBook`, `d_nec`, whose back arrow opens the plain book; `.necrobolt`, `.frostbolt` and `.necrolight` cast from a book too.

`i_spellbook_necro` has no `[ITEMDEF]` in the archive, and nor has the plain `i_spellbook`. Here it's a plain book in Erebor's colour with the first three circles, as `tm_necronewbie`'s `more1=0ffffff`, plus all six necro spells as `i_spellbook_pvp`'s `morex=0ff` (#1001, the user's call). It also holds Summon Creature, because `d_nec` needs that before it shows Summon Undead.

Fire Bolt is 0-17 through armor at 850. Frost Bolt is 0-30, then one second of freeze: within 7 tiles, when `om * 14` doesn't beat `sm * {2 11}`, and past the target's paralyze resist. Light has no native effect beyond a charged layer, so it runs as Night Sight with its own slot.

`allowedSpells` of every profession now match Erebor's `spells_<class>_0_n` bits, for spells that exist. `spells_mystikself` are a Mystic's, on himself only.

Animate Dead is Erebor's `[function animate]`: on a monster's corpse (a player's does nothing) it makes the monster again, stripped of everything not newbie, as the caster's pet with no gold of its own. The monster's exp picks the staff needed: up to 500 spruce, 1000 chestnut, 1500 oak, 2500 teak, 4000 mahagon, 6000 eben, 9000 elven, and past that no check, as Erebor's chain ends. It costs `exp / animateManaDrain + 1` mana a second (`.settings`, Necromancy) until the caster can't pay, lasts `level / 2 + 50` seconds, and leaves no corpse. Corpses of `NoAnimate` defs refuse. The `ra_` abilities are read off tags, nothing sets them yet.

Summon is a gump of Sphere's `sm_summon` before the cursor, by profession, Magery, staff, mana and reagents. Necromancers' dear creatures take `e_necro`'s extra mana and reagents when the cast starts, the first level of each family only, as Erebor's check compares only that id. NPCs still roll from `summonChars`.

Bone Armor is Erebor's `necroarmor`: 10 mana (5 from its scroll), a second later a fizzle unless Magery beats `{300 700}`, then Protection at full power and bone pieces on free layers for 180-320 s.

Spell effects checked against `f_effect_a_*`: Mind Blast is `dam_god`, rounded to whole points, nothing when the target's Int is as high as the caster's, and it ends a paralysis. Mana Drain only touches a target with Magery. Reveal has no contest. Magic Reflection sends a spell back at 1.07 of the power it was cast with (`nastaveni_global_reflectSPlowering`), against the caster's own defense.
