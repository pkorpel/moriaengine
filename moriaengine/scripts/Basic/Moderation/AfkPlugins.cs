using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1473 - Erebor's i_afk_countdown: the warning before an AFK check, a red line each second, then the check
	/// with what's left of its time. Its @logout is e_afk_controlled's: the log notes it.</summary>
	[Dialogs.ViewableClass]
	public partial class AfkCountdownPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_afkCountdown_");

		internal static AfkCountdownPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as AfkCountdownPlugin;
		}

		public void On_Timer() {
			if (!(this.Cont is Player player) || player.IsDeleted) {
				return;
			}
			this.Timer = 1; //each second, as long as it lasts (offline too: it ticks nothing then)
			if (player.GameState == null) { //#1487 - Erebor's isonline is the character's: not one its account left
				return;
			}
			long now = DateTime.UtcNow.Ticks;
			if (now < this.endsAt) {
				int left = (int) Math.Ceiling((this.endsAt - now) / (double) TimeSpan.TicksPerSecond);
				player.RedMessage(string.Format(Loc<AfkLoc>.Get(player.Language).CheckIn, left));
				return;
			}
			string what = this.action;
			this.Delete();
			long late = (now - this.endsAt) / TimeSpan.TicksPerSecond;
			int seconds = (int) Math.Max(1, AfkSettings.instance.answerSeconds - late);
			AfkCheck.Start(player, seconds, what);
		}

		public void On_LogOut() {
			if ((this.Cont is Player player) && (AfkCheckPlugin.Of(player) == null)) {
				AfkLogBook.Add(player, "se odlogoval pri odpocitavani AFK kontroly", null);
			}
		}
	}

	public partial class AfkCountdownPluginDef {
		public static readonly AfkCountdownPluginDef instance =
			(AfkCountdownPluginDef) new AfkCountdownPluginDef("p_afkCountdown", "C# scripts", -1).Register();
	}

	/// <summary>#1473 - Erebor's i_afk_dialog: the check, ticking each second. The red count every other second; a dialog
	/// that was answered wrong or closed comes back. Its @login and @logout are e_afk_controlled's.</summary>
	[Dialogs.ViewableClass]
	public partial class AfkCheckPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_afkCheck_");

		//not saved: after a restart or a login the dialog has to be sent again
		internal bool shown;

		internal static AfkCheckPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as AfkCheckPlugin;
		}

		public void On_Timer() {
			if (!(this.Cont is Player player) || player.IsDeleted) {
				return;
			}
			this.Timer = 1;
			if (player.GameState == null) { //#1487 - Erebor's isonline is the character's: not one its account left
				return;
			}
			int left = AfkCheck.SecondsLeft(this);
			if (left <= 0) {
				AfkCheck.TimedOut(player, this);
				return;
			}
			if (!this.shown) {
				AfkCheck.Show(player, this);
			} else if ((left % 2) == 1) {
				player.RedMessage(string.Format(Loc<AfkLoc>.Get(player.Language).SecondsToCamp, left));
			}
		}

		/// <summary>e_afk_controlled's @login: a check whose time ran out while offline gets one more second.</summary>
		public void On_Login() {
			if (!(this.Cont is Player player)) {
				return;
			}
			if (this.endsAt < DateTime.UtcNow.Ticks) {
				AfkLogBook.Add(player, "se lognul " + (-AfkCheck.SecondsLeft(this)) + "s po konci AFK kontroly", null);
				this.endsAt = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond;
			} else {
				AfkLogBook.Add(player, "po lognuti zbyva " + AfkCheck.SecondsLeft(this) + "s do konce AFK kontroly", null);
			}
			this.shown = false;
		}

		/// <summary>e_afk_controlled's @logout.</summary>
		public void On_LogOut() {
			if (this.Cont is Player player) {
				AfkLogBook.Add(player, "se odlogoval pri AFK kontrole", null);
			}
		}
	}

	public partial class AfkCheckPluginDef {
		public static readonly AfkCheckPluginDef instance =
			(AfkCheckPluginDef) new AfkCheckPluginDef("p_afkCheck", "C# scripts", -1).Register();
	}
}
