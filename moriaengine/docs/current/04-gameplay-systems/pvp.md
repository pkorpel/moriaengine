# PvP

Erebor's PvP: the league, arenas, RP rank, and the keep conquest. Code: `scripts/Basic/Pvp/`. Plan and the two specs:
#1116.

## What counts as PvP

`PvpRules.IsEvaluable(victim, attacker)`, Erebor's `isPvPEvaluable`, decides whether a player's death counts. The
Nazgul rings' kill count, the league and the conquest all use it. A death counts when:
- the two are of different realms;
- the victim isn't in an arena (the region's Arena flag, from Erebor's `tag.arena`, #1117);
- the victim isn't a craftsman;
- the victim's skill rank is 3 or more.

A player who dies again within 5 minutes of dying counts for nothing (`LevelingSystem.IsRecentlyDead`).

## PvP league

Erebor's `questy/pvpLeague.scp`. Points for killing players of the other realm, season by season.

- **Points.** When a player dies, everyone who fought them (`LevelingSystem.FindFighters`: the last 120 s, within 20
  tiles) gets 2, and the one who struck the killing blow 1 more. A kill is worth 3 and an assist 2, as in Erebor,
  where the killer had both the killing blow and a fight memory. GMs get nothing.
- **Seasons.** A season lasts `monthsInSeason` (2) calendar months. It ends at the end of day 1 of the month two on
  from its start: Erebor's `isTimeDateExceeded` is true only past the day. The next point after that starts a new
  season, and the old points are gone. Erebor never wrote a season reward (a TODO in the script), so there's none.
- **The board** keeps the first `playersStored` (30), and anyone tied with the 30th. A player moves up past everyone
  with fewer points, so among equals the one who got there first stays above, as Erebor's insertion.
- **Settings:** `.settings` -> PvP League.
- **The journal** (`.questy`, the PvP side) shows Erebor's `f_questy_buildPvPLeagueDialog` (`PvpLeaguePage`).
  - The title "PvP Sezona <n>".
  - "Hrac" and "Rank", ten rows a page. Places 1-3 are in hue 94, 4-6 in 89, the rest in 51. A name over 14
    letters is cut to 15 and "...".
  - Below: "Tvuj Rank" with the player's points and "Konec Sezony" with the end date.
  - Pages are the journal's sections 21-29, as Erebor's. Opening it starts a new season if the old one has ended.
- **Storage:** Erebor kept the board on a chest of season books at 2282,2597,80,10. Here it's saved with the world
  (`PvpLeague`), and nothing stands in the world for it.

Test: `PvpLeagueTests.KillsAssistsRulesBoardAndSeasons` (protocol, two players and a real death). It checks 3 for a
kill; nothing for a death right after dying, the same realm, a craftsman, rank 2, an arena or a GM; the board with
ties; and a season roll.
