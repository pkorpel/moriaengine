using System;
using System.Collections.Generic;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	public enum EffectSection {
		Positive,
		Negative,
		Neutral,
		Cooldown,
		Status
	}

	/// <summary>#923 - one line of .boosts. SecondsLeft under 0 means it has no end of its own.</summary>
	public sealed class ActiveEffect {
		public readonly EffectSection Section;
		public readonly string Name;
		public double SecondsLeft = -1;
		public string Detail = "";
		public string Source = "";
		public int Icon;

		public ActiveEffect(EffectSection section, string name) {
			this.Section = section;
			this.Name = name;
		}
	}

	/// <summary>#923 - a plugin on a character, or an item they wear, that shows on .boosts. All an
	/// EffectDurationPlugin has to do is exist, it's implemented on the base. null is not shown.</summary>
	public interface IEffectDescriber {
		ActiveEffect DescribeEffect(Character holder);
	}

	/// <summary>#923 - whatever isn't a plugin or a worn item: register one with ActiveEffects.Register.</summary>
	public interface IActiveEffectSource {
		void AddEffects(Character ch, List<ActiveEffect> into);
	}

	/// <summary>#923 - everything affecting a character. Erebor's .boosty listed each thing by hand, twice;
	/// here a new effect shows by itself, and the dialog never changes.</summary>
	public static class ActiveEffects {
		private static readonly List<IActiveEffectSource> sources = new List<IActiveEffectSource> {
			new DescribedThingsSource(), new CharacterStateSource(), new AbilityCooldownSource()
		};

		public static void Register(IActiveEffectSource source) {
			if (!sources.Contains(source)) {
				sources.Add(source);
			}
		}

		public static List<ActiveEffect> Of(Character ch) {
			List<ActiveEffect> effects = new List<ActiveEffect>();
			foreach (IActiveEffectSource source in sources) {
				source.AddEffects(ch, effects);
			}
			effects.Sort((a, b) => {
				int section = a.Section.CompareTo(b.Section);
				return (section != 0) ? section : string.CompareOrdinal(a.Name, b.Name);
			});
			return effects;
		}

		public static string FormatTime(double seconds) {
			if (seconds < 0) {
				return "trvale";
			}
			int whole = (int) Math.Ceiling(seconds);
			return (whole < 60) ? (whole + "s") : ((whole / 60) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture));
		}

		public static string SignedStat(int difference, string stat) {
			if (difference == 0) {
				return "";
			}
			return (difference > 0) ? ("+" + difference + " " + stat) : (difference + " " + stat);
		}

		public static string Join(params string[] parts) {
			return string.Join(", ", Array.FindAll(parts, part => part.Length > 0));
		}

		private sealed class DescribedThingsSource : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				foreach (Plugin plugin in ch.GetAllPlugins()) {
					Add(plugin as IEffectDescriber, ch, into);
				}
				foreach (AbstractItem item in ch) {
					if (!item.IsInLimbo && !(item is BankBox)) {
						Add(item as IEffectDescriber, ch, into);
					}
				}
			}

			private static void Add(IEffectDescriber describer, Character ch, List<ActiveEffect> into) {
				ActiveEffect effect = describer?.DescribeEffect(ch);
				if (effect != null) {
					into.Add(effect);
				}
			}
		}

		/// <summary>What's kept in flags and timers on the character, not in a plugin.</summary>
		private sealed class CharacterStateSource : IActiveEffectSource {
			private const int InvisibilityIcon = 0x8EB, HiddenIcon = 0x500E;

			public void AddEffects(Character ch, List<ActiveEffect> into) {
				if (ch.Flag_InvisByMagic) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Invisibility") { SecondsLeft = InvisibilitySpellDef.SecondsLeft(ch), Icon = InvisibilityIcon });
				}
				if (ch.Flag_Hidden) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Hidden") { Icon = HiddenIcon });
				}
				if (ThiefDefence.IsVigilant(ch)) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Vigilance"));
				}
				if (Notoriety.IsCriminal(ch)) {
					into.Add(new ActiveEffect(EffectSection.Negative, "Zlocinec") { SecondsLeft = Notoriety.CriminalSecondsLeft(ch) });
				}
				if (Notoriety.IsMurderer(ch)) {
					into.Add(new ActiveEffect(EffectSection.Negative, "Vrah") { Detail = "Vrazd: " + Notoriety.GetMurderCount(ch) });
				}
				Character mount = ch.Mount as Character;
				if (mount != null) {
					into.Add(new ActiveEffect(EffectSection.Status, "Jezditko") { Detail = mount.Name });
				}
			}
		}

		private sealed class AbilityCooldownSource : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				foreach (Ability ability in ch.Abilities) {
					AbilityDef def = ability.Def;
					if ((def.Cooldown > 0) && (ability.LastUsage != TimeSpan.MinValue)) {
						double left = def.Cooldown - (Globals.TimeAsSpan - ability.LastUsage).TotalSeconds;
						if (left > 0) {
							into.Add(new ActiveEffect(EffectSection.Cooldown, def.Name) { SecondsLeft = left, Icon = def.Icon });
						}
					}
				}
			}
		}
	}

	public partial class DamagingPoisonEffectPlugin {
		public override string EffectName {
			get {
				return "Poison";
			}
		}
	}

	public partial class BleedingEffectPlugin {
		public override string EffectName {
			get {
				return "Bleeding";
			}
		}
	}

	public partial class NpcBleedEffectPlugin {
		public override string EffectName {
			get {
				return "Bleeding";
			}
		}
	}

	public partial class DiscordanceEffectPlugin : IEffectDescriber {
		public ActiveEffect DescribeEffect(Character holder) {
			return new ActiveEffect(EffectSection.Negative, "Discordance") {
				SecondsLeft = this.Timer,
				Detail = ActiveEffects.Join(
					ActiveEffects.SignedStat(-this.lowed_str, "Str"),
					ActiveEffects.SignedStat(-this.lowed_dex, "Dex"),
					ActiveEffects.SignedStat(-this.lowed_int, "Int"))
			};
		}
	}

}
