using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>#1017 - d_class_info: a class's abilities, its points in each, and the command that uses one.</summary>
	public class D_ClassInfo : CompiledGumpDef {
		private const int TextHue = 52, TitleHue = 42, FirstRow = 62, RowHeight = 23, Width = 547;
		private const int CommandButtonBase = 1000, InfoButtonBase = 100;

		public static void Show(Player player) {
			player.DialogClose(SingletonScript<D_ClassInfo>.Instance, 0);
			player.Dialog(player, SingletonScript<D_ClassInfo>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			string classKey = ClassAbilities.ClassKey(player);
			ClassInfoRow[] rows = ClassAbilityTable.InfoOf(classKey);

			this.SetLocation(205, 150);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (rows.Length * RowHeight) + 16, 2620);
			this.AddLabel(20, 12, TitleHue, (player.Profession == null) ? "" : player.Profession.Name);
			this.AddLabel(50, 36, TextHue, "Nazev:");
			this.AddLabel(260, 36, TextHue, "Body:");
			this.AddLabel(340, 36, TextHue, "Prikaz:");
			this.AddLabel(505, 36, TextHue, "Info");
			for (int i = 0; i < rows.Length; i++) {
				ClassInfoRow row = rows[i];
				int y = FirstRow + (i * RowHeight);
				if (row.Command.Length > 0) {
					this.AddButton(20, y, 4005, 4007, CommandButtonBase + i, GumpButtonType.Reply, 0);
				}
				this.AddLabel(50, y, TextHue, row.Name);
				this.AddLabel(260, y, TextHue, (row.Ability.Length > 0) ?
					ClassAbilities.Points(player, row.Ability).ToString(CultureInfo.InvariantCulture) + "/" + row.MaxPoints.ToString(CultureInfo.InvariantCulture) : "-/-");
				this.AddLabel(340, y, TextHue, (row.Command.Length > 0) ? "." + row.Command : "-");
				this.AddButton(505, y, 4011, 4013, InfoButtonBase + i, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			ClassInfoRow[] rows = ClassAbilityTable.InfoOf(ClassAbilities.ClassKey(player));
			int button = gr.PressedButton;
			if ((button >= CommandButtonBase) && (button < CommandButtonBase + rows.Length)) {
				GameState state = player.GameState;
				if (state != null) {
					Commands.PlayerCommand(state, rows[button - CommandButtonBase].Command);
				}
			} else if ((button >= InfoButtonBase) && (button < InfoButtonBase + rows.Length)) {
				ClassInfoRow row = rows[button - InfoButtonBase];
				player.Dialog(player, SingletonScript<D_ClassInfoAbility>.Instance, new DialogArgs(row.Name, row.Description));
				Show(player);
			}
		}
	}

	/// <summary>d_class_info_ability, beside it.</summary>
	public class D_ClassInfoAbility : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(752, 150);
			this.AddPage(0);
			this.AddBackground(0, 0, 350, 197, 2620);
			this.AddLabel(20, 12, 42, (string) args[0]);
			this.AddHtml(16, 36, 320, 150, (string) args[1], false, true);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
