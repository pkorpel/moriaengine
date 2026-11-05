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

using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1201 - wakes the NPCs that fell asleep where no player was near (NPCAiTimer.TryFallAsleep) as soon as
	/// Regions.ActivityGrid says one is near again: every sleeper in an area that woke, and a sleeper moved into an
	/// awake area (a GM's summon, a teleport). The hooks run in the middle of a sector change: they only resume timers.
	/// </summary>
	public static class NpcSleep {
		public static void Bootstrap() {
			ActivityGrid.AreaWoke = OnAreaWoke;
			ActivityGrid.CharacterChangedArea = OnCharacterChangedArea;
		}

		private static void OnAreaWoke(Map map, int ax, int ay) {
			int x = ax << ActivityGrid.AreaShift;
			int y = ay << ActivityGrid.AreaShift;
			ImmutableRectangle area = new ImmutableRectangle(x, y, x + ActivityGrid.AreaSize - 1, y + ActivityGrid.AreaSize - 1);
			foreach (AbstractCharacter ch in map.GetNPCsInRectangle(area)) {
				NPC npc = ch as NPC;
				if (npc != null) {
					NPCAiTimer.WakeUp(npc);
				}
			}
		}

		private static void OnCharacterChangedArea(AbstractCharacter ch) {
			NPC npc = ch as NPC;
			if (npc == null) {
				return;
			}
			NPCAiTimer timer = NPCAiTimer.GetFor(npc);
			if ((timer != null) && timer.IsSleeping && npc.GetMap().Activity.IsAwake(npc.X, npc.Y, Globals.TimeInSeconds)) {
				timer.WakeUp();
			}
		}
	}
}
