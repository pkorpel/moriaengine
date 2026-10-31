using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#927 - t_allpotions and t_speedpotions: one shared wait between potions, a longer one after
	/// the former, a bottle that can break, and a penalty for hammering the longer ones.</summary>
	public static class PotionDrinking {
		private const int DrinkSound = 0x31, TooSoonSound = 0x50;
		private static readonly TagKey nextPotionTK = TagKey.Acquire("_next_potion_");
		private static readonly TagKey firstAttemptTK = TagKey.Acquire("_potion_first_attempt_");
		private static readonly TagKey attemptsTK = TagKey.Acquire("_potion_attempts_");

		private sealed class CooldownSource : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				double left = NextPotion(ch) - Globals.TimeInSeconds;
				if (left > 0) {
					into.Add(new ActiveEffect(EffectSection.Cooldown, "Dalsi lektvar") { SecondsLeft = left });
				}
			}
		}

		private static readonly CooldownSource cooldowns = new CooldownSource();

		public static void Bootstrap() {
			ActiveEffects.Register(cooldowns);
		}

		internal static double Time(TagHolder holder, TagKey key) {
			object time = holder.GetTag(key);
			return (time != null) ? ConvertTools.ToDouble(time) : 0;
		}

		public static double NextPotion(Character ch) {
			return Time(ch, nextPotionTK);
		}

		internal static void ForgetAttempts(Character ch) {
			ch.RemoveTag(firstAttemptTK);
			ch.RemoveTag(attemptsTK);
		}

		public static void Wait(Character ch, double seconds) {
			ch.SetTag(nextPotionTK, Globals.TimeInSeconds + seconds);
		}

		public static bool CanDrinkNow(Character ch) {
			return Globals.TimeInSeconds > NextPotion(ch);
		}

		private static bool IsLongOne(Potion potion) {
			return VarGate.Matches(potion, "t_allpotions", true);
		}

		/// <summary>The third try within 1.5 seconds costs a player the wait, up to a limit.</summary>
		private static void CountAttempt(Character ch) {
			double now = Globals.TimeInSeconds;
			double first = Time(ch, firstAttemptTK);
			int attempts = TagMath.IGetTag(ch, attemptsTK);
			if (now < (first + 1.5)) {
				if (attempts < 2) {
					ch.SetTag(attemptsTK, attempts + 1);
				} else {
					PotionSettings settings = PotionSettings.instance;
					ch.RedMessage("Tva ukvapenost te zpomalila!");
					if ((now - NextPotion(ch)) < settings.maxSpamPenaltySeconds) {
						ch.SetTag(nextPotionTK, NextPotion(ch) + settings.spamPenaltySeconds);
						ch.RemoveTag(firstAttemptTK);
						ch.RemoveTag(attemptsTK);
					}
				}
			} else {
				ch.SetTag(firstAttemptTK, now);
				ch.SetTag(attemptsTK, 1);
			}
		}

		public static void TryDrink(Character drinker, Potion potion, PotionKind kind) {
			bool isLong = IsLongOne(potion);
			if (isLong) {
				CountAttempt(drinker);
			}
			if (!CanDrinkNow(drinker)) {
				PacketSequences.SendSound(drinker, TooSoonSound, Globals.MaxUpdateRange);
				return;
			}
			PacketSequences.SendSound(drinker, DrinkSound, Globals.MaxUpdateRange);
			PotionSettings settings = PotionSettings.instance;
			if (Globals.dice.Next(100) < settings.bottleBreakChance) {
				drinker.SysMessage("Lahev se rozbila.");
			} else {
				potion.CreateEmptyFlask(drinker.Backpack);
			}
			Wait(drinker, isLong ? settings.longDelay : settings.delay);
			kind.Drink(drinker, potion);
			potion.Consume(1);
		}
	}
}
