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
using SteamEngine.Communication;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class Party : Role {
		private readonly List<Character> candidates;
		private readonly ReadOnlyCollection<Character> candidatesReadonly;
		private Character leader;

		public const int Capacity = 10;

		public static Party GetParty(Character ch) {
			return (Party) RolesManagement.GetRole(ch, PartyDef.rkParty);
		}

		public static bool AreInOneParty(Character a, Character b) {
			Party pa = GetParty(a);
			if (pa != null) {
				return pa == GetParty(b);
			}
			return false;
		}

		internal Party(PartyDef def, RoleKey key)
			: base(def, key) {

			this.candidates = new List<Character>();
			this.candidatesReadonly = new ReadOnlyCollection<Character>(this.candidates);
		}

		public bool IsLeader(AbstractCharacter ch) {
			if (this.leader != null) {
				return ch == this.leader;
			}
			return false;
		}

		public AbstractCharacter Leader {
			get {
				return this.leader;
			}
		}

		public ReadOnlyCollection<Character> Candidates {
			get {
				return this.candidatesReadonly;
			}
		}

		//TODO? loot setting
		//protected class PartyMembership : RoleMembership {
		//    public bool canBeLooted;
		//    internal PartyMembership(Character member) : base(member) {
		//    }
		//}
		//protected override Role.IRoleMembership CreateMembershipObject(Character member) {
		//    return new PartyMembership(member);
		//}

		public void Disband() {
			this.Dispose();
		}

		public bool TryInvite(Character newCandidate) {
			//todo realms
			//1008088); // You cannot have players from opposing factions in the same party!
			//1008093); // The party cannot have members from opposing factions.

			Character leader = (Character) this.Leader;
			if (leader == null) {
				return false;
			}

			if (leader == newCandidate) {
				leader.ClilocSysMessage(1005439); // You cannot add yourself to a party.
				return false;
			}

			if ((this.Members.Count + this.candidates.Count) >= Capacity) {
				leader.ClilocSysMessage(1008095); // You may only have 10 in your party (this includes candidates).
				return false;
			}

			if (!newCandidate.IsPlayer) {
				if (CharModelInfo.IsHumanModel(newCandidate.Model)) {
					GameState leaderState = leader.GameState;
					if (leaderState != null) {
						PacketSequences.SendClilocMessageFrom(leaderState.Conn, newCandidate, 1005443, -1, (string) null); // Nay, I would rather stay here and watch a nail rust.
					}
				} else {
					leader.ClilocSysMessage(1005444); // The creature ignores your offer.
				}
				return false;
			}

			Party hisParty = GetParty(newCandidate);
			if (hisParty != null) {
				if (hisParty == this) {
					leader.ClilocSysMessage(1005440); // This person is already in your party!
				} else {
					leader.ClilocSysMessage(1005441); // This person is already in a party!
				}
				return false;
			}

			GameState candidateState = newCandidate.GameState;
			if (candidateState != null) {
				PacketGroup pg = PacketGroup.AcquireMultiUsePG();
				pg.AcquirePacket<PartyInvitationOutPacket>().Prepare(leader.FlaggedUid);
				pg.AcquirePacket<ClilocMessageAffixOutPacket>().Prepare(null, 1008089, "System", SpeechType.Name, ClientFont.Unified, -1, AffixType.Prepend, leader.Name, "");//  : You are invited to join the party. Type /accept to join or /decline to decline the offer.
				candidateState.Conn.SendPacketGroup(pg);

				leader.ClilocSysMessage(1008090); // You have invited them to join the party.
				if (!this.candidates.Contains(newCandidate)) {
					this.candidates.Add(newCandidate);
				}
				return true;
			}
			leader.SysMessage("Hráè není pøipojen");
			return false;
		}

		public void MembershipDeclined(Character decliningCandidate) {
			AbstractCharacter leader = this.Leader;
			if (leader != null) {
				leader.SysMessage(decliningCandidate.Name + " odmítl tvé pozvání do party");
			}
			this.candidates.Remove(decliningCandidate);
		}

		public void SendPublicMessage(AbstractCharacter self, string text) {
			if (this.Members.Count > 0) {
				using (PacketGroup pg = PacketGroup.AcquireMultiUsePG()) {
					pg.AcquirePacket<TellFullPartyAMessageOutPacket>().Prepare(self.FlaggedUid, text);
					foreach (Character target in this.Members) {
						GameState state = target.GameState;
						if (state != null) {
							state.Conn.SendPacketGroup(pg);
						}
					}
				}
			}
		}

		public void SendPrivateMessage(AbstractCharacter self, AbstractCharacter target, string text) {
			GameState state = target.GameState;
			if (state != null) {
				TellPartyMemberAMessageOutPacket p = Pool<TellPartyMemberAMessageOutPacket>.Acquire();
				p.Prepare(self.FlaggedUid, text);
				state.Conn.SendSinglePacket(p);
			}
		}

		//internal override bool On_DenyAddMember(DenyRoleTriggerArgs args) {
		//    //todo realms
		//}

		protected override void On_MemberAdded(Character newMember, IRoleMembership membership) {
			////  : joined the party.
			//SendToAll(new MessageLocalizedAffix(Serial.MinusOne, -1, MessageType.Label, 0x3B2, 3, 1008094, "", AffixType.Prepend | AffixType.System, from.Name, ""));

			ICollection<Character> members = this.Members;
			if (members.Count > 1) {
				newMember.ClilocSysMessage(1005445); // You have been added to the party.
			} else {
				this.leader = newMember;
			}
			this.candidates.Remove(newMember);

			using (PacketGroup pg = PacketGroup.AcquireMultiUsePG()) {
				pg.AcquirePacket<AddPartyMembersOutPacket>().Prepare(members);
				foreach (Character ch in members) {
					GameState state = ch.GameState;
					if (state != null) {
						state.Conn.SendPacketGroup(pg);
					}
				}
			}
		}

		//internal override bool On_DenyRemoveMember(DenyRoleTriggerArgs args) {
		//    return base.On_DenyRemoveMember(args);
		//}

		protected override void On_MemberRemoved(Character exMember, IRoleMembership membership, bool beingDestroyed) {
			if (!beingDestroyed && this.IsLeader(exMember)) {
				this.Disband();
				beingDestroyed = true;
			}

			GameState exState = exMember.GameState;
			if (exState != null) {
				RemoveAPartyMemberOutPacket empty = Pool<RemoveAPartyMemberOutPacket>.Acquire();
				empty.Prepare(exMember, null);
				exState.Conn.SendSinglePacket(empty);
			}

			if (beingDestroyed) {
				exMember.ClilocSysMessage(1005449); // Your party has disbanded.
			} else {
				exMember.ClilocSysMessage(1005451); // You have been removed from the party.

				ICollection<Character> members = this.Members;
				if (members.Count > 0) {
					using (PacketGroup pg = PacketGroup.AcquireMultiUsePG()) {
						pg.AcquirePacket<RemoveAPartyMemberOutPacket>().Prepare(exMember, members);
						pg.AcquirePacket<ClilocMessageOutPacket>().Prepare(null, 1005452, "System", SpeechType.Speech, ClientFont.Unified, -1, ""); // 1005452 = A player has been removed from your party.
						foreach (Character ch in members) {
							GameState state = ch.GameState;
							if (state != null) {
								state.Conn.SendPacketGroup(pg);
							}
						}
					}
				}
			}
		}
	}
}
