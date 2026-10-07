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
	public partial class FadingEffectDurationPlugin {
		public const double minimumEffectPower = 0.1;

		static TimerKey tickTimerKey = TimerKey.Acquire("_tick_timer_");

		public override void Init(Thing sourceThing, EffectFlag sourceType, double power, TimeSpan duration, AbstractDef sourceDef) {
			base.Init(sourceThing, sourceType, power, duration, sourceDef);

			double tickSpan = this.TypeDef.TickInterval;
			double tickCount = duration.TotalSeconds / tickSpan;
			double differencePerTick = this.EffectPower / tickCount;
			this.AddTimer(tickTimerKey, new FadingEffectTickTimer(TimeSpan.FromSeconds(tickSpan), differencePerTick));
		}

		public virtual void On_Assign() {
			Character self = (Character) this.Cont;
			if ((this.Flags & EffectFlag.HarmfulEffect) == EffectFlag.HarmfulEffect) {
				self.AddPoisonCounter(); //make the healthbar green
			}
		}

		//one day, plugintemplate generator may allow declaring some plugin classes as abstract... till that, we use NotImplementedException
		public virtual void On_FadingEffectTick() {
			throw new NotImplementedException();
		}

		public override void On_UnAssign(Character cont) {
			if ((this.Flags & EffectFlag.HarmfulEffect) == EffectFlag.HarmfulEffect) {
				cont.RemovePoisonCounter();
			}
			base.On_UnAssign(cont);
		}
	}


	[ViewableClass]
	public partial class FadingEffectDurationPluginDef {

		public void Apply(Thing source, Character target, EffectFlag sourceType, double effectPower, int tickCount) {
			//every FadingEffect type has it's pluginkey, so they're independent on each other

			double tickSeconds = this.TickInterval;
			double duration = tickSeconds * tickCount;

			PluginKey key = this.PluginKey;
			FadingEffectDurationPlugin previous = target.GetPlugin(key) as FadingEffectDurationPlugin;
			if (previous != null) {
				if ((previous.Def == this)) {
					//previous FadingEffect is of the same type, we sum them up to the max values and apply to the old one

					double maxDuration = this.MaxTicks * tickSeconds;
					double newDuration = Math.Min(previous.Timer + duration, maxDuration);
					double newPower = Math.Min(previous.EffectPower + effectPower, this.GetMaxPower(source, target, sourceType));

					//reinit with new duration and effect. Hope it doesn't break anything :D
					//also we change it's source, so that the new attacker is responsible now
					previous.Init(source, sourceType, newPower, TimeSpan.FromSeconds(newDuration));

					return;
				}
			}

			FadingEffectDurationPlugin effect = (FadingEffectDurationPlugin) this.Create();
			effect.Init(source, sourceType, effectPower,
				TimeSpan.FromSeconds(duration));
			target.AddPlugin(key, effect);
		}

		public virtual double GetMaxPower(Thing source, Character target, EffectFlag sourceType) {
			return this.MaxPower;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class FadingEffectTickTimer : BoundTimer {

		//[SaveableData, CopyableData]
		//public TimeSpan tickSpan;

		[SaveableData, CopyableData]
		public double differencePerTick;

		[DeepCopyImplementation, LoadingInitializer]
		public FadingEffectTickTimer() {
		}

		public FadingEffectTickTimer(TimeSpan tickSpan, double differencePerTick) {
			this.DueInSpan = tickSpan;
			this.PeriodSpan = tickSpan;

			this.differencePerTick = differencePerTick;
		}

		protected override void OnTimeout(TagHolder cont) {
			FadingEffectDurationPlugin eff = (FadingEffectDurationPlugin) cont;

			eff.On_FadingEffectTick();
			eff.EffectPower -= this.differencePerTick;
		}
	}
}


