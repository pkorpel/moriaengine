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


	[ViewableClass]
	public partial class PoisonedItemPlugin {
		public static PluginKey poisonPK = PluginKey.Acquire("_poison_");

		public const int projectilesPerPotion = 50;


		internal static PoisonedItemPlugin Acquire(PoisonPotion potion) {
			PoisonedItemPlugin plugin = (PoisonedItemPlugin) PoisonedItemPluginDef.instance.Create();
			plugin.poisonTickCount = potion.PoisonTickCount;
			plugin.poisonPower = potion.PoisonPower;
			plugin.poisonType = potion.PoisonType;
			return plugin;
		}

		public static PoisonedItemPlugin GetPoisonPlugin(Item item) {
			return item.GetPlugin(poisonPK) as PoisonedItemPlugin;
		}

		public FadingEffectDurationPluginDef PoisonType {
			get {
				return this.poisonType;
			}
			set {
				this.poisonType = value;
			}
		}

		public double PoisonPower {
			get {
				return this.poisonPower;
			}
			set {
				this.poisonPower = value;
			}
		}

		public int PoisonTickCount {
			get {
				return this.poisonTickCount;
			}
			set {
				this.poisonTickCount = value;
			}
		}

		public int PoisonDoses {
			get {
				return this.poisonDoses;
			}
			set {
				this.poisonDoses = value;
			}
		}

		public void On_BuildAosToolTips(AosToolTips opc, Language language) {
			if (this.poisonDoses <= 0) {
				this.Delete();
				return;
			}
			PoisonedItemLoc loc = Loc<PoisonedItemLoc>.Get(language);
			opc.AddNameColonValue(loc.DosesLeft, this.poisonDoses.ToString(CultureInfo.InvariantCulture));
			opc.AddNameColonValue(loc.Power, this.poisonPower.ToString(CultureInfo.InvariantCulture));
		}

		public void On_Click(Character clicker) {
			if (this.poisonDoses <= 0) {
				this.Delete();
				return;
			}
			GameState state = clicker.GameState;
			if (state != null) {
				PoisonedItemLoc loc = Loc<PoisonedItemLoc>.Get(clicker.Language);
				string msg = loc.DosesLeft + ": " + this.poisonDoses.ToString(CultureInfo.InvariantCulture) +
					Environment.NewLine +
					loc.Power + ": " + this.poisonPower.ToString(CultureInfo.InvariantCulture);
				PacketSequences.SendOverheadMessageFrom(state.Conn, (Thing) this.Cont, msg, -1);
			}
		}

		public void BindToProjectile(Projectile projectile) {
			this.poisonDoses = projectilesPerPotion;
			this.poisonPower *= projectile.PoisoningEfficiency;

			PoisonedItemPlugin previous = projectile.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if (previous != null) {
				Sanity.IfTrueThrow(previous == this, "previous == this");
				Sanity.IfTrueThrow(previous.poisonType != this.poisonType, "previous.poisonType != this.poisonType");

				//the new potion and the old one are summed up, their power averaged.
				int prevDoses = previous.poisonDoses;
				int newDoses = prevDoses + projectilesPerPotion;
				previous.poisonPower = (previous.poisonPower * prevDoses +
					this.poisonPower * projectilesPerPotion) / newDoses;
				previous.poisonTickCount = (previous.poisonTickCount * prevDoses +
					this.poisonTickCount * projectilesPerPotion) / newDoses;
				projectile.InvalidateAosToolTips();
				previous.poisonDoses = newDoses;

				this.Delete(); //probably does nothing, but to be sure...
			} else {
				projectile.AddPlugin(poisonPK, this);
			}
		}

		public void BindToWeapon(Weapon weapon) {
			this.poisonPower *= weapon.PoisoningEfficiency;
			this.poisonDoses = weapon.PoisonCapacity; //full capacity every time?

			weapon.AddPlugin(poisonPK, this);
		}

		//there's a chance this weapon/projectile is in use already. If not, the TG will harmlessly uninstall itself soon.
		public void On_Assign() {
			Thing thing = this.Cont as Thing;
			if (thing != null) {
				Character topChar = thing.TopObj() as Character;
				if (topChar != null) {
					this.InstallCharacterTG(topChar);
				}

				thing.InvalidateAosToolTips(); //we display the poison stats
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

		public void Apply(Thing source, Character target, EffectFlag sourceType) {
			this.poisonType.Apply(source, target, sourceType, this.poisonPower, this.poisonTickCount);
		}

		internal void WipeSingleDoseFromWeapon() {
			if (this.poisonDoses > 0 && this.poisonPower > 0) {
				this.poisonPower -= (this.poisonPower / this.poisonDoses); //poison on weapon weakens by use
				this.poisonDoses--;
				((Item) this.Cont).InvalidateAosToolTips(); //we refresh the displayed poison stats
			} else {
				this.Delete();
			}
		}

		internal void DetractSingleDoseFromAmmo() {
			this.poisonDoses--;
			if (this.poisonDoses <= 0) {
				this.Delete();
			} else {
				((Item) this.Cont).InvalidateAosToolTips(); //we refresh the displayed poison stats
			}
		}

		//we have been split from leftOverStack (in other words, leftOverStack is now an exact copy of us, only with correctly set amounts)
		public void On_SplitFromStack(Item leftOverStack) {
			if (leftOverStack is Projectile) {
				PoisonedItemPlugin original = (PoisonedItemPlugin) leftOverStack.GetPlugin(poisonPK);
				Sanity.IfTrueThrow(original.Def != this.Def, "original.Def != this.Def");
				Sanity.IfTrueThrow(original.poisonDoses != this.poisonDoses, "original.poisonDoses != this.poisonDoses");
				Sanity.IfTrueThrow(original.poisonPower != this.poisonPower, "original.poisonPower != this.poisonPower");
				Sanity.IfTrueThrow(original.poisonType != this.poisonType, "original.poisonType != this.poisonType");

				int newDoses = original.poisonDoses - leftOverStack.Amount;
				if (newDoses > 0) {
					original.poisonDoses -= newDoses;
					this.poisonDoses = newDoses;
				} else {
					this.Delete(); //all of the poison did stay in the other stack
				}
			}
		}



		//this.Cont is being stacked onto another item (projectile) 
		public virtual TriggerResult On_StackOnItem(ItemStackArgs args) {
			Sanity.IfTrueThrow(this.Cont != args.ManipulatedItem, "this != args.ManipulatedItem");

			PoisonedItemPlugin otherPoison = args.WaitingStack.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if (otherPoison == null) {
				//no poison on the waitingstack, we transfer there

				this.Cont.RemovePlugin(this);
				args.WaitingStack.AddPlugin(poisonPK, this);
			}

			return TriggerResult.Continue; //do not cancel
		}

		//another item (projectile) is being stacked onto this.Cont
		public virtual TriggerResult On_ItemStackOn(ItemStackArgs args) {
			Sanity.IfTrueThrow(this.Cont != args.WaitingStack, "this != args.WaitingStack");

			PoisonedItemPlugin otherPoison = args.ManipulatedItem.GetPlugin(poisonPK) as PoisonedItemPlugin;
			if ((otherPoison != null) && (otherPoison.poisonType == this.poisonType)) {
				//the new potion and the old one are summed up, their power averaged.

				int otherDoses = otherPoison.poisonDoses;
				int summedDoses = otherDoses + this.poisonDoses;
				this.poisonPower = (otherPoison.poisonPower * otherDoses +
					this.poisonPower * this.poisonDoses) / summedDoses;
				this.poisonTickCount = (otherPoison.poisonTickCount * otherDoses +
					this.poisonTickCount * this.poisonDoses) / summedDoses;
				this.poisonDoses = summedDoses;

				args.WaitingStack.InvalidateAosToolTips();

				otherPoison.Delete(); //probably doesn't matter, because it's holder is gonna be deleted anyway
			}

			return TriggerResult.Continue;
		}

		public void On_Unassign(Item cont) {
			cont.InvalidateAosToolTips();
		}
	}

	public class E_Poisoned_Weapon_User : CompiledTriggerGroup {
		public void On_AfterSwing(Character self, WeaponSwingArgs swingArgs) {
			if (swingArgs.FinalDamage > 0) { //or maybe we don't really care? 
				bool poisonUsed = false;
				PoisonedItemPlugin poison = self.Weapon.GetPlugin(PoisonedItemPlugin.poisonPK) as PoisonedItemPlugin;
				if (poison != null) {
					poison.Apply(self, swingArgs.defender, EffectFlag.HarmfulEffect | EffectFlag.FromPotion);
					poison.WipeSingleDoseFromWeapon();
					poisonUsed = true;
				}

				Projectile projectile = self.WeaponProjectile;
				if (projectile != null) {
					poison = projectile.GetPlugin(PoisonedItemPlugin.poisonPK) as PoisonedItemPlugin;
					if (poison != null) {
						poison.Apply(self, swingArgs.defender, EffectFlag.HarmfulEffect | EffectFlag.FromPotion);
						poison.DetractSingleDoseFromAmmo();
						poisonUsed = true;
					}
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
		public string DosesLeft = "Poison Doses";
		public string Power = "Poison Power";
	}
}


