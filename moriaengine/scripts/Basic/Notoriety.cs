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

using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	public static class Notoriety {
		private static readonly TagKey murderCountTK = TagKey.Acquire("_murderCount_");
		private static readonly TimerKey criminalTimerTK = TimerKey.Acquire("_criminalTimer_");

		/// <summary>Classic UO threshold at which a player's title turns from innocent to "murderer" (red).</summary>
		public const int MurdererThreshold = 5;

		private static double criminalFlagSeconds = 120;

		/// <summary>How long a criminal flag lasts after a single hostile act, in seconds - the
		/// real 2-minute classic-UO default. Settable (mirrors LightAndWeather.DayLengthSeconds's
		/// own pattern) so a verification test can shorten it to confirm the flag actually expires
		/// without waiting out 2 real minutes.</summary>
		public static double CriminalFlagSeconds {
			get { return criminalFlagSeconds; }
			set { criminalFlagSeconds = value; }
		}

		public static int GetMurderCount(Character self) {
			object tag = self.GetTag(murderCountTK);
			if (tag is int count) {
				return count;
			}
			return 0;
		}

		public static bool IsMurderer(Character self) {
			return GetMurderCount(self) >= MurdererThreshold;
		}

		public static bool IsCriminal(Character self) {
			return self.HasTimer(criminalTimerTK);
		}

		public static void FlagCriminal(Character self) {
			if (!IsMurderer(self)) { //murderers are already the worst notoriety, flagging them criminal too would add nothing
				self.AddTimer(criminalTimerTK, new CriminalExpiryTimer()).DueInSeconds = CriminalFlagSeconds;
			}
		}

		/// <summary>Called whenever `enemy` commits a hostile act against `victim` - flags `enemy` as a criminal unless the act was justified (victim already criminal/murderer, or a declared guild war/party fight).</summary>
		public static void OnHostileAction(Character victim, Character enemy) {
			if ((victim == null) || (enemy == null) || (victim == enemy)) {
				return;
			}
			if (!victim.IsPlayer || !enemy.IsPlayer) {
				return; //no AI/monster notoriety yet (Phase 16)
			}
			if (IsCriminal(victim) || IsMurderer(victim)) {
				return; //victim is already fair game, attacking them is not a crime
			}
			if (Party.AreInOneParty(victim, enemy)) {
				return;
			}
			Guild victimGuild = Guild.GetGuild(victim);
			Guild enemyGuild = Guild.GetGuild(enemy);
			if ((victimGuild != null) && (victimGuild == enemyGuild)) {
				return; //guildmates
			}
			if ((victimGuild != null) && (enemyGuild != null) && Guild.AreInWar(victimGuild, enemyGuild)) {
				return; //declared guild war - legal pvp
			}
			FlagCriminal(enemy);
		}

		/// <summary>Called when `victim` dies at the hands of `killedBy` - registers a murder count against `killedBy`, unless killing `victim` was justified (victim was already criminal/murderer).</summary>
		public static void RegisterMurder(Character victim, Character killedBy) {
			if ((victim == null) || (killedBy == null) || (victim == killedBy)) {
				return;
			}
			if (!victim.IsPlayer || !killedBy.IsPlayer) {
				return;
			}
			if (IsCriminal(victim) || IsMurderer(victim)) {
				return; //killing a criminal/murderer is not a murder
			}
			killedBy.SetTag(murderCountTK, GetMurderCount(killedBy) + 1);
		}

		public static HighlightColor GetHighlightColor(Character viewer, Character self) {
			if (viewer == self) {
				return HighlightColor.NoColor;
			}
			if (IsMurderer(self)) {
				return HighlightColor.Murderer;
			}
			if (IsCriminal(self)) {
				return HighlightColor.Criminal;
			}
			if (Party.AreInOneParty(viewer, self)) {
				return HighlightColor.Yellow;
			}
			Guild viewerGuild = Guild.GetGuild(viewer);
			if (viewerGuild != null) {
				Guild myGuild = Guild.GetGuild(self);
				if (myGuild == viewerGuild) {
					return HighlightColor.Allied;
				}
				if (myGuild != null)
				{
					if (Guild.AreAllied(myGuild, viewerGuild)) {
						return HighlightColor.Allied;
					}
					if (Guild.AreInWar(myGuild, viewerGuild)) {
						return HighlightColor.Attackable;
					}
				}
			}

			//TODO realms, etc.
			return HighlightColor.NoColor;
		}

		public static CharRelation GetCharRelation(Character a, Character b) {
			if (a == b) {
				return CharRelation.Allied;
			}
			if (IsCriminal(b) || IsMurderer(b)) {
				return CharRelation.TempHostile;
			}
			if (Party.AreInOneParty(a, b)) {
				return CharRelation.Allied;
			}
			Guild guildA = Guild.GetGuild(a);
			Guild guildB = Guild.GetGuild(b);
			if ((guildA != null) && (guildA == guildB)) {
				return CharRelation.Allied;
			}
			if ((guildA != null) && (guildB != null)) {
				if (Guild.AreAllied(guildA, guildB)) {
					return CharRelation.Friendly;
				}
				if (Guild.AreInWar(guildA, guildB)) {
					return CharRelation.AlwaysHostile;
				}
			}
			return CharRelation.Unknown;
		}

		/// <summary>Region protection enforcement (#24) - true if `attacker` should be barred from initiating an attack on `target` here. Either party standing in a safe/guarded/no-pvp region blocks it, unless that region is also flagged as an arena.</summary>
		public static bool IsAttackBlockedByRegion(Character attacker, AbstractCharacter target) {
			if (RegionBarsCombat(attacker.Region as FlaggedRegion)) {
				return true;
			}
			Character targetChar = target as Character;
			if ((targetChar != null) && RegionBarsCombat(targetChar.Region as FlaggedRegion)) {
				return true;
			}
			return false;
		}

		private static bool RegionBarsCombat(FlaggedRegion region) {
			if (region == null) {
				return false;
			}
			if (region.Flag_Arena) {
				return false; //arenas explicitly override safe/guarded/nopvp
			}
			return region.Flag_Safe || region.Flag_Guarded || region.Flag_NoPvp;
		}

		/// <summary>True if `self` should be exempt from the leveling system's death-experience-loss
		/// (#128) - safe zones and arenas, matching the user's own stated design (deliberately a
		/// narrower set of flags than RegionBarsCombat above - a guarded town isn't meant to grant
		/// exp-loss immunity, only true safe zones and arenas are).</summary>
		public static bool IsExemptFromExperienceLoss(Character self) {
			FlaggedRegion region = self.Region as FlaggedRegion;
			if (region == null) {
				return false;
			}
			return region.Flag_Safe || region.Flag_Arena;
		}
	}

	[SaveableClass]
	public class CriminalExpiryTimer : BoundTimer {
		[LoadingInitializer]
		public CriminalExpiryTimer() {
		}

		protected sealed override void OnTimeout(TagHolder cont) {
		}
	}
}
