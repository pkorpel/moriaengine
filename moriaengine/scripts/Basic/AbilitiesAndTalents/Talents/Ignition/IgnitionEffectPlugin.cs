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

using System;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	//the similarity to Poison is not random :)
	public partial class IgnitionEffectPlugin {
		public const double minimumIgnitionEffect = 0.1; //plugin gets removed when the regen modifier goes below this


		static TimerKey tickTimerKey = TimerKey.Acquire("_ignitionTickTimer_");

		public virtual void On_Assign() {
			Character self = (Character) this.Cont;

			double effect = this.EffectPower;
			this.AddTimer(tickTimerKey, new IgnitionTickTimer(this.TimerObject.DueInSpan, effect));
			self.HitsRegenSpeed -= effect;
			//? self.Flag_GreenHealthBar = true;
		}

		public override void On_UnAssign(Character cont) {
			cont.HitsRegenSpeed += this.EffectPower;

			//? PoisonSpellDef poisonSpell = SingletonScript<PoisonSpellDef>.Instance;
			//? cont.Flag_GreenHealthBar = //
			//?     cont.HasPlugin(poisonSpell.EffectPluginKey_Potion) || cont.HasPlugin(poisonSpell.EffectPluginKey_Spell);
			base.On_UnAssign(cont);
		}

		public void ModifyEffect(double difference) {
			Character self = (Character) this.Cont;
			if (self != null) {
				double newEffect = this.EffectPower + difference;
				if (newEffect < minimumIgnitionEffect) {
					this.Delete();
				} else {
					self.HitsRegenSpeed -= difference;
					this.EffectPower = newEffect;
				}
			}
		}

		public void AnnounceIgnitionStrength() {
			//TODO some nicer effect?
			Character self = this.Cont as Character;
			if (self != null) {
				EffectFactory.StationaryEffect(self, 0x36BD, 20, 10);
				SoundCalculator.PlayHurtSound(self);
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class IgnitionTickTimer : BoundTimer {
		public static readonly TimeSpan tickSpan = TimeSpan.FromSeconds(5);

		[SaveableData, CopyableData]
		public double differencePerTick;

		[DeepCopyImplementation, LoadingInitializer]
		public IgnitionTickTimer() {
		}

		public IgnitionTickTimer(TimeSpan totalTime, double totalEffect) {
			this.DueInSpan = tickSpan;
			this.PeriodSpan = tickSpan;

			this.differencePerTick = -((tickSpan.Ticks * totalEffect) / totalTime.Ticks);
		}

		protected override void OnTimeout(TagHolder cont) {
			IgnitionEffectPlugin ignition = (IgnitionEffectPlugin) cont;
			ignition.ModifyEffect(this.differencePerTick);
			ignition.AnnounceIgnitionStrength();
		}
	}


	[ViewableClass]
	public partial class IgnitionEffectPluginDef {
		public static readonly IgnitionEffectPluginDef instance =
			(IgnitionEffectPluginDef) new IgnitionEffectPluginDef("p_ignitionEffect", "C# scripts", -1).Register();
	}
}