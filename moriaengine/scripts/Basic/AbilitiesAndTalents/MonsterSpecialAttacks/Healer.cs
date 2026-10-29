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

	/// <summary>
	/// #827 e_healer (Erebor's healers, systems/npc_systems.scp): on=@userclick - a ghost that single-clicks
	/// the healer from closer than 5 tiles is resurrected (resurrect_player: s_resurrection at full power), and
	/// a realm outcast (Erebor's negative tag.realm, our GondorOutcast/MordorOutcast) gets "cursed by the gods"
	/// instead, with the name click swallowed (return 1). This is how Erebor players got resurrected: the
	/// healers' spk_healer "res" speech never heard a ghost, as a vendor-brain NPC can't understand ghost speech
	/// (0.99z8 CanUnderstandGhost) - here an NPC doesn't see a ghost, so it doesn't hear it either.</summary>
	[ViewableClass]
	public class HealerAbilityDef : PassiveAbilityDef {
		private FieldValue clickRange;

		public HealerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.clickRange = this.InitTypedField("healerClickRange", 4, typeof(int));
		}

		/// <summary>The farthest a clicking ghost may stand (archive: distance &lt; 5).</summary>
		public int ClickRange {
			get { return (int) this.clickRange.CurrentValue; }
			set { this.clickRange.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static HealerAbilityDef healerAbilityDef;
		internal static HealerAbilityDef HealerDef {
			get {
				if (healerAbilityDef == null) {
					healerAbilityDef = AbstractScript.GetByDefname("a_tevent_e_healer") as HealerAbilityDef;
				}
				return healerAbilityDef;
			}
		}

		/// <summary>#827 e_healer - Character.On_Click, `clicker` single-clicked this NPC. True when the click is
		/// used up (a realm outcast was turned away), so the name isn't shown.</summary>
		public static bool TryHealerClick(NPC self, Character clicker) {
			HealerAbilityDef def = HealerDef;
			if (!HasAbilityDef(self, def)) {
				return false;
			}
			if (Healer.IsCursed(clicker)) {
				Healer.SayCursed(clicker);
				return true;
			}
			if (clicker.Flag_Dead && (Point2D.GetSimpleDistance(self, clicker) <= def.ClickRange)) {
				Healer.Resurrect(self, clicker);
			}
			return false;
		}
	}

	/// <summary>#827 - what Erebor's healers do for players, shared by e_healer (click) and spk_healer (speech).</summary>
	public static class Healer {
		/// <summary>Erebor's tag.realm &lt; 0: a realm outcast ("vyvrhel"), whom no healer brings back.</summary>
		public static bool IsCursed(Character ch) {
			Realm realm = ch.Realm;
			return (realm == Realm.GondorOutcast) || (realm == Realm.MordorOutcast);
		}

		public static void SayCursed(Character ch) {
			ch.SysMessage(Loc<HealerLoc>.Get(ch.Language).CursedByTheGods);
		}

		/// <summary>resurrect_player: s_resurrection's effect at full power (1000), cast by nobody.</summary>
		public static void Resurrect(NPC healer, Character ghost) {
			SpellDef resurrection = SpellDef.GetByDefname("s_resurrection");
			if (resurrection != null) {
				resurrection.ApplyEffectChar(healer, ghost, 1000, EffectFlag.FromAbility);
			} else {
				ghost.Resurrect();
			}
		}
	}

	public class HealerLoc : CompiledLocStringCollection<HealerLoc> {
		public string CursedByTheGods = "You are cursed by the gods and cast out of this world. I cannot bring you back to life.";
	}
}
