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
using SteamEngine.LScript;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #794 - Erebor's elite monsters (systems/events_npc/EliteNPC/EliteNPC.scp). A monster from a
	/// spawn gem, outside dungeons, worth at least 1000 experience, rarely (experience/500 %, at
	/// most 5%) turns elite when it spawns - checked by creaturestart (MonsterLoot) right before
	/// its gold, so the gold follows the raised experience. An elite has a "power" of 500-900
	/// (permille, growing with experience) and:
	/// - +25% Str/Dex/Int, +20% swing damage, power/3 % extra swing damage vs summons;
	/// - absorbs power permille of every player's swing (less for players of a high enough level);
	/// - regenerates power/7 hit points every 2 seconds while hurt;
	/// - at least 70% (and at least power) poison resist, 97% summon resist;
	/// - 300-500% experience, an extra bundle of reagents/gems/diamonds, a chance of a magic
	///   item (a PLACEHOLDER, as in MonsterLoot);
	/// - once it has met a player, it leaves through a gate 20 minutes later (a 3 s spell a hit
	///   interrupts) - so it can't be camped forever.
	/// Bosses (NPCDef.IsBoss) never turn elite. Tunable on .settings "Elite Monsters" (EliteSettings).
	/// </summary>
	public static class EliteMonster {
		private static readonly TagKey eliteTK = TagKey.Acquire("_elitePower_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey denyEliteTK = TagKey.Acquire("denyElite");
		internal static readonly TimerKey regenTimerKey = TimerKey.Acquire("_eliteRegen_");
		internal static readonly TimerKey escapeTimerKey = TimerKey.Acquire("_eliteEscape_");

		public const int SparkleEffect = 0x375a; //i_fx_sparkle
		public const int SparkleHue = 0x995;
		public const int GateEffect = 0xf6c; //i_fx_yavanna = i_moongate_blue
		public const int TransformSound = 254;

		/// <summary>The elite power (500-900 by default), or 0 for a normal monster.</summary>
		public static int GetPower(Character ch) {
			object tag = ch.GetTag(eliteTK);
			return (tag is int power) ? power : 0;
		}

		public static bool IsElite(Character ch) {
			return GetPower(ch) > 0;
		}

		/// <summary>Erebor's f_spawnEliteForm: spawned (from a spawn gem that doesn't deny it), worth
		/// enough experience, and a lucky roll. Called by MonsterLoot.ApplyCreatureStart (surface only).</summary>
		public static void TrySpawnElite(NPC npc) {
			EliteSettings settings = EliteSettings.instance;
			if (!settings.enabled) {
				return;
			}
			Thing gem = npc.GetTag(spawnGemTK) as Thing;
			if ((gem == null) || gem.HasTag(denyEliteTK)) {
				return;
			}
			int exp = npc.ExperienceReward;
			if (exp < settings.minExperience) {
				return;
			}
			if (SpawnChance(exp, settings) >= MonsterLoot.RollInclusive(Globals.dice, 1, 10000)) {
				Transform(npc);
			}
		}

		/// <summary>Per 10000.</summary>
		public static int SpawnChance(int experience, EliteSettings settings) {
			long chance = ((long) experience * settings.spawnChancePercentOfExperience) / 100;
			return (int) Math.Min(chance, settings.maxSpawnChance);
		}

		public static int PowerFromExperience(int experience, EliteSettings settings) {
			long power = settings.basePower + (((long) (experience - settings.minExperience) * settings.powerPer10000Exp) / 10000);
			return (int) Math.Max(settings.basePower, Math.Min(power, settings.maxPower));
		}

		public static int ExpBonusPercent(int power, EliteSettings settings) {
			return settings.expBonusBasePercent + ((power - settings.basePower) / 2);
		}

		/// <summary>Erebor's f_TransformToElite. Returns false for a boss or an already elite monster.</summary>
		public static bool Transform(NPC npc) {
			if (IsElite(npc) || ((NPCDef) npc.Def).IsBoss) {
				return false;
			}
			EliteSettings settings = EliteSettings.instance;
			int power = PowerFromExperience(npc.ExperienceReward, settings);
			npc.SetTag(eliteTK, power);

			npc.ResistPoison = Math.Max(npc.ResistPoison, Math.Max(settings.minResistPoison, power));
			npc.ResistSummon = Math.Max(npc.ResistSummon, settings.minResistSummon);

			npc.Str = AddPercent(npc.Str, settings.statBonusPercent);
			npc.Dex = AddPercent(npc.Dex, settings.statBonusPercent);
			npc.Int = AddPercent(npc.Int, settings.statBonusPercent);

			npc.ExperienceReward = (int) (((long) npc.ExperienceReward * ExpBonusPercent(power, settings)) / 100);

			if ((power - 400) > MonsterLoot.RollInclusive(Globals.dice, 1, 1000)) {
				//Erebor: i_mi_pouch, a small magic-item bag
				MonsterLoot.GiveMagicBag(npc, "i_mi_pouch");
			}
			if (settings.lootBundle) {
				GiveLootBundle(npc, power);
			}

			//periodic: BoundTimer deletes a one-shot timer before its OnTimeout, so it can't re-arm itself
			npc.AddTimer(regenTimerKey, new EliteRegenTimer()).PeriodInSeconds = settings.regenIntervalSeconds;

			npc.Sound(TransformSound);
			foreach (int[] offset in sparkleOffsets) {
				EffectFactory.StationaryEffectAt(new Point4D(npc.X + offset[0], npc.Y + offset[1], npc.Z, npc.M),
					SparkleEffect, 10, 20, false, false, SparkleHue, RenderModes.Opaque);
			}
			Logger.WriteDebug("EliteMonster: " + npc.Name + "#" + npc.Uid + " turned elite (power " + power + ", exp " + npc.ExperienceReward + ")");
			return true;
		}

		private static readonly int[][] sparkleOffsets = { new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 1, 0 } };

		private static short AddPercent(short stat, int percent) {
			return (short) Math.Min(short.MaxValue, stat + ((stat * percent) / 100));
		}

		/// <summary>Erebor's sure reward: 2x high reagents (power/10), 2x any reagents (power/20),
		/// gems (power/50), diamonds (power/100) - through the template ITEM op, like NPCDef.Loot.</summary>
		private static void GiveLootBundle(NPC npc, int power) {
			string[] lines = {
				"RANDOM_REAGENT_High," + (power / 10),
				"RANDOM_REAGENT_High," + (power / 10),
				"RANDOM_REAGENT_ALL," + (power / 20),
				"RANDOM_REAGENT_ALL," + (power / 20),
				"random_JEWEL," + (power / 50),
				"i_gem_diamond," + (power / 100),
			};
			foreach (string line in lines) {
				try {
					LScriptMain.GetNewSnippetRunner("EliteMonster", 0, "ITEM = " + line).Run(npc.Backpack, (ScriptArgs) null);
				} catch (Exception e) {
					Logger.WriteWarning("EliteMonster", "loot line '" + line + "' failed: " + e.Message);
				}
			}
		}

		/// <summary>Swing damage - DamageManager.ProcessSwing, before CauseDamage. An elite attacker
		/// hits harder (more so vs summons); an elite defender absorbs most of a player's swing.</summary>
		public static void ModifySwingDamage(Character attacker, Character defender, WeaponSwingArgs swingArgs) {
			EliteSettings settings = EliteSettings.instance;
			int attackerPower = GetPower(attacker);
			if (attackerPower > 0) {
				double damage = (swingArgs.DamageAfterAC * settings.swingDamagePermille) / 1000;
				if (SummonSpellDef.IsSummonedCreature(defender) && (settings.summonKillerPowerDivisor > 0)) {
					damage += (damage * (attackerPower / settings.summonKillerPowerDivisor)) / 100;
				}
				swingArgs.DamageAfterAC = damage;
			}

			int defenderPower = GetPower(defender);
			Player player = attacker as Player;
			if ((defenderPower > 0) && (player != null)) {
				int bonus = LevelBonus(player.Level, settings);
				swingArgs.DamageAfterAC = AbsorbedDamage(swingArgs.DamageAfterAC, defenderPower, bonus);
				player.SysMessage((bonus > 0) ? "Your experience lets you strike a stronger blow." :
					"Your target absorbed most of your attack.");
			}
		}

		/// <summary>Erebor's f_getEliteBonus - permille of the elite's power a player's level cancels out.</summary>
		public static int LevelBonus(int level, EliteSettings settings) {
			if (level < settings.levelBonusMinLevel) {
				return 0;
			}
			int levels = Math.Min(level, settings.levelBonusMaxLevel) - settings.levelBonusMinLevel;
			return settings.levelBonusBase + (levels * settings.levelBonusPerLevel);
		}

		public static double AbsorbedDamage(double damage, int power, int levelBonus) {
			int absorbed = Math.Max(0, Math.Min(1000, power - levelBonus));
			return damage - ((damage * absorbed) / 1000);
		}

		/// <summary>Erebor's @NpcSeeNewPlayer/@GetHit - the first player an elite fights starts its
		/// escape countdown, with a challenge. Called from NPCAiTimer.Think.</summary>
		public static void OnFightingPlayer(NPC npc, Character target) {
			if (!(target is Player) || !IsElite(npc) || npc.HasTimer(escapeTimerKey)) {
				return;
			}
			//periodic so it survives its own OnTimeout (see EliteRegenTimer); each step sets DueInSeconds
			BoundTimer escape = npc.AddTimer(escapeTimerKey, new EliteEscapeTimer());
			escape.PeriodInSeconds = EliteSettings.instance.escapeAfterSeconds;
			escape.DueInSeconds = EliteSettings.instance.escapeAfterSeconds;
			npc.Say("Ha! An enemy! Are you strong enough to defeat me?");
		}

		/// <summary>A landed hit interrupts the gate spell (Erebor's @GetHit with more1 == 1).</summary>
		public static void OnGotHit(Character defender) {
			EliteEscapeTimer escape = defender.GetTimer(escapeTimerKey) as EliteEscapeTimer;
			if ((escape != null) && escape.state == EliteEscapeTimer.Casting) {
				escape.state = EliteEscapeTimer.Waiting;
				escape.DueInSeconds = EliteSettings.instance.escapeRetrySeconds;
				defender.Sound(0x5c); //fizzle
				EffectFactory.StationaryEffect(defender, 0x3735, 6, 30);
			}
		}

		/// <summary>True while it's casting its gate or leaving - NPCAiTimer skips its turn (Erebor's flag_freeze).</summary>
		public static bool IsLeaving(Character ch) {
			EliteEscapeTimer escape = ch.GetTimer(escapeTimerKey) as EliteEscapeTimer;
			return (escape != null) && (escape.state != EliteEscapeTimer.Waiting);
		}
	}

	/// <summary>#794 - Erebor's e_dragon_regeneration on an elite: heals power/7 HP every 2 s while hurt.
	/// Periodic (PeriodInSeconds) - a one-shot BoundTimer is deleted before its OnTimeout runs.</summary>
	[SaveableClass, DeepCopyableClass]
	public class EliteRegenTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public EliteRegenTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = cont as NPC;
			if ((npc == null) || npc.IsDeleted || npc.Flag_Dead) {
				return;
			}
			EliteSettings settings = EliteSettings.instance;
			int power = EliteMonster.GetPower(npc);
			if ((power > 0) && (npc.Hits < npc.MaxHits) && (settings.regenPowerDivisor > 0)) {
				npc.Hits = (short) Math.Min(npc.MaxHits, npc.Hits + (power / settings.regenPowerDivisor));
				npc.Emote("is regenerating.");
			}
		}
	}

	/// <summary>#794 - Erebor's i_remover_eliteNPC: Waiting (20 min after meeting a player) ->
	/// Casting a gate (3 s, frozen; a hit sends it back to Waiting for 5 s) -> Leaving (2 s) -> gone.</summary>
	[SaveableClass, DeepCopyableClass]
	public class EliteEscapeTimer : BoundTimer {
		public const int Waiting = 0;
		public const int Casting = 1;
		public const int Leaving = 2;

		[SaveableData, CopyableData]
		public int state;

		[DeepCopyImplementation, LoadingInitializer]
		public EliteEscapeTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = cont as NPC;
			if ((npc == null) || npc.IsDeleted || npc.Flag_Dead) {
				return;
			}
			EliteSettings settings = EliteSettings.instance;
			switch (this.state) {
				case Waiting:
					this.state = Casting;
					npc.Say("Beleg Foroth Kel");
					npc.Anim(11);
					this.DueInSeconds = settings.escapeCastSeconds;
					break;
				case Casting:
					this.state = Leaving;
					npc.Say("You are no match for me!");
					EffectFactory.StationaryEffect(npc, EliteMonster.GateEffect, 10, 40);
					npc.Sound(0x20e); //gate
					this.DueInSeconds = 2;
					break;
				default:
					//Erebor moved it away and killed it there: no corpse, no loot, the spawn respawns it
					Logger.WriteDebug("EliteMonster: " + npc.Name + "#" + npc.Uid + " left through its gate");
					npc.Delete();
					break;
			}
		}
	}
}
