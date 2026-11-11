using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Sphere's moderator verbs that #102 found missing (spherehelp.scp): .kick takes a player off the server,
	/// .summonto brings someone to the GM and remembers where from, .summoncage does that and holds them still,
	/// .summondismiss sends them back, .freeze and .unfreeze hold them still in place, .goname, .gouid and .gocli go to a
	/// character, and .clients lists who's online with buttons for all of it. Sphere's .disconnect isn't a command here:
	/// Character.Disconnect would take it, as GM commands look at the GM's own members first. .kick does the same.</summary>
	public static class GmTools {
		private static readonly TagKey returnPointTK = TagKey.Acquire("_summonReturnPoint_");

		internal enum Order { Kick, SummonTo, SummonCage, SummonDismiss, Freeze, Unfreeze }

		private static void Ask(Player self, Order order) {
			self.SysMessage(Loc<GmToolsLoc>.Get(self.Language).WhichCharacter);
			self.Target(SingletonScript<Targ_GmTool>.Instance, order);
		}

		[SteamFunction]
		public static void Kick(Player self) {
			Ask(self, Order.Kick);
		}

		[SteamFunction]
		public static void SummonTo(Player self) {
			Ask(self, Order.SummonTo);
		}

		[SteamFunction]
		public static void SummonCage(Player self) {
			Ask(self, Order.SummonCage);
		}

		[SteamFunction]
		public static void SummonDismiss(Player self) {
			Ask(self, Order.SummonDismiss);
		}

		[SteamFunction]
		public static void Freeze(Player self) {
			Ask(self, Order.Freeze);
		}

		[SteamFunction]
		public static void Unfreeze(Player self) {
			Ask(self, Order.Unfreeze);
		}

		public static bool IsFrozen(Character ch) {
			return GmFreezePlugin.IsFrozen(ch); //#1409 - a plugin refusing @denyMove
		}

		internal static void Carry(Player gm, Character who, Order order) {
			GmToolsLoc texts = Loc<GmToolsLoc>.Get(gm.Language);
			switch (order) {
				case Order.Kick:
					if (!who.IsOnline) {
						gm.SysMessage(string.Format(texts.NotOnline, who.Name));
						return;
					}
					Logger.WriteInfo(true, gm.Name + " kicked " + who.Name);
					who.Disconnect();
					gm.SysMessage(string.Format(texts.Kicked, who.Name));
					break;
				case Order.SummonTo:
				case Order.SummonCage:
					who.SetTag(returnPointTK, new Point4D(who.X, who.Y, who.Z, who.M));
					if (order == Order.SummonCage) {
						GmFreezePlugin.Freeze(who, true);
					}
					who.Go(gm.X, gm.Y, gm.Z, gm.M);
					break;
				case Order.SummonDismiss:
					GmFreezePlugin.Freeze(who, false);
					if (!(who.GetTag(returnPointTK) is Point4D back)) {
						gm.SysMessage(string.Format(texts.NowhereToReturn, who.Name));
						return;
					}
					who.RemoveTag(returnPointTK);
					who.Go(back);
					break;
				case Order.Freeze:
					GmFreezePlugin.Freeze(who, true);
					gm.SysMessage(string.Format(texts.FrozenThem, who.Name));
					break;
				case Order.Unfreeze:
					GmFreezePlugin.Freeze(who, false);
					gm.SysMessage(string.Format(texts.UnfrozenThem, who.Name));
					break;
			}
		}

		private static string Argument(ScriptArgs sa) {
			return ((sa == null) || (sa.Args == null)) ? "" : sa.Args.Trim().Trim('"').Trim();
		}

		/// <summary>GONAME: the first character whose name holds the text, an online player first.</summary>
		[SteamFunction]
		public static void GoName(Player self, ScriptArgs sa) {
			string name = Argument(sa);
			Character found = null;
			foreach (Player player in OnlinePlayers()) {
				if ((player != self) && (player.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)) {
					found = player;
					break;
				}
			}
			if (found == null) {
				foreach (Thing thing in Thing.AllThings) {
					if ((thing is Character ch) && (ch != self) && !ch.IsDeleted && (ch.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)) {
						found = ch;
						break;
					}
				}
			}
			GoTo(self, found, name);
		}

		/// <summary>GOUID: to a character or item by its uid, decimal or 0x hex.</summary>
		[SteamFunction]
		public static void GoUid(Player self, ScriptArgs sa) {
			string text = Argument(sa);
			int uid;
			bool parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
				int.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uid) :
				int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uid);
			Thing thing = parsed ? Thing.UidGetThing(uid) : null;
			GoTo(self, thing, text);
		}

		/// <summary>GOCLI: to the n-th online player, from 0, in the order .clients lists them.</summary>
		[SteamFunction]
		public static void GoCli(Player self, ScriptArgs sa) {
			string text = Argument(sa);
			List<Player> online = OnlinePlayers();
			Player found = (int.TryParse(text, out int index) && (index >= 0) && (index < online.Count)) ? online[index] : null;
			GoTo(self, found, text);
		}

		private static void GoTo(Player self, Thing target, string asked) {
			if ((target == null) || target.IsDeleted) {
				self.SysMessage(string.Format(Loc<GmToolsLoc>.Get(self.Language).NotFound, asked));
				return;
			}
			Thing top = target.TopObj();
			self.Go(top.X, top.Y, top.Z, top.M);
		}

		/// <summary>Who's online, by name.</summary>
		internal static List<Player> OnlinePlayers() {
			List<Player> online = new List<Player>();
			foreach (AbstractCharacter ch in SteamEngine.Networking.GameServer.GetAllPlayers()) {
				if ((ch is Player player) && player.IsOnline) {
					online.Add(player);
				}
			}
			online.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return online;
		}

		[SteamFunction]
		public static void Clients(Player self) {
			self.Dialog(self, SingletonScript<D_Clients>.Instance, new DialogArgs(0));
		}
	}

	public class Targ_GmTool : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			GmTools.Carry(self, targetted, (GmTools.Order) parameter);
			return TargetResult.Done;
		}
	}

	/// <summary>CLIENTS, as a dialog: each online player with their account and place, and Go, Summon, Info, Jail, Kick.
	/// 20 a page.</summary>
	public class D_Clients : CompiledGumpDef {
		internal const int CloseButton = 0, PrevButton = 1, NextButton = 2;
		internal const int GoBase = 1000, SummonBase = 2000, InfoBase = 3000, JailBase = 4000, KickBase = 5000;
		internal const int PageSize = 20;
		private const int Width = 700, Row = 22, Margin = 10, Hue = 955, TitleHue = 0x481;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int first = (int) args[0];
			List<Player> online = GmTools.OnlinePlayers();
			int height = ((PageSize + 3) * Row) + (2 * Margin);
			this.SetLocation(30, 30);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 9200);
			this.AddLabel(Margin, Margin, TitleHue, "Hracu ve hre: " + online.Count);
			this.AddButton(Width - Margin - 33, Margin, 0xfb1, 0xfb3, CloseButton, GumpButtonType.Reply, 0);
			int y = Margin + Row;
			string[] heads = { "#", "Jmeno", "Ucet", "Kde", "Go", "Sum", "Info", "Jail", "Kick" };
			int[] columns = { Margin, 40, 230, 340, 460, 505, 550, 595, 640 };
			for (int i = 0; i < heads.Length; i++) {
				this.AddLabel(columns[i], y, Hue, heads[i]);
			}
			if (first > 0) {
				this.AddButton(Width - Margin - 16, y, 0xfa, 0xfb, PrevButton, GumpButtonType.Reply, 0);
			}
			y += Row;
			for (int i = first; (i < online.Count) && (i < first + PageSize); i++) {
				Player player = online[i];
				this.AddLabel(columns[0], y, Hue, i.ToString(CultureInfo.InvariantCulture));
				this.AddLabelCropped(columns[1], y, 185, Row, Hue, player.Name);
				this.AddLabelCropped(columns[2], y, 105, Row, Hue, (player.Account != null) ? player.Account.Name : "");
				this.AddLabelCropped(columns[3], y, 115, Row, Hue, player.X + "," + player.Y + "," + player.M);
				this.AddButton(columns[4], y, 4005, 4007, GoBase + i, GumpButtonType.Reply, 0);
				this.AddButton(columns[5], y, 4005, 4007, SummonBase + i, GumpButtonType.Reply, 0);
				this.AddButton(columns[6], y, 4005, 4007, InfoBase + i, GumpButtonType.Reply, 0);
				this.AddButton(columns[7], y, 4005, 4007, JailBase + i, GumpButtonType.Reply, 0);
				this.AddButton(columns[8], y, 4005, 4007, KickBase + i, GumpButtonType.Reply, 0);
				y += Row;
			}
			if (online.Count > first + PageSize) {
				this.AddButton(Width - Margin - 16, height - Margin - 22, 0xfc, 0xfd, NextButton, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm)) {
				return;
			}
			int first = (int) args[0];
			int button = gr.PressedButton;
			if (button == CloseButton) {
				return;
			}
			if ((button == PrevButton) || (button == NextButton)) {
				gm.Dialog(gm, this, new DialogArgs(Math.Max(0, first + ((button == NextButton) ? PageSize : -PageSize))));
				return;
			}
			List<Player> online = GmTools.OnlinePlayers();
			int index = button % 1000;
			if (index >= online.Count) {
				return;
			}
			Player who = online[index];
			switch (button / 1000) {
				case GoBase / 1000:
					gm.Go(who.X, who.Y, who.Z, who.M);
					break;
				case SummonBase / 1000:
					GmTools.Carry(gm, who, GmTools.Order.SummonTo);
					break;
				case InfoBase / 1000:
					gm.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(who));
					break;
				case JailBase / 1000:
					if (Jail.Send(who)) {
						gm.SysMessage(string.Format(Loc<JailLoc>.Get(gm.Language).YouJailed, who.Name));
					}
					break;
				case KickBase / 1000:
					GmTools.Carry(gm, who, GmTools.Order.Kick);
					break;
			}
			if ((button / 1000) != (InfoBase / 1000)) {
				gm.Dialog(gm, this, new DialogArgs(first));
			}
		}
	}

	public class GmToolsLoc : CompiledLocStringCollection<GmToolsLoc> {
		public string WhichCharacter = "Na kterou postavu?";
		public string NotOnline = "{0} neni ve hre.";
		public string Kicked = "{0} byl vyhozen ze hry.";
		public string NowhereToReturn = "Neni znamo, odkud {0} prisel.";
		public string FrozenThem = "{0} je zmrazen.";
		public string UnfrozenThem = "{0} je rozmrazen.";
		public string Frozen = "Nemuzes se pohnout.";
		public string NotFound = "Nenalezeno: {0}";
	}
}
