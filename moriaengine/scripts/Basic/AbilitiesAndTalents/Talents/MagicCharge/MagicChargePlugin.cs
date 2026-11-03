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

		private int Total {
			get { return this.fireCharge + this.mindCharge + this.energyCharge; }
		}

		/// <summary>#1017 - charge_add: one school at a time, a new one starts over at 1. Hitting your own
		/// side or a friendly pet charges nothing.</summary>
		public static void AddCharge(Character self, MagicChargeSchool school, Character target = null) {
			if (target != null) {
				if (target.IsPlayer && (target.Realm == self.Realm)) {
					return;
				}
				if ((target is NPC npc) && npc.IsPetOf(self)) {
					return;
				}
			}
			MagicChargePlugin plugin = (MagicChargePlugin) self.GetPlugin(key);
			if (plugin == null) {
				plugin = (MagicChargePlugin) MagicChargePluginDef.instance.Create();
				self.AddPlugin(key, plugin);
			}
			int charge = System.Math.Min(ClassAbilitySettings.instance.chargeLimit, plugin.GetCharge(school) + 1);
			plugin.fireCharge = plugin.mindCharge = plugin.energyCharge = 0;
			plugin.Set(school, charge);
			plugin.Arm(ClassAbilitySettings.instance.chargeDurationSeconds);
			AbilityIcons.ShowCharge(self, AbilityIcons.KindOf(school), charge); //#1070
		}

		private void Set(MagicChargeSchool school, int charge) {
			switch (school) {
				case MagicChargeSchool.Fire:
					this.fireCharge = charge;
					break;
				case MagicChargeSchool.Energy:
					this.energyCharge = charge;
					break;
				default:
					this.mindCharge = charge;
					break;
			}
		}

		private void Arm(double seconds) {
			BoundTimer timer = this.GetTimer(tickTimerKey);
			if (timer == null) {
				timer = new MagicChargeDecayTimer();
				this.AddTimer(tickTimerKey, timer);
			}
			timer.DueInSeconds = seconds;
			timer.PeriodInSeconds = seconds;
		}

		/// <summary>f_charge_decrease.</summary>
		internal void Decrease(int amount) {
			if (this.Total <= amount) {
				this.fireCharge = this.mindCharge = this.energyCharge = 0;
				BoundTimer timer = this.GetTimer(tickTimerKey);
				if (timer != null) {
					timer.PeriodInSeconds = -1;
				}
				AbilityIcons.HideCharge(this.Cont as Character); //#1070
				return;
			}
			if (this.fireCharge > 0) {
				this.fireCharge -= amount;
				AbilityIcons.ShowCharge(this.Cont as Character, AbilityIcons.Fire, this.fireCharge);
			} else if (this.mindCharge > 0) {
				this.mindCharge -= amount;
				AbilityIcons.ShowCharge(this.Cont as Character, AbilityIcons.Mind, this.mindCharge);
			} else {
				this.energyCharge -= amount;
				AbilityIcons.ShowCharge(this.Cont as Character, AbilityIcons.Energy, this.energyCharge);
			}
			this.Arm(ClassAbilitySettings.instance.chargeDropSeconds);
		}

		/// <summary>#1017 - the school Erebor's def_charge_type gives a spell, or null.</summary>
		internal static MagicChargeSchool? SchoolOf(SpellDef spell) {
			switch (spell.Id) {
				case 5:
				case 18:
				case 43:
				case 51:
				case 55:
					return MagicChargeSchool.Fire;
				case 12:
				case 37:
					return MagicChargeSchool.Mind;
				case 30:
				case 42:
				case 49:
					return MagicChargeSchool.Energy;
			}
			return null;
		}

		private static bool IsCharger(SpellDef spell) {
			return (spell.Id == 18) || (spell.Id == 12) || (spell.Id == 30);
		}

		internal static string AbilityOf(MagicChargeSchool school) {
			switch (school) {
				case MagicChargeSchool.Fire:
					return "magicCharge_fire";
				case MagicChargeSchool.Energy:
					return "magicCharge_energy";
				default:
					return "magicCharge_mind";
			}
		}

		/// <summary>#1017 - e_magicCharge's @afterDoEffect and charge_checkCastedCharger, once a cast. Fireball,
		/// Harm and Lightning charge. Another spell of a school spends: all of it if the school differs or
		/// it's fire, one on a player.</summary>
		internal static void AfterCast(Player self, SpellDef spell, Character target) {
			MagicChargeSchool? school = SchoolOf(spell);
			if (school == null) {
				return;
			}
			if (IsCharger(spell)) {
				if (ClassAbilities.Points(self, AbilityOf(school.Value)) > 0) {
					AddCharge(self, school.Value, target);
				}
				return;
			}
			MagicChargePlugin plugin = Get(self);
			if ((plugin == null) || (plugin.Total == 0)) {
				return;
			}
			int limit = ClassAbilitySettings.instance.chargeLimit;
			if ((plugin.GetCharge(school.Value) == 0) || (school.Value == MagicChargeSchool.Fire)) {
				plugin.Decrease(limit);
			} else if ((target != null) && target.IsPlayer) {
				plugin.Decrease(1);
			}
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
				plugin.Decrease(1);
			}
		}
	}
}
