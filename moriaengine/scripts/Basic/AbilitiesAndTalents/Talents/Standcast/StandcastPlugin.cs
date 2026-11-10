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

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#491, #1386 - Standcast switched on (Erebor's e_standcast, systems/specabilities.scp): it does its
	/// work in its holder's triggers, as an activable ability's plugin does. At the caster's @spellCast, a cast at a target
	/// in sight gets the level bonus against monsters and freezes the caster for the spell's cast time (f_setStandcast);
	/// at @monsterSpellPower the bonus is added (magie.scp: the first monster target only, half for every target of the
	/// three area spells); raising the energy shield switches it off.</summary>
	[ViewableClass]
	public partial class StandcastPlugin {
		private const int spellExplosion = 43, spellChainLightning = 49, spellMeteorSwarm = 55, spellGateTravel = 52;

		/// <summary>#860 - dospellcustom_donesrc: what Standcast adds to the multiplier of a spell on a monster. By level,
		/// up to 200, and half for the spells that hit several targets.</summary>
		public static double CalculateLevelBonus(int level, int spellId, double perLevel) {
			int usableLevel = System.Math.Min(level, 200);
			if (IsAreaSpell(spellId)) {
				usableLevel /= 2;
			}
			return usableLevel * perLevel;
		}

		private static bool IsAreaSpell(int spellId) {
			return (spellId == spellExplosion) || (spellId == spellChainLightning) || (spellId == spellMeteorSwarm);
		}

		public void On_SpellCast(SkillSequenceArgs mageryArgs) {
			Character self = (Character) this.Cont;
			SpellDef spell = (SpellDef) mageryArgs.Param1;
			Thing target = mageryArgs.Target1 as Thing;
			//every cast starts fresh: Erebor's tag could keep an area spell's half boost for a later cast
			this.boost = 0;
			if ((target != null) && self.CanSeeLOS(target).Allow) {
				this.boost = IsAreaSpell(spell.Id) ? 2 : 1;
				if (spell.Id != spellGateTravel) { //#1391 - the gate froze its caster already (FreezeForGate), for this same cast
					CastFreezePlugin.FreezeFor(self, spell.CastTime);
				}
			}
		}

		public void On_MonsterSpellPower(MonsterSpellPowerArgs args) {
			if ((this.boost == 0) || !(args.Caster is Player player)) {
				return;
			}
			if (this.boost == 1) {
				this.boost = 0;
			}
			args.LevelBonus += CalculateLevelBonus(player.Level, args.Spell.Id, MagerySettings.instance.fieldSpellPowerPerLevel);
		}

		/// <summary>Erebor's energyshield: raising the shield ends the standcast.</summary>
		public void On_ActivateAbility(AbilityDef def, Ability ab) {
			if (def == StandcastAbilityDef.EnergyShieldDef) {
				StandcastAbilityDef.SwitchOff((Character) this.Cont, "Jsi zpatky v normalnim stavu.");
			}
		}
	}

	[ViewableClass]
	public partial class StandcastPluginDef {
	}

	/// <summary>#1386 - Erebor's flag_immobile for a cast under Standcast (f_setStandcast, its i_delayed_Function
	/// timer): no step until the timer ends.</summary>
	[ViewableClass]
	public partial class CastFreezePlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_castFreeze_");

		/// <summary>Freezes for a cast of castSeconds; a cast while still frozen changes the time left as f_setStandcast
		/// means to: more than half a second longer than this cast = 1.5 s added as a penalty, under a second left = this
		/// cast's time, else this cast's time and a second. Fixed from Erebor: its "under a second" test reads a bare
		/// name (`runningTimer`, not `arg(runningTimer)`), which Sphere reads as 0, so there the last case never happened
		/// and a recast always got just its own time; its comments say what was meant.</summary>
		public static void FreezeFor(Character ch, double castSeconds) {
			CastFreezePlugin freeze = ch.GetPlugin(key) as CastFreezePlugin;
			if (freeze == null) {
				freeze = (CastFreezePlugin) CastFreezePluginDef.instance.Create();
				ch.AddPlugin(key, freeze);
				freeze.Timer = castSeconds;
				return;
			}
			double running = freeze.Timer;
			if (running - 0.5 > castSeconds) {
				freeze.Timer = running + 1.5;
			} else if (running < 1.0) {
				freeze.Timer = castSeconds;
			} else {
				freeze.Timer = castSeconds + 1.0;
			}
		}

		/// <summary>#1391 - Erebor's e_allplayers @spellcast for Gate Travel: every player casting a gate stands still for
		/// the gate's cast time less a second (time to run into the gate). Already frozen: under the gate's time left =
		/// its time and a second (a penalty for breaking concentration early), else its time less a second. Called at the
		/// end of the gate's @start, so a cast refused there (no rune, unmarked, no way out or in) isn't frozen; Erebor
		/// froze first and refused some of those only later, keeping the freeze. Standcast adds nothing to a gate's
		/// freeze: on Erebor e_standcast ran after it and, through its bare-name bug, left it at the gate's time (9 s);
		/// its intended recast rule would have made it 10 s, a second past the gate opening; the gate's own 8 s
		/// is kept.</summary>
		public static void FreezeForGate(Character caster) {
			if (!(caster is Player)) {
				return;
			}
			double castSeconds = SpellDef.GetByDefname("s_gate_travel").CastTime;
			CastFreezePlugin freeze = caster.GetPlugin(key) as CastFreezePlugin;
			if (freeze == null) {
				freeze = (CastFreezePlugin) CastFreezePluginDef.instance.Create();
				caster.AddPlugin(key, freeze);
				freeze.Timer = castSeconds - 1.0;
			} else {
				freeze.Timer = (freeze.Timer < castSeconds) ? (castSeconds + 1.0) : (castSeconds - 1.0);
			}
		}

		/// <summary>Erebor's flag_immobile: refused before the step is worked out (Character.CheckCanMove).</summary>
		public TriggerResult On_DenyMove(bool running) {
			return TriggerResult.Cancel;
		}

		public void On_Timer() {
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class CastFreezePluginDef {
		public static readonly CastFreezePluginDef instance =
			(CastFreezePluginDef) new CastFreezePluginDef("p_cast_freeze", "C# scripts", -1).Register();
	}
}
