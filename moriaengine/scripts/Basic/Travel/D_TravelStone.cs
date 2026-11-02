using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1035 - d_stone_travel: two columns of seven. As on Erebor a move after it opens makes
	/// its buttons do nothing, which also covers a spell that moved the player.</summary>
	public class D_TravelStone : CompiledGumpDef {
		private const int PerColumn = 7, PerPage = PerColumn * 2, ButtonFirst = 1, ButtonNext = 2, ButtonStopBase = 10;
		private const int ListHue = 995, NextHue = 1157;

		public static void Show(Player player, TravelStone stone, int firstListed) {
			player.Dialog(player, SingletonScript<D_TravelStone>.Instance, new DialogArgs(stone, firstListed, player.X, player.Y));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			TravelStone stone = (TravelStone) args[0];
			int firstListed = (int) args[1];
			Character ch = (Character) sendTo;
			List<TravelStop> offered = TravelStops.Offered(ch);

			this.SetLocation(30, 50);
			this.AddPage(0);
			this.AddBackground(0, 0, 500, 280, 2600);
			this.AddBackground(16, 65, 469, 25, 5100);
			this.AddLabel(60, 35, 0, "Stanice dostavniku a povozu.");
			if (firstListed > 0) {
				this.AddButton(25, 69, 5209, 5003, ButtonFirst, GumpButtonType.Reply, 0);
				this.AddLabel(60, 69, 0, "Na zacatek seznamu");
			}
			if (firstListed + PerPage < offered.Count) {
				this.AddButton(459, 69, 5209, 5003, ButtonNext, GumpButtonType.Reply, 0);
				this.AddLabel(360, 69, NextHue, "Dalsi trasy");
			}
			for (int line = 0; (line < PerPage) && (firstListed + line < offered.Count); line++) {
				TravelStop stop = offered[firstListed + line];
				bool second = line >= PerColumn;
				int row = 1 + (line % PerColumn);
				int y = 80 + (row * 20);
				this.AddLabel(second ? 272 : 37, y, ListHue, stop.Name);
				this.AddLabel(second ? 410 : 175, y, ListHue, TravelStops.Count(TravelStops.Price(ch, stone, stop)) + " gp");
				this.AddButton(second ? 255 : 20, y + 3, 1209, 1210, ButtonStopBase + System.Array.IndexOf(TravelStops.All, stop), GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			TravelStone stone = args[0] as TravelStone;
			int button = gr.PressedButton;
			if ((stone == null) || stone.IsDeleted) {
				return;
			}
			if (button == ButtonFirst) {
				Show(player, stone, 0);
			} else if (button == ButtonNext) {
				Show(player, stone, (int) args[1] + PerPage);
			} else if ((button >= ButtonStopBase) && (button < ButtonStopBase + TravelStops.All.Length) &&
					(player.X == (int) args[2]) && (player.Y == (int) args[3])) {
				stone.Book(player, TravelStops.All[button - ButtonStopBase]);
			}
		}
	}
}
