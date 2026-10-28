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
	/// #794 - GM-tunable elite monsters (.settings -> "Elite Monsters"), defaults from Erebor's
	/// systems/events_npc/EliteNPC/EliteNPC.scp. See EliteMonster.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class EliteSettings : SettingsMetaCategory {

		[SavedMember]
		public static EliteSettings instance = new EliteSettings();

		[LoadingInitializer]
		public EliteSettings() {
		}

		/// <summary>Spawned monsters can turn elite at all.</summary>
		[SaveableData]
		public bool enabled = true;

		/// <summary>Only monsters worth at least this much experience can turn elite.</summary>
		[SaveableData]
		public int minExperience = 1000;

		/// <summary>The chance, per 10000, is experience * this / 100 ...</summary>
		[SaveableData]
		public int spawnChancePercentOfExperience = 20;

		/// <summary>... capped at this (500 = 5%).</summary>
		[SaveableData]
		public int maxSpawnChance = 500;

		/// <summary>Elite power (permille): basePower at minExperience, +powerPer10000Exp/10000
		/// per experience above it, up to maxPower. It's also the share of a player's swing
		/// damage the elite absorbs.</summary>
		[SaveableData]
		public int basePower = 500;

		[SaveableData]
		public int powerPer10000Exp = 2667;

		[SaveableData]
		public int maxPower = 900;

		/// <summary>Str/Dex/Int gain, percent.</summary>
		[SaveableData]
		public int statBonusPercent = 25;

		/// <summary>The elite's own swing damage, permille (1200 = +20%).</summary>
		[SaveableData]
		public int swingDamagePermille = 1200;

		/// <summary>Healed every regenIntervalSeconds while hurt: power / regenPowerDivisor hit points.</summary>
		[SaveableData]
		public int regenPowerDivisor = 7;

		[SaveableData]
		public double regenIntervalSeconds = 2;

		/// <summary>Extra swing damage vs summoned creatures, percent: power / summonKillerPowerDivisor.</summary>
		[SaveableData]
		public int summonKillerPowerDivisor = 3;

		/// <summary>Poison resist is raised to at least this, and to at least the power.</summary>
		[SaveableData]
		public int minResistPoison = 700;

		[SaveableData]
		public int minResistSummon = 970;

		/// <summary>Experience bonus, percent: expBonusBasePercent + (power - basePower) / 2.</summary>
		[SaveableData]
		public int expBonusBasePercent = 300;

		/// <summary>Reagents, gems and diamonds scaled by the power, put in the backpack.</summary>
		[SaveableData]
		public bool lootBundle = true;

		/// <summary>An elite that has met a player leaves after this long, if still alive.</summary>
		[SaveableData]
		public double escapeAfterSeconds = 1200;

		/// <summary>Its gate spell takes this long; a hit interrupts it ...</summary>
		[SaveableData]
		public double escapeCastSeconds = 3;

		/// <summary>... and it tries again this much later.</summary>
		[SaveableData]
		public double escapeRetrySeconds = 5;

		/// <summary>Players from this level up absorb less of their damage into the elite:
		/// levelBonusBase + (level - levelBonusMinLevel) * levelBonusPerLevel permille, the level
		/// counted up to levelBonusMaxLevel. Erebor's values - its levels went that high, ours
		/// don't (60 is about the effective max), so as shipped nobody gets the bonus.</summary>
		[SaveableData]
		public int levelBonusMinLevel = 200;

		[SaveableData]
		public int levelBonusMaxLevel = 400;

		[SaveableData]
		public int levelBonusBase = 200;

		[SaveableData]
		public int levelBonusPerLevel = 5;
	}
}
