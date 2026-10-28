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
	/// #791 - GM-tunable monster loot (.settings -> "Loot"), defaults from Erebor's creaturestart
	/// (systems/spawns.scp). See MonsterLoot.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class LootSettings : SettingsMetaCategory {

		[SavedMember]
		public static LootSettings instance = new LootSettings();

		[LoadingInitializer]
		public LootSettings() {
		}

		/// <summary>A monster's gold, in permille of its experience reward, outside dungeons.</summary>
		[SaveableData]
		public int goldPermilleSurface = 750;

		/// <summary>The same in dungeons (map planes 2 and 10-15) - Erebor's "+20% gold in dungeons".</summary>
		[SaveableData]
		public int goldPermilleDungeon = 900;

		/// <summary>No magic-item chance at or below this much gold.</summary>
		[SaveableData]
		public int magicItemMinGold = 30;

		/// <summary>A magic item drops when the gold beats a roll of {magicItemMinGold .. this} -
		/// roughly a (gold - 30) / 120000 chance.</summary>
		[SaveableData]
		public int magicItemRollMax = 120000;

		/// <summary>The roll's upper end from magicItemRichGold gold up - a better chance for rich monsters.</summary>
		[SaveableData]
		public int magicItemRollMaxRich = 105000;

		[SaveableData]
		public int magicItemRichGold = 500;

		/// <summary>Gold-scaled rare extras (elemental coal, ebony crystal/bowstring, treasure map).</summary>
		[SaveableData]
		public bool rareExtras = true;
	}
}
