using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class CashiersItemDef {
	}

	/// <summary>#883 - Erebor's money items (seky.scp): a cheque, which only a banker cashes, and
	/// the purse and strongbox, which hold gold without its weight. MaxAmount 0 is a cheque.</summary>
	[ViewableClass]
	public partial class CashiersItem {
		private static readonly TagKey valueTK = TagKey.Acquire("_cashiersValue_");
		private static readonly TagKey checkValueTK = TagKey.Acquire("_checkValue_");
		internal const string CheckDef = "i_cashiers_check", PurseDef = "i_cashiers_mesec", StrongboxDef = "i_cashiers_pokladnicka";
		internal const int MinCheck = 5000, MaxCheck = 10000000, MinPurse = 500;
		private const int GoldSound = 0x37;

		/// <summary>Sphere's more1.</summary>
		public int Value {
			get { return Convert.ToInt32(this.GetTag(valueTK) ?? 0); }
			set { this.SetTag(valueTK, value); }
		}

		/// <summary>tag.check_value: what .check wrote the cheque for.</summary>
		public int CheckValue {
			get { return Convert.ToInt32(this.GetTag(checkValueTK) ?? 0); }
			set { this.SetTag(checkValueTK, value); }
		}

		public bool IsCheck {
			get { return this.TypeDef.MaxAmount <= 0; }
		}

		private bool IsStrongbox {
			get { return this.Def == ThingDef.GetByDefname(StrongboxDef); }
		}

		private bool IsCarriedBy(AbstractCharacter ch) {
			return this.TopObj() == ch;
		}

		/// <summary>f_what_is_this_container's 1, in a bank box rather than on the character.</summary>
		private bool IsInBank() {
			Thing thing = this;
			while (thing.Cont is Item) {
				thing = thing.Cont;
			}
			return (thing.Cont is Character) && (((Item) thing).Layer == (int) LayerNames.Bankbox);
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			PacketSequences.SendNameFrom(clickerConn, this, "(" + this.Value.ToString(CultureInfo.InvariantCulture) + " gp)", 0);
		}

		public override void On_DClick(AbstractCharacter from) {
			Character ch = (Character) from;
			if (this.IsCheck) {
				ch.SysMessage("Proplatit sek ti muze jen banker");
			} else if (this.IsCarriedBy(ch)) {
				ch.Dialog(ch, SingletonScript<D_CashiersPurse>.Instance, new DialogArgs(this));
			} else if (this.IsStrongbox) {
				ch.SysMessage("Zde nelze pokladnici vyprazdnit.");
			} else {
				ch.RedMessage("Zde nelze mesec vyprazdnit.");
			}
		}

		/// <summary>@stackOn. The dropped item already lies next to this one.</summary>
		public override void On_PutItemOn(ItemOnItemArgs args) {
			if (this.IsCheck) {
				return;
			}
			Character ch = (Character) args.PuttingChar;
			Item dropped = (Item) args.ManipulatedItem;
			CashiersItem purse = dropped as CashiersItem;
			bool isGold = dropped.Def == GoldUtils.GoldDef;
			if (!isGold && ((purse == null) || purse.IsCheck)) {
				dropped.Cont = ch.Backpack;
				ch.SysMessage(this.IsStrongbox ? "Tohle je pokladnice na penize!" : "Tohle je mesec na penize!");
			} else if (!this.IsCarriedBy(ch)) {
				dropped.Cont = ch.Backpack;
				ch.RedMessage(isGold ? "Mesec i penize musis mit u sebe" : "Oba mesce musis mit u sebe");
			} else if (isGold) {
				this.AddGold(ch, dropped);
			} else {
				this.PourIn(ch, purse);
			}
		}

		private void AddGold(Character ch, Item gold) {
			int added = gold.Amount;
			int total = this.Value + added;
			gold.Delete();
			if (total <= this.TypeDef.MaxAmount) {
				this.Value = total;
				ch.SysMessage((this.IsStrongbox ? "Do pokladnice" : "Do mesce") + " bylo vlozeno " + added.ToString(CultureInfo.InvariantCulture) + " gp.");
			} else if (this.IsStrongbox) {
				CreatePurses(ch, total, ch.Backpack);
				ch.SysMessage("Pokladnice je plna.");
				this.Delete();
			} else {
				CreatePurses(ch, total, this.Cont);
				this.Delete();
			}
		}

		private void PourIn(Character ch, CashiersItem other) {
			int total = this.Value + other.Value;
			other.Delete();
			if (total <= this.TypeDef.MaxAmount) {
				this.Value = total;
			} else {
				CreatePurses(ch, total, this.Cont);
				this.Delete();
			}
			ch.SysMessage((ch.IsFemale ? "Presypala" : "Presypal") + " jsi penize.");
		}

		/// <summary>f_vytvor_mesec: full strongboxes, then the smallest that holds the rest.</summary>
		internal static void CreatePurses(Character ch, int total, Thing cont) {
			int strongboxMax = ((CashiersItemDef) ThingDef.GetByDefname(StrongboxDef)).MaxAmount;
			int purseMax = ((CashiersItemDef) ThingDef.GetByDefname(PurseDef)).MaxAmount;
			while (total > strongboxMax) {
				CreateOne(StrongboxDef, strongboxMax, cont);
				total -= strongboxMax;
			}
			CreateOne((total <= purseMax) ? PurseDef : StrongboxDef, total, cont);
			ch.Sound(GoldSound);
		}

		private static CashiersItem CreateOne(string defname, int value, Thing cont) {
			CashiersItem item = (CashiersItem) ThingDef.GetByDefname(defname).Create(cont);
			item.Value = value;
			return item;
		}

		internal static CashiersItem CreateCheck(Character owner, int value) {
			CashiersItem check = CreateOne(CheckDef, value, owner.Backpack);
			check.CheckValue = value;
			return check;
		}

		/// <summary>d_cashiers_mesec's OK.</summary>
		internal void Split(Character ch, int amount) {
			if (!this.IsCarriedBy(ch)) {
				ch.RedMessage("Predmet musis mit u sebe!");
			} else if ((amount < MinPurse) || ((this.Value - amount) < MinPurse)) {
				ch.RedMessage("Na tuto hodnotu penize nerozdelis.");
			} else {
				CreatePurses(ch, amount, this.Cont);
				this.Value -= amount;
			}
		}

		/// <summary>d_cashiers_mesec's All: coins weigh, so someone overloaded takes what they can
		/// lift. Erebor's "+500 if under 500 stays" made gold from nothing and a negative purse;
		/// here 500 stays.</summary>
		internal void Empty(Character ch) {
			if (!this.IsCarriedBy(ch)) {
				ch.RedMessage("Predmet musis mit u sebe!");
				return;
			}
			int capacity = 20 + (4 * ch.Str);
			int weight = (int) MovementStamina.CarriedWeight(ch);
			if ((capacity < ((this.Value / 500) + weight)) && !this.IsInBank() && (this.Value > 1000)) {
				if (capacity <= (weight + 1)) {
					ch.RedMessage("Jsi " + (ch.IsFemale ? "pretizena" : "pretizeny") + ".");
					return;
				}
				int taken = Math.Min((capacity - weight) * 500, this.Value - MinPurse);
				GoldUtils.Grant(ch, (uint) taken);
				ch.Sound(GoldSound);
				this.Value -= taken;
				ch.SysMessage("Vice minci nepoberes.");
				return;
			}
			GoldUtils.GrantInto(this.Cont, (uint) this.Value);
			ch.Sound(GoldSound);
			ch.SysMessage((ch.IsFemale ? "Vysypala" : "Vysypal") + " jsi " + this.Value.ToString(CultureInfo.InvariantCulture) + " gp.");
			this.Delete();
		}
	}

	/// <summary>#883 - Erebor's money commands, .check from the bank and .mesec from the pack.</summary>
	public static class CashiersCommands {
		/// <summary>fixNumber: 0 for anything that isn't a plain number.</summary>
		internal static int ParseAmount(string text) {
			int amount;
			return int.TryParse((text ?? "").Trim().Trim('"', '(', ')'), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount) ? amount : 0;
		}

		[SteamFunction]
		public static void Check(Character self, ScriptArgs sa) {
			int sum = ParseAmount(sa.Args);
			if (sum <= 0) {
				self.SysMessage("Zadavej ve formatu .check(xx), kde xx je mnozstvi penez, ktere chces do seku vlozit.");
			} else if (sum < CashiersItem.MinCheck) {
				self.RedMessage("Hodnota seku je prilis mala. Minimalni hodnota je " + CashiersItem.MinCheck + " gp.");
			} else if (sum > CashiersItem.MaxCheck) {
				self.RedMessage("Hodnota seku je prilis velka. Maximalni hodnota je " + CashiersItem.MaxCheck + " gp.");
			} else if (GoldUtils.Pay(self, sum, PaySource.Bank, "V bance nemas dostatek penez. Potrebujes " + sum + " gp.")) {
				CashiersItem.CreateCheck(self, sum);
				self.SysMessage("Sek na " + sum + " gp byl umisten do tveho batohu.");
				self.Sound(0x4f);
			}
		}

		[SteamFunction]
		public static void Mesec(Character self, ScriptArgs sa) {
			bool all = (sa.Args == null) || (sa.Args.Trim().Trim('"').Length == 0);
			int sum = all ? (int) GoldUtils.CountGoldIn(self.Backpack) : ParseAmount(sa.Args);
			if (sum <= 0) {
				self.SysMessage("Zadavej ve formatu .mesec(xx), kde xx je mnozstvi penez, ktere chces do mesce vlozit.");
			} else if (sum < CashiersItem.MinPurse) {
				self.RedMessage("Takto maly obnos nelze umistit do mesce");
			} else if (GoldUtils.Pay(self, sum, PaySource.Pack)) {
				CashiersItem.CreatePurses(self, sum, self.Backpack);
			}
		}
	}

	/// <summary>#883 - d_cashiers_mesec.</summary>
	public class D_CashiersPurse : CompiledGumpDef {
		private const int OkButton = 1, AllButton = 2;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			CashiersItem item = (CashiersItem) args[0];
			this.SetLocation(290, 280);
			this.AddPage(0);
			this.AddBackground(0, 0, 220, 110, 2620);
			this.AddLabel(18, 12, 0x481, "Kolik gp chces vyjmout ?");
			this.AddTextEntry(18, 35, 198, 20, 52, 1, item.Value.ToString(CultureInfo.InvariantCulture));
			this.AddButton(12, 72, 0xfb7, 0xfb9, OkButton, GumpButtonType.Reply, 0);
			this.AddButton(45, 72, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddButton(182, 72, 4005, 4007, AllButton, GumpButtonType.Reply, 0);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			CashiersItem item = (CashiersItem) args[0];
			Character ch = (Character) gi.Cont;
			if (gr.PressedButton == 0) {
				ch.SysMessage((ch.IsFemale ? "Stornovala" : "Stornoval") + " jsi volbu.");
			} else if (item.IsDeleted) {
				ch.RedMessage("Predmet musis mit u sebe!");
			} else if (gr.PressedButton == OkButton) {
				item.Split(ch, CashiersCommands.ParseAmount(gr.GetTextResponse(1)));
			} else if (gr.PressedButton == AllButton) {
				item.Empty(ch);
			}
		}
	}
}
