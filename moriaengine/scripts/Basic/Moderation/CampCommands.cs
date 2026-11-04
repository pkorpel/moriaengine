using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Erebor's GM tools for the labour camp, each by cursor (Erebor used .x): .lagr, .afklagr, .lagrpustit,
	/// .lagrodvolat, .afklagrodvolat and .lagrmenu (d_lagr_menu).</summary>
	public static class CampCommands {
		internal enum Order { Camp, AfkCamp, Release, Lower, LowerAfk, Menu }

		private static void Ask(Player self, Order order) {
			self.SysMessage(Loc<CampCommandsLoc>.Get(self.Language).WhichPlayer);
			self.Target(SingletonScript<Targ_Camp>.Instance, order);
		}

		[SteamFunction]
		public static void Lagr(Player self) {
			Ask(self, Order.Camp);
		}

		[SteamFunction]
		public static void AfkLagr(Player self) {
			Ask(self, Order.AfkCamp);
		}

		[SteamFunction]
		public static void LagrPustit(Player self) {
			Ask(self, Order.Release);
		}

		[SteamFunction]
		public static void LagrOdvolat(Player self) {
			Ask(self, Order.Lower);
		}

		[SteamFunction]
		public static void AfkLagrOdvolat(Player self) {
			Ask(self, Order.LowerAfk);
		}

		[SteamFunction]
		public static void LagrMenu(Player self) {
			Ask(self, Order.Menu);
		}

		internal static void Carry(Player gm, Character who, Order order) {
			CampCommandsLoc texts = Loc<CampCommandsLoc>.Get(gm.Language);
			switch (order) {
				case Order.Camp:
				case Order.AfkCamp:
					Camp.Send(who, order == Order.AfkCamp);
					gm.SysMessage(string.Format(texts.Sent, who.Name));
					Logger.WriteInfo(true, gm.Name + " sent " + who.Name + " to the labour camp" + ((order == Order.AfkCamp) ? " for AFK" : ""));
					break;
				case Order.Release:
					Camp.Release(who);
					gm.SysMessage(string.Format(texts.ReleasedThem, who.Name));
					break;
				case Order.Lower:
				case Order.LowerAfk:
					Camp.LowerLevel(who, order == Order.LowerAfk);
					gm.SysMessage(string.Format(texts.Lowered, who.Name));
					Logger.WriteInfo(true, gm.Name + " lowered a" + ((order == Order.LowerAfk) ? "n AFK" : "") + " camp level of " + who.Name);
					break;
				case Order.Menu:
					gm.Dialog(gm, SingletonScript<D_CampMenu>.Instance, new DialogArgs(who));
					break;
			}
		}
	}

	public class Targ_Camp : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!targetted.IsPlayer || (targetted.Account == null)) {
				self.SysMessage(Loc<CampCommandsLoc>.Get(self.Language).OnlyPlayers);
				return TargetResult.RestartTargetting;
			}
			CampCommands.Carry(self, targetted, (CampCommands.Order) parameter);
			return TargetResult.Done;
		}
	}

	/// <summary>d_lagr_menu: the account's two levels, and for a convict the coins owed (in hundredths) and the camp's
	/// Lumberjacking. OK saves them; Zavrit sends to the camp after setting the level; Zavrit za AFK sends for AFK;
	/// Propustit sets the level and releases.</summary>
	public class D_CampMenu : CompiledGumpDef {
		internal const int OkButton = 1, CampButton = 2, AfkCampButton = 3, ReleaseButton = 4;
		internal const int LevelEntry = 101, AfkLevelEntry = 102, SentenceEntry = 103, SkillEntry = 104;
		private const int Hue = 2301;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character who = (Character) args[0];
			this.SetLocation(50, 50);
			this.AddPage(0);
			this.AddBackground(0, 0, 400, 148, 5054);
			this.AddLabel(13, 10, Hue, "Lagr - " + who.Name);
			this.AddButton(13, 35, 4005, 4007, CampButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, 35, Hue, "Zavrit");
			this.AddButton(13, 54, 4005, 4007, AfkCampButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, 54, Hue, "Zavrit za AFK");
			this.AddButton(13, 73, 4005, 4007, ReleaseButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, 73, Hue, "Propustit");
			this.AddLabel(173, 35, Hue, "Pocet prohresku");
			this.AddLabel(173, 54, Hue, "Pocet prohresku AFK");
			this.AddLabel(173, 73, Hue, "Pocet logu");
			this.AddLabel(173, 92, Hue, "Skill Lumberjacking");
			this.AddTextEntry(338, 35, 50, 19, Hue, LevelEntry, Camp.Level(who.Account, false).ToString(CultureInfo.InvariantCulture));
			this.AddTextEntry(338, 54, 50, 19, Hue, AfkLevelEntry, Camp.Level(who.Account, true).ToString(CultureInfo.InvariantCulture));
			this.AddTextEntry(338, 73, 50, 19, Hue, SentenceEntry, Camp.Sentence(who).ToString(CultureInfo.InvariantCulture));
			this.AddTextEntry(338, 92, 50, 19, Hue, SkillEntry, who.GetSkill(SkillName.Lumberjacking).ToString(CultureInfo.InvariantCulture));
			this.AddButton(13, 120, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		private static int Number(GumpResponse gr, int entry) {
			int.TryParse((gr.GetTextResponse(entry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value);
			return value;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Character who = (Character) args[0];
			Player gm = gi.Cont as Player;
			if ((who == null) || who.IsDeleted || (who.Account == null) || (gm == null)) {
				return;
			}
			switch (gr.PressedButton) {
				case OkButton:
					Camp.SetLevel(who.Account, false, Number(gr, LevelEntry));
					Camp.SetLevel(who.Account, true, Number(gr, AfkLevelEntry));
					if (Camp.IsConvict(who)) {
						Camp.SetSentence(who, Number(gr, SentenceEntry));
						Camp.SetSentenceSkill(who, Number(gr, SkillEntry));
						who.SetRealSkillValue((int) SkillName.Lumberjacking, Number(gr, SkillEntry));
					}
					gm.Dialog(gm, this, new DialogArgs(who));
					break;
				case CampButton:
					Camp.SetLevel(who.Account, false, Number(gr, LevelEntry));
					CampCommands.Carry(gm, who, CampCommands.Order.Camp);
					break;
				case AfkCampButton:
					CampCommands.Carry(gm, who, CampCommands.Order.AfkCamp);
					break;
				case ReleaseButton:
					Camp.SetLevel(who.Account, false, Number(gr, LevelEntry));
					CampCommands.Carry(gm, who, CampCommands.Order.Release);
					break;
			}
		}
	}

	public class CampCommandsLoc : CompiledLocStringCollection<CampCommandsLoc> {
		public string WhichPlayer = "Na kterou postavu?";
		public string OnlyPlayers = "Jen hrace.";
		public string Sent = "{0} je v lagru.";
		public string ReleasedThem = "{0} je propusten z lagru.";
		public string Lowered = "{0}: o stupen lagru mene.";
	}
}
