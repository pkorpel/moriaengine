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

namespace SteamEngine.CompiledScripts {

	/// <summary>#1401 - Erebor's energy shield, a mage's or necromancer's class ability: its points are the ra_energyshield
	/// points spent in the class abilities dialog (ClassAbilities). Up for 45 s and half a second a point, with 5 energy a
	/// point; then 180 s of rest before another (i_energyshield's two timers).</summary>
	[ViewableClass]
	public class EnergyShieldAbilityDef : ActivableAbilityDef {
		internal const int RestSeconds = 180;
		private const int manaNeeded = 60, manaTaken = 59;

		public EnergyShieldAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>The energyshield command, [function energyshield]'s checks in its order.</summary>
		public void Use(Character ch) {
			ProfessionDef profession = (ch as Player)?.Profession;
			EnergyShieldPlugin shield = EnergyShieldPlugin.Of(ch);
			if ((profession != Professions.Mage) && (profession != Professions.Necromant)) {
				ch.SysMessage("Tohle nemuzes pouzivat.");
			} else if (ch.GetAbility(this) <= 0) {
				ch.SysMessage("Tohle zatim neumis pouzivat.");
			} else if ((shield != null) && !shield.HasEnergy) {
				ch.SysMessage("Tva magicka moc je jeste prilis oslabena.");
			} else if (shield != null) {
				ch.SysMessage("Jeste stale mas stit kolem sebe vytvoren.");
			} else if (ch.Flag_Dead) {
				//nothing: the ability system would take the amethyst before refusing a ghost
			} else if ((this.Resources != null) && !this.Resources.HasResourcesPresent(ch, ResourcesLocality.Backpack, out _)) {
				ch.SysMessage("Nemas potrebne prisady.");
			} else if (ch.Mana > manaNeeded) {
				this.Activate(ch); //consumes the amethyst (AbilityDef.On_DenyActivate)
			} else if (ch.Mana < manaNeeded) {
				ch.SysMessage("Nemas dost energie!");
			} //exactly 60: nothing, as Erebor's
		}

		/// <summary>The cancelenergyshield command: the shield goes down, the rest starts.</summary>
		public void Cancel(Character ch) {
			EnergyShieldPlugin shield = EnergyShieldPlugin.Of(ch);
			if ((shield != null) && shield.HasEnergy) {
				ch.SysMessage((ch.IsFemale ? "Zrusila" : "Zrusil") + " jsi magicky stit.", ClassAbilities.ClassMessageHue);
				shield.StartRest();
			}
		}

		protected override void On_Activate(Character chr, Ability ab) {
			int points = ab.ModifiedPoints;
			EnergyShieldPlugin shield = (EnergyShieldPlugin) this.PluginDef.Create();
			shield.Init(chr, EffectFlag.BeneficialEffect | EffectFlag.FromAbility, this.EffectPower * points, TimeSpan.FromSeconds(45 + (points / 2)), this);
			shield.ShieldPower = ShieldPowerFor(points);
			chr.AddPlugin(this.PluginKey, shield);
			chr.Speech("Heru Beleg Del Tir", 0, SpeechType.Spell, -1, ClientFont.Unified, null, null);
			EffectFactory.StationaryEffect(chr, 0x2EEA, 6, 20);
			chr.Sound(0x212); //snd_spell_energy_vortex
			chr.SysMessage((chr.IsFemale ? "Vztycila" : "Vztycil") + " jsi stit z ciste energie!", ClassAbilities.ClassMessageHue);
			chr.Mana = (short) Math.Max(0, chr.Mana - manaTaken); //a GM's activation skips Use's mana check
			//the standcast ends in StandcastPlugin.On_ActivateAbility
		}

		/// <summary>i_energyshield's @Equip table: the percent of a blow that still gets through, 79 at 10 points down to
		/// 24 at 50 (Sphere's integer division by 10).</summary>
		public static int ShieldPowerFor(int points) {
			int table;
			if (points < 11) {
				table = points * 21;
			} else if (points < 21) {
				table = 210 + ((points - 10) * 18);
			} else if (points < 31) {
				table = 390 + ((points - 20) * 15);
			} else if (points < 41) {
				table = 540 + ((points - 30) * 12);
			} else if (points < 51) {
				table = 660 + ((points - 40) * 10);
			} else {
				table = 0; //no row above 50: the whole blow, as Erebor's unset arg
			}
			return 100 - (table / 10);
		}
	}

	/// <summary>#1401 - i_energyshield and e_energy_shield: while up, damage from anyone but a player is let through at
	/// ShieldPower percent and taken whole off the energy (DamageManager calls Absorb); then the rest. A Deactivate (a
	/// GM's dialog, points taken away) removes it with its rest.</summary>
	[ViewableClass]
	public partial class EnergyShieldPlugin {
		private const int recallSpellId = 32;

