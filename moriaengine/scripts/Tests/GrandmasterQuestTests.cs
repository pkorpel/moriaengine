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
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - server-side helpers for Tests/Protocol/GrandmasterQuestTests.cs.</summary>
	public static class GrandmasterQuestTests {
		private static readonly TagKey upsetTillTK = TagKey.Acquire("_questNpcUpsetTill_");

		private static Thing Find(string defname, int plane, ImmutableRectangle area) {
			ThingDef def = ThingDef.GetByDefname(defname);
			foreach (Thing t in Map.GetMap(plane).GetThingsInRectangle(area)) {
				if (t.Def == def) {
					return t;
				}
			}
			return null;
		}

		private static ImmutableRectangle Around(Point4D p, int range) {
			return new ImmutableRectangle(p.X - range, p.Y - range, p.X + range, p.Y + range);
		}

		private static Point4D RegionP(string defname) {
			return StaticRegion.GetByDefname(defname).P;
		}

		private static ImmutableRectangle Cave() {
			return new ImmutableRectangle(5200, 1512, 5297, 1600);
		}

		private static Thing Target(string name) {
			ImmutableRectangle plains = new ImmutableRectangle(3970, 495, 4125, 545); //the east of the Ice Plains
			Point4D tirith = RegionP("a_Minas_Tirith");
			switch (name) {
				case "elqira": return Find(GrandmasterQuest.ElqiraDef, 0, new ImmutableRectangle(2760, 2755, 2775, 2770));
				case "elvira": return Find(GrandmasterQuest.ElviraDef, 0, plains);
				case "elmira": return Find(GrandmasterQuest.ElmiraDef, 0, plains);
				case "door": return Find(GrandmasterQuest.CageDoorDef, 0, plains);
				case "entrance": return Find(GrandmasterQuest.CaveEntranceDef, 0, plains);
				case "exit": return Find(GrandmasterQuest.CaveExitDef, RegionP(GrandmasterQuest.CaveRegion).M, Cave());
				case "rinald": return Find(GrandmasterQuest.RinaldDef, RegionP(GrandmasterQuest.CaveRegion).M, Cave());
				case "root": return Find(GrandmasterQuest.RootDef, RegionP(GrandmasterQuest.RootCaveRegion).M, GrandmasterQuest.RootCave);
				case "gateIn": return Find(DungeonEntrancesSetup.GateDef, 0, new ImmutableRectangle(2826, 744, 2846, 762));
				case "gateOut": return Find(DungeonEntrancesSetup.GateDef, RegionP(GrandmasterQuest.RootCaveRegion).M, GrandmasterQuest.RootCave);
				case "envoy": return Find(GrandmasterQuest.GondorEnvoyDef, tirith.M, Around(tirith, 10));
				default: throw new SEException("no such target " + name);
			}
		}

		private static string Arg(ScriptArgs sa) {
			return sa.Args.Trim().Trim('"');
		}

		/// <summary>Args (one string): "stage rank level realm".</summary>
		[SteamFunction]
		public static void TestQ000Prepare(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] args = Arg(sa).Split(' ');
			GrandmasterQuest.SetStage(player, int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture));
			player.SkillRank = (SkillRank) int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
			player.Level = int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
			player.Realm = (Realm) Enum.Parse(typeof(Realm), args[3]);
			Console.WriteLine("Q000_PREPARED: done");
		}

		/// <summary>#837 - the envoy hint on Unas's and Borek's dialogs: offered to a hero at stage 11 only, and following it
		/// sets stage 12.</summary>
		[SteamFunction]
		public static void TestQ000Hint(Character self) {
			Player player = (Player) self;
			NPC unas = (NPC) ((NPCDef) ThingDef.GetByDefname(Q006.UnasDef)).Create(player.X + 30, player.Y, player.Z, player.M);
			NPC borek = (NPC) ((NPCDef) ThingDef.GetByDefname(Q041.BorekDef)).Create(player.X + 32, player.Y, player.Z, player.M);
			D_Q006Unas unasDialog = SingletonScript<D_Q006Unas>.Instance;
			D_Q041Borek borekDialog = SingletonScript<D_Q041Borek>.Instance;
			GrandmasterQuest.SetStage(player, 5);
			bool notTooEarly = !unasDialog.BuildFor(unas, player).HasResponse("q000_getVMDir") && !borekDialog.BuildFor(borek, player).HasResponse("q000_getVMDir");
			GrandmasterQuest.SetStage(player, 11);
			bool unasOffers = unasDialog.BuildFor(unas, player).HasResponse("q000_getVMDir") && unasDialog.BuildFor(unas, player).HasResponse(GrandmasterEnvoyHint.Button);
			bool borekOffers = borekDialog.BuildFor(borek, player).HasResponse("q000_getVMDir") && borekDialog.BuildFor(borek, player).HasResponse(GrandmasterEnvoyHint.Button);
			borekDialog.Press(GrandmasterEnvoyHint.Button, borek, player);
			int afterBorek = GrandmasterQuest.GetStage(player);
			GrandmasterQuest.SetStage(player, 11);
			unasDialog.Press(GrandmasterEnvoyHint.Button, unas, player);
			int afterUnas = GrandmasterQuest.GetStage(player);
			bool stillPromotable = GrandmasterQuest.CanBePromoted(player);
			unas.Delete();
			borek.Delete();
			Console.WriteLine("Q000_HINT: notTooEarly=" + notTooEarly + " unasOffers=" + unasOffers + " borekOffers=" + borekOffers +
				" afterBorek=" + afterBorek + " afterUnas=" + afterUnas + " stillPromotable=" + stillPromotable + " end");
		}

		private static readonly TagKey breathRinaldTK = TagKey.Acquire("_q000test_breathRinald_");

		/// <summary>#837 - Rinald three tiles south of the hero, facing them, turns into the daemon: his breath comes.</summary>
		[SteamFunction]
		public static void TestQ000Breath(Character self) {
			Player player = (Player) self;
			player.Hits = player.MaxHits;
			NPC rinald = (NPC) ((NPCDef) ThingDef.GetByDefname(GrandmasterQuest.RinaldDef)).Create(player.X, player.Y + 3, player.Z, player.M);
			NPCAiTimer.Freeze(rinald, 60);
			rinald.Direction = Direction.North;
			GrandmasterQuest.TransformRinald(rinald);
			player.SetTag(breathRinaldTK, rinald);
			Console.WriteLine("Q000_BREATH: hits=" + player.Hits + " tiles=" + RinaldBreath.CountLiveTiles() + " end");
		}

		[SteamFunction]
		public static void TestQ000BreathReport(Character self) {
			Player player = (Player) self;
			NPC rinald = player.GetTag(breathRinaldTK) as NPC;
			Console.WriteLine("Q000_BREATH_REPORT: lost=" + (player.MaxHits - player.Hits) + " rinaldUnhurt=" + ((rinald != null) && (rinald.Hits == rinald.MaxHits)) + " end");
			if (rinald != null) {
				rinald.Delete();
			}
			player.RemoveTag(breathRinaldTK);
			player.Hits = player.MaxHits;
		}

		/// <summary>#837 - the hero dies in Rinald's cave: where the body lies.</summary>
		[SteamFunction]
		public static void TestQ000Die(Character self) {
			Player player = (Player) self;
			Point4D back = player.GetTag(TagKey.Acquire("_q000CaveReturn_")) as Point4D;
			player.CauseDeath(null);
			Corpse corpse = null;
			foreach (Thing t in player.GetMap().GetThingsInRectangle(new ImmutableRectangle(player.X - 2, player.Y - 2, player.X + 2, player.Y + 2))) {
				corpse = (t as Corpse) ?? corpse;
			}
			bool inCave = corpse != null;
			if (back != null) {
				foreach (Thing t in Map.GetMap(back.M).GetThingsInRectangle(new ImmutableRectangle(back.X, back.Y, back.X, back.Y))) {
					corpse = (t as Corpse) ?? corpse;
				}
			}
			Console.WriteLine("Q000_DIED: back=" + (back != null) + " corpseLeftInCave=" + inCave + " corpseAtReturn=" + ((corpse != null) && (back != null) && (corpse.X == back.X) && (corpse.Y == back.Y)) + " end");
		}

		[SteamFunction]
		public static void TestQ000Resurrect(Character self) {
			self.Resurrect();
			Console.WriteLine("Q000_RESURRECTED: dead=" + self.Flag_Dead + " end");
		}

		/// <summary>Beside the thing, and its uid.</summary>
		[SteamFunction]
		public static void TestQ000GoTo(Character self, ScriptArgs sa) {
			Thing thing = Target(Arg(sa));
			int[] at = WorldSpots.WalkableRing(thing.GetMap(), thing, self.MovementSettings, 1)[0];
			self.Go(at[0], at[1], at[2], thing.M);
			Console.WriteLine("Q000_AT: uid=" + thing.Uid + " los=" + self.CanSeeLOS(thing).Allow);
		}

		/// <summary>The way a player gets through a gate: from where the other gate lands them, on real steps along the path
		/// to it. Arriving by teleport wouldn't trigger it.</summary>
		[SteamFunction]
		public static void TestQ000WalkOnto(Character self, ScriptArgs sa) {
			string name = Arg(sa);
			Thing gate = Target(name);
			Point4D start = RegionP((name == "gateIn") ? "a_Ered_Mithrin_vstup" : GrandmasterQuest.RootCaveRegion);
			self.Go(start.X, start.Y, start.Z, start.M);
			AStar.Options options = new AStar.Options { AllowPartial = false, StopWithin = 0 };
			AStar.Result path = AStar.FindPathOnMap(start, gate, gate.GetMap(), self.MovementSettings, options);
			string steps = "";
			if ((path != null) && (path.Path != null)) {
				foreach (Direction dir in path.Path) {
					self.Direction = dir;
					bool walked = self.WalkRunOrFly(dir, false, true);
					steps += " " + dir + "=" + walked + "(" + self.X + "," + self.Y + "," + self.Z + ")";
				}
			}
			Console.WriteLine("Q000_WALKED: from=" + start + " gate=" + gate.X + "," + gate.Y + "," + gate.Z + " m=" + self.M + " region=" + RegionEventSystem.AreaOf(self.Region).Defname + " steps=" + steps.Trim().Replace(' ', '/'));
		}

		[SteamFunction]
		public static void TestQ000StepOn(Character self, ScriptArgs sa) {
			Thing thing = Target(Arg(sa));
			self.Go(thing.X, thing.Y, thing.Z, thing.M);
			Console.WriteLine("Q000_STEPPED: done");
		}

		/// <summary>Args (one string): "dialog button" - the id of that exit button.</summary>
		[SteamFunction]
		public static void TestQ000Button(Character self, ScriptArgs sa) {
			string[] args = Arg(sa).Split(' ');
			QuestDialogDef dialog;
			switch (args[0]) {
				case "elqira": dialog = SingletonScript<D_Q000Elqira>.Instance; break;
				case "elmira": dialog = SingletonScript<D_Q000Elmira>.Instance; break;
				case "elvira": dialog = SingletonScript<D_Q000Elvira>.Instance; break;
				case "rinald": dialog = SingletonScript<D_Q000Rinald>.Instance; break;
				default: dialog = SingletonScript<D_Q000Envoy>.Instance; break;
			}
			Console.WriteLine("Q000_BUTTON: id=" + dialog.ExitButtonId(args[1]));
		}

		[SteamFunction]
		public static void TestQ000Calm(Character self) {
			Target("elqira").RemoveTag(upsetTillTK);
			Console.WriteLine("Q000_CALM: done");
		}

		/// <summary>The guards around "root" or "rinald" dead.</summary>
		[SteamFunction]
		public static void TestQ000ClearGuards(Character self, ScriptArgs sa) {
			Thing guarded = Target(Arg(sa));
			List<NPC> guards = new List<NPC>();
			int range = GrandmasterQuest.GuardRange;
			foreach (Thing t in guarded.GetMap().GetThingsInRectangle(new ImmutableRectangle(guarded.X - range, guarded.Y - range, guarded.X + range, guarded.Y + range))) {
				NPC npc = t as NPC;
				if ((npc != null) && (npc != guarded) && (npc.AggroMode == AggroMode.Hostile)) {
					guards.Add(npc);
				}
			}
			foreach (NPC guard in guards) {
				guard.Delete();
			}
			Console.WriteLine("Q000_CLEARED: " + guards.Count);
		}

		[SteamFunction]
		public static void TestQ000KillRinald(Character self) {
			((NPC) Target("rinald")).CauseDeath(self);
			Console.WriteLine("Q000_RINALD_DEAD: done");
		}

		[SteamFunction]
		public static void TestQ000State(Character self) {
			Player player = (Player) self;
			Region region = player.Region;
			NPC rinald = (NPC) Target("rinald");
			Console.WriteLine("Q000_STATE: stage=" + GrandmasterQuest.GetStage(player) + " rank=" + player.SkillRank + " region=" + ((region == null) ? "none" : region.Defname) +
				" gold=" + GoldUtils.CountGoldIn(player.Backpack) + " bag=" + (ShamanAbilities.FindInPack(player, "i_mi_bag") != null) + " upset=" + QuestNpc.IsUpset((NPC) Target("elqira")) +
				" rinald=" + ((rinald == null) ? "none" : rinald.Name.Replace(' ', '_')) + " rinaldInvulnerable=" + ((rinald != null) && rinald.Flag_Invulnerable));
		}

		private static bool CanWalk(Character self, IPoint3D from, Thing to) {
			AStar.Options options = new AStar.Options { MaxIterations = 4000000, WindowMargin = 400, AllowPartial = false, StopWithin = 1 };
			AStar.Result path = AStar.FindPathOnMap(from, to, to.GetMap(), self.MovementSettings, options);
			return (path != null) && path.ReachedGoal;
		}

		/// <summary>The setup's pieces, and whether they can be walked to from their regions' spawn points.</summary>
		[SteamFunction]
		public static void TestQ000World(Character self) {
			Point4D spoust = RegionP("a_Severni_Spoust");
			Point4D cave = RegionP(GrandmasterQuest.CaveRegion);
			Thing root = Target("root");
			Console.WriteLine("Q000_WORLD: elvira=" + CanWalk(self, spoust, Target("elvira")) + " elmira=" + CanWalk(self, spoust, Target("elmira")) + " entrance=" + CanWalk(self, spoust, Target("entrance")) +
				" rinald=" + CanWalk(self, cave, Target("rinald")) + " exit=" + CanWalk(self, cave, Target("exit")) + " root=" + CanWalk(self, RegionP(GrandmasterQuest.RootCaveRegion), root) +
				" rootGuarded=" + GrandmasterQuest.AreGuardsAlive(root, null) + " rinaldGuarded=" + GrandmasterQuest.AreGuardsAlive(Target("rinald"), Target("rinald")) +
				" entranceInvul=" + ((NPC) Target("elmira")).Flag_Invulnerable + " envoy=" + (Target("envoy") != null) +
				" gateFromSurface=" + CanWalk(self, RegionP("a_Severni_Spoust"), Target("gateIn")) + " rootAt=" + root.X + "," + root.Y + "," + root.Z + " rootFromGate=" + Point2D.GetSimpleDistance(Target("gateOut"), root) + " sistersApart=" + Point2D.GetSimpleDistance(Target("elvira"), Target("elmira")) +
				" elvira=" + Target("elvira").X + "," + Target("elvira").Y + " elmira=" + Target("elmira").X + "," + Target("elmira").Y);
		}
	}
}
