using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>t_keg_potion: holds a number of one potion, poured in by target and drawn into empty
	/// bottles the same way; a garbage can empties it.</summary>
	[ViewableClass]
	public partial class PotionKeg {
		internal const int LiquidSound = 0x240, Reach = 2;

		public override void On_DClick(AbstractCharacter from) {
			if (from is Player player) {
				player.SysMessage("Kterym lektvarem chces soudek naplnit?");
				player.Target(SingletonScript<Targ_PotionKeg>.Instance, this);
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, SteamEngine.Communication.TCP.TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			if (this.potionsCount > 0) {
				PacketSequences.SendNameFrom(clickerConn, this, "(" + this.potionsCount + " " + this.potionDef.Name + ")", 0);
			}
		}

		internal void Empty() {
			this.potionDef = null;
			this.potionsCount = 0;
			this.Color = 0;
		}
	}

		public class Targ_PotionKeg : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			PotionKeg keg = (PotionKeg) parameter;
			if (keg.IsDeleted) {
				return TargetResult.Done;
			}
			if (VarGate.Matches(targetted, "t_garbage_can", true)) {
				if (Point2D.GetSimpleDistance(self.X, self.Y, targetted.X, targetted.Y) > PotionKeg.Reach) {
					self.SysMessage("Jsi od soudku prilis daleko.");
				} else {
					keg.Empty();
					PacketSequences.SendSound(self, PotionKeg.LiquidSound, Globals.MaxUpdateRange);
					self.SysMessage("Soudek byl vyprazdnen.");
				}
			} else if (VarGate.Matches(targetted, "t_bottle_empty", true)) {
				Draw(self, keg, targetted);
			} else if (targetted is Potion potion) {
				Pour(self, keg, potion);
			} else {
				self.SysMessage("To do soudku nepatri.");
			}
			return TargetResult.Done;
		}

		private static void Draw(Player self, PotionKeg keg, Item bottles) {
			if (keg.potionsCount == 0) {
				self.SysMessage("Neni mozne pouzit prazdny soudek.");
				return;
			}
			if (bottles.TopObj() != self) {
				self.SysMessage("Musis u sebe mit prazdnou lahvicku.");
				return;
			}
			int amount = System.Math.Min(bottles.Amount, keg.potionsCount);
			Item filled = (Item) keg.potionDef.Create(self.Backpack);
			filled.Amount = amount;
			bottles.Consume(amount);
			keg.potionsCount -= amount;
			PacketSequences.SendSound(self, PotionKeg.LiquidSound, Globals.MaxUpdateRange);
			self.SysMessage("Naplnil jsi lahvicku.");
			if (keg.potionsCount == 0) {
				keg.Empty();
			}
		}

		private static void Pour(Player self, PotionKeg keg, Potion potion) {
			if (potion.TopObj() != self) {
				self.SysMessage("To je prilis daleko nebo tam nevidis.");
				return;
			}
			if ((keg.potionDef != null) && (potion.TypeDef != keg.potionDef)) {
				self.SysMessage("Lektvary nemuzes michat.");
				return;
			}
			if (keg.potionsCount >= keg.TypeDef.Capacity) {
				self.SysMessage("Soudek uz je plny.");
				return;
			}
			int amount = System.Math.Min(potion.Amount, keg.TypeDef.Capacity - keg.potionsCount);
			keg.potionDef = potion.TypeDef;
			keg.potionsCount += amount;
			keg.Color = potion.Color;
			ItemDef emptyDef = potion.TypeDef.EmptyFlask;
			if (emptyDef != null) {
				Item bottles = (Item) emptyDef.Create(self.Backpack);
				bottles.Amount = amount;
			}
			potion.Consume(amount);
			PacketSequences.SendSound(self, PotionKeg.LiquidSound, Globals.MaxUpdateRange);
			self.SysMessage("Naplnil jsi soudek lektvarem " + keg.potionDef.Name + ".");
		}
	}
}
