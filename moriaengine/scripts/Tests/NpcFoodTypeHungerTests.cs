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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup/report helpers for #593's protocol test
	/// (Tests/Protocol/NpcFoodTypeHungerTests.cs) - exercises NPCDef.FoodTypes/MaxFood
	/// parsing, PetHungerTimer's install/remove-on-ControlMaster, real decay/starvation
	/// (sped up via the timer's own public DueInSeconds rather than a dedicated test-only
	/// hook - no new infrastructure needed), and the real ".Feed" command/nakrmho formula.
	/// </summary>
	public static class NpcFoodTypeHungerTests {
		[SteamFunction]
		public static void TestSpawnHungerPet(Character self, ScriptArgs sa) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("npc_test_hunger");
			NPC pet = (NPC) def.Create(self.X, self.Y, self.Z, self.M);
			Console.WriteLine("HUNGER_TEST_SPAWNED: uid=" + pet.Uid + " maxFood=" + def.MaxFood);
		}

		[SteamFunction]
		public static void TestTameHungerPet(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC pet = Thing.UidGetThing(uid) as NPC;
			pet.ControlMaster = self;
			PetHungerTimer hunger = PetHungerTimer.GetFor(pet);
			Console.WriteLine("HUNGER_TEST_TAMED: installed=" + (hunger != null) + " food=" + (hunger != null ? hunger.food : -1));
		}

		[SteamFunction]
		public static void TestReleaseHungerPet(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC pet = Thing.UidGetThing(uid) as NPC;
			pet.ControlMaster = null;
			PetHungerTimer hunger = PetHungerTimer.GetFor(pet);
			Console.WriteLine("HUNGER_TEST_RELEASED: stillInstalled=" + (hunger != null));
		}

		[SteamFunction]
		public static void TestReportHunger(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC pet = Thing.UidGetThing(uid) as NPC;
			PetHungerTimer hunger = (pet != null) ? PetHungerTimer.GetFor(pet) : null;
			bool isDeleted = (pet == null) || pet.IsDeleted;
			Console.WriteLine("HUNGER_TEST_REPORT: uid=" + uid + " food=" + (hunger != null ? hunger.food : -1) +
				" isDeleted=" + isDeleted);
		}

		/// <summary>Drives exactly one hunger tick directly and synchronously (PetHungerTimer.
		/// Tick, the same logic OnTimeout calls after validating its NPC) instead of waiting
		/// out the real 1000s/3600s/60s cadences on the background timer thread - same
		/// "internal pure-enough helper, directly testable" precedent as
		/// NPCAiTimer.PickSpell.</summary>
		[SteamFunction]
		public static void TestForceHungerTick(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC pet = Thing.UidGetThing(uid) as NPC;
			PetHungerTimer hunger = PetHungerTimer.GetFor(pet);
			hunger.Tick(pet);
			Console.WriteLine("HUNGER_TEST_FORCED_TICK: uid=" + uid);
		}

		/// <summary>Jumps straight to the critical/near-starving state (food=2, countdown
		/// exhausted) without waiting through the real 10-check critical countdown.</summary>
		[SteamFunction]
		public static void TestSetHungerCritical(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC pet = Thing.UidGetThing(uid) as NPC;
			PetHungerTimer hunger = PetHungerTimer.GetFor(pet);
			hunger.food = 1;
			hunger.criticalCountdown = 0;
			Console.WriteLine("HUNGER_TEST_SET_CRITICAL: uid=" + uid);
		}

		[SteamFunction]
		public static void TestGiveRibsRaw(Character self, ScriptArgs sa) {
			ItemDef ribsDef = (ItemDef) ThingDef.GetByDefname("i_ribs_raw");
			Item ribs = (Item) ribsDef.Create(self.Backpack);
			ribs.Amount = 5;
			Console.WriteLine("HUNGER_TEST_GAVE_RIBS: amount=" + ribs.Amount);
		}
	}
}
