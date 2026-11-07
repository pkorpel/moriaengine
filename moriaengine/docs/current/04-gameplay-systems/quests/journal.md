# Quest journal

Code: `scripts/Basic/Quests/Common/QuestJournal.cs`, from `questy/System/questmenu.scp`. It's the only place a player can look up what
to do next.

`.questy` (any player) opens the menu. Its Questy tab lists the quests a player has started: active in blue first, finished in
green. Clicking one opens its page, with the text for the player's current stage. When a quest moves on, a sound plays and a
scroll appears on the screen's edge, which opens that page; a newer scroll replaces one still showing.

Erebor's journal had the Grandmaster quest (13 texts, byte for byte, in `GrandmasterQuestJournal`), but no page for Expert or
Master. Those two pages are new text in the same style. Expert's scroll shows on starting and passing; a failed attempt
drops the quest from the list.

Boosters and weekly quests have their pages ([boosters.md](boosters.md), [weekly.md](weekly.md)). The PvP league's side shows the season's board ([pvp.md](../pvp.md#pvp-league), #1116). Open dungeons is a placeholder page saying the section isn't available yet (#840),
as that system isn't ported.

## Zivy svet (#1245)

The fourth side panel (sections 31-34) shows the living world ([living-world-design.md](living-world-design.md)):

| Tab | Section | Content |
|---|---|---|
| Ukoly | 31 | Errands, filled by the errand engine (#1237 step 6). Until then a line saying people in the city give them. |
| Mesto | 32 | A page per city, the one the player stands in first. It shows the city's level and value, its factions ([reputation.md](../living-world/reputation.md)), and up to three residents who are friends (name, role, friendship out of 10). A button opens `.povest`. |
| Udalosti | 33 | World events (phase 6). For now, a line saying nothing is happening. |
| Skutky | 34 | Deeds and titles (phase 7). For now, a line saying there are none yet. |

The icons are ones Erebor's client has: side 0x2B04, tabs from the priest book 0x3325/0x3329/0x333D/0x3343. They haven't been
seen on a 3.0.6m client yet. Four tabs sit 60 apart instead of 64, so the row fits the window.

**How a system fills a page:** it calls `QuestJournalPages.Register(section, build, onButton)` from a class's `public static
void Bootstrap()`. The engine calls every scripts class's `Bootstrap` at compile time, so the journal never names the systems
it shows.
- `build(gump, player, width, height)` draws below the tabs. It may add numbered pages for paging, as the quest list does.
- Its buttons are numbered from `QuestJournalPages.FirstButton` (5000) up and come back to `onButton(player, button)`. The
  dialog's own section is in its args, so a button reaches the page that drew it.
- A section with no builder shows its hint. `CityJournalPage` (`scripts/Basic/LivingWorld/Journal/`) is the first registered
  page.

To add a ported quest: one `QuestJournalEntry` in `QuestJournal.Entries` and a `Report` call on each step.

Tests: `QuestJournalTests` (protocol): a plain player's `.questy`, the placeholder sections, the list and its colours, a page,
and the scroll. Since #1245 it also covers the living world's four tabs. On the Mesto page, Edoras comes first with Vazeny and
250, followed by Kraluv dvur and a resident friend at 7/10. The `.povest` button is checked too.
