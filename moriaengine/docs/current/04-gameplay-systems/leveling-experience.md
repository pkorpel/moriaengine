# Leveling & experience

Core level/experience system (#125), built in 5 phases (#126-#130, all shipped). The one
shared entry point every other system (monster kills, death, future quests) should use to
grant or remove experience is `LevelingSystem.AddExperience`
(`scripts/Basic/Leveling/LevelingSystem.cs:63-88`) - keeping the level-up formula and
overflow-carry logic in one place instead of duplicated at every caller.

## Threshold formula and level-up

Level `N` requires `N * nextLevelCoefficient` total experience to reach (default
coefficient **330** - Erebor's own real constant, `nextlevel=330` in its
`experiences.scp`, not an invented number). The level-up check is a `while` loop, not a
single `if`, so one large experience gain can cross several level thresholds in a row,
carrying the leftover experience forward each time rather than clamping it to zero (e.g.
a level-1 character sitting at 600 experience who gains 100 ends up at level 2 with 40
experience left over, not 0). There is no level cap.

Each level-up inside that loop:
- Grants `+5` unspent skill points and `+2` unspent ability points (flat, uncapped).
- Sends a system message and plays a level-up effect/sound (`0x375A` stationary effect +
  `SoundNames.DrumSuccess` - a best-effort aesthetic choice; no canonical "level up"
  effect exists in either this codebase or the Erebor reference to port).
- Calls `player.RefreshTitle()` since a profession's title-ladder tier may have changed -
  see [Professions](professions.md).

`Player.Level`/`Player.Experience` existed before this system as pure unwired TODO
stubs (fields + persistence only) - this system is what actually wires them up.

## `.explevel` - the read-only player dialog

`.explevel` (`scripts/Basic/dialogs/explevel.scp`) already existed in some form before
this system (a pre-existing dialog this session's own earlier investigation initially
missed - see [Known gotchas](../07-testing/known-gotchas.md) for the "grep missed a whole
folder" lesson that recurred a few times this session). It shows Level/Experience plus
combat stats, and was extended with Attributes/Skills/Abilities spend buttons (see below)
and Profession display (see [Professions](professions.md)).

## Three point pools

Every level-up grants points into three independent pools, all stored on `Player`:

| Pool | Granted | Spent via |
|---|---|---|
| Unspent skill points | flat +5/level | `LevelingSystem.TrySpendSkillPoints` |
| Unspent ability points | flat +2/level | `LevelingSystem.TrySpendAbilityPoints` |
| Attribute point budget | tiered formula (below), computed, not stored | `LevelingSystem.TrySpendAttributePoints` |

Earning is profession-agnostic - every class gains points the same way; only *spending*
differs per profession (skill caps, attribute ranges).

### Attribute point budget

`Player.AttributePointBudget` is **computed**, not a stored running total:
`300 (flat base) + LevelingSystem.LevelStatBonus(Level)`, minus whatever's already been
allocated into `StrPointsAllocated`/`DexPointsAllocated`/`IntPointsAllocated`/
`VitPointsAllocated`. `LevelStatBonus` (`LevelingSystem.cs:95-106`) is a tiered formula
ported from and verified against Erebor's own `f_levelstatbonus`:

| Level range | Bonus |
|---|---|
| 1-9 | `level * 4` |
| 10-29 | `40 + (level-10) * 3` |
| 30-39 | `100 + (level-30) * 2` |
| 40+ | flat `120` (level 40 is the *last* level that grants any further attribute points) |

### Spending

Spending follows Erebor's `d_levelbonus` (#882). `LevelingSystem.RaiseSkill` is one click on a
skill: it rounds the skill to the nearest 5.0 (never under the profession's start value), then
raises it by 5.0 (`SkillPointGain`), which costs:

| Skill value | Cost |
|---|---|
| under 50.0 | 1 point |
| 50.0 to 74.9 | 2 points |
| 75.0 and up | 3 points |

It refuses, with Erebor's message, past the profession's cap, at exactly 25.0, 50.0 and 75.0
(those steps come from a [skill teacher](npc-speech.md) for gold, which needs the rank from
the [rank quests](quests/README.md)), without enough points, and under a stat spell. Only
`SpendableSkills`, Erebor's `combat_skills`, can be raised this way. Crafting and gathering are
trained by use.

Death runs `TrimSkillsAtDeath` (`f_srazitskilly` + `roundcombatskills`): every skill is put back
inside the profession's limits, and the spendable ones are rounded to 5.0.

`TrySpendAttributePoints` needs a profession and raises a stat within both
`AttributePointBudget` and the profession's per-stat hard cap (`GetStatRange`'s third element,
Erebor's `nastaveni_povolani_maxstats`). A stat spell refuses it too.

## Starting a character

Erebor's `startclass`, in `RaceSelection.Finish` -> `LevelingSystem.StartNewLife`: level 0, 0
experience, 0 skill points, 0 ability points, all skills exactly at the profession's start
values. The stats dialog takes exactly 300 points within each profession's start range (the
second element of `GetStatRange`), and those count against the budget, so 0 stat points are
left. The first points arrive at level 1: 5 skill, 2 ability, 4 stat. Up to level 40, a
character gets 120 stat points in all.

Characters created before #882 had the 300 uncounted, so 300 free points; `FixUncountedStartingStats`
counts their current stats at login.

Not ported: Erebor's points are an `i_dovedbod` item in the pack, here a counter. `explevel` also
puts skill 33 (Stealing) back to its start value on every opening, which would wipe points spent
on it, so that line is left out.

`TrySpendAbilityPoints` (`LevelingSystem.cs:178-186`) has no profession-based gating at
all yet - it just spends the existing `Character.ModifyAbilityPoints` mechanism. See
[Abilities & talents](abilities-and-talents.md) for why that's still a deliberately
incomplete area.

No real player-facing spend *dialog* exists yet for any of the three pools beyond the
`.explevel` buttons wired directly to these methods - a fuller dialog redesign is a known
open item, not started.

## Who gets experience from a kill

Port of Erebor's `@death` (`sphereevents.scp`, #902). `FightMemory` remembers everyone who fought a
character; `LevelingSystem.FindFighters` takes those whose last blow is within `fightCreditSeconds` (120),
within `fightCreditRange` (20) tiles and on the victim's plane, with the killer. Pets and summons don't count.

**Monsters.** `tag.experience` becomes `ExperienceReward`, rolled at creation: what one fighter gets alone. With
`N` fighters the pool is `reward x sqrt_int(N) / 100` and each gets `pool / N`:

| Fighters | Each | Together |
| --- | --- | --- |
| 1 | 100% | 100% |
| 2 | 70% | 141% |
| 3 | 57% | 173% |
| 4 | 50% | 200% |

In areas of `tag.realm` below 3, a monster pays each fighter at most `monsterExpCap` (100), except elites; 0
turns the cap off. A summoned monster pays nothing. NPCs who fought count towards `N`, and never level.

**Parties** go beyond Erebor, which didn't use them here: everyone alive in a fighter's party, in range,
counts as a fighter of the monster. That's how a healer in a group gets experience. It doesn't apply to kills
of players.

## Death

`ApplyDeathExperienceLoss` follows Erebor's `f_ztratitfame`, replacing the design of #128. Dying costs
`deathExpLossPercent` (5) of what the next level takes, at most what the player has. Nothing is lost if nobody
fought them, within 5 minutes of a previous death, or in a safe zone or arena.

`deathExpPaidPercent` (100) of it goes to those who fought, shared evenly: nothing for one of the victim's own
realm, or for killing a craftsman. The level never goes down.

## Gates and quests - explicitly out of scope

Experience-gated content (quests, milestone gates) is deliberately not built - the user's
own framing was "just leave us a function which handles adding experiences... called from
different places," which `AddExperience` already satisfies. Actual gate/quest content is
deferred indefinitely, tracked only so the idea isn't lost (#130).

## Real bugs found while building this (all fixed)

- **`Character.CauseDeath` is a no-op once already dead** - not itself a bug, but caused
  a test to silently "pass" for the wrong reason when it re-killed a victim without
  resurrecting first. See [Corpses & death](corpses-and-death.md) and
  [Known gotchas](../07-testing/known-gotchas.md).
- **Lazy `.def` expression fields evaluated synchronously inside a chat command's own
  call stack get authorization-checked as if they were commands** - `OpNode_Lazy_
  Expression.Run` calls `Commands.AuthorizeCommandThrow` for first-access lazy-expression
  resolution. Can't happen in real gameplay (real monster death is always async via
  combat timers); only surfaced via test code calling `CauseDeath` directly from a command
  handler. Worked around in tests via a near-immediate `BoundTimer` deferring the kill.
- **`.explevel` was completely broken for every real player except the server's literal
  first-ever account - a genuine production bug.** The dialog's own first lines called
  `InvalidateCombatWeaponValues()`/`InvalidateCombatArmorValues()` bare, and both names
  were missing from `CommandsAuthorization.cs`'s allowlist - silently denied for every
  account except the one account `AbstractAccount.HandleLoginAttempt` auto-elevates
  (`accounts.Count == 1` at creation, the very first account ever). Invisible all session
  because every test so far happened to use that one lucky account. Fixed by adding both
  names to the allowlist at `PLEVEL_PLAYER`.

See [Known gotchas](../07-testing/known-gotchas.md) for the fuller test-infrastructure
lessons from this pass (the 5-character-account-slot cap, the `ScriptArgs` dual/triple
shape problem, the account-sharing interactions between unrelated Facts).
