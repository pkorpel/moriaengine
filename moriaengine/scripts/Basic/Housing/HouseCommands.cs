using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - GM tools for what .SetupHouses can't know: where a sign or rent chest really belongs, and the doors,
	/// which Erebor's GMs placed by hand and aren't in the archive.</summary>
	public static class HouseCommands {
		private const string DefaultDoor = "i_0x675";

		[SteamFunction]
		public static void HouseMove(Character self) {
			Player player = self as Player;
			if (player != null) {
				player.SysMessage("Kterou vyvesku nebo truhlu presunout?");
				player.Target(SingletonScript<Targ_HouseMove>.Instance);
			}
		}

		/// <summary>A door of a house, by default a wooden one: turn it with .flip, lock it from the sign.</summary>
		[SteamFunction]
		public static void HouseDoor(Character self, ScriptArgs sa) {
			Player player = self as Player;
			if (player != null) {
				string defname = ((sa == null) || string.IsNullOrEmpty(sa.Args)) ? DefaultDoor : sa.Args.Trim('"', ' ');
				player.SysMessage("Kam dvere postavit?");
				player.Target(SingletonScript<Targ_HouseDoor>.Instance, defname);
			}
		}
	}

	/// <summary>First the thing, with no parameter, then where it goes.</summary>
	public class Targ_HouseMove : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (parameter != null) {
				return this.MoveTo(self, targetted, (Item) parameter);
			}
			if ((targetted.Type is t_housemenu) || (targetted is RentChest)) {
				self.SysMessage("Kam?");
				self.Target(this, targetted);
			} else {
				self.SysMessage("To neni vyveska ani truhla domu.");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return (parameter == null) ? TargetResult.RestartTargetting : this.MoveTo(self, targetted, (Item) parameter);
		}

		private TargetResult MoveTo(Player self, IPoint3D where, Item piece) {
			if (!piece.IsDeleted) {
				piece.P(where.X, where.Y, where.Z, self.M);
				self.SysMessage("Presunuto.");
			}
			return TargetResult.Done;
		}
	}

	public class Targ_HouseDoor : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			if (House.FindHouseAt(new Point4D(targetted.X, targetted.Y, targetted.Z, self.M)) == null) {
				self.SysMessage("Tady neni zadny dum.");
				return TargetResult.Done;
			}
			ItemDef def = ThingDef.GetByDefname((string) parameter) as ItemDef;
			if (def == null) {
				self.SysMessage("Takova vec neexistuje: " + parameter);
				return TargetResult.Done;
			}
			Item door = (Item) def.Create(targetted.X, targetted.Y, targetted.Z, self.M);
			if (door.IsDoor) {
				door.SetAttribute(ItemAttributes.attr_move_never, true);
			} else {
				self.SysMessage(parameter + " nejsou dvere.");
				door.Delete();
			}
			return TargetResult.Done;
		}
	}
}
