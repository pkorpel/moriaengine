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
using System.Collections.Concurrent;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class AbstractSkillDef : AbstractIndexedDef<AbstractSkillDef, int> /*TriggerGroupHolder*/ {

		//string(defname)-Skilldef pairs
		private static ConcurrentDictionary<string, AbstractSkillDef> byKey = new ConcurrentDictionary<string, AbstractSkillDef>(StringComparer.OrdinalIgnoreCase);
		//string(key)-Skilldef pairs
		//private static List<AbstractSkillDef> byId = new List<AbstractSkillDef>();
		//Skilldef instances by their ID


		private FieldValue key;
		//private int id;
		private FieldValue startByMacroEnabled;

		private TriggerGroup scriptedTriggers;

		#region Accessors
		public new static AbstractSkillDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbstractSkillDef;
		}

		public static AbstractSkillDef GetByKey(string key) {
			AbstractSkillDef retVal;
			byKey.TryGetValue(key, out retVal);
			return retVal;
		}

		public static AbstractSkillDef GetById(int id) {
			return GetByDefIndex(id);
		}

		public static int SkillsCount {
			get {
				return IndexedCount;
			}
		}

		public int Id {
			get {
				return this.DefIndex;
			}
		}

		public string Key {
			get {
				return (string) this.key.CurrentValue;
			}
			set {
				AbstractSkillDef previous;
				if (byKey.TryGetValue(value, out previous)) {
					if (previous != this) {
						throw new ScriptException("There is already a SkillDef with the key '" + value + "'");
					}
				}
				this.Unregister();
				this.key.CurrentValue = value;
				this.Register();
			}
		}

		public bool StartByMacroEnabled {
			get {
				return (bool) this.startByMacroEnabled.CurrentValue;
			}
			set {
				this.startByMacroEnabled.CurrentValue = value;
			}
		}

		public TriggerGroup TG {
			get {
				return this.scriptedTriggers;
			}
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + this.Key;
		}
		#endregion Accessors

		#region Load from scripts

		public override AbstractScript Register() {
			try {
				string key = this.Key;
				var previous = byKey.GetOrAdd(key, this);
				if (previous != this) {
					throw new SEException("previous != this when registering AbstractSkillDef '" + key + "'");
				}

			} finally {
				base.Register();
			}
			return this;
		}

		protected override void Unregister() {
			try {
				string key = this.Key;

				AbstractSkillDef previous;
				if (byKey.TryRemove(key, out previous)) {
					if (previous != this) {
						if (!byKey.TryAdd(key, previous)) {
							throw new FatalException("Parallel loading fucked up.");
						}
						throw new SEException("previous != this when unregistering AbstractSkillDef '" + key + "'");
					}
				}

			} finally {
				base.Unregister();
			}
		}

		public new static void Bootstrap() {
			//SkillDef script sections are special in that they have numeric header indicating skill id
			RegisterDefnameParser<AbstractSkillDef>(ParseDefnames);
		}

		private static void ParseDefnames(PropsSection section, out string defname, out string altdefname) {
			ushort skillId;
			if (!ConvertTools.TryParseUInt16(section.HeaderName, out skillId)) {
				throw new ScriptException("Unrecognized format of the id number in the skilldef script header.");
			}
			defname = "skill_" + skillId.ToString(CultureInfo.InvariantCulture);

			PropsLine defnameLine = section.TryPopPropsLine("defname");
			if (defnameLine != null) {
				altdefname = ConvertTools.LoadSimpleQuotedString(defnameLine.Value);

				if (string.Equals(defname, altdefname, StringComparison.OrdinalIgnoreCase)) {
					Logger.WriteWarning("Defname redundantly specified for " + section.HeaderType + " " + LogStr.Ident(defname) + ".");
					altdefname = null;
				}
			} else {
				altdefname = null;
			}
		}

		internal new static void ForgetAll() {
			AbstractScript.ForgetAll(); //just to be sure

			Sanity.IfTrueThrow(byKey.Count > 0, "byKey.Count > 0 after AbstractScript.ForgetAll");

			//byId.Clear();
			//skillDefCtorsByName.Clear();
		}

		protected AbstractSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.key = this.InitTypedField("key", "", typeof(string));
			this.startByMacroEnabled = this.InitTypedField("startByMacroEnabled", false, typeof(bool));
		}

		public override void LoadScriptLines(PropsSection ps) {
			base.LoadScriptLines(ps);

			this.DefIndex = ConvertTools.ParseUInt16(this.Defname.Substring(6));
			//"skill_" = 6 chars

			//now do load the trigger code. 
			if (ps.TriggerCount > 0) {
				ps.HeaderName = "t__" + this.Defname + "__";
				this.scriptedTriggers = ScriptedTriggerGroup.Load(ps);
			}
		}

		public override void Unload() {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.Unload();
			}
			base.Unload();
		}

		internal static void StartingLoading() {

		}

		internal new static void LoadingFinished() {

		}
		#endregion Load from scripts

		#region trigger methods
		public TriggerResult TryCancellableTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(self, td, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		public void TryTrigger(AbstractCharacter self, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(self, td, sa);
			}
		}
		#endregion trigger methods
	}

	/// <summary>Instances of this class store the skill values of each character</summary>
	public interface ISkill {
		int RealValue { get; set; }
		int ModifiedValue { get; }
		int Cap { get; }
		SkillLockType Lock { get; set; }
		int Id { get; }
	}
}
