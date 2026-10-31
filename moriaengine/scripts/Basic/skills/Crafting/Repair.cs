using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#865 - Erebor's craftrepairthis (repairing.scp): at an anvil, for a share of the
	/// item's resources. Each try costs a point of max durability, whether it works or not.</summary>
	public static class Repair {
		private const int anvilModelA = 0xfaf, anvilModelB = 0xfb0;

		[SteamFunction("repair")]
		public static void RepairCommand(Player self) {
			self.Target(SingletonScript<Targ_Repair>.Instance);
		}

		/// <summary>Armor's first resource is by the damage, a weapon's costs at least 1 while damaged.</summary>
		public static int CalculateFirstResource(bool isArmor, int count, int durability, int maxDurability) {
			if (isArmor) {
				return ((maxDurability - durability) * count) / maxDurability;
			}
			return count - ((durability * count) / maxDurability);
		}

		private static bool IsAnvil(int model) {
			return (model == anvilModelA) || (model == anvilModelB);
		}

		public static bool IsNearAnvil(Character self) {
			Map map = self.GetMap();
			foreach (AbstractInternalItem item in map.GetStaticsAndMultiComponentsInRange(self.X, self.Y, 1)) {
				if (IsAnvil(item.Id)) {
					return true;
				}
			}
			foreach (Thing thing in map.GetThingsInRectangle(new ImmutableRectangle(self.X - 1, self.Y - 1, self.X + 1, self.Y + 1))) {
				if ((thing is Item) && IsAnvil(thing.Model)) {
					return true;
				}
			}
			return false;
		}

		private static int CountItems(Character self, ItemDef def) {
			int count = 0;
			foreach (Item item in self.Backpack.EnumShallow()) {
				if (item.Def == def) {
					count += item.Amount;
				}
			}
			return count;
		}

		private static void ConsumeItems(Character self, ItemDef def, int amount) {
			foreach (Item item in new System.Collections.Generic.List<AbstractItem>(self.Backpack.EnumShallow())) {
				if ((amount > 0) && (item.Def == def)) {
					int taken = System.Math.Min(amount, item.Amount);
					amount -= taken;
					if (taken >= item.Amount) {
						item.Delete();
					} else {
						item.Amount -= taken;
					}
				}
			}
		}

		private static bool IsIngotOrLog(ItemDef def) {
			string type = (def.Type != null) ? def.Type.Defname : "";
			return string.Equals(type, "t_ingot", System.StringComparison.OrdinalIgnoreCase) || string.Equals(type, "t_log", System.StringComparison.OrdinalIgnoreCase);
		}

		public static void TryRepair(Player self, Destroyable item) {
			RepairLoc loc = Loc<RepairLoc>.Get(self.Language);
			if (!item.TypeDef.CanRepair) {
				self.SysMessage(loc.NotRepairable);
				return;
			}
			if (item.Durability >= item.MaxDurability) {
				self.SysMessage(loc.NotNeeded);
				return;
			}
			if (!IsNearAnvil(self)) {
				self.SysMessage(loc.NeedAnvil);
				return;
			}
			ItemDef firstDef = null, secondDef = null;
			int firstAmount = 0, secondAmount = 0;
			foreach (IResourceListEntry entry in item.TypeDef.Resources.Entries) {
				ItemResource resource = entry as ItemResource;
				if ((resource != null) && (firstDef == null)) {
					firstDef = resource.ItemDef;
					firstAmount = CalculateFirstResource(item is Wearable, (int) resource.DesiredCount, item.Durability, item.MaxDurability);
				} else if ((resource != null) && (secondDef == null) && IsIngotOrLog(resource.ItemDef)) {
					secondDef = resource.ItemDef;
					secondAmount = CalculateFirstResource(true, (int) resource.DesiredCount, item.Durability, item.MaxDurability);
				}
			}
			if ((firstDef != null) && (CountItems(self, firstDef) < firstAmount)) {
				self.SysMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture, loc.NotEnough, firstDef.Name));
				return;
			}
			if ((secondDef != null) && (CountItems(self, secondDef) < secondAmount)) {
				self.SysMessage(string.Format(System.Globalization.CultureInfo.InvariantCulture, loc.NotEnough, secondDef.Name));
				return;
			}
			if (firstDef != null) {
				ConsumeItems(self, firstDef, firstAmount);
			}
			if (secondDef != null) {
				ConsumeItems(self, secondDef, secondAmount);
			}
			item.MaxDurability = (ushort) (item.MaxDurability - 1);

			SkillName skill = SkillName.Blacksmith;
			Weapon weapon = item as Weapon;
			if ((weapon != null) && ((weapon.WeaponType == WeaponType.Bow) || (weapon.WeaponType == WeaponType.XBow))) {
				skill = SkillName.Fletching;
			} else if (item is ColoredStaff) {
				skill = SkillName.Carpentry;
			}
			if (self.GetSkill(skill) > Globals.dice.Next(1026)) {
				item.Durability = item.MaxDurability;
				self.SysMessage(loc.Repaired);
			} else {
				self.SysMessage(loc.Failed);
			}
			self.Sound(0x2a);
		}
	}

	public class Targ_Repair : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Destroyable item = targetted as Destroyable;
			if (item == null) {
				self.SysMessage(Loc<RepairLoc>.Get(self.Language).NotRepairable);
				return TargetResult.Done;
			}
			Repair.TryRepair(self, item);
			return TargetResult.Done;
		}
	}

	public class RepairLoc : CompiledLocStringCollection<RepairLoc> {
		internal string NotRepairable = "Tohle neni opravitelne";
		internal string NotNeeded = "Netreba opravovat";
		internal string NeedAnvil = "Musis stat u kovadliny";
		internal string NotEnough = "Nemas dost materialu ({0})";
		internal string Repaired = "Oprava se povedla";
		internal string Failed = "Oprava se nepovedla";
	}
}
