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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>Which attribute a call to LevelingSystem.TrySpendAttributePoints targets (#129).</summary>
	public enum AttributeStat {
		Str,
		Dex,
		Int,
		Vit
	}

	/// <summary>
	/// Core leveling/experience system (#125). This is the one shared entry point other
	/// systems (monster kills, future quests/gates) should call to grant experience, rather
	/// than setting Player.Experience directly - that keeps the level-up check (threshold
	/// formula + overflow carry) in one place instead of duplicated at every call site.
	/// </summary>
	public static class LevelingSystem {
		public const int StartingStatSum = 300;

		private static readonly TagKey lastDeathTimeTK = TagKey.Acquire("_levelingLastDeathTime_");

		/// <summary>How long after a death a player is immune to losing MORE experience from a
		/// subsequent death - mirrors Erebor's own i_checkstats_timer (5 real minutes,
		/// experiences.scp), there to stop death-chaining (e.g. corpse-camping) from draining a
		/// player repeatedly. Not a GM setting (unlike the two percentages below) - the user's
		/// design only called out those two as needing in-game tuning.</summary>
		private static readonly System.TimeSpan PostDeathExperienceLossImmunity = System.TimeSpan.FromSeconds(300);

		/// <summary>Stationary effect graphic + sound played at the player on every level-up -
		/// best-effort aesthetic choice (no canonical "level up" effect exists in either this
		/// codebase or the Erebor reference archive to port), easy to retune: 0x375A is a
		/// golden upward swirl already used elsewhere in this engine for a positive magical
		/// effect (see Magery.cs's Fizzle for the sibling "negative" example, 0x3735), paired
		/// with a short musical "success" sound since no dedicated fanfare/ding sound exists in
		/// SoundNames either.</summary>
		private const int LevelUpEffectGraphic = 0x375A;
		private const SoundNames LevelUpSound = SoundNames.DrumSuccess;

		/// <summary>
		/// Grants (or removes, if negative) experience and runs the level-up check - a while
		/// loop, not a single if, so one large gain can correctly cross several level
		/// thresholds in a row, carrying over the leftover experience each time rather than
		/// clamping it to 0 (e.g. level 1 with 600 exp, +100 -> level 2 with 40 exp left over).
		/// Level never decreases here even if amount is negative - that's intentional, this
		/// method is for gaining experience through normal gameplay. Death's exp loss (#128)
		/// is a separate, simpler operation (reset to 0, no level change) that doesn't go
		/// through this method.
		/// </summary>
		public static void AddExperience(Player player, int amount) {
			if (player == null || player.IsDeleted || amount == 0) { //#1416 - @gainExperience can't be raised on a deleted one
				return;
			}
			int baseAmount = amount;
			amount += Boosters.Bonus(player, baseAmount); //#1090 - Erebor's addexp: a running booster's percent of a gain
			amount += RpRanks.Bonus(player, baseAmount); //#1116 - and the RP rank's, both of the base, added
			amount += WorldEvents.ExperienceBonus(player, baseAmount); //#1286 - the beacons of Gondor lit: the realm's bonus
			int? granted = LevelingTriggers.GainExperience(player, baseAmount, amount); //#1416 - @gainExperience may change it or cancel
			if ((granted == null) || (granted.Value == 0)) {
				return;
			}
			amount = granted.Value;
			player.Experience += amount;
			if (amount > 0) {
				player.SysMessage("You have gained " + amount + " experience.");
			} else {
				player.SysMessage("You have lost " + (-amount) + " experience.");
			}
			int coefficient = LevelingSettings.instance.nextLevelCoefficient;
			int levelBefore = player.Level;
			while (player.Experience > (player.Level + 1) * coefficient) {
				player.Level += 1;
				player.Experience -= player.Level * coefficient;
				//Skill/ability points are flat per-level increments (#129) - attribute points are
				//NOT granted here, that budget is computed on demand from Level (see
				//Player.AttributePointBudget/LevelStatBonus below), since it's a tiered formula
				//of the absolute level, not a simple running total.
				player.UnspentSkillPoints += 5;
				player.UnspentAbilityPoints += 2;
				player.SysMessage("You have advanced to level " + player.Level + "!");
				EffectFactory.StationaryEffect(player, LevelUpEffectGraphic, 10, 20);
				player.Sound((int) LevelUpSound);
				player.RefreshTitle(); //#165 - title ladder tier may have changed
			}
			if (player.Level > levelBefore) {
				WeeklyQuests.Check(player); //#1090 - f_srovnatlevel: a weekly set from level 10
			}
		}

		/// <summary>The tiered attribute-point-budget bonus (#129), verified against Erebor's own
		/// f_levelstatbonus: level*4 through level 9, 40+(level-10)*3 through level 29,
		/// 100+(level-30)*2 through level 39, then flat 120 forever from level 40 on (level 40 is
		/// the last level that grants any further attribute points). Added to a flat base of 300
		/// in Player.AttributePointBudget.</summary>
		public static int LevelStatBonus(int level) {
			if (level <= 9) {
				return level * 4;
			}
			if (level <= 29) {
				return 40 + (level - 10) * 3;
			}
			if (level <= 39) {
				return 100 + (level - 30) * 2;
			}
			return 120;
		}

		/// <summary>A click raises the skill by 5.0% (50 raw units, skill values are stored *10) -
		/// Erebor's [DIALOG d_levelbonus BUTTON]. It costs 1, 2 or 3 points, see SkillRaiseCost.</summary>
		public const int SkillPointGain = 50;

		/// <summary>Combat/magic/social skills a player can raise by spending points (#204) -
		/// everything else (crafting: Blacksmith/Fletching/Carpentry/Cooking/Tailoring/Tinkering/
		/// Inscribe/Alchemy; gathering: Lumberjacking/Mining/Fishing; Cartography/Lockpicking/
		/// AnimalTaming) must be trained by actual use instead - the user's own example was
		/// Fishing. Ported verbatim from Erebor's "[defnames combat_skills]" allowlist
		/// (scripts/experiences.scp lines ~1277-1312, the "combskill_&lt;name&gt;" tags its own
		/// d_levelbonus button handler gates on) rather than inventing a new rule. One entry in
		/// Erebor's list ("Enticement") has no equivalent in this engine's SkillName enum at all
		/// and is simply omitted.
		///
		/// Discordance was originally excluded too (omitted from Erebor's own list), but the user
		/// corrected this - it belongs alongside its bard-skill siblings Peacemaking/Provocation/
		/// Musicianship, all of which were already spendable; excluding only Discordance was an
		/// inconsistent port, not a deliberate design choice.</summary>
		private static readonly System.Collections.Generic.HashSet<SkillName> SpendableSkills = new System.Collections.Generic.HashSet<SkillName> {
			SkillName.Anatomy, SkillName.AnimalLore, SkillName.ItemID, SkillName.ArmsLore,
			SkillName.Parry, SkillName.Begging, SkillName.Peacemaking, SkillName.Camping,
			SkillName.DetectHidden, SkillName.EvalInt, SkillName.Healing, SkillName.Forensics,
			SkillName.Herding, SkillName.Hiding, SkillName.Provocation, SkillName.Magery,
			SkillName.MagicResist, SkillName.Tactics, SkillName.Snooping, SkillName.Musicianship,
			SkillName.Poisoning, SkillName.Archery, SkillName.SpiritSpeak, SkillName.Stealing,
			SkillName.TasteID, SkillName.Tracking, SkillName.Veterinary, SkillName.Swords,
			SkillName.Macing, SkillName.Fencing, SkillName.Wrestling, SkillName.Meditation,
			SkillName.Stealth, SkillName.RemoveTrap, SkillName.Discordance,
		};

		/// <summary>True iff `skill` can be raised by spending points at all (#204) - see
		/// SpendableSkills. Exposed separately from TrySpendSkillPoints so D_LevelSpendSkills can
		/// decide whether to even show a "+1" button for a given row.</summary>
		public static bool IsSkillPointSpendable(SkillName skill) {
			return SpendableSkills.Contains(skill);
		}

		/// <summary>bodunaskill: what the next 5.0 costs, by the value it starts from.</summary>
		public static int SkillRaiseCost(int rawValue) {
			if (rawValue < 500) {
				return 1;
			}
			return (rawValue < 750) ? 2 : 3;
		}

		/// <summary>roundskill: to the nearest 5.0, never under what the profession starts at.</summary>
		public static int RoundSkill(int rawValue, int minimum) {
			return (rawValue < minimum) ? minimum : (((rawValue + 25) / 50) * 50);
		}

		/// <summary>ismagic, the spells among its list: stat changes would be spent on top of.</summary>
		public static bool IsUnderStatMagic(Player player) {
			foreach (Plugin plugin in player.GetAllPlugins()) {
				if ((plugin is StrModSpellEffectPlugin) || (plugin is DexModSpellEffectPlugin) || (plugin is IntModSpellEffectPlugin) ||
					(plugin is BlessSpellEffectPlugin) || (plugin is CurseSpellEffectPlugin)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>#882 - one click of d_levelbonus on a skill: rounds it, then raises it by 5.0
		/// unless a master has to teach this step. Only combat_skills can be raised with points
		/// (#204). Returns why not, or null when it went up.</summary>
		public static string RaiseSkill(Player player, SkillName skill) {
			ProfessionDef profession = player.Profession;
			if ((profession == null) || !IsSkillPointSpendable(skill)) {
				return "Tuto dovednost nelze zvysovat body.";
			}
			if (IsUnderStatMagic(player)) {
				return "Jsi pod vlivem magie - nemuzes zvysovat sve dovednosti.";
			}
			int value = RoundSkill(player.GetRealSkillValue(skill), profession.GetSkillMinimum(skill));
			player.SetRealSkillValue((int) skill, value);
			if ((value + SkillPointGain) > profession.GetSkillCap(skill)) {
				return "V teto dovednosti jsi uz " + (player.IsFemale ? "dosahla" : "dosahl") + " maxima.";
			}
			if ((value == 250) || (value == 500) || (value == 750)) {
				return "Na dalsi uroven v teto dovednosti te musi vyucit mistr.";
			}
			int cost = SkillRaiseCost(value);
			if (player.UnspentSkillPoints < cost) {
				return "Na zvyseni teto dovednosti nemas dost bodu.";
			}
			player.SetRealSkillValue((int) skill, value + SkillPointGain);
			player.UnspentSkillPoints -= cost;
			return null;
		}

		/// <summary>#882 - f_srazitskilly + roundcombatskills, at death: what use taught is kept only
		/// in steps of 5.0 for the skills bought with points, and nothing stays outside the
		/// profession's limits.</summary>
		public static void TrimSkillsAtDeath(Player player) {
			ProfessionDef profession = player.Profession;
			if (profession == null) {
				return;
			}
			foreach (SkillDef skillDef in SkillDef.AllSkillDefs) {
				int minimum = profession.GetSkillMinimum(skillDef);
				int value = System.Math.Min(System.Math.Max(player.GetRealSkillValue(skillDef), minimum), profession.GetSkillCap(skillDef));
				if (IsSkillPointSpendable((SkillName) skillDef.Id)) {
					value = RoundSkill(value, minimum);
				}
				if (value != player.GetRealSkillValue(skillDef)) {
					player.SetRealSkillValue(skillDef, value);
				}
			}
		}

		/// <summary>#882 - startclass: a new life starts at level 0 with nothing to spend, skills
		/// exactly at the profession's start values, and the 300 chosen stats already paid for.</summary>
		public static void StartNewLife(Player player, ProfessionDef profession) {
			player.Level = 0;
			player.Experience = 0;
			player.UnspentSkillPoints = 0;
			player.UnspentAbilityPoints = 0;
			ClassAbilities.Reset(player); //#1017
			foreach (SkillDef skillDef in SkillDef.AllSkillDefs) {
				player.SetRealSkillValue(skillDef, profession.GetSkillMinimum(skillDef));
			}
			RecordStartingStats(player);
		}

		/// <summary>Stats as Erebor's strlev & co: the budget counts them from the first point.</summary>
		public static void RecordStartingStats(Player player) {
			player.StrPointsAllocated = player.Str;
			player.DexPointsAllocated = player.Dex;
			player.IntPointsAllocated = player.Int;
			player.VitPointsAllocated = player.Vit;
		}

		/// <summary>#882 - characters from before the start stats were counted had 300 free points.</summary>
		public static void FixUncountedStartingStats(Player player) {
			int allocated = player.StrPointsAllocated + player.DexPointsAllocated + player.IntPointsAllocated + player.VitPointsAllocated;
			if ((player.Profession != null) && (allocated < StartingStatSum)) {
				RecordStartingStats(player);
			}
		}

		/// <summary>Spends `pointsToSpend` unspent ability points (#129) into `ability`, via the
		/// existing (previously unused) Character.ModifyAbilityPoints mechanism. Does NOT
		/// validate against any profession-specific ability allowlist - that depends on the
		/// Abilities/talent-tree redesign the user explicitly deferred (see CLAUDE.md's
		/// Professions section); this just spends the pool, same way the engine already lets any
		/// ability points be granted today.</summary>
		public static bool TrySpendAbilityPoints(Player player, AbilityDef ability, int pointsToSpend) {
			if ((player == null) || (ability == null) || (pointsToSpend <= 0) ||
				(player.UnspentAbilityPoints < pointsToSpend)) {
				return false;
			}
			player.ModifyAbilityPoints(ability, pointsToSpend);
			player.UnspentAbilityPoints -= pointsToSpend;
			return true;
		}

		/// <summary>Spends `pointsToSpend` attribute points (#129/#156) into one of
		/// STR/DEX/INT/VIT, validated against BOTH Player.AttributePointBudget (the overall pool)
		/// AND the profession's own per-stat hard cap (ProfessionDef.GetStatRange's 3rd element,
		/// Erebor's nastaveni_povolani_maxstats). `refusal` says why not, null for no points left,
		/// which on Erebor just shows the dialog again.</summary>
		public static bool TrySpendAttributePoints(Player player, AttributeStat stat, int pointsToSpend, out string refusal) {
			refusal = null;
			if ((player == null) || (pointsToSpend <= 0) || (player.Profession == null)) {
				return false;
			}
			if (IsUnderStatMagic(player)) {
				refusal = "Jsi pod vlivem magie, nemuzes zvysovat sve dovednosti.";
				return false;
			}
			if (player.AttributePointBudget < pointsToSpend) {
				return false;
			}
			string atMaximum = (player.IsFemale ? "Dosahla" : "Dosahl") + " jsi maximalni hodnoty.";
			int[] range = player.Profession.GetStatRange(stat);
			int hardCap = (range.Length == 3) ? range[2] : short.MaxValue;
			switch (stat) {
				case AttributeStat.Str:
					if ((player.Str + pointsToSpend) > hardCap) {
						refusal = atMaximum;
						return false;
					}
					player.Str += (short) pointsToSpend;
					player.StrPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Dex:
					if ((player.Dex + pointsToSpend) > hardCap) {
						refusal = atMaximum;
						return false;
					}
					player.Dex += (short) pointsToSpend;
					player.DexPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Int:
					if ((player.Int + pointsToSpend) > hardCap) {
						refusal = atMaximum;
						return false;
					}
					player.Int += (short) pointsToSpend;
					player.IntPointsAllocated += pointsToSpend;
					break;
				case AttributeStat.Vit:
					if ((player.Vit + pointsToSpend) > hardCap) {
						refusal = atMaximum;
						return false;
					}
					player.Vit += (short) pointsToSpend;
					player.VitPointsAllocated += pointsToSpend;
					break;
			}
			return true;
		}

		/// <summary>#902 - who shares a death: everyone who fought the victim lately, near it and on its plane,
		/// as Erebor's @death with f_countfight. Pets and summons don't count. With `withParties`, a fighter's
		/// party nearby counts too, alive, which Erebor didn't do: it's how a healer in a group gets experience.</summary>
		public static List<Character> FindFighters(Character victim, Character killedBy, bool withParties) {
			LevelingSettings settings = LevelingSettings.instance;
			List<Character> found = FightMemory.GetFighters(victim, TimeSpan.FromSeconds(settings.fightCreditSeconds));
			if ((killedBy != null) && !found.Contains(killedBy)) {
				found.Add(killedBy);
			}
			found.RemoveAll(ch => !CountsAsFighter(victim, ch));
			if (withParties) {
				foreach (Character fighter in new List<Character>(found)) {
					Party party = Party.GetParty(fighter);
					if (party != null) {
						foreach (Character member in party.Members) {
							if (!found.Contains(member) && !member.Flag_Dead && CountsAsFighter(victim, member)) {
								found.Add(member);
							}
						}
					}
				}
			}
			return found;
		}

		private static bool CountsAsFighter(Character victim, Character ch) {
			if ((ch == victim) || ch.IsDeleted || (ch.M != victim.M)) {
				return false;
			}
			NPC npc = ch as NPC;
			if ((npc != null) && ((npc.ControlMaster != null) || SummonSpellDef.IsSummonedCreature(npc))) {
				return false;
			}
			return Point2D.GetSimpleDistance(ch.X, ch.Y, victim.X, victim.Y) <= LevelingSettings.instance.fightCreditRange;
		}

		/// <summary>Sphere's sqrt_int: about 100 x the square root.</summary>
		public static int SqrtInt(int value) {
			return (int) Math.Sqrt(value * 10000.0);
		}

		/// <summary>What each of `fighters` gets from a monster worth `reward` alone: the pool grows with the
		/// square root of their number, so together they earn more and each less.</summary>
		public static int MonsterShare(int reward, int fighters) {
			return (int) (((long) reward * SqrtInt(fighters) / 100) / fighters);
		}

		/// <summary>Erebor's @death for a monster. The cap is for areas of tag.realm below 3, and not for elites.</summary>
		public static void ShareMonsterExperience(NPC victim, Character killedBy) {
			if (SummonSpellDef.IsSummonedCreature(victim)) {
				return;
			}
			int reward = victim.ExperienceReward;
			List<Character> fighters = FindFighters(victim, killedBy, true);
			if (fighters.Count == 0) {
				return;
			}
			if (reward == 0) { //#1416 - Erebor's loop credits a kill worth nothing too, with 0
				foreach (Character fighter in fighters) {
					if (!ShamanPacts.GivesBloodPact(fighter as Player)) {
						LevelingTriggers.KillCredit(fighter, victim, 0);
					}
				}
				return;
			}
			int share = MonsterShare(reward, fighters.Count);
			int cap = LevelingSettings.instance.monsterExpCap;
			FlaggedRegion area = RegionEventSystem.AreaOf(victim.Region);
			if ((cap > 0) && (share > cap) && !EliteMonster.IsElite(victim) && ((area == null) || (area.RealmTag < 3))) {
				share = cap;
			}
			foreach (Character fighter in fighters) {
				if (ShamanPacts.EarnsNoKillExp(fighter as Player)) { //#1017 - a blood pact's giver earns through its bearer
					continue;
				}
				AddExperience(fighter as Player, share);
				LevelingTriggers.KillCredit(fighter, victim, share); //#1416 - Erebor's trigger_kill after the addexp
				LevelingTriggers.KillCredit(ShamanPacts.ShareKillExp(fighter as Player, share), victim, share); //and on the blood pact's bearer
				Q017.OnKillCredit(fighter as Player, victim); //#1268 - Terlak's gate crystals
			}
		}

		/// <summary>#902 - Erebor's @death for a player, which replaced the design of #128. It costs a share of
		/// what the next level takes, nothing if nobody fought them, within 5 minutes of a previous death
		/// (i_checkstats_timer) or in a safe zone or arena. Those who fought share what was lost evenly,
		/// but nothing for someone of their own realm or a craftsman.</summary>
		public static void ApplyDeathExperienceLoss(Player victim, Character killedBy) {
			if (victim == null) {
				return;
			}
			LevelingSettings settings = LevelingSettings.instance;
			List<Character> fighters = FindFighters(victim, killedBy, false);
			bool immune = IsWithinPostDeathImmunity(victim) || Notoriety.IsExemptFromExperienceLoss(victim);
			victim.SetTag(lastDeathTimeTK, Globals.TimeAsSpan);
			if (fighters.Count == 0) {
				return;
			}
			int lost = 0;
			if (!immune) {
				int nextLevel = (victim.Level + 1) * settings.nextLevelCoefficient;
				lost = Math.Max(0, Math.Min((int) ((long) nextLevel * settings.deathExpLossPercent / 100), victim.Experience));
				victim.Experience -= lost;
			}
			//#1416 - Erebor's loop runs whatever was lost (immune, nothing left to lose: addfame 0) and credits every fighter
			int share = (int) ((long) lost * settings.deathExpPaidPercent / 100) / fighters.Count;
			bool craftsman = Professions.HasProfession(victim, Professions.Craftsman);
			foreach (Character fighter in fighters) {
				int paid = (craftsman || ((fighter is Player) && (fighter.Realm == victim.Realm))) ? 0 : share; //own realm: player kills only, as Erebor
				AddExperience(fighter as Player, paid);
				LevelingTriggers.KillCredit(fighter, victim, paid);
			}
		}

		/// <summary>i_checkstats_timer: this player died moments ago, which a kill doesn't count on.</summary>
		public static bool IsRecentlyDead(Player player) {
			return IsWithinPostDeathImmunity(player);
		}

		private static bool IsWithinPostDeathImmunity(Player victim) {
			object tag = victim.GetTag(lastDeathTimeTK);
			if (tag is System.TimeSpan lastDeath) {
				return (Globals.TimeAsSpan - lastDeath) < PostDeathExperienceLossImmunity;
			}
			return false;
		}
	}
}
