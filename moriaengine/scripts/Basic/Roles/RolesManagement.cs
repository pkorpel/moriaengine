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

namespace SteamEngine.CompiledScripts {
	/// <summary>Utility class managing everything about roles including assigning and storing information about casts</summary>
	public static class RolesManagement {
		//dictionary holding a set of assigned roles to all characters that have any...
		internal static Dictionary<Character, Dictionary<RoleKey, Role>> charactersRoles = new Dictionary<Character, Dictionary<RoleKey, Role>>();

		/// <summary>
		/// Try assign chr to role. Runs and obeys @deny triggers.
		/// </summary>
		/// <param name="chr">The CHR.</param>
		/// <param name="role">The role.</param>
		/// <returns>Allow = true: chr is now member of role, otherwise it's not</returns>
		public static DenyResult TryAssign(Character chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						return DenyResultMessages.Allow; // we're already in place, all is ok
					}
					DenyResult result = role.Trigger_DenyAddMember(chr); //check if we can enter this role
					if (result.Allow) {
						result = TryUnAssign(chr, prevRole); //check if we can leave the previous role
						if (!result.Allow) {
							return result; //can't leave previous one => can't enter new one
						}
					}
					return result;
				}
			} else {
				DenyResult result = role.Trigger_DenyAddMember(chr);
				if (result.Allow) {
					rolesByKey = new Dictionary<RoleKey, Role>(); //check if we can enter this role
					charactersRoles[chr] = rolesByKey;
				} else {
					return result;
				}
			}

			rolesByKey[key] = role;
			role.InternalAddMember(chr);
			return DenyResultMessages.Allow;
		}

		/// <summary>Assign chr to role. Ignores @deny triggers.</summary>
		public static void Assign(Character chr, Role role) {
			InternalAddLoadedRole(role, chr);
			role.InternalAddMember(chr);
		}

		internal static void InternalAddLoadedRole(Role role, Character chr) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole))
				{
					if (role == prevRole) {
						return; // we're already in place, all is ok
					}
					UnAssign(chr, role);
				}
			} else {
				rolesByKey = new Dictionary<RoleKey, Role>();
				charactersRoles[chr] = rolesByKey;
			}

			rolesByKey[key] = role;
			//role.InternalAddMember(chr); this is done when calling this method
		}

		/// <summary>
		/// Find a list of characters for given role and remove the specified character from it
		/// then remove the role from the character's roles list
		/// </summary>
		/// <param name="chr">The CHR.</param>
		/// <param name="role">The role.</param>
		/// <returns>true = chr is not member of role, false = it is</returns>
		public static DenyResult TryUnAssign(Character chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						Role.IRoleMembership membership = role.GetMembership(chr);
						DenyResult result = role.Trigger_DenyRemoveMember(chr, membership);
						if (result.Allow) {
							if (rolesByKey.Count == 1) { //last role of that char
								charactersRoles.Remove(chr);
							} else {
								rolesByKey.Remove(key);
							}
							role.InternalRemoveMember(chr, membership);
						}
						return result;
					}
				}
			}
			return DenyResultMessages.Allow; //wasn't member in the first place
		}

		public static void UnAssign(Character chr, Role role) {
			RoleKey key = role.Key;
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role prevRole;
				if (rolesByKey.TryGetValue(key, out prevRole)) {
					if (role == prevRole) {
						if (rolesByKey.Count == 1) { //last role of that char
							charactersRoles.Remove(chr);
						} else {
							rolesByKey.Remove(key);
						}
						role.InternalRemoveMember(chr, role.GetMembership(chr));
					}
				}
			}
		}

		public static void UnassignAll(Role role, bool beingDestroyed) {
			RoleKey key = role.Key;
			foreach (Character chr in role.Members) {
				Dictionary<RoleKey, Role> rolesByKey;
				if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
					rolesByKey.Remove(key);
					if (rolesByKey.Count == 0) { //last role of that char
						charactersRoles.Remove(chr);
					}
				}
			}
			role.InternalClearMembers(beingDestroyed);
		}

		public static Role GetRole(Character chr, RoleKey roleKey) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role retVal;
				if (rolesByKey.TryGetValue(roleKey, out retVal)) {
					return retVal;
				}
			}
			return null;
		}

		public static bool HasRole(Character chr, Role role) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				Role byKey;
				if (rolesByKey.TryGetValue(role.Key, out byKey)) {
					return role == byKey;
				}
			}
			return false;
		}

		public static bool HasRole(Character chr, RoleKey roleKey) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				return rolesByKey.ContainsKey(roleKey);
			}
			return false;
		}

		public static ICollection<Character> GetCharactersInRole(Role role) {
			if (role != null) {
				return role.Members;
			}
			return EmptyReadOnlyGenericCollection<Character>.instance;
		}

		public static ICollection<Role> GetCharactersRoles(Character chr) {
			Dictionary<RoleKey, Role> rolesByKey;
			if (charactersRoles.TryGetValue(chr, out rolesByKey)) {
				return rolesByKey.Values;
			}
			return EmptyReadOnlyGenericCollection<Role>.instance;
		}
	}
}
