using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>One line of def_rearAbilities_x and its _class: who, and pairs of minimum level, maximum points.</summary>
	internal sealed class RearRule {
		internal readonly string[] Classes;
		internal readonly int[] Limits;

		internal RearRule(string classes, params int[] limits) {
			this.Classes = (classes.Length == 0) ? new string[0] : classes.Split(',');
			this.Limits = limits;
		}
	}

	internal sealed class RearAbility {
		internal readonly string Name, Description;
		internal readonly RearRule[] Rules;

		internal RearAbility(string name, string description, params RearRule[] rules) {
			this.Name = name;
			this.Description = description;
			this.Rules = rules;
		}

		/// <summary>f_ra_add_hasClass: the first rule naming the class. Only a first rule may name nobody,
		/// which means everyone.</summary>
		internal RearRule RuleFor(string classKey) {
			for (int i = 0; i < this.Rules.Length; i++) {
				RearRule rule = this.Rules[i];
				if ((i == 0) && (rule.Classes.Length == 0)) {
					return rule;
				}
				if (Array.IndexOf(rule.Classes, classKey) >= 0) {
					return rule;
				}
			}
			return null;
		}
	}

	internal sealed class ClassInfoRow {
		internal readonly string Name, Ability, Command, Description;
		internal readonly int MaxPoints;

		internal ClassInfoRow(string name, string ability, int maxPoints, string command, string description) {
			this.Name = name;
			this.Ability = ability;
			this.MaxPoints = maxPoints;
			this.Command = command;
			this.Description = description;
		}
	}

	/// <summary>#1017 - Erebor's vedlejsi ability, the ra_ tags of experiences.scp. Two points a level to spend,
	/// each ability limited by class and level. What we already had as an AbilityDef keeps it, so the code
	/// reading those works as it did; the rest are tags named as on Erebor.</summary>
	public static class ClassAbilities {
		private static readonly Dictionary<string, string> abilityDefs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ "manareg", "a_regen_mana" }, { "hitsreg", "a_regen_hits" }, { "stamreg", "a_regen_stam" },
			{ "weapdodge", "a_weapon_dodge" }, { "speldodge", "a_spell_absorption" },
			{ "hitsleech", "a_leech_hits" }, { "manaleech", "a_leech_mana" }, { "stamleech", "a_leech_stam" },
			{ "warcry", "a_warcry" }, { "rangearrow", "a_longrange" }, { "throwing", "a_throwing" },
			{ "energyshield", "a_energy_shield" },
			{ "mystikDoubleswing", "a_mystik_doubleswing" }, { "thief_rebound", "a_thief_rebound" }, { "thief_defstance", "a_thief_defstance" },
		};

		internal const int ClassMessageHue = 0x57;

		/// <summary>#1070 - fizzmagictravel: a recall or a gate being cast stops. Erebor's thrown knife and knockback do it
		/// with a long range running, and so does a mystic's special strike on a player.</summary>
		internal static void FizzMagicTravel(Character target) {
			if ((target.CurrentSkillName == SkillName.Magery) && (target.CurrentSkillArgs != null) &&
					(target.CurrentSkillArgs.Param1 is SpellDef spell) && ((spell.Id == 32) || (spell.Id == 52))) {
				target.AbortSkill();
			}
		}

		/// <summary>#1070 - e_allplayers' @beforegeteffect: a monster's spell is swallowed whole by points * pvMdodge out
		/// of 1000 of speldodge (the permille weapon dodge uses too). Not while possessing.</summary>
		internal static bool TryAbsorbSpell(Character target, Character caster) {
			if (!(target is Player) || !(caster is NPC) || MysticHypnosis.IsPossessing(target)) {
				return false;
			}
			if ((Points(target, "speldodge") * CombatSettings.instance.weaponDodgePerPoint) <= Globals.dice.Next(1000)) {
				return false;
			}
			target.SysMessage("Kouzlo pohlceno.", ClassMessageHue);
			return true;
		}

		/// <summary>A serv.time tag: when something may be used again.</summary>
		internal static double SecondsTag(Character ch, TagKey key) {
			object value = ch.GetTag(key);
			return (value is double seconds) ? seconds : 0;
		}

		/// <summary>reduceStamina(x,1): all of it or none, and it says so.</summary>
		internal static bool ReduceStamina(Character ch, int amount) {
			if (amount > ch.Stam) {
				ch.RedMessage("Nemas dost staminy.");
				return false;
			}
			ch.Stam = (short) (ch.Stam - amount);
			return true;
		}

		/// <summary>reduceStamina(x) with no second argument: whatever is there, down to 0.</summary>
		internal static void ReduceStaminaToZero(Character ch, int amount) {
			ch.Stam = (short) Math.Max(0, ch.Stam - amount);
		}

		/// <summary>f_abilitycheck: true when it was just turned on. A second use turns it off, for free. The counter is
		/// the profession plugin's since #1438, so a new profession starts without one.</summary>
		internal static bool ToggleCounter(Player self, ProfessionDef profession) {
			ProfessionPlugin plugin = ProfessionPlugin.GetInstalledPlugin(self);
			if (self.Level < 1) {
				self.RedMessage("Musis byt alespon na prvni urovni.");
			} else if ((plugin == null) || (self.Profession != profession)) {
				self.RedMessage("Toto umi pouzit pouze " + profession.Name + ".");
			} else if (plugin.counterOn) {
				plugin.counterOn = false;
			} else if (ReduceStamina(self, 3)) {
				plugin.counterOn = true;
				return true;
			}
			return false;
		}

		internal static bool CounterOn(Player self) {
			ProfessionPlugin plugin = ProfessionPlugin.GetInstalledPlugin(self);
			return (plugin != null) && plugin.counterOn;
		}

		/// <summary>specability: chance out of 100000, by level, while the counter's on.</summary>
		internal static bool CounterFires(Player self) {
			return CounterOn(self) && (Globals.dice.Next(100001) < CombatCalculator.CalculateClassAbility(self.EffectiveLevel));
		}

		/// <summary>A player's @AfterSwing on a blow that's landing: the critical blow, from the counter the classes share.
		/// Each class's own part follows in its profession plugin's @swingLanding (warrior #1429, ranger #1431, thief #1433).</summary>
		internal static void ModifyOwnBlow(Player self, Character target, WeaponSwingArgs swing) {
			if (CounterFires(self)) {
				if (RangerAbilities.IsShooting(self)) {
					RangerAbilities.CriticalShot(self, target, swing);
				} else {
					self.SysMessage("Kriticky zasah!", ClassMessageHue);
					swing.DamageAfterAC = (swing.DamageAfterAC * ClassAbilitySettings.instance.criticalPermille) / 1000;
				}
			}
		}

		/// <summary>What a death ends for the classes still on direct calls. The counter goes in the profession plugin's
		/// @death (#1438).</summary>
		internal static void OnDeath(Character self) {
			PriestAbilities.NoteDeath(self);
		}

		private static TagKey TagOf(string name) {
			return TagKey.Acquire("ra_" + name.ToLowerInvariant());
		}

		private static AbilityDef DefOf(string name) {
			string defname;
			return abilityDefs.TryGetValue(name, out defname) ? AbilityDef.GetByDefname(defname) : null;
		}

		/// <summary>tag(ra_name): points in one of Erebor's abilities, "stuntattack" for ra_stuntattack.</summary>
		public static int Points(Character ch, string name) {
			AbilityDef def = DefOf(name);
			if (def != null) {
				return ch.GetAbility(def);
			}
			return Convert.ToInt32(ch.GetTag(TagOf(name)) ?? 0);
		}

		internal static void SetPoints(Character ch, string name, int points) {
			AbilityDef def = DefOf(name);
			if (def != null) {
				ch.SetRealAbilityPoints(def, points);
			} else if (points == 0) {
				ch.RemoveTag(TagOf(name));
			} else {
				ch.SetTag(TagOf(name), points);
			}
		}

		/// <summary>ratotal.</summary>
		public static int Spent(Character ch) {
			int total = 0;
			foreach (RearAbility ability in ClassAbilityTable.Rear) {
				total += Points(ch, ability.Name);
			}
			return total;
		}

		public static int Spare(Player player) {
			return (player.Level * 2) - Spent(player);
		}

		/// <summary>zakladabilities.</summary>
		public static void Reset(Character ch) {
			foreach (RearAbility ability in ClassAbilityTable.Rear) {
				SetPoints(ch, ability.Name, 0);
			}
		}

		/// <summary>The word Erebor's tables use for a class, or null.</summary>
		internal static string ClassKey(Player player) {
			ProfessionDef profession = player.Profession;
			if (profession == null) {
				return null;
			}
			if (profession == Professions.Mage) {
				return "mag";
			} else if (profession == Professions.Shaman) {
				return "shaman";
			} else if (profession == Professions.Thief) {
				return "thief";
			} else if (profession == Professions.Priest) {
				return "priest";
			} else if (profession == Professions.Warrior) {
				return "war";
			} else if (profession == Professions.Craftsman) {
				return "craft";
			} else if (profession == Professions.Necromant) {
				return "necro";
			} else if (profession == Professions.Ranger) {
				return "ranger";
			} else if (profession == Professions.Mystic) {
				return "mystik";
			}
			return null;
		}

		/// <summary>The abilities this player's class has at all, in Erebor's order.</summary>
		internal static List<RearAbility> Available(Player player) {
			List<RearAbility> available = new List<RearAbility>();
			string classKey = ClassKey(player);
			foreach (RearAbility ability in ClassAbilityTable.Rear) {
				if ((classKey != null) && (ability.RuleFor(classKey) != null)) {
					available.Add(ability);
				}
			}
			return available;
		}

		internal static RearAbility Find(string name) {
			foreach (RearAbility ability in ClassAbilityTable.Rear) {
				if (string.Equals(ability.Name, name, StringComparison.OrdinalIgnoreCase)) {
					return ability;
				}
			}
			return null;
		}

		/// <summary>f_ra_add_meetsRestrictions. Pairs go up by level: the first one the player is too young for
		/// ends it, as a higher limit can't apply then.</summary>
		internal static bool MayRaise(Player player, RearAbility ability, bool tell) {
			RearRule rule = ability.RuleFor(ClassKey(player));
			int points = Points(player, ability.Name);
			for (int i = 0; i + 1 < rule.Limits.Length; i += 2) {
				if (player.Level < rule.Limits[i]) {
					if (tell) {
						player.RedMessage("Nejsi dost " + (player.IsFemale ? "zkusena" : "zkuseny") + ", aby jsi " +
							(player.IsFemale ? "navysila" : "navysil") + " tuto abilitu.");
					}
					return false;
				}
				if (points < rule.Limits[i + 1]) {
					return true;
				}
			}
			if (tell) {
				player.RedMessage("Vic bodu uz do teto ability pridat nelze.");
			}
			return false;
		}

		/// <summary>ra_add.</summary>
		internal static bool Raise(Player player, RearAbility ability) {
			if (Spare(player) <= 0) {
				player.RedMessage("Jiz nemas dalsi body k rozdeleni.");
				return false;
			}
			string classKey = ClassKey(player);
			if ((classKey == null) || (ability.RuleFor(classKey) == null)) {
				player.RedMessage("Tato abilita neni pro tebe!");
				return false;
			}
			if (!MayRaise(player, ability, true)) {
				return false;
			}
			SetPoints(player, ability.Name, Points(player, ability.Name) + 1);
			return true;
		}

		[SteamFunction]
		public static void Class_Info(Player self) {
			D_ClassInfo.Show(self);
		}
	}
}
