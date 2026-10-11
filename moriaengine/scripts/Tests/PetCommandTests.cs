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
	/// V-2 (verification pass, #70): server-side setup/inspection helpers for pet-command
	/// scenarios driven by real speech packets from Tests/Protocol/PetCommandTests.cs. Each
	/// "TestSetup*" function spawns the NPC(s) a scenario needs under a fixed, predictable name
	/// (so the protocol test can address them by name in a real "&lt;name&gt; &lt;verb&gt;" speech
	/// command, exactly like a player would); "TestCheckPetState" reads back the resulting state
	/// after the real PetCommands.cs speech handling ran, since most of that state (FollowTarget/
	/// ControlMaster/IsGuarding) isn't otherwise observable from outside the process.
	/// </summary>
	public static class PetCommandTests {
		/// <summary>
		/// Sets the account's plevel directly rather than toggling it (the real ".GM" command's
		/// own behavior) - every scenario in this fixture shares one account, so a test that
		/// toggled would leave its resulting state dependent on every earlier test's own history,
		/// not just its own setup. Deterministic regardless of xUnit's (unspecified) test order.
		/// Two zero-arg functions rather than one bool-parameterized one - SimpleCommandParser's
		/// single-argument binding for non-numeric text is one more thing to get wrong for no
		/// real benefit here.
		/// </summary>
		[SteamFunction]
		public static void TestSetGmOn(Character self) {
			AbstractAccount acc = self.Account;
			if (acc != null) {
				acc.PLevel = acc.MaxPLevel;
			}
			Console.WriteLine("GM_MODE_TEST: SET to GM (IsGM=" + self.IsGM + ")");
		}

		[SteamFunction]
		public static void TestSetGmOff(Character self) {
			AbstractAccount acc = self.Account;
			if (acc != null) {
				acc.PLevel = 1;
			}
			Console.WriteLine("GM_MODE_TEST: SET to player (IsGM=" + self.IsGM + ")");
		}

		[SteamFunction]
		public static void TestSetupOwnedPet(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC pet = (NPC) sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			pet.Name = "TestPet";
			pet.ControlMaster = self;
			Console.WriteLine("PET_SETUP: STARTED (owned pet #" + pet.Uid + ")");
		}

		[SteamFunction]
		public static void TestSetupTwoOwnedPets(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC pet1 = (NPC) sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			pet1.Name = "TestPetOne";
			pet1.ControlMaster = self;
			NPC pet2 = (NPC) sheepDef.Create(self.X + 2, self.Y, self.Z, self.M);
			pet2.Name = "TestPetTwo";
			pet2.ControlMaster = self;
			Console.WriteLine("PET_SETUP: STARTED (two owned pets #" + pet1.Uid + ", #" + pet2.Uid + ")");
		}

		/// <summary>An unowned, wild NPC - no ControlMaster set at all - for the "non-GM can't
		/// command what they don't own" and "GM can command anything" scenarios.</summary>
		[SteamFunction]
		public static void TestSetupWildNpc(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC wild = (NPC) sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			wild.Name = "TestWild";
			Console.WriteLine("PET_SETUP: STARTED (wild NPC #" + wild.Uid + ")");
		}

		/// <summary>A wild NPC plus a dummy victim adjacent to it, for the "GM-ordered kill on an
		/// unowned NPC actually lands damage" scenario.</summary>
		[SteamFunction]
		public static void TestSetupWildNpcAndVictim(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC wild = (NPC) sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			wild.Name = "TestWild";
			NPC victim = (NPC) sheepDef.Create(self.X + 2, self.Y, self.Z, self.M);
			victim.Name = "TestVictim";
			int startingHits = victim.Hits;
			// 30s, not the original 12s - matches CombatScenarioTests.cs/CombatDepthTests.cs's
			// own bump for the same reason: CI's slower/more contended runner needs more margin
			// for combat RNG (weapon-skill success retries every ~1s, not guaranteed quickly).
			// Attached to self, not victim - see CombatDepthTests.cs's TestSpellcasterDamage
			// comment: BoundTimer.Cont returns null once its TagHolder IsDeleted, so a check
			// attached directly to the (possibly-killed) victim would silently never fire in
			// exactly the case - the target dying - most likely to be a real pass.
			self.AddTimer(DamageCheckTimer.TimerKey, new DamageCheckTimer(victim, startingHits, "PET_KILL_TEST"))
				.DueInSeconds = 30;
			Console.WriteLine("PET_SETUP: STARTED (wild #" + wild.Uid + ", victim #" + victim.Uid + ")");
		}

		/// <summary>A pet already mid-fight (via direct EngageTarget, same as CombatScenarioTests.cs)
		/// for the "stop actually breaks off combat" scenario.</summary>
		[SteamFunction]
		public static void TestSetupFightingPet(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef sheepDef = ThingDef.GetByDefname("c_sheep_woolly") as NPCDef;
			NPC pet = (NPC) sheepDef.Create(self.X + 1, self.Y, self.Z, self.M);
			pet.Name = "TestPet";
			pet.ControlMaster = self;
			NPC victim = (NPC) sheepDef.Create(self.X + 2, self.Y, self.Z, self.M);
			victim.Name = "TestVictim";
			NPCAiTimer.GetFor(pet).EngageTarget(victim);
			Console.WriteLine("PET_SETUP: STARTED (fighting pet #" + pet.Uid + ")");
		}

		/// <summary>Reads back TestPet's current control-related state - the actual effect of
		/// whatever real speech pet-command just ran, not just whether it logged a message.</summary>
		[SteamFunction]
		public static void TestCheckPetState(Character self) {
			NPC pet = FindByName(self, "TestPet");
			if (pet == null) {
				Console.WriteLine("PET_STATE_CHECK: FAIL (TestPet not found)");
				return;
			}
			Console.WriteLine("PET_STATE_CHECK: ControlMaster=" + (pet.ControlMaster != null ? pet.ControlMaster.Name : "null") +
				" FollowTarget=" + (pet.FollowTarget != null ? pet.FollowTarget.Name : "null") +
				" IsGuarding=" + pet.IsGuarding +
				" PetGoalPoint=" + (pet.PetGoalPoint != null ? pet.PetGoalPoint.ToString() : "null"));
		}

		private static NPC FindByName(Character self, string name) {
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				NPC npc = ch as NPC;
				if ((npc != null) && string.Equals(npc.Name, name, StringComparison.OrdinalIgnoreCase)) {
					return npc;
				}
			}
			return null;
		}
	}
}
