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
				case "root": return Find(GrandmasterQuest.RootDef, 0, GrandmasterQuest.RootCave);
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

		/// <summary>Beside the thing, and its uid.</summary>
		[SteamFunction]
		public static void TestQ000GoTo(Character self, ScriptArgs sa) {
			Thing thing = Target(Arg(sa));
			int[] at = ExpertQuest.WalkableRing(thing.GetMap(), thing, self.MovementSettings, 1)[0];
			self.Go(at[0], at[1], at[2], thing.M);
			Console.WriteLine("Q000_AT: uid=" + thing.Uid + " los=" + self.CanSeeLOS(thing).Allow);
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
				" gold=" + GoldUtils.CountGoldIn(player.Backpack) + " bag=" + (MasterQuest.FindInBackpack(player, "i_mi_bag") != null) + " upset=" + QuestNpc.IsUpset((NPC) Target("elqira")) +
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
				" rinald=" + CanWalk(self, cave, Target("rinald")) + " exit=" + CanWalk(self, cave, Target("exit")) + " root=" + CanWalk(self, GrandmasterQuest.RootCaveMouth, root) +
				" rootGuarded=" + GrandmasterQuest.AreGuardsAlive(root, null) + " rinaldGuarded=" + GrandmasterQuest.AreGuardsAlive(Target("rinald"), Target("rinald")) +
				" entranceInvul=" + ((NPC) Target("elmira")).Flag_Invulnerable + " envoy=" + (Target("envoy") != null) +
				" rootFromSurface=" + CanWalk(self, RegionP("a_Severni_Spoust"), root) + " rootAt=" + root.X + "," + root.Y + "," + root.Z + " rootFromMouth=" + Point2D.GetSimpleDistance(GrandmasterQuest.RootCaveMouth.X, GrandmasterQuest.RootCaveMouth.Y, root.X, root.Y) + " sistersApart=" + Point2D.GetSimpleDistance(Target("elvira"), Target("elmira")) +
				" elvira=" + Target("elvira").X + "," + Target("elvira").Y + " elmira=" + Target("elmira").X + "," + Target("elmira").Y);
		}
	}
}
