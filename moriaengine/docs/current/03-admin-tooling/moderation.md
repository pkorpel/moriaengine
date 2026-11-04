# Moderation

The GM's tools for players who break the rules: the jail, Erebor's labour camp, the AFK check and its log. Code:
`scripts/Basic/Moderation/`. Plan and sources: #1107.

## Jail

Sphere 0.99z8's JAIL and FORGIVE (EreborNext `CChar.PrivilegeMethods`).

- `.jail`, then target a player: the account is jailed, the character remembers where it stood and goes to the jail
  area (`a_Jail`, plane 250). "Byl jsi uveznen."
- While jailed, any move elsewhere lands in jail again, with "Citis, ze te svet tresta." or "Citis, ze by ses mel za sve
  ciny stydet.": spells, gates, telepads, travel stones and a GM's `.go` all go through `Character.Go`. A login puts
  the character back in jail.
- Monsters and guards leave a jailed character alone.
- `.forgive` or `.pardon`, then target: the jail ends and the character goes back where it was jailed from.

Sphere also stores a release time, but its expiry does nothing (an empty branch in 0.99z8), so a jailed player stays
until forgiven. No release time here.

Test: `ModerationTests.JailAndForgive` (protocol, the real cursor commands).

## Labour camp

Erebor's `lagr` (`systems/lagr.scp`, `systems/lagr_afk.scp`, `speech/lagrspeech.scp`), on plane 1 in `a_Lagr`. Code:
`Camp.cs`, `CampSpeech.cs`, `CampCommands.cs`; the guard is placed by `.SetupWorld` (`CampSetup`, a spawn at the area's
point).

**Sentences.** Each account has two levels, one for crimes and one for failed AFK checks. They go up by one with each
sentence, and they stay when the convict is released: only a GM lowers them. The level sets the camp Lumberjacking and
the release coins owed, from Erebor's tables (`trest_N`/`lumber_N`, `afk_trest_N`/`afk_lumber_N`):

| Level | Crime: coins, Lumberjacking | AFK: coins, Lumberjacking |
|---|---|---|
| 1 | 10, 20.0 | 15, 5.0 |
| 2 | 30, 10.0 | 30, 5.0 |
| 3 | 100, 5.0 | 60, 5.0 |
| 4 | 200, 3.0 | 120, 5.0 |
| 5 | 400, 2.0 | 200, 5.0 |
| 6 | 800, 1.0 | 400, 5.0 |
| 7 | 1000, 0.5 | 8000, 5.0 |

Past level 7 the convict is a recidivist: Lumberjacking 0, nothing to earn, "Kontatuj GM pres page". A GM sets their
sentence in `.lagrmenu`.

**Going in.** A jailed player is forgiven first. The character is renamed "Trestanec <name>"; murders and karma go to 0
(the murders come back on release, karma doesn't, as Erebor). A ridden mount is shrunk into the pack, and then the pack,
the dragged item and what's worn go to the bank, except newbie, fixed, owned and invisible things. In the tutorial,
everything is deleted instead and a night sight potion makes sure there's a pack. The convict gets a hatchet and a
skirt, lands at a random spot of 356-505, 1177-1270 on plane 1, and is told how many coins to earn.

**In the camp.**
- Trees there give camp logs (`i_log_lager`, `r_lagr_tree`, #813), fixed in the pack. Convicts carry at most 250
  stones (40 x Str in tenths, Erebor's) and a log is 1 stone (`nastaveni_cm_weight_i_log_lager`). A chop over that says
  "Vic toho nepoberes" and takes a log.
- One chop in 100 brings a peasant who nags five times, five seconds apart, then leaves.
- No monster ambushes on plane 1 (Erebor's `randompotvora` skips it).
- A player's blows and spells don't hurt another player there (`e_lagr_nodmg`).

**The guard** (`c_h_strazce_lagru`), in Erebor's order:
- *hi/ahoj/buy*: how many coins are still needed, or the recidivist's line.
- *vymena*: 100 camp logs for one release coin (`i_gold_lagr`, fixed in the pack).
- *sekera*: a new hatchet if there's none.
- *svetlo*: night sight.
- *jidlo*: fed.
- *propustit*: enough coins release the convict; fewer are paid in and the rest is said.

**Release.** The tools, up to 10000 camp logs and all coins go. Name, murders and Lumberjacking come back, and the
character goes to their home. What went to the bank stays there.

**GM commands**, each by cursor:
- `.lagr` and `.afklagr` send to the camp at the next level.
- `.lagrpustit` releases.
- `.lagrodvolat` and `.afklagrodvolat` lower a level, releasing one in the camp first.
- `.lagrmenu` is Erebor's `d_lagr_menu`: both levels, the coins owed in hundredths, the camp Lumberjacking. *OK* saves;
  *Zavrit* sets the level and sends to the camp; *Zavrit za AFK* sends for AFK; *Propustit* sets the level and
  releases.

Test: `CampTests.TheCampFromSentenceToRelease` (protocol: the real `.lagr` cursor, the guard's real speech, a partial
payment, the release, a recidivist, the menu's gump). The no-harm rule between players isn't covered by a test.
