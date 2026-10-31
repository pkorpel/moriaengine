using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	/// <summary>#927 - what a potion does: Erebor's ON=@potioneffect. Most are drunk.</summary>
	public abstract class PotionKind {
		private const int SparkleEffect = 0x373A;

		public virtual void Use(Character user, Potion potion) {
			PotionDrinking.TryDrink(user, potion, this);
		}

		public abstract void Drink(Character drinker, Potion potion);

		public virtual void On_PutOnGround(Character by, Potion potion) {
		}

		protected static void Sparkle(Character drinker) {
			EffectFactory.StationaryEffect(drinker, SparkleEffect, 6, 15);
		}

		/// <summary>Sphere's "time,min,max" of nastaveni_potions_*, as on .settings.</summary>
		protected static int[] Numbers(string text) {
			string[] parts = text.Split(',');
			int[] numbers = new int[parts.Length];
			for (int i = 0; i < parts.Length; i++) {
				numbers[i] = ConvertTools.ToInt32(parts[i].Trim());
			}
			return numbers;
		}

		protected static int Roll(int[] numbers) {
			return (numbers.Length < 3) ? numbers[numbers.Length - 1] : Globals.dice.Next(numbers[1], numbers[2] + 1);
		}
	}

	/// <summary>Which def does what. Keys are defnames: a def's own name needn't be the one it's asked for by.</summary>
	public static class PotionKinds {
		private static Dictionary<ThingDef, PotionKind> kinds;

		public static PotionKind Find(Potion potion) {
			if (kinds == null) {
				kinds = new Dictionary<ThingDef, PotionKind>();
				StatPotions.AddTo(Add);
				HealPotions.AddTo(Add);
				RefreshPotions.AddTo(Add);
				NightsightPotions.AddTo(Add);
				InvisibilityPotions.AddTo(Add);
				SkinPotions.AddTo(Add);
				MobilityPotions.AddTo(Add);
				SpellEffectPotions.AddTo(Add);
				CurePotions.AddTo(Add);
				DragonPotions.AddTo(Add);
				BombPotions.AddTo(Add);
				ShrinkPotions.AddTo(Add);
				HpSightPotions.AddTo(Add);
				SpellBottles.AddTo(Add);
			}
			kinds.TryGetValue(potion.Def, out PotionKind kind);
			return kind;
		}

		private static void Add(string defname, PotionKind kind) {
			ThingDef def = ThingDef.GetByDefname(defname);
			if (def != null) {
				kinds[def] = kind;
			}
		}
	}
}
