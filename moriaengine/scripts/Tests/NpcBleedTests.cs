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
		[SteamFunction]
		public static void TestBleedAppliesOnHit(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_bleed_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //30 damage -> expect effectPower 10

			NpcBleedEffectPlugin plugin = dummy.GetPlugin(SingletonScript<NpcBleedEffectPluginDef>.Instance.PluginKey) as NpcBleedEffectPlugin;
			bool bleeding = plugin != null;
			double effectPower = plugin != null ? plugin.EffectPower : -1;
			Console.WriteLine("BLEED_APPLY_TEST: bleeding=" + bleeding + " effectPower=" + effectPower);
		}

		[SteamFunction]
		public static void TestBleedStacksOnRepeatedHits(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef attackerDef = (NPCDef) ThingDef.GetByDefname("npc_test_bleed_attacker");
			NPCDef dummyDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC attacker = (NPC) attackerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			NPC dummy = (NPC) dummyDef.Create(self.X + 2, self.Y, self.Z, self.M);

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //effectPower +10
			NpcBleedEffectPlugin plugin = dummy.GetPlugin(SingletonScript<NpcBleedEffectPluginDef>.Instance.PluginKey) as NpcBleedEffectPlugin;
			double afterFirst = plugin != null ? plugin.EffectPower : -1;

			MonsterSpecialAttacks.TryApplyBleed(attacker, dummy, 30); //should stack: +10 more
			plugin = dummy.GetPlugin(SingletonScript<NpcBleedEffectPluginDef>.Instance.PluginKey) as NpcBleedEffectPlugin;
			double afterSecond = plugin != null ? plugin.EffectPower : -1;

			Console.WriteLine("BLEED_STACK_TEST: afterFirst=" + afterFirst + " afterSecond=" + afterSecond +
				" stacked=" + (afterSecond > afterFirst));
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
			NpcBleedEffectPlugin plugin = dummy.GetPlugin(SingletonScript<NpcBleedEffectPluginDef>.Instance.PluginKey) as NpcBleedEffectPlugin;
			bool appliedBeforeTick = plugin != null;

			if (plugin != null) {
				plugin.On_FadingEffectTick();
			}
			bool stillBleedingAfterTick = dummy.GetPlugin(SingletonScript<NpcBleedEffectPluginDef>.Instance.PluginKey) != null;

			Console.WriteLine("BLEED_RESIST_TEST: appliedBeforeTick=" + appliedBeforeTick +
				" stillBleedingAfterTick=" + stillBleedingAfterTick);
		}
	}
}
