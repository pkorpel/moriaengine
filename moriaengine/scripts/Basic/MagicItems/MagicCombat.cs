using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - what worn magic does in a fight: slayers (e_daemonbonus...), leech (e_hitsleech... with the class
	/// abilities' half), paralysis (e_paralysing), and the spell reflex and block. The melee and ranged reflex and block
	/// are ThiefDefence's, reading the same wearer tags.</summary>
	public static class MagicCombat {
		private static readonly PluginKey paralysisPK = PluginKey.Acquire("_magicItemParalysis_");

		/// <summary>getmonstertypebonus: of the three tags naming this kind, the last one's percent (100 if none).</summary>
		internal static int MonsterTypeBonus(Character target, string kind, int value) {
			if (!(target is NPC npc) || (value == 0)) {
				return 0;
			}
			NPCDef def = (NPCDef) npc.Def;
			int percent = -1;
			foreach (string tag in new[] { def.MonsterType, def.MonsterType2, def.MonsterType3 }) {
				if (string.IsNullOrEmpty(tag)) {
					continue;
				}
				string[] parts = tag.Split(',');
				if (string.Equals(parts[0].Trim(), kind, StringComparison.OrdinalIgnoreCase)) {
					percent = ((parts.Length > 1) && int.TryParse(parts[1].Trim(), out int given)) ? given : 100;
				}
			}
			return (percent < 0) ? 0 : (value * percent) / 100;
		}

		/// <summary>The five slayers against this target, in Erebor's fallbacks: undead then daemon, dragon then a dragon's
		/// brain, humanoid, spider then animal, eye then golem.</summary>
		internal static int[] SlayerBonuses(Character attacker, Character target) {
			int[] bonuses = new int[5];
			int demon = MagicItems.WearerValue(attacker, 101);
			bonuses[0] = MonsterTypeBonus(target, "undead", demon);
			if (bonuses[0] == 0) {
				bonuses[0] = MonsterTypeBonus(target, "daemon", demon);
			}
			int dragon = MagicItems.WearerValue(attacker, 102);
			bonuses[1] = MonsterTypeBonus(target, "dragon", dragon);
			if ((bonuses[1] == 0) && (target is NPC drake) && ((NPCDef) drake.Def).DragonBrain) {
				bonuses[1] = dragon;
			}
			bonuses[2] = MonsterTypeBonus(target, "humanoid", MagicItems.WearerValue(attacker, 103));
			int animal = MagicItems.WearerValue(attacker, 104);
			bonuses[3] = MonsterTypeBonus(target, "spider", animal);
			if (bonuses[3] == 0) {
				bonuses[3] = MonsterTypeBonus(target, "animal", animal);
			}
			int mechanic = MagicItems.WearerValue(attacker, 105);
			bonuses[4] = MonsterTypeBonus(target, "eye", mechanic);
			if (bonuses[4] == 0) {
				bonuses[4] = MonsterTypeBonus(target, "golem", mechanic);
			}
			return bonuses;
		}

		internal static double ApplySlayers(Character attacker, Character target, double amount) {
			foreach (int bonus in SlayerBonuses(attacker, target)) {
				if (bonus != 0) {
					amount = (amount * (100 + bonus)) / 100;
				}
			}
			return amount;
		}

		/// <summary>@AfterSwing's slayers, on a physical blow.</summary>
		internal static void ModifyBlow(Character attacker, Character defender, WeaponSwingArgs swing) {
			if (attacker.IsPlayer) {
				swing.DamageAfterAC = ApplySlayers(attacker, defender, swing.DamageAfterAC);
			}
		}

		/// <summary>@BeforeDoEffect's slayers, on every spell.</summary>
		internal static int ModifySpellPower(Character caster, Character target, int spellPower) {
			if (!caster.IsPlayer || (spellPower <= 0)) {
				return spellPower;
			}
			return (int) ApplySlayers(caster, target, spellPower);
		}

		/// <summary>ra_ + 2 x mi_.</summary>
		internal static int LeechSum(Character ch, string ability, int property) {
			return ClassAbilities.Points(ch, ability) + (2 * MagicItems.WearerValue(ch, property));
		}

		/// <summary>e_hitsleech, e_manaleech and e_stamleech after a landed blow on a monster: class points and items together, so
		/// a direct call for every player (MagicWearerPlugin exists only while magic is worn).</summary>
		internal static void Leech(Character attacker, Character defender, int damage) {
			if (!attacker.IsPlayer || (damage <= 0) || defender.IsDeleted || attacker.IsDeleted) {
				return;
			}
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!defender.IsPlayer) {
				int hits = Leech(damage, LeechSum(attacker, "hitsleech", 111), settings);
				if ((hits > 0) && (attacker.Hits < attacker.MaxHits)) {
					hits = Math.Min(Math.Min(hits, damage), settings.hitsLeechMax);
					attacker.Hits = (short) Math.Min(attacker.MaxHits, attacker.Hits + hits);
					attacker.SysMessage("Vysavas zivoty!");
				}
				int mana = Leech(damage, LeechSum(attacker, "manaleech", 112), settings);
				if ((mana > 0) && (defender.GetSkill(SkillName.Magery) > 0) && (defender.Mana > 0) && (attacker.Mana < attacker.MaxMana)) {
					mana = Math.Min(Math.Min(mana, defender.Mana), settings.manaLeechMax);
					attacker.SysMessage("Vysavas manu");
					attacker.Mana = (short) Math.Min(attacker.MaxMana, attacker.Mana + mana);
				}
				int stam = Leech(damage, LeechSum(attacker, "stamleech", 113), settings);
				if ((stam > 0) && (defender.Stam > 0) && (attacker.Stam < attacker.MaxStam)) {
					stam = Math.Min(Math.Min(stam, defender.Stam), settings.stamLeechMax);
					attacker.SysMessage("Vysavas staminu");
					attacker.Stam = (short) Math.Min(attacker.MaxStam, attacker.Stam + stam);
				}
			}
		}

		/// <summary>e_paralysing after a landed blow (MagicWearerPlugin's @afterHit).</summary>
		internal static void TryFreeze(Character attacker, Character defender, int damage) {
			if (!attacker.IsPlayer || (damage <= 0) || defender.IsDeleted || attacker.IsDeleted) {
				return;
			}
			int paralysis = MagicItems.WearerValue(attacker, 121);
			if ((paralysis > 0) && !(defender.IsPlayer && (attacker.FindLayer(LayerNames.Hand2) is ColoredStaff)) &&
					(paralysis > Globals.dice.Next(0, 100)) && (defender.ResistParalyse < 700 + Globals.dice.Next(-200, 201))) {
				ParalyzeEffectPluginDef.Paralyze(defender, paralysisPK, TimeSpan.FromSeconds(2));
			}
		}

		/// <summary>ubrano * sum * nastaveni_ability_leechpower / 200000.</summary>
		internal static int Leech(int damage, int sum, ClassAbilitySettings settings) {
			return (int) (((long) damage * sum * settings.leechPower) / 200000);
		}

		/// <summary>e_spellreflex and e_spellblock's @BeforeGetEffect: true when the spell won't land on the target.</summary>
		internal static bool ReflectsOrBlocks(SpellDef spell, Character target, SpellEffectArgs args, Action<Character> runOn) {
			Character caster = args.Caster;
			if ((caster == null) || (caster == target)) {
				return false;
			}
			if (MagicItems.WearerValue(target, 133) > Globals.dice.Next(0, 100)) {
				caster.RedMessage("Kouzlo magicky odrazeno");
				target.SysMessage("Kouzlo magicky odrazeno", ClassAbilities.ClassMessageHue);
				args.CurrentTarget = caster;
				runOn(caster);
				return true;
			}
			if (((spell.Flags & SpellFlag.IsHarmful) == SpellFlag.IsHarmful) && (MagicItems.WearerValue(target, 136) > Globals.dice.Next(0, 100))) {
				caster.RedMessage("Kouzlo magicky zastaveno");
				target.SysMessage("Kouzlo magicky zastaveno", ClassAbilities.ClassMessageHue);
				return true;
			}
			return false;
		}
	}
}
