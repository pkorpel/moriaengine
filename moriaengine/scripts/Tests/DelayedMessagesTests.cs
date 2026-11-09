using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {
	/// <summary>#901 - the waiting messages, driven by Tests/Protocol/DelayedMessagesTests.cs.</summary>
	public static class DelayedMessagesTests {
		private static Player Of(ScriptArgs sa) {
			return (Player) Thing.UidGetCharacter(uint.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture));
		}

		/// <summary>Two plain messages and an important one to the player with this uid, through Player's own calls
		/// (the conquest's and PvP's) and DelayedMessages.Send (the towns', the realm's, the rings').</summary>
		[SteamFunction]
		public static void TestDelayedSend(Character self, ScriptArgs sa) {
			Player to = Of(sa);
			to.DelayedMessage("Zprava jedna");
			DelayedMessages.Send(to, "Zprava dva");
			to.DelayedRedMessage("Dulezita jedna");
			Console.WriteLine("DELAYED_SENT: end");
		}

		[SteamFunction]
		public static void TestDelayedReport(Character self, ScriptArgs sa) {
			Player who = ((sa == null) || (sa.Args.Trim().Trim('"').Length == 0)) ? (Player) self : Of(sa);
			Console.WriteLine("DELAYED_REPORT: uid=" + (uint) who.FlaggedUid + " online=" + DelayedMessages.IsPlaying(who) + " normal=" + DelayedMessages.Count(who, false) +
				" red=" + DelayedMessages.Count(who, true) + " first=" + string.Join("|", DelayedMessages.Read(who, false)).Replace(' ', '_') + " end");
		}
	}
}
