using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#924 - Sphere's food stat on players and Erebor's use of it: ishungry, ishungrymsg, hungryhurts.
	/// Someone who never ate holds no tag and counts as full, so old saves and new characters start at 15.</summary>
	public static class PlayerHunger {
		public const int MaxFood = 15; //c_man's FOODTYPE=15 t_food, t_fruit

		private const int LevelsOfMessage = 8, HungryBelowPercent = 40, HungryRollOf = 10, HungryMargin = 2;
		private static readonly TagKey foodTK = TagKey.Acquire("_food_");
		private static readonly int[] eatSounds = { 0x3a, 0x3b, 0x3c };

		private sealed class Source : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				if (ch is Player) {
					into.Add(new ActiveEffect(EffectSection.Status, Loc<HungerLoc>.Get(ch.Language).FoodLine) { Detail = LevelText(ch) });
				}
			}
		}

		private static readonly Source source = new Source();

		public static void Bootstrap() {
			ActiveEffects.Register(source);
		}

		public static int FoodOf(Character ch) {
			object food = ch.GetTag(foodTK);
			return (food == null) ? MaxFood : (int) food;
		}

		public static void SetFood(Character ch, int food) {
			ch.SetTag(foodTK, food);
			ch.HitsRegenSpeed = ch.HitsRegenSpeed; //starving held it, it may have to start again
		}

		/// <summary>Sphere stops a player's hits regenerating at nothing left.</summary>
		public static bool StopsHealing(Character ch) {
			return (ch is Player) && (FoodOf(ch) == 0);
		}

		public static string LevelText(Character ch) {
			int level = System.Math.Min((FoodOf(ch) * LevelsOfMessage) / MaxFood, LevelsOfMessage - 1);
			HungerLoc texts = Loc<HungerLoc>.Get(ch.Language);
			string[] levels = { texts.Starving, texts.VeryHungry, texts.Hungry, texts.FairlyContent, texts.Content, texts.Fed, texts.WellFed, texts.Stuffed };
			return levels[level];
		}

		/// <summary>Erebor's ishungry: from 7 food never, at none 7 times in 10.</summary>
		public static bool IsHungry(Character ch) {
			return (ch is Player) && !ch.IsGM && (Globals.dice.Next(HungryRollOf) > (FoodOf(ch) + HungryMargin));
		}

		/// <summary>ishungrymsg, on what a skill just did well: hunger makes it a failure.</summary>
		public static bool DistractsFrom(Character ch) {
			if (IsHungry(ch)) {
				ch.RedMessage(Loc<HungerLoc>.Get(ch.Language).Distracted);
				return true;
			}
			return false;
		}

		/// <summary>hungryhurts, trying a blow, a spell or a job on nothing.</summary>
		public static void HurtIfStarving(Character ch) {
			if ((ch is Player) && (FoodOf(ch) < 1) && !ch.Flag_Dead) {
				ch.SysMessage(Loc<HungerLoc>.Get(ch.Language).Dying);
				DamageManager.CauseDamage(ch, ch, DamageType.Irresistable, HungerSettings.instance.starvingDamage);
			}
		}

		/// <summary>Sphere's Use_Eat of one piece: it fills by its weight in stones, at least 1, and may overfill.</summary>
		public static void Eat(Item food, Character eater) {
			HungerLoc texts = Loc<HungerLoc>.Get(eater.Language);
			if (food.Flag_NonMovable) {
				eater.SysMessage(texts.CantMove);
				return;
			}
			int had = FoodOf(eater);
			if (had >= MaxFood) {
				eater.SysMessage(texts.TooFull);
				return;
			}
			int value = System.Math.Max(1, (int) food.Def.Weight);
			string name = food.Name;
			if (food.Amount > 1) {
				food.Amount--;
			} else {
				food.Delete();
			}
			int now = had + value;
			SetFood(eater, now);
			eater.Sound(eatSounds[Globals.dice.Next(eatSounds.Length)]);
			AnimCalculator.PerformAnim(eater, GenericAnim.Eat);
			eater.Emote(string.Format(texts.EatSome, name));
			string[] feelings = { texts.StillHungry, texts.LessHungry, texts.Satiated, texts.Satiated, texts.NearlyStuffed, texts.QuiteFull, texts.Stuffed2 };
			eater.SysMessage(feelings[System.Math.Min((now * 6) / MaxFood, feelings.Length - 1)]);
		}

		internal static void StartTicking(Player player) {
			if (player.GetTimer(PlayerHungerTimer.Key) == null) {
				double seconds = HungerSettings.instance.secondsPerFoodPoint;
				PlayerHungerTimer timer = new PlayerHungerTimer();
				player.AddTimer(PlayerHungerTimer.Key, timer).DueInSeconds = seconds;
				timer.PeriodInSeconds = seconds;
			}
		}

		/// <summary>Sphere's OnTickFood. It goes up from nothing, so 0 and 1 take turns while starving.</summary>
		internal static void Tick(Player player) {
			int food = FoodOf(player);
			food = (food > 0) ? (food - 1) : (food + 1);
			SetFood(player, food);
			if (((food * 100) / MaxFood) < HungryBelowPercent) {
				player.SysMessage(string.Format(Loc<HungerLoc>.Get(player.Language).YouAre, LevelText(player)));
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class PlayerHungerTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_player_hunger_");

		[LoadingInitializer, DeepCopyImplementation]
		public PlayerHungerTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Player player = (Player) cont;
			if (player.GameState != null) { //as Sphere, only in the world
				PlayerHunger.Tick(player);
			}
			this.PeriodInSeconds = HungerSettings.instance.secondsPerFoodPoint;
		}
	}

	internal class HungerLoc : CompiledLocStringCollection<HungerLoc> {
		public string FoodLine = "Jidlo";
		public string Starving = "starving";
		public string VeryHungry = "very hungry";
		public string Hungry = "hungry";
		public string FairlyContent = "fairly content";
		public string Content = "content";
		public string Fed = "fed";
		public string WellFed = "well fed";
		public string Stuffed = "stuffed";
		public string YouAre = "You are {0}";
		public string Distracted = "Ve tvem soustredeni te rusi hlad...";
		public string Dying = "Umiras hlady...";
		public string CantMove = "You can't move the item.";
		public string TooFull = "You are simply too full to eat any more!";
		public string EatSome = "eat some {0}";
		public string StillHungry = "You eat the food, but are still extremely hungry.";
		public string LessHungry = "After eating the food, you feel much less hungry.";
		public string Satiated = "You eat the food, and begin to feel more satiated.";
		public string NearlyStuffed = "You are nearly stuffed, but manage to eat the food.";
		public string QuiteFull = "You feel quite full after consuming the food.";
		public string Stuffed2 = "You are stuffed!";
	}
}
