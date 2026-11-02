using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>T_potion_shrink: turns a pet into a figurine. Picks the pet, and a wrong one asks again.</summary>
	public sealed class ShrinkPotionKind : PotionKind {
		public override void Use(Character user, Potion potion) {
			if (user is Player player) {
				player.Target(SingletonScript<Targ_ShrinkPet>.Instance, potion);
			}
		}

		public override void Drink(Character drinker, Potion potion) {
		}
	}

	public class Targ_ShrinkPet : CompiledTargetDef {
		private const int Range = 3;

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			Potion potion = (Potion) parameter;
			NPC pet = targetted as NPC;
			if (potion.IsDeleted) {
				return TargetResult.Done;
			}
			if ((pet == null) || !pet.IsPetOf(self) || AnimateDeadSpellDef.IsAnimated(pet) || SummonSpellDef.IsSummonedCreature(pet) ||
					(Point2D.GetSimpleDistance(self.X, self.Y, pet.X, pet.Y) > Range) || !self.CanSeeLOS(pet).Allow) {
				return TargetResult.RestartTargetting;
			}
			PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(self.Backpack);
			figurine.Keep(pet);
			pet.Delete();
			potion.CreateEmptyFlask(self.Backpack);
			potion.Consume(1);
			return TargetResult.Done;
		}
	}

	public static class ShrinkPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			add("i_potion_Shrink", new ShrinkPotionKind());
		}
	}
}
