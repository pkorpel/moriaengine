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

using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#491 - Magic Charge ("Magical Shot"): casting Fire Ball/Harm/Lightning
	/// successfully stacks a charge (max 10, decays over time) in one of 3 schools (fire/mind/
	/// energy respectively - mapping taken directly from #491's own stated order; Harm already
	/// casts via UseMindPower, which independently supports "mind" as its school). Charges boost
	/// that school's other spells' spell power (see MagicChargeHelper, called from SpellDef's
	/// GetSpellPowerAgainstChar/AgainstNonChar). Do NOT confuse with the unrelated, already-
	/// working ColoredStaff.On_DClick mana-storage feature - left untouched.
	/// <para>Managed entirely from C# (AddCharge/Get below), never created from an LScript
	/// header - but Plugin.TryRun/Run dereference this.def.scriptedTriggers/compiledTriggers
	/// unconditionally (no null check), so a bare `new MagicChargePlugin()` with a null Def
	/// crashes the moment AddPlugin's own assign-trigger dispatch runs. Needs a real PluginDef
	/// instance even with no LScript header behind it - same "construct directly + Register(),
	/// no LScript header" pattern as IgnitionEffectPluginDef's own
	/// `new IgnitionEffectPluginDef("p_ignitionEffect", "C# scripts", -1).Register()`.</para></summary>
	public enum MagicChargeSchool {
		Fire,
		Mind,
		Energy
	}

	[ViewableClass]
	public class MagicChargePluginDef : PluginDef {
		public static readonly MagicChargePluginDef instance =
			(MagicChargePluginDef) new MagicChargePluginDef("p_magic_charge", "C# scripts", -1).Register();

		public MagicChargePluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new MagicChargePlugin();
		}
	}

	[DeepCopyableClass]
	[SaveableClass]
	public class MagicChargePlugin : Plugin {
		private static readonly PluginKey key = PluginKey.Acquire("_magicCharge_");
		private static readonly TimerKey tickTimerKey = TimerKey.Acquire("_magicChargeDecayTick_");

		[SaveableData, CopyableData]
		public int fireCharge;
		[SaveableData, CopyableData]
		public int mindCharge;
		[SaveableData, CopyableData]
		public int energyCharge;

		[DeepCopyImplementation]
		public MagicChargePlugin(MagicChargePlugin copyFrom)
			: base(copyFrom) {
			this.fireCharge = copyFrom.fireCharge;
			this.mindCharge = copyFrom.mindCharge;
			this.energyCharge = copyFrom.energyCharge;
		}

		[LoadingInitializer]
		public MagicChargePlugin() {
		}

		public int GetCharge(MagicChargeSchool school) {
			switch (school) {
				case MagicChargeSchool.Fire:
					return this.fireCharge;
				case MagicChargeSchool.Energy:
					return this.energyCharge;
				default:
					return this.mindCharge;
			}
		}

		private bool AnyChargeLeft() {
			return (this.fireCharge > 0) || (this.mindCharge > 0) || (this.energyCharge > 0);
		}

		public static void AddCharge(Character self, MagicChargeSchool school) {
			MagicChargePlugin plugin = (MagicChargePlugin) self.GetPlugin(key);
			if (plugin == null) {
				plugin = (MagicChargePlugin) MagicChargePluginDef.instance.Create();
				self.AddPlugin(key, plugin);
			}
			switch (school) {
				case MagicChargeSchool.Fire:
					plugin.fireCharge = System.Math.Min(10, plugin.fireCharge + 1);
					break;
				case MagicChargeSchool.Energy:
					plugin.energyCharge = System.Math.Min(10, plugin.energyCharge + 1);
					break;
				default:
					plugin.mindCharge = System.Math.Min(10, plugin.mindCharge + 1);
					break;
			}
			double interval = MagerySettings.instance.magicChargeDecayIntervalSeconds;
			BoundTimer timer = plugin.GetTimer(tickTimerKey);
			if (timer == null) {
				timer = new MagicChargeDecayTimer();
				plugin.AddTimer(tickTimerKey, timer);
			}
			// Always re-arm (not just on first creation) - a previous OnTimeout may have gone
			// dormant (PeriodInSeconds=-1) once charges hit 0, and this call just added a fresh
			// one.
			timer.DueInSeconds = interval;
			timer.PeriodInSeconds = interval;
		}

		public static MagicChargePlugin Get(Character self) {
			return (MagicChargePlugin) self.GetPlugin(key);
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class MagicChargeDecayTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public MagicChargeDecayTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				MagicChargePlugin plugin = cont as MagicChargePlugin;
				if (plugin == null) {
					return;
				}
				if (plugin.fireCharge > 0) {
					plugin.fireCharge--;
				}
				if (plugin.mindCharge > 0) {
					plugin.mindCharge--;
				}
				if (plugin.energyCharge > 0) {
					plugin.energyCharge--;
				}
				if (!plugin.AnyChargeLeft()) {
					// Stop rescheduling rather than deleting the plugin from within its own
					// timer's OnTimeout (a risky self-deletion-of-host pattern) - a negative
					// PeriodInSeconds means Timer.cs's own ProcessTimingOut simply never re-
					// enqueues this timer, so it goes permanently dormant. The plugin itself
					// staying attached (at 0/0/0 charge) is harmless - GetCharge/ApplyBonus
					// already treat 0 as a clean no-op.
					this.PeriodInSeconds = -1;
				}
			}
		}
	}
}
