using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#916 - server-side helpers for Tests/Protocol/PartyDialogTests.cs.</summary>
	public static class PartyDialogTests {
		private static Player first;

		/// <summary>Everyone at one spot, a plain player of a realm.</summary>
		[SteamFunction]
		public static void TestPartyReady(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			first = first ?? player;
			player.P(first.P());
			player.Realm = (sa.Args.Trim().Trim('"') == "M") ? Realm.Mordor : Realm.Gondor;
			player.Account.PLevel = 1;
			Console.WriteLine("PARTY_READY: uid=" + (uint) self.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestPartyHide(Character self, ScriptArgs sa) {
			self.Flag_Hidden = sa.Args.Trim().Trim('"') == "1";
			Console.WriteLine("PARTY_HIDDEN: done");
		}

		[SteamFunction]
		public static void TestPartyReport(Character self) {
			Party party = Party.GetParty(self);
			Console.WriteLine("PARTY_REPORT: inParty=" + (party != null) + " leader=" + ((party != null) ? party.Leader.Name.Replace(' ', '_') : "none") +
				" members=" + ((party != null) ? party.Members.Count : 0) + " end");
		}
	}
}
