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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#843 - server-side helpers for Tests/Protocol/WorldSetupTests.cs.</summary>
	public static class WorldSetupTests {
		private static readonly ImmutableRectangle eastPlains = new ImmutableRectangle(3970, 495, 4125, 545);
		private static int movedX, movedY, strayUid;

		private static int Count(string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int count = 0;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing.Def == def) && !thing.IsDeleted) {
					count++;
				}
			}
			return count;
		}

		private static Thing FindOne(string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			foreach (Thing thing in Thing.AllThings) {
				if ((thing.Def == def) && !thing.IsDeleted) {
					return thing;
				}
			}
			return null;
		}

		private static readonly TagKey idTK = TagKey.Acquire("_worldPieceId_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly string[] spawnedDefs = { AlchemistShop.NpcDef, ProdejceShop.NpcDef, MasterQuest.LenastirDef, GrandmasterQuest.ElqiraDef,
			GrandmasterQuest.ElmiraDef, GrandmasterQuest.ElviraDef, GrandmasterQuest.GondorEnvoyDef, GrandmasterQuest.MordorEnvoyDef };

		private static bool HasGem(Thing npc) {
			return (npc.GetTag(spawnGemTK) is Thing gem) && !gem.IsDeleted && (gem.GetTag(TagKey.Acquire("spawnInfo")) != null);
		}

		/// <summary>What a GM might do between two runs: moves Elvira, deletes the cage door, and leaves an unrelated item
		/// where a piece stands. A Lenastir's gem also loses its id, like a gem placed before there were ids.</summary>
		[SteamFunction]
		public static void TestWorldSetupEdit(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			int[] to = WorldSpots.NearestStandable(elvira.GetMap(), 3950, 505, 8); //far from her spot: she walks about while the setup runs
			movedX = to[0];
			movedY = to[1];
			((Character) elvira).Go(to[0], to[1], to[2], elvira.M);
			Thing door = FindOne(GrandmasterQuest.CageDoorDef);
			Thing stray = ThingDef.GetByDefname("i_q000_caveExit").Create(door.X, door.Y, door.Z, door.M);
			strayUid = stray.Uid;
			door.Delete();
			((Thing) FindOne(MasterQuest.LenastirDef).GetTag(spawnGemTK)).RemoveTag(idTK);
			((Item) FindOne(ExpertQuest.SignDef)).SetAttribute(ItemAttributes.attr_move_never, false); //as placed before item pieces were fixed
			Console.WriteLine("WORLD_EDIT: moved=" + to[0] + "," + to[1]);
		}

		/// <summary>Takes me beside a quest sign, and reports it.</summary>
		[SteamFunction]
		public static void TestWorldSetupGoToSign(Character self) {
			Thing sign = FindOne(ExpertQuest.SignDef);
			int[] at = WorldSpots.NearestStandable(sign.GetMap(), sign.X + 1, sign.Y, 3);
			self.Go(at[0], at[1], at[2], sign.M);
			ReportSign(self, sign);
		}

		[SteamFunction]
		public static void TestWorldSetupReportSign(Character self, ScriptArgs sa) {
			ReportSign(self, Thing.UidGetThing(int.Parse(sa.Args.Trim().Trim('"'))));
		}

		private static void ReportSign(Character self, Thing sign) {
			Item item = (Item) sign;
			Console.WriteLine("WORLD_SIGN: uid=" + sign.Uid + " at=" + sign.X + "," + sign.Y + " me=" + self.X + " " + self.Y + " " + self.Z + " onGround=" + (item.Cont == null) +
				" fixed=" + item.Flag_NonMovable + " decaying=" + ItemDecaySystem.HasActiveTimer(item));
		}

		//item pieces that a player could pick up, or that would decay away
		private static int LoosePieces() {
			int loose = 0;
			foreach (Thing thing in Thing.AllThings) {
				Item item = thing as Item;
				if ((item != null) && !item.IsDeleted && (item.GetTag(idTK) != null) && (!item.Flag_NonMovable || ItemDecaySystem.HasActiveTimer(item))) {
					loose++;
				}
			}
			return loose;
		}

		[SteamFunction]
		public static void TestWorldSetupCheck(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			Thing stray = Thing.UidGetThing(strayUid);
			Console.WriteLine("WORLD_CHECK: elviras=" + Count(GrandmasterQuest.ElviraDef) + " elviraMoved=" + (Point2D.GetSimpleDistance(elvira.X, elvira.Y, movedX, movedY) < 15) + " elviraAt=" + elvira.X + "," + elvira.Y +
				" loose=" + LoosePieces() + " doors=" + Count(GrandmasterQuest.CageDoorDef) + " strayThere=" + (stray != null) +
				" stones=" + Count(ExpertQuest.StartStoneDef) + " lenastirs=" + Count(MasterQuest.LenastirDef) + " rinalds=" + Count(GrandmasterQuest.RinaldDef) + " elqiras=" + Count(GrandmasterQuest.ElqiraDef));
			//the user's call: every lone NPC is a spawn of one, the piece its gem; only Rinald, whose life his quest runs, isn't
			int npcPieces = 0, bare = 0, shopsReady = 0, invulnerable = 0, spawned = 0;
			foreach (Thing thing in Thing.AllThings) {
				if (thing.IsDeleted) {
					continue;
				}
				if ((thing is NPC) && (thing.GetTag(idTK) != null)) {
					npcPieces++;
				}
				if ((thing is NPC npc) && (Array.IndexOf(spawnedDefs, npc.Def.PrettyDefname) >= 0)) {
					spawned++;
					if (!HasGem(npc)) {
						bare++;
					}
					if (npc.Flag_Invulnerable) {
						invulnerable++;
					}
					if (((npc.Def.PrettyDefname == AlchemistShop.NpcDef) || (npc.Def.PrettyDefname == ProdejceShop.NpcDef)) && (npc.Realm != Realm.None)) {
						shopsReady++;
					}
				}
			}
			Console.WriteLine("WORLD_SPAWNS: npcPieces=" + npcPieces + " spawned=" + spawned + " bare=" + bare + " allInvulnerable=" + (invulnerable == spawned) + " shopsReady=" + shopsReady + " end");
		}

		/// <summary>Elvira is gone: her gem spawns another, standing still and invulnerable, at once instead of in a minute or two.</summary>
		[SteamFunction]
		public static void TestWorldSetupRespawn(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			Thing gem = (Thing) elvira.GetTag(spawnGemTK);
			elvira.Delete();
			Timers.Timer timer = gem.GetTimer(Timers.TimerKey.Acquire("SpawnTimer"));
			if (timer != null) {
				timer.DueInSeconds = 0.5;
			}
			Console.WriteLine("WORLD_RESPAWN: gone=" + (Count(GrandmasterQuest.ElviraDef) == 0) + " timer=" + (timer != null) + " end");
		}

		[SteamFunction]
		public static void TestWorldSetupRespawned(Character self) {
			Thing elvira = FindOne(GrandmasterQuest.ElviraDef);
			Console.WriteLine("WORLD_RESPAWNED: elviras=" + Count(GrandmasterQuest.ElviraDef) + " fromGem=" + ((elvira != null) && HasGem(elvira)) +
				" invulnerable=" + ((elvira is Character ch) && ch.Flag_Invulnerable) + " still=" + ((elvira is NPC npc) && (NPCAiTimer.GetFor(npc).WanderLeash == 0)) + " end");
		}
	}
}
