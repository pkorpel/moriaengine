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

using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// GM-tunable rules for walking onto a tile another character stands on (#767) - shown as
	/// a "Movement" category in the .settings dialog, same pattern as NpcAggroSettings. See
	/// CharacterCollision for where these are applied.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class CharCollisionSettings : SettingsMetaCategory {

		[SavedMember]
		public static CharCollisionSettings instance = new CharCollisionSettings();

		[LoadingInitializer]
		public CharCollisionSettings() {
		}

		/// <summary>Players can only shove past another character at full stamina - the same
		/// rule the UO client enforces locally (and TazUO's "ignore stamina check" option
		/// skips). Off = plain Sphere 0.99z8 server rule: stamina just has to exceed the cost.</summary>
		[SaveableData]
		public bool requireFullStamina = true;

		/// <summary>Stamina a successful shove costs. 0 = Sphere's formula, log2(obstacle STR)+1
		/// (STR 50 -> 6, STR 100 -> 7, STR 200 -> 8); anything above 0 is a flat cost.</summary>
		[SaveableData]
		public int flatPushCost = 0;

		/// <summary>Off = players walk through other characters freely (the pre-#767 behavior).</summary>
		[SaveableData]
		public bool playersBlockedByCharacters = true;

		/// <summary>NPCs (pets and summons included) never shove - any living character on the
		/// tile stops them, as in Sphere. Off = NPCs walk through other characters freely.</summary>
		[SaveableData]
		public bool npcsBlockedByCharacters = true;

		/// <summary>#1031 - tiles around an NPC where characters count as obstacles when it plans a
		/// path. Further away they're ignored, they'll have moved by the time it gets there.</summary>
		[SaveableData]
		public int npcPathCharacterRadius = 8;

		/// <summary>#1031 - seconds an NPC keeps finding no way past a character before it looks
		/// for a long way round. The wait lets a pack in a corridor sort itself out.</summary>
		[SaveableData]
		public double npcDetourWaitSeconds = 2;

		/// <summary>#1031 - seconds between two long searches of one NPC, they're the costly kind.</summary>
		[SaveableData]
		public double npcDetourRetrySeconds = 10;

		/// <summary>#1031 - tiles beyond the NPC and its target the long search may wander.</summary>
		[SaveableData]
		public int npcDetourSearchMargin = 64;

		/// <summary>#1031 - nodes the long search may expand before giving up.</summary>
		[SaveableData]
		public int npcDetourMaxIterations = 4000;

		/// <summary>#1199 - seconds an NPC doesn't search again, from about the same place, toward a spot it found
		/// no way to; it steps straight at it or stands meanwhile. 0 = no back-off.</summary>
		[SaveableData]
		public double npcFailedPathRetrySeconds = 8;

		/// <summary>#779 - players at 0 stamina, or carrying more than maxLoadPercent of what they can
		/// carry (STR*4+30 stones), can't move, and walking costs stamina depending on the load
		/// (Sphere 0.99z8's CheckMoveWalkDir / Calc_DropStamWhileMoving). Off = none of that.</summary>
		[SaveableData]
		public bool fatigueAndOverload = true;

		/// <summary>#779 - load (percent of max carry weight) beyond which a player can't move at all.</summary>
		[SaveableData]
		public int maxLoadPercent = 200;

		/// <summary>#779 - load percent at which each step costs 1 stamina half the time; the chance
		/// falls off (rises) along an S-curve below (above) it. Sphere's STAMINALOSSATWEIGHT - Erebor
		/// ran 150.</summary>
		[SaveableData]
		public int staminaLossAtWeight = 150;

		/// <summary>#779 - percent added to the load while running (Sphere's RUNNINGPENALTY, 50).</summary>
		[SaveableData]
		public int runningStaminaPenalty = 50;
	}
}
