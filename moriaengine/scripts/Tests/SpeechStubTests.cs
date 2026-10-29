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
using System.Linq;

namespace SteamEngine.CompiledScripts {

	/// <summary>#825 - server-side helpers for Tests/Protocol/SpeechStubTests.cs.</summary>
	public static class SpeechStubTests {
		private const string ListenerDef = "c_h_quest_postava_0007"; //TSPEECH = spk_vypravec_vtipy, spk_kecy_a_drby

		[SteamFunction]
		public static void TestSpeechStubReport(Character self) {
			int stubs = AbstractScript.AllScripts.OfType<SpeechStub>().Count();
			SpeechStub chatter = AbstractScript.GetByDefname("spk_kecy_a_drby") as SpeechStub;
			string chatterPatterns = (chatter == null) ? "none" : (chatter.Patterns.Count + " first=" + chatter.Patterns[0]);
			NPCDef def = (NPCDef) ThingDef.GetByDefname(ListenerDef);
			bool defHasIt = def.GetAllTriggerGroups().Any(tg => tg.Defname.Equals("spk_kecy_a_drby", StringComparison.OrdinalIgnoreCase));
			Console.WriteLine("SPEECH_STUB_REPORT: stubs=" + stubs + " chatter=" + chatterPatterns + " defHasIt=" + defHasIt +
				" banker=" + TypeOf("spk_jobbanker") + " shopkeep=" + TypeOf("spk_shopkeep") + " vendorCustom=" + TypeOf("spk_vendor_custom") +
				" guard=" + TypeOf("spk_guard"));
		}

		private static string TypeOf(string defname) {
			AbstractScript s = AbstractScript.GetByDefname(defname);
			return (s == null) ? "none" : s.GetType().Name;
		}

		[SteamFunction]
		public static void TestSpawnSpeechStubListener(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = (NPCDef) ThingDef.GetByDefname(ListenerDef);
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("SPEECH_STUB_LISTENER: " + npc.Name);
		}

		/// <summary>Drops the account to a plain player (no GM) - the test's last step, as it can't run dot commands after.</summary>
		[SteamFunction]
		public static void TestSpeechStubBecomePlayer(Character self) {
			self.Account.PLevel = 1;
			Console.WriteLine("SPEECH_STUB_PLAYER: gm=" + self.IsGM);
		}
	}
}
