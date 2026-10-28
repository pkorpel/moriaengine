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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #791 - Erebor's i_monster_create (systems/spawns.scp): a one-shot script item every monster
	/// gets, whose 0-second timer runs "creaturestart" right after the NPC is created - after its
	/// ON=@NPCRestock loot (NPCDef.Loot) is already in the backpack, and after a spawn gem has
	/// tagged it (#794 needs that). Here a CreatureStartTimer, also due at once. The loot part:
	/// - the gold marker ("Loot = i_gold") becomes ExperienceReward * goldPermille / 1000 gold
	///   (750 outside dungeons, 900 in them) - unless the monster was resurrected;
	/// - i_nofame: no experience, no gold;
	/// - a gold-scaled chance of a magic item - a PLACEHOLDER here (magic items come later);
	/// - gold-scaled rare extras: elemental coal (more likely for fire monsters), ebony crystal,
	///   ebony bowstring, treasure map;
	/// - an experience reward between 0 and 10 is raised to 10.
	/// #794 - outside dungeons, right before the gold: the elite-monster roll (EliteMonster).
	/// Not ported (not loot): combat-type tags, spawn bookkeeping.
	/// Tunable on the .settings "Loot" page (LootSettings).
	/// </summary>
	public static class MonsterLoot {
		public const string MonsterCreateDefname = "i_monster_create";

		[ThreadStatic]
		private static bool creatingResurrected;

		/// <summary>True while a self-resurrecting monster is re-created (SelfRes) - like Erebor's
		/// isResurrectedNPC, it keeps its loot items but its gold marker stays at 1.</summary>
		public static bool CreatingResurrected {
			get { return creatingResurrected; }
			set { creatingResurrected = value; }
		}

		public static bool IsMonsterCreateMarker(string defname) {
			return string.Equals(defname, MonsterCreateDefname, StringComparison.OrdinalIgnoreCase);
		}

		internal static readonly TimerKey creatureStartTimerKey = TimerKey.Acquire("_creatureStart_");

		/// <summary>Called by NPC.On_Create for "Equip = i_monster_create" monsters.</summary>
		public static void ScheduleCreatureStart(NPC npc) {
			npc.AddTimer(creatureStartTimerKey, new CreatureStartTimer(creatingResurrected)).DueInSeconds = 0;
		}

		/// <summary>Runs a still pending creaturestart right now (tests, or code that needs the final
		/// gold/elite state of a monster it just created). False if there was none.</summary>
		public static bool FlushCreatureStart(NPC npc) {
			CreatureStartTimer timer = npc.RemoveTimer(creatureStartTimerKey) as CreatureStartTimer;
			if (timer == null) {
				return false;
			}
			ApplyCreatureStart(npc, timer.resurrected);
			return true;
		}

		public static void ApplyCreatureStart(NPC npc, bool resurrected) {
			LootSettings settings = LootSettings.instance;
			AbstractItem pack = npc.FindLayer(LayerNames.Pack);

			AbstractItem noFame = FindByDefname(npc, pack, "i_nofame");
			if (noFame != null) {
				npc.ExperienceReward = 0;
				noFame.Delete();
			} else {
				bool dungeon = IsDungeonMapPlane(npc.M);
				if (!dungeon && !resurrected) {
					EliteMonster.TrySpawnElite(npc); //#794 - before the gold, which follows its raised experience
				}
				AbstractItem gold = FindByDefname(npc, pack, "i_gold");
				if ((gold != null) && !resurrected) {
					int gp = GoldFromExperience(npc.ExperienceReward, dungeon,
						settings.goldPermilleSurface, settings.goldPermilleDungeon);
					if (gp <= 0) {
						gold.Delete();
					} else {
						gold.Amount = gp;
						if (RollMagicItem(gp, settings, Globals.dice)) {
							DropMagicItemPlaceholder(npc, gp + " gold");
						}
						if (settings.rareExtras) {
							GiveRareExtras(npc, gp, Globals.dice);
						}
					}
				}
			}

			int exp = npc.ExperienceReward;
			if ((exp > 0) && (exp < 10)) {
				npc.ExperienceReward = 10;
			}
		}

		/// <summary>Erebor's isDung: map planes 2 and 10-15.</summary>
		public static bool IsDungeonMapPlane(byte m) {
			return (m == 2) || ((m >= 10) && (m <= 15));
		}

		public static int GoldFromExperience(int experience, bool dungeon, int surfacePermille, int dungeonPermille) {
			if (experience <= 0) {
				return 0;
			}
			return (int) (((long) experience * (dungeon ? dungeonPermille : surfacePermille)) / 1000);
		}

		/// <summary>Erebor's f_creatureStart_giveMIBag: nothing at or below magicItemMinGold gold;
		/// otherwise a drop when the gold beats a roll of {min .. max} (max lower for rich monsters).</summary>
		public static bool RollMagicItem(int gold, LootSettings settings, Random random) {
			if (gold <= settings.magicItemMinGold) {
				return false;
			}
			int max = (gold < settings.magicItemRichGold) ? settings.magicItemRollMax : settings.magicItemRollMaxRich;
			return gold > RollInclusive(random, settings.magicItemMinGold, max);
		}

		/// <summary>PLACEHOLDER (#791) - magic items aren't designed yet. Erebor gave an i_mi_bag here;
		/// for now the roll only logs, so the chance is in place for when the drop is.</summary>
		public static void DropMagicItemPlaceholder(NPC npc, string why) {
			Logger.WriteDebug("MonsterLoot: " + npc.Name + "#" + npc.Uid + " (" + why + ") rolled a magic item - not implemented yet");
		}

		/// <summary>Erebor's thresholds, each "gold &gt; a random roll" - the bigger the gold, the likelier.</summary>
		public static void GiveRareExtras(NPC npc, int gold, Random random) {
			if (gold > RollInclusive(random, 3500, 100000) * RollInclusive(random, 1, 3)) {
				Give(npc, "i_coal_elemental");
			}
			if (IsFireMonster(npc) && (gold > RollInclusive(random, 20, 30000))) {
				Give(npc, "i_coal_elemental");
			}
			if (gold > RollInclusive(random, 3500, 100000) * RollInclusive(random, 1, 10)) {
				Give(npc, "i_krystal_eben");
			}
			if (gold > RollInclusive(random, 3500, 100000) * RollInclusive(random, 1, 8)) {
				Give(npc, "i_tetiva_eben");
			}
			if (gold > RollInclusive(random, 30, 100000) * RollInclusive(random, 1, 17)) {
				Give(npc, "i_u_treasure_map");
			}
		}

		/// <summary>Sphere's {a b}: a random whole number from a to b, both included.</summary>
		public static int RollInclusive(Random random, int min, int max) {
			return (max <= min) ? min : random.Next(min, max + 1);
		}

		private static bool IsFireMonster(NPC npc) {
			//Erebor: body c_elem_fire_m, color 044c, or the fire fanatic/fire lord
			string defname = npc.Def.PrettyDefname;
			return (npc.Color == 0x44c) ||
				string.Equals(defname, "c_elem_fire_m", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(defname, "c_fire_fanatik", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(defname, "c_fire_lord", StringComparison.OrdinalIgnoreCase);
		}

		private static void Give(NPC npc, string defname) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if (def == null) {
				Logger.WriteWarning("MonsterLoot: rare extra '" + defname + "' is not a known ItemDef.");
				return;
			}
			def.Create(npc.Backpack);
		}

		private static AbstractItem FindByDefname(NPC npc, AbstractItem pack, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			if (def == null) {
				return null;
			}
			if (pack != null) {
				foreach (AbstractItem item in pack) {
					if (item.Def == def) {
						return item;
					}
				}
			}
			foreach (AbstractItem item in npc) {
				if (item.Def == def) {
					return item;
				}
			}
			return null;
		}
	}

	/// <summary>#791/#794 - Erebor's i_monster_create timer (timerd=0): creaturestart right after the
	/// creation code - a spawn gem's tag included - has run.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CreatureStartTimer : BoundTimer {
		/// <summary>Re-created by SelfRes - like Erebor's isResurrectedNPC: no gold, no elite.</summary>
		[SaveableData, CopyableData]
		public bool resurrected;

		[DeepCopyImplementation, LoadingInitializer]
		public CreatureStartTimer() {
		}

		public CreatureStartTimer(bool resurrected) {
			this.resurrected = resurrected;
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = cont as NPC;
			if ((npc != null) && !npc.IsDeleted) {
				MonsterLoot.ApplyCreatureStart(npc, this.resurrected);
			}
		}
	}
}
