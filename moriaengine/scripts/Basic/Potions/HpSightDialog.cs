using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>d_boosty_HPsight, from .boosts.</summary>
	public class D_HpSight : CompiledGumpDef {
		private const int OnButton = 1, CompactButton = 2, ByTargetButton = 3, BlindButton = 4;

		public static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_HpSight>.Instance, new DialogArgs());
		}

		private void Line(int y, int button, string text, bool on) {
			this.AddButton(14, y, 0xfa5, 0xfa7, button, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, 2301, text + ": " + (on ? "ano" : "ne"));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 160, 2620);
			this.AddLabel(14, 12, 0x481, "Zobrazovani HP");
			double left = HpSight.SecondsLeft(player);
			this.AddLabel(14, 34, 52, (left > 0) ? ("Aktivni: " + ActiveEffects.FormatTime(left)) : "Neaktivni, vypij lektvar vnimani");
			this.Line(56, OnButton, "Zapnuto", !HpSight.Is(player, HpSight.Off));
			this.Line(76, CompactButton, "Kompaktni mod", HpSight.Is(player, HpSight.Compact));
			this.Line(96, ByTargetButton, "Barvy podle NPC", HpSight.Is(player, HpSight.ByTarget));
			this.Line(116, BlindButton, "Barvoslepy mod", HpSight.Is(player, HpSight.ColorBlind));
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			switch (gr.PressedButton) {
				case OnButton:
					HpSight.Flip(player, HpSight.Off);
					break;
				case CompactButton:
					HpSight.Flip(player, HpSight.Compact);
					break;
				case ByTargetButton:
					HpSight.Flip(player, HpSight.ByTarget);
					break;
				case BlindButton:
					HpSight.Flip(player, HpSight.ColorBlind);
					break;
				default:
					return;
			}
			Show(player);
		}
	}
}
