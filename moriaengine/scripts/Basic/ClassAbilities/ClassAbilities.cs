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
			{ "energyshield", "a_energy_shield" }, { "mind_blast_silence", "a_silence" }, { "mind_blast_silenced_time", "a_silence_bonus" },
			{ "mystikDoubleswing", "a_mystik_doubleswing" }, { "thief_rebound", "a_thief_rebound" }, { "thief_defstance", "a_thief_defstance" },
		};

		internal const int ClassMessageHue = 0x57;
		private static readonly TagKey counterTK = TagKey.Acquire("_specAbilityCounter_");

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

		/// <summary>f_abilitycheck: true when it was just turned on. A second use turns it off, for free.</summary>
		internal static bool ToggleCounter(Player self, ProfessionDef profession) {
			if (self.Level < 1) {
				self.RedMessage("Musis byt alespon na prvni urovni.");
			} else if (self.Profession != profession) {
				self.RedMessage("Toto umi pouzit pouze " + profession.Name + ".");
			} else if (self.GetTag(counterTK) != null) {
				self.RemoveTag(counterTK);
			} else if (ReduceStamina(self, 3)) {
				self.SetTag(counterTK, 1);
				return true;
			}
			return false;
		}

		/// <summary>specability: chance out of 100000, by level, while the counter's on.</summary>
		internal static bool CounterFires(Player self) {
			return (self.GetTag(counterTK) != null) && (Globals.dice.Next(100001) < CombatCalculator.CalculateClassAbility(self.EffectiveLevel));
		}

		/// <summary>Death takes the counter away, sphereevents.scp.</summary>
		internal static void OnDeath(Character self) {
			self.RemoveTag(counterTK);
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
