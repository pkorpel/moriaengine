using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#924, #1396 - a player's hunger, on every player from login. It does its work in its holder's triggers,
	/// as Erebor's player event e_allplayers does (sphereevents.scp): its timer is Sphere's food tick, @skillSuccess
	/// (ishungrymsg: a hungry player's success is a failure), @spellCast and @hitTry (hungryhurts, Erebor's @spellcast and
	/// @hittry). The skills that ask about hunger themselves in Erebor's scripts call PlayerHunger, which reads this.</summary>
	[ViewableClass]
	public partial class HungerPlugin {
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

		/// <summary>Erebor's @spellcast: hungryhurts. Not for a custom spell scroll: Erebor's customspellcast never ran
		/// e_allplayers' @spellcast. Our @spellCast comes once the cast's checks have passed, so a refused cast doesn't
		/// hurt; on Erebor hungryhurts was @spellcast's first line, before its own refusals.</summary>
		public void On_SpellCast(SkillSequenceArgs mageryArgs) {
			if (!((SpellDef) mageryArgs.Param1).IsCustom) {
				PlayerHunger.HurtIfStarving((Character) this.Cont);
			}
		}

		/// <summary>Erebor's @hittry: hungryhurts, trying a blow on nothing (WeaponSkillDef, as the swing is drawn).</summary>
		public void On_HitTry(SkillSequenceArgs swingArgs) {
			PlayerHunger.HurtIfStarving((Character) this.Cont);
		}
	}

	[ViewableClass]
	public partial class HungerPluginDef {
		public static readonly HungerPluginDef instance =
			(HungerPluginDef) new HungerPluginDef("p_hunger", "C# scripts", -1).Register();
	}
}
