using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's f_vyroba_latky: wool and cotton on a spinning wheel, thread and yarn on a loom. A batch at a time,
	/// its size by Tailoring, the product 3 s later.</summary>
	public static class ClothMaking {
		internal static readonly TimerKey timerKey = TimerKey.Acquire("_cloth_making_");

		private const int SourcesInBatch = 6;

		private sealed class Recipe {
			internal readonly string station, product;
			internal readonly bool fromFibre;

			internal Recipe(string station, string product, bool fromFibre) {
				this.station = station;
				this.product = product;
				this.fromFibre = fromFibre;
			}
		}

		private static Recipe RecipeOf(Item source) {
			switch ((source.Type == null) ? null : source.Type.Defname) {
				case "t_wool": return new Recipe("t_spinwheel", "i_yarn_ball", true);
				case "t_cotton": return new Recipe("t_spinwheel", "i_thread", true);
				case "t_thread":
				case "t_yarn": return new Recipe("t_loom", "i_cloth", false);
				default: return null;
			}
		}

		public static TriggerResult Ask(Item source, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return TriggerResult.Continue;
			}
			ClothMakingLoc texts = Loc<ClothMakingLoc>.Get(player.Language);
			if (source.TopObj() != player) {
				string type = source.Type.Defname;
				player.SysMessage((type == "t_thread") ? texts.ThreadOnYou : (type == "t_yarn") ? texts.YarnOnYou : texts.ThingOnYou);
				return TriggerResult.Cancel;
			}
			player.Target(SingletonScript<Targ_ClothMaking>.Instance, source);
			return TriggerResult.Cancel;
		}

		internal static string PromptFor(Item source, ClothMakingLoc texts) {
			switch (source.Type.Defname) {
				case "t_wool": return texts.AskWool;
				case "t_cotton": return texts.AskCotton;
				case "t_thread": return texts.AskThread;
				default: return texts.AskYarn;
			}
		}

		internal static void Use(Player self, Item source, Item station) {
			Recipe recipe = RecipeOf(source);
			if ((recipe == null) || !CraftingSkillDef.IsOfType(station, recipe.station)) {
				return;
			}
			ClothMakingLoc texts = Loc<ClothMakingLoc>.Get(self.Language);
			if (!self.CanSeeLOS(station).Allow) {
				self.SysMessage(recipe.fromFibre ? texts.NoSightOfWheel : texts.NoSightOfLoom);
				return;
			}
			if (self.GetTimer(timerKey) != null) {
				self.SysMessage(texts.AlreadyMaking);
				return;
			}

			ItemDef sourceDef = (ItemDef) source.Def;
			SpinningSettings settings = SpinningSettings.instance;
			int batches = self.GetSkill(SkillName.Tailoring) / Math.Max(1, settings.skillPerBatch);
			int batch = Math.Max(batches, 1) * SourcesInBatch;
			int used = Math.Min(batch, Count(self.Backpack, sourceDef));
			IResourceListEntry missing;
			if ((used <= 0) || !ResourcesListParser.Parse(used + " " + sourceDef.PrettyDefname).ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing)) {
				return;
			}

			ClothMakingTimer timer = new ClothMakingTimer();
			timer.product = (ItemDef) ThingDef.GetByDefname(recipe.product);
			timer.amount = used * (recipe.fromFibre ? settings.yieldPerFibre : settings.yieldPerThread);
			if (recipe.fromFibre) {
				timer.wheel = station;
				Spin(station, true);
			}
			self.AddTimer(timerKey, timer).DueInSeconds = settings.secondsToMake;
		}

		private static int Count(Container inside, ItemDef def) {
			int count = 0;
			foreach (Item item in inside.EnumShallow()) {
				if (item.Def == def) {
					count += item.Amount;
				}
				Container bag = item as Container;
				if (bag != null) {
					count += Count(bag, def);
				}
			}
			return count;
		}

		//f_spinningwheel_rotate: each wheel's still and turning picture
		private static readonly int[] stillWheels = { 0x1015, 0x1019, 0x101c, 0x10a4 };

		internal static void Spin(Item wheel, bool turning) {
			foreach (int still in stillWheels) {
				if (turning && (wheel.Model == still)) {
					wheel.Model = still + 1;
					return;
				}
				if (!turning && (wheel.Model == still + 1)) {
					wheel.Model = still;
					return;
				}
			}
		}
	}

	public class Targ_ClothMaking : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(ClothMaking.PromptFor((Item) parameter, Loc<ClothMakingLoc>.Get(self.Language)));
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Item source = (Item) parameter;
			Item station = targetted as Item;
			if ((station != null) && !source.IsDeleted) {
				ClothMaking.Use(self, source, station);
			}
			return TargetResult.Done;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class ClothMakingTimer : BoundTimer {
		[SaveableData, CopyableData]
		public ItemDef product;
		[SaveableData, CopyableData]
		public int amount;
		[SaveableData, CopyableData]
		public Item wheel;

		[LoadingInitializer, DeepCopyImplementation]
		public ClothMakingTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if ((this.wheel != null) && !this.wheel.IsDeleted) {
				ClothMaking.Spin(this.wheel, false);
			}
			Item made = (Item) this.product.Create(self.Backpack);
			made.Amount = this.amount;
			SkillGainSystem.TryGainSkillCustomFormula(self, SkillName.Tailoring, SpinningSettings.instance.tailoringAdvRate);
		}
	}

	internal class ClothMakingLoc : CompiledLocStringCollection<ClothMakingLoc> {
		public string ThingOnYou = "Vec musis mit u sebe.";
		public string ThreadOnYou = "Nite musis mit u sebe.";
		public string YarnOnYou = "Prizi musis mit u sebe.";
		public string AskWool = "Na cem chces tuto vlnu zpracovat?";
		public string AskCotton = "Na cem chces tuto bavlnu zpracovat?";
		public string AskThread = "Na cem chces nite sprist?";
		public string AskYarn = "Na cem chces prizi utkat?";
		public string NoSightOfWheel = "Nevidis na kolovrat.";
		public string NoSightOfLoom = "Nevidis na stav.";
		public string AlreadyMaking = "Uz vyrabis.";
	}
}
