---
name: erebor-reference
description: Look up how Erebor or Sphere 0.99z8 really did something — get and search the Erebor archive fork, EreborNext (native engine behaviour) and Sphere 0.56b, and read their scripts correctly. Use before porting or judging any Erebor mechanic, number or message.
---

# Looking things up in Erebor

Rules for interpreting what you find: `.claude/rules/erebor-port.md`.

## Get the sources (into the scratchpad, never into the repo)

- **Erebor scripts**: `git clone --depth 1 https://github.com/pkorpel/erebor-sphere-archive.git <scratchpad>/erebor-archive`.
  Use the fork, not gitlab's upstream. `scripts/` holds the scripts and `save/sphereworld.scp` the live
  `nastaveni_*` values. If `SphereShardConverter/convert/` already has a clone, use that.
- **EreborNext** (Sphere 0.99z8 ported from the binary, private): the token is `$GL_TOKEN` in the
  environment. Never echo it.
  ```bash
  git clone --depth 1 "https://oauth2:${GL_TOKEN}@gitlab.com/erebor/EreborNext.git" <scratchpad>/EreborNext
  git -C <scratchpad>/EreborNext remote set-url origin https://gitlab.com/erebor/EreborNext.git
  ```
  The code is in `src/EreborNext.Core/SphereSvr/` (CChar, CItem, CObjBase...). Use it for native
  behaviour: trigger order, dupes, tags, drops, touch rules, timers.
- **Sphere 0.56b**: `github.com/necr0potenc3/Sphere`, a public second reference.
- **TazUO** (client): `/workspace/TazUO`.
- The Erebor wiki (`moria.uoerebor.cz/wiki`) is behind Cloudflare Access and can't be reached.

## Search

- Always `grep -a` (or `--binary-files=text`): the archive is cp1250 with CRLF, and plain grep silently
  returns nothing on it. Sample the real archive, not the converted output, to see its vocabulary.
- Where things live: skills in `scripts/systems/skills/*.scp`; the spell table in
  `scripts/spheretable_x.scp` (`[Spell N]`, `DEFNAME`/`FLAGS`); live values `nastaveni_*` in
  `save/sphereworld.scp` (these override the tables); combat in `combat.scp` (`damage_final`); quests in
  `scripts/questy/`; shared functions in `funkce.scp`.
- Follow calls to their end: check that a function is really called (Erebor has dead code), and read
  what **sets** a tag or variable, not just its name.

## Report

On the issue: quote the file and section you read, what Erebor does, what differs from ours, and any
Erebor bug you're deliberately not copying.
