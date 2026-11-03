using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1090 - the journal's weekly section (questmenu.scp's f_questy_buildWeeklyDialog, QM:370-551): the open tasks in
	/// blue first, the done ones in grey, four a page, the locked slots with the level that opens them, and the set's reward
	/// with the time left.</summary>
	internal static class WeeklyPage {
		internal const int FirstDetailButton = 100, TasksOnPage = 4, Background = 9270;

		internal static void Add(CompiledGumpDef gump, Player player, int width) {
			WeeklySet set = WeeklyQuests.Of(player);
			bool crafter = Professions.HasProfession(player, Professions.Craftsman);
			gump.AddBackground(50, 302, 280, 108, Background);
			if ((player.Level < WeeklyQuests.MinLevel) || crafter || (set == null)) {
				if (crafter) {
					gump.AddLabel(83, 332, 0x481, "Plneni Tydennich Questu");
					gump.AddLabel(83, 346, 0x481, "je prozatim pro povolani kraft");
					gump.AddLabel(83, 360, 0x481, "nepristupne");
				} else {
					gump.AddLabel(83, 340, 0x481, "Plneni se otevre na " + WeeklyQuests.MinLevel + " urovni.");
				}
				return;
			}
			double left = (set.ExpiresTicks - DateTime.UtcNow.Ticks) / (double) TimeSpan.TicksPerSecond;
			gump.AddBackground(70, 425, 240, 50, Background);
			if (set.AllDone) {
				gump.AddLabel(123, 340, 0x44, "S P L N E N O");
				gump.AddLabel(165 - 66, 440, 0x481, "Nova Sada: " + Boosters.TimeText(left));
			} else {
				gump.AddLabel(83, 310, 0x481, "Odmena za splneni vsech Questu");
				gump.AddImage(70, 330, 0x34CC);
				gump.AddLabel(110, 330, 0x35, set.RewGold.ToString(System.Globalization.CultureInfo.InvariantCulture));
				gump.AddImage(70, 356, 0x91F);
				gump.AddLabel(110, 365, 0x35, set.RewXP.ToString(System.Globalization.CultureInfo.InvariantCulture));
				if (set.RewMI > 0) {
					gump.AddItem(180, 330, 0x0E76);
					gump.AddLabel(220, 330, 0x35, set.RewMI + "x MP");
				}
				gump.AddLabel(180 - 66, 440, 0x481, "Zbyva: " + Boosters.TimeText(left));
			}

			List<int> rows = new List<int>();
			for (int slot = 0; slot < set.Tasks.Count; slot++) {
				if (!set.Tasks[slot].IsDone) {
					rows.Add(slot);
				}
			}
			for (int slot = 0; slot < set.Tasks.Count; slot++) {
				if (set.Tasks[slot].IsDone) {
					rows.Add(slot);
				}
			}
			for (int locked = set.Tasks.Count; locked < WeeklyQuests.MaxTasks; locked++) {
				rows.Add(-1 - locked);
			}
			int page = 1;
			gump.AddPage(page);
			for (int i = 0; i < rows.Count; i++) {
				if ((i > 0) && ((i % TasksOnPage) == 0)) {
					gump.AddButton(width - 30, 47, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
					page++;
					gump.AddPage(page);
					gump.AddButton(93, 47, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
				}
				int y = 84 + ((i % TasksOnPage) * 54);
				gump.AddBackground(75, y, 235, 49, Background);
				if (rows[i] < 0) { //a slot a higher level opens
					int number = -rows[i];
					gump.AddImage(97, y + 15, 0x82C);
					gump.AddLabel(126, y + 13, 0x481, "Potrebna uroven : " + Math.Max(WeeklyQuests.MinLevel, (number - 3) * 20));
					continue;
				}
				WeeklyTask task = set.Tasks[rows[i]];
				string color = task.IsDone ? "#383838" : "#00F0FA";
				gump.AddItem(77 - task.MoveX, y + 12 - task.MoveY, task.Icon);
				string progress = (task.Total > 100) ? (task.Done + "<br>/" + task.Total) : (task.Done + "/" + task.Total);
				gump.AddHtml(115, (task.Total > 100) ? (y + 6) : (y + 14), 50, 40, "<basefont color=" + color + ">" + progress, false, false);
				gump.AddHtml(160, y + 6, 140, 34, "<basefont color=" + color + ">" + task.Description, false, false);
				if (task.IsDone) {
					gump.AddHtml(50, y + 16, 40, 20, "<basefont size=8 color=#00FA55>S P L N E N O", false, false);
				} else {
					gump.AddButton(290, y + 11, 0x828, 0x1F, FirstDetailButton + rows[i], GumpButtonType.Reply, 0);
				}
			}
		}
	}

	/// <summary>#1090 - d_questy_weeklyDetail: a task's reward. One gump here, Erebor alternated two for a client race.</summary>
	public class D_WeeklyDetail : CompiledGumpDef {
		internal static void Show(Player player, int slot) {
			D_WeeklyDetail detail = SingletonScript<D_WeeklyDetail>.Instance;
			player.DialogClose(detail, 0);
			player.Dialog(player, detail, new DialogArgs(slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			WeeklySet set = WeeklyQuests.Of((Player) sendTo);
			int slot = (int) args[0];
			if ((set == null) || (slot >= set.Tasks.Count)) {
				return;
			}
			WeeklyTask task = set.Tasks[slot];
			this.SetLocation(380, 5);
			this.AddPage(0);
			this.AddBackground(20, 20, 400, 202, WeeklyPage.Background);
			this.AddHtml(120, 35, 200, 50, "<center><basefont size=6 color=#00F0FA>" + task.Description + "</basefont></center>", false, false);
			this.AddLabel(40, 80, 0x481, "Odmena");
			this.AddLabel(240, 80, 0x481, task.Done + "/" + task.Total);
			this.AddImage(40, 110, 0x34CC);
			this.AddLabel(90, 110, 0x35, task.RewGold + " zlatych");
			this.AddImage(40, 146, 0x91F);
			this.AddLabel(90, 155, 0x35, task.RewXP + " xp");
			if (task.RewMI > 0) {
				this.AddItem(200, 110, 0x0E76);
				this.AddLabel(250, 110, 0x35, task.RewMI + "x magicky predmet");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}

	/// <summary>#1090 - d_weekly_completed: shown on each task done; its button opens the weekly section.</summary>
	public class D_WeeklyCompleted : CompiledGumpDef {
		internal static void Show(Player player, int slot) {
			player.Dialog(player, SingletonScript<D_WeeklyCompleted>.Instance, new DialogArgs(slot));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			WeeklySet set = WeeklyQuests.Of((Player) sendTo);
			int slot = (int) args[0];
			if ((set == null) || (slot < 0) || (slot >= set.Tasks.Count)) {
				return;
			}
			WeeklyTask task = set.Tasks[slot];
			this.SetLocation(300, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, 260, 90, WeeklyPage.Background);
			this.AddLabel(52, 10, 0x35, "Splnen Weekly Quest");
			this.AddHtml(50, 35, 200, 50, "<basefont color=#00F0FA>" + task.Description, false, false);
			this.AddItem(20 - task.MoveX, 38 - task.MoveY, task.Icon);
			this.AddButton(240, 29, 0x828, 0x1F, 1, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if ((gr.PressedButton == 1) && (gi.Cont is Player player)) {
				QuestJournal.Open(player, D_QuestJournal.SectionWeekly);
			}
		}
	}
}
