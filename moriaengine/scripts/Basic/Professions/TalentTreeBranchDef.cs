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
	/// <summary>Def listing fields necessary for all professions. Actual profession-elated active code is in ProfessionPlugin class</summary>
	[ViewableClass]
	public class TalentTreeBranchDef : AbstractIndexedDef<TalentTreeBranchDef, string> {
		#region Accessors
		public new static TalentTreeBranchDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as TalentTreeBranchDef;
		}

		public static TalentTreeBranchDef GetByName(string key) {
			return GetByDefIndex(key);
		}

		private Dictionary<AbilityDef, TalentTreeEntry> cachedLeafs = new Dictionary<AbilityDef, TalentTreeEntry>();
		private bool cacheComplete;

		public string Name {
			get {
				return this.DefIndex;
			}
		}

		public int GetTalentMaxPoints(AbilityDef def) {
			TalentTreeEntry leaf = this.GetEntry(def);
			if (leaf != null) {
				return leaf.maxPoints;
			}
			return 0;
		}

		public void SetTalentMaxPoints(AbilityDef def, int points) {
			string fvName = talentMaxPointsPrefix + def.PrettyDefname;
			if (!this.HasFieldValue(fvName)) {
				this.InitTypedField(fvName, 0, typeof(int)).CurrentValue = points;
			} else {
				this.SetCurrentFieldValue(fvName, points);
			}
			this.ClearCache();
		}

		public ResourcesList GetTalentDependency(AbilityDef def) {
			TalentTreeEntry leaf = this.GetEntry(def);
			if (leaf != null) {
				return leaf.dependencies;
			}
			return null;
		}

		public void SetTalentDependency(AbilityDef def, ResourcesList dependency) {
			string fvName = talentMaxPointsPrefix + def.PrettyDefname;
			if (!this.HasFieldValue(fvName)) {
				this.InitTypedField(fvName, 0, typeof(ResourcesList)).CurrentValue = dependency;
			} else {
				this.SetCurrentFieldValue(fvName, dependency);
			}
			this.ClearCache();
		}

		public TalentTreeEntry GetEntry(AbilityDef def) {
			TalentTreeEntry leaf;
			if (!this.cachedLeafs.TryGetValue(def, out leaf)) {
				string defname = def.PrettyDefname;
				string fvName = talentTierPrefix + defname;
				if (this.HasFieldValue(fvName)) {
					leaf = new TalentTreeEntry(def,
						Convert.ToInt32(this.GetCurrentFieldValue(fvName)),
						Convert.ToInt32(this.GetCurrentFieldValue(talentTierPositionPrefix + defname)),
						Convert.ToInt32(this.GetCurrentFieldValue(talentMaxPointsPrefix + defname)),
						(ResourcesList) this.GetCurrentFieldValue(talentDependencyPrefix + defname));
					this.cachedLeafs[def] = leaf;
					this.cacheComplete = false;
				}
			}
			return leaf;
		}

		public IEnumerable<TalentTreeEntry> AllTalents {
			get {
				if (!this.cacheComplete) {
					foreach (AbilityDef abilityDef in AbilityDef.AllAbilities) {
						this.GetEntry(abilityDef);
					}
					this.cacheComplete = true;
				}
				return this.cachedLeafs.Values;
			}
		}

		public override string ToString() {
			return string.Concat("[", this.Name, ": ", Tools.TypeToString(this.GetType()), "]");
		}
		#endregion Accessors

		#region Load from scripts
		public TalentTreeBranchDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public override void LoadScriptLines(PropsSection ps) {
			PropsLine p = ps.PopPropsLine("name");
			this.DefIndex = ConvertTools.LoadSimpleQuotedString(p.Value);

			base.LoadScriptLines(ps);

			if (ps.TriggerCount > 0) {
				Logger.WriteWarning("Triggers in a TalentTreeBranchDef are ignored.");
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			//try recognizing an ability name or defname
			AbilityDef ability = AbilityDef.GetByDefname(param);
			if (ability == null) {
				ability = AbilityDef.GetByName(param);
			}

			//now load the parameters. it goes Tier, TierPosition, MaxPoints, resourcelist
			if (ability != null) {
				string[] preparsed = Utility.SplitSphereString(args, false);
				int len = preparsed.Length;
				if (len < 3) {
					throw new SEException("TalentTree entries need at least 3 numbers - Tier, TierPosition, MaxPoints");
				}

				string abilityName = ability.PrettyDefname;

				this.InitOrSetFieldValue<int>(filename, line, talentTierPrefix + abilityName, preparsed[0]);
				this.InitOrSetFieldValue<int>(filename, line, talentTierPositionPrefix + abilityName, preparsed[1]);
				this.InitOrSetFieldValue<int>(filename, line, talentMaxPointsPrefix + abilityName, preparsed[2]);

				string resListFieldName = talentDependencyPrefix + abilityName;
				if (len > 3) {
					string reconstructedResList = string.Join(", ", preparsed, 3, len - 3);
					this.InitOrSetFieldValue<ResourcesList>(filename, line, resListFieldName, reconstructedResList);
				} else if (!this.HasFieldValue(resListFieldName)) {
					this.InitTypedField(resListFieldName, null, typeof(ResourcesList));
				}

			} else {
				base.LoadScriptLine(filename, line, param, args);
			}
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
			this.cachedLeafs.Clear();
			this.cacheComplete = false;
		}
		#endregion Load from scripts

		#region Load from saves
		private const string talentTierPrefix = "TalentTier.";
		private const string talentTierPositionPrefix = "TalentTierPosition.";
		private const string talentMaxPointsPrefix = "TalentMaxPoints.";
		private const string talentDependencyPrefix = "TalentDependency.";

		public override void LoadFromSaves(PropsSection input) {
			foreach (PropsLine line in input.PropsLines) {
				string name = line.Name;
				if (!this.HasFieldValue(name)) {
					if (name.StartsWith(talentTierPrefix) ||
							name.StartsWith(talentTierPositionPrefix) ||
							name.StartsWith(talentMaxPointsPrefix)) {
						this.InitTypedField(name, 0, typeof(int));
					} else if (name.StartsWith(talentDependencyPrefix)) {
						this.InitTypedField(name, null, typeof(ResourcesList));
					}
				}
			}

			base.LoadFromSaves(input);
		}
		#endregion Load from saves
	}

	//represents the settings of 1 talent in 1 talenttree branch
	public class TalentTreeEntry {
		public readonly AbilityDef talent;
		public readonly int tier;
		public readonly int tierPosition;
		public readonly int maxPoints;
		public readonly ResourcesList dependencies;

		public TalentTreeEntry(AbilityDef talent, int tier, int tierPosition, int maxPoints, ResourcesList dependencies) {
			this.talent = talent;
			this.tier = tier;
			this.tierPosition = tierPosition;
			this.maxPoints = maxPoints;
			this.dependencies = dependencies;
		}
	}
}