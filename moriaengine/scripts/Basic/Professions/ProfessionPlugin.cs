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

using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// This class holds the profession assigned to one character (cont field). 
	/// it holds a reference to the ProfessionDef, dispatches all trigger calls etc.
	/// </summary>
	[ViewableClass]
	public partial class ProfessionPlugin {
		internal static PluginKey professionKey = PluginKey.Acquire("_profession_");

		/// <summary>Initialize the profession on the character - grants the .def's starting
		/// skill values immediately (#135). User: "it should just grant it now - that way once
		/// we implement specific skill, it will simply start working for player." Only RAISES a
		/// skill to the profession's minimum, never lowers an already-higher value - this is a
		/// grant, not a reset, so reassigning/testing professions on a character that already
		/// has progress doesn't destroy it. Gated by ProfessionSettings.grantSkillsOnAssign so a
		/// GM can disable this for testing profession-switching without the skill side effect.
		/// Also refreshes the title (#165) - NOT gated by that setting, a title change has no
		/// gameplay side effect worth suppressing.</summary>
		public virtual void On_Assign() {
			Sanity.IfTrueThrow(this.profession == null, "this.def == null on ProfessionPlugin assigning");

			Player player = (Player) this.Cont;

			if (ProfessionSettings.instance.grantSkillsOnAssign) {
				foreach (SkillDef skillDef in SkillDef.AllSkillDefs) {
					int minimum = this.profession.GetSkillMinimum(skillDef);
					if ((minimum > 0) && (player.GetRealSkillValue(skillDef) < minimum)) {
						player.SetRealSkillValue(skillDef, minimum);
					}
				}
			}

			player.RefreshTitle();
		}

		/// <summary>#951 - Erebor's tag.skillrank, read through Player.SkillRank.</summary>
		public SkillRank SkillRank {
			get {
				return this.skillRank;
			}
			set {
				this.skillRank = value;
			}
		}

		public ProfessionDef ProfessionDef {
			get {
				return this.profession;
			}
		}

		public virtual TriggerResult On_SkillSelect(SkillSequenceArgs skillSeqArgs) {
			return this.CheckCancelSkill((Player) this.Cont, skillSeqArgs);
		}

		public virtual TriggerResult On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			return this.CheckCancelSkill((Player) this.Cont, skillSeqArgs);
		}

		private TriggerResult CheckCancelSkill(Player player, SkillSequenceArgs skillSeqArgs) {
			if ((!player.IsGM) && skillSeqArgs.SkillDef.Id == (int) SkillName.Magery) {
				SpellDef spell = (SpellDef) skillSeqArgs.Param1;
				if (!spell.IsCustom && !this.profession.AllowedSpells.Contains(spell)) { //those ask their scroll
					player.RedMessage(string.Format(CultureInfo.InvariantCulture,
						Loc<ProfessionPluginLoc>.Get(player.Language).YouCantCastThis,
						this.profession.Name));

					return TriggerResult.Cancel; //we don't know that spell
				}
			}
			return TriggerResult.Continue;
		}

		/// <summary>Weapon/armor-by-profession restriction (#138) - hooks the same denyEquip
		/// trigger every equip attempt already goes through (ThingManipulationTriggers.cs's
		/// TryEquipItemOnChar), mirroring CheckCancelSkill's existing Magery-restriction
		/// pattern above. No prior content ever overrode On_DenyEquip at all - this is the
		/// first user of that extension point. Plain Clothing (WearableType.Clothing) is never
		/// restricted, only real armor materials - nobody should be blocked from wearing a
		/// shirt. Gated by ProfessionSettings.enforceWeaponArmorRestrictions and skipped for
		/// GMs, same spirit as CheckCancelSkill's own IsGM bypass.</summary>
		public virtual TriggerResult On_DenyEquip(DenyEquipArgs args) {
			if (!ProfessionSettings.instance.enforceWeaponArmorRestrictions) {
				return TriggerResult.Continue;
			}

			Player player = this.Cont as Player;
			if ((player == null) || player.IsGM) {
				return TriggerResult.Continue;
			}

			Weapon weapon = args.ManipulatedItem as Weapon;
			if (weapon != null) {
				if (!this.profession.CanUseWeaponType(weapon.WeaponType)) {
					args.Result = DenyResultMessages_ProfessionPlugin.Deny_WeaponNotAllowedForProfession;
					return TriggerResult.Cancel;
				}
				return TriggerResult.Continue;
			}

			Wearable wearable = args.ManipulatedItem as Wearable;
			if (wearable != null) {
				WearableType material = ((WearableDef) wearable.Def).WearableType;
				if ((material != WearableType.Clothing) && !this.profession.CanWearArmorMaterial(material)) {
					args.Result = DenyResultMessages_ProfessionPlugin.Deny_ArmorNotAllowedForProfession;
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		/// <summary>#1438 - death takes the class counter away (sphereevents.scp). Every class's counter is this field: Erebor's
		/// specability, switched by ClassAbilities.ToggleCounter.</summary>
		public virtual void On_Death() {
			this.counterOn = false;
		}

		/// <summary>#1429 - a blow landing on the player, before the dodge (Erebor's @aftergetswing): the shield block,
		/// which six classes may learn. A profession's own plugin adds its part and calls this.</summary>
		public virtual void On_GetSwingLanding(WeaponSwingArgs swing) {
			WarriorAbilities.ShieldBlock((Character) this.Cont, swing);
		}

		public static ProfessionPlugin GetInstalledPlugin(Player player) {
			return (ProfessionPlugin) player.GetPlugin(professionKey);
		}

		public static void InstallProfessionPlugin(Player player, ProfessionDef professionDef) {
			if (professionDef != null) {
				ProfessionPlugin newPlugin = (ProfessionPlugin) professionDef.ProfessionPluginDef.Create();
				newPlugin.profession = professionDef;
				newPlugin.skillRank = player.SkillRank; //a rank outlives the profession it was earned in
				player.AddPlugin(professionKey, newPlugin); //if there was a previous one, it's deleted now
			} else {
				SkillRank rank = player.SkillRank;
				player.DeletePlugin(professionKey); //null professiondef = no profession = no professionplugin
				player.SkillRank = rank;
			}
		}
	}

	[ViewableClass]
	public partial class ProfessionPluginDef {
	}

	public class ProfessionPluginLoc : CompiledLocStringCollection<ProfessionPluginLoc> {
		public string YouCantCastThis = "You can't cast this spell, being {0} of profession.";
		public string WeaponNotAllowedForProfession = "Your profession does not allow you to use this weapon.";
		public string ArmorNotAllowedForProfession = "Your profession does not allow you to wear this armor.";
	}

	public static class DenyResultMessages_ProfessionPlugin {
		public static readonly DenyResult Deny_WeaponNotAllowedForProfession =
			new CompiledLocDenyResult<ProfessionPluginLoc>("WeaponNotAllowedForProfession");

		public static readonly DenyResult Deny_ArmorNotAllowedForProfession =
			new CompiledLocDenyResult<ProfessionPluginLoc>("ArmorNotAllowedForProfession");
	}
}