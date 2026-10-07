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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	public static class StatModSpellsUtils {
		public const int minStat = 10;
		public const int minResist = 0;

		static StatModSpellsUtils() {
			new StrModSpellEffectPluginDef("p_strMod", "C# scripts", -1).Register();
			new DexModSpellEffectPluginDef("p_dexMod", "C# scripts", -1).Register(); ;
			new IntModSpellEffectPluginDef("p_intMod", "C# scripts", -1).Register(); ;
			new CurseSpellEffectPluginDef("p_curse", "C# scripts", -1).Register(); ;
			new BlessSpellEffectPluginDef("p_bless", "C# scripts", -1).Register(); ;
		}

		public static void Bootstrap() { //ensure calling the static initialiser
		}

		public static short ModifyStat(int lowBoundary, short statValue, short statDiff, out short resultDiff) {
			if (statDiff < 0) {
				short retVal = (short) (statValue + statDiff);
				if (retVal < minStat) { //this would decrease the stat under the boundary
					if (statValue < minStat) { //the stat is already under the boundary, we leave it as is
						resultDiff = 0;
						return statValue;
					}
					resultDiff = (short) (minStat - statValue);
					return minStat;
				}
				resultDiff = statDiff;
				return retVal;
			} //positive change has no boundary
			resultDiff = statDiff;
			return (short) (statValue + statDiff);
		}
	}

	[ViewableClass]
	public partial class StrModSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			self.Str = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Str, (short) this.EffectPower, out this.strDifference);
		}

		public override void On_UnAssign(Character cont) {
			cont.Str -= this.strDifference;
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class StrModSpellEffectPluginDef {
	}

	[ViewableClass]
	public partial class DexModSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			self.Dex = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Dex, (short) this.EffectPower, out this.dexDifference);
		}

		public override void On_UnAssign(Character cont) {
			cont.Dex -= this.dexDifference;
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class DexModSpellEffectPluginDef {
	}

	[ViewableClass]
	public partial class IntModSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			short shortEffect = (short) this.EffectPower;

			self.Int = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Int, shortEffect, out this.intDifference);
			//also lower npc maxmana?

			if (shortEffect < 0) {
				self.Mana = Math.Min(self.Mana, self.MaxMana);
			}
		}

		public override void On_UnAssign(Character cont) {
			cont.Int -= this.intDifference;
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class IntModSpellEffectPluginDef {
	}

	[ViewableClass]
	public partial class BlessSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			short shortEffect = (short) this.EffectPower;
			self.Str = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Str, shortEffect, out this.strDifference);
			self.Dex = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Dex, shortEffect, out this.dexDifference);
			self.Int = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Int, shortEffect, out this.intDifference);

			if (shortEffect < 0) {
				self.Mana = Math.Min(self.Mana, self.MaxMana);
			}
		}

		public override void On_UnAssign(Character cont) {
			cont.Str -= this.strDifference;
			cont.Dex -= this.dexDifference;
			cont.Int -= this.intDifference;
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class BlessSpellEffectPluginDef {
	}

	[ViewableClass]
	public partial class CurseSpellEffectPlugin {
		public void On_Assign() {
			Character self = (Character) this.Cont;
			short shortEffect = (short) this.EffectPower;

			self.Str = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Str, shortEffect, out this.strDifference);
			self.Dex = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Dex, shortEffect, out this.dexDifference);
			self.Int = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.Int, shortEffect, out this.intDifference);

			Player contAsPlayer = self as Player;
			if (contAsPlayer != null) {
				contAsPlayer.Vit = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, contAsPlayer.Vit, shortEffect, out this.maxHitsDifference);
			} else {
				NPC contAsNPC = (NPC) self;

				contAsNPC.MaxStam = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.MaxStam, shortEffect, out this.maxStamDifference);
				contAsNPC.MaxMana = StatModSpellsUtils.ModifyStat(StatModSpellsUtils.minStat, self.MaxMana, shortEffect, out this.maxManaDifference);

				//TODO
				//Player sourceAsPlayer = this.Source as Player;
				//if ((sourceAsPlayer != null) && ((this.SourceType == SpellSourceType.Book) || (this.SourceType == SpellSourceType.Scroll))) {
					//if (cont.tag(classcastera)==necro)
					//    if (cont.tag(resist_fire))
					//        if (<cont.tag(resist_fire)> < 990)
					//            cont.tag(resist_fire,#-<cont.tag(levelcastera)>)
					//            finduid(cont.tag(UIDcastera)).sysmessage("<cont.name> je nyni zranitelnejsi ohnem.")
					//            tag(rfire,1)
					//        endif
					//        elseif !(cont.tag(resist_fire))
					//        cont.tag(resist_fire,-<cont.tag(levelcastera)>)
					//        finduid(cont.tag(UIDcastera)).sysmessage("<cont.name> je nyni zranitelnejsi ohnem.")
					//        tag(rfire,1)
					//    endif
					//endif
					//if (cont.tag(classcastera)==shaman)
					//    if (cont.tag(resist_cold))
					//        if (<cont.tag(resist_cold)> < 990)
					//            cont.tag(resist_cold,#-<cont.tag(levelcastera)>)
					//            finduid(<cont.tag(UIDcastera)>).sysmessage("Cil je nyni zranitelnejsi mrazem.")
					//            tag(rcold,1)
					//            endif
					//            elseif !(cont.tag(resist_cold))
					//            cont.tag(resist_cold,-<cont.tag(levelcastera)>)
					//            finduid(cont.tag(UIDcastera)).sysmessage("<cont.name> je nyni zranitelnejsi mrazem.")
					//            tag(rcold,1)
					//        endif
					//    endif
					//endif 
				//}
			}


			if (shortEffect < 0) {
				self.Hits = Math.Min(self.Hits, self.MaxHits);
				self.Stam = Math.Min(self.Stam, self.MaxStam);
				self.Mana = Math.Min(self.Mana, self.MaxMana);
			}
		}

		public override void On_UnAssign(Character cont) {
			cont.Str -= this.strDifference;
			cont.Dex -= this.dexDifference;
			cont.Int -= this.intDifference;
			Player contAsPlayer = cont as Player;
			if (contAsPlayer != null) {
				contAsPlayer.Vit -= this.maxHitsDifference;
			} else {
				NPC contAsNPC = (NPC) cont;
				contAsNPC.MaxStam -= this.maxStamDifference;
				contAsNPC.MaxMana -= this.maxManaDifference;

				contAsNPC.ResistFire -= this.resistFireDifference;
				contAsNPC.ResistCold -= this.resistColdDifference;
			}
			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class CurseSpellEffectPluginDef {
	}
}