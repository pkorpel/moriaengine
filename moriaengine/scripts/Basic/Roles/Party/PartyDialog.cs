using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#916 - .party, in the guildstone's style: a party is a quick arrangement, so everything
	/// about one is here. It lists only visible players of one's own realm, so it can't be used to find enemies.</summary>
	public class D_Party : CompiledGumpDef {
		private const int Width = 320, RowHeight = 20, MaxNearby = 8, Hue = 955, LabelHue = 0x481, TextHue = 2301, NearDistance = 2;
		private const int FoundButton = 1, LeaveButton = 2, DisbandButton = 3, KickButton = 100, PromoteButton = 200, InviteButton = 300;

		public static void Show(Player player) {
			Party party = Party.GetParty(player);
			List<Character> roster = (party != null) ? new List<Character>(party.OrderedMembers) : new List<Character>();
			player.Dialog(player, SingletonScript<D_Party>.Instance, new DialogArgs(roster, Nearby(player, party)));
		}

		/// <summary>Whom this player could invite: what they can see of their own realm, the closest stay
		/// on the list when it's full.</summary>
		private static List<Character> Nearby(Player player, Party party) {
			List<Character> found = new List<Character>();
			foreach (AbstractCharacter around in player.GetMap().GetPlayersInRange(player.X, player.Y, Globals.MaxUpdateRange)) {
				Character ch = around as Character;
				if ((ch != null) && (ch != player) && ch.IsOnline && !ch.Flag_Dead && (ch.Realm == player.Realm) &&
						!ch.Flag_Hidden && !ch.Flag_InvisByMagic && !ch.IsGM && ((party == null) || (Party.GetParty(ch) != party))) {
					found.Add(ch);
				}
			}
			found.Sort((a, b) => {
				int distance = DistanceTo(player, a).CompareTo(DistanceTo(player, b));
				return (distance != 0) ? distance : string.CompareOrdinal(a.Name, b.Name);
			});
			return (found.Count > MaxNearby) ? found.GetRange(0, MaxNearby) : found;
		}

		private static int DistanceTo(Character a, Character b) {
			return Point2D.GetSimpleDistance(a.X, a.Y, b.X, b.Y);
		}

		private static string Where(Player viewer, Character member) {
			if (member == viewer) {
				return "ty";
			}
			if (member.M != viewer.M) {
				return "daleko";
			}
			int distance = DistanceTo(viewer, member);
			if (distance <= NearDistance) {
				return "u tebe";
			}
			return (distance <= Globals.MaxUpdateRange) ? (distance + " poli") : "daleko";
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			List<Character> roster = (List<Character>) args[0];
			List<Character> nearby = (List<Character>) args[1];
			Party party = Party.GetParty(viewer);
			bool isLeader = (party != null) && party.IsLeader(viewer);
			int rows = roster.Count + nearby.Count + (isLeader ? 2 : 1);

			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, 84 + (rows * RowHeight) + 34, 2620);
			this.AddLabel(14, 12, LabelHue, "Druzina");
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			int y = 36;
			if (party != null) {
				this.AddLabel(14, y, LabelHue, "Vudce: " + party.Leader.Name);
				this.AddLabel(210, y, LabelHue, "Clenu: " + roster.Count + "/" + Party.Capacity);
				y += 24;
				for (int i = 0; i < roster.Count; i++) {
					Character member = roster[i];
					if (isLeader && (member != viewer)) {
						this.AddButton(14, y, 4017, 4019, KickButton + i, GumpButtonType.Reply, 0);
						this.AddButton(44, y, 4005, 4007, PromoteButton + i, GumpButtonType.Reply, 0);
					}
					this.AddLabel(76, y, TextHue, member.Name);
					this.AddLabel(200, y, TextHue, Where(viewer, member));
					this.AddLabel(262, y, TextHue, (member.MaxHits > 0) ? (((member.Hits * 100) / member.MaxHits) + "%") : "-");
					y += RowHeight;
				}
			} else {
				this.AddLabel(14, y, TextHue, "Nejsi v zadne druzine.");
				y += 24;
			}
			y += 6;
			this.AddLabel(14, y, LabelHue, "V okoli:");
			y += RowHeight;
			for (int i = 0; i < nearby.Count; i++) {
				this.AddButton(14, y, 4005, 4007, InviteButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, Hue, nearby[i].Name);
				this.AddLabel(240, y, Hue, "level " + ((Player) nearby[i]).Level);
				y += RowHeight;
			}
			y += 6;
			if (party == null) {
				this.Line(y, FoundButton, "Zalozit druzinu");
			} else {
				this.Line(y, LeaveButton, "Opustit druzinu");
				if (isLeader) {
					this.Line(y + RowHeight, DisbandButton, "Rozpustit druzinu");
				}
			}
		}

		private void Line(int y, int button, string text) {
			this.AddButton(14, y, 4005, 4007, button, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, Hue, text);
		}

		private static Character Row(List<Character> list, int index) {
			return ((index >= 0) && (index < list.Count)) ? list[index] : null;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			List<Character> roster = (List<Character>) args[0];
			List<Character> nearby = (List<Character>) args[1];
			Party party = Party.GetParty(player);
			int button = gr.PressedButton;
			if (button == 0) {
				return;
			}
			if ((button == FoundButton) && (party == null)) {
				PartyDef.NewParty(player);
			} else if ((button == LeaveButton) && (party != null)) {
				RolesManagement.TryUnAssign(player, party);
			} else if ((button == DisbandButton) && (party != null) && party.IsLeader(player)) {
				party.Disband();
			} else if (button >= InviteButton) {
				Character candidate = Row(nearby, button - InviteButton);
				if (candidate != null) {
					Invite(player, party, candidate);
				}
			} else if ((party != null) && party.IsLeader(player)) {
				Character member = Row(roster, button - ((button >= PromoteButton) ? PromoteButton : KickButton));
				if ((member != null) && (Party.GetParty(member) == party)) {
					if (button >= PromoteButton) {
						party.SetLeader(member);
					} else {
						RolesManagement.TryUnAssign(member, party);
					}
				}
			}
			Show(player);
		}

		/// <summary>A first invite makes the party, which goes away again if the invite can't be made.</summary>
		private static void Invite(Player player, Party party, Character candidate) {
			bool isNew = party == null;
			Party inviting = party ?? PartyDef.NewParty(player);
			if (!inviting.TryInvite(player, candidate) && isNew) {
				inviting.Dispose();
			}
		}
	}

	public static class PartyDialogCommands {
		[SteamFunction]
		public static void Party(Character self) {
			if (self is Player player) {
				D_Party.Show(player);
			}
		}
	}
}
