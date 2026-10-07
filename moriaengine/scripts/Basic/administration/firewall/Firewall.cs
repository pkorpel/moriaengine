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

using System.Collections;
using System.Collections.Generic;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public class E_Firewall_Global : CompiledTriggerGroup {
		public void On_ClientAttach(Globals ignored, GameState state, TcpConnection<GameState> conn) {
			if (Firewall.IsBlockedIP(conn.EndPoint.Address)) {
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.Blocked);
				conn.Close("IP Blocked");
			}
		}
	}

	[HasSavedMembers]
	public static class Firewall {
		[SavedMember]
		private static Dictionary<IPAddress, FirewallEntry> blockedIPEntries = new Dictionary<IPAddress, FirewallEntry>();
		[SavedMember]
		private static List<FirewallEntry> blockedIPRangeEntries = new List<FirewallEntry>();

		public static List<FirewallEntry> GetAllEntries() {
			List<FirewallEntry> entries = new List<FirewallEntry>(blockedIPRangeEntries);
			entries.AddRange(blockedIPEntries.Values);
			return entries;
		}

		public static void AddBlockedIP(IPAddress ip, string reason, AbstractAccount blockedBy) {
			if (IsBlockedIP(ip)) {
				Globals.SrcWriteLine("The IP " + ip + " is already blocked.");
				return;
			}
			blockedIPEntries[ip] = new FirewallEntry(ip, reason, blockedBy);
			Globals.SrcWriteLine("The IP '" + ip + "' has been blocked.");
		}

		public static void AddBlockedIP(string ip, string reason, AbstractAccount blockedBy) {
			AddBlockedIP(IPAddress.Parse(ip), reason, blockedBy);
		}

		public static void RemoveBlockedIP(IPAddress ip) {
			if (IsBlockedIP(ip) == false) {
				return;
			}
			blockedIPEntries.Remove(ip);
			Globals.SrcWriteLine("The IP " + ip + " has been unblocked.");
		}

		public static void RemoveBlockedIP(string ip) {
			RemoveBlockedIP(IPAddress.Parse(ip));
		}

		public static bool IsBlockedIP(IPAddress ip) {
			if (blockedIPEntries.ContainsKey(ip)) {
				return true;
			}

			foreach (FirewallEntry bire in blockedIPRangeEntries) {
				if (IsIPInRange(ip, bire.LowerBound, bire.UpperBound)) {
					return true;
				}
			}
			return false;
		}


		public static bool IsBlockedIP(string IP) {
			return IsBlockedIP(IPAddress.Parse(IP));
		}

		public static void ShowBlockedIPs() {
			foreach (FirewallEntry bie in blockedIPEntries.Values) {
				Globals.SrcWriteLine("Blocked IP: " + bie.LowerBound + " Reason: " + bie.Reason + " Blocked by: " + bie.BlockedBy);
			}
			foreach (FirewallEntry bire in blockedIPRangeEntries) {
				Globals.SrcWriteLine("Blocked IPRange from: " + bire.LowerBound + " to: " + bire.UpperBound + " Reason: " + bire.Reason + " Blocked by: " + bire.BlockedBy);
			}
		}

		public static void AddBlockedIPRange(IPAddress lowerBound, IPAddress upperBound, string reason, AbstractAccount blockedBy) {
			if (CompareIPs(lowerBound, upperBound) == 0) {
				AddBlockedIP(lowerBound, reason, blockedBy);
			} else {
				blockedIPRangeEntries.Add(new FirewallEntry(lowerBound, upperBound, reason, blockedBy));
			}
		}

		public static void AddBlockedIPRange(string lowerBound, string upperBound, string reason, AbstractAccount blockedBy) {
			AddBlockedIPRange(IPAddress.Parse(lowerBound), IPAddress.Parse(upperBound), reason, blockedBy);
		}


		public static void RemoveBlockedIPRange(IPAddress lowerBound, IPAddress upperBound) {
			foreach (FirewallEntry bipe in blockedIPRangeEntries) {
				if ((bipe.LowerBound.Equals(lowerBound)) && (bipe.UpperBound.Equals(upperBound))) {
					blockedIPRangeEntries.Remove(bipe);
					Globals.SrcWriteLine("IP range: " + bipe.LowerBound + "-" + bipe.UpperBound + " has been unblocked.");
					return;
				}
			}
			Globals.SrcWriteLine("IP range not found");
		}

		public static void RemoveBlockedIPRange(string fromIP, string toIP) {
			RemoveBlockedIPRange(IPAddress.Parse(fromIP), IPAddress.Parse(toIP));
		}

		public static bool IsIPInRange(IPAddress ip, IPAddress lower, IPAddress upper) {
			return (CompareIPs(ip, lower) >= 0) && (CompareIPs(ip, upper) <= 0);
		}

		public static int CompareIPs(IPAddress a, IPAddress b) {
			IStructuralComparable x = a.GetAddressBytes();
			IStructuralComparable y = b.GetAddressBytes();

			return x.CompareTo(y, Comparer<byte>.Default);
		}
	}

	[SaveableClass]
	[ViewableClass]
	public class FirewallEntry {

		public FirewallEntry(IPAddress ip, string reason, AbstractAccount blockedBy) {
			this.lowerBound = ip;
			this.upperBound = ip;
			this.reason = reason;
			this.blockedBy = blockedBy;
		}

		public FirewallEntry(IPAddress lowerBound, IPAddress upperBound, string reason, AbstractAccount blockedBy) {
			Sanity.IfTrueThrow(Firewall.CompareIPs(lowerBound, upperBound) > 0, "lowerBound > higherBound");

			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
			this.reason = reason;
			this.blockedBy = blockedBy;
		}

		private IPAddress lowerBound;
		private IPAddress upperBound;
		private string reason;
		private AbstractAccount blockedBy;

		public IPAddress LowerBound {
			get { return this.lowerBound; }
			set { this.lowerBound = value; }
		}

		public IPAddress UpperBound {
			get { return this.upperBound; }
			set { this.upperBound = value; }
		}

		public string Reason {
			get { return this.reason; }
			set { this.reason = value; }
		}

		public AbstractAccount BlockedBy {
			get { return this.blockedBy; }
			set { this.blockedBy = value; }
		}

		public bool IsSingleIPEntry {
			get {
				return Firewall.CompareIPs(this.lowerBound, this.upperBound) == 0;
			}
		}

		#region Persistence
		[LoadSection]
		public static FirewallEntry Load(PropsSection section) {
			IPAddress lowerBound = (IPAddress) ObjectSaver.OptimizedLoad_SimpleType(section.PopPropsLine("lowerBound").Value, typeof(IPAddress));
			IPAddress upperBound = lowerBound;
			var upperBoundLine = section.TryPopPropsLine("upperBound");
			if (upperBoundLine != null) {
				upperBound = (IPAddress) ObjectSaver.OptimizedLoad_SimpleType(upperBoundLine.Value, typeof(IPAddress));
			}

			string reason = null;
			var reasonLine = section.TryPopPropsLine("reason");
			if (reasonLine != null) {
				reason = (string) ObjectSaver.OptimizedLoad_String(reasonLine.Value);
			}

			FirewallEntry retVal = new FirewallEntry(lowerBound, upperBound, reason, null);

			var blockedByLine = section.TryPopPropsLine("blockedBy");
			if (blockedByLine != null) {
				ObjectSaver.Load(blockedByLine.Value, retVal.DelayedLoad_BlockedBy, section.Filename, blockedByLine.Line);
			}

			return retVal;
		}

		private void DelayedLoad_BlockedBy(object resolvedObject, string filename, int line) {
			this.blockedBy = (AbstractAccount) resolvedObject;
		}

		[Save]
		public void Save(SaveStream stream) {
			stream.WriteValue("lowerBound", this.lowerBound);
			if (!this.IsSingleIPEntry) {
				stream.WriteValue("upperBound", this.upperBound);
			}
			if (!string.IsNullOrEmpty(this.reason)) {
				stream.WriteValue("reason", this.reason);
			}
			if (this.blockedBy != null) {
				stream.WriteValue("blockedBy", this.blockedBy);
			}
		}
		#endregion Persistence
	}
}