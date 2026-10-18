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
	[ViewableClass]
	public partial class EquippableDef {
		private static readonly TagKey associatedSkillTK = TagKey.Acquire("_associatedSkill_");

		/// <summary>#370 - the skill a dclick/`.usehand` "use" action on this equippable
		/// should start (e.g. a pickaxe's Mining, a hatchet's Lumberjacking). SkillName.None
		/// (the default) means this item has no "use" action - dclicking it only equips,
		/// matching every plain weapon/armor's existing behavior. Data-driven on purpose, so
		/// wiring a new tool to its skill is a one-line ".def" change, no new C# class needed -
		/// see Equippable.OnUse.</summary>
		public SkillName AssociatedSkill {
			get {
				object tag = this.GetTag(associatedSkillTK);
				if (tag is SkillName skill) {
					return skill;
				}
				if ((tag is int intTag) && Enum.IsDefined(typeof(SkillName), intTag)) {
					return (SkillName) intTag; //defensive coercion - same save/reload retyping class as AggroMode/TamingDifficulty (#168/#265)
				}
				return SkillName.None;
			}
			set {
				this.SetTag(associatedSkillTK, value);
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param == "associatedskill") {
				SkillName skill;
				if (Enum.TryParse(args, true, out skill)) {
					this.AssociatedSkill = skill;
				} else {
					Logger.WriteWarning(filename, line, "associatedskill '" + args + "' is not a known SkillName value.");
				}
				return;
			}
			base.LoadScriptLine(filename, line, param, args);
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
		/// Default behavior is data-driven off TypeDef.AssociatedSkill - any equippable tool can
		/// opt into "dclick starts my skill" with one ".def" line, no override needed. Virtual
		/// so a specific item class can still provide a richer "use" action later (e.g. a real
		/// chop-tree/cut-cloth target flow) without being forced through the skill-select shape.</summary>
		public virtual void OnUse(AbstractCharacter from) {
			SkillName skill = this.TypeDef.AssociatedSkill;
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
		/// off the same AssociatedSkill field the dclick hook above already reads, instead of
		/// each skill hardcoding its own tool ItemDef/class check.</summary>
		public static bool IsHoldingToolFor(Character self, SkillName skill) {
			return IsToolFor(self.FindLayer(LayerNames.Hand1), skill) || IsToolFor(self.FindLayer(LayerNames.Hand2), skill);
		}

		private static bool IsToolFor(AbstractItem equipped, SkillName skill) {
			Equippable tool = equipped as Equippable;
			return (tool != null) && (tool.TypeDef.AssociatedSkill == skill);
		}
	}
}