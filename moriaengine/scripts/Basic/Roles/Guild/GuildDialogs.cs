using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#884 - d_guild_menu: what a stone offers depends on the viewer's rank.</summary>
	public class D_GuildMenu : CompiledGumpDef {
		internal const int MembersButton = 1, CandidatesButton = 2, ResignButton = 3, AbbrevButton = 4,
			AddCandidateButton = 5, NameButton = 6, ShortNameButton = 7, TransferButton = 8;
		private const int Hue = 955, RowHeight = 20;
		private static readonly string[] rankNames = { "Verejnost", "Kandidat", "Clen" };
		private static readonly int[] rowsByRank = { 1, 4, 5, 8 };

		private static string MasterTitle(Guild guild) {
			return ((guild.Master != null) && guild.Master.IsFemale) ? "Mistress" : "Master";
		}

		private int AddSection(int y, int rank, int neededRank, int button, string text) {
			if (rank < neededRank) {
				return y;
			}
			this.AddButton(14, y, 4005, 4007, button, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, Hue, text);
			return y + RowHeight;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Character viewer = (Character) sendTo;
			int rank = guild.GetRank(viewer);
			string masterName = (guild.Master != null) ? guild.Master.Name : "";

			this.SetLocation(30, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, 260, 150 + (rowsByRank[rank] * RowHeight), 2620);
			this.AddImage(10, 10, 100);
			this.AddHtml(30, 30, 100, 60, guild.Name, false, false);
			this.AddLabel(150, 14, Hue, MasterTitle(guild) + ":");
			this.AddLabel(150, 34, Hue, masterName);
			this.AddLabel(150, 54, Hue, "Tvuj rank:");
			this.AddLabel(150, 74, Hue, (rank == Guild.RankMaster) ? MasterTitle(guild) : rankNames[rank]);
			this.AddLabel(14, 112, Hue, "Zkratka: " + guild.Abbreviation);

			int y = 136;
			y = this.AddSection(y, rank, Guild.RankPublic, MembersButton, "Zobrazit seznam clenu");
			y = this.AddSection(y, rank, Guild.RankCandidate, CandidatesButton, "Zobrazit seznam kandidatu");
			y = this.AddSection(y, rank, Guild.RankCandidate, ResignButton, "Vzdat se clenstvi");
			y = this.AddSection(y, rank, Guild.RankCandidate, AbbrevButton, (guild.ShowsAbbreviation(viewer) ? "Vy" : "Za") + "pnout zobrazeni zkratky");
			y = this.AddSection(y, rank, Guild.RankMember, AddCandidateButton, "Pridat kandidata na clenstvi");
			y = this.AddSection(y, rank, Guild.RankMaster, NameButton, "Nastavit jmeno guildy");
			y = this.AddSection(y, rank, Guild.RankMaster, ShortNameButton, "Nastavit zkratku");
			this.AddSection(y, rank, Guild.RankMaster, TransferButton, "Predat vedeni");
		}

		private static bool CheckRights(Guild guild, Player player, int rank) {
			if (guild.GetRank(player) >= rank) {
				return true;
			}
			player.RedMessage("Na to nemas pravo.");
			return false;
		}

		internal static void Show(Guild guild, Player player) {
			player.Dialog(player, SingletonScript<D_GuildMenu>.Instance, new DialogArgs(guild));
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Player player = (Player) gi.Cont;
			switch (gr.PressedButton) {
				case MembersButton:
					player.Dialog(player, SingletonScript<D_GuildMembers>.Instance, new DialogArgs(guild, guild.FullMembers));
					break;
				case CandidatesButton:
					if (CheckRights(guild, player, Guild.RankCandidate)) {
						player.Dialog(player, SingletonScript<D_GuildCandidates>.Instance, new DialogArgs(guild, guild.Candidates));
					}
					break;
				case ResignButton:
					if (CheckRights(guild, player, Guild.RankCandidate)) {
						player.Dialog(player, SingletonScript<D_GuildConfirm>.Instance, new DialogArgs(guild, player, "Chces opravdu opustit guildu?"));
					}
					break;
				case AbbrevButton:
					if (CheckRights(guild, player, Guild.RankCandidate)) {
						guild.ToggleAbbreviation(player);
						Show(guild, player);
					}
					break;
				case AddCandidateButton:
					if (CheckRights(guild, player, Guild.RankMember)) {
						player.Target(SingletonScript<Targ_GuildCandidate>.Instance, guild);
					}
					break;
				case NameButton:
					if (CheckRights(guild, player, Guild.RankMaster)) {
						player.Dialog(player, SingletonScript<D_GuildInput>.Instance, new DialogArgs(guild, true));
					}
					break;
				case ShortNameButton:
					if (CheckRights(guild, player, Guild.RankMaster)) {
						player.Dialog(player, SingletonScript<D_GuildInput>.Instance, new DialogArgs(guild, false));
					}
					break;
				case TransferButton:
					if (CheckRights(guild, player, Guild.RankMaster)) {
						player.SysMessage("Zamer osobu");
						player.Target(SingletonScript<Targ_GuildMaster>.Instance, guild);
					}
					break;
			}
		}
	}

	/// <summary>The shared shape of d_g_members and d_g_candidates: 10 names a page.</summary>
	public abstract class GuildListDialog : CompiledGumpDef {
		private const int Width = 300, PageRows = 10, RowHeight = 20, FirstRow = 62, TextHue = 2301;
		protected const int FirstRowButton = 100;

		protected abstract string CountLabel { get; }

		protected abstract void AddColumnTitles(bool isMaster);

		protected abstract void AddRow(Guild guild, Character listed, int index, int y, bool isMaster);

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Guild guild = (Guild) args[0];
			List<Character> listed = (List<Character>) args[1];
			bool isMaster = guild.GetRank((Character) sendTo) == Guild.RankMaster;

			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (PageRows * RowHeight) + 20, 2620);
			this.AddLabel(14, 12, TextHue, this.CountLabel + ": " + listed.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddColumnTitles(isMaster);

			int pages = (listed.Count + PageRows - 1) / PageRows;
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 30, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 30, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = System.Math.Min(listed.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					this.AddRow(guild, listed[i], i, FirstRow + ((i % PageRows) * RowHeight), isMaster);
				}
			}
		}

		protected void AddTextLabel(int x, int y, string text) {
			this.AddLabel(x, y, TextHue, text);
		}

		protected static void ConfirmKick(Guild guild, Player player, Character listed) {
			player.Dialog(player, SingletonScript<D_GuildConfirm>.Instance,
				new DialogArgs(guild, listed, "Opravdu chces " + listed.Name + " vyhodit z guildy?"));
		}

		/// <summary>The row a button stands for, null for a button id no row has.</summary>
		protected static Character Listed(DialogArgs args, int index) {
			List<Character> listed = (List<Character>) args[1];
			return ((index >= 0) && (index < listed.Count)) ? listed[index] : null;
		}
	}

	/// <summary>d_g_members. Erebor's vote buttons did nothing, so the column only marks the master.</summary>
	public class D_GuildMembers : GuildListDialog {
		protected override string CountLabel {
			get { return "Clenu"; }
		}

		protected override void AddColumnTitles(bool isMaster) {
			this.AddTextLabel(14, 36, "gm");
			this.AddTextLabel(50, 36, "Jmeno clena");
			if (isMaster) {
				this.AddTextLabel(220, 36, "vyhodit");
			}
		}

		protected override void AddRow(Guild guild, Character listed, int index, int y, bool isMaster) {
			if (listed == guild.Master) {
				this.AddImage(14, y, 211);
			}
			this.AddTextLabel(50, y, listed.Name);
			if (isMaster) {
				this.AddButton(232, y, 4005, 4007, FirstRowButton + index, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Player player = (Player) gi.Cont;
			Character listed = Listed(args, gr.PressedButton - FirstRowButton);
			if (gr.PressedButton == 0) {
				D_GuildMenu.Show(guild, player);
			} else if ((listed != null) && (guild.GetRank(player) == Guild.RankMaster)) {
				ConfirmKick(guild, player, listed);
			}
		}
	}

	/// <summary>d_g_candidates: the master accepts; anyone who sees the list can remove, as on Erebor.</summary>
	public class D_GuildCandidates : GuildListDialog {
		private const int AcceptButton = 500;

		protected override string CountLabel {
			get { return "Kandidatu"; }
		}

		protected override void AddColumnTitles(bool isMaster) {
			this.AddTextLabel(14, 36, "Fire");
			this.AddTextLabel(50, 36, "Jmeno kandidata");
			if (isMaster) {
				this.AddTextLabel(220, 36, "Prijmout");
			}
		}

		protected override void AddRow(Guild guild, Character listed, int index, int y, bool isMaster) {
			this.AddButton(14, y, 4005, 4007, FirstRowButton + index, GumpButtonType.Reply, 0);
			this.AddTextLabel(50, y, listed.Name);
			if (isMaster) {
				this.AddButton(232, y, 4005, 4007, AcceptButton + index, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Player player = (Player) gi.Cont;
			if (gr.PressedButton == 0) {
				D_GuildMenu.Show(guild, player);
			} else if (gr.PressedButton < AcceptButton) {
				Character listed = Listed(args, gr.PressedButton - FirstRowButton);
				if ((listed != null) && (guild.GetRank(player) >= Guild.RankCandidate)) {
					ConfirmKick(guild, player, listed);
				}
			} else if (guild.GetRank(player) == Guild.RankMaster) {
				Character candidate = Listed(args, gr.PressedButton - AcceptButton);
				if ((candidate != null) && guild.IsCandidate(candidate)) {
					guild.AddMember(candidate);
				}
				player.Dialog(player, this, new DialogArgs(guild, guild.Candidates));
			}
		}
	}

	/// <summary>d_potvrdit, for leaving a guild or removing someone from it.</summary>
	public class D_GuildConfirm : CompiledGumpDef {
		private const int YesButton = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 150, 2620);
			this.AddLabel(14, 12, 0x481, "Potvrzeni prikazu");
			this.AddHtml(14, 40, 270, 60, "<basefont color=#ffffff>" + (string) args[2], false, false);
			this.AddButton(14, 112, 0xfb7, 0xfb9, YesButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, 114, 0x481, "Ano");
			this.AddButton(220, 112, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(256, 114, 0x481, "Ne");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Character who = (Character) args[1];
			if ((gr.PressedButton == YesButton) && (Guild.GetGuild(who) == guild)) {
				guild.Resign(who);
			}
		}
	}

	/// <summary>input(...) for the guild's name or its abbreviation.</summary>
	public class D_GuildInput : CompiledGumpDef {
		private const int OkButton = 1, TextEntryId = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Guild guild = (Guild) args[0];
			bool isName = (bool) args[1];
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 110, 2620);
			this.AddLabel(14, 12, 0x481, isName ? "Jak se ma guilda jmenovat?" : "Jakou ma mit guilda zkratku?");
			this.AddTextEntry(14, 40, 270, 20, 52, TextEntryId, isName ? guild.Name : guild.Abbreviation);
			this.AddButton(14, 72, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Guild guild = (Guild) args[0];
			Player player = (Player) gi.Cont;
			string text = (gr.GetTextResponse(TextEntryId) ?? "").Trim();
			if ((gr.PressedButton == OkButton) && (text.Length > 0) && (guild.GetRank(player) == Guild.RankMaster)) {
				if (!(bool) args[1]) {
					guild.Abbreviation = text;
				} else if ((Guild.FindByName(text) == null) || (Guild.FindByName(text) == guild)) {
					guild.Rename(text);
				} else {
					player.RedMessage("Guilda s timto jmenem uz existuje.");
				}
			}
			D_GuildMenu.Show(guild, player);
		}
	}

	/// <summary>d_guild_showGuildMembers: everyone online, adepts marked. Erebor listed only those
	/// with the guild chat on; there's no such switch here.</summary>
	public class D_GuildOnline : CompiledGumpDef {
		private const int PageRows = 20, RowHeight = 18, FirstRow = 40, Hue = 2301;

		private static List<string[]> OnlineRows(Guild guild) {
			List<string[]> rows = new List<string[]>();
			foreach (Character member in guild.Members) {
				if (member.IsOnline) {
					string name = guild.IsCandidate(member) ? (member.Name + " [adept]") : member.Name;
					string region = (member.Region != null) ? member.Region.Name : "";
					rows.Add(new[] { name, member.X + "," + member.Y + "," + member.Z + "," + member.M, region });
				}
			}
			return rows;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<string[]> rows = OnlineRows((Guild) args[0]);
			this.SetLocation(55, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, 661, FirstRow + (PageRows * RowHeight) + 40, 2620);
			this.AddLabel(30, 12, 42, " Seznam on-line clenu guildy");
			int pages = System.Math.Max(1, (rows.Count + PageRows - 1) / PageRows);
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(270, FirstRow + (PageRows * RowHeight) + 8, 0x983, 0x984, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(300, FirstRow + (PageRows * RowHeight) + 8, 0x985, 0x986, 0, GumpButtonType.Page, page + 2);
				}
				int last = System.Math.Min(rows.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					int y = FirstRow + ((i % PageRows) * RowHeight);
					this.AddLabel(30, y, Hue, rows[i][0]);
					this.AddLabel(230, y, Hue, rows[i][1]);
					this.AddLabel(410, y, Hue, rows[i][2]);
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
