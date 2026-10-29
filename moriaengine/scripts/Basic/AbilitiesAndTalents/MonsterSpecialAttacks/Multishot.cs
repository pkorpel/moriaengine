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

using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #799 e_npc_multishot (29 monsters). Archive: the same template as the area curses -
	/// when hit, tag.multishot_chance (default chance(110,3) = 11%) once tag.multishot_delay
	/// has passed (default 3 s) to loose a split shot at every player/pet within 8 tiles and in
	/// line of sight EXCEPT the one it is fighting, for tag.multishot_dmg ({40,80}) physical
	/// damage, 5x to pets.</summary>
	[ViewableClass]
	public class MultishotAbilityDef : AreaCurseAbilityDef {
		public MultishotAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "multishot", new double[] { 40, 80 }, new double[] { 3, 3 }) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int ArrowEffect = 0x1BFE;

		private static MultishotAbilityDef multishotAbilityDef;
		internal static MultishotAbilityDef MultishotDef {
			get {
				if (multishotAbilityDef == null) {
					multishotAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_multishot") as MultishotAbilityDef;
				}
				return multishotAbilityDef;
			}
		}

		private static readonly TagKey nextMultishotTimeTK = TagKey.Acquire("_npcNextMultishotTime_");

		/// <summary>#799 e_npc_multishot - defender side, after a landed swing. Returns true when it shot.</summary>
		public static bool TryApplyMultishot(Character defender) {
			NPC self = defender as NPC;
			if ((self == null) || !self.IsAliveAndValid) {
				return false;
			}
			NPCDef npcDef = (NPCDef) self.Def;
			return TryApplyAreaCurse(self, MultishotDef, nextMultishotTimeTK,
				npcDef.MultishotChance, npcDef.MultishotCooldown, npcDef.MultishotDamage,
				delegate (Character target, double amount) {
					EffectFactory.EffectFromTo(self, target, ArrowEffect, 10, 1, false, false);
					DamageManager.CauseDamage(self, target, DamageType.PhysicalArchery, amount);
					target.SysMessage("You have been hit by a split shot.");
				});
		}
	}
}
