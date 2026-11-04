using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Erebor's [speech spk_strazcelagru] (speech/lagrspeech.scp), the camp guard c_h_strazce_lagru. Its
	/// patterns in Erebor's order, the first that matches answers: hi/ahoj/buy, vymena, sekera, svetlo, jidlo, propustit.
	/// Replaces the converted [SpeechStub spk_strazcelagru].</summary>
	public sealed class CampSpeech : AbstractSpeech {
		public new static void Bootstrap() {
			new CampSpeech("spk_strazcelagru").Register();
		}

		private CampSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		private static bool Says(string speech, string word) {
			return speech.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC guard = listener as NPC;
			Character who = speechArgs.Speaker as Character;
			string speech = speechArgs.Speech;
			if ((guard == null) || (who == null) || (speech == null)) {
				return SpeechResult.IgnoredOrActedUpon;
			}
			CampLoc texts = Loc<CampLoc>.Get(who.Language);
			if (Says(speech, "hi") || Says(speech, "ahoj") || Says(speech, "buy")) {
				guard.Say((Camp.IsRecidivist(who) && (Camp.Sentence(who) <= 0)) ? texts.GuardRecidivist : string.Format(texts.GuardHello, Camp.Sentence(who) / Camp.CoinValue));
			} else if (Says(speech, "vymena")) {
				if (!Camp.Exchange(who)) {
					guard.Say(texts.GuardNotEnoughWood);
				}
			} else if (Says(speech, "sekera")) {
				if (Camp.Count(who, "i_hatchet") > 0) {
					guard.Say(texts.GuardHaveAxe);
				} else {
					try {
						who.NewEquip((IThingFactory) ThingDef.GetByDefname("i_hatchet"));
					} catch (SEException) {
						ThingDef.GetByDefname("i_hatchet").Create(who.Backpack);
					}
					guard.Say(texts.GuardNewAxe);
				}
			} else if (Says(speech, "svetlo")) {
				SpellDef nightSight = SpellDef.GetById(NightSightSpellId); //src.spelleffect 6 1000
				if (nightSight != null) {
					nightSight.ApplyEffectChar(guard, who, 1000, EffectFlag.BeneficialEffect | EffectFlag.FromSpellBook);
				}
			} else if (Says(speech, "jidlo")) {
				PlayerHunger.SetFood(who, PlayerHunger.MaxFood);
			} else if (Says(speech, "propustit")) {
				Release(guard, who, texts);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		private const int NightSightSpellId = 6;

		/// <summary>propustit: enough coins release; under the levels, too few are paid in and the rest is said.</summary>
		private static void Release(NPC guard, Character who, CampLoc texts) {
			int needed = Camp.Sentence(who) / Camp.CoinValue;
			int coins = Camp.Count(who, "i_gold_lagr");
			if (!Camp.IsRecidivist(who)) {
				if (coins >= needed) {
					Camp.Release(who);
					return;
				}
				Camp.SetSentence(who, Camp.Sentence(who) - (coins * Camp.CoinValue));
				Camp.Consume(who, "i_gold_lagr", coins);
				guard.Say(string.Format(texts.GuardOnlyHave, coins));
				guard.Say(string.Format(texts.GuardStillNeed, Camp.Sentence(who) / Camp.CoinValue));
				return;
			}
			if (Camp.Sentence(who) <= 0) {
				guard.Say(texts.GuardRecidivist);
			} else if (coins >= needed) {
				Camp.Release(who);
			} else {
				guard.Say(string.Format(texts.GuardNeed, needed));
			}
		}
	}

	/// <summary>i_prudic_spk (systems/miningfix.scp): one chop in a hundred brings a peasant who nags the convict five times,
	/// five seconds apart, then goes.</summary>
	public static class CampPeasant {
		internal static void Come(Character convict) {
			NPC peasant = (NPC) ThingDef.GetByDefname("c_h_peasant").Create(convict.X, convict.Y, convict.Z, convict.M);
			string name = NameList.Random("NAMES_HUMANMALE");
			if (!string.IsNullOrEmpty(name)) {
				peasant.Name = name;
			}
			CampPeasantTimer timer = new CampPeasantTimer();
			peasant.AddTimer(CampPeasantTimer.Key, timer);
			timer.PeriodInSeconds = CampPeasantTimer.Seconds;
			timer.DueInSeconds = CampPeasantTimer.Seconds;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class CampPeasantTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_campPeasant_");
		internal const double Seconds = 5;
		private const int Times = 5;
		private static readonly string[] lines = {
			"Jsi AFK mukle ?!", "Koukej sekat latinu!", "Jsi tu ? Co je za den ?!", "Nejhorsi je AFK kontrola !", "Je tu Stin ?", "Je spatne videt.",
			"Kontrola ! Jak se ti seka ?!", "Pamatuj, ze AFK se nesmi byt ani zde!", "do lagru s tebou", "Za co jsi tu ? Za AFK ?", "Co drzis v ruce?",
			"Pauza Pause Konec", "Nechtel bych tu stravit pul zivota, jako ty!", "Umiras hlady...", "Za co jsi tu? Ja za ucelem buzerace veznu, chacha",
			"Odpovez! Ja jsem stinna stranka Lagru", "Znas je? Svarog Vdova Yavanna Attila Shadow ?", "Co to je - World save ?" };

		[SaveableData, CopyableData]
		public int said = 1; //Erebor's tag.stav starts at 1

		[LoadingInitializer, DeepCopyImplementation]
		public CampPeasantTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is NPC peasant) || peasant.IsDeleted) {
				return;
			}
			if (this.said >= Times) {
				peasant.Delete();
				return;
			}
			this.said++;
			peasant.Say(lines[Globals.dice.Next(lines.Length)]);
		}
	}
}
