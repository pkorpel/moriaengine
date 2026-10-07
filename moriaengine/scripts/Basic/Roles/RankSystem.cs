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
using System.Collections.ObjectModel;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[SaveableClass]
	public class RankSystem : Role {

		private List<string> rankNames;
		private ReadOnlyCollection<string> rankNamesReadonly;

		internal RankSystem(RankSystemDef def, RoleKey key)
			: base(def, key) {

			this.rankNames = new List<string>();
			this.rankNamesReadonly = new ReadOnlyCollection<string>(this.rankNames);
		}

		public void InsertRankName(string name, int index) {
			this.rankNames.Insert(index, name);
		}

		public void AddRankName(string name) {
			this.rankNames.Add(name);
		}

		public void SetRankName(string name, int index) {
			this.rankNames[index] = name;
		}

		public void RemoveRankName(int index) {
			this.rankNames.RemoveAt(index);
		}

		public ReadOnlyCollection<string> RankNames {
			get {
				return this.rankNamesReadonly;
			}
		}

		[SaveableClass]
		public class RankMembership : RoleMembership {
			private int rankIndex;

			internal RankMembership(Character member, RankSystem cont)
				: base(member, cont) {
			}

			public int RankIndex {
				get {
					return this.rankIndex;
				}
				set {
					if (value >= 0 && value < ((RankSystem) this.Cont).rankNames.Count) {
						this.rankIndex = value;
					} else {
						throw new SEException("There's no rank indexed " + value);
					}
				}
			}

			public string Name {
				get {
					return ((RankSystem) this.Cont).rankNames[this.rankIndex];
				}
			}

			#region persistence
			[LoadSection]
			public RankMembership(PropsSection input)
				: base(input) {
			}

			protected override void LoadLine(string filename, int line, string valueName, string valueString) {
				switch (valueName) {
					case "rankindex":
						this.rankIndex = ConvertTools.ParseInt32(valueString);
						return;
				}
				base.LoadLine(filename, line, valueName, valueString);
			}

			[Save]
			public override void Save(SaveStream output) {
				base.Save(output);
				output.WriteValue("rankIndex", this.rankIndex);
			}
			#endregion persistence
		}

		protected override IRoleMembership CreateMembershipObject(Character member) {
			return new RankMembership(member, this);
		}

		#region persistence
		[Save]
		public override void Save(SaveStream output) {
			base.Save(output);
			output.WriteValue("rankNames", this.rankNames);
		}

		protected override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "rankNames":
					ObjectSaver.Load(valueString, this.Load_RankNames, filename, line);
					return;
			}
			base.LoadLine(filename, line, valueName, valueString);
		}

		private void Load_RankNames(object resolvedObject, string filename, int line) {
			this.rankNames = (List<string>) resolvedObject;
			this.rankNamesReadonly = new ReadOnlyCollection<string>(this.rankNames);
		}
		#endregion persistence
	}

	public class RankSystemDef : RoleDef {
		public RankSystemDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new RankSystem(this, key);
		}
	}
}
