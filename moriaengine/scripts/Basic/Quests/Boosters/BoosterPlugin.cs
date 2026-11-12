using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1471 - a player's boosters (Erebor's i_booster and its tags): the stored rows ("kind:count,..."), the running
	/// kind, whose timer is this plugin's, and the seconds left while offline. A logout pauses it, a login resumes it, and
	/// each experience gain gets the running one's percent of its base. It goes once it holds nothing.</summary>
	[Dialogs.ViewableClass]
	public partial class BoosterPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_boosterPlugin_");

		internal static BoosterPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as BoosterPlugin;
		}

		internal static BoosterPlugin Ensure(Character ch) {
			BoosterPlugin plugin = Of(ch);
			if (plugin == null) {
				plugin = (BoosterPlugin) BoosterPluginDef.instance.Create();
				ch.AddPlugin(Key, plugin);
			}
			return plugin;
		}

		/// <summary>A booster runs (not paused).</summary>
		internal bool IsRunning {
			get {
				return (this.running >= 0) && (this.TimerObject != null);
			}
		}

		internal void DropIfEmpty() {
			if (string.IsNullOrEmpty(this.stored) && (this.running < 0)) {
				this.Delete();
			}
		}

		/// <summary>i_booster's end.</summary>
		public void On_Timer() {
			if (this.Cont is Character ch) {
				ch.SysMessage("Tvuj bonus do zkusenosti vyprsel!", ClassAbilities.ClassMessageHue);
			}
			this.running = -1;
			this.DropIfEmpty();
		}

		/// <summary>Sphere doesn't tick an offline character, so a booster's hours count only while its player plays.</summary>
		public void On_LogOut() {
			if (this.Cont is Player player) {
				Boosters.Pause(player);
			}
		}

		/// <remarks>Inside @login: a handler after this one cancelling the login would leave it running while offline. None
		/// cancels a login today.</remarks>
		public void On_Login() {
			if (this.Cont is Player player) {
				Boosters.Resume(player);
			}
		}

		/// <summary>funkce.scp's addexp: the running booster's percent of a gain's base, never of a loss.</summary>
		public void On_GainExperience(GainExperienceArgs args) {
			if (this.IsRunning && (args.BaseAmount > 0)) {
				args.Amount += (args.BaseAmount * Boosters.Kinds[this.running].Percent) / 100;
			}
		}
	}

	public partial class BoosterPluginDef {
		public static readonly BoosterPluginDef instance =
			(BoosterPluginDef) new BoosterPluginDef("p_boosters", "C# scripts", -1).Register();
	}
}
