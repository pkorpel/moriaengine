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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>Player-facing ".Feed" command (#593) - mirrors the real archive's own
	/// "target krmeni" item-triggered feed interaction (funkce.scp): target a pet within
	/// range, the engine searches the feeder's own backpack for a matching food item and
	/// applies the real nakrmho formula.</summary>
	public static class PetFeedingCommands {
		[SteamFunction]
		public static void Feed(Player self) {
			self.SysMessage("Target the animal you want to feed.");
			self.Target(SingletonScript<Targ_FeedPet>.Instance);
		}
	}

	public class Targ_FeedPet : CompiledTargetDef {
		//Real source's own distance check (funkce.scp's "target krmeni" item,
		//src.distancefrom(<src.targ>) < 4).
		private const int MaxFeedDistance = 4;

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			NPC pet = targetted as NPC;
			if (pet == null) {
				self.SysMessage("That is not an animal.");
				return TargetResult.Done;
			}
			if (Point2D.GetSimpleDistance(self, pet) >= MaxFeedDistance) {
				self.SysMessage("You are too far away.");
				return TargetResult.Done;
			}
			if (PetFeedingSystem.TryFeed(self, pet)) {
				self.SysMessage("You feed " + pet.Name + ".");
			} else {
				self.SysMessage("You don't have anything on you that this animal would want to eat.");
			}
			return TargetResult.Done;
		}
	}

	/// <summary>Real "nakrmho" feeding formula (funkce.scp), ported for #593. Finds items in
	/// the feeder's own backpack matching one of the target NPC's configured FoodTypes
	/// entries (NPCDef.FoodTypes), consumes enough of each match (nutrition per unit = the
	/// item's own real Weight, floored at 10 - same floor the real formula uses) to raise
	/// PetHungerTimer.food up to NPCDef.MaxFood, rounding up one extra unit so the animal
	/// ends up fully fed rather than slightly short (matches the real formula's own "we want
	/// the animal totally full" comment).</summary>
	public static class PetFeedingSystem {
		private const int MinFoodWeight = 10;

		public static bool TryFeed(Character feeder, NPC pet) {
			NPCDef def = (NPCDef) pet.Def;
			List<NPCDef.FoodTypeEntry> foodTypes = def.FoodTypes;
			if ((foodTypes == null) || (foodTypes.Count == 0)) {
				return false; //this species was never configured with real food data
			}
			PetHungerTimer hunger = PetHungerTimer.GetFor(pet);
			if (hunger == null) {
				return false; //not a tracked pet (e.g. not actually owned)
			}
			int maxFood = def.MaxFood;
			if (hunger.food >= maxFood) {
				return false; //not hungry
			}

			Container backpack = feeder.Backpack;
			if (backpack == null) {
				return false;
			}

			bool fedAnything = false;
			foreach (NPCDef.FoodTypeEntry entry in foodTypes) {
				if (hunger.food >= maxFood) {
					break;
				}
				//Snapshot the backpack contents before consuming - Item.Consume can delete a
				//fully-drained stack mid-iteration (same caution GoldUtils.cs's own consume
				//loop already documents).
				List<Item> candidates = new List<Item>();
				foreach (AbstractItem item in backpack) {
					Item realItem = item as Item;
					if ((realItem != null) && FoodCategories.Matches(realItem.TypeDef as ItemDef, entry.CategoryOrDefname)) {
						candidates.Add(realItem);
					}
				}

				foreach (Item foodItem in candidates) {
					if (hunger.food >= maxFood) {
						break;
					}
					if (foodItem.IsDeleted || (foodItem.Amount <= 0)) {
						continue;
					}

					int foodWeight = (int) foodItem.Weight;
					if (foodWeight < MinFoodWeight) {
						foodWeight = MinFoodWeight;
					}

					int neededWeight = (maxFood - hunger.food) * 10;
					long neededCount = neededWeight / foodWeight;
					if (((neededCount * foodWeight) / 10) < (maxFood - hunger.food)) {
						neededCount++; //round up one extra unit so the animal ends up fully fed
					}
					if (neededCount > foodItem.Amount) {
						neededCount = foodItem.Amount;
					}
					if (neededCount <= 0) {
						continue;
					}

					long consumed = foodItem.Consume(neededCount);
					hunger.food += (int) ((consumed * foodWeight) / 10);
					if (hunger.food > maxFood) {
						hunger.food = maxFood;
					}
					fedAnything = true;
				}
			}

			if (fedAnything) {
				pet.Emote("eats some food");
			}
			return fedAnything;
		}
	}
}
