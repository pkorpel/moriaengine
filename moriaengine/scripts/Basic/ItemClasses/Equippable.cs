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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#392 - a tool-category tag independent of WeaponType (pickaxe/fishing pole
	/// aren't safely derivable from WeaponType - OneHandSpike is shared with real Fencing
	/// weapons like daggers, and a fishing pole isn't even a WeaponDef). Any equippable tool
	/// just needs one ".def" "tooltype=" line (Equippable.ct's toolType) to automatically grant
	/// its gathering skill - see EquippableDef.UseSkill.</summary>
	public enum ToolType {
		None,
		Pickaxe,
		FishingPole
	}

	[ViewableClass]
	public partial class EquippableDef {
		/// <summary>#370 - the skill a dclick/`.usehand` "use" action on this equippable
		/// starts (e.g. a pickaxe's Mining, a hatchet's Lumberjacking), and the skill it counts
		/// as the tool for (IsHoldingToolFor). SkillName.None means this item has no "use"
		/// action - dclicking it only equips, matching every plain weapon/armor's behavior.
		///
		/// The def's own AssociatedSkill (Equippable.ct, a .def "associatedskill=" line) when
		/// set; otherwise (#392) derived from type-level data: any weapon whose WeaponType is
		/// OneHandAxe/TwoHandAxe (an exclusively-axe category, #157) grants Lumberjacking, and
		/// ToolType Pickaxe/FishingPole (a .def "tooltype=" line) grants Mining/Fishing - so a
		/// new axe or tool item works with no per-item line beyond its tool type.</summary>
		public SkillName UseSkill {
			get {
				SkillName skill = this.AssociatedSkill;
				if (skill != SkillName.None) {
					return skill;
				}
				if (this is WeaponDef weaponDef) {
					WeaponType wt = weaponDef.WeaponType;
					if ((wt == WeaponType.OneHandAxe) || (wt == WeaponType.TwoHandAxe)) {
						return SkillName.Lumberjacking;
					}
				}
				switch (this.ToolType) {
					case ToolType.Pickaxe:
						return SkillName.Mining;
					case ToolType.FishingPole:
						return SkillName.Fishing;
				}
				return SkillName.None;
			}
		}

	}

	[ViewableClass]
	public partial class Equippable : Item {
		public override int Layer {
			get {
				return this.TypeDef.Layer;
			}
		}

		public sealed override bool IsEquippable {
			get {
				return this.TypeDef.Layer != 0;
			}
		}

		//public void Equip() {
		//    AbstractCharacter src = Globals.SrcCharacter;
		//    src.Equip(src, this);
		//}

		/// <summary>#370 - previously only ever equipped the item, with no further action once
		/// equipped (or already equipped) - a real gap, since real UO/Sphere dclicking a tool
		/// equips it AND starts its "use" action in one step. Now always calls OnUse afterward
		/// (equip-failure still short-circuits, same as before - nothing to "use" if it never
		/// made it into the player's hand).</summary>
		public override void On_DClick(AbstractCharacter from) {
			if (this.IsEquippable) {
				//pick it up into dragging layer, then equip it.
				//We want the same triggers fired as if the client dragged it manually (yes, a lot of triggers)

				bool alreadyEquipped = (this.Cont == from) && (this.Z == this.Layer);
				if (!alreadyEquipped) {
					DenyResult dr = from.TryPickupItem(this, 1);
					if (dr.Allow) {
						dr = from.TryEquipItemOnChar(from);
					}
					if (!dr.Allow) {
						dr.SendDenyMessage(from);
						return;
					}
				}
				this.OnUse(from);
			}
		}

		/// <summary>#370 - the generic "use" hook, called once the item is confirmed equipped
		/// (whether it just got equipped by this same dclick, or was already on the character).
		/// Default behavior is data-driven off TypeDef.UseSkill - any equippable tool can
		/// opt into "dclick starts my skill" with one ".def" line, no override needed. Virtual
		/// so a specific item class can still provide a richer "use" action later (e.g. a real
		/// chop-tree/cut-cloth target flow) without being forced through the skill-select shape.</summary>
		public virtual void OnUse(AbstractCharacter from) {
			SkillName skill = this.TypeDef.UseSkill;
			if (skill != SkillName.None) {
				Character clicker = from as Character;
				if (clicker != null) {
					clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, skill, this));
				}
			}
		}

		public override bool IsTwoHanded {
			get {
				if (this.Def != null) return this.TypeDef.TwoHanded;
				return false;
			}
		}

		/// <summary>#370 - shared "is the right tool equipped for this skill" check, reused by
		/// every tool-gated skill (Mining/Lumberjacking/Fishing) so the gate stays data-driven
		/// off the same UseSkill the dclick hook above already reads, instead of
		/// each skill hardcoding its own tool ItemDef/class check.</summary>
		public static bool IsHoldingToolFor(Character self, SkillName skill) {
			return IsToolFor(self.FindLayer(LayerNames.Hand1), skill) || IsToolFor(self.FindLayer(LayerNames.Hand2), skill);
		}

		private static bool IsToolFor(AbstractItem equipped, SkillName skill) {
			Equippable tool = equipped as Equippable;
			return (tool != null) && (tool.TypeDef.UseSkill == skill);
		}
	}
}