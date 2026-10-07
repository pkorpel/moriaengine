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
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class PlayerTrackingPlugin {
		public static readonly PlayerTrackingPluginDef defInstance = new PlayerTrackingPluginDef("p_tracking", "C#scripts", -1);
		private static readonly PluginKey trackingPluginKey = PluginKey.Acquire("_tracking_");

		public const int refreshTimeout = 5; //number of seconds after which all displayed footsteps will be refreshed (if necessary)

		private ImmutableRectangle trackingRectangle;
		private TimeSpan lastRefreshAt;

		internal static void InstallOnChar(Character trackingChar, Character trackedChar, ImmutableRectangle playerRect, TimeSpan maxAge, int safeSteps) {
			trackingChar.DeletePlugin(trackingPluginKey);
			PlayerTrackingPlugin tpl = (PlayerTrackingPlugin) defInstance.Create();
			tpl.Init(trackedChar, playerRect, maxAge, safeSteps);
			trackingChar.AddPlugin(trackingPluginKey, tpl);
		}

		internal static void UninstallPlugin(Character self) {
			self.DeletePlugin(trackingPluginKey);
		}

		private void Init(Character trackedChar, ImmutableRectangle rectangle, TimeSpan maxAge, int safeSteps) {
			this.trackedChar = trackedChar;
			this.safeSteps = safeSteps;
			this.trackingRectangle = rectangle;
			this.maxFootstepAge = maxAge;
			this.rectWidth = (ushort) rectangle.Width;
			this.Timer = refreshTimeout;//set the first timer
		}

		public static PlayerTrackingPlugin GetInstalledPlugin(Character self) {
			return (PlayerTrackingPlugin) self.GetPlugin(trackingPluginKey);
		}

		public bool IsObservingPoint(IPoint4D point) {
			Character cont = (Character) this.Cont;
			if (cont.M == point.M) {
				return this.trackingRectangle.Contains(point);
			}
			return false;
		}

		public TimeSpan MaxFootstepAge {
			get {
				return this.maxFootstepAge;
			}
		}

		public int SafeSteps {
			get {
				return this.safeSteps;
			}
		}

		public Character TrackedChar {
			get {
				return this.trackedChar;
			}
		}

		public int StepsCounter {
			get {
				return this.stepsCntr;
			}
		}

		public int RectangleWidth {
			get {
				return this.rectWidth;
			}
		}

		public ImmutableRectangle TrackingRectangle {
			get {
				return this.trackingRectangle;
			}
		}

		public TimeSpan LastRefreshAt {
			get {
				return this.lastRefreshAt;
			}
		}

		//Get all available TrackPoints and send a fake item packet to the Cont about it
		private void SendAllVisibleFootsteps() {
			//lower bound of the footsteps visibility-age (upper bound is Globals.TimeAsSpan)
			//the footsteps LastStepTime must lie between these two bounds for the footprint to be visible
			//the maxFootstepAge is computed from the tracker's skill
			Character tracker = (Character) this.Cont;
			GameState state = tracker.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;
				this.lastRefreshAt = Globals.TimeAsSpan;
				this.trackingRectangle = new ImmutableRectangle(tracker, (ushort) (this.rectWidth / 2));
				foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, this.trackingRectangle, this.lastRefreshAt, this.maxFootstepAge, tracker.M)) {
					this.SendObjectPacket(this.lastRefreshAt, conn, tp);
				}
			}
		}

		private void RefreshVisibleFootsteps() {
			if ((this.lastRefreshAt == TimeSpan.Zero) || this.trackingRectangle == null) {
				this.SendAllVisibleFootsteps();
				return;
			}

			Character tracker = (Character) this.Cont;
			GameState state = tracker.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;
				TimeSpan now = Globals.TimeAsSpan;
				TimeSpan sinceLastRefresh = now - this.lastRefreshAt;
				TimeSpan totalMaxAge = this.maxFootstepAge + sinceLastRefresh; //those > maxFootstepAge will get removed
				foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, this.trackingRectangle, now, totalMaxAge, tracker.M)) {
					this.SendRefreshObject(now, conn, tp);
				}
				this.lastRefreshAt = now;
			}
		}

		//remove all visible points from view
		private void SendDeleteAllVisibleFootsteps(Character tracker) {
			if ((this.lastRefreshAt == TimeSpan.Zero) || this.trackingRectangle == null) {
				return;
			}

			GameState state = tracker.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;

				foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, this.trackingRectangle, Globals.TimeAsSpan, this.maxFootstepAge, tracker.M)) {
					SendDeletePacket(conn, tp);
				}
				this.lastRefreshAt = TimeSpan.Zero;
				this.trackingRectangle = null;
			}
		}

		public void On_Assign() {
			//display all footsteps to the player
			this.RefreshVisibleFootsteps();
			this.stepsCntr = this.safeSteps; //set the counter

			Character cont = (Character) this.Cont;
			//to the tracked char's list add the actual tracker
			List<Character> tbList = (List<Character>) this.trackedChar.GetTag(TrackingSkillDef.trackedByTK);
			if (tbList == null) {
				tbList = new List<Character>();
				this.trackedChar.SetTag(TrackingSkillDef.trackedByTK, tbList);
			}
			if (!tbList.Contains(cont)) {
				tbList.Add(cont);
			}
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ClilocSysMessage(502989);//Tracking failed.

			this.SendDeleteAllVisibleFootsteps(formerCont);
			//remove from the trackedBy list on the tracked character
			List<Character> tbList = (List<Character>) this.trackedChar.GetTag(TrackingSkillDef.trackedByTK);
			tbList.Remove(formerCont);
			if (tbList.Count == 0) {
				this.trackedChar.RemoveTag(TrackingSkillDef.trackedByTK);
			}
		}

		public void On_NewPosition(Point4D oldP) {
			Player tracker = (Player) this.Cont;
			//check the steps counter
			this.stepsCntr--;
			if (this.stepsCntr == 0)
			{
//force another check of tracking success
				if (!SkillDef.GetBySkillName(SkillName.Tracking).CheckSuccess(tracker, Globals.dice.Next(700))) { //the same success check as in On_Stroke phase
					this.Delete();
					return;
				}
				this.stepsCntr = this.safeSteps; //reset the counter
			}

			ImmutableRectangle oldRect = this.trackingRectangle;
			ImmutableRectangle newRect = new ImmutableRectangle(tracker, (ushort) (this.rectWidth / 2));
			this.trackingRectangle = newRect;
			int dist = Point2D.GetSimpleDistance(oldRect.MinX, oldRect.MinY, newRect.MinX, newRect.MinY);
			TimeSpan now = Globals.TimeAsSpan;

			GameState state = tracker.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;
				if (dist > this.rectWidth) {//old and new have no intersection. We treat them separately, and only send delete packets in the area still visible to client
					ImmutableRectangle updateRect = new ImmutableRectangle(tracker, state.UpdateRange);
					ImmutableRectangle oldRectVisible = ImmutableRectangle.GetIntersection(oldRect, updateRect);
					foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, oldRectVisible, now, this.maxFootstepAge, tracker.M)) {
						SendDeletePacket(conn, tp);
					}
					foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, newRect, now, this.maxFootstepAge, tracker.M)) {
						this.SendObjectPacket(now, conn, tp);
					}
				} else {
					int bigRectMinX = Math.Min(oldRect.MinX, newRect.MinX);
					int bigRectMinY = Math.Min(oldRect.MinY, newRect.MinY);
					int bigRectMaxX = Math.Max(oldRect.MaxX, newRect.MaxX);
					int bigRectMaxY = Math.Max(oldRect.MaxY, newRect.MaxY);
					ImmutableRectangle bigRect = new ImmutableRectangle(bigRectMinX, bigRectMinY, bigRectMaxX, bigRectMaxY);

					TimeSpan sinceLastRefresh = now - this.lastRefreshAt;
					TimeSpan totalMaxAge = this.maxFootstepAge + sinceLastRefresh; //those > maxFootstepAge will get removed

					foreach (TrackPoint tp in ScriptSector.GetCharsPath(this.trackedChar, bigRect, now, totalMaxAge, tracker.M)) {
						Point4D loc = tp.Location;
						if (newRect.Contains(loc)) {
							if (oldRect.Contains(loc)) {//intersection. We only refresh colors/remove too old
								this.SendRefreshObject(now, conn, tp);
							} else { //new Footstep. We send it.
								this.SendObjectPacket(now, conn, tp);
							}
						} else if (oldRect.Contains(loc)) {
							if (newRect.Contains(loc)) {//intersection. We only refresh colors/remove too old
								this.SendRefreshObject(now, conn, tp);
							} else { //footstep out of tracking range. We send delete packet. (or do we?)
								SendDeletePacket(conn, tp);
							}
						}
					}
				}

				this.lastRefreshAt = now;
			}
		}

		private void SendObjectPacket(TimeSpan now, TcpConnection<GameState> conn, TrackPoint tp) {
			int color = tp.GetColor(now, this.maxFootstepAge);
			ObjectInfoOutPacket oiop = Pool<ObjectInfoOutPacket>.Acquire();
			oiop.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, color);
			conn.SendSinglePacket(oiop);
		}

		private void SendRefreshObject(TimeSpan now, TcpConnection<GameState> conn, TrackPoint tp) {
			TimeSpan minTimeToShow = now - this.maxFootstepAge;
			TimeSpan tpCreatedAt = tp.CreatedAt;
			if (tpCreatedAt >= minTimeToShow) {
				int oldColor = tp.GetColor(this.lastRefreshAt, this.maxFootstepAge);
				int newColor = tp.GetColor(now, this.maxFootstepAge);
				if (oldColor != newColor) {
					ObjectInfoOutPacket oiop = Pool<ObjectInfoOutPacket>.Acquire();
					oiop.PrepareFakeItem(tp.FakeUID, tp.Model, tp.Location, 1, Direction.North, newColor);
					conn.SendSinglePacket(oiop);
				}
			} else {
				SendDeletePacket(conn, tp);
			}
		}

		private static void SendDeletePacket(TcpConnection<GameState> conn, TrackPoint tp) {
			DeleteObjectOutPacket doop = Pool<DeleteObjectOutPacket>.Acquire();
			doop.Prepare(tp.FakeUID);
			conn.SendSinglePacket(doop);
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			this.Delete();
		}

		public void On_Timer() {
			if (!this.IsDeleted && this.Cont != null) {
				this.RefreshVisibleFootsteps(); //force to recompute the displayed footsteps color and send the necessary refresh packets
				this.Timer = refreshTimeout;
			}
		}
	}

	[ViewableClass]
	public partial class PlayerTrackingPluginDef {
	}

	[ViewableClass]
	public partial class NPCTrackingPlugin {
		public static readonly NPCTrackingPluginDef defInstance = new NPCTrackingPluginDef("p_NPCtracking", "C#scripts", -1);
		internal static PluginKey npcTrackingPluginKey = PluginKey.Acquire("_NPCtracking_");
		internal static int refreshTimeout = 10; //number of seconds after which all displayed footsteps will be refreshed (if necessary)

		private int stepsCntr;

		private void CheckDistance(Player tracker) {
			//check the distance to the tracked target
			int currentDist = Point2D.GetSimpleDistance(tracker, this.whoToTrack);
			if (currentDist > this.maxAllowedDist) {
				this.Delete();
			}
		}

		public void On_Step(ScriptArgs args) {//1st arg = direction (byte), 2nd arg = running (bool)
			Player tracker = (Player) this.Cont;
			//check the steps counter
			this.stepsCntr--;
			if (this.stepsCntr == 0)
			{
//force another check of tracking success
				if (!SkillDef.GetBySkillName(SkillName.Tracking).CheckSuccess(tracker, Globals.dice.Next(700))) { //the same success check as in On_Stroke phase
					this.Delete();
					return;
				}
				this.stepsCntr = this.safeSteps; //reset the counter
			}

			//now check the arrow
			this.CheckDistance(tracker);
		}

		public void On_Assign() {
			//send the QuestArrow displaying packet
			((Player) this.Cont).QuestArrow(true, this.whoToTrack);
			this.stepsCntr = this.safeSteps; //set the counter
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ClilocSysMessage(502989);//Tracking failed

			//send the QuestArrow removal packet
			((Player) formerCont).QuestArrow(false, formerCont);
		}

		public void On_Timer() {
			this.CheckDistance((Player) this.Cont); //force check the distance to the target
			this.Timer = PlayerTrackingPlugin.refreshTimeout;
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class NPCTrackingPluginDef {
	}
}