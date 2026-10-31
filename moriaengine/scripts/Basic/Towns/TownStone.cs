using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class TownStoneDef {
	}

	/// <summary>#892 - i_townstone (t_townmenu): a Gondor city's seat, standing in the map area of that
	/// city. Everything of the town is on Town, and this saves it.</summary>
	[ViewableClass]
	public partial class TownStone {
		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(this.Region);
			if ((area == null) || (area.Realm != Realm.Gondor)) {
				player.RedMessage("Realm nevhodny pro Gondorske mesto, kontaktuj GM, kamen by zde nemel byt!");
			} else if (RegionEventSystem.AreaOf(player.Region) != area) {
				player.SysMessage("Musis byt v prostoru mesta!");
			} else if (string.IsNullOrEmpty(area.City)) {
				player.SysMessage("Mesto neni zatim pripraveno, kontaktuj GM");
			} else {
				this.Open(player, area);
			}
		}

		private void Open(Player player, FlaggedRegion area) {
			Town town = Town.ForStone(this, area.City);
			town.EnsureTaxChest(player);
			town.CheckTimers();
			town.RaiseTaxToMinimum(player);
			TownCitizen citizen = town.FindCitizen(player);
			if ((citizen != null) && !citizen.paidTax && (town.mayor != player)) {
				player.SysMessage("Na vcasne zaplaceni dani ti zbyva " + TownDialogs.TimeLeft(town.taxDueAt));
			}
			D_TownMenu.Show(town, player);
		}

		public override void On_Destroy() {
			Town town = Town.OfStone(this);
			if (town != null) {
				town.Remove();
			}
			base.On_Destroy();
		}

		/// <summary>The town is saved with its stone, as a guild is with its own.</summary>
		public override void Save(SaveStream output) {
			base.Save(output);
			Town town = Town.OfStone(this);
			if (town != null) {
				ObjectSaver.Save(town);
			}
		}
	}

	[ViewableClass]
	public partial class TaxChestDef {
	}

	/// <summary>i_tax_chest, which never opens: double click tells the tax and offers to pay it, and gold
	/// that got into it goes to the treasury.</summary>
	[ViewableClass]
	public partial class TaxChest {
		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			Town town = Town.OfChest(this);
			if ((player == null) || (town == null)) {
				return;
			}
			town.CollectChest();
			player.SysMessage("Dane ve meste " + town.Name + " cini " + town.tax + "gp.");
			TownCitizen citizen = town.FindCitizen(player);
			if ((citizen != null) && !citizen.paidTax) {
				D_TownConfirm.Ask(player, "Prejes si zaplatit dan mestu?", () => town.PayTax(player));
			}
		}
	}

	/// <summary>Who stops being what, chosen on a character: the GM lines of the menu.</summary>
	public class Targ_TownCitizen : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Town town = (Town) parameter;
			Town current = Town.OfCitizen(targetted);
			if (targetted.Realm != Realm.Gondor) {
				self.SysMessage("Obcanem se muze stat jen pravoplatny obcan Gondoru!");
			} else if (town.GetRank(targetted) == Town.RankEnemy) {
				self.SysMessage(targetted.Name + " je na cerne listine!");
			} else if (current != null) {
				self.SysMessage(targetted.Name + " jiz je obcanem mesta " + current.Name);
			} else {
				town.AddCitizen(targetted);
				self.SysMessage(targetted.Name + " uspesne pridan jako obcan");
				targetted.SysMessage("Stal jsi se obcanem mesta " + town.Name + ".");
			}
			return TargetResult.Done;
		}
	}

	public class Targ_TownMayor : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Town town = (Town) parameter;
			Town current = Town.OfCitizen(targetted);
			if (town.mayor != null) {
				self.SysMessage("Mesto " + town.Name + " jiz starostu ma.");
			} else if (targetted.Realm != Realm.Gondor) {
				self.SysMessage("Starostou se muze stat jen pravoplatny obcan Gondoru!");
			} else if (town.GetRank(targetted) == Town.RankEnemy) {
				self.SysMessage(targetted.Name + " je na cerne listine!");
			} else if ((current != null) && (current != town)) {
				self.SysMessage(targetted.Name + " je obcanem mesta " + current.Name);
			} else {
				if (current == null) {
					town.AddCitizen(targetted);
				}
				town.mayor = targetted;
				self.SysMessage(targetted.Name + " uspesne pridan jako starosta");
				targetted.SysMessage("Stal jsi se starostou mesta " + town.Name + ".");
			}
			return TargetResult.Done;
		}
	}
}
