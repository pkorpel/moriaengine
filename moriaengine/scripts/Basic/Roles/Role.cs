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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	[SaveableClass]
	public partial class Role : Disposable {
		private RoleDef def;
		private RoleKey key;
		private string name;

		private Dictionary<Character, IRoleMembership> members = new Dictionary<Character, IRoleMembership>();

		//private System.Collections.ObjectModel.ReadOnlyCollection<Character> membersReadOnly;

		internal Role(RoleDef def, RoleKey key) {
			//this.members = new List<Character>();
			//this.memberships = new List<IRoleMembership>();
			//this.membersReadOnly = new System.Collections.ObjectModel.ReadOnlyCollection<IRoleMembership>(this.members);
			this.key = key;
			this.def = def;
		}

		internal void InternalAddMember(Character newMember) {
			IRoleMembership membership = this.CreateMembershipObject(newMember);
			Sanity.IfTrueThrow(newMember != membership.Member, "newMember != membership.Member");

			this.members.Add(newMember, membership);
			this.Trigger_MemberAdded(newMember, membership);
		}

		internal void InternalRemoveMember(Character removedMember, IRoleMembership membership) {
			Sanity.IfTrueThrow(removedMember != membership.Member, "removedMember != membership.Member");

			bool removed = this.members.Remove(removedMember);
			Sanity.IfTrueThrow(!removed, "!this.members.ContainsKey(removedMember)");

			this.Trigger_MemberRemoved(removedMember, membership, false);
		}

		internal void InternalLoadMembership(IRoleMembership loaded) {
			RolesManagement.InternalAddLoadedRole(this, loaded.Member);
			this.members.Add(loaded.Member, loaded);
		}

		internal void InternalClearMembers(bool beingDestroyed) {
			if (this.members.Count > 0) {
				KeyValuePair<Character, IRoleMembership>[] oldMembers = new KeyValuePair<Character, IRoleMembership>[this.members.Count];
				((ICollection<KeyValuePair<Character, IRoleMembership>>) this.members).CopyTo(oldMembers, 0);
				this.members.Clear();
				foreach (KeyValuePair<Character, IRoleMembership> pair in oldMembers) {
					this.Trigger_MemberRemoved(pair.Key, pair.Value, beingDestroyed);
				}
			}
		}

		public interface IRoleMembership : IDisposable {
			Character Member { get; }
		}

		protected virtual IRoleMembership CreateMembershipObject(Character member) {
			return new RoleMembership(member, this);
		}

		#region triggers

		internal void Trigger_Create() {
			this.def.TryTrigger(this, RoleDef.tkCreate, null);
			try {
				this.On_Create();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal void Trigger_Destroy() {
			RolesManagement.UnassignAll(this, true);

			this.def.TryTrigger(this, RoleDef.tkDestroy, null);
			try {
				this.On_Destroy();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResult Trigger_DenyAddMember(Character chr) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, null, this);
			if (TriggerResult.Cancel != this.def.TryCancellableTrigger(this, RoleDef.tkDenyAddMember, args)) {
				try {
					this.On_DenyAddMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		private void Trigger_MemberAdded(Character newMember, IRoleMembership membership) {
			this.def.TryTrigger(this, RoleDef.tkMemberAdded, new ScriptArgs(newMember, membership));
			try {
				this.On_MemberAdded(newMember, membership);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal DenyResult Trigger_DenyRemoveMember(Character chr, IRoleMembership membership) {
			DenyRoleTriggerArgs args = new DenyRoleTriggerArgs(chr, membership, this);
			if (TriggerResult.Cancel != this.def.TryCancellableTrigger(this, RoleDef.tkDenyRemoveMember, args)) {
				try {
					this.On_DenyRemoveMember(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return args.Result;
		}

		internal void Trigger_MemberRemoved(Character chr, IRoleMembership membership, bool beingDestroyed) {
			this.def.TryTrigger(this, RoleDef.tkMemberRemoved, new ScriptArgs(chr, membership, beingDestroyed));
			try {
				this.On_MemberRemoved(chr, membership, beingDestroyed);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		/// <summary>Trigger called when the new role is created</summary>
		protected virtual void On_Create() {
		}

		/// <summary>Trigger called when the role is destroyed</summary>
		protected virtual void On_Destroy() {
		}

		/// <summary>Trigger called when the new member adding is requested from this role</summary>
		protected virtual void On_DenyAddMember(DenyRoleTriggerArgs args) {
		}

		/// <summary>Trigger called when the new member is assigned to this role</summary>
		protected virtual void On_MemberAdded(Character newMember, IRoleMembership membership) {
			//this trigger will be run after @DenyAddMember
		}

		/// <summary>Trigger called when the member remove is requested from this role</summary>
		protected virtual void On_DenyRemoveMember(DenyRoleTriggerArgs args) {
		}

		/// <summary>Trigger called when the member is unassigned from this role</summary>
		protected virtual void On_MemberRemoved(Character exMember, IRoleMembership membership, bool beingDestroyed) {
			//this trigger will be run after @DenyRemoveMember
		}

		#endregion triggers

		public RoleDef RoleDef {
			get {
				return this.def;
			}
		}

		public RoleKey Key {
			get {
				return this.key;
			}
		}

		public string Name {
			get {
				return this.name;
			}
			set {
				this.name = value;
			}
		}

		public override string ToString() {
			return string.Concat(this.GetType().ToString(), " °", this.key.Name);
		}

		public ICollection<Character> Members {
			get {
				return this.members.Keys;
			}
		}
		public ICollection<IRoleMembership> Memberships {
			get {
				return this.members.Values;
			}
		}

		public IRoleMembership GetMembership(Character ch) {
			IRoleMembership retVal;
			this.members.TryGetValue(ch, out retVal);
			return retVal;
		}

		internal bool IsMember(Character target) {
			if (target != null) {
				return this.members.ContainsKey(target);
			}
			return false;
		}

		protected override void On_DisposeManagedResources() {
			this.Trigger_Destroy();
			base.On_DisposeManagedResources();
		}

		#region persistence
		[Save]
		public virtual void Save(SaveStream output) {
			output.WriteValue("def", this.def);
			output.WriteValue("key", this.key);
			if (this.name != null) {
				output.WriteValue("name", this.name);
			}
			//int count = this.members.Count;
			//output.WriteValue("count", count);
			//int i = 0;
			foreach (IRoleMembership membership in this.members.Values) {
				//output.WriteValue(i.ToString(), membership);				
				//i++;
				ObjectSaver.Save(membership);
			}
		}

		[LoadSection]
		public static Role LoadSection(PropsSection input) {
			int currentLineNumber = input.HeaderLine;
			try {
				PropsLine pl = input.PopPropsLine("def");
				currentLineNumber = pl.Line;
				RoleDef def = (RoleDef) ObjectSaver.OptimizedLoad_Script(pl.Value);

				pl = input.PopPropsLine("key");
				currentLineNumber = pl.Line;
				RoleKey key = (RoleKey) ObjectSaver.OptimizedLoad_SimpleType(pl.Value, typeof(RoleKey));

				//pl = input.PopPropsLine("count");
				//currentLineNumber = pl.line;
				//int count = ConvertTools.ParseInt32(pl.value);

				Role role = def.CreateWhenLoading(key);

				//for (int i = 0; i < count; i++) {
				//    pl = input.PopPropsLine(i.ToString());
				//    if (pl != null) {
				//        ObjectSaver.Load(pl.value, role.Load_RoleMembership, input.filename, pl.line);
				//    }
				//}

				foreach (PropsLine p in input.PropsLines) {
					try {
						role.LoadLine(input.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
					} catch (FatalException) {
						throw;
					} catch (Exception ex) {
						Logger.WriteWarning(input.Filename, p.Line, ex);
					}
				}

				return role;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.Filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.Filename, currentLineNumber, e);
			}
		}

		protected virtual void LoadLine(string filename, int line, string valueName, string valueString) {
			if (valueName.Equals("name", StringComparison.OrdinalIgnoreCase)) {
				this.name = (string) ObjectSaver.OptimizedLoad_String(valueString);
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		#endregion persistence
	}
}
