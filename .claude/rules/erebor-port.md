# Porting Erebor

The goal is Erebor as it really ran: its scripts on Sphere 0.99z8. The maintainer said "mirror Erebor in
everything": its variance, advance rates, literal numbers and messages. Don't add conveniences of ours
where Erebor differs.

## Sources, most authoritative first

1. **Erebor's archive scripts**: `github.com/pkorpel/erebor-sphere-archive`. This is the maintainer's fork
   with their own edits. Don't use the gitlab upstream that older docs cite. The original `.scp` beats the
   converted `.def` and the old wiki.
2. **Sphere 0.99z8 native behaviour**: EreborNext, the maintainer's private C# port of the binary.
   Public Sphere 0.56b (`github.com/necr0potenc3/Sphere`) is a second reference.
3. **The client**: TazUO's source in `/workspace/TazUO`, for client preconditions.

How to get and search them: the `erebor-reference` skill.

## Reading Erebor correctly

- `sphereworld.scp`'s `nastaveni_*` values override the spheretables (skill delays, advance rates, spell
  times and mana, `cm_*`). Check `nastaveni_` before trusting any table number.
- Sphere numbers are often tenths: `timerd`, `paralyze(10)` (1 s), `serv.time`, a delay of `20` = 2 s.
  Read the function that consumes the number.
- `attr=0NN` is **hex** (`attr=010` is 0x10, move_never, not newbie). Sphere skill values are percentages;
  the engine stores skills ×10, but not stats. Archive units vary (0-100 vs per mille), so scale them.
- `obj.tag.x` reads only the instance's own tags; it never falls back to the CHARDEF/ITEMDEF's `TAG.x`
  (`typedef.tag(x)` does). Before believing a `tag.x` gate, check whether anything sets that tag on
  instances.
- Erebor has dead code: functions that are never called, unreachable branches. Check that something
  calls a function before porting it.
- A name in Erebor's scripts can mislead (`spawned_item` marks every creature-start item, not only items
  from spawn gems). Read what sets a value, not what it's called.
- Erebor scripts most skills in `systems/skills/`. Read those, not Sphere's native skill code.
- Erebor's spell table is `spheretable_x.scp` (cp1250, CRLF). Erebor's Mordor realm is `2`; ours is
  `Realm.Mordor = -1`, and `tag.guard` 1/2 means Gondor/Mordor.
- Sphere containers are newest-first, so menus built from them read reversed.

## When Erebor is wrong or missing

- Fix a clear Erebor bug, say so on the issue, and note it in the code or doc. Keep a deliberate quirk.
- Where the archive has nothing (GM-placed world pieces, missing numbers), make a reasonable choice,
  make it tunable, and list it as "mine / unreviewed" in the report.
- Don't port aspirational comments or mechanics that Erebor never shipped. Where an issue's summary and
  the real source disagree, follow the source.

## Decisions that override Erebor (the maintainer's calls)

- **Not ported, and not suggested or listed as to-do:** CTF and Christmas CTF, bomberman, the sheep game,
  wedding rings (`svatba`), wands (and nothing to stand in for them), Chivalry, Bushido, Ninjitsu.
  Leveling points stay a counter, not Erebor's `i_dovedbod` item.
- **Quest-special items are player tags**, never real items, and quest-only crafted items (like the
  potion of purification) are never brewable or craftable: the quest NPC sets the tag. Ordinary goods a
  quest counts (gold, ore, loot) stay real items. Each ported quest gets its own page under
  `docs/current/04-gameplay-systems/quests/`.
- **Every gold payment by a player goes through `GoldUtils.Pay`** (Erebor's `[function pay]`: a GM pays
  nothing; pack and its bags, then the bank; locked containers don't count). There is no second path.
- Numbers GMs tune live go on `.settings`. Sphere-level server settings (like `flipDroppedItems`) go in
  `steamengine.ini`.
- SIGTERM (`docker stop`, `run-local.sh stop`) shuts down quickly **without** saving. `.exit` saves, and
  `.exitnosave` stays available.

## Clients

Most players run **3.0.6m**; only a few use TazUO. Design for 3.0.6m: no in-place gump redraw, no
dragged-gump position, no buff bar. The protocol test client is 7.0.15.1, so a green test doesn't prove
3.0.6m behaviour; say so in the report. "Not tried in a real client" is the honest default. Players also
need the `map2` files for plane 2.
