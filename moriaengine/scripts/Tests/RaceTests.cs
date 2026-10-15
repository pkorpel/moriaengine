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
	/// <summary>Server-side helpers for the races system protocol tests (#161-165).</summary>
	public static class RaceTests {
		/// <summary>Spawns a real i_stone_raceclass item on the ground at self's own feet and
		/// reports its uid, so a protocol test can double-click it for real (#174) rather than
		/// invoking the flow through a test-only command.</summary>
		[SteamFunction]
		public static void TestSpawnRaceStone(Character self) {
			ItemDef stoneDef = (ItemDef) ThingDef.GetByDefname("i_stone_raceclass");
			AbstractItem stone = (AbstractItem) stoneDef.Create(self.P());
			Console.WriteLine("RACE_STONE_SPAWNED: uid=" + stone.Uid);
		}

		[SteamFunction]
		public static void TestCheckRaceState(Character self) {
			Player player = (Player) self;
			Point4D home = player.Home;
			string homeStr = (home != null) ? (home.X + "," + home.Y + "," + home.Z) : "none";
			Console.WriteLine("RACE_STATE: race=" + player.Race +
				" nation=" + player.Nation +
				" realm=" + player.Realm +
				" home=" + homeStr +
				" title=" + player.Title +
				" str=" + player.Str + " dex=" + player.Dex + " int=" + player.Int + " vit=" + player.Vit);
		}

		/// <summary>Direct server-side check of Notoriety's realm-based rules (#163) - creates
		/// two throwaway NPCs (any Character works, Notoriety's checks don't require IsPlayer),
		/// sets Gondor on one and Mordor on the other, and reports GetHighlightColor/
		/// GetCharRelation both ways. Avoids any packet/proximity complexity entirely - this is
		/// a pure logic check, same spirit as LevelStatBonus's own direct-call test pattern.</summary>
		[SteamFunction]
		public static void TestCheckRealmNotoriety(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			Character gondor = (Character) npcDef.Create(self.P());
			Character mordor = (Character) npcDef.Create(self.P());
			Character gondor2 = (Character) npcDef.Create(self.P());
			gondor.Realm = Realm.Gondor;
			mordor.Realm = Realm.Mordor;
			gondor2.Realm = Realm.Gondor;

			Console.WriteLine("REALM_NOTORIETY: " +
				"mordorSeenByGondor=" + Notoriety.GetHighlightColor(gondor, mordor) +
				" gondorSeenByMordor=" + Notoriety.GetHighlightColor(mordor, gondor) +
				" crossRelation=" + Notoriety.GetCharRelation(gondor, mordor) +
				" sameRealmRelation=" + Notoriety.GetCharRelation(gondor, gondor2));

			gondor.Delete();
			mordor.Delete();
			gondor2.Delete();
		}
	}
}