		public static EnergyShieldPlugin Of(Character ch) {
			return ch.GetPlugin(PluginKey.Acquire("energyShield")) as EnergyShieldPlugin;
		}

		/// <summary>Erebor's tag.energyofshield, read as a condition by the other events (no knockback, bleeding, eye
		/// drain or wraith poison while it holds). The blow that empties it leaves it below zero until the timer's next
		/// tick, as Erebor's timer=0, so that blow's own events still see a shield.</summary>
		public static bool IsUp(Character ch) {
			EnergyShieldPlugin shield = Of(ch);
			return (shield != null) && shield.HasEnergy;
		}

		public bool HasEnergy {
			get {
				return !this.resting && ((int) this.EffectPower != 0);
			}
		}

		public int ShieldPower {
			get {
				return this.shieldPower;
			}
			set {
				this.shieldPower = value;
			}
		}

		public bool Resting {
			get {
				return this.resting;
			}
		}

		/// <summary>i_energyshield's first @timer: the shield goes, the rest starts.</summary>
		internal void StartRest() {
			if (this.resting) {
				return;
			}
			this.resting = true;
			this.EffectPower = 0;
			if (this.Cont is Character self) {
				self.SysMessage("Stit se vycerpal.", ClassAbilities.ClassMessageHue);
			}
			this.Timer = EnergyShieldAbilityDef.RestSeconds;
		}

		public override void On_Timer() {
			if (!this.resting) {
				this.StartRest();
				return;
			}
			if (this.Cont is Character self) {
				self.SysMessage("Nyni muzes pouzit stit znovu.", ClassAbilities.ClassMessageHue);
			}
			this.Delete();
		}

		/// <summary>Erebor's damage_final for a hit on a shielded one: e_energy_shield's @aftergetswing takes any damage
		/// from anyone but a player (a blow, a spell, a monster's ability such as a round attack) while the shield is on,
		/// even at no energy; f_dmgFinal_applyEnergyShield then takes a spell again while energy is left, so a spell
		/// costs twice and gets through twice reduced, as Erebor ran it. Damage with no source never reached damage_final
		/// (Sphere raises no @GetHit then), nor did damage_noresponse (DamageManager.CauseDamageNoResponse: bleeding,
		/// dragon fire, hazard tiles, multishot). Called by DamageManager after the resists,
		/// before the mana shield, armor wear and minimal damage. Fixed from Erebor: its partial absorb divided by the
		/// covered share (100 damage on 10 energy hit for 790); here the covered part is let through at ShieldPower
		/// percent and the rest whole.</summary>
		internal static double Absorb(Character attacker, Character defender, double damage, bool isSpell) {
			EnergyShieldPlugin shield = Of(defender);
			if ((shield == null) || shield.resting || (attacker == null) || attacker.IsPlayer) {
				return damage;
			}
			damage = shield.Take(defender, damage, "Rana pohlcena stitem", "Rana castecne pohlcena stitem");
			if (isSpell && shield.HasEnergy) {
				damage = shield.Take(defender, damage, "Kouzlo pohlceno stitem", "Kouzlo castesne pohlceno."); //Erebor's spelling
			}
			return damage;
		}

		private double Take(Character self, double damage, string absorbed, string partly) {
			int whole = (int) damage;
			int energy = (int) this.EffectPower;
			this.EffectPower = energy - whole;
			if (energy - whole < 0) {
				int covered = Math.Max(energy, 0);
				self.SysMessage(partly, ClassAbilities.ClassMessageHue);
				this.Timer = 0; //timer=0: the rest starts at the next tick
				return ((covered * this.shieldPower) / 100) + (whole - covered);
			}
			EffectFactory.StationaryEffect(self, 0x3779, 6, 20); //i_fx_sparkle_2
			self.SysMessage(absorbed, ClassAbilities.ClassMessageHue);
			return (whole * this.shieldPower) / 100;
		}

		/// <summary>e_energy_shield's @spellcast: `argn==32 || argn==32`, so Recall is refused and Gate isn't (Erebor's
		/// typo, kept as it ran). After the casting rules, as e_energy_shield ran after e_allplayers.</summary>
		public TriggerResult On_DenySpellCast(SkillSequenceArgs mageryArgs) {
			if (!this.resting && (mageryArgs.Param1 is SpellDef spell) && (spell.Id == recallSpellId)) {
				mageryArgs.Self.SysMessage("Nelze cestovat kouzlem.");
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		/// <summary>i_energyshield is a memory item: it stays through death, with its timers.</summary>
		public override void On_Death() {
		}

		/// <summary>Its own lines say when it goes.</summary>
		protected override void EffectEndedMessage(Character cont) {
		}
	}

	[ViewableClass]
	public partial class EnergyShieldPluginDef {
	}
}
