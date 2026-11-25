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

using System.Collections.Generic;
using SteamEngine.Common;

//using SteamEngine.PScript;

namespace SteamEngine {
	public abstract class AbstractDefTriggerGroupHolder : AbstractDef, ITriggerGroupHolder {

		//attention! this class does not (yet?) use the prevNode field on TGListNode, cos we don't need it here.
		internal PluginHolder.TGListNode firstTGListNode; //linked list of triggergroup references

		private static List<TGResolver> tgResolvers = new List<TGResolver>();

		protected AbstractDefTriggerGroupHolder(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "event":
				case "tevent":
				case "events":
				case "tevents":
				case "speech":
				case "tspeech":
				case "triggergroup":
				case "triggergroups":
					//case "resources"://in sphere, resources are the same like events... is it gonna be that way too in SE? NO!
					tgResolvers.Add(new TGResolver(this, args, filename, line));
					break;
				default:
					base.LoadScriptLine(filename, line, param, args);//the AbstractDef Loadline
					break;
			}
		}

		//This is necessary, since when our ThingDef are being made, not all scripts may have been loaded yet. -SL
		private class TGResolver {
			AbstractDefTriggerGroupHolder cont;
			string filename, tgName;
			int line;

			internal TGResolver(AbstractDefTriggerGroupHolder cont, string tgName, string filename, int line) {
				this.cont = cont;
				this.tgName = tgName;
				this.filename = filename;
				this.line = line;
			}

			internal void Resolve() {
				if (this.tgName != "0") {	//"0" means nothing
					TriggerGroup tg = TriggerGroup.GetByDefname(this.tgName);
					if (tg == null) {
						Logger.WriteWarning(LogStr.FileLine(this.filename, this.line) + "'" + LogStr.Ident(this.tgName) + "' is not a valid TriggerGroup (Event/Type).");
					} else {
						this.cont.AddTriggerGroup(tg);
					}
				}
			}
		}

		internal new static void LoadingFinished() {
			foreach (TGResolver resolver in tgResolvers) {
				resolver.Resolve();
			}
			tgResolvers = new List<TGResolver>();
		}

		public void AddTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (this.firstTGListNode == null) {
				this.firstTGListNode = new PluginHolder.TGListNode(tg);
			} else {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				while (true) {
					if (curNode.storedTG == tg) {
						return;// false;//we already have it
					}
					if (curNode.nextNode == null) {
						curNode.nextNode = new PluginHolder.TGListNode(tg);
						return;
					}
					curNode = curNode.nextNode;
				}
				//return true;//we had to add it
			}
		}

		public IEnumerable<TriggerGroup> GetAllTriggerGroups() {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					yield return curNode.storedTG;
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		public void RemoveTriggerGroup(TriggerGroup tg) {
			if (tg == null) return;
			if (this.firstTGListNode != null) {
				if (this.firstTGListNode.storedTG == tg) {
					this.firstTGListNode = this.firstTGListNode.nextNode;
					return;
				}
				PluginHolder.TGListNode lastNode = this.firstTGListNode;
				PluginHolder.TGListNode curNode = lastNode.nextNode;
				while (curNode != null) {
					if (curNode.storedTG == tg) {
						lastNode.nextNode = curNode.nextNode;
						return;
					}
					lastNode = curNode;
					curNode = curNode.nextNode;
				}
				//return false;//we didnt have it, so we didnt do anything
			}
		}

		public bool HasTriggerGroup(TriggerGroup tg) {
			if (tg == null) return false;
			PluginHolder.TGListNode curNode = this.firstTGListNode;
			do {
				if (curNode.storedTG == tg) {
					return true;
				}
				curNode = curNode.nextNode;
			} while (curNode != null);
			return false;
		}

		public void ClearTriggerGroups() {
			this.firstTGListNode = null;
		}


		public override void Unload() {
			//if (firstTGListNode != null) {
			//    PluginHolder.TGListNode curNode = firstTGListNode;
			//    do {
			//        curNode.storedTG.Unload();
			//        curNode = curNode.nextNode;
			//    } while (curNode != null);
			//}
			this.firstTGListNode = null;
			base.Unload();
		}

		/// <summary>
		/// Triggers a trigger on this object, using the specified ScriptArgs
		/// </summary>
		/// <param name="tk">The TriggerKey for the trigger to call.</param>
		/// <param name="sa">The arguments (other than argv) for sphere scripts</param>
		public virtual void Trigger(TriggerKey tk, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					curNode.storedTG.Run(this, tk, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		public virtual void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					curNode.storedTG.TryRun(this, tk, sa);
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
		}

		/// <summary>
		/// Executes the trigger, reads return values, and returns true if anything returned 1 (returning false otherwise).
		/// </summary>
		/// <param name="tk">The trigger to execute</param>
		/// <param name="sa">Arguments for scripts (argn, args, argo, argn1, argn2, etc). Can be null.</param>
		/// <returns>TriggerResult.Cancel if any called trigger scripts returned 1, TriggerResult.Continue otherwise.</returns>
		public virtual TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					if (TagMath.Is1(curNode.storedTG.Run(this, tk, sa))) {
						return TriggerResult.Cancel;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return TriggerResult.Continue;
		}

		public virtual TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			if (this.firstTGListNode != null) {
				PluginHolder.TGListNode curNode = this.firstTGListNode;
				do {
					if (TagMath.Is1(curNode.storedTG.TryRun(this, tk, sa))) {
						return TriggerResult.Cancel;
					}
					curNode = curNode.nextNode;
				} while (curNode != null);
			}
			return TriggerResult.Continue;
		}

		public void Trigger(TriggerKey tk, params object[] scriptArguments) {
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				this.Trigger(tk, new ScriptArgs(scriptArguments));
			} else {
				this.Trigger(tk, (ScriptArgs) null);
			}
		}

		public TriggerResult CancellableTrigger(TriggerKey tk, params object[] scriptArguments)
		{
			if ((scriptArguments != null) && (scriptArguments.Length > 0)) {
				return this.CancellableTrigger(tk, new ScriptArgs(scriptArguments));
			}
			return this.CancellableTrigger(tk, (ScriptArgs) null);
		}
	}
}
