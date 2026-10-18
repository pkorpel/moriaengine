# Design decisions log

Durable "why it works this way" reasoning distilled from session history (the repo-root
`CLAUDE.md`), organized by system rather than chronologically. `CLAUDE.md` itself is a
session-continuity log, not a reference document - it will keep accumulating new,
unrelated entries over time. This page exists so the *permanent* reasoning behind
already-shipped decisions survives independent of that log's own churn. When a decision
here is superseded by later work, update this page in the same change (see the root
`CLAUDE.md`'s workflow section) rather than leaving two conflicting "why" stories.

## Item decay: per-item timers over a periodic sweep

**Decision**: the decay system schedules one `BoundTimer` per eligible item/corpse
(`ItemDecaySystem.ScheduleOrCancel`), rather than a single recurring sweep scanning every
`Thing` each tick.

**Why**: explicit user request, specifically citing engine performance as a first-class
goal ("we are aiming for full performant game, not just alfa/beta testing") - a sweep's
cost scales with total world item count every tick regardless of how many items are
actually decaying; the per-item approach costs nothing beyond the currently-eligible
count. It also happened to simplify persistence - a scheduled `BoundTimer` is "just a
tag" (see [Tags & timers](../02-engine-core/tags-and-timers.md)) and needs zero special
save/load code, whereas the original sweep design would have needed its own explicit
save-time bookkeeping to track "how long has this item already been sitting here."

See [Item decay](../04-gameplay-systems/item-decay.md).

## Non-corpse item decay defaults ON, not off (#371, supersedes the original #219 policy)

**Decision**: a non-corpse item loose on the ground is decay-eligible by default -
`attr_decay` no longer gates anything in `ItemDecaySystem.GetDecayMinutesIfEligible`.
Exempt: `attr_questitem`, `attr_lockeddown`, `attr_secure`, and a `Flag_NoDecay` region.

**Why**: the original #219 pass deliberately chose decay-OFF-by-default, matching this
engine's general "incomplete config defaults permissive" convention - but real
gameplay testing showed this meant nothing dropped by a player, and no non-corpse loot,
ever decayed at all, since nothing anywhere ever sets `attr_decay`. Real Sphere's own
model is the opposite: a global `DECAYTIMER` ini setting decays virtually everything
loose by default, and `attr_decay` is only ever used by authored content as a
forcing/override flag on specific items, never as the master on/off switch. User
explicitly confirmed flipping to match that real default. `attr_lockeddown`/
`attr_secure` were added as new exemptions alongside the flip (not present in #219) -
without them, a player's intentionally-secured house belongings sitting loose on a
house floor (not in a container) would have started vanishing the moment this shipped,
which would have been a severe regression, not the intended behavior.

See [Item decay](../04-gameplay-systems/item-decay.md).

## Three separate decay rates, not one generic interval

**Decision**: `DecaySettings` has `playerCorpseDecayMinutes`/`npcCorpseDecayMinutes`/
`itemDecayMinutes` as three independent GM-tunable values, not one shared interval.

**Why**: explicit user request - "a player's corpse, an NPC's corpse, and a loose
attr_decay item are three different real-world defaults," not variations on one concept.

## GM-togglable flags get a dedicated dialog, not reflection-surfaced booleans

**Decision**: when a feature needs many independent GM-toggle flags (the 21 ATTR flags),
build a dedicated checkbox dialog (`D_AttrList`, modeled on `D_TagList`/`D_PluginList`),
not individual boolean properties auto-surfaced through the generic `.info` reflection
view.

**Why**: the first draft of the ATTR flags system did exactly the opposite (22 scattered
boolean properties), and the user's explicit correction established this as a standing
preference: "I like the idea of having entire separate dialog (just as we have timers,
plugins, tags) with all the possible attributes and checkbox next to them." Apply this
preference by default for any future "many togglable things" GM tooling need, unless
told otherwise. See
[Item ATTR flags](../04-gameplay-systems/item-attr-flags.md#d_attrlist---the-dedicated-gm-dialog).

## Realm-based notoriety: a clean override, not Erebor's own hack

**Decision**: `Notoriety.GetHighlightColor` consults `Character.Realm` directly
(Gondor->blue, Mordor->red), layered *under* the existing murderer/criminal escalation.

**Why**: the reference Sphere implementation (Erebor) achieves the same visual effect by
giving every Mordor character a starting `kills=30000` to artificially trip the murder-
counter threshold - a hack that would have corrupted this engine's own, separate murder-
tracking mechanic. The user's explicit decision was a real, additive `Realm` check
instead, confirmed during the Races design pass (#161 decision #3). **General lesson**:
a faithful Sphere-behavior port is not always the same thing as a faithful Sphere-*code*
port - prefer the clean mechanism when the reference implementation's own approach would
collide with something this engine does differently.

## Race has zero mechanical effect; profession is where stats/skills attach

**Decision**: a character's `Race` never affects stats, skill caps, or any other
mechanical value. All of that comes from `Profession` alone.

**Why**: explicit, repeated user decision (#161 decision #2) - the original Sphere
source's own point-buy stats dialog never references race at all, only the chosen
profession's range; the wiki's prose vaguely gestures at racial traits that don't
actually exist anywhere in the executable source. Confirmed again, independently, for
profession/race *selection* order (profession is chosen before race in the creation
flow) and for weapon/armor restrictions (profession-gated, never race-gated).

## Abilities/talent-trees deliberately untouched by Professions and Races

**Decision**: neither the Professions pass (#134) nor the Races pass (#161) touches
ability/talent-tree content at all, including not finishing Mage's/Thief's existing
half-built trees.

**Why**: explicit, repeated user instruction across both passes - a possible future
ability-tree *redesign* is under consideration, and the user didn't want new work built
on top of the current half-done shape in the meantime. See
[Abilities & talents](../04-gameplay-systems/abilities-and-talents.md).

## Death's experience loss: user's own design, deliberately diverging from Erebor

**Decision**: on death, a player's experience-loss percentage and the killer's
cut-of-the-loss percentage are both independent, GM-tunable settings (default 100%/10%),
rather than Erebor's actual ~5%-capped-loss mechanic.

**Why**: explicit user design choice during the Leveling system's design pass (#125) -
at the chosen defaults, a dying player loses everything and the killer gets 10% of what
was lost, intentionally simpler and harsher than the reference shard's own mechanic.

## House lockdown: two separate restriction strengths, matching real UO

**Decision**: `attr_lockeddown` restricts movement only; `attr_secure` restricts
movement *and* restricts who can take items out of the container (if it is one).

**Why**: matches real UO's own distinction between the two mechanics - a friend can use
a house and see what's inside a secure container, but only the owner/co-owners can move
either kind of protected item, and only owner/co-owners/friends (not arbitrary visitors)
can take something out of a secure container. See
[House lockdown & security](../04-gameplay-systems/house-lockdown.md).

## `attr_opened` deliberately not implemented

**Decision**: closed not-planned after investigation, rather than built.

**Why**: `Door.IsOpen` is already fully derived from the door's own `Model` value with no
separate stored state - adding a parallel flag would either duplicate working logic for
no benefit, or create a mirror value that could drift out of sync with the real state.
**General lesson, worth reapplying**: when a reference spec's flag would duplicate state
this engine already derives correctly and cheaply, the flag is a liability, not free
compatibility - `attr_norepair` (no repair system exists to attach to) is the same
pattern from a different angle. See
[Doors](../04-gameplay-systems/doors.md#attr_opened---investigated-closed-not-planned-225).

## Localization: direct `Language`-keyed lookup, not `ResourceManager`+`CultureInfo`

**Decision**: `GameTextResources` looks up two plain embedded resx resources directly by
the `Language` enum, not via `ResourceManager.GetString(key, CultureInfo)` with
culture-named satellite resx files.

**Why**: this sandbox's `dotnet` CLI needs `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` to
run at all (no ICU), which made MSBuild's culture-splitting behavior for
`*.cs-CZ.resx`-named files unreliable to validate locally - and there was never a real
need for `CultureInfo`'s general multi-culture machinery anyway, since this server has
exactly two known languages. See [Localization](../05-localization.md).

## Issue-driven workflow: raise first, comment while working, close with the PR

**Decision**: every bug or feature gets a GitHub issue filed *before* work starts, with
comments posted as findings/decisions land during the work, closed only at the end
referencing the shipping PR.

**Why**: explicit standing user instruction - issues should capture the *reasoning trail*
as it happens, not just a terse "found and fixed" summary after the fact. This is also
why so much of this very design-decisions log exists: the reasoning was already being
captured in issue comments and `CLAUDE.md`, this page just consolidates it into a
reference someone can read without archaeology through dozens of closed issues.
