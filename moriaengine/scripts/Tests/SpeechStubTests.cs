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
		private const string GuardDef = "c_vguard_rohan_hala_jezdec"; //TSPEECH = spk_guard

		[SteamFunction]
		public static void TestSpeechStubReport(Character self) {
			int stubs = AbstractScript.AllScripts.OfType<SpeechStub>().Count();
			SpeechStub guard = AbstractScript.GetByDefname("spk_guard") as SpeechStub;
			string guardPatterns = (guard == null) ? "none" : string.Join("|", guard.Patterns);
			NPCDef def = (NPCDef) ThingDef.GetByDefname(GuardDef);
			bool defHasIt = def.GetAllTriggerGroups().Any(tg => tg.Defname.Equals("spk_guard", StringComparison.OrdinalIgnoreCase));
			Console.WriteLine("SPEECH_STUB_REPORT: stubs=" + stubs + " guard=" + guardPatterns + " guardDefHasIt=" + defHasIt +
				" banker=" + TypeOf("spk_jobbanker") + " shopkeep=" + TypeOf("spk_shopkeep") + " vendorCustom=" + TypeOf("spk_vendor_custom"));
		}

		private static string TypeOf(string defname) {
			AbstractScript s = AbstractScript.GetByDefname(defname);
			return (s == null) ? "none" : s.GetType().Name;
		}

		[SteamFunction]
		public static void TestSpawnSpeechStubGuard(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = (NPCDef) ThingDef.GetByDefname(GuardDef);
			NPC guard = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("SPEECH_STUB_GUARD: " + guard.Name);
		}

		/// <summary>Drops the account to a plain player (no GM) - the test's last step, as it can't run dot commands after.</summary>
		[SteamFunction]
		public static void TestSpeechStubBecomePlayer(Character self) {
			self.Account.PLevel = 1;
			Console.WriteLine("SPEECH_STUB_PLAYER: gm=" + self.IsGM);
		}
	}
}
