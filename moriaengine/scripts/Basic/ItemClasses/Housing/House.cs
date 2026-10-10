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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Housing (enhancements #19) - previously entirely missing beyond a dialog typedef with no
	/// backing implementation. Built on top of MultiItem/MultiRegion, which already existed and
	/// already works (ships use the same base) - this adds ownership and the security-list data
	/// model on top. Ownership/lists are tag-backed (same idiom used throughout this codebase for
	/// per-instance data, e.g. TrackingSkillDef's List&lt;Character&gt; tag), not [SaveableData]
	/// fields - Item/Thing-family classes serialize via their own hand-rolled Save/LoadLine chain
	/// rather than generic attribute-reflection, so a raw [SaveableData] field here would silently
	/// never be written.
	///
	/// Deliberately out of scope for this pass: a friend/co-owner/ban LIST EDITING UI (the data
	/// model below is ready for one), decay (idle-time destruction), and interior
	/// customization/redecoration. The only interaction implemented is examining a house and the
	/// owner releasing it - see HouseDeed.cs's HouseCommands.
	/// </summary>
	[ViewableClass]
	public class HouseDef : MultiItemDef {
		public HouseDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Thing CreateImpl() {
			return new House(this);
		}
	}

	[ViewableClass]
	public class House : MultiItem {
		public House(MultiItemDef myDef)
			: base(myDef) {
		}

		private static readonly TagKey ownerTK = TagKey.Acquire("_houseOwner_");
		private static readonly TagKey coOwnersTK = TagKey.Acquire("_houseCoOwners_");
		private static readonly TagKey friendsTK = TagKey.Acquire("_houseFriends_");
		private static readonly TagKey bannedTK = TagKey.Acquire("_houseBanned_");

		public Character Owner {
			get {
				return this.GetTag(ownerTK) as Character;
			}
			set {
				this.SetTag(ownerTK, value);
			}
		}

		public IEnumerable<Character> CoOwners {
			get {
				return (List<Character>) this.GetTag(coOwnersTK) ?? (IEnumerable<Character>) System.Array.Empty<Character>();
			}
		}

		public IEnumerable<Character> Friends {
			get {
				return (List<Character>) this.GetTag(friendsTK) ?? (IEnumerable<Character>) System.Array.Empty<Character>();
			}
		}

		public IEnumerable<Character> Banned {
			get {
				return (List<Character>) this.GetTag(bannedTK) ?? (IEnumerable<Character>) System.Array.Empty<Character>();
			}
		}

		/// <summary>Owner, co-owners, and friends can all enter/use the house; the ban list explicitly cannot regardless of the above.</summary>
		public bool HasAccess(Character ch) {
			if (ch == null) {
				return false;
			}
			if (ch.IsGM) {
				return true;
			}
			List<Character> banList = (List<Character>) this.GetTag(bannedTK);
			if ((banList != null) && banList.Contains(ch)) {
				return false;
			}
			if (ch == this.Owner) {
				return true;
			}
			List<Character> coOwnerList = (List<Character>) this.GetTag(coOwnersTK);
			if ((coOwnerList != null) && coOwnerList.Contains(ch)) {
				return true;
			}
			List<Character> friendList = (List<Character>) this.GetTag(friendsTK);
			if ((friendList != null) && friendList.Contains(ch)) {
				return true;
			}
			return false;
		}

		public void AddCoOwner(Character ch) {
			this.SetTag(coOwnersTK, AddUnique((List<Character>) this.GetTag(coOwnersTK), ch));
		}

		public void AddFriend(Character ch) {
			this.SetTag(friendsTK, AddUnique((List<Character>) this.GetTag(friendsTK), ch));
		}

		public void AddBanned(Character ch) {
			this.SetTag(bannedTK, AddUnique((List<Character>) this.GetTag(bannedTK), ch));
		}

		private static List<Character> AddUnique(List<Character> list, Character ch) {
			if (list == null) {
				list = new List<Character>();
			}
			if (!list.Contains(ch)) {
				list.Add(ch);
			}
			return list;
		}

		public override void On_DClick(AbstractCharacter clicker) {
			Character self = clicker as Character;
			if (self != null) {
				ShowInfoTo(this, self);
			}
			base.On_DClick(clicker);
		}

		internal static void ShowInfoTo(House house, Character self) {
			if (house.Owner == null) {
				self.SysMessage("This house is unowned.");
			} else {
				self.SysMessage("This is " + house.Owner.Name + "'s house.");
				if (house.Owner == self) {
					self.SysMessage("Use .releasehouse while standing inside to release it.");
				}
			}
		}
	}

	/// <summary>The house sign component (converted content: i_sign_brass, TYPE = t_housemenu) - previously had no backing trigger group at all (just an empty [typedef] in moriatypes.scp). Shows the same info House.On_DClick does; the sign is what real UO players actually click for this, the house body is a bonus alias.</summary>
	public class t_housemenu : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			House house = MultiItem.GetOwningMulti(self) as House;
			if (house == null) {
				return;
			}
			House.ShowInfoTo(house, clicker);
		}
	}
}
