using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - d_chat_options: .chat. Only the channels that mean something to the player, and the
	/// list is edited here, not by Erebor's one input box per name.</summary>
	public class D_ChatOptions : CompiledGumpDef {
		private const int Width = 400, RowHeight = 22, LabelHue = 0x481, TextHue = 2301, WarnHue = 36;
		private const int NameEntry = 1, AddNameButton = 2, ChannelButton = 100, RemoveButton = 200;

		private const string Rules = "Chat slouzi ke snadnemu dorozumivani hracu. Muze dobre poslouzit jak v pripade upozorneni ostatnich hracu na nepritele " +
			"v nejake lokaci, tak i k lukrativni obchodni nabidce. Mejte vsak na pameti, ze z RP hlediska by vase postava neco takoveho vedet nemela. " +
			"Inu dobry RP hrac si chat proste vypne.<br><br>Je zakazano spamovani serveru opakujicimi se zpravami. Pro chat jakozto samozrejme pro celou hru " +
			"plati zakaz uzivani vulgarismu.";

		private static readonly ChatChannel[] allChannels = { ChatChannel.Gondor, ChatChannel.Mordor, ChatChannel.All, ChatChannel.Gm, ChatChannel.Guild };

		public static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_ChatOptions>.Instance);
		}

		private static string NameOf(ChatChannel channel) {
			switch (channel) {
				case ChatChannel.Gondor:
					return "GONDOR Realm";
				case ChatChannel.Mordor:
					return "MORDOR Realm";
				case ChatChannel.All:
					return "Slyset vse";
				case ChatChannel.Gm:
					return "GMChat";
				default:
					return "GuildChat";
			}
		}

		private static bool IsOffered(Player player, ChatChannel channel) {
			switch (channel) {
				case ChatChannel.Gondor:
					return (player.Realm == Realm.Gondor) || player.IsGM;
				case ChatChannel.Mordor:
					return (player.Realm == Realm.Mordor) || player.IsGM;
				case ChatChannel.Gm:
					return player.IsGM;
				case ChatChannel.Guild:
					return Guild.GetGuild(player) != null;
				default:
					return true;
			}
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			List<string> muted = ChatSystem.MutedNames(player);
			this.SetLocation(80, 80);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 400, 2620);
			this.AddLabel(14, 12, LabelHue, "Nastaveni chatu");
			this.AddHtml(14, 36, Width - 28, 80, Rules, true, true);
			int y = 124;
			this.AddLabel(14, y, LabelHue, "Kanaly:");
			y += RowHeight;
			foreach (ChatChannel channel in allChannels) {
				if (IsOffered(player, channel)) {
					this.AddButton(14, y, 0xfa5, 0xfa7, ChannelButton + (int) channel, GumpButtonType.Reply, 0);
					this.AddLabel(50, y, TextHue, NameOf(channel) + ": " + (ChatSystem.IsOn(player, channel) ? "On" : "Off"));
					y += RowHeight;
				}
			}
			y += 6;
			this.AddLabel(14, y, WarnHue, "Umlcovaci List: (jmena postav, ktere nechces slyset)");
			y += RowHeight;
			for (int i = 0; i < muted.Count; i++) {
				int x = ((i % 2) == 0) ? 14 : 200;
				this.AddButton(x, y, 0xfb1, 0xfb3, RemoveButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(x + 34, y, TextHue, muted[i]);
				y += i % 2 * RowHeight;
			}
			if ((muted.Count % 2) == 1) {
				y += RowHeight;
			}
			if (muted.Count < ChatSystem.MaxMuted) {
				this.AddButton(14, y, 0xfbd, 0xfbf, AddNameButton, GumpButtonType.Reply, 0);
				this.AddTextEntry(50, y, 300, 20, LabelHue, NameEntry, "");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (button == 0) {
				return;
			}
			if (button == AddNameButton) {
				ChatSystem.Mute(player, gr.GetTextResponse(NameEntry) ?? "");
			} else if (button >= RemoveButton) {
				ChatSystem.Unmute(player, button - RemoveButton);
			} else if (button >= ChannelButton) {
				ChatSystem.Flip(player, (ChatChannel) (button - ChannelButton));
			}
			Show(player);
		}
	}
}
