# Notoriety & murder counts

`Notoriety` (`scripts/Basic/Notoriety.cs`) is a static helper class, not a per-character
component - all state it reads/writes lives as tags/timers on the `Character` itself
(murder count, a criminal-flag timer), everything else is computed live from relationships
(party/guild/realm).

## Criminal flag and murder count

`FlagCriminal` (line 57) attaches a `CriminalExpiryTimer` (a `BoundTimer`, see
[tags-and-timers.md](../02-engine-core/tags-and-timers.md)) for `CriminalFlagSeconds`
(default 120 - the real classic-UO 2-minute window; settable, mirroring
`LightAndWeather.DayLengthSeconds`'s own pattern, specifically so a test can shorten it
without waiting out 2 real minutes). `IsCriminal` is just `HasTimer` on that key - the flag
expires itself for free the moment the timer fires, no explicit "unflag" step needed.
Murderers (`IsMurderer`, `GetMurderCount(self) >= MurdererThreshold` where
`MurdererThreshold = 5`, the classic UO value) never get criminal-flagged on top - already
the worst notoriety tier, flagging would add nothing (line 58).

Both `MurdererThreshold` and `CriminalFlagSeconds` are backed by `NotorietySettings`
(`scripts/Basic/NotorietySettings.cs`) - GM-tunable via the `.settings` dialog's
"Notoriety" category (cross-link [settings-dialog.md](../03-admin-tooling/settings-dialog.md)),
same pattern as `DecaySettings`/`ProfessionSettings`. `Notoriety.MurdererThreshold`/
`CriminalFlagSeconds` are thin pass-through properties onto
`NotorietySettings.instance`'s fields, kept so existing call sites didn't need to change
when `MurdererThreshold` stopped being a compile-time `const`.

`OnHostileAction(victim, enemy)` (line 64) is the entry point that actually flags someone
criminal - called whenever `enemy` commits a hostile act against `victim`. It no-ops unless
both parties are players (**"no AI/monster notoriety yet"**, line 69, a stated scope
boundary, not a bug), and skips flagging entirely if the act was justified: victim already
criminal/murderer (line 71), same party (`Party.AreInOneParty`), same guild, or a declared
guild war between the two guilds (lines 74-84).

