using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - d_inputChat: .zprava. A GM picks the channel, which Erebor's dialog lost.</summary>
	public class D_Zprava : CompiledGumpDef {
		private const int Width = 680, LabelHue = 0x481, TextEntry_ = 1, GuildCheck = 1, OkButton = 1;
		private const int GondorButton = 2, MordorButton = 3, GmButton = 4, AllButton = 5;

		public static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_Zprava>.Instance);
		}

		private void GmSend(int x, int y, int button, string text) {
			this.AddButton(x, y, 0xfa5, 0xfa7, button, GumpButtonType.Reply, 0);
			this.AddLabel(x + 34, y, LabelHue, text);
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			this.SetLocation(40, 350);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, player.IsGM ? 140 : 110, 2620);
			this.AddLabel(14, 12, LabelHue, "Zadej zpravu:");
			this.AddTextEntry(14, 40, Width - 28, 20, LabelHue, TextEntry_, "");
			if (player.IsGM) {
				this.GmSend(14, 104, GondorButton, "Gondor");
				this.GmSend(150, 104, MordorButton, "Mordor");
				this.GmSend(286, 104, GmButton, "GM");
				this.GmSend(422, 104, AllButton, "Vsem");
			}
			this.AddButton(14, 72, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			this.AddButton(47, 72, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (Guild.GetGuild(player) != null) {
				this.AddLabel(560, 72, LabelHue, "GuildChat");
				this.AddCheck(640, 72, 210, 211, ChatSystem.GuildBox(player), GuildCheck);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			string text = gr.GetTextResponse(TextEntry_);
			switch (gr.PressedButton) {
				case OkButton:
					bool toGuild = (Guild.GetGuild(player) != null) && gr.IsSwitched(GuildCheck);
					if (Guild.GetGuild(player) != null) {
						ChatSystem.SetGuildBox(player, toGuild);
					}
					if (toGuild) {
						ChatSystem.SendToGuild(player, text);
					} else {
						ChatSystem.SendToRealm(player, text);
					}
					break;
				case GondorButton:
					ChatSystem.SendToRealm(player, ChatChannel.Gondor, text);
					break;
				case MordorButton:
					ChatSystem.SendToRealm(player, ChatChannel.Mordor, text);
					break;
				case GmButton:
					ChatSystem.SendToEveryone(player, ChatChannel.Gm, text);
					break;
				case AllButton:
					ChatSystem.SendToEveryone(player, ChatChannel.All, text);
					break;
			}
		}
	}
}
