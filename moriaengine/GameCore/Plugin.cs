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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class Plugin : TagHolder {
		internal PluginHolder cont;
		internal Plugin prevInList;
		internal Plugin nextInList;

		internal PluginDef def;

		private bool isDeleted;

		protected Plugin(Plugin copyFrom)
			: base(copyFrom) {
			this.def = copyFrom.def;
		}

		protected Plugin() {
		}

		public PluginDef Def {
			get {
				return this.def;
			}
		}

		public PluginDef TypeDef {
			get {
				return this.def;
			}
		}

		public PluginHolder Cont {
			get {
				return this.cont;
			}
		}

		//the first trigger that throws an exceptions terminates the other ones that way
		public void Run(TriggerKey tk, ScriptArgs sa, out object scriptedRetVal, out object compiledRetVal) {
			scriptedRetVal = null;
			compiledRetVal = null;
			if (!this.isDeleted) {
				TriggerGroup scriptedTriggers = this.def.scriptedTriggers;
				if (scriptedTriggers != null) {
					scriptedRetVal = scriptedTriggers.Run(this, tk, sa);
				}

				PluginDef.PluginTriggerGroup compiledTriggers = this.def.compiledTriggers;
				if (compiledTriggers != null) {
					compiledRetVal = compiledTriggers.Run(this, tk, sa);
				}
			}
		}

		//does not throw the exceptions - all triggers are run, regardless of their errorness
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void TryRun(TriggerKey tk, ScriptArgs sa, out object scriptedRetVal, out object compiledRetVal) {
			scriptedRetVal = null;
			compiledRetVal = null;
			if (!this.isDeleted) {
				TriggerGroup scriptedTriggers = this.def.scriptedTriggers;
				if (scriptedTriggers != null) {
					scriptedRetVal = scriptedTriggers.TryRun(this, tk, sa);
				}
				PluginDef.PluginTriggerGroup compiledTriggers = this.def.compiledTriggers;
				if (compiledTriggers != null) {
					try {
						compiledRetVal = compiledTriggers.Run(this, tk, sa);
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e);
					}
				}
			}
		}

		public override void Delete() {
			if (this.cont != null) {
				this.cont.RemovePlugin(this);
			}
			if (!this.isDeleted) {
				object scriptedRetVal, compiledRetVal;
				this.TryRun(TriggerKey.destroy, null, out scriptedRetVal, out compiledRetVal);
				this.isDeleted = true;
			}
			base.Delete();
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + this.Def.PrettyDefname;
		}

		#region save/load
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public override void Save(SaveStream output) {
			output.WriteValue("def", this.def);
			base.Save(output);
		}

		[LoadLine]
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			if ("def".Equals(valueName, StringComparison.OrdinalIgnoreCase)) {
				this.def = (PluginDef) ObjectSaver.OptimizedLoad_Script(valueString);
			} else {
				base.LoadLine(filename, line, valueName, valueString);
			}
		}

		[LoadingFinalizer]
		public virtual void LoadingFinished() {
			if (this.def == null) {

				//Delete() calls @destroy triggers, which require def. 
				//This is an emergency deleting of an object in wrong state, so there's no real correct solution anyway...
				if (this.cont != null) {
					this.cont.RemovePlugin(this);
				}
				base.Delete();

				throw new SEException("No PluginDef reference loaded for this "+this.GetType().Name+". Deleting.");
			}
		}
		#endregion save/load
	}
}
