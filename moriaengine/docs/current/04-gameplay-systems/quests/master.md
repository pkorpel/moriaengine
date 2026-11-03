# Master quest

Code: `scripts/Basic/Quests/Master/`, from `questy/mistr.scp`. Its NPCs are in `questy/mistr_chars.scp`. Setup: `scripts/Setup/MasterQuestSetup.cs`.

An Expert of level 6+ greets Mistr Lenastir, says "ano" and gets the task: a fertilized egg of the snake queen. They also
get a key to an old locked teleport. The key and the egg are tags on the player, not items (`QuestItems`, the user's rule):
nobody can buy, pass on, drop or lose either. Asking about it ("kde",
"klic", "teleport") gets Erebor's hint: underground near the snake altars, and there is another way too.

| Step | What happens |
|---|---|
| Stepping on the teleport with the key | The key is used up. An Expert lands on the Snake Islands at 1508,3860,10; anyone else gets "You can't go through here!". A new key is had by saying "ano" to Lenastir again |
| Entering the Snake Islands, by any way | Marks the player (`r_hadiostrovy`, Erebor's `tag.mistrquest`) |
| Killing the queen (`c_hadi_kralovna`) | Her loot line `i_hadi_vejce` makes no item: her corpse holds the egg |
| Opening her corpse | The first player to open it takes the egg: "Sebral jsi oplodnene vejce hadi kralovny." |
| Greeting Lenastir with the egg | Rank Master, 500 experience, egg and key taken |

Lenastir refuses a player who has the egg but was never on the islands ("a plain bought egg"); only a GM could give the egg that
way now.

## Setting it up

`.SetupMasterQuest` (GM) places the pieces. **The archive doesn't record any of these spots, they are proposals**; move them
as you like, they are ordinary world objects:

| Piece | Spot | Why |
|---|---|---|
| Lenastir (invulnerable), twice | Beside both Expert start stones | Both realms can reach him |
| Teleport | Beridor Level 3, plane 10, between the serpent pillars around 5331,2745 | The hint says underground near snake altars. The map has serpent pillars here. Walkable from the level's spawn point |
| Queen, spawn gem | Snake Islands, near 1518,3890, respawn 30-60 min | Walkable from where the teleport lands |
| Her serpents, spawn gem | Beside the queen, 6 of the `_mistr` serpents, respawn 10-20 min | Those chardefs exist only for this quest |

A GM adding Lenastir by hand needs to make him invulnerable (Erebor's `@create`, dropped by the converter).

## Differences from Erebor

- The key and the egg are tags, not items (#1090). The egg goes to whoever opens the queen's corpse first, as lifting it out
  of the corpse was on Erebor. Erebor's checks for someone else's egg (the bug area) and a bought egg from a shop had
  nothing left to catch: the bug-area branch is gone, the bought-egg answer stays for a GM's egg.
- The quest stone statistics (`questMenu_questStone`) are not kept.

Rank quest 2 of 3; shared parts in the [quests README](README.md).

Tests: `MasterQuestTests` (protocol: the setup and that its spots are walkable, Lenastir's answers, the key as a tag and the
teleport, the real queen with no egg item, her corpse giving the egg to the first who opens it, the reward, a GM's egg with no
visit, a wasted key).
