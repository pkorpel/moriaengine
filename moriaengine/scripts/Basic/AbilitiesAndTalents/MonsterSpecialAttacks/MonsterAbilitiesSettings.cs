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

	/// <summary>One consolidated settings page for every teventStubs monster-ability's
	/// engine-wide default (#758, user-requested) - same pass-through-property pattern as
	/// RangerSettings wrapping CombatSettings.Ranges: every property here forwards straight
	/// to the live AbilityDef instance's own FieldValue, no separate storage of its own. This
	/// means editing a value here and editing the SAME ability via .info's generic Abilities
	/// "Def info" editor are the exact same operation, always in sync - and a null AbilityDef
	/// (teventStubs.scp not loaded, shouldn't happen outside a broken boot) just means the
	/// getter/setter silently no-ops rather than throwing. Roundattack's Chance/Damage/
	/// Cooldown/Range are shared across TWO separate AbilityDef instances (the plain and
	/// "_pullall" variants, a_tevent_e_npc_roundattack/_pullall) - the setters below write to
	/// BOTH so they stay in lockstep, since nothing in real content ever configures them to
	/// diverge.</summary>
	[ViewableClass]
	public class MonsterAbilitiesSettings : SettingsMetaCategory {

		public static MonsterAbilitiesSettings instance = new MonsterAbilitiesSettings();

		public double[] StunChance {
			get { return MonsterSpecialAttacks.StunDef?.Chance; }
			set { if (MonsterSpecialAttacks.StunDef != null) { MonsterSpecialAttacks.StunDef.Chance = value; } }
		}

		public double[] StunDuration {
			get { return MonsterSpecialAttacks.StunDef?.Duration; }
			set { if (MonsterSpecialAttacks.StunDef != null) { MonsterSpecialAttacks.StunDef.Duration = value; } }
		}

		public double[] StunCooldown {
			get { return MonsterSpecialAttacks.StunDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.StunDef != null) { MonsterSpecialAttacks.StunDef.Cooldown = value; } }
		}

		public double[] CriticChance {
			get { return MonsterSpecialAttacks.CriticDef?.Chance; }
			set { if (MonsterSpecialAttacks.CriticDef != null) { MonsterSpecialAttacks.CriticDef.Chance = value; } }
		}

		public double[] CriticDamage {
			get { return MonsterSpecialAttacks.CriticDef?.Damage; }
			set { if (MonsterSpecialAttacks.CriticDef != null) { MonsterSpecialAttacks.CriticDef.Damage = value; } }
		}

		public double[] RoundattackChance {
			get { return MonsterSpecialAttacks.RoundattackDef?.Chance; }
			set {
				if (MonsterSpecialAttacks.RoundattackDef != null) { MonsterSpecialAttacks.RoundattackDef.Chance = value; }
				if (MonsterSpecialAttacks.RoundattackPullDef != null) { MonsterSpecialAttacks.RoundattackPullDef.Chance = value; }
			}
		}

		public double[] RoundattackDamage {
			get { return MonsterSpecialAttacks.RoundattackDef?.Damage; }
			set {
				if (MonsterSpecialAttacks.RoundattackDef != null) { MonsterSpecialAttacks.RoundattackDef.Damage = value; }
				if (MonsterSpecialAttacks.RoundattackPullDef != null) { MonsterSpecialAttacks.RoundattackPullDef.Damage = value; }
			}
		}

		public double[] RoundattackCooldown {
			get { return MonsterSpecialAttacks.RoundattackDef?.Cooldown; }
			set {
				if (MonsterSpecialAttacks.RoundattackDef != null) { MonsterSpecialAttacks.RoundattackDef.Cooldown = value; }
				if (MonsterSpecialAttacks.RoundattackPullDef != null) { MonsterSpecialAttacks.RoundattackPullDef.Cooldown = value; }
			}
		}

		public double RoundattackRange {
			get { return MonsterSpecialAttacks.RoundattackDef == null ? 0 : MonsterSpecialAttacks.RoundattackDef.Range; }
			set {
				if (MonsterSpecialAttacks.RoundattackDef != null) { MonsterSpecialAttacks.RoundattackDef.Range = value; }
				if (MonsterSpecialAttacks.RoundattackPullDef != null) { MonsterSpecialAttacks.RoundattackPullDef.Range = value; }
			}
		}

		public double RoundattackPullDistance {
			get { return MonsterSpecialAttacks.RoundattackPullDef == null ? 0 : MonsterSpecialAttacks.RoundattackPullDef.PullDistance; }
			set { if (MonsterSpecialAttacks.RoundattackPullDef != null) { MonsterSpecialAttacks.RoundattackPullDef.PullDistance = value; } }
		}

		public double[] BleedChance {
			get { return MonsterSpecialAttacks.BleedDef?.Chance; }
			set { if (MonsterSpecialAttacks.BleedDef != null) { MonsterSpecialAttacks.BleedDef.Chance = value; } }
		}

		public double[] KnockbackChance {
			get { return MonsterSpecialAttacks.KnockbackDef?.Chance; }
			set { if (MonsterSpecialAttacks.KnockbackDef != null) { MonsterSpecialAttacks.KnockbackDef.Chance = value; } }
		}

		/// <summary>#758 - tiles to push, stopping early at the first blocked tile. Was a fixed
		/// single-tile push before this; default 3.</summary>
		public double KnockbackDistance {
			get { return MonsterSpecialAttacks.KnockbackDef == null ? 0 : MonsterSpecialAttacks.KnockbackDef.Distance; }
			set { if (MonsterSpecialAttacks.KnockbackDef != null) { MonsterSpecialAttacks.KnockbackDef.Distance = value; } }
		}

		public double[] SelfresDelay {
			get { return MonsterSpecialAttacks.SelfReserDef?.Delay; }
			set { if (MonsterSpecialAttacks.SelfReserDef != null) { MonsterSpecialAttacks.SelfReserDef.Delay = value; } }
		}

		public double DetectRange {
			get { return MonsterSpecialAttacks.DetectDef == null ? 0 : MonsterSpecialAttacks.DetectDef.Range; }
			set { if (MonsterSpecialAttacks.DetectDef != null) { MonsterSpecialAttacks.DetectDef.Range = value; } }
		}

		public double DetectInterval {
			get { return MonsterSpecialAttacks.DetectDef == null ? 0 : MonsterSpecialAttacks.DetectDef.Interval; }
			set { if (MonsterSpecialAttacks.DetectDef != null) { MonsterSpecialAttacks.DetectDef.Interval = value; } }
		}

		/// <summary>#758 - default bumped 2 -> 7 (see LeaperAbilityDef's own doc comment for the
		/// real-run-speed math) - tune here if 7 feels too fast/slow in practice.</summary>
		public double LeaperExtraSteps {
			get { return MonsterSpecialAttacks.LeaperDef == null ? 0 : MonsterSpecialAttacks.LeaperDef.ExtraSteps; }
			set { if (MonsterSpecialAttacks.LeaperDef != null) { MonsterSpecialAttacks.LeaperDef.ExtraSteps = value; } }
		}

		public double[] ParaKrikChance {
			get { return MonsterSpecialAttacks.ParaKrikDef?.Chance; }
			set { if (MonsterSpecialAttacks.ParaKrikDef != null) { MonsterSpecialAttacks.ParaKrikDef.Chance = value; } }
		}

		public double[] ParaKrikCooldown {
			get { return MonsterSpecialAttacks.ParaKrikDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.ParaKrikDef != null) { MonsterSpecialAttacks.ParaKrikDef.Cooldown = value; } }
		}

		public double ParaKrikRange {
			get { return MonsterSpecialAttacks.ParaKrikDef == null ? 0 : MonsterSpecialAttacks.ParaKrikDef.Range; }
			set { if (MonsterSpecialAttacks.ParaKrikDef != null) { MonsterSpecialAttacks.ParaKrikDef.Range = value; } }
		}

		public double[] NpcCurseChance {
			get { return MonsterSpecialAttacks.NpcCurseDef?.Chance; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.Chance = value; } }
		}

		public double[] NpcCurseCooldown {
			get { return MonsterSpecialAttacks.NpcCurseDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.Cooldown = value; } }
		}

		public double[] NpcCurseDamage {
			get { return MonsterSpecialAttacks.NpcCurseDef?.Damage; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.Damage = value; } }
		}

		public double NpcCurseRange {
			get { return MonsterSpecialAttacks.NpcCurseDef == null ? 0 : MonsterSpecialAttacks.NpcCurseDef.Range; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.Range = value; } }
		}

		public double[] SummonerChance {
			get { return MonsterSpecialAttacks.SummonerDef?.Chance; }
			set { if (MonsterSpecialAttacks.SummonerDef != null) { MonsterSpecialAttacks.SummonerDef.Chance = value; } }
		}

		public double[] SummonerCooldown {
			get { return MonsterSpecialAttacks.SummonerDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.SummonerDef != null) { MonsterSpecialAttacks.SummonerDef.Cooldown = value; } }
		}

		public double[] SummonerLifetime {
			get { return MonsterSpecialAttacks.SummonerDef?.SummonLifetime; }
			set { if (MonsterSpecialAttacks.SummonerDef != null) { MonsterSpecialAttacks.SummonerDef.SummonLifetime = value; } }
		}

		public double[] SniperChance {
			get { return MonsterSpecialAttacks.SniperDef?.Chance; }
			set { if (MonsterSpecialAttacks.SniperDef != null) { MonsterSpecialAttacks.SniperDef.Chance = value; } }
		}

		public double[] SniperDamage {
			get { return MonsterSpecialAttacks.SniperDef?.Damage; }
			set { if (MonsterSpecialAttacks.SniperDef != null) { MonsterSpecialAttacks.SniperDef.Damage = value; } }
		}

		//#778/#783 - landed hits a strangler vine (e_ovinovac) takes before it lets go of whoever it entangled
		public int StranglerVineHitsToBreak {
			get { return MonsterSpecialAttacks.StranglerVineDef == null ? 0 : MonsterSpecialAttacks.StranglerVineDef.HitsToBreak; }
			set { if (MonsterSpecialAttacks.StranglerVineDef != null) { MonsterSpecialAttacks.StranglerVineDef.HitsToBreak = value; } }
		}
	}
}
