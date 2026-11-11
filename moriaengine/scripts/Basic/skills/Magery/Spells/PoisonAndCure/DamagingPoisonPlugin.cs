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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1422 - Erebor's i_memory_jed: one running poison of a kind, under that kind's key (Poison keeps the rules:
	/// who gets poisoned, a stronger dose replacing the weaker, what a hit does). Not a FadingEffectDurationPlugin: it has
	/// no duration, each hit is a tenth weaker, and doses don't add up. The green health bar goes through the character's
	/// poison counter, shared with bleeding; a death ends it (EffectDurationPlugin.On_Death).</summary>
	[ViewableClass]
	public partial class DamagingPoisonEffectPlugin {
		private const int LeastLoss = 1, LossShare = 10, HitEffectSpeed = 1, HitEffectLength = 32, HitEffect = 0x1ae4;
		private static readonly TimerKey tickTK = TimerKey.Acquire("_poisonTick_");
		private static readonly PluginKey[] keys = CreateKeys();

		private static PluginKey[] CreateKeys() {
			PluginKey[] created = new PluginKey[Enum.GetValues(typeof(PoisonKind)).Length];
			for (int i = 0; i < created.Length; i++) {
				created[i] = PluginKey.Acquire("_poison" + i + "_");
			}
			return created;
		}

		internal static PluginKey KeyOf(PoisonKind kind) {
			return keys[(int) kind];
		}

		public PoisonKind Kind {
			get {
				return this.kind;
			}
		}

		public int Strength {
			get {
				return this.strength;
			}
		}

		/// <summary>Who poisoned: the source of its last stronger dose.</summary>
		public Character Dealer {
			get {
				return this.SourceThing as Character;
			}
		}

		/// <summary>A new poison: its kind, strength and dealer, hitting every `seconds`.</summary>
		internal void Begin(Character dealer, PoisonKind kind, int strength, double seconds) {
			this.kind = kind;
			this.Renew(dealer, strength);
			this.AddTimer(tickTK, new PoisonTickTimer(TimeSpan.FromSeconds(seconds)));
		}

		/// <summary>A stronger dose: its strength and dealer, the hits going on as they were.</summary>
		internal void Renew(Character dealer, int strength) {
			this.strength = strength;
			this.Init(dealer, EffectFlag.HarmfulEffect, strength, TimeSpan.FromSeconds(-1)); //no duration: it ends by weakening
		}

		public void On_Assign() {
			((Character) this.Cont).AddPoisonCounter(); //the green health bar
		}

		public override void On_UnAssign(Character cont) {
			cont.RemovePoisonCounter(); //Erebor's poison ends without a word
		}

		/// <summary>i_memory_jed's timer: a hit, a tenth weaker after it. Nothing left of it, or a hit doing nothing, ends it.</summary>
		internal void Tick() {
			Character self = this.Cont as Character;
			if ((self == null) || self.Flag_Dead || (this.strength < 1) || Poison.IsImmune(self) || (Poison.Hit(self, this) <= 0)) {
				this.Delete();
				return;
			}
			if (!self.IsDeleted && !self.Flag_Dead && (self.GetPlugin(KeyOf(this.kind)) == this)) { //not cured by the hit (a possession's swap back)
				EffectFactory.StationaryEffect(self, HitEffect, HitEffectSpeed, HitEffectLength);
				this.strength -= Math.Max(this.strength / LossShare, LeastLoss);
			}
		}

		public override string EffectName {
			get {
				Character holder = this.Cont as Character;
				return Loc<PoisonLoc>.Get((holder == null) ? Language.Default : holder.Language).Poisoned;
			}
		}

		public override string EffectDetail {
			get {
				return this.strength.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary>.boosts doesn't name the poisoner (a thief's knife, a hidden attacker), as the poison's own list never did.</summary>
		protected override bool ShowsSource {
			get {
				return false;
			}
		}
	}

	[ViewableClass]
	public partial class DamagingPoisonEffectPluginDef {
		private static DamagingPoisonEffectPluginDef instance;

		/// <summary>p_poison (poisonSpell.def).</summary>
		public static DamagingPoisonEffectPluginDef Instance {
			get {
				if (instance == null) {
					instance = (DamagingPoisonEffectPluginDef) GetByDefname("p_poison");
				}
				return instance;
			}
		}
	}

	/// <summary>#1422 - the poison's hits, on its plugin.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PoisonTickTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public PoisonTickTimer() {
		}

		public PoisonTickTimer(TimeSpan period) {
			this.DueInSpan = period;
			this.PeriodSpan = period;
		}

		protected override void OnTimeout(TagHolder cont) {
			((DamagingPoisonEffectPlugin) cont).Tick();
		}
	}
}
