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

		/// <summary>Initialize the profession on the character</summary>
		public virtual void On_Assign() {
			Sanity.IfTrueThrow(this.profession == null, "this.def == null on ProfessionPlugin assigning");

			//maybe we should do this on the first use of said skill/spell/ability...? Or maybe just in the startroom stone script?
			//changing of profession should be available for testing on players, without messing them up right away...

			//SkillName oneName;
			//foreach (ISkill skill in ((Player) Cont).Skills) {//the Cont of the Profession is always Player...
			//    oneName = (SkillName) skill.Id;
			//    skill.RealValue = Math.Min(def.BasicSkill(oneName), def.MaxSkill(oneName));
			//}
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
				if (!this.profession.AllowedSpells.Contains(spell)) {
					player.RedMessage(string.Format(CultureInfo.InvariantCulture,
						Loc<ProfessionPluginLoc>.Get(player.Language).YouCantCastThis,
						this.profession.Name));

					return TriggerResult.Cancel; //we don't know that spell
				}
			}
			return TriggerResult.Continue;
		}

		public static ProfessionPlugin GetInstalledPlugin(Player player) {
			return (ProfessionPlugin) player.GetPlugin(professionKey);
		}

		public static void InstallProfessionPlugin(Player player, ProfessionDef professionDef) {
			if (professionDef != null) {
				ProfessionPlugin newPlugin = (ProfessionPlugin) professionDef.ProfessionPluginDef.Create();
				newPlugin.profession = professionDef;
				player.AddPlugin(professionKey, newPlugin); //if there was a previous one, it's deleted now
			} else {
				player.DeletePlugin(professionKey); //null professiondef = no profession = no professionplugin
			}
		}
	}

	[ViewableClass]
	public partial class ProfessionPluginDef {
	}

	public class ProfessionPluginLoc : CompiledLocStringCollection<ProfessionPluginLoc> {
		public string YouCantCastThis = "You can't cast this spell, being {0} of profession.";
	}
}