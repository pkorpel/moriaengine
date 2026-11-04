# Keep conquest

Erebor's "dobyvani" (`dobyvani/*.scp`) and keep commanders (`systems/velitele_pevnosti*.scp`), ported in steps on #1116.
Code: `scripts/Basic/Conquest`. Spec of the whole of it: Erebor's scripts, read for #1116.

## Keeps

A keep is a group of battlefield areas (`FlaggedRegion.Battlefield`, Erebor's `tag.bf`, since #1117). A positive level is
Gondor's, a negative one Mordor's, and the number is the depth from the front:

| Fort | Areas | Shown as |
|---|---|---|
| Gondor_1 | Pelenorska pole, Osgiliath zapadni breh | Osgiliath zapadni breh |
| Gondor_2 | Vychodni Marka, Anorien | Vychodni Marka |
| Mordor_1 | Osgiliath vychodni breh, Vychodni breh, Severni Ithilien, Jizni Ithilien | Osgiliath vychodni breh |
| Mordor_2 | Minas Morgul, Gorgoroth | Minas Morgul |

## Front line

Each realm's front (`WarFront`, saved with the world; Erebor kept it on the realm's war declarator stone) remembers the
level of the last keep its realm took. Gondor's next target is one lower, Mordor's one higher. From the start (0) Gondor
attacks Mordor_1 and Mordor attacks Gondor_1.

## War lieutenants

Erebor's `namestek_valky_*.scp` (`WarLieutenant`): Norbert (`c_namestek_valky_g`) for Gondor and Urdak
(`c_namestek_valky_m`) for Mordor. `.SetupWorld` places each beside its realm's declarator; the spots are unreviewed.

