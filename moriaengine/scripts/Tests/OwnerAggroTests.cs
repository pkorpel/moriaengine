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
	/// Server-side setup/inspection helpers for the owner-attack and "rebel" protocol
	/// scenarios driven by Tests/Protocol/OwnerAggroTests.cs. Uses c_titan (AggroMode.Hostile,
	/// see VerificationTestUtils' own remarks) as the test creature.
	/// </summary>
	public static class OwnerAggroTests {
		/// <summary>A wild Hostile-mode monster directed via "come" (FollowTarget set,
		/// ControlMaster deliberately left null) - the exact state PetCommands.cs's "come" verb
		/// produces for an unowned NPC a GM is temporarily commanding.</summary>
		[SteamFunction]
		public static void TestSetupHostileFollower(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			titan.Name = "TestHostileFollower";
			titan.FollowTarget = self;
			titan.ShouldFollowOwner = true;
			Console.WriteLine("DIAG: setup self=" + self.Name + "#" + self.Uid + " titan.FollowTarget readback=" +
				(titan.FollowTarget != null ? titan.FollowTarget.Name + "#" + titan.FollowTarget.Uid : "null") +
				" sameRef=" + ReferenceEquals(titan.FollowTarget, self));
			Console.WriteLine("OWNER_AGGRO_SETUP: STARTED (uid=" + titan.Uid + ") ownerHitsBaseline=" + self.Hits);
		}

		/// <summary>A real tamed pet (ControlMaster set) for the "rebel" scenario.</summary>
		[SteamFunction]
		public static void TestSetupOwnedHostilePet(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef titanDef = (NPCDef) ThingDef.GetByDefname("c_titan");
			NPC titan = (NPC) titanDef.Create(self.X + 1, self.Y, self.Z, self.M);
			titan.Name = "TestOwnedPet";
			titan.ControlMaster = self;
			Console.WriteLine("OWNER_AGGRO_SETUP: STARTED (uid=" + titan.Uid + ") ownerHitsBaseline=" + self.Hits);
		}

		[SteamFunction]
		public static void TestCheckOwnerAggroState(Character self, ScriptArgs sa) {
			int uid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC npc = (NPC) Thing.UidGetCharacter(uid);
			NPCAiTimer timer = NPCAiTimer.GetFor(npc);
			Character target = (timer != null) ? timer.Target : null;
			Console.WriteLine("OWNER_AGGRO_CHECK: target=" + (target != null ? target.Name : "none") +
				" ownerHits=" + self.Hits +
				" controlMaster=" + (npc.ControlMaster != null ? npc.ControlMaster.Name : "none") +
				" followTarget=" + (npc.FollowTarget != null ? npc.FollowTarget.Name : "none"));
		}

		/// <summary>Directly invokes DamageManager.CauseDamage (self attacking its own pet) -
		/// mirrors InvulTests.TestApplyDamage's reasoning for going straight to the damage
		/// choke point rather than simulating a full weapon swing.</summary>
		[SteamFunction]
		public static void TestOwnerAttacksPet(Character self, ScriptArgs sa) {
			int uid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC npc = (NPC) Thing.UidGetCharacter(uid);
			int actualDamage = DamageManager.CauseDamage(self, npc, DamageType.PhysicalBlunt, 15);
			Console.WriteLine("OWNER_ATTACK: actualDamage=" + actualDamage + " petHits=" + npc.Hits);
		}
	}
}
