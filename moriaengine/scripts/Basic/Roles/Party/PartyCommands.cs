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

namespace SteamEngine.CompiledScripts.Dialogs {

	public sealed class PartyCommands : Networking.PartyCommands {
		public static void Bootstrap() {
			new PartyCommands();
		}

		public override void RequestAddMember(AbstractCharacter ach) {
			Player self = ach as Player;
			if (self != null) {
				Party p = Party.GetParty(self);
				if (p != null)
				{
					if (!p.IsLeader(self)) {
						self.ClilocSysMessage(1005453); // You may only add members to the party if you are the leader.
						return;
					}
					if ((p.Members.Count + p.Candidates.Count) >= Party.Capacity) {
						self.ClilocSysMessage(1008095); // You may only have 10 in your party (this includes candidates).
						return;
					}
				}
				self.Target(SingletonScript<PartyAddTargetDef>.Instance);
			}
		}

		public override void RequestRemoveMember(AbstractCharacter ach, AbstractCharacter target) {
			Player self = ach as Player;
			if (self != null) {
				Party p = Party.GetParty(self);
				if (p != null) {
					if (p.IsLeader(self) || self == target) {
						RolesManagement.TryUnAssign((Character) target, p);
					}
				} else {
					self.ClilocSysMessage(3000211); // You are not in a party.
				}
			}
		}

		public override void RequestPrivateMessage(AbstractCharacter ach, AbstractCharacter target, string text) {
			if (!string.IsNullOrEmpty(text)) {
				Player self = ach as Player;
				if (self != null) {
					Party p = Party.GetParty(self);
					if (p != null) {
						if (p.IsMember((Character) target)) {
							p.SendPrivateMessage(self, target, text);
						}
					} else {
						self.ClilocSysMessage(3000211); // You are not in a party.
					}
				}
			}
		}

		public override void RequestPublicMessage(AbstractCharacter ach, string text) {
			if (!string.IsNullOrEmpty(text)) {
				Player self = ach as Player;
				if (self != null) {
					Party p = Party.GetParty(self);
					if (p != null) {
						p.SendPublicMessage(self, text);
					} else {
						self.ClilocSysMessage(3000211); // You are not in a party.
					}
				}
			}
		}

		public override void SetCanLoot(AbstractCharacter self, bool canLoot) {
			//TODO?
		}

		public override void AcceptJoinRequest(AbstractCharacter self, AbstractCharacter leader) {
			Character candidate = (Character) self;
			Party party = Party.GetParty((Character) leader);
			if (leader == null || party == null || !party.Candidates.Contains(candidate)) {
				self.ClilocSysMessage(3000222); // No one has invited you to be in a party.
			} else {
				RolesManagement.TryAssign(candidate, party);
			}
		}

		public override void DeclineJoinRequest(AbstractCharacter self, AbstractCharacter leader) {
			Character candidate = (Character) self;
			Party party = Party.GetParty((Character) leader);
			if (leader == null || party == null || !party.Candidates.Contains(candidate)) {
				self.ClilocSysMessage(3000222); // No one has invited you to be in a party.
			} else {
				party.MembershipDeclined(candidate);
			}
		}
	}

	public class PartyAddTargetDef : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.ClilocSysMessage(1005454); // Who would you like to add to your party?
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Party myParty = Party.GetParty(self);
			bool isNew = false;
			if (myParty != null) {
				if (!myParty.IsLeader(self)) {
					self.ClilocSysMessage(1005453); // You may only add members to the party if you are the leader.
					return TargetResult.Done;
				}
			} else {
				myParty = PartyDef.NewParty(self);
				isNew = true;
			}

			bool inviteLegal = myParty.TryInvite(targetted);
			if (!inviteLegal && isNew) {
				myParty.Dispose();
			}
			return inviteLegal ? TargetResult.Done : TargetResult.RestartTargetting;
		}
	}
}