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
	/// GM-tunable settings for NPCAiTimer's autonomous-aggro distances - shown as an "NPC
	/// Aggro" category in the .settings dialog, same pattern as LogoutSettings/
	/// NotorietySettings. Both fields replace what used to be hardcoded `const int`s on
	/// NPCAiTimer itself (AggroRange=8, GiveUpRange=12) - defaults bumped to 14/18 per
	/// explicit user request.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class NpcAggroSettings : SettingsMetaCategory {

		[SavedMember]
		public static NpcAggroSettings instance = new NpcAggroSettings();

		[LoadingInitializer]
		public NpcAggroSettings() {
		}

		/// <summary>Tiles an NPC scans (Map.GetCharsInRange, checked once per 1.5s AI tick) to
		/// autonomously notice and pick a target (NPCAiTimer.FindTarget) - a candidate still
		/// needs NPC.ShouldAggroOn and real line-of-sight to actually trigger. Also the radius
		/// AlertGuardiansOf uses to find nearby guardians when their ward is attacked.</summary>
		[SaveableData]
		public int aggroRange = 14;

		/// <summary>Tiles a target can get from an already-engaged NPC before it's dropped and
		/// the NPC goes back to wandering (NPCAiTimer.IsValidTarget) - covers a target fleeing,
		/// teleporting, or just outrunning the NPC.</summary>
		[SaveableData]
		public int giveUpRange = 18;

		/// <summary>#768 - an NPC that gets attacked fights back (Sphere's OnHarmedBy), unless it's
		/// already fighting someone. Off = only autonomously aggressive NPCs ever fight.</summary>
		[SaveableData]
		public bool npcsDefendThemselves = true;

		/// <summary>#768 - pets following their master attack whatever the master attacks, like
		/// Sphere's following pets. Off = only "guard"/"kill" orders send pets into a fight.</summary>
		[SaveableData]
		public bool petsAssistOwner = true;

		/// <summary>#826 - calling the guards ("guard"/"straz" near a guard, Erebor's spk_guard) sends them after
		/// whoever the caller fought with: someone who attacked the caller, or whom the caller attacked, at
		/// most this many seconds ago (Sphere's fight memory).</summary>
		[SaveableData]
		public int guardCallMemorySeconds = 120;

		/// <summary>#1033 - an NPC heals itself under this percent of its hits, and only when the heal
		/// wouldn't be wasted. Above about 90 it looks like healing at full health.</summary>
		[SaveableData]
		public int npcHealBelowPercent = 75;

		/// <summary>#1033 - creatures an NPC keeps summoned at once.</summary>
		[SaveableData]
		public int npcMaxSummons = 2;

		/// <summary>#1100 - Erebor's i_attack_guard: once a second a guard fighting a player runs at them, this
		/// many steps at most. 0 turns the pursuit off.</summary>
		[SaveableData]
		public int guardPursuitSteps = 8;

		/// <summary>#1100 - a player further than this from a guard isn't run at.</summary>
		[SaveableData]
		public int guardPursuitRange = 20;

		/// <summary>#1100 - a pursuing guard further than this from its spawn teleports home.</summary>
		[SaveableData]
		public int guardLeavesForHomeAt = 70;

		/// <summary>#1100 - e_guard's @skillstroke: a guard heading home from further than this teleports there.</summary>
		[SaveableData]
		public int guardTeleportsHomeFrom = 7;
	}
}
