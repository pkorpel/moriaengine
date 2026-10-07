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
	/// Ability class serving as a parent for special types of abilities that can assign a plugin or a 
	/// trigger group (or both) to the ability holder when activated.
	/// The performing of the ability ends when it is either manually deactivated or some other conditions are fulfilled
	/// The included TriggerGroup/Plugin will be attached to the holder after activation (and removed after deactivation)
	/// </summary>
	[ViewableClass]
	public class ActivableAbilityDef : AbilityDef {
		//internal static readonly TriggerKey tkDeactivate = TriggerKey.Acquire("deActivate");
		//internal static readonly TriggerKey tkDeactivateAbility = TriggerKey.Acquire("deActivateAbility");

		//fields for storing the keys (comming from LScript or set in constructor of children)
		private FieldValue pluginDef;
		private FieldValue pluginKey;

		public ActivableAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//initialize the field value
			//the field will be in the LScript as follows:
			//[ActivableAbilityDef a_bility]
			//...
			//pluginDef=p_some_plugindef
			//pluginKey=some_pluginkey
			//these values will be then used for assigning plugin to the ability holder
			//...
			//we expect the values from Lscript as follows
			//triggerGroup = InitTypedField("triggerGroup", null, typeof(TriggerGroup)); //which trigger group will be stored on ability holder
			this.pluginDef = this.InitTypedField("pluginDef", null, typeof(PluginDef)); //which plugin will be stored on ability holder
			this.pluginKey = this.InitTypedField("pluginKey", null, typeof(PluginKey)); //how the plugin will be stored on ability holder
		}

		public bool IsActive(Character ch) {
			PluginKey key = this.PluginKey;
			if (key != null) {
				Plugin plugin = ch.GetPlugin(key);
				if (plugin != null) {
					return plugin.Def == this.PluginDef;
				}
			}
			return false;
		}

		/// <summary>If its is running - deactivate, otherwise - activate.</summary>
		public void Switch(Character chr) {
			PluginKey key = this.PluginKey;
			if (key != null) {
				Plugin plugin = chr.GetPlugin(key);
				if (plugin != null) {
					plugin.Delete();
					return;
				}
			}
			this.Activate(chr); //try to activate
		}

		public override void Activate(Character chr) {
			if (!this.IsActive(chr)) {
				base.Activate(chr); //try to activate
			}
		}

		/// <summary>Add Plugin to ability holder, if specified for this AbilityDef</summary>
		protected override void On_Activate(Character chr, Ability ab) {
			PluginDef def = this.PluginDef;
			PluginKey key = this.PluginKey;
			Sanity.IfTrueThrow((def == null) != (key == null),
				"Both PluginDef and PluginKey must be defined for " +
				this + ", or neither."); //could not hold in some curious cases, we'll see

			if ((def != null) && (key != null)) {
				Plugin plugin = def.Create();
				EffectDurationPlugin durationPlugin = plugin as EffectDurationPlugin;
				if (durationPlugin != null) {
					durationPlugin.Init(chr, EffectFlag.BeneficialEffect | EffectFlag.FromAbility,
						this.EffectPower * ab.ModifiedPoints, TimeSpan.MinValue, this); //the "power" formula is somewhat arbitrary, but this particular combination (points * effect) seems to be quite popular
				}
				chr.AddPlugin(key, plugin);
			}

			base.On_Activate(chr, ab);
		}

		public void Deactivate(Character chr) {
			PluginKey key = this.PluginKey;
			if (key != null) {
				chr.DeletePlugin(key);
			}
		}

		#region triggerMethods
		/// <summary>When unassigning, do not forget to deactivate the ability</summary>
		protected override void On_UnAssign(Character ch, Ability ab) {
			this.Deactivate(ch); //deactivate the ability automatically
		}
		#endregion triggerMethods

		/// <summary>
		/// Plugindef connected with this ability (can be null if no key is specified). It will be used 
		/// for creating plugin instances and setting them to the ability holder
		/// </summary>
		public PluginDef PluginDef {
			get {
				return (PluginDef) this.pluginDef.CurrentValue;
			}
			set {
				this.pluginDef.CurrentValue = value;
			}
		}

		/// <summary>Return plugin key from the field value (used e.g. for adding/removing plugins to the character)</summary>
		public PluginKey PluginKey {
			get {
				return (PluginKey) this.pluginKey.CurrentValue;
			}
			set {
				this.pluginKey.CurrentValue = value;
			}
		}
	}
}
