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
