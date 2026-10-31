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

		//who joined when, the longest-standing first, except that the leader is always at the head
		private readonly List<Character> order = new List<Character>();

		private static readonly TagKey canLootMeTK = TagKey.Acquire("_party_can_loot_me_");

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

		/// <summary>#918 - whether one's party may take from one's corpse. On the character and not the
		/// party, as a client keeps its switch from party to party until it reconnects.</summary>
		public static bool CanLootMe(Character ch) {
			return ch.HasTag(canLootMeTK);
		}

		public static void SetCanLootMe(Character ch, bool canLoot) {
			if (canLoot) {
				ch.SetTag(canLootMeTK, true);
				ch.ClilocSysMessage(1005447); // You have chosen to allow your party to loot your corpse.
			} else {
				ch.RemoveTag(canLootMeTK);
				ch.ClilocSysMessage(1005448); // You have chosen to prevent your party from looting your corpse.
			}
		}

		/// <summary>A client logs in with its switch off.</summary>
		public static void ForgetCanLootMe(Character ch) {
			ch.RemoveTag(canLootMeTK);
		}

		public void Disband() {
			this.Dispose();
		}

		/// <summary>The members in order, the leader first, which is how a client tells who leads.</summary>
		public IList<Character> OrderedMembers {
			get {
				return this.order;
			}
		}

		/// <summary>#916 - hands the party over. Everyone's client hears of the new order.</summary>
		public void SetLeader(Character newLeader) {
			if (this.order.Remove(newLeader)) {
				this.order.Insert(0, newLeader);
				this.leader = newLeader;
				this.SendMemberList();
				foreach (Character member in this.order) {
					member.SysMessage(newLeader.Name + " je novym vudcem druziny.");
				}
			}
		}

		private void SendMemberList() {
			using (PacketGroup pg = PacketGroup.AcquireMultiUsePG()) {
				pg.AcquirePacket<AddPartyMembersOutPacket>().Prepare(this.order);
				foreach (Character ch in this.order) {
					GameState state = ch.GameState;
					if (state != null) {
						state.Conn.SendPacketGroup(pg);
					}
				}
			}
		}

		/// <summary>#916 - any member can invite, and not across the realms' war.</summary>
		public bool TryInvite(Character inviter, Character newCandidate) {
			Character leader = inviter;
			if (leader == null) {
				return false;
			}

			if (NPCAiTimer.IsOppositeRealm(inviter.Realm, newCandidate.Realm)) {
				inviter.ClilocSysMessage(1008088); // You cannot have players from opposing factions in the same party!
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

			if (GetParty(newCandidate) == this) { //#916 - someone in another party can accept, and leaves it
				leader.ClilocSysMessage(1005440); // This person is already in your party!
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
			this.order.Add(newMember);
			if (this.order.Count > 1) {
				newMember.ClilocSysMessage(1005445); // You have been added to the party.
			} else {
				this.leader = newMember;
			}
			this.candidates.Remove(newMember);
			this.SendMemberList();
		}

		//internal override bool On_DenyRemoveMember(DenyRoleTriggerArgs args) {
		//    return base.On_DenyRemoveMember(args);
		//}

		protected override void On_MemberRemoved(Character exMember, IRoleMembership membership, bool beingDestroyed) {
			this.order.Remove(exMember);
			if (!beingDestroyed && (this.order.Count < 2)) { //#916 - a party is at least two
				this.Disband();
				beingDestroyed = true;
			}
			if (!beingDestroyed && this.IsLeader(exMember)) { //#916 - the group goes on, with the longest-standing
				this.SetLeader(this.order[0]);
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
