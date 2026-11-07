/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#839 - one quest in the journal. Its progress lives with the quest: `describe` gives the player's text for
	/// the stage they're at (Erebor's markup allowed), or null if they haven't started it.</summary>
	public sealed class QuestJournalEntry {
		public readonly string Name;
		public readonly int Face;
		private readonly Func<Player, string> describe;
		private readonly Func<Player, bool> isDone;

		public QuestJournalEntry(string name, int face, Func<Player, string> describe, Func<Player, bool> isDone) {
			this.Name = name;
			this.Face = face;
			this.describe = describe;
			this.isDone = isDone;
		}

		public bool IsStarted(Player player) {
			return this.describe(player) != null;
		}

		public bool IsDone(Player player) {
			return this.isDone(player);
		}

		public string GetText(Player player) {
			return QuestDialogContent.ExpandMarkup(this.describe(player), player, this.Name);
		}
	}

	/// <summary>#839 - Erebor's quest journal (questy/System/questmenu.scp): the .questy menu with the active and finished
	/// quests, a page for each, and the scroll that shows up when a quest moves on. The rank quests are listed first, then the
	/// ported quests in the order they Register (#1264); Erebor's list had 59 quests, all with their own pages there.</summary>
	public static class QuestJournal {
		public static readonly QuestJournalEntry Expert = new QuestJournalEntry("Povyseni na Experta", 0x3000, ExpertQuest.GetJournalText, p => p.SkillRank >= SkillRank.Expert);
		public static readonly QuestJournalEntry Master = new QuestJournalEntry("Povyseni na Mistra", 0x3000, MasterQuest.GetJournalText, p => p.SkillRank >= SkillRank.Master);
		public static readonly QuestJournalEntry Grandmaster = new QuestJournalEntry("Ztracene Dcery", 0x3000, GrandmasterQuest.GetJournalText, p => GrandmasterQuest.GetStage(p) == 13);

		internal static readonly List<QuestJournalEntry> Entries = new List<QuestJournalEntry> { Expert, Master, Grandmaster };

		/// <summary>#1264 - a ported quest's entry, from its class's Bootstrap; registering it again (a recompile) replaces it in place.</summary>
		public static QuestJournalEntry Register(QuestJournalEntry entry) {
			int index = Entries.FindIndex(e => e.Name == entry.Name);
			if (index >= 0) {
				Entries[index] = entry;
			} else {
				Entries.Add(entry);
			}
			return entry;
		}

		/// <summary>Erebor's .questy command.</summary>
		[SteamFunction]
		public static void Questy(Character self) {
			Player player = self as Player;
			if (player != null) {
				if (!player.IsGM) {
					WeeklyQuests.Check(player); //#1090 - Erebor's .questy, not for GMs
				}
				Open(player, D_QuestJournal.SectionQuests);
			}
		}

		internal static void Open(Player player, int section) {
			player.Dialog(player, SingletonScript<D_QuestJournal>.Instance, section);
		}

		/// <summary>Erebor's f_questMenu_reportQuest: a scroll on the screen's edge that opens the quest's page. A new one
		/// replaces the one still there.</summary>
		public static void Report(Player player, QuestJournalEntry entry) {
			D_QuestJournalReport scroll = SingletonScript<D_QuestJournalReport>.Instance;
			player.DialogClose(scroll, 0);
			player.Sound(513);
			player.Dialog(player, scroll, Entries.IndexOf(entry));
		}

		internal static void OpenPage(Player player, int entryIndex) {
			D_QuestJournalPage page = SingletonScript<D_QuestJournalPage>.Instance;
			player.DialogClose(page, 0);
			player.Dialog(player, page, entryIndex);
		}
	}

	/// <summary>#1245 - the journal's pages that other systems fill: a section's builder draws into the window below the tabs, its
	/// buttons (from FirstButton up) come back to its own handler. Registered from a class's Bootstrap, so the journal never
	/// names the systems it shows.</summary>
	public static class QuestJournalPages {
		public const int FirstButton = 5000;

		public delegate void Builder(CompiledGumpDef gump, Player player, int width, int height);
		public delegate void ButtonHandler(Player player, int button);

		private static readonly Dictionary<int, Builder> builders = new Dictionary<int, Builder>();
		private static readonly Dictionary<int, ButtonHandler> handlers = new Dictionary<int, ButtonHandler>();

		public static void Register(int section, Builder build, ButtonHandler onButton = null) {
			builders[section] = build;
			if (onButton != null) {
				handlers[section] = onButton;
			} else {
				handlers.Remove(section);
			}
		}

		internal static bool TryAdd(CompiledGumpDef gump, Player player, int section, int width, int height) {
			Builder build;
			if (!builders.TryGetValue(section, out build)) {
				return false;
			}
			build(gump, player, width, height);
			return true;
		}

		internal static bool TryRespond(Player player, int section, int button) {
			ButtonHandler handle;
			if (!handlers.TryGetValue(section, out handle)) {
				return false;
			}
			handle(player, button);
			return true;
		}
	}

	/// <summary>#839 - Erebor's d_questy. Sections: 1-3 the main one's tabs (quests, open dungeons, boosters), 11 weekly quests,
	/// 21 the PvP league, 31-34 the living world's tabs (#1245: errands, city, events, deeds).</summary>
	public class D_QuestJournal : CompiledGumpDef {
		public const int SectionQuests = 1, SectionDungeons = 2, SectionBoosters = 3, SectionWeekly = 11, SectionPvp = 21;
		public const int SectionErrands = 31, SectionCity = 32, SectionEvents = 33, SectionDeeds = 34;
		private const int Width = 300, Height = 400, Background = 9270, TitleHue = 95, HintHue = 0x21;
		private const int QuestsOnPage = 7, FirstQuestButton = 1000, SelectedTabHue = 52;
		internal const int FirstBoosterButton = 500, BoostersOnPage = 3;
		private static readonly int[] sectionPics = { 0x2B08, 0x2B03, 0x2B05, 0x2B04 };
		//#1245 - the living world's tabs: icons Erebor's priest book draws, so its client has them
		private static readonly int[] livingTabPics = { 0x3325, 0x3329, 0x333D, 0x3343 };
		private static readonly int[] livingTabTitleX = { 8, 8, -2, 8 };
		private static readonly string[] livingTabTitles = { "Ukoly", "Mesto", "Udalosti", "Skutky" };
		private static readonly string[] livingTabHints = {
			"Ukoly ti daji lide ve meste. Mluv s nimi.",
			"",
			"Ve svete se ted nic zvlastniho nedeje.",
			"Zatim nemas zadne skutky."
		};
		private static readonly int[] tabPics = { 0x3324, 0x3334, 0x332B };
		private static readonly int[] tabTitleX = { 15, -22, 10 };
		private static readonly string[] tabTitles = { "Questy", "Otevrene Dungeony", "Boostery" };

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			int section = Convert.ToInt32(args[0], CultureInfo.InvariantCulture);
			int selectedSide = section / 10;

			this.SetLocation(5, 5);
			this.AddPage(0);
			for (int side = 0; side < sectionPics.Length; side++) {
				if (side != selectedSide) {
					this.AddSideButton(side, true);
				}
			}
			this.AddBackground(40, 20, Width, Height, Background);
			this.AddBackground(80, 30, Width - 80, 50, Background);
			this.AddSideButton(selectedSide, false);
			if (section < 10) {
				this.AddLabel((Width / 2) + tabTitleX[section - 1], 45, TitleHue, tabTitles[section - 1]);
				for (int tab = 0; tab < tabPics.Length; tab++) {
					this.AddTab(tab, tabPics.Length, tabPics[tab], tab + 1, tab != section - 1);
				}
			} else if (section == SectionWeekly) {
				this.AddLabel((Width / 2) - 17, 45, TitleHue, "Tydenni Questy");
			} else if (IsLivingWorld(section)) {
				int livingTab = section - SectionErrands;
				this.AddLabel((Width / 2) + livingTabTitleX[livingTab], 45, TitleHue, livingTabTitles[livingTab]);
				for (int tab = 0; tab < livingTabPics.Length; tab++) {
					this.AddTab(tab, livingTabPics.Length, livingTabPics[tab], SectionErrands + tab, tab != livingTab);
				}
			} else if (section >= SectionPvp) {
				PvpLeague.CheckExpiration();
				this.AddLabel((Width / 2) - 12, 45, TitleHue, "PvP Sezona " + PvpLeague.Season); //#1116
			} else {
				this.AddLabel((Width / 2) - 12, 45, TitleHue, "PvP Sezona");
			}
			this.AddImage(Width + 7, 35, 0x28C9);

			QuestJournalLoc loc = Loc<QuestJournalLoc>.Get(player.Language);
			if (section == SectionQuests) {
				this.AddQuests(player, loc);
			} else if (section == SectionBoosters) {
				this.AddBoosters(player);
			} else if (section == SectionWeekly) {
				WeeklyPage.Add(this, player, Width);
			} else if ((section >= SectionPvp) && (section <= SectionPvp + PvpLeaguePage.LastPage)) {
				PvpLeaguePage.Add(this, player, section, Width, Height); //#1116
			} else if (QuestJournalPages.TryAdd(this, player, section, Width, Height)) {
				//#1245 - a page another system registered
			} else if (IsLivingWorld(section)) {
				this.AddHtml(80, Height / 2, Width - 80, 40, "<center><basefont color=#00F0FA>" + livingTabHints[section - SectionErrands] + "</basefont></center>", false, false);
			} else {
				//not ported yet: dungeon marks - #840
				this.AddLabel((Width / 2) - 62, Height / 2, HintHue, loc.NotAvailableYet);
			}
		}

		private void AddSideButton(int side, bool active) {
			int y = 30 + (side * 85);
			this.AddBackground(0, y, 72, 78, Background);
			if (active) {
				this.CheckerTrans(10, y + 10, 52, 58);
				this.AddButton(15, y + 13, sectionPics[side], sectionPics[side], (side * 10) + 1, GumpButtonType.Reply, 0);
			} else {
				this.AddImage(15, y + 13, sectionPics[side]);
			}
		}

		internal static bool IsLivingWorld(int section) {
			return (section >= SectionErrands) && (section <= SectionDeeds);
		}

		//four tabs in a row are 60 apart, three 64, so both rows fit the window
		private void AddTab(int tab, int count, int pic, int button, bool active) {
			int step = (count > 3) ? 60 : 64;
			int x = 35 + ((Width - (count * step)) / 2) + (tab * step);
			this.AddBackground(x, 73, 58, 56, Background);
			//Erebor dimmed the other tabs with a checkertrans, but the client makes everything it overlaps see-through, and
			//that includes the whole window under the tab; so the selected tab is the one that stands out instead
			if (active) {
				this.AddButton(x + 13, 85, pic, pic, button, GumpButtonType.Reply, 0);
			} else {
				this.AddImage(x + 13, 85, pic, SelectedTabHue);
			}
		}

		/// <summary>#1090 - Erebor's boosters page: the running one, then the stored ones, three a page, each a button that
		/// starts it while none runs.</summary>
		private void AddBoosters(Player player) {
			BoosterTimer active = Boosters.Active(player);
			this.AddBackground(80, 128, 225, 80, Background);
			this.AddLabel(150, 138, TitleHue, "Aktivni booster");
			if (active != null) {
				this.AddImage(100, 152, Boosters.Icon);
				this.AddHtml(135, 158, 140, 20, "<center><basefont color=#00FA55 size=6>" + Boosters.Kinds[active.kind].Description + "</basefont></center>", false, false);
				this.AddLabel(135, 176, TitleHue, Boosters.TimeText(active.DueInSeconds));
			} else {
				this.AddLabel(110, 162, HintHue, "Zadny Booster neni aktivni.");
			}
			List<KeyValuePair<int, int>> stored = Boosters.Stored(player);
			int page = 1;
			this.AddPage(page);
			for (int row = 0; row < stored.Count; row++) {
				if ((row > 0) && ((row % BoostersOnPage) == 0)) {
					this.AddButton(Width - 30, 47, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
					page++;
					this.AddPage(page);
					this.AddButton(93, 47, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
				}
				int y = 214 + ((row % BoostersOnPage) * 64);
				this.AddBackground(75, y, 235, 60, Background);
				this.AddBackground(250, y, 60, 60, Background);
				this.AddHtml(88, y + 12, 160, 20, "<center><basefont color=#00F0FA>" + Boosters.Kinds[stored[row].Key].Description + "</basefont></center>", false, false);
				this.AddHtml(88, y + 28, 160, 20, "<basefont color=#00FA55 size=6>" + stored[row].Value + "x</basefont>", false, false);
				if (active != null) {
					this.AddImage(264, y + 14, Boosters.Icon, SelectedTabHue); //Erebor greyed it with a checkertrans, which the client lets through to the window
				} else {
					this.AddButton(264, y + 14, Boosters.Icon, Boosters.Icon, FirstBoosterButton + row, GumpButtonType.Reply, 0);
				}
			}
		}

		//the active ones first, as Erebor's list
		private void AddQuests(Player player, QuestJournalLoc loc) {
			List<int> shown = new List<int>();
			foreach (bool done in new[] { false, true }) {
				for (int i = 0; i < QuestJournal.Entries.Count; i++) {
					QuestJournalEntry entry = QuestJournal.Entries[i];
					if (entry.IsStarted(player) && (entry.IsDone(player) == done)) {
						shown.Add(i);
					}
				}
			}
			if (shown.Count == 0) {
				this.AddLabel((Width / 2) - 62, Height / 2, HintHue, loc.NoQuestsYet);
				return;
			}
			int page = 1;
			this.AddPage(page);
			for (int i = 0; i < shown.Count; i++) {
				if ((i > 0) && ((i % QuestsOnPage) == 0)) {
					this.AddButton(Width - 30, 47, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
					page++;
					this.AddPage(page);
					this.AddButton(93, 47, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
				}
				QuestJournalEntry entry = QuestJournal.Entries[shown[i]];
				int y = 132 + ((i % QuestsOnPage) * 40);
				this.AddBackground(75, y, Width - 65, 35, Background);
				this.AddButton(Width - 10, y, 0x828, 0x1F, FirstQuestButton + shown[i], GumpButtonType.Reply, 0);
				string color = entry.IsDone(player) ? "#00FA55" : "#00F0FA";
				this.AddHtml((Width / 2) - 32, y + 8, 150, 20, "<basefont color=" + color + ">" + entry.Name, false, false);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			int button = gr.PressedButton;
			if ((player == null) || (button == 0)) {
				return;
			}
			bool renewed = WeeklyQuests.Check(player); //#1090 - every button of d_questy
			int section = Convert.ToInt32(args[0], CultureInfo.InvariantCulture);
			if (button >= QuestJournalPages.FirstButton) {
				if (!QuestJournalPages.TryRespond(player, section, button)) {
					QuestJournal.Open(player, section);
				}
			} else if (button >= FirstQuestButton) {
				QuestJournal.OpenPage(player, button - FirstQuestButton);
				QuestJournal.Open(player, SectionQuests);
			} else if ((button >= WeeklyPage.FirstDetailButton) && (button < FirstBoosterButton)) {
				QuestJournal.Open(player, SectionWeekly);
				if (!renewed) {
					D_WeeklyDetail.Show(player, button - WeeklyPage.FirstDetailButton);
				}
			} else if (button >= FirstBoosterButton) {
				Boosters.Use(player, button - FirstBoosterButton);
				QuestJournal.Open(player, SectionBoosters);
			} else {
				QuestJournal.Open(player, button);
			}
		}
	}

	/// <summary>#839 - a quest's page in the journal (Erebor's d_questy_* and f_qDialogInstructions). args: its index.</summary>
	public class D_QuestJournalPage : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			QuestJournalEntry entry = QuestJournal.Entries[Convert.ToInt32(args[0], CultureInfo.InvariantCulture)];
			this.SetLocation(380, 5);
			this.AddPage(0);
			this.AddBackground(20, 20, 400, 202, 9270);
			this.AddLabel(195, 40, 95, entry.Name);
			this.AddImage(25, 30, 0x3182);
			this.AddImage(36, 37, entry.Face);
			if (entry.IsStarted(player)) {
				this.AddHtml(160, 80, 242, 130, entry.GetText(player), false, true);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	/// <summary>#839 - Erebor's d_questMenu_reportDialog. args: the quest's index.</summary>
	public class D_QuestJournalReport : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.NoMove();
			this.NoClose();
			this.SetLocation(5, 30);
			this.AddPage(0);
			this.AddButton(0, 0, 0x1e, 0x1e, 1, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			if ((player != null) && (gr.PressedButton == 1)) {
				QuestJournal.OpenPage(player, Convert.ToInt32(args[0], CultureInfo.InvariantCulture));
			}
		}
	}

	public class QuestJournalLoc : CompiledLocStringCollection<QuestJournalLoc> {
		public string NoQuestsYet = "You have no quests yet.";
		public string NotAvailableYet = "This section isn't available yet.";
	}
}
