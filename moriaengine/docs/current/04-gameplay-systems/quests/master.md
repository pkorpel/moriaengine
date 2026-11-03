# Master quest

Code: `scripts/Basic/Quests/Master/`, from `questy/mistr.scp`. Its NPCs are in `questy/mistr_chars.scp`. Setup: `scripts/Setup/MasterQuestSetup.cs`.

An Expert of level 6+ greets Mistr Lenastir, says "ano" and gets the task: a fertilized egg of the snake queen. They also
get a key (`i_klic_teleportu`, newbie, handed out again if lost) to an old locked teleport. Asking about it ("kde",
"klic", "teleport") gets Erebor's hint: underground near the snake altars, and there is another way too.

| Step | What happens |
|---|---|
| Stepping on the teleport with the key | The key is used up. An Expert lands on the Snake Islands at 1508,3860,10; anyone else gets "You can't go through here!" |
| Entering the Snake Islands, by any way | Marks the player (`r_hadiostrovy`, Erebor's `tag.mistrquest`) |
| Killing the queen (`c_hadi_kralovna`) | Her corpse holds an egg |
| Lifting the egg out of her corpse | It belongs to the first player to do so. An egg lifted from anywhere else belongs to nobody and doesn't move the quest on |
| Greeting Lenastir with the egg | Rank Master, 500 experience, egg and key taken |

Lenastir refuses a player who has an egg but was never on the islands ("a plain bought egg"). A player who has been there
but carries someone else's egg is called a cheat and sent to the bug area (370,2536,45), keeping the egg.

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

- Only a corpse counts for owning the egg, where Erebor's `@pickup_pack` took any container.
- The quest stone statistics (`questMenu_questStone`) are not kept.

Rank quest 2 of 3; shared parts in the [quests README](README.md).

Tests: `MasterQuestTests` (protocol: the setup and that its spots are walkable, Lenastir's answers, the key and teleport, the real
queen's egg lifted from her corpse, the reward, a plain egg, someone else's egg, a wasted key).
