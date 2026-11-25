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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine {
	public interface IDeletable {
		bool IsDeleted { get; }
		void Delete();
	}

	public interface ITagHolder {
		object GetTag(TagKey tk);
		void SetTag(TagKey tk, object value);
		bool HasTag(TagKey tk);
		void RemoveTag(TagKey tk);
		void ClearTags();
	}

	/// <summary>
	/// This is the base class for implementation of our \"lightweight polymorphism\". 
	/// TagHolder class holds tags (values indexed by names) and Timers.
	/// </summary>
	/// <remarks>
	/// All Things (Items and Characters) and GameAccounts are TagHolders, as is Server.globals.
	/// </remarks>
	public class TagHolder : IDeletable, ITagHolder {
		internal Hashtable tags; //in this Hashtable are stored Tags, Timers, and by PluginHolder class also Plugins and TGListNodes

		public TagHolder() {
		}

		#region DeepCopy implementation
		public TagHolder(TagHolder copyFrom) { //copying constuctor
			if (copyFrom.tags != null) {
				foreach (DictionaryEntry entry in copyFrom.tags) {
					TagKey tagK = entry.Key as TagKey;
					if (tagK != null) {
						DeepCopyFactory.GetCopyDelayed(entry.Value, this.DelayedGetCopy_Tag, tagK);
					} else {
						TimerKey timerK = entry.Key as TimerKey;
						if (timerK != null) {
							DeepCopyFactory.GetCopyDelayed(entry.Value, this.DelayedGetCopy_Timer);
						}
					}
				}
			}
		}

		private void DelayedGetCopy_Tag(object copy, object paramTagKey) {
			this.SetTag((TagKey) paramTagKey, copy);
		}

		private void DelayedGetCopy_Timer(object copy) {
			//it should enqueue itself
		}

		#endregion DeepCopy implementation

		public virtual string Name {
			get {
				return "<nameless TagHolder>";
			}
			set {

			}
		}

		#region Timers
		//called by Timer after load, do not use otherwise.
		public BoundTimer AddTimer(TimerKey key, BoundTimer timer) {
			if (this.tags != null) {
				TimerKey prevKey = this.tags[timer] as TimerKey;
				if (prevKey != null && prevKey != key) {
					throw new SEException("You can't assign one Timer to one TagHolder under 2 different TimerKeys");
				}

				BoundTimer prevTimer = this.tags[key] as BoundTimer;
				if (prevTimer != null && prevTimer != timer) {
					this.RemoveTimer(prevTimer);
				}
			} else {
				this.tags = new Hashtable();
			}
			this.tags[key] = timer;
			this.tags[timer] = key;
			timer.contRef.Target = this;
			return timer;
		}

		public BoundTimer RemoveTimer(TimerKey key) {
			if (this.tags != null) {
				BoundTimer timer = this.tags[key] as BoundTimer;
				if (timer != null) {
					this.tags.Remove(key);
					this.tags.Remove(timer);
					timer.contRef.Target = null;
					return timer;
				}
			}
			return null;
		}

		public void RemoveTimer(BoundTimer timer) {
			if (this.tags != null) {
				TimerKey key = this.tags[timer] as TimerKey;
				if (key != null) {
					this.tags.Remove(key);
					this.tags.Remove(timer);
					timer.contRef.Target = null;
				}
			}
		}

		public void DeleteTimer(TimerKey key) {
			BoundTimer timer = this.RemoveTimer(key);
			if (timer != null) {
				timer.Delete();
			}
		}

		public void DeleteTimers() {
			if (this.tags == null) {
				return;
			}
			List<TimerKey> toBeRemoved = new List<TimerKey>();
			foreach (object keyObj in this.tags.Keys) {
				TimerKey key = keyObj as TimerKey;
				if (key != null) {
					toBeRemoved.Add(key);
				}
			}

			foreach (TimerKey key in toBeRemoved) {
				this.RemoveTimer(key).Delete();
			}
			this.ReleaseTagsTableIfEmpty();
		}

		public bool HasTimer(TimerKey key) {
			if (this.tags != null) {
				return (this.tags.ContainsKey(key));
			}
			return false;
		}

		public bool HasTimer(BoundTimer timer) {
			return timer.contRef.Target == this;
		}

		public BoundTimer GetTimer(TimerKey key) {
			if (this.tags != null) {
				return (BoundTimer) this.tags[key];
			}
			return null;
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IEnumerable<KeyValuePair<TimerKey, BoundTimer>> GetAllTimers() {
			if (this.tags != null) {
				foreach (DictionaryEntry entry in this.tags) {
					TimerKey tk = entry.Key as TimerKey;
					if (tk != null) {
						yield return new KeyValuePair<TimerKey, BoundTimer>(tk, (BoundTimer) entry.Value);
					}
				}
			}
		}
		#endregion Timers

		#region Tags
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate"), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public IEnumerable<KeyValuePair<TagKey, object>> GetAllTags() {
			if (this.tags != null) {
				foreach (DictionaryEntry entry in this.tags) {
					TagKey tk = entry.Key as TagKey;
					if (tk != null) {
						yield return new KeyValuePair<TagKey, object>(tk, entry.Value);
					}
				}
			}
		}

		internal void EnsureTagsTable() {
			if (this.tags == null) {
				this.tags = new Hashtable();
			}
		}

		internal void ReleaseTagsTableIfEmpty() {
			if (this.tags != null) {
				if (this.tags.Count == 0) {
					this.tags = null;
				}
			}
		}

		public void SetTag(TagKey tk, object value) {
			this.EnsureTagsTable();
			//Console.WriteLine("TagKey["+tk+"]="+value);
			this.tags[tk] = value;
		}

		public object GetTag(TagKey tk) {
			if (this.tags == null) {
				return null;
			}
			return this.tags[tk];
		}

		public bool HasTag(TagKey tk) {
			if (this.tags == null) {
				return false;
			}
			return (this.tags.ContainsKey(tk));
		}

		public void RemoveTag(TagKey tk) {
			if (this.tags == null) return;
			this.tags.Remove(tk);
		}

		public void ClearTags() {
			if (this.tags == null) {
				return;
			}
			List<TagKey> toBeRemoved = new List<TagKey>();
			foreach (object keyObj in this.tags.Keys) {
				TagKey key = keyObj as TagKey;
				if (key != null) {
					toBeRemoved.Add(key);
				}
			}
			foreach (TagKey key in toBeRemoved) {
				this.RemoveTag(key);
			}
			this.ReleaseTagsTableIfEmpty();
		}

		public string ListTags() {
			int tagcount = 0;
			StringBuilder sb = null;
			if (this.tags != null) {
				sb = new StringBuilder("Tags of the object '").Append(this).Append("' :").Append(Environment.NewLine);
				foreach (DictionaryEntry entry in this.tags) {
					if (entry.Key is TagKey) {
						sb.Append(entry.Key).Append(" = ").Append(entry.Value).Append(Environment.NewLine);
						tagcount++;
					}
				}
				sb.Length -= Environment.NewLine.Length;
			}
			if (tagcount > 0) {
				return sb.ToString();
			}
			return "Object '" + this + "' has no tags";
		}
		#endregion Tags

		#region HELP (todo? remove?)
		private static string ListProperties(Type type) {
			return ListProperties(type, null);
		}

		private static string ListProperties(Type type, string name) {
			PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			StringBuilder propNames = new StringBuilder("(");
			foreach (PropertyInfo propertyInfo in props) {
				if (name == null || StringComparer.OrdinalIgnoreCase.Equals(propertyInfo.Name, name)) {
					if (propNames.Length > 1) {
						propNames.Append(", ");
					}
					propNames.Append(propertyInfo.Name);
				}
			}
			if (propNames.Length == 1) {
				propNames.Append("[Sorry, no properties found])");
			} else {
				propNames.Append(")");
			}
			return propNames.ToString();
		}

		private static string ListMethods(Type type) {
			return ListMethods(type, null);
		}

		private static string ListMethods(Type type, string name) {
			MethodInfo[] meths = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
			StringBuilder methNames = new StringBuilder("(");
			foreach (MethodInfo methodInfo in meths) {
				if (name == null || StringComparer.OrdinalIgnoreCase.Equals(methodInfo.Name, name)) {
					if (methNames.Length > 1) {
						methNames.Append(", ");
					}
					methNames.Append(methodInfo.Name);
				}
			}
			if (methNames.Length == 1) {
				methNames.Append("[Sorry, no methods found])");
			} else {
				methNames.Append(")");
			}
			return methNames.ToString();
		}

		public void Help() {
			Globals.Src.WriteLine("Properties: " + ListProperties(this.GetType()));
			Globals.Src.WriteLine("Methods: " + ListMethods(this.GetType()));
		}
		#endregion HELP

		#region save/load


		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public virtual void Save(SaveStream output) {
			if (this.tags != null) {
				ArrayList forDeleting = null;
				foreach (DictionaryEntry entry in this.tags) {
					object key = entry.Key;
					object value = entry.Value;
					if (key is TagKey) {
						IDeletable deletableValue = value as IDeletable;
						if (deletableValue != null) {
							if (deletableValue.IsDeleted) {
								if (forDeleting == null) {
									forDeleting = new ArrayList();
								}
								forDeleting.Add(entry.Key);
								continue;//we don't save deleted values
							}
						}
						output.WriteValue("tag." + key, value);
					} else if (key is TimerKey) {
						output.WriteValue("%" + key, value);
						//} else {
						//Logger.WriteError(string.Format("This should not happen. Unknown key-value pair: {0} - {1}", key, value));
					}
				}
				if (forDeleting != null) {
					foreach (object key in forDeleting) {
						this.tags.Remove(key);
					}
				}
			}
		}

		//regular expressions for textual loading
		//tag.name
		internal static Regex tagRE = new Regex(@"tag\.(?<name>\w+)\s*",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		internal static Regex timerKeyRE = new Regex(@"^\%(?<name>.+)\s*$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "3#")]
		public virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			Match m = tagRE.Match(valueName);
			if (m.Success) {	//If the name begins with 'tag.'
				string tagName = m.Groups["name"].Value;
				TagKey tk = TagKey.Acquire(tagName);
				ObjectSaver.Load(valueString, this.DelayedLoad_Tag, filename, line, tk);
				return;
			}
			m = timerKeyRE.Match(valueName);
			if (m.Success) {	//If the name begins with '%'
				string timerName = m.Groups["name"].Value;
				TimerKey tk = TimerKey.Acquire(timerName);
				ObjectSaver.Load(valueString, this.DelayedLoad_Timer, filename, line, tk);
				return;
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		//used by loaders (Thing, GameAccount...)
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void LoadSectionLines(PropsSection ps) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					this.LoadLine(ps.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		private void DelayedLoad_Tag(object resolvedObject, string filename, int line, object tagKey) {
			//throw new Exception("LoadTag_Delayed");
			this.SetTag((TagKey) tagKey, resolvedObject);
		}

		private void DelayedLoad_Timer(object resolvedObject, string filename, int line, object timerKey) {
			//throw new Exception("LoadTag_Delayed");
			this.AddTimer((TimerKey) timerKey, (BoundTimer) resolvedObject);
		}

		#endregion save/load

		#region IDeletable

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void ThrowIfDeleted() {
			if (this.IsDeleted) {
				throw new DeletedException("You can not manipulate a deleted object (" + this + ")");
			}
			if (this.IsLimbo && RunLevelManager.IsRunning) { //when loading, it's ok for stuff to be in limbo
				try {
					this.Delete();
				} catch { }
				throw new SEException("This object is in Limbo state (" + this + "). This should not happen.");
			}
		}

		internal virtual bool IsLimbo {
			get {
				return false;
			}
		}

		public virtual bool IsDeleted { get { return false; } }

		public virtual void Delete() {
			this.DeleteTimers();
		}
		#endregion IDeletable
	}
}