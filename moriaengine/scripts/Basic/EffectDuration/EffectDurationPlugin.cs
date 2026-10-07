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
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class EffectDurationPlugin {

		//sourcething = spell caster / ability user / trap item / etc
		public void Init(Thing sourceThing, EffectFlag sourceType, double power, TimeSpan duration) {
			this.Init(sourceThing, sourceType, power, duration, null);
		}

		//sourceDef = source spelldef / abilitydef / etc.
		public virtual void Init(Thing sourceThing, EffectFlag sourceType, double power, TimeSpan duration, AbstractDef sourceDef) {
			this.sourceThing = sourceThing;
			this.flags = sourceType;
			this.effectPower = power;
			if (duration >= TimeSpan.Zero) {
				this.Timer = duration.TotalSeconds;
			}
			this.sourceDef = sourceDef;
		}

		public virtual void On_Timer() {
			this.Delete();
		}

		public virtual void On_Dispell(SpellEffectArgs spell) {
			if (this.Dispellable) {
				Thing t = this.Cont as Thing;
				if (t != null) {
					DispellDef.ShowDispellEffect(t.TopObj());
				}
				this.Delete();
			}
		}

		public double EffectPower {
			get {
				return this.effectPower;
			}
			set {
				this.effectPower = value;
			}
		}

		public bool Dispellable {
			get {
				return (this.flags & EffectFlag.FromBook) == EffectFlag.FromBook; //potion effects are generally not dispellable. Might want some exception from this rule at some point...?
			}
		}

		public Thing SourceThing {
			get {
				return this.sourceThing;
			}
		}

		//source spelldef/abilitydef 
		public AbstractDef SourceDef {
			get {
				return this.sourceDef;
			}
		}

		public EffectFlag Flags {
			get {
				return this.flags;
			}
		}
		
		public virtual string EffectName {
			get {
				if (this.sourceDef != null)
				{
					if ((this.flags & EffectFlag.FromAbility) == EffectFlag.FromAbility) {
						return ((AbilityDef) this.sourceDef).Name;
					}
					if ((this.flags & (EffectFlag.FromSpellBook | EffectFlag.FromSpellScroll)) != EffectFlag.None) {
						return ((SpellDef) this.sourceDef).Name;
					}
					return this.sourceDef.PrettyDefname;
				}
				return this.ToString();
			}
		}

		public virtual void On_Death() {
			this.Delete();
		}

		//default "effect ended" message
		public virtual void On_UnAssign(Character cont) {
			this.EffectEndedMessage(cont);
		}

		protected virtual void EffectEndedMessage(Character cont) {
			if ((this.flags & EffectFlag.FromAbility) == EffectFlag.FromAbility) {
				if (cont == this.sourceThing) { //my own ability
					cont.SysMessage(string.Format(CultureInfo.InvariantCulture,
						Loc<EffectDurationLoc>.Get(cont.Language).AbilityDeactivated,
						this.EffectName));
				} else {
					string msg;
					if ((this.flags & EffectFlag.BeneficialEffect) == EffectFlag.BeneficialEffect) {
						msg = Loc<EffectDurationLoc>.Get(cont.Language).GoodAbilityEffectEnded;
					} else if ((this.flags & EffectFlag.HarmfulEffect) == EffectFlag.HarmfulEffect) {
						msg = Loc<EffectDurationLoc>.Get(cont.Language).EvilAbilityEffectEnded;
					} else {
						msg = Loc<EffectDurationLoc>.Get(cont.Language).AbilityEffectEnded;
					}
					cont.SysMessage(string.Format(CultureInfo.InvariantCulture,
						msg, this.EffectName));
				}
			} else if ((this.flags & (EffectFlag.FromSpellBook | EffectFlag.FromSpellScroll)) != EffectFlag.None) {
				string msg;
				if ((this.flags & EffectFlag.BeneficialEffect) == EffectFlag.BeneficialEffect) {
					msg = Loc<EffectDurationLoc>.Get(cont.Language).GoodSpellEffecEnded;
				} else if ((this.flags & EffectFlag.HarmfulEffect) == EffectFlag.HarmfulEffect) {
					msg = Loc<EffectDurationLoc>.Get(cont.Language).EvilSpellEffecEnded;
				} else {
					msg = Loc<EffectDurationLoc>.Get(cont.Language).SpellEffecEnded;
				}
				cont.SysMessage(string.Format(CultureInfo.InvariantCulture,
						msg, this.EffectName));
			} else {
				cont.SysMessage(string.Format(CultureInfo.InvariantCulture,
						Loc<EffectDurationLoc>.Get(cont.Language).UnspecifiedEffectEnded,
						this.EffectName));
			}
		}
	}

	[ViewableClass]
	public partial class EffectDurationPluginDef {
	}

	public class EffectDurationLoc : CompiledLocStringCollection<EffectDurationLoc> {
		public string AbilityDeactivated = "Ability '{0}' deactivated.";

		public string AbilityEffectEnded = "Effect of ability '{0}' ended.";
		public string EvilAbilityEffectEnded = "The unpleasant effect of ability '{0}' ended.";
		public string GoodAbilityEffectEnded = "The pleasant effect of ability '{0}' ended.";

		public string SpellEffecEnded = "Effect of spell '{0}' ended.";
		public string EvilSpellEffecEnded = "The unpleasant effect of spell '{0}' ended.";
		public string GoodSpellEffecEnded = "The pleasant effect of spell '{0}' ended.";

		public string UnspecifiedEffectEnded = "Effect of '{0}' ended.";
	}
}