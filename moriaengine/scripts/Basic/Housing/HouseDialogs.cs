using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - d_house_menu. Buttons keep Erebor's numbers, a rank above the one in the table shows them.</summary>
	public class D_House : CompiledGumpDef {
		private const int Width = 290, RowHeight = 22, LabelHue = 955, ButtonHue = 2301;
		private const int AddFriend = 3, ListFriends = 4, AddCoOwner = 5, ListCoOwners = 6, Rename = 7, ChangeSign = 8, Transfer = 10;
		private const int Block = 11, Empty = 12, ResetPaid = 13, SetRent = 14, AutoRent = 15;

		public static void Show(Player player, House house, Item sign) {
			player.Dialog(player, SingletonScript<D_House>.Instance, new DialogArgs(house, sign));
		}

		private static string RankName(HouseRank rank) {
			switch (rank) {
				case HouseRank.Friend:
					return "Pritel";
				case HouseRank.CoOwner:
					return "Spoluvlastnik";
				case HouseRank.Owner:
					return "Vlastnik";
				case HouseRank.GM:
					return "GM";
				default:
					return "Verejnost";
			}
		}

		private static List<KeyValuePair<int, string>> Entries(House house, HouseRank rank) {
			List<KeyValuePair<int, string>> entries = new List<KeyValuePair<int, string>>();
			void Add(int button, HouseRank above, string text) {
				if (rank > above) {
					entries.Add(new KeyValuePair<int, string>(button, text));
				}
			}
			Add(AddFriend, HouseRank.Friend, "Pridat pritele domu");
			Add(ListFriends, HouseRank.Public, "Seznam pratel domu");
			Add(AddCoOwner, HouseRank.CoOwner, "Pridat spoluvlastnika domu");
			Add(ListCoOwners, HouseRank.Public, "Seznam spoluvlastniku domu");
			Add(Rename, HouseRank.Friend, "Prejmenovat dum");
			Add(ChangeSign, HouseRank.Friend, "Zmenit vyvesku domu");
			Add(Transfer, HouseRank.CoOwner, "Predat vlastnictvi domu");
			Add(Block, HouseRank.Owner, (house.Blocked ? "Od" : "Za") + "blokovat dum");
			Add(Empty, HouseRank.Owner, "Vyprazdnit dum");
			Add(ResetPaid, HouseRank.Owner, "Vynulovat zaplaceni");
			Add(SetRent, HouseRank.Owner, "Nastavit najem");
			Add(AutoRent, HouseRank.Owner, (house.AutoRent ? "Vy" : "Za") + "pnout autorent");
			return entries;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			House house = (House) args[0];
			Item sign = (Item) args[1];
			Player viewer = (Player) sendTo;
			HouseRank rank = house.RankOf(viewer);
			string title = "Dum cislo " + house.Number + "<br>" + house.HouseName;

			this.SetLocation(30, 0);
			this.AddPage(0);
			if (rank < t_housemenu.LevelOf(sign)) { //only the plaque
				this.AddImage(100, 100, 100);
				this.AddHtml(120, 120, 100, 60, title, false, false);
				return;
			}
			if (house.Blocked) {
				viewer.RedMessage("Dum je zablokovan! Pro vice informaci napis page.");
			}
			List<KeyValuePair<int, string>> entries = Entries(house, rank);
			this.AddBackground(0, 0, Width, 216 + (entries.Count * RowHeight), 2620);
			this.AddImage(14, 14, 100);
			this.AddHtml(34, 34, 100, 60, title, false, false);
			this.AddLabel(160, 16, LabelHue, "Majitel:");
			this.AddLabel(160, 36, LabelHue, (house.Owner == null) ? "nikdo" : house.Owner.Name);
			this.AddLabel(160, 66, LabelHue, "Tvuj status:");
			this.AddLabel(160, 86, LabelHue, RankName(rank));
			this.AddLabel(14, 154, LabelHue, "Najem: " + house.Rent + " gp");
			this.AddLabel(14, 174, LabelHue, "Zaplaceno na " + house.PaidDays + " dni");
			int y = 204;
			foreach (KeyValuePair<int, string> entry in entries) {
				this.AddButton(14, y, 4005, 4007, entry.Key, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, ButtonHue, entry.Value);
				y += RowHeight;
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			House house = (House) args[0];
			Item sign = (Item) args[1];
			Player player = (Player) gi.Cont;
			if (house.IsDeleted) {
				return;
			}
			switch (gr.PressedButton) {
				case AddFriend:
					this.AskFor(player, house, HouseRank.Friend, HouseRank.Friend, "pritelem");
					break;
				case ListFriends:
					if (house.Allows(player, HouseRank.Public)) {
						D_HouseList.Show(player, house, sign, false, 0);
					}
					break;
				case AddCoOwner:
					this.AskFor(player, house, HouseRank.CoOwner, HouseRank.CoOwner, "spoluvlastnikem");
					break;
				case ListCoOwners:
					if (house.Allows(player, HouseRank.Public)) {
						D_HouseList.Show(player, house, sign, true, 0);
					}
					break;
				case Rename:
					if (house.Allows(player, HouseRank.Friend)) {
						D_TownInput.Ask(player, "Jak se ma dum jmenovat?", house.HouseName ?? "", answer => {
							house.HouseName = answer;
						});
					}
					break;
				case ChangeSign:
					if (house.Allows(player, HouseRank.Friend)) {
						D_HouseSignStyle.Show(player, house, sign, 1);
					}
					break;
				case Transfer:
					this.AskFor(player, house, HouseRank.CoOwner, HouseRank.Owner, "vlastnikem");
					break;
				case Block:
					if (house.Allows(player, HouseRank.Owner)) {
						house.Blocked = !house.Blocked;
						if (house.Blocked) {
							player.RedMessage("Dum zablokovan.");
						} else {
							player.SysMessage("Dum odblokovan.");
						}
					}
					break;
				case Empty:
					if (house.Allows(player, HouseRank.Owner)) {
						D_TownConfirm.Ask(player, "Opravdu chces vyklidit veci a obyvatele domu?", house.Empty);
					}
					break;
				case ResetPaid:
					if (house.Allows(player, HouseRank.Owner)) {
						D_TownConfirm.Ask(player, "Opravdu chces vynulovat najem?", () => house.PaidUntil = Globals.TimeInSeconds);
					}
					break;
				case SetRent:
					if (house.Allows(player, HouseRank.Owner)) {
						D_TownInput.Ask(player, "kolik ma byt najem?", house.Rent.ToString(System.Globalization.CultureInfo.InvariantCulture), answer => {
							int rent;
							if (int.TryParse(answer, out rent)) {
								house.SetRent(rent);
							}
						});
					}
					break;
				case AutoRent:
					if (house.Allows(player, HouseRank.Owner)) {
						house.AutoRent = !house.AutoRent;
						player.SysMessage("Automaticky najem " + (house.AutoRent ? "zapnut." : "vypnut."));
					}
					break;
			}
		}

		private void AskFor(Player player, House house, HouseRank above, HouseRank made, string what) {
			if (house.Allows(player, above)) {
				player.SysMessage("Koho chces udelat " + what + "?");
				player.Target(SingletonScript<Targ_HouseMember>.Instance, new object[] { house, made });
			}
		}
	}

	/// <summary>d_hmseznam: friends or co-owners. A line's button takes that person's place in the house away.</summary>
	public class D_HouseList : CompiledGumpDef {
		private const int Width = 250, Rows = 10, RowHeight = 22, Hue = 2301, PreviousButton = 1, NextButton = 2, RemoveButton = 100;

		public static void Show(Player player, House house, Item sign, bool coOwners, int first) {
			player.Dialog(player, SingletonScript<D_HouseList>.Instance, new DialogArgs(house, sign, coOwners, first));
		}

		private static List<Character> Members(DialogArgs args) {
			House house = (House) args[0];
			return (bool) args[2] ? house.CoOwners : house.Friends;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<Character> members = Members(args);
			int first = (int) args[3];
			int last = System.Math.Min(first + Rows, members.Count);

			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 300, 2620);
			this.AddLabel(14, 12, Hue, ((bool) args[2] ? "spoluvlastniku" : "pratel") + " : " + members.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			int y = 40;
			for (int i = first; i < last; i++) {
				this.AddButton(14, y, 4005, 4007, RemoveButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, Hue, members[i].Name);
				y += RowHeight;
			}
			if (first > 0) {
				this.AddButton(Width - 30, 40, 0xfa, 0xfb, PreviousButton, GumpButtonType.Reply, 0);
			}
			if (members.Count > last) {
				this.AddButton(Width - 30, 262, 0xfc, 0xfd, NextButton, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			House house = (House) args[0];
			Item sign = (Item) args[1];
			bool coOwners = (bool) args[2];
			int first = (int) args[3];
			Player player = (Player) gi.Cont;
			if (house.IsDeleted) {
				return;
			}
			switch (gr.PressedButton) {
				case 0:
					D_House.Show(player, house, sign);
					break;
				case PreviousButton:
					Show(player, house, sign, coOwners, System.Math.Max(0, first - Rows));
					break;
				case NextButton:
					Show(player, house, sign, coOwners, first + Rows);
					break;
				default:
					int index = gr.PressedButton - RemoveButton;
					List<Character> members = Members(args);
					if ((index < members.Count) && house.Allows(player, coOwners ? HouseRank.CoOwner : HouseRank.Friend)) {
						Character member = members[index];
						house.RemoveMember(member);
						member.RedMessage("Byl jsi zbaven prav v domu " + house.Title);
						Show(player, house, sign, coOwners, first);
					}
					break;
			}
		}
	}

	/// <summary>d_house_menu_sign_1 to 3: a button is its sign's model.</summary>
	public class D_HouseSignStyle : CompiledGumpDef {
		private const int PreviousButton = 1, NextButton = 2, FirstModel = 2979, LastPage = 3;
		private static readonly int[] GroupButtonX = { 40, 210, 380 };

		public static void Show(Player player, House house, Item sign, int page) {
			player.Dialog(player, SingletonScript<D_HouseSignStyle>.Instance, new DialogArgs(house, sign, page));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int page = (int) args[2];
			this.SetLocation(0, 0);
			this.AddPage(0);
			this.AddBackground(0, 130, 500, 250, 2620);
			if (page == LastPage) {
				this.AddGroup(FirstModel + 84, 0, 6);
				this.AddGroup(FirstModel + 96, 2, 6);
			} else {
				int start = FirstModel + ((page - 1) * 42);
				this.AddGroup(start, 0, 7);
				this.AddGroup(start + 14, 1, 7);
				this.AddGroup(start + 28, 2, 7);
			}
			this.AddButton(175, 340, 2466, 2467, PreviousButton, GumpButtonType.Reply, 0);
			this.AddButton(260, 340, 2469, 2470, NextButton, GumpButtonType.Reply, 0);
		}

		private void AddGroup(int model, int group, int rows) {
			int x = GroupButtonX[group];
			for (int row = 0; row < rows; row++) {
				int y = 155 + (row * 25);
				this.AddButton(x, y, 2103, 2104, model + (row * 2), GumpButtonType.Reply, 0);
				this.AddItem(x + 10, y - 5, model + (row * 2));
				this.AddItem(x + 35, y - 5, model + (row * 2) + 1);
				this.AddButton(x + 75, y, 2103, 2104, model + (row * 2) + 1, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			House house = (House) args[0];
			Item sign = (Item) args[1];
			int page = (int) args[2];
			Player player = (Player) gi.Cont;
			if (house.IsDeleted || sign.IsDeleted) {
				return;
			}
			switch (gr.PressedButton) {
				case 0:
					break;
				case PreviousButton:
					if (page == 1) {
						D_House.Show(player, house, sign);
					} else {
						Show(player, house, sign, page - 1);
					}
					break;
				case NextButton:
					Show(player, house, sign, (page == LastPage) ? 1 : (page + 1));
					break;
				default:
					if (house.Allows(player, HouseRank.Friend)) {
						sign.Model = gr.PressedButton;
					}
					break;
			}
		}
	}
}
