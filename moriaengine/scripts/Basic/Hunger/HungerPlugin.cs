using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#924, #1396 - a player's hunger, on every player from login. It does its work in its holder's triggers,
	/// as Erebor's player event e_allplayers does (sphereevents.scp): its timer is Sphere's food tick, @skillSuccess
	/// (ishungrymsg: a hungry player's success is a failure), and #1558 the skill chain's attempt, @skillStart and a swing's
	/// @skillStroke (hungryhurts: Erebor's @hittry, @spellcast, mining, taming and makestring_start; a starving attempt fails,
	/// pkorpel's call). Each is charged only where Erebor charged it, past the skill's own checks: see AttemptAtStart and
	/// WeaponSkillDef.IsSwingAttempt. Lockpicking isn't on the skill chain, so Targ_Lockpick asks PlayerHunger itself.</summary>
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

		/// <summary>#1558 - hungryhurts at the start of the skills Erebor charged it on: a starving player is hurt and the attempt
		/// fails. Cancelling here skips the skill's On_Start, so nothing of the attempt happens.</summary>
		public TriggerResult On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			if (AttemptAtStart(skillSeqArgs) && PlayerHunger.HurtIfStarving((Character) this.Cont)) {
				if (skillSeqArgs.SkillDef is CraftingSkillDef) {
					CraftingProcessPlugin.UnInstallCraftingPlugin((Character) this.Cont); //the order stops, as one its menu refuses
				}
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		/// <summary>#1558 - Erebor's @hittry: hungryhurts when a swing is drawn in reach, past the stroke's own checks (a target
		/// gone or out of sight ends the attack in On_Stroke, uncharged). A starving swing fails and stays drawn, tried again a
		/// weapon delay later: the hurt once a swing, as Erebor's.</summary>
		public TriggerResult On_SkillStroke(SkillSequenceArgs skillSeqArgs) {
			if ((skillSeqArgs.SkillDef is WeaponSkillDef) && WeaponSkillDef.IsSwingAttempt(skillSeqArgs)) {
				Character self = (Character) this.Cont;
				if (PlayerHunger.HurtIfStarving(self)) {
					if (!self.IsDeleted && !self.Flag_Dead) {
						skillSeqArgs.DelaySpan = self.WeaponDelay;
						skillSeqArgs.DelayStroke();
					}
					return TriggerResult.Cancel;
				}
			}
			return TriggerResult.Continue;
		}

		/// <summary>Whether this @skillStart is an attempt Erebor charged, at the point it charged it:
		/// - a cast past Sphere's own checks (sight, mana, reagents; MagerySkillDef.PassesOwnChecks), before the casting rules (#1401),
		///   not a custom scroll (customspellcast never ran e_allplayers' @spellcast);
		/// - mining, fishing, lumberjacking: past the wait, the saddle and the dark, which Gathering.Prepare does before the start
		///   (miningfix);
		/// - taming (f_tamingstart);
		/// - a craft-menu order's first item (makestring_start: once an order; its light is checked in the item's start, after).
		///   Cooking on a fire is no order and costs nothing.</summary>
		private static bool AttemptAtStart(SkillSequenceArgs skillSeqArgs) {
			SkillDef skill = skillSeqArgs.SkillDef;
			if (skill is CraftingSkillDef) {
				return CraftingProcessPlugin.IsOrdersFirstItem(skillSeqArgs.Self);
			}
			switch ((SkillName) skill.Id) {
				case SkillName.Magery:
					return MagerySkillDef.PassesOwnChecks(skillSeqArgs);
				case SkillName.AnimalTaming:
				case SkillName.Mining:
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
