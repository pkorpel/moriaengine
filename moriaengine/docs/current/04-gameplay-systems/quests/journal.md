# Quest journal

Code: `scripts/Basic/Quests/Common/QuestJournal.cs`, from `questy/System/questmenu.scp`. It's the only place a player can look up what
to do next.

`.questy` (any player) opens the menu. Its Questy tab lists the quests a player has started: active in blue first, finished in
green. Clicking one opens its page, with the text for the player's current stage. When a quest moves on, a sound plays and a
scroll appears on the screen's edge, which opens that page; a newer scroll replaces one still showing.

Erebor's journal had the Grandmaster quest (13 texts, byte for byte, in `GrandmasterQuestJournal`), but no page for Expert or
Master. Those two pages are new text in the same style. Expert's scroll shows on starting and passing; a failed attempt
drops the quest from the list.

Boosters and weekly quests have their pages ([boosters.md](boosters.md), [weekly.md](weekly.md)). Open dungeons and the PvP league are placeholder pages saying the section isn't available yet (#840).
Their systems aren't ported.

To add a ported quest: one `QuestJournalEntry` in `QuestJournal.Entries` and a `Report` call on each step.

Tests: `QuestJournalTests` (protocol: a plain player's `.questy`, the placeholder sections, the list and its colours, a page, the scroll).
