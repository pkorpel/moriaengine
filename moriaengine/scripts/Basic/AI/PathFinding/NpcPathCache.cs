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
	/// #769 - remembers an NPC's last AStar path, so NPCAiTimer.StepTowardsSmart walks it instead
	/// of searching from scratch every think-tick. The next step is only handed out while the path
	/// still applies: the NPC is where the path expects it, the target hasn't moved more than
	/// TargetDrift tiles from where it was when the path was planned, and fewer than ReplanEvery
	/// steps were taken since. Anything else - or a step that fails - means plan again.
	/// #1031 - a detour around a blocked passage is walked to its end, unless the target moves
	/// DetourTargetDrift tiles: a replan halfway would lead back to the same blocked spot.
	/// #1199 - a search that found no way to its target is remembered: no new search toward a
	/// spot within FailedTargetDrift tiles of it until the back-off runs out. The partial way such a
	/// search found is set as a detour, walked to its end.
	/// </summary>
	public sealed class NpcPathCache {
		public const int TargetDrift = 2;
		public const int ReplanEvery = 8;
		public const int DetourTargetDrift = 8;
		public const int FailedTargetDrift = 4;

		private Direction[] path;
		private int index;
		private int expectedX, expectedY;
		private int plannedTargetX, plannedTargetY;
		private bool isDetour;

		private bool hasFailure;
		private int failedTargetX, failedTargetY;
		private double searchAgainAt;

		public bool HasPath {
			get { return (this.path != null) && (this.index < this.path.Length); }
		}

		public void SetPath(Direction[] newPath, int moverX, int moverY, int targetX, int targetY, bool detour = false) {
			this.path = newPath;
			this.isDetour = detour;
			this.index = 0;
			this.expectedX = moverX;
			this.expectedY = moverY;
			this.plannedTargetX = targetX;
			this.plannedTargetY = targetY;
		}

		public bool TryGetNextStep(int moverX, int moverY, int targetX, int targetY, out Direction dir) {
			dir = Direction.North;
			if (!this.HasPath || (!this.isDetour && (this.index >= ReplanEvery)) ||
					(moverX != this.expectedX) || (moverY != this.expectedY) ||
					(Math.Max(Math.Abs(targetX - this.plannedTargetX), Math.Abs(targetY - this.plannedTargetY)) > (this.isDetour ? DetourTargetDrift : TargetDrift))) {
				return false;
			}
			dir = this.path[this.index];
			return true;
		}

		/// <summary>The step handed out by TryGetNextStep was taken and the mover now stands at (x, y).</summary>
		public void Advance(int moverX, int moverY) {
			this.index++;
			this.expectedX = moverX;
			this.expectedY = moverY;
		}

		public void Invalidate() {
			this.path = null;
		}

		/// <summary>#1199 - a search toward (targetX, targetY) didn't reach it at time `now`.</summary>
		public void RememberFailedSearch(int targetX, int targetY, double now, double backOffSeconds) {
			this.hasFailure = true;
			this.failedTargetX = targetX;
			this.failedTargetY = targetY;
			this.searchAgainAt = now + backOffSeconds;
		}

		/// <summary>#1199 - true while a recent search toward about the same spot failed: don't search again yet.</summary>
		public bool IsBackingOff(int targetX, int targetY, double now) {
			return this.hasFailure && (now < this.searchAgainAt) &&
				(Math.Max(Math.Abs(targetX - this.failedTargetX), Math.Abs(targetY - this.failedTargetY)) <= FailedTargetDrift);
		}
	}
}
