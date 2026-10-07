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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>Def listing fields necessary for all professions. Actual profession-related active code is in ProfessionPlugin class</summary>
	[ViewableClass]
	public class ProfessionDef : AbstractIndexedDef<ProfessionDef, string> {
		#region Accessors
		public new static ProfessionDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as ProfessionDef;
		}

		public static ProfessionDef GetByName(string key) {
			return GetByDefIndex(key);
		}

		private FieldValue professionPluginDef;
		private FieldValue allowedSpells;

		private FieldValue ttb1; //ttb = TalentTreeBranch
		private FieldValue ttb2;
		private FieldValue ttb3;

		private HashSet<SpellDef> cachedSpells;

		private ProfessionAbilityEntry[] sortedAbilityCache;
		private Dictionary<AbilityDef, ProfessionAbilityEntry> abilityCache = new Dictionary<AbilityDef, ProfessionAbilityEntry>();

		private SortedDictionary<int, ProfessionSkillEntry> skillsCache = new SortedDictionary<int, ProfessionSkillEntry>();
		private bool skillsCacheComplete;

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		public PluginDef ProfessionPluginDef {
			get {
				return (PluginDef) this.professionPluginDef.CurrentValue;
			}
			set {
				this.professionPluginDef.CurrentValue = value;
			}
		}

		private HashSet<SpellDef> GetCachedSpells() {
			if (this.cachedSpells == null) {
				HashSet<SpellDef> hs = new HashSet<SpellDef>();
				foreach (SpellDef def in (SpellDef[]) this.allowedSpells.CurrentValue) {
					hs.Add(def);
				}
				this.cachedSpells = hs;
			}
			return this.cachedSpells;
		}

		public ICollection<SpellDef> AllowedSpells {
			get {
				return this.GetCachedSpells();
			}
		}

		public bool CanCastSpell(SpellDef spell) {
			return this.GetCachedSpells().Contains(spell);
		}

		public TalentTreeBranchDef TTB1 {
			get {
				return (TalentTreeBranchDef) this.ttb1.CurrentValue;
			}
			set {
				this.ttb1.CurrentValue = value;
			}
		}

		public TalentTreeBranchDef TTB2 {
			get {
				return (TalentTreeBranchDef) this.ttb2.CurrentValue;
			}
			set {
				this.ttb2.CurrentValue = value;
			}
		}

		public TalentTreeBranchDef TTB3 {
			get {
				return (TalentTreeBranchDef) this.ttb3.CurrentValue;
			}
			set {
				this.ttb3.CurrentValue = value;
			}
		}

		#region Skills
		/// <summary>Return the maximal value of the given skill for this profession</summary>
		public int GetSkillCap(SkillDef skillDef) {
			return this.GetSkillCap(skillDef.Id);
		}

		/// <summary>Return the maximal value of the given skill for this profession</summary>
		public int GetSkillCap(SkillName skillName) {
			return this.GetSkillCap((int) skillName);
		}

		/// <summary>Return the maximal value of the given skill for this profession</summary>
		public int GetSkillCap(int skillId) {
			return this.GetSkillEntry(skillId).cap;
		}

		/// <summary>Return the value of the given skill at which this profession starts</summary>
		public int GetSkillMinimum(SkillDef skillDef) {
			return this.GetSkillMinimum(skillDef.Id);
		}

		/// <summary>Return the value of the given skill at which this profession starts</summary>
		public int GetSkillMinimum(SkillName skillName) {
			return this.GetSkillMinimum((int) skillName);
		}

		/// <summary>Return the value of the given skill at which this profession starts</summary>
		public int GetSkillMinimum(int skillId) {
			return this.GetSkillEntry(skillId).minimum;
		}

		public ProfessionSkillEntry GetSkillEntry(int skillId) {
			ProfessionSkillEntry retVal;
			if (!this.skillsCache.TryGetValue(skillId, out retVal)) {
				string name = AbstractSkillDef.GetById(skillId).PrettyDefname;
				retVal = new ProfessionSkillEntry(
					Convert.ToInt32(this.GetCurrentFieldValue(skillMinimumPrefix + name)),
					Convert.ToInt32(this.GetCurrentFieldValue(skillCapPrefix + name)));
				this.skillsCache.Add(skillId, retVal);
				this.skillsCacheComplete = false;
			}
			return retVal;
		}

		public IEnumerable<ProfessionSkillEntry> AllSkillsSorted {
			get {
				if (!this.skillsCacheComplete) {
					foreach (SkillDef skill in SkillDef.AllSkillDefs) {
						this.GetSkillEntry(skill.Id);
					}
				}
				return this.skillsCache.Values;
			}
		}
		#endregion Skills

		#region Abilities
		/// <summary>Return the maximal value of the given ability for this profession</summary>
		public int GetAbilityMaximumPoints(AbilityDef abilityDef) {
			ProfessionAbilityEntry entry = this.GetAbilityEntry(abilityDef);
			if (entry != null) {
				return entry.maxPoints;
			}
			return 0;
		}

		public ProfessionAbilityEntry GetAbilityEntry(AbilityDef abilityDef) {
			ProfessionAbilityEntry retVal;
			if (!this.abilityCache.TryGetValue(abilityDef, out retVal)) {
				string name = abilityDef.PrettyDefname;
				string prefixed = abilityOrderPrefix + name;
				if (this.HasFieldValue(prefixed)) {
					retVal = new ProfessionAbilityEntry(abilityDef,
						Convert.ToInt32(this.GetCurrentFieldValue(prefixed)),
						Convert.ToInt32(this.GetCurrentFieldValue(abilityMaxPointsPrefix + name)));
					this.abilityCache.Add(abilityDef, retVal);
					this.sortedAbilityCache = null;
				}
			}
			return retVal;
		}

		public IEnumerable<ProfessionAbilityEntry> AllAbilitiesSorted {
			get {
				if (this.sortedAbilityCache == null) {
					foreach (AbilityDef def in AbilityDef.AllAbilities) {
						this.GetAbilityEntry(def);
					}
					List<ProfessionAbilityEntry> list = new List<ProfessionAbilityEntry>(this.abilityCache.Values);
					list.Sort(delegate(ProfessionAbilityEntry a, ProfessionAbilityEntry b) {
						return Comparer<int>.Default.Compare(
							a.order, b.order);
					});
					this.sortedAbilityCache = list.ToArray();
				}
				return this.sortedAbilityCache;
			}
		}
		#endregion Abilities

		public override string ToString() {
			return string.Concat("[", this.Name, " ", Tools.TypeToString(this.GetType()), "]");
		}
		#endregion Accessors

		#region Load from scripts
		public ProfessionDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.professionPluginDef = this.InitTypedField("professionPluginDef", null, typeof(PluginDef));
			this.allowedSpells = this.InitTypedField("allowedSpells", new SpellDef[0], typeof(SpellDef[]));

			this.ttb1 = this.InitTypedField("ttb1", null, typeof(TalentTreeBranchDef));
			this.ttb2 = this.InitTypedField("ttb2", null, typeof(TalentTreeBranchDef));
			this.ttb3 = this.InitTypedField("ttb3", null, typeof(TalentTreeBranchDef));
		}

		public override void LoadScriptLines(PropsSection ps) {
			this.ClearCache();

			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				Logger.WriteWarning("Triggers in a ProfessionDef aren't valid. Use the relevant PluginDef for that functionality");
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			//try recognizing an ability name or defname. The parameters mean order of displaying and max points in that ability for this profession
			AbilityDef ability = AbilityDef.GetByDefname(param);
			if (ability == null) {
				ability = AbilityDef.GetByName(param);
			}
			if (ability != null) {
				string[] preparsed = Utility.SplitSphereString(args, true);
				if (preparsed.Length < 2) {
					throw new SEException("ProfessionDef ability entries need 2 numbers - order and maximum points");
				}

				string abilityName = ability.PrettyDefname;
				this.InitOrSetFieldValue<int>(filename, line, abilityOrderPrefix + abilityName, preparsed[0]);
				this.InitOrSetFieldValue<int>(filename, line, abilityMaxPointsPrefix + abilityName, preparsed[1]);
				return;
			}

			//try recognizing a skill name or defname. The parameters then mean starting and max (cap) points for this profession
			AbstractSkillDef skillDef = AbstractSkillDef.GetByDefname(param);
			if (skillDef == null) {
				skillDef = AbstractSkillDef.GetByKey(param);
			}
			if (skillDef != null) {
				string[] preparsed = Utility.SplitSphereString(args, true);
				if (preparsed.Length < 2) {
					throw new SEException("ProfessionDef skill entries need 2 numbers - minimum and cap");
				}

				string skillName = skillDef.PrettyDefname;
				this.InitOrSetFieldValue<int>(filename, line, skillMinimumPrefix + skillName, preparsed[0]);
				this.InitOrSetFieldValue<int>(filename, line, skillCapPrefix + skillName, preparsed[1]);
				return;
			}


			base.LoadScriptLine(filename, line, param, args);
		}

		private void InitOrSetFieldValue<T>(string filename, int line, string fvName, string fvValue) {
			if (!this.HasFieldValue(fvName)) {
				this.InitTypedField(fvName, default(T), typeof(T))
					.SetFromScripts(filename, line, fvValue);
			} else {
				base.LoadScriptLine(filename, line, fvName, fvValue);
			}
		}

		public override void Unload() {
			this.ClearCache();
			base.Unload();
		}

		public override void UnUnload() {
			this.ClearCache();
			base.UnUnload();
		}

		protected override void Unregister() {
			this.ClearCache();
			base.Unregister();
		}

		public override AbstractScript Register() {
			this.ClearCache();
			return base.Register();
		}

		public void ClearCache() {
			this.cachedSpells = null;

			this.abilityCache.Clear();
			this.sortedAbilityCache = null;

			this.skillsCache.Clear();
			this.skillsCacheComplete = false;
		}

		#endregion Load from scripts

		#region Load from saves
		private const string abilityOrderPrefix = "AbilityOrder.";
		private const string abilityMaxPointsPrefix = "AbilityMaxPoints.";
		private const string skillMinimumPrefix = "SkillMinimum.";
		private const string skillCapPrefix = "SkillCap.";

		public override void LoadFromSaves(PropsSection input) {
			foreach (PropsLine line in input.PropsLines) {
				string name = line.Name;
				if (!this.HasFieldValue(name)) {
					if (name.StartsWith(abilityMaxPointsPrefix) ||
							name.StartsWith(abilityOrderPrefix) ||
							name.StartsWith(skillMinimumPrefix) ||
							name.StartsWith(skillCapPrefix)) {
						this.InitTypedField(name, 0, typeof(int));
					}
				}
			}

			base.LoadFromSaves(input);
		}

		#endregion Load from saves

		#region Static utility methods

		public static ProfessionDef GetProfessionOfChar(Player player) {
			ProfessionPlugin plugin = ProfessionPlugin.GetInstalledPlugin(player);
			if (plugin != null) {
				return plugin.ProfessionDef;
			}
			return null;
		}

		public static void SetProfessionOfChar(Player player, ProfessionDef value) {
			ProfessionPlugin.InstallProfessionPlugin(player, value);
		}
		#endregion Static utility methods
	}


	public class ProfessionSkillEntry {
		public readonly SkillDef skillDef;
		public readonly int minimum;
		public readonly int cap;

		public ProfessionSkillEntry(int minimum, int cap) {
			this.minimum = minimum;
			this.cap = cap;
		}
	}

	public class ProfessionAbilityEntry {
		public readonly AbilityDef abilityDef;
		public readonly int order;
		public readonly int maxPoints;

		public ProfessionAbilityEntry(AbilityDef def, int order, int maxPoints) {
			this.abilityDef = def;
			this.order = order;
			this.maxPoints = maxPoints;
		}
	}
}