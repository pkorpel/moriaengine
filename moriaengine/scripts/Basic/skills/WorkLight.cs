using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's nolightfizz: is there light enough to work by? Night sight, a lit thing within 2 tiles or in the
	/// off hand always do. Otherwise it's a roll, so even daylight fails 1 time in 65, as the script's {0 64} does.</summary>
	public static class WorkLight {
		private const int NearEnough = 2, RollOf = 65;

		public static bool CheckWithMessage(Character self) {
			if (CanSee(self)) {
				return true;
			}
			self.SysMessage(Loc<WorkLightLoc>.Get(self.Language).TooDark);
			return false;
		}

		public static bool CanSee(Character self) {
			if (NightsightPotions.HasNightSight(self) || (LightAndWeather.GetLightAt(self) < Globals.dice.Next(RollOf))) {
				return true;
			}
			Point2D light;
			return CraftingSkillDef.FindNearby(self, "t_light_lit", NearEnough, out light)
				|| CraftingSkillDef.IsOfType(self.FindLayer(LayerNames.Hand2) as Item, "t_light_lit");
		}
	}

	internal class WorkLightLoc : CompiledLocStringCollection<WorkLightLoc> {
		public string TooDark = "Je spatne videt.";
	}
}
