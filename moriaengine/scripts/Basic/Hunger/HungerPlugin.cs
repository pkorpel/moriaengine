using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#924, #1396 - a player's hunger, on every player from login. It does its work in its holder's triggers,
	/// as Erebor's player event e_allplayers does (sphereevents.scp): its timer is Sphere's food tick, @skillSuccess
	/// (ishungrymsg: a hungry player's success is a failure), and #1558 the skill chain's attempt, @skillStart and a swing's
	/// @skillStroke (hungryhurts: Erebor's @hittry, @spellcast, mining, taming, makestring_start and lockpicking).</summary>
	[ViewableClass]
	[PluginTemplate]
	public partial class HungerPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		private int food = 15;

		public static readonly PluginKey key = PluginKey.Acquire("_hunger_");

		internal static HungerPlugin Of(Character ch) {
			return ch.GetPlugin(key) as HungerPlugin;
		}

		/// <summary>At login: the plugin, its food tick running.</summary>
		internal static HungerPlugin Install(Player player) {
			HungerPlugin hunger = Of(player);
			if (hunger == null) {
				hunger = (HungerPlugin) HungerPluginDef.instance.Create();
				player.AddPlugin(key, hunger);
			}
			if (hunger.TimerObject == null) {
				hunger.Timer = HungerSettings.instance.secondsPerFoodPoint;
			}
			return hunger;
		}

		public int Food {
			get {
				return this.food;
			}
			set {
				this.food = value;
			}
		}

		/// <summary>Sphere's OnTickFood, only for a player in the world.</summary>
		public void On_Timer() {
			this.Timer = HungerSettings.instance.secondsPerFoodPoint; //first, so a failing tick doesn't stop the clock
			if ((this.Cont is Player player) && (player.GameState != null)) {
				PlayerHunger.Tick(player);
			}
		}

		/// <summary>Erebor's @skillsuccess: the hungry fail. A blow isn't a skill's success in Sphere.</summary>
		public TriggerResult On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			if (!(skillSeqArgs.SkillDef is WeaponSkillDef) && PlayerHunger.DistractsFrom((Character) this.Cont)) {
				skillSeqArgs.Success = false;
				skillSeqArgs.PhaseFail();
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		/// <summary>#1558 - hungryhurts at the attempt of the skills Erebor charged it on: a starving player is hurt, and the attempt
		/// fails (pkorpel's call; on Erebor it only hurt). Before the skill's own checks: #1401's order, a cast the casting rules
		/// refuse still hurts. Not for a custom spell scroll: Erebor's customspellcast never ran e_allplayers' @spellcast. A
		/// crafting order that fails here stops, as one its menu refuses.</summary>
		public TriggerResult On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			if (HurtsAtStart(skillSeqArgs) && PlayerHunger.HurtIfStarving((Character) this.Cont)) {
				if (skillSeqArgs.SkillDef is CraftingSkillDef) {
					CraftingProcessPlugin.UnInstallCraftingPlugin((Character) this.Cont);
				}
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		/// <summary>#1558 - Erebor's @hittry: hungryhurts when a swing is drawn, in reach. A starving swing fails and is tried
		/// again a second later, as a refused one (@denySwing).</summary>
		public TriggerResult On_SkillStroke(SkillSequenceArgs skillSeqArgs) {
			if ((skillSeqArgs.SkillDef is WeaponSkillDef) && WeaponSkillDef.IsSwingAttempt(skillSeqArgs) &&
					PlayerHunger.HurtIfStarving((Character) this.Cont)) {
				Character self = (Character) this.Cont;
				if (!self.IsDeleted && !self.Flag_Dead) {
					WeaponSkillDef.RetryStroke(skillSeqArgs);
				}
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		private static bool HurtsAtStart(SkillSequenceArgs skillSeqArgs) {
			SkillDef skill = skillSeqArgs.SkillDef;
			if (skill is CraftingSkillDef) {
				return true; //makestring_start
			}
			switch ((SkillName) skill.Id) {
				case SkillName.Magery:
					return (skillSeqArgs.Param1 is SpellDef spell) && !spell.IsCustom;
				case SkillName.AnimalTaming:
				case SkillName.Lockpicking:
				case SkillName.Mining: //miningfix; fishing and lumberjacking share its Gathering.Begin, as before #1558
				case SkillName.Fishing:
				case SkillName.Lumberjacking:
					return true;
				default:
					return false;
			}
		}

	}

	[ViewableClass]
	[PluginDefTemplate]
	public partial class HungerPluginDef : TimerPluginDef {
		public static readonly HungerPluginDef instance =
			(HungerPluginDef) new HungerPluginDef("p_hunger", "C# scripts", -1).Register();
	}
}
