namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - a house's terminal to its visitor's own bank: paid for once, used from inside by the house's realm.</summary>
	public partial class House {
		private static readonly TagKey bankActivatedTK = TagKey.Acquire("_house_bank_activated_");

		public bool BankActivated {
			get {
				return this.GetTag(bankActivatedTK) != null;
			}
			set {
				if (value) {
					this.SetTag(bankActivatedTK, true);
				} else {
					this.RemoveTag(bankActivatedTK);
				}
			}
		}

		internal void UseBank(Player player, Item sign) {
			if (!this.BankActivated) {
				int price = HouseSettings.instance.bankActivationPrice;
				D_TownConfirm.Ask(player, "Skutecne chces aktivovat banku v tomto dome a za to zaplatit " + price + " gp?", () => {
					if (player.Pay(price)) {
						this.BankActivated = true;
						player.SysMessage("Domovni terminal aktivovan.");
						D_House.Show(player, this, sign);
					} else {
						player.SysMessage("Nemas dost penez.");
					}
				});
			} else if (player.Region != this.Area) {
				player.SysMessage("Musis byt uvnitr domu.");
			} else if (!player.MayEnterHouse(this.Area)) {
				player.SysMessage("K teto funkci nemas prava.");
			} else {
				player.Bank.OpenBankTo(player);
			}
		}
	}
}
