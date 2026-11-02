/*
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
    Or visit http://www.gnu.org/copyleft/gpl.html
 */

using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - jed_sila, jed_vydrz and jed_typ on a poisoned weapon or arrows. A blade's dose wears down with its
	/// strength, sniz_jed. Arrows are spent as they're shot.</summary>
	[ViewableClass]
	public partial class PoisonedItemPlugin {
		public static PluginKey poisonPK = PluginKey.Acquire("_poison_");

		internal static void Bind(Item item, PoisonPotion bottle, int power, int doses) {
			Bind(item, bottle.Kind, bottle.Name, power, doses);
		}

		internal static void Bind(Item item, PoisonKind kind, string name, int power, int doses) {
			item.DeletePlugin(poisonPK);
			PoisonedItemPlugin plugin = (PoisonedItemPlugin) PoisonedItemPluginDef.instance.Create();
			plugin.poisonPower = power;
			plugin.poisonDoses = doses;
			plugin.poisonKind = kind;
			plugin.poisonName = name;
			item.AddPlugin(poisonPK, plugin);
		}

		public static PoisonedItemPlugin GetPoisonPlugin(Item item) {
			return item.GetPlugin(poisonPK) as PoisonedItemPlugin;
		}

		public PoisonKind PoisonKind {
			get {
				return this.poisonKind;
			}
		}

		/// <summary>jed_name, the bottle's.</summary>
		public string PoisonName {
			get {
				return this.poisonName;
			}
		}

		public int PoisonPower {
			get {
				return this.poisonPower;
			}
		}

		public int PoisonDoses {
			get {
				return this.poisonDoses;
			}
		}

		/// <summary>e_poisonitem's @userclick.</summary>
		public void On_Click(Character clicker) {
			if (this.poisonPower > 0) {
				((Item) this.Cont).ClickMessage(clicker, Loc<PoisonedItemLoc>.Get(clicker.Language).Poisoned);
			}
		}

		//there's a chance this weapon/projectile is in use already. If not, the TG will harmlessly uninstall itself soon.
		public void On_Assign() {
			Thing thing = this.Cont as Thing;
			if (thing != null) {
				Character topChar = thing.TopObj() as Character;
				if (topChar != null) {
					this.InstallCharacterTG(topChar);
				}
			}
		}

		//our weapon is being equipped to be fought with
		public void On_Equip(ItemInCharArgs args) {
			this.InstallCharacterTG((Character) args.Cont);
		}

		//our projectile is being used with a weapon
		public void On_CoupledWithWeapon(Character cont, Weapon weapon) {
			this.InstallCharacterTG(cont);
		}

		private void InstallCharacterTG(Character topChar) {
			topChar.AddTriggerGroup(SingletonScript<E_Poisoned_Weapon_User>.Instance);
		}

		/// <summary>e_poison's @AfterSwing. poison_c can't be dealt from a weapon, it's plain damage there.</summary>
		internal void Hit(Character attacker, Character target) {
			PoisonEffectSettings settings = PoisonEffectSettings.instance;
			int strength = (target is NPC) ? (this.poisonPower * settings.percentOnMonsters) / 100 : this.poisonPower;
			PoisonKind kind = (this.poisonKind == PoisonKind.PercentOfHits) ? PoisonKind.AbsoluteDamage : this.poisonKind;
			Poison.Apply(target, attacker, strength, settings.SecondsFor(kind), kind, true);
		}

		/// <summary>sniz_jed.</summary>
		internal void WearDown() {
			if ((this.poisonDoses > 0) && (this.poisonPower > 0)) {
				this.poisonPower -= this.poisonPower / this.poisonDoses;
				this.poisonDoses--;
			} else {
				this.Delete();
			}
		}
	}

	public class E_Poisoned_Weapon_User : CompiledTriggerGroup {
		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			if (swingArgs.FinalDamage > 0) {
				bool poisonUsed = false;
				Weapon weapon = self.Weapon;
				PoisonedItemPlugin poison = (weapon == null) ? null : PoisonedItemPlugin.GetPoisonPlugin(weapon);
				if (poison != null) {
					if ((self.CurrentSkillName == SkillName.Swords) || (self.CurrentSkillName == SkillName.Fencing)) {
						poison.Hit(self, swingArgs.defender);
						poison.WearDown();
					}
					poisonUsed = true;
				}

				Projectile projectile = self.WeaponProjectile;
				poison = (projectile == null) ? null : PoisonedItemPlugin.GetPoisonPlugin(projectile);
				if (poison != null) {
					poison.Hit(self, swingArgs.defender);
					poisonUsed = true;
				}

				if (!poisonUsed) {
					self.RemoveTriggerGroup(this);
				}
			}
		}
	}

	[ViewableClass]
	public partial class PoisonedItemPluginDef {
		public static readonly PoisonedItemPluginDef instance = (PoisonedItemPluginDef)
			new PoisonedItemPluginDef("p_poisoned_item", "C# scripts", -1).Register();
	}

	public class PoisonedItemLoc : CompiledLocStringCollection<PoisonedItemLoc> {
		public string Poisoned = "Otravena";
	}
}
