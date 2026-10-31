using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#883 - Erebor's d_banker_ok and d_banker_bank (speak_minas_tirith.scp).</summary>
	public static class BankerDialog {
		internal const int Greeting = 1, Gossip = 2, Examine = 3, Job = 4;
		private const int MaxServiceDistance = 6;

		/// <summary>f_banker_ok.</summary>
		public static void Open(Character banker, Player player) {
			if ((banker.Realm != Realm.None) && (banker.Realm != player.Realm)) {
				banker.Say("Jedes potvoro !!!!");
				return;
			}
			if ((player.FindLayer(LayerNames.Pants) == null) && (player.FindLayer(LayerNames.Robe) == null) && (player.FindLayer(LayerNames.Skirt) == null)) {
				banker.Say("Tahni uchyle !!!!");
				return;
			}
			Show(banker, player, Greeting, player.X, player.Y);
		}

		/// <summary>startX/Y stand in for e_vendor_check: where the talk began, for its whole length.</summary>
		internal static void Show(Character banker, Player player, int page, int startX, int startY) {
			player.Dialog(player, SingletonScript<D_BankerOk>.Instance, new DialogArgs(banker, page, startX, startY));
		}

		internal static long Balance(Character player) {
			return GoldUtils.CountGoldIn(player.Bank);
		}

		internal static string Title(Character banker) {
			return banker.Name + ", banker mesta " + ((banker.Region != null) ? banker.Region.Name : "");
		}

		/// <summary>e_vendor_check and the distance: what the service buttons ask first.</summary>
		internal static bool CanServe(Character banker, Player player, int startX, int startY) {
			if ((player.X != startX) || (player.Y != startY)) {
				player.RedMessage("Behem hovoru se nesmis hybat !");
				return false;
			}
			if (banker.IsDeleted || (Point2D.GetSimpleDistance(banker, player) > MaxServiceDistance)) {
				player.RedMessage("Jsi prilis daleko od bankere !!");
				return false;
			}
			return true;
		}

		/// <summary>"Proplatit upisy": every cheque on the player goes to the bank.</summary>
		internal static void CashChecks(Character banker, Player player) {
			List<CashiersItem> checks = FindChecks(player);
			if (checks.Count == 0) {
				banker.Say("Nemas u sebe zadny sek.");
				return;
			}
			foreach (CashiersItem check in checks) {
				if (check.Value <= 0) {
					check.Delete();
					banker.Say("Bohuzel, sek nebyl kryty zadnym zlatem.");
				} else if ((check.Value > CashiersItem.MaxCheck) && (check.CheckValue == 0)) {
					player.RedMessage("Tvoj sek ma prilis velku hodnotu a je stary, nemozem ho preplatit. Napis si page a bude vymeneny za novy.");
					return;
				} else if ((check.Value > CashiersItem.MaxCheck) && (check.Value != check.CheckValue)) {
					player.RedMessage("Tvoj sek je bugly, pis page!");
					return;
				} else {
					GoldUtils.GrantToBank(player, (uint) check.Value);
					player.Sound(0x37);
					player.SysMessage("Do banky bylo vlozeno " + check.Value.ToString(CultureInfo.InvariantCulture) + " gp.");
					check.Delete();
				}
			}
			banker.Say("Rad jsem poslouzil.");
		}

		/// <summary>The older *sek* speech of spk_jobbanker: one cheque, in its own words.</summary>
		internal static void CashOneCheck(Character banker, Character speaker) {
			List<CashiersItem> checks = FindChecks(speaker);
			if (checks.Count == 0) {
				banker.Say("Nemas u sebe zadny sek");
				return;
			}
			CashiersItem check = checks[0];
			if (check.Value <= 0) {
				banker.Say("Bohuzel, sek nebyl kryty zadnym zlatem");
			} else {
				GoldUtils.GrantToBank(speaker, (uint) check.Value);
				speaker.Sound(0x37);
				speaker.SysMessage("Bylo ti proplaceno " + check.Value.ToString(CultureInfo.InvariantCulture) + " gp.");
			}
			check.Delete();
			banker.Say("Rad jsem poslouzil");
		}

		private static List<CashiersItem> FindChecks(Character ch) {
			List<CashiersItem> found = new List<CashiersItem>();
			CollectChecks(ch.Backpack, found);
			CollectChecks(ch.Bank, found);
			return found;
		}

		private static void CollectChecks(AbstractItem cont, List<CashiersItem> found) {
			foreach (AbstractItem item in cont) {
				CashiersItem cashiers = item as CashiersItem;
				if ((cashiers != null) && cashiers.IsCheck) {
					found.Add(cashiers);
				} else if (item.IsContainer) {
					CollectChecks(item, found);
				}
			}
		}
	}

	public class D_BankerOk : CompiledGumpDef {
		private static readonly int[] buttonX = { 160, 325, 160, 490 };
		private static readonly int[] buttonY = { 330, 330, 360, 330 };
		private static readonly int[] labelIndent = { 40, 40, 45, 48 };
		private static readonly string[] labels = { "Bankovni sluzby", "Nejake drby ?", "Hledam praci", "Prohlednout.." };

		private static string Text(Character banker, Player player, int page) {
			long balance = BankerDialog.Balance(player);
			switch (page) {
				case BankerDialog.Examine:
					if (balance >= 1000000) {
						return new[] { "Banker se ti zda az trapne servilini.", "V soustredenem vyrazu bankere lze jasne vycist hlad po tvych penezich.",
							"Je k nevire, ze cloveku tak nizkeho puvodu, je dovoleno se starat o tvoje penize." }[Globals.dice.Next(3)];
					}
					if (balance > 50000) {
						return new[] { "Banker pusobi solidnim dojmem a diva se na tebe s uctivym zajmem.", "Typicky banker. Formalni a nudny.",
							"Banker zkusenym zrakem odhaduje vysi tveho majetku." }[Globals.dice.Next(3)];
					}
					return new[] { "Pod jeho odsuzujicim pohledem si pripadas jak posledni zebrak.",
						"Banker pusobi dojmem nafoukaneho urednika a zhlizi na tebe se spatne skryvanym despektem.",
						"V tehle bance zrejme nemaji moc zajem o drobne stradatele." }[Globals.dice.Next(3)];
				case BankerDialog.Job:
					if ((player.Realm == Realm.Gondor) && (banker.Realm == Realm.Gondor)) {
						return "Pokud mas odvahu, hledej praci u zmocnence Gondoru. Lide v jeho sluzbach uspesni, najdou uplatneni i pri nasich delikatnich zakazkach.";
					}
					return "Pro praci v bance je nutna vysoka kvalifikace, duveryhodnost a smysl pro loajalitu.";
				default:
					return "O cem presne chces hovorit ?";
			}
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character banker = (Character) args[0];
			Player player = (Player) sendTo;
			int page = (int) args[1];
			this.SetLocation(5, 5);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, 0x3000);
			this.AddLabel(220, 210, 52, BankerDialog.Title(banker));
			this.AddHtml(170, 230, 440, 100, "<basefont color=#ffffff>" + Text(banker, player, page), false, true);
			for (int i = 0; i < labels.Length; i++) {
				this.AddButton(buttonX[i], buttonY[i], 0x988, 0x988, i + 1, GumpButtonType.Reply, 0);
				this.AddLabel(buttonX[i] + labelIndent[i], buttonY[i] + 2, 1152, labels[i]);
			}
			if (page == BankerDialog.Gossip) {
				this.AddLabel(60, 395, 1152, "Kdepak " + (player.IsFemale ? "pani" : "pane") + ". Banka je seriozni instituce, zadne drby zde netrpime.");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Character banker = (Character) args[0];
			Player player = (Player) gi.Cont;
			if (banker.IsDeleted) {
				return;
			}
			int startX = (int) args[2], startY = (int) args[3];
			switch (gr.PressedButton) {
				case 1:
					player.Dialog(player, SingletonScript<D_BankerBank>.Instance, new DialogArgs(banker, startX, startY));
					break;
				case 2:
					BankerDialog.Show(banker, player, BankerDialog.Gossip, startX, startY);
					break;
				case 3:
					BankerDialog.Show(banker, player, BankerDialog.Job, startX, startY);
					break;
				case 4:
					BankerDialog.Show(banker, player, BankerDialog.Examine, startX, startY);
					break;
			}
		}
	}

	public class D_BankerBank : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character banker = (Character) args[0];
			Player player = (Player) sendTo;
			long balance = BankerDialog.Balance(player);
			string amount = "Vase hotovost v bance cini " + balance.ToString(CultureInfo.InvariantCulture) + " zlatych.";
			string text;
			if (balance >= 1000000) {
				text = (player.IsFemale ? "Vzacna pani" : "Velevazeny pane") + " " + player.Name + ", jmenem nasi banky si dovoluji Vam nabidnout nase skromne sluzby.<br>" + amount;
			} else if (balance > 50000) {
				text = amount;
			} else {
				text = "A jeje. " + (player.IsFemale ? "Pani" : "Pan") + " z Nemanic.<br>" + amount;
			}
			this.SetLocation(5, 5);
			this.AddBackground(20, 200, 640, 350, 2620);
			this.AddImage(30, 210, 0x3182);
			this.AddImage(40, 215, 0x3000);
			this.AddLabel(220, 210, 52, BankerDialog.Title(banker));
			this.AddHtml(170, 230, 440, 100, "<basefont color=#ffffff>" + text, false, true);
			this.AddButton(25, 415, 0xfa5, 0xfa7, 1, GumpButtonType.Reply, 0);
			this.AddLabel(60, 415, 152, "Otevrit banku.");
			this.AddButton(25, 435, 0xfa5, 0xfa7, 2, GumpButtonType.Reply, 0);
			this.AddLabel(60, 435, 152, "Proplatit upisy.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Character banker = (Character) args[0];
			Player player = (Player) gi.Cont;
			if (gr.PressedButton == 0) {
				if (!banker.IsDeleted) {
					BankerDialog.Show(banker, player, BankerDialog.Greeting, (int) args[1], (int) args[2]);
				}
				return;
			}
			if (!BankerDialog.CanServe(banker, player, (int) args[1], (int) args[2])) {
				return;
			}
			if (gr.PressedButton == 1) {
				player.Bank.OpenBankTo(player);
			} else {
				BankerDialog.CashChecks(banker, player);
			}
		}
	}
}
