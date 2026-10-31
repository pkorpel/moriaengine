# Races & nations

6 races and 12 nations (#161, sub-issues #162-#165), the third "basic character creation"
pillar alongside [Leveling & experience](leveling-experience.md) and
[Professions](professions.md). Source: the live Erebor wiki cross-referenced against the
full pre-conversion Sphere source (`newbie_portals.scp`) - the wiki's thin overview page
only clearly documents 5 races (it conflates Orc/Uruk-hai); the source data is
authoritative and was used instead wherever the two disagreed.

## Data model

`Race` (`scripts/Basic/Races/Race.cs`) - 6 values: Human, Elf, Dwarf, Hobbit, Orc,
Uruk-hai. `RaceData` maps each race to the nation(s) it may settle in
(`GetAvailableNations`) and a display name. **Race has zero mechanical stat effect** - a
deliberate, explicit user decision (#161 decision #2): the source's own point-buy dialog
only ever bounds by the chosen *profession's* range, never race, despite the wiki's prose
vaguely gesturing at racial traits.

`Nation` (`scripts/Basic/Races/Nation.cs`) - 12 values. `NationData` maps each nation to
exactly one `Realm` (`Gondor`/`Mordor` - drives notoriety, see below) and one home
coordinate, ported verbatim from the original source's `home_<nation>`/`nation_realm`
constants.

| Race | Nation choice |
|---|---|
| Human | 7 nations (real choice) |
| Elf | 2 nations (real choice) |
| Orc | 2 nations (real choice) |
| Dwarf | locked to Ereboran |
| Hobbit | locked to Pulcik |
| Uruk-hai | locked to Mordoran |

`RaceData.HasNationChoice(Race)` is simply "does this race's available-nations array have
more than one entry" - races locked to one nation skip the nation-picker gump entirely.

`Player` gained tag-backed `Race`/`Nation`/`Home` properties - deliberately **not**
`.ct`-declared, avoiding the `*.Generated.cs` regeneration pipeline for a simple
persisted value (see
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md)) - the
same choice already made for the pre-existing `Character.Realm`.

## Selection flow

`RaceSelection` (`scripts/Basic/Races/RaceSelection.cs`) drives a chained sequence of
gumps, matching Erebor's own `D_RACEclass_*` order exactly: **profession picker -> race
picker -> nation picker (skipped for the 3 locked races) -> stats point-buy -> finish**.

Triggered by double-clicking `i_stone_raceclass` ("Stone of the Beginning", a converted
content stub that previously had no trigger at all) via `D_RaceClassEntry`
(`RaceSelection.cs:138-155`), a `D_OptionPicker` subclass that builds its own args
internally since a bare LScript `Dialog()` call can't construct a closure. If the player
has already chosen a race, it shows an empty "Already chosen" gump instead of restarting
the flow (a `CompiledGumpDef` can't suppress rendering entirely from `Construct`).

Each picker step (`ShowRacePicker`/`ShowNationPicker`/`ShowStatsPicker`,
`RaceSelection.cs:48-77`) is a reusable `D_OptionPicker` gump (see
[Dialog framework](../03-admin-tooling/dialogs-framework.md)) - a generic "pick one of N
labeled options" dialog, rather than near-identical one-off gump classes per step. The
final step, `D_RaceClassStats`, is a real interactive point-buy dialog (sum must equal
300, each stat bounded by the chosen profession's own range) - not an auto-allocation.

**Only `Finish()` (`RaceSelection.cs:81-90`) commits anything** to the player - every
step before it only threads the in-progress choice through callbacks, so closing the gump
partway through (the cross button) leaves the player completely untouched. `Finish`:

```csharp
player.Race = race;
player.Profession = profession; //triggers ProfessionPlugin.On_Assign
player.Nation = nation; //cascades Realm + Home
player.P(player.Home); //one-time placement, NOT a reusable recall
GiveStartingEquipment(player, profession);
```

## Entering the world (#881)

`RaceSelection.Finish` follows Erebor's `startclass`. `ClearForNewLife` is `promazat`: worn items, the backpack,
the bank and anything held are gone. Then 2000 gold and the profession's `tm_<class>newbie` kit: clothing and
armor go on, weapons and shields into the pack, and what's worn gets the class color.

## Starting equipment

`GiveStartingEquipment` (`RaceSelection.cs:119-129`) grants 2000 starting gold plus the
profession's own newbie-equipment kit (`tm_<suffix>newbie` TemplateDef), matching
Erebor's own `startclass` function. The profession-name-to-template-suffix mapping is an
explicit table (`professionToTemplateSuffix`, `RaceSelection.cs:97-107`), not a guessed
string transform - the two naming schemes don't match 1:1 ("warrior" -> "war",
"craftsman" -> "craft", "mystic" -> "Mystik").

**Real content bugs found and fixed while wiring this up** (5 of 9 profession templates
needed it): the auto-converted content had silently dropped items it couldn't resolve -
Craftsman's template was missing every craft tool entirely; Warrior/Craftsman were each
missing 2 of their 3 weapons; Priest was missing a weapon; Ranger's crossbow referenced a
typo'd nonexistent defname; 4 templates used an unsupported `random_pants` Sphere macro.

**Deliberately not ported**: a recall rune (recall is a separate, not-yet-designed
feature, see "Home" below) and a decorative per-class "soul crystal" item that was never
converted into this engine's content at all.

## Realm-based notoriety (#163)

`Notoriety.GetHighlightColor`/`GetCharRelation` consult `Character.Realm` directly:
Gondor -> blue/Innocent, Mordor -> red/`HighlightColor.Murderer` (used purely as a
rendering choice, decoupled from the real murder-count mechanic) - layered **under** the
existing murderer/criminal escalation, not replacing it. This is a clean override,
deliberately **not** Erebor's own hack of giving every Mordor character a starting
`kills=30000` to trip the murder-counter threshold artificially. See
[Notoriety & murder counts](notoriety.md) for the general mechanism.

## Home (#164) - static placement only, no recall

`Player.Home` is set once at `Finish()` and used for exactly one placement at that
moment. **Explicit scope correction from the user**: "home is basic home location which
doesn't change for recall - for that I was thinking of different functionality, lets
leave recall away from this." This page deliberately does not build Erebor's own
`gohome`-on-portal-step recall mechanism, or any other recall trigger - that's a
separate, not-yet-designed feature.

## Title ladders

Per-profession title ladders (#165) are profession-owned data and behavior, not a Races
concern - see [Professions](professions.md#title-ladders-165177).

## A real engine bug found while building this (#166)

See [Professions](professions.md#a-real-engine-bug-found-while-building-this-166) for the
full writeup - `[SteamFunction("Race")]` silently colliding with the new `Player.Race`
property via LScript's own member-resolution order is the concrete bug that surfaced
here, worked around by renaming the command rather than fixed at the engine level.

## Still open/deferred

- No physical selection-stone *editing* UI - the stone's trigger is fixed, not
  reconfigurable.
- The LScript command/property name-shadowing footgun (#166) is unfixed at the engine
  level, just sidestepped for this one collision.
