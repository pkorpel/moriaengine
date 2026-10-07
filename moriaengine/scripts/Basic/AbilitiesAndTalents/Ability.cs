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
	/// This class holds information about one ability the user has - the number of ability points 
	/// and any additional info (such as timers connected with the ability running etc.)
	/// </summary>
	[ViewableClass]
	public sealed class Ability {
		private byte realPoints;
		private sbyte modification;
		private Character cont;
		private AbilityDef def;
		private TimeSpan lastUsage;

		internal Ability(AbilityDef def, Character cont) {
			this.modification = 0;
			this.realPoints = 0;
			this.cont = cont;
			this.def = def;
		}

		//copying constructor
		internal Ability(Ability copyFrom, Character cont) {
			this.realPoints = copyFrom.realPoints;
			this.modification = copyFrom.modification;
			this.def = copyFrom.def;
			this.cont = cont;
		}

		/// <summary>Character's ability points. This is the real value, i.e. unmodified by temporary effect, equipped magic items, etc.</summary>
		public int RealPoints {
			get {
				return this.realPoints;
			}
			set {
				int newValue = Math.Max(0, value);
				if (newValue != this.realPoints) {
					int oldModified = this.ModifiedPoints;
					this.realPoints = (byte) newValue;
					if (oldModified != this.ModifiedPoints) {
						this.def.Trigger_ValueChanged(this.cont, this, oldModified); //call changetrigger with information about previous value
					}
				}
				this.DisposeIfEmpty();
			}
		}

		/// <summary>
		/// Character's ability points. This is the modified value, which can be different from RealPoints when some temporary effects take place.
		/// When character dies, this value should become equal to RealPoints.
		/// </summary>
		/// <remarks>
		/// This will never return a negative value, even if RealPoints+modification is negative. That's why the modification must be changed in a separate method.
		/// </remarks>
		public int ModifiedPoints {
			get {
				return Math.Max(0, this.realPoints + this.modification);
			}
		}

		/// <summary>Change the value of ModifiedPoints</summary>
		public void ModifyPoints(int difference) {
			if (difference != 0) {
				int oldValue = this.ModifiedPoints;
				this.modification += (sbyte) difference;
				int newValue = this.ModifiedPoints;
				if (oldValue != newValue) { //modified value may not have changed if we're still in negative numbers
					this.def.Trigger_ValueChanged(this.cont, this, oldValue); //call changetrigger with information about previous value
				}
			}
			this.DisposeIfEmpty();
		}

		//not to be used widely, it's just for possible quick reference for GMs etc.
		public int MaxPoints {
			get {
				int maxPoints = 0;
				Player player = this.Cont as Player;
				if (player != null) {
					ProfessionDef prof = player.Profession;
					if (prof != null) {
						maxPoints = prof.GetAbilityMaximumPoints(this.def);
						TalentTreeBranchDef branch = prof.TTB1;
						if (branch != null) {
							maxPoints = Math.Max(maxPoints, branch.GetTalentMaxPoints(this.def));
						}
						branch = prof.TTB2;
						if (branch != null) {
							maxPoints = Math.Max(maxPoints, branch.GetTalentMaxPoints(this.def));
						}
						branch = prof.TTB3;
						if (branch != null) {
							maxPoints = Math.Max(maxPoints, branch.GetTalentMaxPoints(this.def));
						}
					}
				}
				return maxPoints;
			}
		}

		/// <summary>If this is an activable ability, is it running?</summary>
		public bool Running {
			get {
				ActivableAbilityDef activableAbility = this.def as ActivableAbilityDef;
				if (activableAbility != null) {
					return activableAbility.IsActive(this.cont);
				}
				return false;
			}
		}

		public string Name {
			get {
				return this.def.Name;
			}
		}

		/// <summary>Character who possesses this ability</summary>
		public Character Cont {
			get {
				return this.cont;
			}
		}

		public AbilityDef Def {
			get {
				return this.def;
			}
		}

		/// <summary>Server time of the last usage</summary>
		public TimeSpan LastUsage {
			get {
				return this.lastUsage;
			}
			internal set {
				this.lastUsage = value;
			}
		}

		private void DisposeIfEmpty() {
			if ((this.realPoints == 0) && (this.modification == 0)) { //removed last point(s)						
				this.cont.InternalRemoveAbility(this.def);
			}
		}

		#region Load / Save
		internal string GetSaveString()
		{
			if (this.modification == 0) {
				return this.realPoints.ToString();
			}
			return string.Concat(this.RealPoints.ToString(), ", ", this.modification.ToString());
		}

		internal bool LoadSavedString(string p) {
			string[] split = Utility.SplitSphereString(p, true);
			if (!ConvertTools.TryParseByte(split[0], out this.realPoints)) {
				return false;
			}
			int len = split.Length;
			if (len > 1) {
				if (!ConvertTools.TryParseSByte(split[1], out this.modification)) {
					return false;
				}
			} else {
				this.modification = 0;
			}
			this.DisposeIfEmpty();
			return true;
		}
		#endregion Load / Save
	}
}