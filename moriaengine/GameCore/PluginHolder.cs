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

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

namespace SteamEngine {
	public interface ITriggerGroupHolder {
		void AddTriggerGroup(TriggerGroup tg);
		void ClearTriggerGroups();
		bool HasTriggerGroup(TriggerGroup tg);
		void RemoveTriggerGroup(TriggerGroup tg);

		void Trigger(TriggerKey tk, ScriptArgs sa);
		void TryTrigger(TriggerKey tk, ScriptArgs sa);
		TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa);
		TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa);

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		IEnumerable<TriggerGroup> GetAllTriggerGroups();
	}

	public interface IPluginHolder : ITriggerGroupHolder {
		Plugin AddPlugin(PluginKey pk, Plugin plugin);
		Plugin AddPluginAsSimple(PluginKey pk, Plugin plugin);
		void DeletePlugins();
		Plugin GetPlugin(PluginKey pk);
		bool HasPlugin(PluginKey pk);
		bool HasPlugin(Plugin plugin);
		Plugin RemovePlugin(Plugin plugin);
		Plugin RemovePlugin(PluginKey pk);
		void DeletePlugin(PluginKey pk);

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		IEnumerable<Plugin> GetAllPlugins();
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		IEnumerable<Plugin> GetNonSimplePlugins();
	}

	/*
		Class: TagHolder
		All Things (Items and Characters) and GameAccounts are TagHolders, as is Server.globals.
	*/
	public class PluginHolder : TagHolder, IPluginHolder {
		private TGListNode firstTGListNode;	//double-linked list of triggergroup references. We only have it for fast "foreach" operation in Trigger methods. Lookup goes thru tags Hashtable
		private Plugin firstPlugin;			//double-linked list of Plugins. We only have it for fast "foreach" operation in Trigger methods. Lookup goes thru tags Hashtable

		public PluginHolder() {
		}

		#region Copying constructor
		public PluginHolder(PluginHolder copyFrom)
			: base(copyFrom) { //copying constuctor
			if (copyFrom.firstTGListNode != null) {
				this.EnsureTagsTable();

				//copy triggergroups
				TGListNode curNode = new TGListNode(copyFrom.firstTGListNode.storedTG);
				this.firstTGListNode = curNode;
				TGListNode copiedNode = copyFrom.firstTGListNode.nextNode;
				while (copiedNode != null) {
					curNode.nextNode = new TGListNode(copiedNode.storedTG);
					this.tags[copiedNode.storedTG] = curNode.nextNode;
					curNode.nextNode.prevNode = curNode;
					curNode = curNode.nextNode;
					copiedNode = copiedNode.nextNode;
				}
			}

			//copy plugins
			if (copyFrom.tags != null) {
				foreach (DictionaryEntry entry in copyFrom.tags) {
					PluginKey pk = entry.Key as PluginKey;
					if (pk != null) {
						DeepCopyFactory.GetCopyDelayed(entry.Value, this.DelayedGetCopy_Plugin, pk);
					}
				}

				//now set nondelayed ones. If all works well, they shouldn't be copied twice
				Plugin curPlugin = copyFrom.firstPlugin;
				while (curPlugin != null) {
					DeepCopyFactory.GetCopyDelayed(curPlugin, this.DelayedGetCopy_SetPluginAsNonSimple);
					curPlugin = curPlugin.nextInList;
				}
			}
		}

		private void DelayedGetCopy_SetPluginAsNonSimple(object resolvedObject) {
			Plugin plugin = (Plugin) resolvedObject;
			if (this.firstPlugin != null) {
				this.firstPlugin.prevInList = plugin;
				plugin.nextInList = this.firstPlugin;
			}
			this.firstPlugin = plugin;
		}

		private void DelayedGetCopy_Plugin(object resolvedObject, object pluginKey) {
			this.AddPluginImpl((PluginKey) pluginKey, (Plugin) resolvedObject);
		}
		#endregion Copying constructor

		public override void Delete() {
			this.DeletePlugins();
			this.ClearTriggerGroups();
			base.Delete();
		}

		#region Triggergroups

		public void AddTriggerGroup(TriggerGroup tg) {
			if (this.PrivateAddTriggerGroup(tg)) {
				tg.TryRun(this, TriggerKey.assign, null);
			}
		}

		private bool PrivateAddTriggerGroup(TriggerGroup tg) {
			if (tg == null)
				return false;
			if (this.tags != null) {
				if (this.tags.ContainsKey(tg)) {
					return false;
				}
			} else {
				this.tags = new Hashtable();
			}

			TGListNode listNode = new TGListNode(tg);
			this.tags[tg] = listNode;
			if (this.firstTGListNode != null) {
				this.firstTGListNode.prevNode = listNode;
				listNode.nextNode = this.firstTGListNode;
			}
			this.firstTGListNode = listNode;
			return true;
		}

		public void RemoveTriggerGroup(TriggerGroup tg) {
			if (tg == null)
				return;
			if (this.tags != null) {
				TGListNode listNode = this.tags[tg] as TGListNode;
				if (listNode != null) {
					if (listNode.prevNode != null) {
						listNode.prevNode.nextNode = listNode.nextNode;
					} else {//no prev, means we were the first
						this.firstTGListNode = listNode.nextNode;
					}
					if (listNode.nextNode != null) {
						listNode.nextNode.prevNode = listNode.prevNode;
					}
					tg.TryRun(this, TriggerKey.unAssign, null);
				}
			}
		}

		public bool HasTriggerGroup(TriggerGroup tg) {
			if (this.tags != null) {
				return this.tags.ContainsKey(tg);
			}
			return false;
		}

		public IEnumerable<TriggerGroup> GetAllTriggerGroups() {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				yield return curNode.storedTG;
				curNode = curNode.nextNode;
			}
		}

		public void ClearTriggerGroups() {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				this.tags.Remove(curNode.storedTG);
				curNode = curNode.nextNode;
			}
			this.firstTGListNode = null;
		}

		internal class TGListNode {
			public readonly TriggerGroup storedTG;
			internal TGListNode prevNode;
			internal TGListNode nextNode;

			internal TGListNode(TriggerGroup storedTG) {
				this.storedTG = storedTG;
			}
		}

		#endregion Triggergroups

		#region Trigger() methods
		/*
			Method: Trigger
			Triggers a trigger on this object, using the specified ScriptArgs
			
			Parameters:
				sa - The arguments (other than argv) for sphere scripts
				td - The TriggerKey for the trigger to call.
		
			Returns:
				The return value(s) from the triggers called.
		
			See also:
				<Trigger>, <CancellableTriggers>
		*/
		public virtual void Trigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				curNode.storedTG.Run(this, tk, sa);
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = this.firstPlugin;
			while (curPlugin != null) {
				object scriptedRetVal, compiledRetVal;
				curPlugin.Run(tk, sa, out scriptedRetVal, out compiledRetVal);
				curPlugin = curPlugin.nextInList;
			}
		}

		public virtual void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				curNode.storedTG.TryRun(this, tk, sa);
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = this.firstPlugin;
			while (curPlugin != null) {
				object scriptedRetVal, compiledRetVal;
				curPlugin.TryRun(tk, sa, out scriptedRetVal, out compiledRetVal);
				curPlugin = curPlugin.nextInList;
			}
		}

		/*
			Method: CancellableTrigger
			Executes the trigger, reads return values, and returns true if anything returned 1 (returning false otherwise).
			
			Parameters:
				td - The trigger to execute
				sa - Arguments for scripts (argn, args, argo, argn1, argn2, etc). Can be null.
			
			Returns:
				True if any called trigger scripts returned 1, false otherwise.
			
			See also:
				<Trigger>, <CancellableTriggers>
		*/

		public virtual TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				if (TagMath.Is1(curNode.storedTG.Run(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = this.firstPlugin;
			while (curPlugin != null) {
				object scriptedRetVal, compiledRetVal;
				curPlugin.Run(tk, sa, out scriptedRetVal, out compiledRetVal);
				if (TagMath.Is1(scriptedRetVal) || TagMath.Is1(compiledRetVal)) {
					return TriggerResult.Cancel;
				}
				curPlugin = curPlugin.nextInList;
			}
			return TriggerResult.Continue;
		}

		public virtual TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				if (TagMath.Is1(curNode.storedTG.TryRun(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
				curNode = curNode.nextNode;
			}
			Plugin curPlugin = this.firstPlugin;
			while (curPlugin != null) {
				object scriptedRetVal, compiledRetVal;
				curPlugin.TryRun(tk, sa, out scriptedRetVal, out compiledRetVal);
				if (TagMath.Is1(scriptedRetVal) || TagMath.Is1(compiledRetVal)) {
					return TriggerResult.Cancel;
				}
				curPlugin = curPlugin.nextInList;
			}
			return TriggerResult.Continue;
		}
		#endregion Trigger() methods

		#region save/load
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			TGListNode curNode = this.firstTGListNode;
			while (curNode != null) {
				output.WriteValue("TriggerGroup", curNode.storedTG);
				curNode = curNode.nextNode;
			}
			if (this.tags != null) {
				foreach (DictionaryEntry entry in this.tags) {
					object key = entry.Key;
					if (key is PluginKey) {
						Plugin value = (Plugin) entry.Value;
						if (!value.IsDeleted) {
							if ((value == this.firstPlugin) || (value.prevInList != null) || (value.nextInList != null)) {
								output.WriteValue("@@" + key, value);
							} else {
								output.WriteValue("@@" + key + "*", value);
							}
						}
					}
				}
			}
			base.Save(output);
		}

		internal static Regex pluginKeyRE = new Regex(@"^\@@(?<name>.+?)(?<asterisk>\*)?\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			Match m = pluginKeyRE.Match(valueName);
			if (m.Success) {	//If the name begins with '@@'
				string pluginName = m.Groups["name"].Value;
				PluginKey tk = PluginKey.Acquire(pluginName);
				if (m.Groups["asterisk"].Value.Length > 0) {
					ObjectSaver.Load(valueString, this.DelayedLoad_SimplePlugin, filename, line, tk);
				} else {
					ObjectSaver.Load(valueString, this.DelayedLoad_Plugin, filename, line, tk);
				}
				return;
			}

			switch (valueName) {
				case "events":
				case "event":
				case "triggergroup":
				case "type":
					string tgName;
					m = ObjectSaver.abstractScriptRE.Match(valueString);
					if (m.Success) {
						tgName = m.Groups["value"].Value;
					} else {
						tgName = valueString;
					}
					TriggerGroup tg = TriggerGroup.GetByDefname(tgName);
					if (tg != null) {
						this.PrivateAddTriggerGroup(tg);
					} else {
						throw new SEException("TriggerGroup '" + tgName + "' does not exist.");
					}
					return;
			}
			base.LoadLine(filename, line, valueName, valueString);
		}

		private void DelayedLoad_Plugin(object resolvedObject, string filename, int line, object pluginKey) {
			Plugin plugin = (Plugin) resolvedObject;
			this.AddPluginImpl((PluginKey) pluginKey, plugin);
			if (this.firstPlugin != null) {
				this.firstPlugin.prevInList = plugin;
				plugin.nextInList = this.firstPlugin;
			}
			this.firstPlugin = plugin;
		}

		private void DelayedLoad_SimplePlugin(object resolvedObject, string filename, int line, object pluginKey) {
			this.AddPluginImpl((PluginKey) pluginKey, (Plugin) resolvedObject);
		}

		#endregion save/load

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public Plugin AddNewPlugin(PluginKey key, PluginDef def) {
			return this.AddPlugin(key, def.Create());
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public Plugin AddNewPluginAsSimple(PluginKey key, PluginDef def) {
			return this.AddPluginAsSimple(key, def.Create());
		}

		#region IPluginHolder Members
		public Plugin AddPlugin(PluginKey pk, Plugin plugin) {
			this.AddPluginImpl(pk, plugin);
			if (this.firstPlugin != null) {
				this.firstPlugin.prevInList = plugin;
				plugin.nextInList = this.firstPlugin;
			}
			this.firstPlugin = plugin;
			object scriptedRetVal, compiledRetVal;
			plugin.TryRun(TriggerKey.assign, null, out scriptedRetVal, out compiledRetVal);
			return plugin;
		}

		public Plugin AddPluginAsSimple(PluginKey pk, Plugin plugin) {
			this.AddPluginImpl(pk, plugin);
			object scriptedRetVal, compiledRetVal;
			plugin.TryRun(TriggerKey.assign, null, out scriptedRetVal, out compiledRetVal);
			return plugin;
		}

		private void AddPluginImpl(PluginKey pk, Plugin plugin) {
			if (plugin.cont != null) {
				plugin.cont.RemovePlugin(plugin);
			}
			if (this.tags != null) {
				PluginKey prevKey = this.tags[plugin] as PluginKey;
				if (prevKey != null && prevKey != pk) {
					throw new SEException("You can't assign one Plugin to one PluginHolder under 2 different PluginKeys");
				}

				Plugin prevPlugin = this.tags[pk] as Plugin;
				if (prevPlugin != null && prevPlugin != plugin) {
					this.RemovePlugin(prevPlugin).Delete();
				}
			} else {
				this.tags = new Hashtable();
			}
			this.tags[pk] = plugin;
			this.tags[plugin] = pk;
			plugin.cont = this;
		}

		public void DeletePlugins() {
			if (this.tags != null) {
				List<DictionaryEntry> allPlugins = new List<DictionaryEntry>();
				foreach (DictionaryEntry entry in this.tags) {
					if (entry.Key is PluginKey) {
						allPlugins.Add(entry);
					}
				}
				foreach (DictionaryEntry entry in allPlugins) {
					this.RemovePluginImpl((PluginKey) entry.Key, (Plugin) entry.Value).Delete();
				}
			}
		}

		public bool HasPlugin(PluginKey pk) {
			if (this.tags != null) {
				return this.tags.ContainsKey(pk);
			}
			return false;
		}

		public Plugin GetPlugin(PluginKey pk) {
			if (this.tags != null) {
				return this.tags[pk] as Plugin;
			}
			return null;
		}

		public bool HasPlugin(Plugin plugin) {
			return plugin.cont == this;
		}

		public Plugin RemovePlugin(Plugin plugin) {
			if (this.tags != null) {
				PluginKey pg = this.tags[plugin] as PluginKey;
				if (pg != null) {
					return this.RemovePluginImpl(pg, plugin);
				}
			}
			return null;
		}

		public Plugin RemovePlugin(PluginKey pk) {
			if (this.tags != null) {
				Plugin plugin = this.tags[pk] as Plugin;
				if (plugin != null) {
					return this.RemovePluginImpl(pk, plugin);
				}
			}
			return null;
		}

		public void DeletePlugin(PluginKey pk) {
			if (this.tags != null) {
				Plugin plugin = this.tags[pk] as Plugin;
				if (plugin != null) {
					plugin.Delete();
				}
			}
		}

		private Plugin RemovePluginImpl(PluginKey pg, Plugin plugin) {
			this.tags.Remove(pg);
			this.tags.Remove(plugin);
			if (plugin == this.firstPlugin) {
				this.firstPlugin = plugin.nextInList;
			}
			if (plugin.nextInList != null) {
				plugin.nextInList.prevInList = plugin.prevInList;
			}
			if (plugin.prevInList != null) {
				plugin.prevInList.nextInList = plugin.nextInList;
			}
			plugin.cont = null;
			plugin.prevInList = null;
			plugin.nextInList = null;
			object scriptedRetVal, compiledRetVal;
			plugin.TryRun(TriggerKey.unAssign, new ScriptArgs(this), out scriptedRetVal, out compiledRetVal);
			return plugin;
		}

		public IEnumerable<Plugin> GetNonSimplePlugins() {
			Plugin curPlugin = this.firstPlugin;
			while (curPlugin != null) {
				yield return curPlugin;
				curPlugin = curPlugin.nextInList;
			}
		}

		public IEnumerable<Plugin> GetAllPlugins() {
			if (this.tags != null) {
				foreach (DictionaryEntry entry in this.tags) {
					Plugin p = entry.Value as Plugin;
					if (p != null) {
						yield return p;
					}
				}
			}
		}

		/// <summary>
		/// Enumerate all plugins with their keys
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<KeyValuePair<PluginKey, Plugin>> GetAllPluginsWithKeys() {
			if (this.tags != null) {
				foreach (DictionaryEntry entry in this.tags) {
					PluginKey pk = entry.Key as PluginKey;
					if (pk != null) {
						//returning PluginKey-Plugin pair (if the key is of type PluginKey then we expect Plugin as a value
						yield return new KeyValuePair<PluginKey, Plugin>(pk, (Plugin) entry.Value);
					}
				}
			}
		}
		#endregion
	}
}