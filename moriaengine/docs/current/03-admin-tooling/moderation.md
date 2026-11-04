# Moderation

The GM's tools for players who break the rules: the jail, Erebor's labour camp, the AFK check and its log. Code:
`scripts/Basic/Moderation/`. Plan and sources: #1107.

## GM menu and .info

- **`.gmmenu`** (#1114) lists the GM commands in four groups: moderation, players, GM, world. Each has a line saying
  what it does.
  - A button runs the command exactly as if typed, through `Commands.PlayerCommand`, so it gets the same permission
    check and argument parsing.
  - A command that takes an argument (`.afk` seconds, `.afklog` filter, `.goname`, `.gouid`, `.gocli`,
    `.addbooster`) has a field for it; text is sent quoted.
  - A new command is added to the menu by a line in `D_GmCommands.groups`.
- **A character's `.info`** shows *Jailed* and *InLabourCamp*.
  - **Jail / Forgive** jails a free player or forgives a jailed one.
  - **Labour camp** opens that player's `.lagrmenu` dialog.

The engine also has Sphere's `.x <command>` prefix, which runs a command on a targeted object.

Test: part of `GmToolsTests.SummonFreezeGoClientsKick` (protocol: the menu's `.goname` with its field filled, and
`.info`'s Jail / Forgive both ways).

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

## AFK check

Erebor's `systems/administrace/afk.scp`. Code: `AfkCheck.cs`, `AfkSettings.cs` (`.settings` -> AFK), `AfkLog.cs`.

**The check.**
- The dialog shows four random letters drawn as red dots, from Erebor's own dot font (the converted
  `d_afk_<letter>_<n>` points). Each letter is at a random size and place, and the dialog sits at a random spot.
- "AFK KONTROLA!": the player types the letters (any case, anywhere in the box) within `answerSeconds` (15).
- A red "N sekund do lagru" comes every other second. A wrong answer or a closed dialog brings it back the next
  second.
- "Spravne, prijemnou hru" ends it. An answer with less than 2 seconds used is logged as suspiciously fast.

**Failures** count within a period of `periodDays` (15) from the first one:
- the first and then up to `allowedFailures` (2) are told and let go;
- the next ends the period and sends the player to the labour camp for AFK. Someone already in the camp has what they
  owe doubled and their AFK level raised instead.
- Every `correctToWipeFailure` (60) correct answers wipe a failure. A correct answer after the period has run out
  wipes one per period passed. `.pocetafk` tells a player how many count.

**Logging out** doesn't help: a check or its warning carries on at login, and a check whose time ran out offline gets
one second.

**Who gets checked.**
- `.afk [seconds]` (cursor) starts a check now, optionally with a shorter time.
- `.runafk` (cursor) gives the `countdownSeconds` (15) warning first: "AFK kontrola za N sekund. Doporucuji vypnout
  makro!".
- `.forceafk` warns everyone gathering or crafting now.
- The automatic round (`automaticCheck`) does the same every 500-3300 seconds, one round in 15 only 60-130 seconds
  after the last. **It's off by default, as on Erebor** (`nastaveni_afk_toggleonoff=0`), where GMs ran the checks.
  "Gathering or crafting" is Erebor's `ismacroing`: cooking, smithing, alchemy, carpentry, lumberjacking, mining,
  fishing, tinkering, bowcraft, tailoring, inscription, cartography, taming or lockpicking. GMs and the dead are skipped.
- On Erebor `.forceafk` only ran the round when the automatic check was on. Here a GM's `.forceafk` always runs it.

`.lagrodvolat`/`.afklagrodvolat` also take back one catch and one failure, as Erebor's.

Not ported: Erebor's fake letters for catching scripts (off there, `nastaveni_afk_fakeonoff=0`) and its pardon for a
check that ended during a world save.

Every step goes to the AFK log (the server log too), as Erebor's messages to the GMs.

Test: `AfkCheckTests.TheCheckItsFailuresAndTheCamp` (protocol, the real `.afk`/`.runafk` cursors and the dialog: the
dots match the font, a wrong answer brings the dialog back, a right one ends it, two failures are let go and the third
sends to the camp, three more in the camp double what's owed, the warning leads to the check). The automatic round and
the login resume aren't covered by a test.

## AFK log

Erebor's `d_afk_dialog` (`systems/administrace/afk_dia_log.scp`). `.afklog [text]` opens the AFK log (`D_AfkLog`), 20
lines a page, filtered by any text: a name, an account, a word. Every line comes from the AFK check, the warning and
the camp; the log keeps the last 2000 and is saved.

- *Smaz* deletes a line.
- *Trest*, on a line where a player went to the camp, lowers the skill they were macroing by an amount the GM types
  (10.0 by default). It never goes below the profession's minimum. For someone still in the camp who was chopping,
  it's the Lumberjacking they get back at release.
- *Milost* takes back one AFK camp level after a yes, releasing them if they're in the camp.

Erebor kept a log per GM, built from each GM's own messages. Here there's one shared log.

Test: `AfkCheckTests.TheLogPunishesPardonsFiltersAndDeletes` (protocol, the real gumps).

## GM commands

Sphere's moderator verbs (`spherehelp.scp`) that #102 found missing. Code: `GmTools.cs`.

| Command | What it does |
|---|---|
| `.kick` (cursor) | takes a player off the server |
| `.summonto` (cursor) | brings someone to the GM and remembers where from |
| `.summoncage` (cursor) | the same, and holds them still |
| `.summondismiss` (cursor) | sends them back and lets them move |
| `.freeze`, `.unfreeze` (cursor) | holds someone still where they are ("Nemuzes se pohnout."); a GM is never held |
| `.goname "text"` | to the first character whose name holds the text, an online player first |
| `.gouid uid` | to a character or item by uid, decimal or 0x hex |
| `.gocli n` | to the n-th online player, from 0, in `.clients`' order |
| `.clients` | online players with account and place, and Go, Sum (summon), Info, Jail, Kick for each |

Sphere's `.disconnect` isn't a command here: `Character.Disconnect` would take it, as a GM's own members come before
commands. `.kick` does the same thing.

Test: `GmToolsTests.SummonFreezeGoClientsKick` (protocol: the real cursors on an NPC, `.gouid`, `.goname`, the
`.clients` dialog and its Kick).
