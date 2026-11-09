using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1333 - the roads out of the towns (WorldEvents.RoutesOutOf) as saved: per walk's key, its roads, each a stretch of
	/// tiles past the town's edge, written "x,y,z;x,y,z|x,y,z;..." (roads by '|', tiles by ';').</summary>
	[SaveableClass]
	public sealed class RoadsOutState {
		[SaveableData]
		public List<string> keys = new List<string>(), roads = new List<string>();

		[LoadingInitializer]
		public RoadsOutState() {
		}
	}

	/// <summary>#1333 - keeps the roads out of the towns with the world (the user's pick): a big town's first walk out takes up to
	/// ~0.9 s on the game thread, so it's paid once per town for the world's life (by .SetupWorld, RoadsOutSetup), not after every
	/// restart nor while a player waits for an errand. A town not walked yet still walks on first use, and is saved from then.</summary>
	[HasSavedMembers]
	public static class RoadsOutStore {
		[SavedMember]
		private static RoadsOutState State {
			get { return Snapshot(); }
			set { Restore(value); }
		}

		internal static RoadsOutState Snapshot() {
			RoadsOutState s = new RoadsOutState();
			foreach (KeyValuePair<string, List<List<int[]>>> walk in WorldEvents.routesOut) {
				s.keys.Add(walk.Key);
				s.roads.Add(string.Join("|", walk.Value.ConvertAll(road => string.Join(";", road.ConvertAll(t =>
					t[0].ToString(CultureInfo.InvariantCulture) + "," + t[1].ToString(CultureInfo.InvariantCulture) + "," + t[2].ToString(CultureInfo.InvariantCulture))))));
			}
			return s;
		}

		internal static void Restore(RoadsOutState s) {
			if (s == null) {
				return;
			}
			int bad = 0;
			for (int i = 0; (i < s.keys.Count) && (i < s.roads.Count); i++) {
				try {
					List<List<int[]>> roads = new List<List<int[]>>();
					foreach (string road in s.roads[i].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
						roads.Add(new List<string>(road.Split(';')).ConvertAll(Tile));
					}
					WorldEvents.routesOut[s.keys[i]] = roads; //a town that can't be left keeps its empty list: no walk again either
				} catch (FatalException) {
					throw;
				} catch (Exception) {
					bad++; //a hand-edited or damaged entry: walked again on use
				}
			}
			Logger.WriteInfo(true, "Roads out of towns restored: " + WorldEvents.routesOut.Count + (bad > 0 ? (", " + bad + " unreadable") : "")); //once a boot
		}

		private static int[] Tile(string text) {
			int[] tile = Array.ConvertAll(text.Split(','), n => int.Parse(n, NumberStyles.Integer, CultureInfo.InvariantCulture));
			if (tile.Length != 3) {
				throw new FormatException("a road's tile is x,y,z: " + text);
			}
			return tile;
		}

		/// <summary>Forgets every road out of a town (after a town's area or the map changed): they're walked again on use, or by
		/// .SetupWorld.</summary>
		[SteamFunction]
		public static void ResetRoadsOut(Character self) {
			int forgotten = WorldEvents.routesOut.Count;
			WorldEvents.routesOut.Clear();
			self.SysMessage("Zapomenuto cest z mest: " + forgotten + ". Projdou se znovu pri pouziti nebo v .SetupWorld.");
		}
	}
}
