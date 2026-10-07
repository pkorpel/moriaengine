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
using SteamEngine.LScript;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	public sealed class ScriptedTargetDef : AbstractTargetDef {
		private FieldValue message;

		private LScriptHolder on_start;
		private LScriptHolder targon_ground;
		private LScriptHolder targon_item;
		private LScriptHolder targon_char;
		private LScriptHolder targon_thing;
		private LScriptHolder targon_static;
		private LScriptHolder targon_cancel;
		private LScriptHolder targon_point;

		public ScriptedTargetDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.message = this.InitTypedField("message", "Target?", typeof(string));
		}

		public override void LoadScriptLines(PropsSection ps) {
			base.LoadScriptLines(ps);

			LoadTriggers(ps, this);
		}

		public string Message {
			get {
				return (string) this.message.CurrentValue;
			}
			set {
				this.message.CurrentValue = value;
			}
		}

		protected override bool AllowGround {
			get {
				return ((this.targon_ground != null) || (this.targon_point != null));
			}
		}

		private static void LoadTriggers(PropsSection input, ScriptedTargetDef td) {
			TriggerSection trigger_start = input.PopTrigger("start");
			if (trigger_start != null) {
				td.on_start = new LScriptHolder(trigger_start);
			}

			int n = input.TriggerCount;
			TriggerSection trigger_point = input.GetTrigger("targon_coords");
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_coordinates");
			}
			if (trigger_point == null) {
				trigger_point = input.GetTrigger("targon_point");
			}
			if (trigger_point != null) {
				if (n > 1) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has targon_point defined. All other triggers ignored.");
				}
				td.targon_point = new LScriptHolder(trigger_point);
			} else {

				for (int i = 0; i < n; i++) {
					TriggerSection trigger = input.GetTrigger(i);
					switch (trigger.TriggerName.ToLowerInvariant()) {
						case "targon_ground":
							td.targon_ground = new LScriptHolder(trigger);
							break;
						case "targon_item":
							td.targon_item = new LScriptHolder(trigger);
							break;
						case "targon_char":
						case "targon_character":
							td.targon_char = new LScriptHolder(trigger);
							break;
						case "targon_thing":
							td.targon_thing = new LScriptHolder(trigger);
							break;
						case "targon_static":
							td.targon_static = new LScriptHolder(trigger);
							break;
						case "targon_cancel":
							td.targon_cancel = new LScriptHolder(trigger);
							break;
						default:
							Logger.WriteWarning(trigger.Filename, trigger.StartLine, LogStr.Ident(trigger.TriggerName) + " is an invalid trigger name for a ScriptedTargetDef section. Ignored.");
							break;
					}
				}

				if ((td.targon_thing != null) && (td.targon_item != null)) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has both @targon_thing and @targon_item defined. @targon_item ignored.");
					td.targon_item = null;
				}
				if ((td.targon_thing != null) && (td.targon_char != null)) {
					Logger.WriteWarning(input.Filename, input.HeaderLine, "ScriptedTargetDef " + LogStr.Ident(input) + " has both @targon_thing and @targon_char defined. @targon_char ignored.");
					td.targon_char = null;
				}
			}
		}

		protected override void On_Start(Player ch, object parameter) {
			this.ThrowIfUnloaded();
			if (this.on_start != null) {
				if (this.TryRunTrigger(this.on_start, ch, parameter)) {
					return;
				}
			} else {
				string msg = this.Message;
				if (!string.IsNullOrEmpty(msg)) {
					ch.SysMessage(this.Message);
				}
			}
			base.On_Start(ch, parameter);
		}

		protected override void On_Targon(GameState state, IPoint3D getback, object parameter) {
			Player player = state.Character as Player;
			if (player != null) {
				if (this.targon_point != null) {
					if (this.TryRunTrigger(this.targon_point, player, getback, parameter)) {
						this.On_Start(player, parameter);
					}
					return;
				}
				Thing targettedThing = getback as Thing;
				if (targettedThing != null) {
					if (this.targon_thing != null) {
						if (this.TryRunTrigger(this.targon_thing, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
					Character targettedChar = getback as Character;
					if ((targettedChar != null) && (this.targon_char != null)) {
						if (this.TryRunTrigger(this.targon_char, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
					Item targettedItem = getback as Item;
					if ((targettedItem != null) && (this.targon_item != null)) {
						if (this.TryRunTrigger(this.targon_item, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
				} else {
					AbstractInternalItem targettedStatic = getback as AbstractInternalItem;
					if (targettedStatic != null)
					{
						if (this.targon_static != null) {
							if (this.TryRunTrigger(this.targon_static, player, getback, parameter)) {
								this.On_Start(player, parameter);
							}
							return;
						}
						if (this.targon_ground != null) {
							if (this.TryRunTrigger(this.targon_ground, player, getback, parameter)) {
								this.On_Start(player, parameter);
							}
							return;
						}
					}
					if (this.targon_ground != null) {
						if (this.TryRunTrigger(this.targon_ground, player, getback, parameter)) {
							this.On_Start(player, parameter);
						}
						return;
					}
				}
			}
			PacketSequences.SendClilocSysMessage(state.Conn, 1046439, 0);//That is not a valid target.
			this.On_Start(player, parameter);
		}

		protected override void On_TargonCancel(GameState state, object parameter) {
			AbstractCharacter ch = state.Character;
			if ((ch != null) && (this.targon_cancel != null)) {
				this.targon_cancel.TryRun(ch, parameter);
			}
		}

		private bool TryRunTrigger(LScriptHolder script, AbstractCharacter self, params object[] parameters) {
			object retVal = script.TryRun(self, parameters);
			try {
				int retInt = Convert.ToInt32(retVal);
				if (retInt == 1) {
					return true;
				}
			} catch (Exception) {
			}
			return false;
		}

		public override void Unload() {
			this.targon_ground = null;
			this.targon_item = null;
			this.targon_char = null;
			this.targon_thing = null;
			this.targon_static = null;
			this.targon_cancel = null;
			this.targon_point = null;

			base.Unload();
		}
	}
}