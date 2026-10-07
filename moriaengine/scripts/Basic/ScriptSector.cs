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
using SteamEngine.Communication;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>Sector-defining class used in various scripts</summary>
	[ViewableClass]
	public class ScriptSector {
		/// <summary>
		/// Dictionary mapping the computed SectorKey to the ScriptSector 
		/// we use computed X and Y (byte shift) and the normal M for determining the ScriptSector 
		/// </summary>
		internal static Dictionary<SectorKey, ScriptSector> scriptSectors = new Dictionary<SectorKey, ScriptSector>();

		/// <summary>Power of 2 determining the size of the sector i.e. size 5 means a rectangle 32*32 fields.</summary>
		private const int scriptSectorSize = 5;

		public static readonly TimeSpan cleaningPeriod = TimeSpan.FromMinutes(3);

		/// <summary>
		/// Time for how long we allow all information to remain in the ScriptSector 
		/// (if not refreshed) - e.g. player's TrackPoints etc.
		/// </summary>
		public static readonly TimeSpan maxEntityAge = TimeSpan.FromMinutes(3);

		/// <summary>
		/// The dictionary containing all characters that passed through this sector. Everyone leaves an information 
		/// about the fields he stepped onto. For character Tracking purposes.
		/// </summary>
		private Dictionary<Character, TrackPoint.LinkedQueue> charsPassing = new Dictionary<Character, TrackPoint.LinkedQueue>();

		private ScriptSectorTimer sectorTimer;

		/// <summary>
		/// Computed SEctorKey determining a set of map fields that belong to this sector - 
		///  see the GetScriptSector method
		///  </summary>
		private SectorKey sectorIdentifier;

		private ScriptSector(SectorKey sectorIdentifier) {
			this.sectorIdentifier = sectorIdentifier;
			this.sectorTimer = new ScriptSectorTimer(this);
			this.sectorTimer.DueInSpan = cleaningPeriod;//set the first timeout for checking
			this.sectorTimer.PeriodSpan = cleaningPeriod;//set the period for periodic checking
		}

		/// <summary>Perform all check necesarry to do on timeout</summary>
		internal void CheckOnTimeout() {
			//check and clean all old TrackPoints left by passing characters
			TimeSpan minServerTime = Globals.TimeAsSpan - maxEntityAge;

			List<Character> charsToRemove = null;
			foreach (KeyValuePair<Character, TrackPoint.LinkedQueue> pair in this.charsPassing) {
				Character oneChar = pair.Key;
				TrackPoint.LinkedQueue charsPath = pair.Value;

				TrackPoint nextPoint = charsPath.Oldest;
				TrackPoint newQueueStart = nextPoint;

				while (nextPoint != null) {
					if (nextPoint.CreatedAt <= minServerTime) {//check the oldest Queue element
						nextPoint.Dispose();
						nextPoint = nextPoint.OlderNeighbor;
						newQueueStart = nextPoint;
					} else {
						break;
					}
				}
				if (nextPoint != newQueueStart) {
					if (newQueueStart == null) {//nothing is left in the queue
						if (charsToRemove == null) {
							charsToRemove = new List<Character>();
						}
						charsToRemove.Add(oneChar); //we will remove the char later
					} else {
						charsPath.SliceOldest(newQueueStart);
					}
				}
			}
			if (charsToRemove != null) {
				foreach (Character charToRemove in charsToRemove) {
					this.charsPassing.Remove(charToRemove);
				}
			}

			//check if the sector contains any information, otherwise remove it from the global list
			if (this.charsPassing.Count == 0) {
				this.Remove();
			}
		}

		private void Remove() {
			//delete the timer
			this.sectorTimer.Delete();
			//and remove the sector from the overall dictionary
			scriptSectors.Remove(this.sectorIdentifier);
		}

		/// <summary>For the given map Point4D compute and return the corresponding ScriptSector for further purposes</summary>
		public static ScriptSector GetScriptSector(IPoint4D forPoint) {
			SectorKey determiningPoint = new SectorKey((ushort) (forPoint.X >> scriptSectorSize), (ushort) (forPoint.Y >> scriptSectorSize), forPoint.M);
			ScriptSector retSector;
			if (!scriptSectors.TryGetValue(determiningPoint, out retSector)) {
				retSector = new ScriptSector(determiningPoint);//create new
				scriptSectors.Add(determiningPoint, retSector); //and store it in the dictionary
			}
			return retSector;
		}

		/// <summary>
		/// Method stolen from the Map class. Find all ScriptSectors that intersect the given 
		/// Rectangle (can be ScriptRectangle etc) lying in the given mapplane
		/// </summary>
		public static IEnumerable<ScriptSector> GetScriptSectorsInRectangle(AbstractRectangle rectangle, byte mapplane) {
			//get first and last computed ScriptSector point for the given rectangle
			//(i.e. the sectors where the top left and bottom right rectangle points lies)
			int ssMinX = rectangle.MinX >> scriptSectorSize;
			int ssMinY = rectangle.MinY >> scriptSectorSize;
			int ssMaxX = rectangle.MaxX >> scriptSectorSize;
			int ssMaxY = rectangle.MaxY >> scriptSectorSize;

			//check all computed sector identifiers if some ScriptSector exists for them and if so, return it
			for (int sx = ssMinX; sx <= ssMaxX; sx++) {
				for (int sy = ssMinY; sy <= ssMaxY; sy++) {
					ScriptSector oneSector;
					if (scriptSectors.TryGetValue(new SectorKey(sx, sy, mapplane), out oneSector)) {
						yield return oneSector;
					}
				}
			}
		}

		/// <summary>
		/// Get all ScriptSectors that intersect with the given rectangle (lying in the give mapplane), then check all 
		/// characters (typically Players) contained inside if they also belong to the rectangle, check if the 
		/// character in the rect. is of the desired type and if its footsteps are not too old. 
		/// Return the list of found characters.
		/// </summary>
		public static List<AbstractCharacter> GetCharactersInRectangle(AbstractRectangle rect, TimeSpan now, TimeSpan maxAge, byte mapplane) {
			List<AbstractCharacter> retChars = new List<AbstractCharacter>();
			TimeSpan minServTime = now - maxAge;
			foreach (ScriptSector sector in GetScriptSectorsInRectangle(rect, mapplane)) {
				foreach (KeyValuePair<Character, TrackPoint.LinkedQueue> pair in sector.charsPassing) {
					Character candidate = pair.Key;
					if (retChars.Contains(candidate)) {//if  already added from another sector, don't bother.
						break;
					}
					TrackPoint.LinkedQueue queue = pair.Value;
					foreach (TrackPoint tp in queue.EnumerateFromOldest()) {
						if (tp.CreatedAt < minServTime) {
							break; //the queue is sorted by creation time, once we reach one tp that is too old, all the rest are too old.
						}
						if (rect.Contains(tp.Location)) {
							retChars.Add(candidate);
							break;//one point is enough, this character can be tracked
						}
					}
				}
			}
			return retChars;
		}

		/// <summary>
		/// For the given character, get the set of all his footsteps belonging to the given rectangle in the given mapplane 
		/// and which are not older than specified.
		/// </summary>
		public static IEnumerable<TrackPoint> GetCharsPath(Character whose, AbstractRectangle rect, TimeSpan now, TimeSpan maxAge, byte mapplane) {
			TimeSpan minTimeToYield = now - maxAge; //newer than this are returned
			TimeSpan minTimeToStay = now - maxEntityAge; //older than this are deleted

			Dictionary<Point4D, TrackPoint> uniqueFootsteps = new Dictionary<Point4D, TrackPoint>();
			foreach (ScriptSector relevantSec in GetScriptSectorsInRectangle(rect, mapplane)) {
				TrackPoint.LinkedQueue charPoints;
				if (relevantSec.charsPassing.TryGetValue(whose, out charPoints)) {//only if the char is still loaded on sector (i.e. it was not yet cleaned)
					TrackPoint next = charPoints.Newest;
					if (next == null) { //empty queue
						continue;
					}
					do {
						TrackPoint tp = next;
						next = tp.OlderNeighbor;

						Point4D loc = tp.Location;
						if (!rect.Contains(tp.Location)) { //not interesting
						} else {
							TimeSpan tpCreatedAt = tp.CreatedAt;
							if (tpCreatedAt > minTimeToYield) { //the footprint is not too old
								TrackPoint previousInDict;
								if (uniqueFootsteps.TryGetValue(loc, out previousInDict)) {
									if (previousInDict.CreatedAt < tpCreatedAt) { //previous is older. Since we start from the newest, and sector overlapping doesnt happen, this shouldn't happen either. 
										//previousInDict.Queue.Remove(previousInDict);
										//uniqueFootsteps[loc] = tp;
										throw new SEException("previousInDict.CreatedAt < tpCreatedAt. This should not happen.");
									} //this one is older. 
									charPoints.RemoveAndDispose(tp);
								} else {
									uniqueFootsteps.Add(loc, tp);
								}
							} else if (tpCreatedAt < minTimeToStay) {
								charPoints.RemoveAndDispose(tp);
							}
						}
					} while (next != null);
				}
			}
			return uniqueFootsteps.Values;
		}

		public static IEnumerable<TrackPoint> GetTrackPointsOn(Player trackedChar, IPoint4D point) {
			SectorKey key = new SectorKey((ushort) (point.X >> scriptSectorSize), (ushort) (point.Y >> scriptSectorSize), point.M);
			ScriptSector sector;
			if (scriptSectors.TryGetValue(key, out sector)) {
				TrackPoint.LinkedQueue queue;
				if (sector.charsPassing.TryGetValue(trackedChar, out queue)) {
					foreach (TrackPoint tp in queue.EnumerateFromOldest()) {
						if (Point4D.Equals(point, tp.Location)) {
							yield return tp;
						}
					}
				}
			}
		}

		/// <summary>For the given player make a record of his actual position as a new tracking step</summary>
		internal static void AddTrackingStep(Player whose, Direction direction) {
			//get actual sector
			ScriptSector hisSector = GetScriptSector(whose);

			//check if we already have this any of char's trackpoints in this sector
			TrackPoint.LinkedQueue sectorTPQueue;
			if (!hisSector.charsPassing.TryGetValue(whose, out sectorTPQueue)) {
				sectorTPQueue = new TrackPoint.LinkedQueue();
				hisSector.charsPassing.Add(whose, sectorTPQueue);
			}

			ushort model;
			switch (direction) {
				case Direction.North://0
				case Direction.NorthEast://1
					model = (ushort) GumpIDs.Footprint_North; //0x1e04
					break;
				case Direction.East://2
				case Direction.SouthEast://3
					model = (ushort) GumpIDs.Footprint_East; //0x1e05
					break;
				case Direction.South://4
				case Direction.SouthWest://5
					model = (ushort) GumpIDs.Footprint_South; //0x1e06
					break;
				case Direction.West://6
				case Direction.NorthWest://7
					model = (ushort) GumpIDs.Footprint_West; //0x1e03
					break;
				default:
					throw new SEException("Invalid direction");
			}

			//add the actually stepped point to the queue (no matter if we have stepped on it previously)
			TrackPoint tp = new TrackPoint(whose, whose, model);

			//check if we are being tracked and in this case, send the information about the new step made
			List<Character> tbList = (List<Character>) whose.GetTag(TrackingSkillDef.trackedByTK);
			if (tbList != null) {
				if (tbList.Count > 0) {
					TryRefreshPoint(tbList, tp);
				} else {
					whose.RemoveTag(TrackingSkillDef.trackedByTK);
				}
			}
			sectorTPQueue.AddNewest(tp);
		}

		private static void TryRefreshPoint(List<Character> trackers, TrackPoint newTP) {
			PacketGroup removingPacket = null;
			bool needRemovePointsChecked = true;
			PacketGroup addingPacket = null;
			Player trackedChar = newTP.Owner;
			Point4D newTPLocation = newTP.Location;

			foreach (Character tracker in trackers) {
				GameState trackerState = tracker.GameState;
				if (trackerState != null) {
					PlayerTrackingPlugin trackersPlugin = PlayerTrackingPlugin.GetInstalledPlugin(tracker);
					if ((trackersPlugin != null) && (trackersPlugin.IsObservingPoint(newTPLocation))) {
						if (needRemovePointsChecked) {
							foreach (TrackPoint tp in GetTrackPointsOn(trackedChar, newTPLocation)) {
								if (removingPacket == null) {
									removingPacket = PacketGroup.AcquireMultiUsePG();
								}
								removingPacket.AcquirePacket<DeleteObjectOutPacket>().Prepare(tp.FakeUID);
							}
							needRemovePointsChecked = false;
						}
						if (removingPacket != null) {
							trackerState.Conn.SendPacketGroup(removingPacket);
						}
						if (addingPacket == null) {
							addingPacket = PacketGroup.AcquireMultiUsePG();
							addingPacket.AcquirePacket<ObjectInfoOutPacket>().PrepareFakeItem(newTP.FakeUID, newTP.Model, newTPLocation, 1, Direction.North,
								newTP.GetColor(Globals.TimeAsSpan, trackersPlugin.MaxFootstepAge)); //+1 necessary?
						}
						trackerState.Conn.SendPacketGroup(addingPacket);
					}
				}
			}

			if (removingPacket != null) {
				removingPacket.Dispose();
			}
			if (addingPacket != null) {
				addingPacket.Dispose();
			}
		}

		[InfoField("Sector size")]
		public static int ScriptSectorSize {
			get {
				return scriptSectorSize;
			}
		}

		public SectorKey Identifier {
			get {
				return this.sectorIdentifier;
			}
		}

		private class ScriptSectorTimer : Timer {
			ScriptSector ownerSector;

			internal ScriptSectorTimer(ScriptSector ownerSector) {
				this.ownerSector = ownerSector;
			}

			protected override void OnTimeout() {
				if ((this.ownerSector != null) && (this == this.ownerSector.sectorTimer)) { //sector exists and I'm it's timer
					this.ownerSector.CheckOnTimeout();
				}
			}
		}
	}

	/// <summary>
	/// Special immutable key for determining ScriptSectors. It is specified by computed X, Y (byteshifted position) 
	/// and the mapplane it lies in
	/// </summary>
	public struct SectorKey {
		private readonly int x, y;
		private readonly byte m;

		public SectorKey(int x, int y, byte m) {
			this.x = x;
			this.y = y;
			this.m = m;
		}

		public int X {
			get {
				return this.x;
			}
		}

		public int Y {
			get {
				return this.y;
			}
		}

		public byte M {
			get {
				return this.m;
			}
		}

		public override bool Equals(object obj) {
			if (obj is SectorKey) {
				SectorKey sk = (SectorKey) obj;
				return ((this.x == sk.x) && (this.y == sk.y) && (this.m == sk.m));
			}
			return false;
		}

		//stolen from PointXD
		public override int GetHashCode() {
			return ((37 * 17 ^ this.x) ^ this.y) ^ this.m;
		}
	}
}
