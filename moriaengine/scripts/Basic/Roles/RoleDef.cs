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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class RoleDef : AbstractDef {
		internal static readonly TriggerKey tkMemberAdded = TriggerKey.Acquire("MemberAdded");
		internal static readonly TriggerKey tkMemberRemoved = TriggerKey.Acquire("MemberRemoved");
		internal static readonly TriggerKey tkCreate = TriggerKey.Acquire("Create");
		internal static readonly TriggerKey tkDestroy = TriggerKey.Acquire("Destroy");
		internal static readonly TriggerKey tkDenyRemoveMember = TriggerKey.Acquire("DenyRemoveMember");
		internal static readonly TriggerKey tkDenyAddMember = TriggerKey.Acquire("DenyAddMember");

		//private static Dictionary<string, RoleDef> byName = new Dictionary<string, RoleDef>(StringComparer.OrdinalIgnoreCase);

		//private static Dictionary<string, ConstructorInfo> roleDefCtorsByName = new Dictionary<string, ConstructorInfo>(StringComparer.OrdinalIgnoreCase);

		private TriggerGroup scriptedTriggers;

		/// <summary>
		/// Method for instatiating Roles. Basic implementation is easy but the CreateImpl method should be overriden 
		/// in every RoleDef's descendant!
		/// </summary>
		public Role Create(RoleKey key, string name) {
			Role newRole = this.Create(key);
			newRole.Name = name;
			return newRole;
		}

		/// <summary>
		/// Method for instatiating Roles. Basic implementation is easy but the CreateImpl method should be overriden 
		/// in every RoleDef's descendant!
		/// </summary>
		public Role Create(RoleKey key) {
			Role newRole = this.CreateImpl(key);
			newRole.Trigger_Create();
			return newRole;
		}

		internal Role CreateWhenLoading(RoleKey key) {
			Role newRole = this.CreateImpl(key);
			return newRole;
		}

		protected virtual Role CreateImpl(RoleKey key) {
			return new Role(this, key);
		}

		public new static RoleDef GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as RoleDef;
		}

		#region Loading from scripts

		public RoleDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			//name = InitField_Typed("name", "", typeof(string));
		}

		public override void LoadScriptLines(PropsSection ps) {
			base.LoadScriptLines(ps);

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
		#endregion Loading from scripts

		public TriggerResult TryCancellableTrigger(Role role, TriggerKey td, ScriptArgs sa) {
			//cancellable trigger just for the one triggergroup
			if (this.scriptedTriggers != null) {
				if (TagMath.Is1(this.scriptedTriggers.TryRun(role, td, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		public void TryTrigger(Role role, TriggerKey td, ScriptArgs sa) {
			if (this.scriptedTriggers != null) {
				this.scriptedTriggers.TryRun(role, td, sa);
			}
		}

		public override string ToString() {
			return Tools.TypeToString(this.GetType()) + " " + this.Defname;
		}

		#region utilities
		/// <summary>Return enumerable containing all roles (copying the values from the main dictionary)</summary>
		public static IEnumerable<RoleDef> AllRoles {
			get {
				foreach (AbstractScript script in AllScripts) {
					RoleDef roleDef = script as RoleDef;
					if (roleDef != null) {
						yield return roleDef;
					}
				}
			}
		}
		#endregion utilities
	}

	/// <summary>Argument wrapper used in DenyMemberAddRequest trigger</summary>
	public class DenyRoleTriggerArgs : DenyTriggerArgs {
		public readonly Role.IRoleMembership membership;
		public readonly Character assignee;
		public readonly Role role;

		public DenyRoleTriggerArgs(Character assignee, Role.IRoleMembership membership, Role role)
			: base(DenyResultMessages.Allow, assignee, membership, role) {
			this.membership = membership;
			this.role = role;
			this.assignee = assignee;
		}
	}
}