`RegisterMurder(victim, killedBy)` (line 89) is the matching death-time entry point -
increments `killedBy`'s murder count tag unless the victim was already
criminal/murderer (killing a criminal isn't a murder). Same player-only scope restriction as
`OnHostileAction`.

## Looting

A player's body has Erebor's limits (#1023, `t_item_in_corpse`, `CorpseLooting.cs`; numbers on `.settings`
Notoriety):

- nothing can be taken in the first 3 s after the death ("Z tohoto tela jeste nemuzes nic brat.");
- one item per 0.8 s ("Ne tak rychle!"; a refused pick starts the wait again);
- each pick holds the looter still for 0.5 s, picks adding up (`i_looter_immobilizer`; walking only, not acting);
- below Grandmaster, someone who died in the last 5 minutes (`i_checkstats_timer`, the same window that spares them
  experience) may not take from another's body ("Jeste nemuzes obirat cizi mrtvoly.").

GMs and monsters' bodies are free. Stricter than Erebor's, which marked only what was on the body at the death and
let a thing go after its first pick: here anything under a player's body counts. Erebor's crime rule there (outside your guild, unless the owner is a murderer) isn't
ported: the user kept #918's below. Taking from a player's corpse (#918) flags the looter criminal when all of this
holds:

- the owner and the looter are of the same realm, and that realm's `.settings` switch is on:
  `gondorLootingIsACrime` (on) or `mordorLootingIsACrime` (off);
- the owner isn't criminal or a murderer;
- the owner isn't in the looter's party with "can loot me" on, see [Parties](parties.md#looting).

This goes beyond Erebor, which ran `LOOTINGISACRIME=0`. Unlike attacking, looting a guildmate or a party member who
hasn't allowed it is a crime. Enemy corpses, your own, monsters' and a GM's taking are never one. It happens on
the real pickup (`AbstractCharacter.On_PickingUp`), so a denied one isn't a crime, and putting the item back
doesn't undo it.

## Realm-based coloring (#163, redesigned 2026-10-16)

`GetHighlightColor(viewer, self)` is what actually drives the in-client nameplate color.
**Redesigned per an explicit user priority order** - the original pass (self-view
colorless, murderer-red checked globally, guild-at-war rendering grey) didn't hold up once
the user actually looked at the logic in detail. Current order, first match wins:

1. **Party** (`Party.AreInOneParty`) -> `HighlightColor.Yellow`. Unchanged from before, and
   now genuinely checked even for self-view (see point 3) - a partied character viewing
   themselves correctly sees their own name as Yellow too, since
   `AreInOneParty(self, self)` is trivially true whenever `self` is in any party at all.
2. **Mordor is always red, unconditionally** - checked here, *before* the criminal check,
   specifically so a Mordor character's criminal-flag status (or lack of it) never
   matters. A Mordor character who's never committed a hostile act in their life still
   renders exactly the same red as one mid-criminal-flag.
3. **Criminal** (`IsCriminal`) -> `HighlightColor.Criminal` (grey). Only reachable for a
   non-Mordor character now, since Mordor already returned at step 2 - in practice this
   means only Gondor (or unaligned) players can ever show this.
4. **Murderer status (`IsMurderer`) is deliberately NOT checked anywhere in this method
   at all anymore.** The previous design rendered *any* murderer red, regardless of realm
   - meaning a Gondor player who racked up a murder count would render visually identical
   to a real Mordor citizen, erasing the realm distinction the user explicitly wants kept
   intact ("gondor could become murderer and pretend he's mordor, we don't want that").
   `Notoriety.IsMurderer`/`GetMurderCount` still exist and still gate other things (e.g.
   `FlagCriminal` still skips an already-murderer character) - they just have zero effect
   on rendered color now. A Gondor murderer falls through this method exactly like an
   ordinary Gondor player: Gondor-blue, or guild-green if they're in a guild, same as
   anyone else of their realm.
5. **Guild**: same guild, or an allied guild -> `HighlightColor.Allied` (green). The old
   "at-war guild -> grey (`Attackable`)" branch is gone entirely, per explicit request - an
   at-war guild member now simply falls through to whatever their realm/default color
   would otherwise be (Gondor-blue, Mordor-red via step 2, or the default grey).
6. **Wild monsters** (`AggroMode.Hostile`) -> `HighlightColor.Enemy` (orange) - unchanged,
   see its own section below.
7. **Gondor** -> `HighlightColor.Innocent` (blue).
8. **Default** -> `HighlightColor.Attackable` (grey) - every NPC/pet/wildlife with no realm
   and no hostile `AggroMode` (a deer, a bird, a tamed pet), any `Realm` value this method
   doesn't otherwise special-case (`None`/`Neutral`/the `MordorOutcast`/`GondorOutcast`
   variants), and the general fallback once nothing else above matched.

**Self-view no longer short-circuits to `NoColor`** - removed entirely, per explicit user
request ("it should still follow the standard logic"). Viewing yourself now runs through
the exact same 8-step chain as viewing anyone else - a Gondor player sees their own name
rendered blue, a partied character sees themselves Yellow, a guild member sees themselves
Green, etc. The one deliberate exception in the *tests* (not the method itself): a
same-guild self-view would trivially satisfy step 5 before ever reaching step 7's realm
check, so the regression test for "self-view follows the realm baseline" uses a dedicated
character with no guild/party to isolate that specific claim - see
`NotorietyTests.TestCheckHighlightColorRedesign`.

Mordor-red (step 2) is still a deliberate, confirmed-with-the-user departure from how the
original Sphere reference (Erebor) did it: Erebor's own source fakes Mordor-red by seeding
every Mordor-aligned character with a starting `kills=30000` to trip the real murderer
threshold (confirmed via `newbie_portals.scp`'s `startnation` function). moriaengine
instead checks `Character.Realm` directly as an independent rendering layer, leaving the
real murder-count mechanic completely disconnected from rendering now (point 4 above).

`GetCharRelation(a, b)` is a **separate** method for the non-visual "are these two hostile"
question used by area-spell targeting (see [magery-spells.md](magery-spells.md)) - it was
NOT part of this redesign and still uses its own older realm/guild logic (opposing realms
are `AlwaysHostile`, same realm is `Allied`, guild relations checked first). Don't assume
a change to `GetHighlightColor`'s priority order also changed `GetCharRelation` - they're
intentionally independent, answering different questions.

`Realm` itself, and race-to-realm/nation assignment, are covered in
[races-and-nations.md](races-and-nations.md).

## Wild monsters render orange (user-requested, not real OSI behavior)

Real UO has no dedicated "monster" notoriety color - hostile NPCs render via the exact
same blue/gray/orange/red logic as players. This engine deliberately deviates: a wild
monster (`NPC.AggroMode.Hostile` - "attacks any valid player regardless of
realm/notoriety," see `NPC.ShouldAggroOn`/`NPC.cs`) renders `HighlightColor.Enemy`
(orange), checked in `GetHighlightColor` right before the realm switch above. Reusing
`AggroMode.Hostile` as the signal avoids inventing a new flag - it's already, almost by
definition, the one NPC category with no realm/guild side to render by, and without this
check a wild-monster encounter would otherwise fall through to `NoColor` (no realm set)
rather than looking visually distinct from a Mordor player's red. `AggroMode.GuardsRealm`
NPCs (town guards) are untouched by this - they DO have a `Realm` set, so the existing
realm switch already colors them correctly as their own side's blue/red.

**`AggroMode` is per-NPC-instance, not per-species (#260)** - `NPC.AggroMode` is a
tag-backed instance property, seeded from `NPCDef.AggroMode` (the species-level default
from `.def` content) once at creation, then lives independently from there on. Both
`ShouldAggroOn` and this notoriety check read the instance value. This exists so a GM
can view and fix one specific spawned monster's aggro behavior via `.info` (it's a plain
public property on `NPC`, which `.info`'s reflection already surfaces as an editable
enum field) without needing to re-author that monster's whole species `.def` content -
real motivating case: `c_titan_mage`/`c_titan_lord` have no `AggroMode=` line at all and
default to `Calm`, unlike plain `c_titan`. `NPCDef.AggroMode` itself is untouched and
still drives what a *newly spawned* NPC of that type starts with.

GM-tunable: `NotorietySettings.wildMonstersRenderOrange` (default on) - a GM who wants
strict OSI-accurate notoriety instead can turn this off via the `.settings` dialog's
"Notoriety" category, falling back to the real-UO-matching `NoColor` behavior.

## Two separate color mechanisms: health bar vs. name text

A real, user-reported bug (2026-10-16): the health-bar/status overlay was already
correctly blue/red for Gondor/Mordor, but the separate floating overhead NAME text (the
response to a single click) stayed a generic uncolored gray. These are genuinely two
different client-rendered things driven by two different packets:

- **Health bar** - `DrawObjectOutPacket`'s highlight byte, set via
  `AbstractCharacter.GetHighlightColorFor`/`Character.GetHighlightColorFor` ->
  `Notoriety.GetHighlightColor`. The client derives the bar's actual color client-side
  from this byte automatically - this part was already correct.
- **Overhead name text** - an ordinary speech-type message (`UnicodeSpeechOutPacket`
  0xAE for `On_Click`, or `ClilocMessageOutPacket` 0xC1 for `On_AosClick`, depending on
  `Globals.UseAosToolTips`/client version) with its own explicit hue field. The client
  does **not** derive this hue from notoriety itself - the server has to set it
  explicitly, same as a real OSI server does (confirmed against ServUO's own
  `Mobile.OnAosSingleClick`, which computes `Notoriety.GetHue(Notoriety.Compute(...))` for
  exactly this field). `Thing.On_Click`/`On_AosClick`'s base implementation always hardcoded
  hue 0 (correct for a plain item, wrong for a character) - `Character.On_Click`/
  `On_AosClick` now override this, calling `Notoriety.GetNameHue(this.GetHighlightColorFor(clicker))`.

`Notoriety.GetNameHue(HighlightColor)` is a small lookup table (`nameHues`, matching
ServUO's own `m_Hues` index-for-index against this engine's `HighlightColor` enum - both
run `NoColor=0` through `Invulnerable=7`): `0x0059` blue/Innocent, `0x003F` green/Allied,
`0x03B2` gray/Attackable-or-Criminal, `0x0090` orange/Enemy, `0x0022` red/Murderer,
`0x0035` yellow/Invulnerable. `Corpse.On_Click`/`On_AosClick` (`scripts/Basic/ItemClasses/
Corpse.cs`) had the identical gap (a long-standing `//TODO notoriety hue stuff` comment)
- fixed the same way, deriving the hue from `Corpse.Owner`'s notoriety color as seen by
the clicker (null `Owner` - an NPC's corpse - has nothing to color by, same as a plain item).

## Region-based combat/exp-loss exemptions

Two related but deliberately different-scoped checks:

- `IsAttackBlockedByRegion(attacker, target)` - true if either party stands in a `Flag_Safe`
  region, or, for a player attacking a player, in a `Flag_NoPvp` region (Sphere's NO_PVP),
  UNLESS that region is also `Flag_Arena` (arenas override both). Used to bar combat from even
  starting. #1019, the user's call: a `Flag_Guarded` region bars nothing, as on Erebor, where
  the guards punish the attacker instead. No-PvP used to bar fighting monsters too, so nobody
  could fight in the goblin labyrinths (32 no-PvP areas). On the archive map no region is
  guarded without also being safe (the jail, the cage, the labour camp). It covers only a
  player's attack request; the damage itself isn't stopped (below).
- The damage itself (#1347): safe and no-PvP areas don't stop it, as on Erebor. Sphere 0.99z8's engine has a
  damage rule for them (EreborNext `CChar.Damage`: SAFE takes all but god damage, NO_PVP a player's on a player), but
  Erebor never used it: every player and creature carries `e_gethit`, whose `@GetHit` cancels the engine's hit and
  applies it through `damage_final`/`dmgHits` as god damage, which that rule lets through. Erebor's real refuges use the
  `r_SafeZone` region event instead (invulnerable on entering, `RegionEvents.cs`). What the engine's safe flag does
  keep, from Sphere: an NPC doesn't pick, or keep, a target standing in a safe area (`NPC_GetAttackMotivation`,
  `NPCAiTimer.FindTarget`/`IsValidTarget`), and an NPC standing in one doesn't cast (`NPC_FightMayCast`,
  `NPCAiTimer.TryCastSpell`, beneficial spells too). A living player walking or teleporting across the edge gets
  Sphere's lines (`Character.On_RegionChanged`, not at login): "You are safe from other players here." / "You lose
  your safety from other players." and "You have a feeling of complete safety" / "You lose your feeling of safety".
  Sphere's stealing refusals in these areas aren't ported: stealing is off on Erebor.
- `IsExemptFromExperienceLoss(self)` - a **narrower** set of flags
  (`Flag_Safe`/`Flag_Arena` only, deliberately excluding `Flag_Guarded`/`Flag_NoPvp`) used by
  the Leveling system's death-experience-loss mechanic (see
  [leveling-experience.md](leveling-experience.md)/#128). The user's own stated design: a
  guarded town shouldn't grant exp-loss immunity, only true safe zones and arenas should.

Don't conflate these two - a region that blocks combat entirely is not automatically exempt
from the (unrelated) death-experience-loss penalty, and vice versa.

## Known real-world ordering gotcha: `CauseDeath`/`Flag_Dead`

Not part of `Notoriety.cs` itself, but directly relevant to anything that calls
`RegisterMurder`/checks notoriety around a death: `Character.CauseDeath` is a no-op once
`Flag_Dead` is already true, and for a non-player character, death deletes the `Thing`
**synchronously**, inside the same call that reduced HP below 1 (see
[combat.md](combat.md)'s `DamageManager.CauseDamage`/`IsDeleted` guard, and
[corpses-and-death.md](corpses-and-death.md)). Two concrete traps this has caused in test
code (not production bugs, but worth knowing before writing anything that kills a character
and immediately inspects notoriety/skill-sequence state):

1. Killing the same character twice without resurrecting between calls silently does
   nothing the second time - a test (or script) that assumes a second `CauseDeath` call
   re-triggers everything will be wrong.
2. Code re-entering a weapon-skill or spell sequence against a target that a killing blow
   just deleted (same tick, before the caller's own stack unwinds) must check `IsDeleted`
   before touching the target further - see [combat.md](combat.md)'s re-entrancy guard for
   the concrete instance of this.

## Known CI-only flake (#121)

`OwnerAggroTests.HostileFollowerNeverAutonomouslyTargetsItsCommander` fails reproducibly on
CI only (never locally) - a titan-class NPC occasionally acquires its own commander as a
combat target, which `DamageManager.CauseDamage`'s owner/commander backstop (see
[combat.md](combat.md)) should prevent. Root cause not yet confirmed; a draft diagnostic PR
(#119, not merged) exists with instrumentation ready to go once CI capacity allows
re-running it. Treat a failure of this specific test on CI as this known flake first, not
evidence of a fresh regression, until #121 is actually root-caused.
