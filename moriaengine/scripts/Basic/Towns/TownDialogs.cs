using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	public static class TownDialogs {
		/// <summary>f_zbyva_casu</summary>
		public static string TimeLeft(double until) {
			TimeSpan left = TimeSpan.FromSeconds(Math.Max(0, until - Globals.TimeInSeconds));
			return left.Days + " dni " + left.Hours + " hodin";
		}
	}

	/// <summary>d_potvrdit for whatever needs a yes.</summary>
	public class D_TownConfirm : CompiledGumpDef {
		private const int YesButton = 1;

		public static void Ask(Player player, string question, Action onYes) {
			player.Dialog(player, SingletonScript<D_TownConfirm>.Instance, new DialogArgs(question, onYes));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 150, 2620);
			this.AddLabel(14, 12, 0x481, "Potvrzeni prikazu");
			this.AddHtml(14, 40, 270, 60, "<basefont color=#ffffff>" + (string) args[0], false, false);
			this.AddButton(14, 112, 0xfb7, 0xfb9, YesButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, 114, 0x481, "Ano");
			this.AddButton(220, 112, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(256, 114, 0x481, "Ne");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == YesButton) {
				((Action) args[1])();
			}
		}
	}

	/// <summary>input(...)</summary>
	public class D_TownInput : CompiledGumpDef {
		private const int OkButton = 1, TextEntryId = 1;

		public static void Ask(Player player, string question, string current, Action<string> onAnswer) {
			player.Dialog(player, SingletonScript<D_TownInput>.Instance, new DialogArgs(question, current, onAnswer));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 110, 2620);
			this.AddLabel(14, 12, 0x481, (string) args[0]);
			this.AddTextEntry(14, 40, 270, 20, 52, TextEntryId, (string) args[1]);
			this.AddButton(14, 72, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			string text = (gr.GetTextResponse(TextEntryId) ?? "").Trim();
			if ((gr.PressedButton == OkButton) && (text.Length > 0)) {
				((Action<string>) args[2])(text);
			}
		}
	}

	/// <summary>d_town_menu: the lines depend on who's looking. The button numbers are Erebor's.</summary>
	public class D_TownMenu : CompiledGumpDef {
		internal const int CitizensButton = 1, BlacklistButton = 2, ElectionButton = 3, PayTaxButton = 4, StandButton = 5, TaxStatusButton = 6, AddBanButton = 12, SetTaxButton = 14, MayorButton = 15,
			ExpelButton = 16, CaptainButton = 13, PostsButton = 19, RecruiterButton = 20, RemoveRingButton = 26, MakeNazgulButton = 27, MakeMayorButton = 17, MakeCitizenButton = 18, SuggestedBansButton = 21, NewsButton = 23, HomeButton = 24, CitizenshipButton = 25;
		private const int Hue = 955, LabelHue = 0x481, RowHeight = 20, Width = 280, FirstRow = 176;
		private static readonly string[] rankNames = { "Nepritel", "Verejnost", "Obcan", "Starosta", "GM" };

		internal static void Show(Town town, Player player) {
			player.Dialog(player, SingletonScript<D_TownMenu>.Instance, new DialogArgs(town));
		}

		private static List<KeyValuePair<int, string>> Lines(Town town, Player player) {
			int rank = town.GetRank(player);
			List<KeyValuePair<int, string>> lines = new List<KeyValuePair<int, string>>();
			void Line(int neededRank, int button, string text) {
				if (rank >= neededRank) {
					lines.Add(new KeyValuePair<int, string>(button, text));
				}
			}
			if (town.Realm == Realm.Mordor) { //d_nazg_menu: the shrine opens this only to a Nazgul
				Line(Town.RankPublic, BlacklistButton, "Blacklist");
				Line(Town.RankMayor, SuggestedBansButton, "Hraci navrzeni na blacklist");
				Line(Town.RankMayor, AddBanButton, "Pridat hrace na blacklist");
				Line(Town.RankPublic, NewsButton, "Novinky");
				Line(Town.RankMayor, HomeButton, "Zmenit home");
				Line(Town.RankMayor, CaptainButton, "Pokyny strazim");
				Line(Town.RankMayor, PostsButton, "Rozmistit straze");
				Line(Town.RankMayor, RecruiterButton, "Najimat straze");
				Line(Town.RankGm, RemoveRingButton, "Zbavit nazgula prstenu");
				Line(Town.RankGm, MakeNazgulButton, "Udelat nazgulem");
				return lines;
			}
			Line(Town.RankPublic, CitizensButton, "Seznam obcanu");
			Line(Town.RankPublic, BlacklistButton, "Blacklist");
			Line(Town.RankPublic, HomeButton, "Nastavit home");
			Line(Town.RankMayor, SuggestedBansButton, "Hraci navrzeni na Blacklist");
			Line(Town.RankCitizen, ElectionButton, "Volby starosty");
			Line(Town.RankCitizen, PayTaxButton, "Zaplatit dane");
			Line(Town.RankCitizen, StandButton, (town.FindCandidate(player) != null) ? "Zrusit svou kandidaturu" : "Kandidovat na starostu");
			Line(Town.RankCitizen, TaxStatusButton, "Stav platby dani");
			Line(Town.RankCitizen, NewsButton, "Novinky");
			Line(Town.RankMayor, AddBanButton, "Pridat hrace na blacklist");
			Line(Town.RankMayor, SetTaxButton, "Nastavit dane");
			Line(Town.RankMayor, MayorButton, player.IsGM ? "Zbavit starostu uradu" : "Slozit funkci");
			Line(Town.RankGm, ExpelButton, "Zbavit obcanstvi");
			Line(Town.RankGm, MakeMayorButton, "Udelat starostou");
			Line(Town.RankGm, MakeCitizenButton, "Udelat obcanem");
			Line(Town.RankPublic, CitizenshipButton, (town.FindCitizen(player) != null) ? "Vzdat se obcanstvi" : "Stan se obcanem");
			RealmOffice office = RealmOffice.For(Realm.Gondor);
			if ((rank >= Town.RankMayor) || ((office != null) && (office.holder == player))) {
				Line(Town.RankPublic, CaptainButton, "Pokyny strazim");
			}
			Line(Town.RankMayor, PostsButton, "Rozmistit straze");
			Line(Town.RankMayor, RecruiterButton, "Najimat straze");
			return lines;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Town town = (Town) args[0];
			Player player = (Player) sendTo;
			List<KeyValuePair<int, string>> lines = Lines(town, player);

			this.SetLocation(30, 0);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (lines.Count * RowHeight) + 14, 2620);
			this.AddImage(10, 10, 100);
			this.AddHtml(30, 30, 100, 60, "Mesto " + town.Name, false, false);
			bool mordor = town.Realm == Realm.Mordor;
			this.AddLabel(150, 14, LabelHue, mordor ? "Nazgul:" : "Starosta:");
			this.AddLabel(150, 34, Hue, (town.mayor != null) ? town.mayor.Name : "");
			this.AddLabel(150, 54, LabelHue, "Tvuj status:");
			this.AddLabel(150, 74, Hue, (mordor && (town.GetRank(player) == Town.RankMayor)) ? "Nazgul" : rankNames[town.GetRank(player)]);
			if (!mordor) {
				this.AddLabel(14, 118, LabelHue, "Dane: " + town.tax + " gp");
			}
			this.AddLabel(14, 138, LabelHue, "Stav pokladny: " + town.treasury + " gp");

			int y = FirstRow;
			foreach (KeyValuePair<int, string> line in lines) {
				this.AddButton(14, y, 4005, 4007, line.Key, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, Hue, line.Value);
				y += RowHeight;
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Town town = (Town) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			int rank = town.GetRank(player);
			foreach (KeyValuePair<int, string> line in Lines(town, player)) {
				if (line.Key == button) {
					this.Press(town, player, button, rank);
					return;
				}
			}
		}

		private void Press(Town town, Player player, int button, int rank) {
			switch (button) {
				case CitizensButton:
					D_TownCitizens.Show(town, player, D_TownCitizens.ListMode.Citizens);
					break;
				case TaxStatusButton:
					D_TownCitizens.Show(town, player, D_TownCitizens.ListMode.Tax);
					break;
				case ExpelButton:
					D_TownCitizens.Show(town, player, D_TownCitizens.ListMode.Expel);
					break;
				case CaptainButton:
					D_GuardCaptain.Show(town.Force, player);
					break;
				case PostsButton:
					D_GuardPosts.Show(town.Force, player);
					break;
				case RecruiterButton:
					D_GuardRecruiter.Show(town.Force, player);
					break;
				case BlacklistButton:
					D_TownBans.Show(town, player, false);
					break;
				case SuggestedBansButton:
					D_TownBans.Show(town, player, true);
					break;
				case AddBanButton:
					player.SysMessage("Koho chces pridat na blacklist?");
					player.Target(SingletonScript<Targ_TownBan>.Instance, town);
					break;
				case ElectionButton:
					D_TownElection.Show(town, player);
					break;
				case StandButton:
					if (town.FindCandidate(player) != null) {
						D_TownConfirm.Ask(player, "Opravdu chces zrusit svou kandidaturu?", () => town.WithdrawCandidate(player));
					} else {
						D_TownConfirm.Ask(player, "Opravdu chces kandidovat na starostu", () => town.Stand(player));
					}
					break;
				case PayTaxButton:
					D_TownConfirm.Ask(player, "Prejes si zaplatit dan svemu mestu?", () => town.PayTax(player));
					break;
				case NewsButton:
					player.Dialog(player, SingletonScript<D_TownNews>.Instance, new DialogArgs(town));
					break;
				case SetTaxButton:
					D_TownInput.Ask(player, "Jake maji byt dane?", town.tax.ToString(System.Globalization.CultureInfo.InvariantCulture), text => {
						if (int.TryParse(text, out int amount)) {
							town.SetTax(player, amount);
						}
					});
					break;
				case MayorButton:
					if (player.IsGM) {
						D_TownConfirm.Ask(player, "Opravdu chces odstranit starostu?", () => {
							if (town.mayor == null) {
								player.SysMessage("Mesto nema zadneho starostu");
							} else {
								town.mayor.RedMessage("Byl jsi zbaven uradu starosty ve meste " + town.Name);
								town.ClearMayor();
							}
						});
					} else {
						D_TownConfirm.Ask(player, "Opravdu chces rezignovat?", () => {
							town.ClearMayor();
							player.RedMessage("Slozil jsi urad starosty ve meste " + town.Name);
						});
					}
					break;
				case MakeMayorButton:
					player.SysMessage("Koho chces udelat starostou?");
					player.Target(SingletonScript<Targ_TownMayor>.Instance, town);
					break;
				case MakeCitizenButton:
					player.SysMessage("Koho chces udelat obcanem?");
					player.Target(SingletonScript<Targ_TownCitizen>.Instance, town);
					break;
				case RemoveRingButton:
					if (town.mayor == null) {
						player.SysMessage("Neni Nazgul, ktery by vladl tomuto mestu");
					} else {
						Character nazgul = town.mayor;
						nazgul.RedMessage("Marghul rozhodl o odebrani tveho prstenu");
						NazgulRings.TakeAway(NazgulRings.RingOf(nazgul), nazgul);
					}
					break;
				case MakeNazgulButton:
					player.SysMessage("Koho chces udelat nazgulem?");
					player.Target(SingletonScript<Targ_MakeNazgul>.Instance, town);
					break;
				case HomeButton:
					Point4D home = StaticRegion.GetByDefname(RegionEventSystem.AreaOf(town.stone.Region).Defname).P;
					D_TownConfirm.Ask(player, "Prejes si zmenit domovske mesto ? (" + TownSettings.instance.homeChangeFee + "gp, pro obcany zdarma)",
						() => town.MoveHomeHere(player, home));
					break;
				case CitizenshipButton:
					if (town.FindCitizen(player) != null) {
						D_TownConfirm.Ask(player, "Opravdu se chces vzdat obcanstvi ve meste " + town.Name + "?", () => {
							town.RemoveCitizen(player);
							player.RedMessage("Jiz nejsi obcanem mesta " + town.Name + ".");
						});
					} else {
						D_TownConfirm.Ask(player, "Opravdu se chces stat obcanem tohoto mesta? Poplatek je " + TownSettings.instance.citizenshipFee + " zlatych",
							() => town.TryJoin(player));
					}
					break;
			}
		}
	}

	/// <summary>d_tmseznam's lists of citizens, a page at a time.</summary>
	public class D_TownCitizens : CompiledGumpDef {
		public enum ListMode { Citizens, Tax, Expel }

		private const int Width = 300, PageRows = 12, RowHeight = 20, FirstRow = 40, FirstRowButton = 100, TextHue = 2301;

		internal static void Show(Town town, Player player, ListMode mode) {
			player.Dialog(player, SingletonScript<D_TownCitizens>.Instance, new DialogArgs(town, mode));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Town town = (Town) args[0];
			ListMode mode = (ListMode) args[1];
			List<TownCitizen> citizens = town.citizens;
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (PageRows * RowHeight) + 20, 2620);
			this.AddLabel(14, 12, 0x481, "Obcanu: " + citizens.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			int pages = (citizens.Count + PageRows - 1) / PageRows;
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 30, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 30, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = Math.Min(citizens.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					this.AddRow(town, citizens[i], i, FirstRow + ((i % PageRows) * RowHeight), mode, (Character) sendTo);
				}
			}
		}

		private void AddRow(Town town, TownCitizen citizen, int index, int y, ListMode mode, Character viewer) {
			this.AddLabel(50, y, TextHue, citizen.who.Name);
			if (mode == ListMode.Expel) {
				this.AddButton(14, y, 4005, 4007, FirstRowButton + index, GumpButtonType.Reply, 0);
			} else if (mode == ListMode.Tax) {
				if (citizen.who == town.mayor) {
					this.AddLabel(200, y, TextHue, "Neplati");
				} else {
					this.AddLabel(200, y, citizen.paidTax ? TextHue : 0x22, citizen.paidTax ? (citizen.amountPaid + " gp") : "Neplaceno");
				}
				if (citizen.who == viewer) {
					this.AddButton(14, y, citizen.autoPay ? 0xd3 : 0xd2, citizen.autoPay ? 0xd2 : 0xd3, FirstRowButton + index, GumpButtonType.Reply, 0);
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Town town = (Town) args[0];
			ListMode mode = (ListMode) args[1];
			Player player = (Player) gi.Cont;
			if (gr.PressedButton == 0) {
				D_TownMenu.Show(town, player);
				return;
			}
			int index = gr.PressedButton - FirstRowButton;
			if ((index < 0) || (index >= town.citizens.Count)) {
				return;
			}
			TownCitizen citizen = town.citizens[index];
			if ((mode == ListMode.Tax) && (citizen.who == player)) {
				town.ToggleAutoPay(citizen);
				Show(town, player, mode);
			} else if ((mode == ListMode.Expel) && (town.GetRank(player) >= Town.RankGm)) {
				Character who = citizen.who;
				D_TownConfirm.Ask(player, "Opravdu chces zbavit " + who.Name + " obcanstvi?", () => {
					town.RemoveCitizen(who);
					who.RedMessage("Jiz nejsi obcanem mesta " + town.Name + ".");
				});
			}
		}
	}

	/// <summary>d_tmseznam's 3: the candidates and their votes, and a button by each to vote.</summary>
	public class D_TownElection : CompiledGumpDef {
		private const int FirstRowButton = 100, RowHeight = 20, FirstRow = 62, TextHue = 2301;

		internal static void Show(Town town, Player player) {
			player.Dialog(player, SingletonScript<D_TownElection>.Instance, new DialogArgs(town));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Town town = (Town) args[0];
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, FirstRow + (Math.Max(town.candidates.Count, 1) * RowHeight) + 20, 2620);
			this.AddLabel(14, 12, 0x481, "Volby starosty: " + town.candidates.Count);
			this.AddButton(255, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (town.candidates.Count > 0) {
				this.AddLabel(14, 36, TextHue, "Do konce voleb: " + TownDialogs.TimeLeft(town.electionStart + (TownSettings.instance.electionDays * 24 * 60 * 60)));
			}
			for (int i = 0; i < town.candidates.Count; i++) {
				int y = FirstRow + (i * RowHeight);
				this.AddButton(14, y, 4005, 4007, FirstRowButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, TextHue, town.candidates[i].who.Name);
				this.AddLabel(255, y, TextHue, town.candidates[i].votes.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Town town = (Town) args[0];
			Player player = (Player) gi.Cont;
			int index = gr.PressedButton - FirstRowButton;
			if (gr.PressedButton == 0) {
				D_TownMenu.Show(town, player);
			} else if ((index >= 0) && (index < town.candidates.Count)) {
				town.Vote(player, town.candidates[index]);
				Show(town, player);
			}
		}
	}

	/// <summary>d_tmseznam's blacklist and its suggestions. A mayor or GM removes a name from the blacklist;
	/// a suggestion the mayor takes (first button) or turns down (the second).</summary>
	public class D_TownBans : CompiledGumpDef {
		private const int Width = 620, PageRows = 12, RowHeight = 20, FirstRow = 40, FirstRowButton = 100, RejectButton = 1000, TextHue = 2301;

		internal static void Show(Town town, Player player, bool suggestions) {
			player.Dialog(player, SingletonScript<D_TownBans>.Instance, new DialogArgs(town, suggestions));
		}

		private static List<TownBan> List(Town town, bool suggestions) {
			return suggestions ? town.suggestedBans : town.banned;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Town town = (Town) args[0];
			bool suggestions = (bool) args[1];
			bool canEdit = town.GetRank((Character) sendTo) >= Town.RankMayor;
			List<TownBan> bans = List(town, suggestions);
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (PageRows * RowHeight) + 20, 2620);
			this.AddLabel(14, 12, 0x481, (suggestions ? "Navrzenych na blacklist: " : "Nepratel: ") + bans.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			int pages = (bans.Count + PageRows - 1) / PageRows;
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 25, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 25, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = Math.Min(bans.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					TownBan ban = bans[i];
					int y = FirstRow + ((i % PageRows) * RowHeight);
					if (canEdit) {
						this.AddButton(14, y, 4005, 4007, FirstRowButton + i, GumpButtonType.Reply, 0);
					}
					this.AddLabel(50, y, TextHue, ban.name);
					this.AddLabel(200, y, TextHue, "Duvod: " + ban.reason);
					if (suggestions) {
						this.AddLabel(420, y, TextHue, "Navrhl: " + ban.suggestedBy);
						if (canEdit) {
							this.AddButton(Width - 55, y, 0xfb1, 0xfb3, RejectButton + i, GumpButtonType.Reply, 0);
						}
					}
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Town town = (Town) args[0];
			bool suggestions = (bool) args[1];
			Player player = (Player) gi.Cont;
			List<TownBan> bans = List(town, suggestions);
			int button = gr.PressedButton;
			if (button == 0) {
				D_TownMenu.Show(town, player);
				return;
			}
			bool reject = button >= RejectButton;
			int index = button - (reject ? RejectButton : FirstRowButton);
			if ((index < 0) || (index >= bans.Count) || (town.GetRank(player) < Town.RankMayor)) {
				return;
			}
			TownBan ban = bans[index];
			bans.RemoveAt(index);
			if (!suggestions) {
				ban.who.SysMessage(ban.name + " byl odstranen z cerne listiny ve meste " + town.Name);
			} else if (reject) {
				player.SysMessage("Navrh zamitnut");
			} else {
				ban.suggestedBy = null;
				town.banned.Add(ban);
				player.SysMessage(ban.name + " uspesne pridan na cernou listinu");
				ban.who.RedMessage("Byl jsi dan na cernou listinu mesta " + town.Name);
			}
			Show(town, player, suggestions);
		}
	}

	/// <summary>The news of d_tmseznam and d_news_read: 3 at most, written by the mayor or a GM.</summary>
	public class D_TownNews : CompiledGumpDef {
		private const int MaxNews = 3, AddButtonId = 3, ReadButton = 100, RemoveButton = 1000, RowHeight = 24, FirstRow = 40;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Town town = (Town) args[0];
			bool canEdit = town.GetRank((Character) sendTo) >= Town.RankMayor;
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 150, 2620);
			this.AddLabel(14, 12, 0x481, "Novinky");
			this.AddButton(255, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			for (int i = 0; i < town.news.Count; i++) {
				int y = FirstRow + (i * RowHeight);
				this.AddButton(14, y, 4005, 4007, ReadButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, 2301, "Novinka cislo " + (i + 1));
				if (canEdit) {
					this.AddButton(255, y, 0xfb1, 0xfb3, RemoveButton + i, GumpButtonType.Reply, 0);
				}
			}
			if (canEdit) {
				this.AddButton(14, 120, 4005, 4007, AddButtonId, GumpButtonType.Reply, 0);
				this.AddLabel(50, 120, 955, "Nova novinka");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Town town = (Town) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			bool canEdit = town.GetRank(player) >= Town.RankMayor;
			if (button == 0) {
				D_TownMenu.Show(town, player);
			} else if ((button == AddButtonId) && canEdit) {
				if (town.news.Count >= MaxNews) {
					player.RedMessage("Nejdrive odstran nekterou ze starych novinek");
					player.Dialog(player, this, args);
				} else {
					D_TownInput.Ask(player, "Zapis novinku:", "", text => {
						town.news.Add(text);
						player.Dialog(player, this, new DialogArgs(town));
					});
				}
			} else if ((button >= RemoveButton) && canEdit) {
				if ((button - RemoveButton) < town.news.Count) {
					town.news.RemoveAt(button - RemoveButton);
					player.SysMessage("Smazal jsi novinku cislo " + (button - RemoveButton + 1));
				}
				player.Dialog(player, this, args);
			} else if ((button >= ReadButton) && ((button - ReadButton) < town.news.Count)) {
				player.Dialog(player, SingletonScript<D_TownNewsText>.Instance, new DialogArgs(town.news[button - ReadButton], button - ReadButton));
			}
		}
	}

	public class D_TownNewsText : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(20, 20);
			this.AddPage(0);
			this.AddBackground(0, 0, 400, 150, 2620);
			this.AddLabel(14, 12, 0x481, "Novinka cislo " + ((int) args[1] + 1));
			this.AddHtml(14, 40, 370, 96, "<basefont color=#ffffff>" + (string) args[0], false, false);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
