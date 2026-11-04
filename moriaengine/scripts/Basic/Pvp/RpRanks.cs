using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's RP rank (questy/System/RP_rank.scp): a GM rates a character's role-play from -3 to 7, with a
	/// short reason. The rank adds the converted RP_rank[n] percent (-30 to +70) of every experience gain, from the base
	/// amount and next to a booster's (addexp, funkce.scp). The player sees it in .explevel. GMs open it with .rprank or the
	/// "RP Rank" button of a character's .info, as Erebor's character-info row.
	/// The log keeps the last 12 lines, newest last; Erebor's keys started at "00" and "0" in different places and could
	/// leave the newest line out of the dialog, and a comma cut its reason (qtag splits on commas): neither is copied.
	/// Erebor's Christmas event wrote its bonus into this rank; that event isn't ported.</summary>
	public static class RpRanks {
		private static readonly TagKey rankTK = TagKey.Acquire("_rpRank_");
		private static readonly TagKey logTK = TagKey.Acquire("_rpRankLog_");
		internal const int Min = -3, Max = 7, LogLines = 12;

		public static int Of(Character ch) {
			return Convert.ToInt32(ch.GetTag(rankTK) ?? 0, CultureInfo.InvariantCulture);
		}

		/// <summary>RP_rank[n]: the percent of a gain. Erebor spelled 0 as "00"; either way it's 0.</summary>
		public static int PercentOf(int rank) {
			Constant constant = Constant.GetByName("RP_rank[" + ((rank == 0) ? "00" : rank.ToString(CultureInfo.InvariantCulture)) + "]");
			return (constant == null) ? 0 : Convert.ToInt32(constant.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>addexp's rankBonus: positive gains only.</summary>
		public static int Bonus(Character ch, int amount) {
			if (amount <= 0) {
				return 0;
			}
			return (amount * PercentOf(Of(ch))) / 100;
		}

		public static List<string> Log(Character ch) {
			string all = ch.GetTag(logTK) as string;
			return string.IsNullOrEmpty(all) ? new List<string>() : new List<string>(all.Split('\n'));
		}

		/// <summary>The changer's OK. Null when it's done, else why not.</summary>
		public static string Set(Character ch, int rank, string reason) {
			if ((rank > Max) || (rank < Min)) {
				return "RP Rank musi byt cele cislo mezi -3 a 7 !";
			}
			List<string> log = Log(ch);
			log.Add(DateTime.Now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) + ": " + Of(ch) + "->" + rank + " - " + (reason ?? "").Replace('\n', ' '));
			if (log.Count > LogLines) {
				log.RemoveRange(0, log.Count - LogLines);
			}
			ch.SetTag(logTK, string.Join("\n", log));
			if (rank == 0) {
				ch.RemoveTag(rankTK);
			} else {
				ch.SetTag(rankTK, rank);
			}
			return null;
		}

		[SteamFunction("RpRank")]
		public static void RpRankCommand(Player self) {
			self.SysMessage("Na kterou postavu?");
			self.Target(SingletonScript<Targ_RpRank>.Instance);
		}

		internal static void Show(Character gm, Character who) {
			if (!(who is Player)) {
				gm.RedMessage("Funkci musis pouzit na hrace!");
				return;
			}
			gm.Dialog(gm, SingletonScript<D_RpRankInfo>.Instance, new DialogArgs(who));
		}
	}

	public class Targ_RpRank : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			RpRanks.Show(self, targetted);
			return TargetResult.Done;
		}
	}

	/// <summary>d_RP_Rank_info: the rank with a button to change it, the notes, and every character of the account.</summary>
	public class D_RpRankInfo : CompiledGumpDef {
		internal const int ChangeButton = 1;
		private const int Hue = 0x481, TitleHue = 42;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character who = (Character) args[0];
			this.SetLocation(15, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, 396, 438, 5054);
			this.AddLabel(15, 12, TitleHue, "RP Rank - " + who.Name);
			this.AddButton(355, 10, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(15, 45, Hue, "Hodnota Ranku:");
			this.AddLabel(140, 45, Hue, RpRanks.Of(who).ToString(CultureInfo.InvariantCulture));
			this.AddButton(180, 43, 4005, 4007, ChangeButton, GumpButtonType.Reply, 0);
			this.AddLabel(15, 75, Hue, "Poznamky:");
			int y = 95;
			foreach (string line in RpRanks.Log(who)) {
				this.AddLabelCropped(15, y, 370, 18, Hue, line);
				y += 18;
			}
			y = Math.Max(y + 10, 320);
			this.AddLabel(15, y, Hue, "Prehled postav na acc:");
			y += 20;
			this.AddLabel(170, y, Hue, "Rasa");
			this.AddLabel(260, y, Hue, "Level");
			this.AddLabel(320, y, Hue, "Rank");
			y += 18;
			if (who.Account != null) {
				foreach (AbstractCharacter ch in who.Account.Characters) {
					if (!(ch is Player player)) {
						continue;
					}
					this.AddLabelCropped(15, y, 150, 18, Hue, player.Name);
					this.AddLabel(170, y, Hue, player.Race.ToString());
					this.AddLabel(260, y, Hue, player.Level.ToString(CultureInfo.InvariantCulture));
					this.AddLabel(320, y, Hue, RpRanks.Of(player).ToString(CultureInfo.InvariantCulture));
					y += 18;
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if ((gr.PressedButton == ChangeButton) && (gi.Cont is Character gm)) {
				gm.Dialog(gm, SingletonScript<D_RpRankChanger>.Instance, new DialogArgs(args[0]));
			}
		}
	}

	/// <summary>d_PR_Rank_changer: the new rank and a short reason.</summary>
	public class D_RpRankChanger : CompiledGumpDef {
		internal const int OkButton = 1, RankEntry = 1, ReasonEntry = 2;
		private const int Hue = 0x481, TitleHue = 42;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character who = (Character) args[0];
			this.SetLocation(15, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, 396, 131, 5054);
			this.AddLabel(15, 12, TitleHue, "RP Rank Changer - " + who.Name);
			this.AddButton(355, 10, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(15, 45, Hue, "Nova Hodnota Ranku:");
			this.AddTextEntry(160, 45, 60, 20, Hue, RankEntry, RpRanks.Of(who).ToString(CultureInfo.InvariantCulture));
			this.AddLabel(15, 70, Hue, "Kratky Popis:");
			this.AddTextEntry(160, 70, 220, 20, Hue, ReasonEntry, "--");
			this.AddButton(15, 100, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if ((gr.PressedButton != OkButton) || !(gi.Cont is Character gm)) {
				return;
			}
			Character who = (Character) args[0];
			int.TryParse((gr.GetTextResponse(RankEntry) ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank); //Erebor's eval: not a number is 0
			string refused = RpRanks.Set(who, rank, (gr.GetTextResponse(ReasonEntry) ?? "").Trim());
			if (refused != null) {
				gm.RedMessage(refused);
				return;
			}
			Logger.WriteInfo(true, gm.Name + " set the RP rank of " + who.Name + " to " + rank);
			RpRanks.Show(gm, who);
		}
	}
}
