# Elite monsters (#794)

Ported from Erebor's `systems/events_npc/EliteNPC/EliteNPC.scp` (pkorpel/erebor-sphere-archive).
Code: `scripts/Basic/Loot/EliteMonster.cs`; settings: `EliteSettings` (`.settings` -> "Elite Monsters").

## When a monster turns elite

Checked once, by creaturestart ([Monster loot](monster-loot.md), part 2), outside dungeons and
before the gold - Erebor's `f_spawnEliteForm`:

- it was made by a **spawn gem** (its `spawnGem` tag), and the gem has no `denyElite` tag -
  a GM's `.add` never makes an elite;
- it's worth at least **1000 experience** (Sphere's x10 fixed point - 42 chardefs qualify,
  dragons, mithril golems, high daemons...);
- it isn't a **boss** - Erebor's `tag(isBoss,1)` is converted to `IsBoss = 1` (28 chardefs);
- a roll: `experience × 20 / 100` per 10000, at most 500 (2% at 1000 experience, 5% from 2500).

## What changes - `EliteMonster.Transform`

**Power** = 500 + (experience - 1000) × 2667 / 10000, at most 900 (permille). Then:

| | |
|---|---|
| Str / Dex / Int | +25% |
| its swing damage | +20%; + power/3 % more vs summoned creatures |
| a player's swing at it | the elite absorbs power permille (50-90%); message "Your target absorbed most of your attack." |
| regeneration | power/7 hit points every 2 s while hurt ("is regenerating.") |
| resists | poison at least 700 and at least the power; summon at least 970 |
| experience | × (300 + (power - 500)/2) % - 300-500% - so its **gold** grows too (creaturestart runs after) |
| loot | 2× high reagents (power/10), 2× any reagents (power/20), gems (power/50), diamonds (power/100); `(power-400)/1000` chance of a magic item - a PLACEHOLDER like the creaturestart one |
| show | sound 254, blue sparkles around it |

**Level bonus**: Erebor let players from level 200 up absorb less (200 + 5 per level above 200,
up to level 400, permille off the power). Our levels don't go that high (about 60 is the
effective max), so as shipped nobody gets it - the thresholds are settings.

## Leaving - `EliteEscapeTimer`

The first time an elite fights a player it says *"Ha! An enemy! Are you strong enough to defeat
me?"* and a 20-minute countdown starts. Then it casts a gate (*"Beleg Foroth Kel"*, 3 s, frozen:
NPCAiTimer skips it); a landed hit interrupts the spell (fizzle) and it retries 5 s later. Once
cast: *"You are no match for me!"*, a gate effect, and 2 s later it's gone - deleted, no corpse,
no loot; its spawn gem respawns normally. So an elite can't be camped forever.

Not ported: Erebor's `enticement = 250` (no equivalent here).

## Settings: `.settings` -> "Elite Monsters" (`EliteSettings`)

`enabled`, `minExperience` (1000), `spawnChancePercentOfExperience` (20), `maxSpawnChance` (500),
`basePower` (500), `powerPer10000Exp` (2667), `maxPower` (900), `statBonusPercent` (25),
`swingDamagePermille` (1200), `regenPowerDivisor` (7), `regenIntervalSeconds` (2),
`summonKillerPowerDivisor` (3), `minResistPoison` (700), `minResistSummon` (970),
`expBonusBasePercent` (300), `lootBundle`, `escapeAfterSeconds` (1200), `escapeCastSeconds` (3),
`escapeRetrySeconds` (5), `levelBonusMinLevel` (200), `levelBonusMaxLevel` (400),
`levelBonusBase` (200), `levelBonusPerLevel` (5).

Tests: `Tests/EliteMonsterFormulaTests.cs` (fast tier - chance, power, exp bonus, level bonus,
absorption), `Tests/Protocol/NpcGearEliteTests.cs` (real `c_golem_mithril`: transform, stats,
exp, gold follows exp, loot bundle, resists, regeneration; `c_balrogh` boss refuses; the spawn-gem
roll with/without gem/denyElite; escape countdown, hit interrupt, leaving).
