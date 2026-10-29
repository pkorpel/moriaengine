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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#813 - the Erebor archive's most-used region events (sphereregion.scp's event-only
	/// [RegionType]s, named in each area's EVENTS=), ported to C#. Like Sphere, a character is in
	/// one area at a time - the innermost map area at its position (a house or ship region defers
	/// to the area under it) - and when that changes, the old area's events get @exit and the new
	/// one's @enter. Sphere's scripts attach a character event on enter (e_nofield, e_nohorse...)
	/// and remove it on exit; here those rules are checked against the character's current area
	/// when they matter (casting, stepping), which needs no saved state. GMs are exempt where the
	/// archive exempts them.
	///
	/// Ported: r_nosumon, r_playerhouse (its e_zabitivdome house-death log needs a "last murderer"
	/// we don't track - not ported), r_doly_nohorse, r_SafeZone, r_nofield, r_notele,
	/// r_nohorsehigh. Other event names stay on the areas as data until ported.</summary>
	public static class RegionEventSystem {
		public const string NoSummon = "r_nosumon";
		public const string PlayerHouse = "r_playerhouse";
		public const string MinesNoHorse = "r_doly_nohorse";
		public const string SafeZone = "r_safezone";
		public const string NoField = "r_nofield";
		public const string NoTeleport = "r_notele";
		public const string NoHorseHigh = "r_nohorsehigh";

		/// <summary>e_nofield: Wall of Stone, Fire Field, Poison Field, Paralyze Field, Energy Field.</summary>
		public static readonly int[] FieldSpellIds = { 24, 28, 39, 47, 50 };

		/// <summary>e_notele: Teleport.</summary>
		public const int TeleportSpellId = 22;

		/// <summary>e_nohorsehigh: riding is refused above this Z (the higher floors).</summary>
		public static int NoHorseHighMaxZ = 19;

		/// <summary>e_nohorsehigh: mounts allowed at any height - the ostards' mount items
		/// (i_mt_ostard_desert 0x3ea3, i_mt_ostard_zostrich 0x3ea4, i_mt_ostard_forest 0x3ea5; their
		/// itemdefs aren't converted, so by item id).</summary>
		public static readonly int[] HighMountItems = { 0x3ea3, 0x3ea4, 0x3ea5 };

		/// <summary>removeAgressiveSummon: summoned mounts are allowed anywhere.</summary>
		public static readonly string[] SummonedMountDefnames = { "mage_horse_brown", "necro_horse_undead" };

		/// <summary>The area a region belongs to for region events (and region resources): the
		/// innermost map area - a house/ship region sits inside a static area and defers to it.</summary>
		public static FlaggedRegion AreaOf(Region region) {
			while ((region != null) && !(region is FlaggedRegion)) {
				region = region.Parent;
			}
			return region as FlaggedRegion;
		}

		public static bool HasEvent(FlaggedRegion area, string eventName) {
			if (area != null) {
				foreach (string name in area.RegionEvents) {
					if (StringComparer.OrdinalIgnoreCase.Equals(name, eventName)) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Is ch's current area one with this event?</summary>
		public static bool IsIn(Character ch, string eventName) {
			return HasEvent(AreaOf(ch.Region), eventName);
		}

		/// <summary>From Character.On_RegionChanged: @exit of the old area's events, @enter of the new one's.</summary>
		public static void Transition(Character ch, Region oldRegion, Region newRegion) {
			FlaggedRegion oldArea = AreaOf(oldRegion);
			FlaggedRegion newArea = AreaOf(newRegion);
			if (oldArea == newArea) {
				return;
			}
			if (oldArea != null) {
				foreach (string name in new List<string>(oldArea.RegionEvents)) {
					Exit(name, ch);
				}
			}
			if (newArea != null) {
				foreach (string name in new List<string>(newArea.RegionEvents)) {
					Enter(name, ch);
				}
			}
		}

		private static void Enter(string eventName, Character ch) {
			switch (eventName.ToLowerInvariant()) {
				case NoSummon:
				case PlayerHouse:
					RemoveAggressiveSummon(ch);
					break;
				case MinesNoHorse:
					RemoveAggressiveSummon(ch);
					ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).enteredMines);
					break;
				case SafeZone:
					if (ch.IsPlayer) {
						if (!ch.IsGM) {
							ch.Flag_Invulnerable = true;
							ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).enteredSafeZone);
						}
					} else {
						NPC npc = ch as NPC;
						if (npc != null) {
							NPCAiTimer ai = NPCAiTimer.GetFor(npc);
							if (ai != null) {
								ai.Disengage(); //gohome: drop the fight, the wander leash walks it home
							}
						}
					}
					break;
			}
		}

		private static void Exit(string eventName, Character ch) {
			switch (eventName.ToLowerInvariant()) {
				case MinesNoHorse:
					ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).leftMines);
					break;
				case SafeZone:
					if (ch.IsPlayer && !ch.IsGM) {
						ch.Flag_Invulnerable = false;
						ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).leftSafeZone);
					}
					break;
			}
		}

		/// <summary>Sphere's removeAgressiveSummon: a summoned creature (other than a summoned
		/// mount) entering the area is unsummoned - its despawn timer runs out now.</summary>
		public static void RemoveAggressiveSummon(Character ch) {
			if (!SummonSpellDef.IsSummonedCreature(ch)) {
				return;
			}
			NPC npc = ch as NPC;
			if ((npc != null) && (npc.Def != null)) {
				foreach (string mount in SummonedMountDefnames) {
					if (StringComparer.OrdinalIgnoreCase.Equals(npc.Def.PrettyDefname, mount)) {
						return;
					}
				}
			}
			SummonSpellDef.DespawnNow(ch);
		}

		/// <summary>e_nofield/e_notele: may ch cast this spell where it stands? Called with the
		/// other region checks when a cast starts and when it lands.</summary>
		public static bool CheckSpellAllowed(Character caster, SpellDef spell) {
			if (caster.IsGM) {
				return true;
			}
			FlaggedRegion area = AreaOf(caster.Region);
			if ((spell.Id == TeleportSpellId) && HasEvent(area, NoTeleport)) {
				caster.SysMessage(Loc<RegionEventsLoc>.Get(caster.Language).noTeleport);
				return false;
			}
			if (caster.IsPlayer && (Array.IndexOf(FieldSpellIds, spell.Id) >= 0) && HasEvent(area, NoField)) {
				caster.SysMessage(Loc<RegionEventsLoc>.Get(caster.Language).noFieldSpell);
				return false;
			}
			return true;
		}

		/// <summary>e_nohorse/e_nohorsehigh, from Character.On_StepTaken (never called for GMs):
		/// a rider stepping in the mines, or above the high-floor limit on anything but an
		/// ostard, gets off.</summary>
		public static void OnStepTaken(Character ch) {
			if (!ch.Flag_Riding) {
				return;
			}
			FlaggedRegion area = AreaOf(ch.Region);
			bool dismount = HasEvent(area, MinesNoHorse);
			if (!dismount && ch.IsPlayer && (ch.Z > NoHorseHighMaxZ) && HasEvent(area, NoHorseHigh)) {
				AbstractCharacter mount = ch.Mount;
				dismount = (mount == null) || (Array.IndexOf(HighMountItems, mount.MountItem) < 0);
			}
			if (dismount) {
				ch.Dismount();
				ch.SysMessage(Loc<RegionEventsLoc>.Get(ch.Language).mountRefuses);
			}
		}
	}

	public class RegionEventsLoc : CompiledLocStringCollection<RegionEventsLoc> {
		public string enteredMines = "You have descended into the mines."; //r_doly_nohorse
		public string leftMines = "You are leaving the mines.";
		public string enteredSafeZone = "You are now in a safe zone."; //r_SafeZone
		public string leftSafeZone = "You have left the safe zone.";
		public string noFieldSpell = "This spell cannot be used here."; //r_nofield
		public string noTeleport = "Don't try to cheat!"; //r_notele
		public string mountRefuses = "Your mount refuses to go any further."; //r_doly_nohorse, r_nohorsehigh
	}
}
