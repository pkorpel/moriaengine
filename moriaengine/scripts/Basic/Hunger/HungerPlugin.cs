using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#924, #1396 - a player's hunger, on every player from login. It does its work in its holder's triggers,
	/// as Erebor's player event e_allplayers does (sphereevents.scp): its timer is Sphere's food tick and @skillSuccess
	/// (ishungrymsg: a hungry player's success is a failure). Erebor's hungryhurts (PlayerHunger.HurtIfStarving) is asked
	/// where Erebor asked it, each attempt's own point (#1558: a starving attempt then fails, pkorpel's call).</summary>
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
	}

	[ViewableClass]
	[PluginDefTemplate]
	public partial class HungerPluginDef : TimerPluginDef {
		public static readonly HungerPluginDef instance =
			(HungerPluginDef) new HungerPluginDef("p_hunger", "C# scripts", -1).Register();
	}
}