Talking to one:
- A player of his realm opens his dialog with a double click within 3 tiles, or with *ahoj*/*hi*. He says "Chces
  zautocit...?" (Norbert says "Hmmm?" to speech).
- Anyone else hears "Tahni!" (Urdak's double click: "Tahni...").
- The dialog has Erebor's six pages.

"Vyhlasit utok" sells the right to declare war on the other realm for 100 000 gold (`GoldUtils.Pay`). He refuses, in
Erebor's order:
1. a player below skill rank 3 or level 30;
2. a keep captain;
3. a player of another realm;
4. when the realm has taken all it can (Gondor at -2, Mordor at 2);
5. a player who holds the right already.

Fixed from Erebor:
- The double click's distance is measured to him. Erebor measured to the player's last target.
- A player of neither realm can't buy. Erebor refused only the other realm.

Not ported: the older Minas Tirith lieutenant (`c_minas_namestek_valky_g`), which nothing placed.

## Declaring war

The right to declare war is bought from the realm's war lieutenant. `.AllowMeDeclarator` gives it to a GM.

The war declarator (`i_war_declarator`, one per realm, placed by `.SetupWorld` in Minas Tirith and Barad-dur, spots
unreviewed):
- A GM's double click sets its realm from the area it stands in.
- A player of its realm with the right gets the dialog:
  - the keep next on the front line, with a checkbox;
  - day, month and hour fields;
  - OK;
  - the current war lord and the queue, with a cancel button on one's own row.
- Anyone else: "Nejsi opravnen vyhlasit Mordoru valku."

The rules for the date and hour, in order:
- all three fields filled, and the keep ticked;
- the date exists: "Rok ma jenom dvanact mesicu.", "Unor ma jen 28 dni.";
- the hour is between 19 and 21 (`.settings` "Dobyvani");
- the date is tomorrow or later. A month before this one means next year;
- not the hour the enemy declared, nor one the realm already has;
- with a war lord already set, a later time than the last one queued joins the queue.

Cancelling costs the right, as on Erebor. When the war lord cancels:
- a running war ends as defended;
- otherwise the next queued player takes over at their own time, or, with no one queued, "byl podepsan docasny mir".

## The war clock

Each declarator stone ticks its realm's front every 10 s (`WarDeclaratorTimer`), on the server's local time:
- At the declared hour the war starts and runs 3000 s (`warSeconds`).
- Each tick, the war lord hears the seconds left. Each realm's herald speaks through its chat channel:
  - at the start;
  - every fifth minute while the war runs;
  - at minute 59 of each hour before the start.
- When the time runs out, the keep is defended. The war lord keeps the right, as on Erebor, and the next one in the
  queue takes over.
- A declaration whose evening passed while the server was down lapses the same way.

## The palantir

Erebor's `dobyvani-palantir.scp` (`FortressPalantir`, `Palantirs`). There is one per keep, placed by `.SetupWorld` in the
keep's first area; the spots are unreviewed. Minas Morgul's spawn point lies outside its own rectangle, so its spot
is found from the rectangle's middle. A GM's double click sets the keep from the battlefield it stands on.

Each keep's state is saved with the world (`KeepState`; Erebor kept it on the palantir): the owner, the breach and its
countdown, and the cash box. Every keep starts held by its own realm.

A player's double click while the war on the keep runs:
- No war yet: "Teto pevnosti nebyla vyhlasena valka.", or "Jeste neprisla hodina valky." before its hour. A ghost
  hears "Tahni pryc, mrtvolo!".
- An attacker of the declaring realm with the right breaches it:
  - the palantir turns blue (Gondor) or red (Mordor);
  - it says "Obrana pevnosti byla prolomena! Uhlidej palantir po dobu 900 s.";
  - the heralds tell both realms.
  - Without the right: "Nejsi vyzyvatelem, nejsi opravnen."
- A defender turns the breach back ("Prolomeni bylo prozatim zvraceno."). The countdown then fills again, a second at a
  time, and a new breach goes on from where it stands.

Each second (`PalantirTimer`):
- **While breached** the countdown falls. The palantir speaks every 10 s and the heralds every 60 s. At 0 the keep is
  taken:
  - it changes hands;
  - the attackers' declaration and queue go, with the war lord's and the queued attackers' rights;
  - both fronts' lines move: the winner's to the keep's level (0 if it was their own first keep), the loser's to one
    behind it;
  - the loser's own declaration aims at its new target;
  - the cash box fills with 1 gold for every 5 s since the keep was last taken, for the war lord.
- **While not breached** the countdown fills, until "Prolomeni odvraceno.".
- **When the war is over** the palantir says the keep was defended and resets.

The cash box (`i_fort_cashbox`, beside the palantir; Erebor's was a brass box with the `t_fort_cashbox` event) pays its
war lord a cheque on a double click ("Bylo ve me nashromazdeno n zlatych."). As on Erebor, a new capture overwrites
what the last war lord didn't collect.

Fixed from Erebor:
- The palantir works for as long as the war runs. Erebor's `canbewar` also wanted the hour 19-21, so every war stopped
  dead at 22:00.

Still to come (#1116): the keep's guards must be dead before a breach; the captain stones and guards go over to the
winner and the war lord becomes captain; participation rewards.

`.dobyvani` tells a player what their realm prepares and what is coming at it. `.ResetWars` (GM) clears the front of the
realm the GM stands in.

## Fixed from Erebor

- Real dates. Erebor compared the month, day and hour each on its own, so a war for 30.11 never started on 1.12. A missed
  war also started at that hour on any later day. Erebor refused a date in an earlier month ("ZATIM ZRUSIT, BUGUJE!");
  here it means next year. A later month keeps the hour and "tomorrow" rules, which Erebor skipped.
- No weekend hours. Erebor's dialog offered 17-22 at weekends, but `isWeekend` always returned 0, so they never applied.
- Each realm has its own running war. Erebor had one `ConqueredRegion` for both.
- The heralds speak once per fifth minute and once before each hour. Erebor repeated them on every 10 s tick of the minute.
- A war lord cancelling a running war ends it as defended. Erebor skipped `endWar`.
- `.dobyvani` named Mordor to Gondor's players.

Test: `ConquestTests.DeclareQueueRunAndEnd` (protocol). Two players declare at the stone through the dialog, with a
refused hour, a taken hour and the queue. It then checks `.dobyvani`, the start at its hour, the run-out handing over to
the queued one, a cancel costing the right, and a lapse. `WarLieutenantTests.NorbertSellsTheRightToDeclareWar`:
*ahoj* opens the dialog, level 29 is refused with nothing paid, and at level 30 a double click and "Vyhlasit utok" buys
the right for 100 000. `PalantirTests.BreachCounterCaptureAndTheCashBox`:
- every keep has spots;
- an attacker breaches and the countdown runs;
- a defender turns it back and it fills again;
- held to 0, the keep is Gondor's, the lines are -1/-2 and the right is spent;
- the cash box pays its war lord a cheque of 23240 for 32 hours.
