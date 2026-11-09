# Guard bosses (#1234)

Erebor's guard boss system (`systems/guardBossSystem.scp`, `.guardBoss`). Code: `scripts/Basic/Spawning/GuardBoss.cs`,
the button in `spawn.scp`'s `d_spawn`.

A spawn can be guarded by other spawns. While a monster of any of its guard spawns lives, the guarded spawn's monsters
take no damage: swings, spells, poison and bleeding, fields and abilities all go through one check in
`DamageManager.CauseDamage`. They can still be attacked, and on 1 hit in 5 they say one of Erebor's lines ("Muj strazce
je na zivu!", "Nemuzes mne zranit!", "Kouzla ani zbrane mi neublizi!", "Muhaha!"). Once every guard's monsters are dead,
they're hurt as usual, until a guard respawns: tune the guards' respawn times.

- A guard spawn can be guarded too, so a dungeon opens layer by layer. Each spawn looks at its own guards only.
- A guard counts while its gem has a monster alive (spawn.scp's own count: a death or taming takes it off at once).
- A deleted guard gem guards nothing; the dialog lists it as "(deleted spawn)" until it's removed.
- Only damage is refused, not paralysis, curses or other effects. GM tools that remove or kill without damage still work.

## Setting it up

Double click a placed spawn gem (its `.spawn` dialog) and press **Guarded by**: a dialog lists the guard spawns (where,
what, how many alive, Go, remove) and **+ Guarded by** gives a cursor for the guarding gem. A spawn being placed with
`.spawn` has no gem yet, so its dialog says to place it first. Refused: a target that isn't a spawn gem, the spawn
itself, one already linked, and any loop (A guards B while B guards A, or longer, named in the message).

Fixed from Erebor: Erebor set the guarded monsters' resists to 1000, which irresistible damage got through and which
needed the old values kept and restored; and it allowed loops, only warning about them in its help text.

Test: `GuardBossTests.AGuardedSpawnTakesNoDamageWhileItsGuardsLive` (protocol).
