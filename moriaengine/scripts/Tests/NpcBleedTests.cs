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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - server-side setup helpers
	/// for Tests/Protocol/NpcBleedTests.cs. Calls MonsterSpecialAttacks.TryApplyBleed directly
	/// with an explicit actualDamage value, same "deterministic direct call" precedent as the
	/// stun/critic tests - no need to wait out a real swing's RNG to get a specific damage
	/// number.
	/// </summary>
	public static class NpcBleedTests {
		private static BleedingEffectPlugin Bleed(Character ch) {
			return ch.GetPlugin(SingletonScript<BleedingEffectPluginDef>.Instance.PluginKey) as BleedingEffectPlugin;
		}

		[SteamFunction]
		public static void TestBleedAppliesOnHit(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_bleed_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //30 damage -> (30/3)/2 = 5, Erebor's npc_bleed

			BleedingEffectPlugin plugin = Bleed(dummy);
			bool bleeding = plugin != null;
			double effectPower = plugin != null ? plugin.BleedPower : -1;
			Console.WriteLine("BLEED_APPLY_TEST: bleeding=" + bleeding + " effectPower=" + effectPower);
		}

		[SteamFunction]
		public static void TestBleedStacksOnRepeatedHits(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_bleed_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //+5
			BleedingEffectPlugin plugin = Bleed(dummy);
			double afterFirst = plugin != null ? plugin.BleedPower : -1;

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //should stack: +5 more
			plugin = Bleed(dummy);
			double afterSecond = plugin != null ? plugin.BleedPower : -1;

			//#745 - it's the one bleed a bandage stops, as Erebor's i_krvaceni
			self.SetRealSkillValue((int) SkillName.Veterinary, 1000);
			for (int attempt = 0; (attempt < 10) && (Bleed(dummy) != null); attempt++) {
				Bandaging.Finish(self, dummy, false); //a 1000 skill fails one roll in 41
			}
			bool bandaged = Bleed(dummy) == null;
			Console.WriteLine("BLEED_STACK_TEST: afterFirst=" + afterFirst + " afterSecond=" + afterSecond +
				" stacked=" + (afterSecond > afterFirst) + " bandageStops=" + bandaged);
		}

		[SteamFunction]
		public static void TestBleedTickSelfRemovesOnFullResist(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_bleed_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);
			dummy.ResistBleed = 1000; //full resist - same permille scale as the other Resist* fields

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30);
			BleedingEffectPlugin plugin = Bleed(dummy);
			bool appliedBeforeTick = plugin != null;

			if (plugin != null) {
				plugin.On_FadingEffectTick();
			}
			bool stillBleedingAfterTick = Bleed(dummy) != null;

			Console.WriteLine("BLEED_RESIST_TEST: appliedBeforeTick=" + appliedBeforeTick +
				" stillBleedingAfterTick=" + stillBleedingAfterTick);
		}
	}
}
