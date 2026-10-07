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

	/// <summary>War's warcry</summary>
	[ViewableClass]
	public class WarcryDef : ImmediateAbilityDef {
		public WarcryDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		#region triggerMethods

		/// <summary>Functional implementation of warcry ability</summary>
		protected override void On_Activate(Character chr, Ability ab) {
			//TODO - taky nejak zarvat nebo co !

			double power = ab.Def.EffectPower * ab.ModifiedPoints; //
			TimeSpan duration = TimeSpan.FromSeconds(ab.Def.EffectDuration);

			foreach (Player target in chr.GetMap().GetPlayersInRange(chr.X, chr.Y, (ushort) this.ComputeRange(chr))) {
				if (chr == target) {
					continue; //dont do selfwarcry ;)
				}

				WarcryEffectPlugin warcryEffect = (WarcryEffectPlugin) WarcryEffectPlugin.defInstance.Create();
				warcryEffect.Init(chr, EffectFlag.FromAbility | EffectFlag.HarmfulEffect, power, duration, this);
				target.AddPlugin(WarcryEffectPlugin.warcyEffectPluginKey, warcryEffect);
			}
		}
		#endregion triggerMethods

		/// <summary>
		/// Compute the warcry range using the information from character (using i.e char's level
		/// and the ability points...). Consider that 18 steps should be maximum (client limits)
		/// </summary>
		private int ComputeRange(Character chr) {
			//TODO - udelat to nejak sofistikovaneji			
			return chr.GetAbility(this);
		}

		/// <summary>
		/// Running the warcry (if the player has the ability)
		/// </summary>
		[SteamFunction("Warcry")]
		public static void WarcryFunction(Character chr, ScriptArgs args) {
			WarcryDef wcrDef = SingletonScript<WarcryDef>.Instance;
			wcrDef.Activate(chr);
		}
	}

	[ViewableClass]
	public partial class WarcryEffectPlugin {

		public static readonly WarcryEffectPluginDef defInstance =
			(WarcryEffectPluginDef) new WarcryEffectPluginDef("p_warcryEffect", "C# scripts", -1).Register();
		internal static PluginKey warcyEffectPluginKey = PluginKey.Acquire("_warcryEffect_");

		public TriggerResult On_SkillSelect(SkillSequenceArgs ssa) {
			if (ssa.SkillDef.Id == (int) SkillName.Magery) {
				((Character) this.Cont).RedMessage("Jsi v šoku a nemùžeš kouzlit!");
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		public TriggerResult On_SkillStart(SkillSequenceArgs ssa) {
			if (ssa.SkillDef.Id == (int) SkillName.Magery) {
				((Character) this.Cont).RedMessage("Jsi v šoku a nemùžeš kouzlit!");
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		public void On_Assign() {
			((Character) this.Cont).RedMessage("Jsi v šoku!");
		}

		public override void On_UnAssign(Character formerCont) {
			formerCont.SysMessage("Neblahé úèinky warcry pominuly");
			//base.On_UnAssign(formerCont);
		}
	}
}
