# Professions

9 professions (Ranger, Priest, Mage, Mystic, Necromancer, Craftsman, Shaman, Warrior,
Thief - #134, sub-issues #135-#138), built on top of an enforcement layer that already
existed in the engine but had nothing feeding it before this pass.

## The mechanism existed before any content did

`ProfessionDef` (`scripts/Basic/Professions/ProfessionDef.cs`) already had per-skill
`(minimum, cap)` pairs, an `AllowedSpells` list, 3 talent-tree-branch slots, and
ability-point caps - and skill-cap enforcement plus Magery-only spell-cast restriction
(`ProfessionPlugin.On_SkillSelect`/`On_SkillStart` -> `CheckCancelSkill`,
`ProfessionPlugin.cs:64-84`) were already genuinely live. But `ProfessionPlugin.
On_Assign()` was dead code (granted nothing), and **nothing anywhere ever assigned a
profession to a character** - confirmed via grep, zero callers. The enforcement layer
existed with nothing feeding it.

Content status before this pass: only Mage and Thief had a `.def` file at all.

## Data format: reused `.def`/`ProfessionDef`, extended with new fields

No new content format was invented. `ProfessionDef.LoadScriptLine`
(`ProfessionDef.cs:381-418`) recognizes a line as either an ability name/defname (two
numbers: display order, max points) or a skill name/defname (two numbers: starting
minimum, cap) - the same per-skill-cap model that already existed, just now actually
populated for all 9 professions.

New fields added in this pass (`ProfessionDef.cs:36-51`), using the existing generic
typed-`FieldValue` array-of-enum resolution already proven by `allowedSpells` - no new
parsing code needed:
- `allowedWeaponTypes` / `allowedArmorMaterials` (`WeaponType[]`/`WearableType[]`) - see
  "Weapon/armor restriction" below. Empty array means "never configured, unrestricted" -
  the same "incomplete content defaults permissive" convention used elsewhere.
- `strRange`/`dexRange`/`intRange`/`vitRange` (`int[3]`: minimum, startingMax, hardCap,
  native stat scale - confirmed via the client's own byte-based char-creation protocol
  that STR/DEX/INT/VIT are **not** ×10 scaled the way skills are) - consumed by
  [Leveling & experience](leveling-experience.md)'s attribute-point spending.
- `titles`/`titlesFemale` (string arrays, the per-profession title ladder - see below).

## Assignment

`ProfessionDef.SetProfessionOfChar` -> `ProfessionPlugin.InstallProfessionPlugin`
(`ProfessionPlugin.cs:128-136`) installs (or removes, for a null profession) a
`ProfessionPlugin` on the player. `On_Assign` (`ProfessionPlugin.cs:41-56`) then:

1. If `ProfessionSettings.instance.grantSkillsOnAssign` is true (default), raises every
   skill the `.def` configures a minimum for - **only raises, never lowers** an
   already-higher value. This is a grant, not a reset, so reassigning/testing professions
   on a character with existing progress doesn't destroy it. User's own framing: "it
   should just grant it now - that way once we implement specific skill, it will simply
   start working for player."
2. Always calls `player.RefreshTitle()` (not gated by the setting above - a title change
   has no gameplay side effect worth suppressing).

Real player-facing profession assignment happens through
[Races & nations](races-and-nations.md)'s selection flow, which calls `player.Profession
= profession` as part of `RaceSelection.Finish()`. A GM command (`.profession <name>`,
accepting either the profession's `Name` or `Defname`) also exists as the original
minimum-viable path from before the races flow was built.

## Weapon & armor restriction (#138)

`ProfessionPlugin.On_DenyEquip` (`ProfessionPlugin.cs:94-122`) hooks the pre-existing
`denyEquip` trigger (`ThingManipulationTriggers.cs`'s `TryEquipItemOnChar`) - confirmed
via grep this was a completely unused extension point before this, the first real
content to override it. Mirrors `CheckCancelSkill`'s existing pattern: skipped entirely
for GMs, and gated by a settings toggle
(`ProfessionSettings.enforceWeaponArmorRestrictions`, default on).

Plain `WearableType.Clothing` is **never** restricted, only real armor materials
(Leather/Studded/Bone/Chain/Ring/Plate) - nobody should be blocked from wearing a shirt.

**Localization gotcha hit while adding this**: a brand-new `CompiledLocStringCollection`
field is not usable just by adding the C# field - this engine's resx-based localization
migration (see [Localization](../05-localization.md)) means it resolves to
`<LocalisationEntryNotAvailable>` until a matching `{ClassName}_{FieldName}` entry exists
in **both** `GameText.resx` and `GameText.Czech.resx`. The code compiles fine either way,
so this is easy to miss.

## Content authoring: the other 7 `.def` files

Mage and Thief already existed; Ranger/Priest/Mystic/Necromancer/Craftsman/Shaman/Warrior
were authored from scratch. **Major correction mid-task**: the original plan was the live
Erebor wiki (×10-scaling its 0-100 skill tables) as the sole source - partway through,
the user pointed at the original pre-conversion Sphere source file,
`newbie_portals.scp` (not the already-converted, less-complete
`scripts/converted/newbie_portals.def` copy in this repo). That file's `[profession N]`
blocks (exact skill caps, 0-1000 scale, no conversion math needed) and `zaklad_skilly`
block (exact skill minimums) are authoritative - cross-checking against them confirmed
the wiki-derived numbers were accurate for 8 of 9 professions (only Mystic's `Stealing`
cap differed - the wiki implied a small nonzero cap, the real source zeroes it). STR/
DEX/INT/VIT ranges, weapon/armor allowlists, and spell lists aren't in that legacy file in
readable form, so those stayed wiki-derived, cross-validated against `newbie_portals.def`'s
own `basestats_*` constants (matched on 7/9 professions exactly).

**3 real pre-existing content bugs found and fixed as part of authoring this** (filed as
#159): `mage.def`/`thief.def` were both missing their `Cooking`/`Fishing` starting-value
grant (a systematic conversion-tool bug affecting every profession, not just Mage);
`mage.def`'s `allowedSpells` had a typo (`s_paralyze_field` -> real defname
`s_paralyzation_field`) and two unimplemented spells left active instead of commented out;
`thief.def` granted `Stealing` despite the original source explicitly zeroing it
(consistent with stealing being disabled shard-wide).

**Significant content gaps found, not fixed, filed separately** (same severity class as
the armor-mitigation and NPC-content gaps tracked elsewhere): converted weapon content's
`WeaponType` field is almost entirely a blanket `//guessed by Converter` value - #157.
Necromancer's entire "Black Magic" spell identity is unimplemented in this engine at all
- #158.

## Title ladders (#165/#177/#844)

A character's title reads "The Good/The Evil <name>, <rank> <profession title>", such as "The Good Aragorn, Master Sorcerer".
The rank is `Player.SkillRank`, kept on the `ProfessionPlugin` so it stays when the profession changes, in English: Novice, Expert, Master or Grandmaster, see [Rank quests](rank-quests.md). Erebor's
title had the rank too. A player's title updates when their rank or level changes, and at login.

The ladders are English translations of Erebor's Czech ones, in `scripts/Basic/Professions/Definitions/*.def`
(`titles`/`titlesFemale`). Each entry is quoted: unquoted, the script parser reads it as a name, and a title matching a class,
as "Healer" does, fails when a character of that profession first needs it. A title can't contain a rank word, because it
follows one. `ProfessionTitleLadderTests` checks those rules, and that every step reads differently.

`ProfessionDef.GetTitle(int level, bool female)` (`ProfessionDef.cs:317-324`) looks up a
10-rank title ladder (11 for Craftsman) per profession/gender, ported from the original
source's `titul_<profession>[0-9]`/`titul_<profession>_f` data. The level-to-tier mapping
(`LevelToTitleTier`, `ProfessionDef.cs:329-338`) uses real breakpoints ported from
Erebor's `titulnumber` function - **not** the original (wrong) `Math.Min(level, ladder.
Length-1)` guess, which showed e.g. a level-10 character the ladder's *last* entry
(meant for 200+) because it clamped far too early. `Player.RefreshTitle()` is called
from both profession assignment and every level-up (see
[Leveling & experience](leveling-experience.md)), so the displayed title always reflects
current level + profession + gender.

## A real engine bug found while building this (#166)

A `[SteamFunction("Name")]` command string that collides with an instance property/method
name on the type it's invoked against gets **silently resolved by LScript's member-
resolution path as a property read on `self`**, instead of ever invoking the command
method - no exception, just nothing happening. This specifically hit `[SteamFunction
("Race")]` colliding with the new `Player.Race` property (see
[Races & nations](races-and-nations.md)). Worked around by renaming the command string
(`"Race"` -> `"ChooseRace"`), not fixed at the engine level - see
[LScript](../02-engine-core/lscript.md) and
[Known gotchas](../07-testing/known-gotchas.md) for the general footgun.

## Still open/deferred

- No real player-facing profession-*re*selection flow beyond the GM command (the races
  selection stone only ever runs once per character, per
  [Races & nations](races-and-nations.md)).
- Abilities/talent-trees are untouched by this system entirely - see
  [Abilities & talents](abilities-and-talents.md) for why that's deliberately deferred.
- STR/DEX/INT/VIT ranges are data-only on `ProfessionDef`; the actual spend validation
  lives in [Leveling & experience](leveling-experience.md).
