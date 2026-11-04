using System.Collections.Generic;
using System.Globalization;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - the quest journal's PvP side (Erebor's f_questy_buildPvPLeagueDialog, questmenu.scp): the season's
	/// board, ten a page, places 1-3 and 4-6 in their own colours; below, the player's own points and when the season ends.
	/// Pages are the journal's sections 21-29, as Erebor's argv 20-29. Opening it starts a new season if the old one ended.</summary>
	internal static class PvpLeaguePage {
		internal const int RowsOnPage = 10, LastPage = 8;
		private const int TopHue = 94, NextHue = 89, OtherHue = 51, HeadHue = 95, NameLength = 15;

		internal static void Add(CompiledGumpDef gump, Player player, int section, int width, int height) {
			PvpLeague.CheckExpiration();
			int page = section - D_QuestJournal.SectionPvp;
			List<PvpLeagueEntry> board = PvpLeague.Board;
			int x = 70;
			gump.AddLabel(x + 10, 95, HeadHue, "Hrac");
			gump.AddLabel(x + 180, 95, HeadHue, "Rank");
			int y = 120;
			for (int i = page * RowsOnPage; (i < board.Count) && (i < (page + 1) * RowsOnPage); i++) {
				int hue = (i < 3) ? TopHue : ((i < 6) ? NextHue : OtherHue);
				string name = board[i].player.Name;
				if (name.Length > NameLength - 1) {
					name = name.Substring(0, NameLength) + "...";
				}
				gump.AddLabel(x - 10, y, hue, (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
				gump.AddLabel(x + 15, y, hue, name);
				gump.AddLabel(x + 180, y, hue, board[i].points.ToString(CultureInfo.InvariantCulture));
				y += 25;
			}
			if (page > 0) {
				gump.AddButton(x + 200, 350, 0xfa, 0xfb, section - 1, GumpButtonType.Reply, 0);
			}
			if ((board.Count > (page + 1) * RowsOnPage) && (page < LastPage)) {
				gump.AddButton(x + 230, 350, 0xfc, 0xfd, section + 1, GumpButtonType.Reply, 0);
			}
			gump.AddBackground(70, height + 25, width - 60, 70, 9270);
			gump.AddLabel(85, height + 37, HeadHue, "Tvuj Rank");
			gump.AddLabel(200, height + 37, OtherHue, PvpLeague.PointsOf(player).ToString(CultureInfo.InvariantCulture));
			gump.AddLabel(85, height + 62, HeadHue, "Konec Sezony");
			gump.AddLabel(200, height + 62, OtherHue, PvpLeague.SeasonEnd.ToString("d.M. yyyy", CultureInfo.InvariantCulture));
		}
	}
}
