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

		//#797 batch 4 (+ the curse rule switches the user asked for on all three area curses)
		public bool NpcCurseSpareCurrentTarget {
			get { return (MonsterSpecialAttacks.NpcCurseDef != null) && MonsterSpecialAttacks.NpcCurseDef.SpareCurrentTarget; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.SpareCurrentTarget = value; } }
		}

		public double NpcCursePetMultiplier {
			get { return MonsterSpecialAttacks.NpcCurseDef == null ? 0 : MonsterSpecialAttacks.NpcCurseDef.PetMultiplier; }
			set { if (MonsterSpecialAttacks.NpcCurseDef != null) { MonsterSpecialAttacks.NpcCurseDef.PetMultiplier = value; } }
		}

		public double[] NpcCurseManaChance {
			get { return MonsterSpecialAttacks.NpcCurseManaDef?.Chance; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.Chance = value; } }
		}

		public double[] NpcCurseManaCooldown {
			get { return MonsterSpecialAttacks.NpcCurseManaDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.Cooldown = value; } }
		}

		public double[] NpcCurseManaDamage {
			get { return MonsterSpecialAttacks.NpcCurseManaDef?.Damage; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.Damage = value; } }
		}

		public double NpcCurseManaRange {
			get { return MonsterSpecialAttacks.NpcCurseManaDef == null ? 0 : MonsterSpecialAttacks.NpcCurseManaDef.Range; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.Range = value; } }
		}

		public bool NpcCurseManaSpareCurrentTarget {
			get { return (MonsterSpecialAttacks.NpcCurseManaDef != null) && MonsterSpecialAttacks.NpcCurseManaDef.SpareCurrentTarget; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.SpareCurrentTarget = value; } }
		}

		public double NpcCurseManaPetMultiplier {
			get { return MonsterSpecialAttacks.NpcCurseManaDef == null ? 0 : MonsterSpecialAttacks.NpcCurseManaDef.PetMultiplier; }
			set { if (MonsterSpecialAttacks.NpcCurseManaDef != null) { MonsterSpecialAttacks.NpcCurseManaDef.PetMultiplier = value; } }
		}

		public double[] NpcCurseStamChance {
			get { return MonsterSpecialAttacks.NpcCurseStamDef?.Chance; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.Chance = value; } }
		}

		public double[] NpcCurseStamCooldown {
			get { return MonsterSpecialAttacks.NpcCurseStamDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.Cooldown = value; } }
		}

		public double[] NpcCurseStamDamage {
			get { return MonsterSpecialAttacks.NpcCurseStamDef?.Damage; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.Damage = value; } }
		}

		public double NpcCurseStamRange {
			get { return MonsterSpecialAttacks.NpcCurseStamDef == null ? 0 : MonsterSpecialAttacks.NpcCurseStamDef.Range; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.Range = value; } }
		}

		public bool NpcCurseStamSpareCurrentTarget {
			get { return (MonsterSpecialAttacks.NpcCurseStamDef != null) && MonsterSpecialAttacks.NpcCurseStamDef.SpareCurrentTarget; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.SpareCurrentTarget = value; } }
		}

		public double NpcCurseStamPetMultiplier {
			get { return MonsterSpecialAttacks.NpcCurseStamDef == null ? 0 : MonsterSpecialAttacks.NpcCurseStamDef.PetMultiplier; }
			set { if (MonsterSpecialAttacks.NpcCurseStamDef != null) { MonsterSpecialAttacks.NpcCurseStamDef.PetMultiplier = value; } }
		}

		public double[] PhoenixFireChance {
			get { return MonsterSpecialAttacks.PhoenixFireDef?.Chance; }
			set { if (MonsterSpecialAttacks.PhoenixFireDef != null) { MonsterSpecialAttacks.PhoenixFireDef.Chance = value; } }
		}

		public int PhoenixFireColumns {
			get { return MonsterSpecialAttacks.PhoenixFireDef == null ? 0 : MonsterSpecialAttacks.PhoenixFireDef.Columns; }
			set { if (MonsterSpecialAttacks.PhoenixFireDef != null) { MonsterSpecialAttacks.PhoenixFireDef.Columns = value; } }
		}

		public int PhoenixFireRadius {
			get { return MonsterSpecialAttacks.PhoenixFireDef == null ? 0 : MonsterSpecialAttacks.PhoenixFireDef.Radius; }
			set { if (MonsterSpecialAttacks.PhoenixFireDef != null) { MonsterSpecialAttacks.PhoenixFireDef.Radius = value; } }
		}

		public double[] PhoenixFireDamage {
			get { return MonsterSpecialAttacks.PhoenixFireDef?.Damage; }
			set { if (MonsterSpecialAttacks.PhoenixFireDef != null) { MonsterSpecialAttacks.PhoenixFireDef.Damage = value; } }
		}

		public double PhoenixHitsThreshold {
			get { return MonsterSpecialAttacks.PhoenixDef == null ? 0 : MonsterSpecialAttacks.PhoenixDef.HitsThreshold; }
			set { if (MonsterSpecialAttacks.PhoenixDef != null) { MonsterSpecialAttacks.PhoenixDef.HitsThreshold = value; } }
		}

		public int PhoenixMinMaxHits {
			get { return MonsterSpecialAttacks.PhoenixDef == null ? 0 : MonsterSpecialAttacks.PhoenixDef.MinMaxHits; }
			set { if (MonsterSpecialAttacks.PhoenixDef != null) { MonsterSpecialAttacks.PhoenixDef.MinMaxHits = value; } }
		}

		public double PhoenixCursedFailChance {
			get { return MonsterSpecialAttacks.PhoenixDef == null ? 0 : MonsterSpecialAttacks.PhoenixDef.CursedFailChance; }
			set { if (MonsterSpecialAttacks.PhoenixDef != null) { MonsterSpecialAttacks.PhoenixDef.CursedFailChance = value; } }
		}

		public double[] PhoenixRingDamage {
			get { return MonsterSpecialAttacks.PhoenixDef?.RingDamage; }
			set { if (MonsterSpecialAttacks.PhoenixDef != null) { MonsterSpecialAttacks.PhoenixDef.RingDamage = value; } }
		}

		public double[] ExplodeDamage {
			get { return MonsterSpecialAttacks.ExplodeDef?.Damage; }
			set { if (MonsterSpecialAttacks.ExplodeDef != null) { MonsterSpecialAttacks.ExplodeDef.Damage = value; } }
		}

		public double[] DragonRegenerationAmount {
			get { return MonsterSpecialAttacks.DragonRegenerationDef?.Amount; }
			set { if (MonsterSpecialAttacks.DragonRegenerationDef != null) { MonsterSpecialAttacks.DragonRegenerationDef.Amount = value; } }
		}

		public double[] DragonRegenerationInterval {
			get { return MonsterSpecialAttacks.DragonRegenerationDef?.Interval; }
			set { if (MonsterSpecialAttacks.DragonRegenerationDef != null) { MonsterSpecialAttacks.DragonRegenerationDef.Interval = value; } }
		}

		public double[] LifeLeechAmount {
			get { return MonsterSpecialAttacks.LifeLeechDef?.Amount; }
			set { if (MonsterSpecialAttacks.LifeLeechDef != null) { MonsterSpecialAttacks.LifeLeechDef.Amount = value; } }
		}

		public double[] ReactiveArmorPercent {
			get { return MonsterSpecialAttacks.ReactiveArmorDef?.Percent; }
			set { if (MonsterSpecialAttacks.ReactiveArmorDef != null) { MonsterSpecialAttacks.ReactiveArmorDef.Percent = value; } }
		}

		public int ReactiveArmorRange {
			get { return MonsterSpecialAttacks.ReactiveArmorDef == null ? 0 : MonsterSpecialAttacks.ReactiveArmorDef.Range; }
			set { if (MonsterSpecialAttacks.ReactiveArmorDef != null) { MonsterSpecialAttacks.ReactiveArmorDef.Range = value; } }
		}

		public double[] StamDmgChance {
			get { return MonsterSpecialAttacks.StamDmgDef?.Chance; }
			set { if (MonsterSpecialAttacks.StamDmgDef != null) { MonsterSpecialAttacks.StamDmgDef.Chance = value; } }
		}

		public double[] StamDmgPower {
			get { return MonsterSpecialAttacks.StamDmgDef?.Power; }
			set { if (MonsterSpecialAttacks.StamDmgDef != null) { MonsterSpecialAttacks.StamDmgDef.Power = value; } }
		}

		public double[] LightningsChance {
			get { return MonsterSpecialAttacks.LightningsDef?.Chance; }
			set { if (MonsterSpecialAttacks.LightningsDef != null) { MonsterSpecialAttacks.LightningsDef.Chance = value; } }
		}

		public double[] LightningsRange {
			get { return MonsterSpecialAttacks.LightningsDef?.Range; }
			set { if (MonsterSpecialAttacks.LightningsDef != null) { MonsterSpecialAttacks.LightningsDef.Range = value; } }
		}

		public double[] LightningsDamage {
			get { return MonsterSpecialAttacks.LightningsDef?.Damage; }
			set { if (MonsterSpecialAttacks.LightningsDef != null) { MonsterSpecialAttacks.LightningsDef.Damage = value; } }
		}

		public int SummonEaterVampStatCap {
			get { return MonsterSpecialAttacks.SummonEaterVampDef == null ? 0 : MonsterSpecialAttacks.SummonEaterVampDef.StatCap; }
			set { if (MonsterSpecialAttacks.SummonEaterVampDef != null) { MonsterSpecialAttacks.SummonEaterVampDef.StatCap = value; } }
		}

		public double ManaGainHitsStep {
			get { return MonsterSpecialAttacks.ManaGainDef == null ? 0 : MonsterSpecialAttacks.ManaGainDef.HitsStep; }
			set { if (MonsterSpecialAttacks.ManaGainDef != null) { MonsterSpecialAttacks.ManaGainDef.HitsStep = value; } }
		}

		public double ManaGainManaGain {
			get { return MonsterSpecialAttacks.ManaGainDef == null ? 0 : MonsterSpecialAttacks.ManaGainDef.ManaGain; }
			set { if (MonsterSpecialAttacks.ManaGainDef != null) { MonsterSpecialAttacks.ManaGainDef.ManaGain = value; } }
		}

		public int InexhaustibleStamPerStep {
			get { return MonsterSpecialAttacks.InexhaustibleDef == null ? 0 : MonsterSpecialAttacks.InexhaustibleDef.StamPerStep; }
			set { if (MonsterSpecialAttacks.InexhaustibleDef != null) { MonsterSpecialAttacks.InexhaustibleDef.StamPerStep = value; } }
		}

		public double[] PushallChance {
			get { return MonsterSpecialAttacks.PushallDef?.Chance; }
			set { if (MonsterSpecialAttacks.PushallDef != null) { MonsterSpecialAttacks.PushallDef.Chance = value; } }
		}

		public double[] PushallCooldown {
			get { return MonsterSpecialAttacks.PushallDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.PushallDef != null) { MonsterSpecialAttacks.PushallDef.Cooldown = value; } }
		}

		public int PushallRange {
			get { return MonsterSpecialAttacks.PushallDef == null ? 0 : MonsterSpecialAttacks.PushallDef.Range; }
			set { if (MonsterSpecialAttacks.PushallDef != null) { MonsterSpecialAttacks.PushallDef.Range = value; } }
		}

		public double[] MagicShieldStrength {
			get { return MonsterSpecialAttacks.MagicShieldDef?.Strength; }
			set { if (MonsterSpecialAttacks.MagicShieldDef != null) { MonsterSpecialAttacks.MagicShieldDef.Strength = value; } }
		}

		public int MagicShieldMysticPierceLevel {
			get { return MonsterSpecialAttacks.MagicShieldDef == null ? 0 : MonsterSpecialAttacks.MagicShieldDef.MysticPierceLevel; }
			set { if (MonsterSpecialAttacks.MagicShieldDef != null) { MonsterSpecialAttacks.MagicShieldDef.MysticPierceLevel = value; } }
		}

		public double AutoReflectPower {
			get { return MonsterSpecialAttacks.AutoReflectDef == null ? 0 : MonsterSpecialAttacks.AutoReflectDef.Power; }
			set { if (MonsterSpecialAttacks.AutoReflectDef != null) { MonsterSpecialAttacks.AutoReflectDef.Power = value; } }
		}

		//#799 batch 5
		public double[] MultishotChance {
			get { return MonsterSpecialAttacks.MultishotDef?.Chance; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.Chance = value; } }
		}

		public double[] MultishotCooldown {
			get { return MonsterSpecialAttacks.MultishotDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.Cooldown = value; } }
		}

		public double[] MultishotDamage {
			get { return MonsterSpecialAttacks.MultishotDef?.Damage; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.Damage = value; } }
		}

		public double MultishotRange {
			get { return MonsterSpecialAttacks.MultishotDef == null ? 0 : MonsterSpecialAttacks.MultishotDef.Range; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.Range = value; } }
		}

		public bool MultishotSpareCurrentTarget {
			get { return (MonsterSpecialAttacks.MultishotDef != null) && MonsterSpecialAttacks.MultishotDef.SpareCurrentTarget; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.SpareCurrentTarget = value; } }
		}

		public double MultishotPetMultiplier {
			get { return MonsterSpecialAttacks.MultishotDef == null ? 0 : MonsterSpecialAttacks.MultishotDef.PetMultiplier; }
			set { if (MonsterSpecialAttacks.MultishotDef != null) { MonsterSpecialAttacks.MultishotDef.PetMultiplier = value; } }
		}

		public double[] EyeStamDrain {
			get { return MonsterSpecialAttacks.EyeDef?.StamDrain; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.StamDrain = value; } }
		}

		public double EyeLeechMultiplier {
			get { return MonsterSpecialAttacks.EyeDef == null ? 0 : MonsterSpecialAttacks.EyeDef.LeechMultiplier; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.LeechMultiplier = value; } }
		}

		public double EyeHitsCapMultiplier {
			get { return MonsterSpecialAttacks.EyeDef == null ? 0 : MonsterSpecialAttacks.EyeDef.HitsCapMultiplier; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.HitsCapMultiplier = value; } }
		}

		public int EyePlayerManaCost {
			get { return MonsterSpecialAttacks.EyeDef == null ? 0 : MonsterSpecialAttacks.EyeDef.PlayerManaCost; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.PlayerManaCost = value; } }
		}

		public double EyeDeflectChance {
			get { return MonsterSpecialAttacks.EyeDef == null ? 0 : MonsterSpecialAttacks.EyeDef.DeflectChance; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.DeflectChance = value; } }
		}

		public double[] EyeAttentionInt {
			get { return MonsterSpecialAttacks.EyeDef?.AttentionInt; }
			set { if (MonsterSpecialAttacks.EyeDef != null) { MonsterSpecialAttacks.EyeDef.AttentionInt = value; } }
		}

		public double GolemChanceAbove75 {
			get { return MonsterSpecialAttacks.GolemDef == null ? 0 : MonsterSpecialAttacks.GolemDef.ChanceAbove75; }
			set { if (MonsterSpecialAttacks.GolemDef != null) { MonsterSpecialAttacks.GolemDef.ChanceAbove75 = value; } }
		}

		public double GolemChanceAbove50 {
			get { return MonsterSpecialAttacks.GolemDef == null ? 0 : MonsterSpecialAttacks.GolemDef.ChanceAbove50; }
			set { if (MonsterSpecialAttacks.GolemDef != null) { MonsterSpecialAttacks.GolemDef.ChanceAbove50 = value; } }
		}

		public double GolemChanceAbove25 {
			get { return MonsterSpecialAttacks.GolemDef == null ? 0 : MonsterSpecialAttacks.GolemDef.ChanceAbove25; }
			set { if (MonsterSpecialAttacks.GolemDef != null) { MonsterSpecialAttacks.GolemDef.ChanceAbove25 = value; } }
		}

		public double GolemChanceAbove10 {
			get { return MonsterSpecialAttacks.GolemDef == null ? 0 : MonsterSpecialAttacks.GolemDef.ChanceAbove10; }
			set { if (MonsterSpecialAttacks.GolemDef != null) { MonsterSpecialAttacks.GolemDef.ChanceAbove10 = value; } }
		}

		public double GolemChanceBelow10 {
			get { return MonsterSpecialAttacks.GolemDef == null ? 0 : MonsterSpecialAttacks.GolemDef.ChanceBelow10; }
			set { if (MonsterSpecialAttacks.GolemDef != null) { MonsterSpecialAttacks.GolemDef.ChanceBelow10 = value; } }
		}

		public double BackstabHideDelay {
			get { return MonsterSpecialAttacks.BackstabDef == null ? 0 : MonsterSpecialAttacks.BackstabDef.HideDelay; }
			set { if (MonsterSpecialAttacks.BackstabDef != null) { MonsterSpecialAttacks.BackstabDef.HideDelay = value; } }
		}

		public double BackstabDamageFactor {
			get { return MonsterSpecialAttacks.BackstabDef == null ? 0 : MonsterSpecialAttacks.BackstabDef.DamageFactor; }
			set { if (MonsterSpecialAttacks.BackstabDef != null) { MonsterSpecialAttacks.BackstabDef.DamageFactor = value; } }
		}

		public double BackstabRehideDelay {
			get { return MonsterSpecialAttacks.BackstabDef == null ? 0 : MonsterSpecialAttacks.BackstabDef.RehideDelay; }
			set { if (MonsterSpecialAttacks.BackstabDef != null) { MonsterSpecialAttacks.BackstabDef.RehideDelay = value; } }
		}

		public double BackstabInterruptDelay {
			get { return MonsterSpecialAttacks.BackstabDef == null ? 0 : MonsterSpecialAttacks.BackstabDef.InterruptDelay; }
			set { if (MonsterSpecialAttacks.BackstabDef != null) { MonsterSpecialAttacks.BackstabDef.InterruptDelay = value; } }
		}

		public int BackstabNoticeRange {
			get { return MonsterSpecialAttacks.BackstabDef == null ? 0 : MonsterSpecialAttacks.BackstabDef.NoticeRange; }
			set { if (MonsterSpecialAttacks.BackstabDef != null) { MonsterSpecialAttacks.BackstabDef.NoticeRange = value; } }
		}

		public double[] MassLeapChance {
			get { return MonsterSpecialAttacks.MassLeapDef?.Chance; }
			set { if (MonsterSpecialAttacks.MassLeapDef != null) { MonsterSpecialAttacks.MassLeapDef.Chance = value; } }
		}

		public int MassLeapRange {
			get { return MonsterSpecialAttacks.MassLeapDef == null ? 0 : MonsterSpecialAttacks.MassLeapDef.Range; }
			set { if (MonsterSpecialAttacks.MassLeapDef != null) { MonsterSpecialAttacks.MassLeapDef.Range = value; } }
		}

		public double MassLeapFirstDelay {
			get { return MonsterSpecialAttacks.MassLeapDef == null ? 0 : MonsterSpecialAttacks.MassLeapDef.FirstDelay; }
			set { if (MonsterSpecialAttacks.MassLeapDef != null) { MonsterSpecialAttacks.MassLeapDef.FirstDelay = value; } }
		}

		public double[] MassLeapHopDelay {
			get { return MonsterSpecialAttacks.MassLeapDef?.HopDelay; }
			set { if (MonsterSpecialAttacks.MassLeapDef != null) { MonsterSpecialAttacks.MassLeapDef.HopDelay = value; } }
		}

		public int DuplicatorDupeCount {
			get { return MonsterSpecialAttacks.DuplicatorDef == null ? 0 : MonsterSpecialAttacks.DuplicatorDef.DupeCount; }
			set { if (MonsterSpecialAttacks.DuplicatorDef != null) { MonsterSpecialAttacks.DuplicatorDef.DupeCount = value; } }
		}

		public double[] CraftMonsterBonus {
			get { return MonsterSpecialAttacks.CraftMonsterDef?.Bonus; }
			set { if (MonsterSpecialAttacks.CraftMonsterDef != null) { MonsterSpecialAttacks.CraftMonsterDef.Bonus = value; } }
		}

		public double CraftMonsterCritChance {
			get { return MonsterSpecialAttacks.CraftMonsterDef == null ? 0 : MonsterSpecialAttacks.CraftMonsterDef.CritChance; }
			set { if (MonsterSpecialAttacks.CraftMonsterDef != null) { MonsterSpecialAttacks.CraftMonsterDef.CritChance = value; } }
		}

		public double[] CraftMonsterCritBonus {
			get { return MonsterSpecialAttacks.CraftMonsterDef?.CritBonus; }
			set { if (MonsterSpecialAttacks.CraftMonsterDef != null) { MonsterSpecialAttacks.CraftMonsterDef.CritBonus = value; } }
		}

		public double[] WarcryCooldown {
			get { return MonsterSpecialAttacks.WarcryDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.WarcryDef != null) { MonsterSpecialAttacks.WarcryDef.Cooldown = value; } }
		}

		public int WarcryRange {
			get { return MonsterSpecialAttacks.WarcryDef == null ? 0 : MonsterSpecialAttacks.WarcryDef.Range; }
			set { if (MonsterSpecialAttacks.WarcryDef != null) { MonsterSpecialAttacks.WarcryDef.Range = value; } }
		}

		//#807 - e_item_refuser; {0} is the refused item's name
		public string ItemRefuserMessage {
			get { return MonsterSpecialAttacks.ItemRefuserDef?.Message; }
			set { if (MonsterSpecialAttacks.ItemRefuserDef != null) { MonsterSpecialAttacks.ItemRefuserDef.Message = value; } }
		}

		//#827 - e_healer: how close a ghost must be to the healer it clicks
		public int HealerClickRange {
			get { return (MonsterSpecialAttacks.HealerDef != null) ? MonsterSpecialAttacks.HealerDef.ClickRange : 0; }
			set { if (MonsterSpecialAttacks.HealerDef != null) { MonsterSpecialAttacks.HealerDef.ClickRange = value; } }
		}

		public double[] DispelBombChance {
			get { return MonsterSpecialAttacks.DispelBombDef?.Chance; }
			set { if (MonsterSpecialAttacks.DispelBombDef != null) { MonsterSpecialAttacks.DispelBombDef.Chance = value; } }
		}

		public double[] DispelBombCooldown {
			get { return MonsterSpecialAttacks.DispelBombDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.DispelBombDef != null) { MonsterSpecialAttacks.DispelBombDef.Cooldown = value; } }
		}

		public double[] DispelBombThrowRange {
			get { return MonsterSpecialAttacks.DispelBombDef?.ThrowRange; }
			set { if (MonsterSpecialAttacks.DispelBombDef != null) { MonsterSpecialAttacks.DispelBombDef.ThrowRange = value; } }
		}

		public double DispelBombFuse {
			get { return MonsterSpecialAttacks.DispelBombDef == null ? 0 : MonsterSpecialAttacks.DispelBombDef.Fuse; }
			set { if (MonsterSpecialAttacks.DispelBombDef != null) { MonsterSpecialAttacks.DispelBombDef.Fuse = value; } }
		}

		public int DispelBombBlastRange {
			get { return MonsterSpecialAttacks.DispelBombDef == null ? 0 : MonsterSpecialAttacks.DispelBombDef.BlastRange; }
			set { if (MonsterSpecialAttacks.DispelBombDef != null) { MonsterSpecialAttacks.DispelBombDef.BlastRange = value; } }
		}

		public int GoldDropDropOneIn {
			get { return MonsterSpecialAttacks.GoldDropDef == null ? 0 : MonsterSpecialAttacks.GoldDropDef.DropOneIn; }
			set { if (MonsterSpecialAttacks.GoldDropDef != null) { MonsterSpecialAttacks.GoldDropDef.DropOneIn = value; } }
		}

		public int ReagDropDropOneIn {
			get { return MonsterSpecialAttacks.ReagDropDef == null ? 0 : MonsterSpecialAttacks.ReagDropDef.DropOneIn; }
			set { if (MonsterSpecialAttacks.ReagDropDef != null) { MonsterSpecialAttacks.ReagDropDef.DropOneIn = value; } }
		}

		public int DiamondDropDropOneIn {
			get { return MonsterSpecialAttacks.DiamondDropDef == null ? 0 : MonsterSpecialAttacks.DiamondDropDef.DropOneIn; }
			set { if (MonsterSpecialAttacks.DiamondDropDef != null) { MonsterSpecialAttacks.DiamondDropDef.DropOneIn = value; } }
		}

		public int ScalesAbsorbOneIn {
			get { return MonsterSpecialAttacks.ScalesDef == null ? 0 : MonsterSpecialAttacks.ScalesDef.AbsorbOneIn; }
			set { if (MonsterSpecialAttacks.ScalesDef != null) { MonsterSpecialAttacks.ScalesDef.AbsorbOneIn = value; } }
		}

		public double[] ScalesAbsorbedDamage {
			get { return MonsterSpecialAttacks.ScalesDef == null ? null : MonsterSpecialAttacks.ScalesDef.AbsorbedDamage; }
			set { if (MonsterSpecialAttacks.ScalesDef != null) { MonsterSpecialAttacks.ScalesDef.AbsorbedDamage = value; } }
		}

		public int NpcManaShieldRaiseOneIn {
			get { return MonsterSpecialAttacks.NpcManaShieldDef == null ? 0 : MonsterSpecialAttacks.NpcManaShieldDef.RaiseOneIn; }
			set { if (MonsterSpecialAttacks.NpcManaShieldDef != null) { MonsterSpecialAttacks.NpcManaShieldDef.RaiseOneIn = value; } }
		}

		public double NpcManaShieldShieldSeconds {
			get { return MonsterSpecialAttacks.NpcManaShieldDef == null ? 0 : MonsterSpecialAttacks.NpcManaShieldDef.ShieldSeconds; }
			set { if (MonsterSpecialAttacks.NpcManaShieldDef != null) { MonsterSpecialAttacks.NpcManaShieldDef.ShieldSeconds = value; } }
		}

		public double NpcRegenerationFirstDelay {
			get { return MonsterSpecialAttacks.NpcRegenerationDef == null ? 0 : MonsterSpecialAttacks.NpcRegenerationDef.FirstDelay; }
			set { if (MonsterSpecialAttacks.NpcRegenerationDef != null) { MonsterSpecialAttacks.NpcRegenerationDef.FirstDelay = value; } }
		}

		public double NpcRegenerationHealPeriod {
			get { return MonsterSpecialAttacks.NpcRegenerationDef == null ? 0 : MonsterSpecialAttacks.NpcRegenerationDef.HealPeriod; }
			set { if (MonsterSpecialAttacks.NpcRegenerationDef != null) { MonsterSpecialAttacks.NpcRegenerationDef.HealPeriod = value; } }
		}

		public double[] NpcRegenerationHitsDivisor {
			get { return MonsterSpecialAttacks.NpcRegenerationDef == null ? null : MonsterSpecialAttacks.NpcRegenerationDef.HitsDivisor; }
			set { if (MonsterSpecialAttacks.NpcRegenerationDef != null) { MonsterSpecialAttacks.NpcRegenerationDef.HitsDivisor = value; } }
		}

		public int NpcRegenerationMostHeals {
			get { return MonsterSpecialAttacks.NpcRegenerationDef == null ? 0 : MonsterSpecialAttacks.NpcRegenerationDef.MostHeals; }
			set { if (MonsterSpecialAttacks.NpcRegenerationDef != null) { MonsterSpecialAttacks.NpcRegenerationDef.MostHeals = value; } }
		}

		public double[] ResistCountdownCountdownChance {
			get { return MonsterSpecialAttacks.ResistCountdownDef == null ? null : MonsterSpecialAttacks.ResistCountdownDef.CountdownChance; }
			set { if (MonsterSpecialAttacks.ResistCountdownDef != null) { MonsterSpecialAttacks.ResistCountdownDef.CountdownChance = value; } }
		}

		public double[] ResistCountdownCountdownAmount {
			get { return MonsterSpecialAttacks.ResistCountdownDef == null ? null : MonsterSpecialAttacks.ResistCountdownDef.CountdownAmount; }
			set { if (MonsterSpecialAttacks.ResistCountdownDef != null) { MonsterSpecialAttacks.ResistCountdownDef.CountdownAmount = value; } }
		}

		public int ResistCountdownImmuneFrom {
			get { return MonsterSpecialAttacks.ResistCountdownDef == null ? 0 : MonsterSpecialAttacks.ResistCountdownDef.ImmuneFrom; }
			set { if (MonsterSpecialAttacks.ResistCountdownDef != null) { MonsterSpecialAttacks.ResistCountdownDef.ImmuneFrom = value; } }
		}

		public double Phoenix25RebornShare {
			get { return MonsterSpecialAttacks.Phoenix25Def == null ? 0 : MonsterSpecialAttacks.Phoenix25Def.RebornShare; }
			set { if (MonsterSpecialAttacks.Phoenix25Def != null) { MonsterSpecialAttacks.Phoenix25Def.RebornShare = value; } }
		}

		public double[] GoldDropDropAmount {
			get { return MonsterSpecialAttacks.GoldDropDef == null ? null : MonsterSpecialAttacks.GoldDropDef.DropAmount; }
			set { if (MonsterSpecialAttacks.GoldDropDef != null) { MonsterSpecialAttacks.GoldDropDef.DropAmount = value; } }
		}

		public string GoldDropDropItems {
			get { return MonsterSpecialAttacks.GoldDropDef == null ? null : MonsterSpecialAttacks.GoldDropDef.DropItems; }
			set { if (MonsterSpecialAttacks.GoldDropDef != null) { MonsterSpecialAttacks.GoldDropDef.DropItems = value; } }
		}

		public int GoldDropDropSound {
			get { return MonsterSpecialAttacks.GoldDropDef == null ? 0 : MonsterSpecialAttacks.GoldDropDef.DropSound; }
			set { if (MonsterSpecialAttacks.GoldDropDef != null) { MonsterSpecialAttacks.GoldDropDef.DropSound = value; } }
		}

		public string GoldDropDropSpeech {
			get { return MonsterSpecialAttacks.GoldDropDef == null ? null : MonsterSpecialAttacks.GoldDropDef.DropSpeech; }
			set { if (MonsterSpecialAttacks.GoldDropDef != null) { MonsterSpecialAttacks.GoldDropDef.DropSpeech = value; } }
		}

		public double[] ReagDropDropAmount {
			get { return MonsterSpecialAttacks.ReagDropDef == null ? null : MonsterSpecialAttacks.ReagDropDef.DropAmount; }
			set { if (MonsterSpecialAttacks.ReagDropDef != null) { MonsterSpecialAttacks.ReagDropDef.DropAmount = value; } }
		}

		public string ReagDropDropItems {
			get { return MonsterSpecialAttacks.ReagDropDef == null ? null : MonsterSpecialAttacks.ReagDropDef.DropItems; }
			set { if (MonsterSpecialAttacks.ReagDropDef != null) { MonsterSpecialAttacks.ReagDropDef.DropItems = value; } }
		}

		public int ReagDropDropSound {
			get { return MonsterSpecialAttacks.ReagDropDef == null ? 0 : MonsterSpecialAttacks.ReagDropDef.DropSound; }
			set { if (MonsterSpecialAttacks.ReagDropDef != null) { MonsterSpecialAttacks.ReagDropDef.DropSound = value; } }
		}

		public string ReagDropDropSpeech {
			get { return MonsterSpecialAttacks.ReagDropDef == null ? null : MonsterSpecialAttacks.ReagDropDef.DropSpeech; }
			set { if (MonsterSpecialAttacks.ReagDropDef != null) { MonsterSpecialAttacks.ReagDropDef.DropSpeech = value; } }
		}

		public double[] DiamondDropDropAmount {
			get { return MonsterSpecialAttacks.DiamondDropDef == null ? null : MonsterSpecialAttacks.DiamondDropDef.DropAmount; }
			set { if (MonsterSpecialAttacks.DiamondDropDef != null) { MonsterSpecialAttacks.DiamondDropDef.DropAmount = value; } }
		}

		public string DiamondDropDropItems {
			get { return MonsterSpecialAttacks.DiamondDropDef == null ? null : MonsterSpecialAttacks.DiamondDropDef.DropItems; }
			set { if (MonsterSpecialAttacks.DiamondDropDef != null) { MonsterSpecialAttacks.DiamondDropDef.DropItems = value; } }
		}

		public int DiamondDropDropSound {
			get { return MonsterSpecialAttacks.DiamondDropDef == null ? 0 : MonsterSpecialAttacks.DiamondDropDef.DropSound; }
			set { if (MonsterSpecialAttacks.DiamondDropDef != null) { MonsterSpecialAttacks.DiamondDropDef.DropSound = value; } }
		}

		public string DiamondDropDropSpeech {
			get { return MonsterSpecialAttacks.DiamondDropDef == null ? null : MonsterSpecialAttacks.DiamondDropDef.DropSpeech; }
			set { if (MonsterSpecialAttacks.DiamondDropDef != null) { MonsterSpecialAttacks.DiamondDropDef.DropSpeech = value; } }
		}

		public double PhoenixRebornShare {
			get { return MonsterSpecialAttacks.PhoenixDef == null ? 0 : MonsterSpecialAttacks.PhoenixDef.RebornShare; }
			set { if (MonsterSpecialAttacks.PhoenixDef != null) { MonsterSpecialAttacks.PhoenixDef.RebornShare = value; } }
		}

		public int HealDrinkerHealOneIn {
			get { return MonsterSpecialAttacks.HealDrinkerDef == null ? 0 : MonsterSpecialAttacks.HealDrinkerDef.HealOneIn; }
			set { if (MonsterSpecialAttacks.HealDrinkerDef != null) { MonsterSpecialAttacks.HealDrinkerDef.HealOneIn = value; } }
		}

		public int HealDrinkerStoneskinOneIn {
			get { return MonsterSpecialAttacks.HealDrinkerDef == null ? 0 : MonsterSpecialAttacks.HealDrinkerDef.StoneskinOneIn; }
			set { if (MonsterSpecialAttacks.HealDrinkerDef != null) { MonsterSpecialAttacks.HealDrinkerDef.StoneskinOneIn = value; } }
		}

		public int HealDrinkerSpellshieldOneIn {
			get { return MonsterSpecialAttacks.HealDrinkerDef == null ? 0 : MonsterSpecialAttacks.HealDrinkerDef.SpellshieldOneIn; }
			set { if (MonsterSpecialAttacks.HealDrinkerDef != null) { MonsterSpecialAttacks.HealDrinkerDef.SpellshieldOneIn = value; } }
		}

		public int HealDrinkerHealDivisor {
			get { return MonsterSpecialAttacks.HealDrinkerDef == null ? 0 : MonsterSpecialAttacks.HealDrinkerDef.HealDivisor; }
			set { if (MonsterSpecialAttacks.HealDrinkerDef != null) { MonsterSpecialAttacks.HealDrinkerDef.HealDivisor = value; } }
		}

		public int PotionDrinkerHealOneIn {
			get { return MonsterSpecialAttacks.PotionDrinkerDef == null ? 0 : MonsterSpecialAttacks.PotionDrinkerDef.HealOneIn; }
			set { if (MonsterSpecialAttacks.PotionDrinkerDef != null) { MonsterSpecialAttacks.PotionDrinkerDef.HealOneIn = value; } }
		}

		public int PotionDrinkerStoneskinOneIn {
			get { return MonsterSpecialAttacks.PotionDrinkerDef == null ? 0 : MonsterSpecialAttacks.PotionDrinkerDef.StoneskinOneIn; }
			set { if (MonsterSpecialAttacks.PotionDrinkerDef != null) { MonsterSpecialAttacks.PotionDrinkerDef.StoneskinOneIn = value; } }
		}

		public int PotionDrinkerSpellshieldOneIn {
			get { return MonsterSpecialAttacks.PotionDrinkerDef == null ? 0 : MonsterSpecialAttacks.PotionDrinkerDef.SpellshieldOneIn; }
			set { if (MonsterSpecialAttacks.PotionDrinkerDef != null) { MonsterSpecialAttacks.PotionDrinkerDef.SpellshieldOneIn = value; } }
		}

		public int PotionDrinkerHealDivisor {
			get { return MonsterSpecialAttacks.PotionDrinkerDef == null ? 0 : MonsterSpecialAttacks.PotionDrinkerDef.HealDivisor; }
			set { if (MonsterSpecialAttacks.PotionDrinkerDef != null) { MonsterSpecialAttacks.PotionDrinkerDef.HealDivisor = value; } }
		}

		public int PotionDrinkerBoostOneIn {
			get { return MonsterSpecialAttacks.PotionDrinkerDef == null ? 0 : MonsterSpecialAttacks.PotionDrinkerDef.BoostOneIn; }
			set { if (MonsterSpecialAttacks.PotionDrinkerDef != null) { MonsterSpecialAttacks.PotionDrinkerDef.BoostOneIn = value; } }
		}

		public int BerserkDex {
			get { return MonsterSpecialAttacks.BerserkDef == null ? 0 : MonsterSpecialAttacks.BerserkDef.Dex; }
			set { if (MonsterSpecialAttacks.BerserkDef != null) { MonsterSpecialAttacks.BerserkDef.Dex = value; } }
		}

		public double[] BerserkSpeeds {
			get { return MonsterSpecialAttacks.BerserkDef == null ? null : MonsterSpecialAttacks.BerserkDef.Speeds; }
			set { if (MonsterSpecialAttacks.BerserkDef != null) { MonsterSpecialAttacks.BerserkDef.Speeds = value; } }
		}

		public double[] BerserkResistLoss {
			get { return MonsterSpecialAttacks.BerserkDef == null ? null : MonsterSpecialAttacks.BerserkDef.ResistLoss; }
			set { if (MonsterSpecialAttacks.BerserkDef != null) { MonsterSpecialAttacks.BerserkDef.ResistLoss = value; } }
		}

		public double[] BerserkThresholds {
			get { return MonsterSpecialAttacks.BerserkDef == null ? null : MonsterSpecialAttacks.BerserkDef.Thresholds; }
			set { if (MonsterSpecialAttacks.BerserkDef != null) { MonsterSpecialAttacks.BerserkDef.Thresholds = value; } }
		}

		public double BerserkCalmAbove {
			get { return MonsterSpecialAttacks.BerserkDef == null ? 0 : MonsterSpecialAttacks.BerserkDef.CalmAbove; }
			set { if (MonsterSpecialAttacks.BerserkDef != null) { MonsterSpecialAttacks.BerserkDef.CalmAbove = value; } }
		}

		public int WeaponKnockoutKnockoutOneIn {
			get { return MonsterSpecialAttacks.WeaponKnockoutDef == null ? 0 : MonsterSpecialAttacks.WeaponKnockoutDef.KnockoutOneIn; }
			set { if (MonsterSpecialAttacks.WeaponKnockoutDef != null) { MonsterSpecialAttacks.WeaponKnockoutDef.KnockoutOneIn = value; } }
		}

		public int BattleSummonerSummonOneIn {
			get { return MonsterSpecialAttacks.BattleSummonerDef == null ? 0 : MonsterSpecialAttacks.BattleSummonerDef.SummonOneIn; }
			set { if (MonsterSpecialAttacks.BattleSummonerDef != null) { MonsterSpecialAttacks.BattleSummonerDef.SummonOneIn = value; } }
		}

		public double BattleSummonerBattleSummonLifetime {
			get { return MonsterSpecialAttacks.BattleSummonerDef == null ? 0 : MonsterSpecialAttacks.BattleSummonerDef.BattleSummonLifetime; }
			set { if (MonsterSpecialAttacks.BattleSummonerDef != null) { MonsterSpecialAttacks.BattleSummonerDef.BattleSummonLifetime = value; } }
		}

		public double[] VampireLeechLeechCap {
			get { return MonsterSpecialAttacks.VampireLeechDef == null ? null : MonsterSpecialAttacks.VampireLeechDef.LeechCap; }
			set { if (MonsterSpecialAttacks.VampireLeechDef != null) { MonsterSpecialAttacks.VampireLeechDef.LeechCap = value; } }
		}

		public double[] AcidChance {
			get { return MonsterSpecialAttacks.AcidDef == null ? null : MonsterSpecialAttacks.AcidDef.Chance; }
			set { if (MonsterSpecialAttacks.AcidDef != null) { MonsterSpecialAttacks.AcidDef.Chance = value; } }
		}

		public double[] AcidDamage {
			get { return MonsterSpecialAttacks.AcidDef == null ? null : MonsterSpecialAttacks.AcidDef.Damage; }
			set { if (MonsterSpecialAttacks.AcidDef != null) { MonsterSpecialAttacks.AcidDef.Damage = value; } }
		}

		public int AcidRange {
			get { return MonsterSpecialAttacks.AcidDef == null ? 0 : MonsterSpecialAttacks.AcidDef.Range; }
			set { if (MonsterSpecialAttacks.AcidDef != null) { MonsterSpecialAttacks.AcidDef.Range = value; } }
		}

		public double BlowUpFuseSeconds {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.FuseSeconds; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.FuseSeconds = value; } }
		}

		public int BlowUpSureReach {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.SureReach; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.SureReach = value; } }
		}

		public int BlowUpReach {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.Reach; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.Reach = value; } }
		}

		public int BlowUpFarOneIn {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.FarOneIn; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.FarOneIn = value; } }
		}

		public int BlowUpDamageByDistance {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.DamageByDistance; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.DamageByDistance = value; } }
		}

		public int BlowUpNpcMultiplier {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.NpcMultiplier; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.NpcMultiplier = value; } }
		}

		public int BlowUpLowHitsDivisor {
			get { return MonsterSpecialAttacks.BlowUpDef == null ? 0 : MonsterSpecialAttacks.BlowUpDef.LowHitsDivisor; }
			set { if (MonsterSpecialAttacks.BlowUpDef != null) { MonsterSpecialAttacks.BlowUpDef.LowHitsDivisor = value; } }
		}

		public int IllusionistIllusionOneIn {
			get { return MonsterSpecialAttacks.IllusionistDef == null ? 0 : MonsterSpecialAttacks.IllusionistDef.IllusionOneIn; }
			set { if (MonsterSpecialAttacks.IllusionistDef != null) { MonsterSpecialAttacks.IllusionistDef.IllusionOneIn = value; } }
		}

		public double IllusionistCopyLifetime {
			get { return MonsterSpecialAttacks.IllusionistDef == null ? 0 : MonsterSpecialAttacks.IllusionistDef.CopyLifetime; }
			set { if (MonsterSpecialAttacks.IllusionistDef != null) { MonsterSpecialAttacks.IllusionistDef.CopyLifetime = value; } }
		}

		public int IllusionistCopyMana {
			get { return MonsterSpecialAttacks.IllusionistDef == null ? 0 : MonsterSpecialAttacks.IllusionistDef.CopyMana; }
			set { if (MonsterSpecialAttacks.IllusionistDef != null) { MonsterSpecialAttacks.IllusionistDef.CopyMana = value; } }
		}

		public double SummonKillerBoostPercent {
			get { return MonsterSpecialAttacks.SummonKillerDef == null ? 0 : MonsterSpecialAttacks.SummonKillerDef.BoostPercent; }
			set { if (MonsterSpecialAttacks.SummonKillerDef != null) { MonsterSpecialAttacks.SummonKillerDef.BoostPercent = value; } }
		}

		public double[] ElemTerrorDeathDamage {
			get { return MonsterSpecialAttacks.ElemTerrorDef == null ? null : MonsterSpecialAttacks.ElemTerrorDef.DeathDamage; }
			set { if (MonsterSpecialAttacks.ElemTerrorDef != null) { MonsterSpecialAttacks.ElemTerrorDef.DeathDamage = value; } }
		}

		public string MenivecForms {
			get { return MonsterSpecialAttacks.MenivecDef == null ? null : MonsterSpecialAttacks.MenivecDef.Forms; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.Forms = value; } }
		}

		public int MenivecMorphOneIn {
			get { return MonsterSpecialAttacks.MenivecDef == null ? 0 : MonsterSpecialAttacks.MenivecDef.MorphOneIn; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.MorphOneIn = value; } }
		}

		public double MenivecFirstDelay {
			get { return MonsterSpecialAttacks.MenivecDef == null ? 0 : MonsterSpecialAttacks.MenivecDef.FirstDelay; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.FirstDelay = value; } }
		}

		public double MenivecFormSeconds {
			get { return MonsterSpecialAttacks.MenivecDef == null ? 0 : MonsterSpecialAttacks.MenivecDef.FormSeconds; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.FormSeconds = value; } }
		}

		public double MenivecRestSeconds {
			get { return MonsterSpecialAttacks.MenivecDef == null ? 0 : MonsterSpecialAttacks.MenivecDef.RestSeconds; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.RestSeconds = value; } }
		}

		public double MenivecNearDeathRest {
			get { return MonsterSpecialAttacks.MenivecDef == null ? 0 : MonsterSpecialAttacks.MenivecDef.NearDeathRest; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.NearDeathRest = value; } }
		}

		public string PowerfulMenivecForms {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? null : MonsterSpecialAttacks.PowerfulMenivecDef.Forms; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.Forms = value; } }
		}

		public int PowerfulMenivecMorphOneIn {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? 0 : MonsterSpecialAttacks.PowerfulMenivecDef.MorphOneIn; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.MorphOneIn = value; } }
		}

		public double PowerfulMenivecFirstDelay {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? 0 : MonsterSpecialAttacks.PowerfulMenivecDef.FirstDelay; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.FirstDelay = value; } }
		}

		public double PowerfulMenivecFormSeconds {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? 0 : MonsterSpecialAttacks.PowerfulMenivecDef.FormSeconds; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.FormSeconds = value; } }
		}

		public double PowerfulMenivecRestSeconds {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? 0 : MonsterSpecialAttacks.PowerfulMenivecDef.RestSeconds; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.RestSeconds = value; } }
		}

		public double PowerfulMenivecNearDeathRest {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef == null ? 0 : MonsterSpecialAttacks.PowerfulMenivecDef.NearDeathRest; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.NearDeathRest = value; } }
		}

		public string IceMenivecForms {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? null : MonsterSpecialAttacks.IceMenivecDef.Forms; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.Forms = value; } }
		}

		public int IceMenivecMorphOneIn {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? 0 : MonsterSpecialAttacks.IceMenivecDef.MorphOneIn; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.MorphOneIn = value; } }
		}

		public double IceMenivecFirstDelay {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? 0 : MonsterSpecialAttacks.IceMenivecDef.FirstDelay; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.FirstDelay = value; } }
		}

		public double IceMenivecFormSeconds {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? 0 : MonsterSpecialAttacks.IceMenivecDef.FormSeconds; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.FormSeconds = value; } }
		}

		public double IceMenivecRestSeconds {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? 0 : MonsterSpecialAttacks.IceMenivecDef.RestSeconds; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.RestSeconds = value; } }
		}

		public double IceMenivecNearDeathRest {
			get { return MonsterSpecialAttacks.IceMenivecDef == null ? 0 : MonsterSpecialAttacks.IceMenivecDef.NearDeathRest; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.NearDeathRest = value; } }
		}
			//#1022 dragons
		public double[] FireDragonChance {
			get { return MonsterSpecialAttacks.FireDragonDef?.Chance; }
			set { if (MonsterSpecialAttacks.FireDragonDef != null) { MonsterSpecialAttacks.FireDragonDef.Chance = value; } }
		}

		public double[] FireDragonCooldown {
			get { return MonsterSpecialAttacks.FireDragonDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.FireDragonDef != null) { MonsterSpecialAttacks.FireDragonDef.Cooldown = value; } }
		}

		public double[] FireDragonDamage {
			get { return MonsterSpecialAttacks.FireDragonDef?.Damage; }
			set { if (MonsterSpecialAttacks.FireDragonDef != null) { MonsterSpecialAttacks.FireDragonDef.Damage = value; } }
		}

		public double[] FireDragonFireType {
			get { return MonsterSpecialAttacks.FireDragonDef?.FireType; }
			set { if (MonsterSpecialAttacks.FireDragonDef != null) { MonsterSpecialAttacks.FireDragonDef.FireType = value; } }
		}

		public double[] FireDragonBombChance {
			get { return MonsterSpecialAttacks.FireDragonBombDef?.Chance; }
			set { if (MonsterSpecialAttacks.FireDragonBombDef != null) { MonsterSpecialAttacks.FireDragonBombDef.Chance = value; } }
		}

		public double[] FireDragonBombCooldown {
			get { return MonsterSpecialAttacks.FireDragonBombDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.FireDragonBombDef != null) { MonsterSpecialAttacks.FireDragonBombDef.Cooldown = value; } }
		}

		public double[] FireDragonBombDamage {
			get { return MonsterSpecialAttacks.FireDragonBombDef?.Damage; }
			set { if (MonsterSpecialAttacks.FireDragonBombDef != null) { MonsterSpecialAttacks.FireDragonBombDef.Damage = value; } }
		}

		public double[] FireDragonBombFireType {
			get { return MonsterSpecialAttacks.FireDragonBombDef?.FireType; }
			set { if (MonsterSpecialAttacks.FireDragonBombDef != null) { MonsterSpecialAttacks.FireDragonBombDef.FireType = value; } }
		}

		public double[] FireDragonRoundChance {
			get { return MonsterSpecialAttacks.FireDragonRoundDef?.Chance; }
			set { if (MonsterSpecialAttacks.FireDragonRoundDef != null) { MonsterSpecialAttacks.FireDragonRoundDef.Chance = value; } }
		}

		public double[] FireDragonRoundCooldown {
			get { return MonsterSpecialAttacks.FireDragonRoundDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.FireDragonRoundDef != null) { MonsterSpecialAttacks.FireDragonRoundDef.Cooldown = value; } }
		}

		public double[] FireDragonRoundDamage {
			get { return MonsterSpecialAttacks.FireDragonRoundDef?.Damage; }
			set { if (MonsterSpecialAttacks.FireDragonRoundDef != null) { MonsterSpecialAttacks.FireDragonRoundDef.Damage = value; } }
		}

		public double[] FireDragonRoundFireType {
			get { return MonsterSpecialAttacks.FireDragonRoundDef?.FireType; }
			set { if (MonsterSpecialAttacks.FireDragonRoundDef != null) { MonsterSpecialAttacks.FireDragonRoundDef.FireType = value; } }
		}

		public double[] FireDragonRoundTurns {
			get { return MonsterSpecialAttacks.FireDragonRoundDef?.Turns; }
			set { if (MonsterSpecialAttacks.FireDragonRoundDef != null) { MonsterSpecialAttacks.FireDragonRoundDef.Turns = value; } }
		}

		public double[] FireDragonCageChance {
			get { return MonsterSpecialAttacks.FireDragonCageDef?.Chance; }
			set { if (MonsterSpecialAttacks.FireDragonCageDef != null) { MonsterSpecialAttacks.FireDragonCageDef.Chance = value; } }
		}

		public double[] FireDragonCageCooldown {
			get { return MonsterSpecialAttacks.FireDragonCageDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.FireDragonCageDef != null) { MonsterSpecialAttacks.FireDragonCageDef.Cooldown = value; } }
		}

		public double[] FireDragonCageFireType {
			get { return MonsterSpecialAttacks.FireDragonCageDef?.FireType; }
			set { if (MonsterSpecialAttacks.FireDragonCageDef != null) { MonsterSpecialAttacks.FireDragonCageDef.FireType = value; } }
		}

		public double[] FireDragonCageDuration {
			get { return MonsterSpecialAttacks.FireDragonCageDef?.Duration; }
			set { if (MonsterSpecialAttacks.FireDragonCageDef != null) { MonsterSpecialAttacks.FireDragonCageDef.Duration = value; } }
		}

		public double FireDragonCageWarriorEscape {
			get { return MonsterSpecialAttacks.FireDragonCageDef?.WarriorEscape ?? 0; }
			set { if (MonsterSpecialAttacks.FireDragonCageDef != null) { MonsterSpecialAttacks.FireDragonCageDef.WarriorEscape = value; } }
		}

		public double[] FireDragonAuraChance {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Chance; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Chance = value; } }
		}

		public double[] FireDragonAuraCooldown {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Cooldown = value; } }
		}

		public double[] FireDragonAuraInterval {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Interval; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Interval = value; } }
		}

		public double[] FireDragonAuraTicks {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Ticks; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Ticks = value; } }
		}

		public double[] FireDragonAuraDamage {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Damage; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Damage = value; } }
		}

		public double[] FireDragonAuraResist {
			get { return MonsterSpecialAttacks.FireDragonAuraDef?.Resist; }
			set { if (MonsterSpecialAttacks.FireDragonAuraDef != null) { MonsterSpecialAttacks.FireDragonAuraDef.Resist = value; } }
		}

		public double[] SwampDragonAuraChance {
			get { return MonsterSpecialAttacks.SwampDragonAuraDef?.Chance; }
			set { if (MonsterSpecialAttacks.SwampDragonAuraDef != null) { MonsterSpecialAttacks.SwampDragonAuraDef.Chance = value; } }
		}

		public double[] SwampDragonAuraCooldown {
			get { return MonsterSpecialAttacks.SwampDragonAuraDef?.Cooldown; }
			set { if (MonsterSpecialAttacks.SwampDragonAuraDef != null) { MonsterSpecialAttacks.SwampDragonAuraDef.Cooldown = value; } }
		}

		public double[] SwampDragonAuraInterval {
			get { return MonsterSpecialAttacks.SwampDragonAuraDef?.Interval; }
			set { if (MonsterSpecialAttacks.SwampDragonAuraDef != null) { MonsterSpecialAttacks.SwampDragonAuraDef.Interval = value; } }
		}

		public double[] SwampDragonAuraTicks {
			get { return MonsterSpecialAttacks.SwampDragonAuraDef?.Ticks; }
			set { if (MonsterSpecialAttacks.SwampDragonAuraDef != null) { MonsterSpecialAttacks.SwampDragonAuraDef.Ticks = value; } }
		}

		public double[] SwampDragonAuraDamage {
			get { return MonsterSpecialAttacks.SwampDragonAuraDef?.Damage; }
			set { if (MonsterSpecialAttacks.SwampDragonAuraDef != null) { MonsterSpecialAttacks.SwampDragonAuraDef.Damage = value; } }
		}

		public double DragonBloodDamage {
			get { return MonsterSpecialAttacks.DragonBloodDef?.Damage ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBloodDef != null) { MonsterSpecialAttacks.DragonBloodDef.Damage = value; } }
		}

		public double DragonBloodPoisonDamage {
			get { return MonsterSpecialAttacks.DragonBloodDef?.PoisonDamage ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBloodDef != null) { MonsterSpecialAttacks.DragonBloodDef.PoisonDamage = value; } }
		}

		public double DragonBloodPoisonTicks {
			get { return MonsterSpecialAttacks.DragonBloodDef?.PoisonTicks ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBloodDef != null) { MonsterSpecialAttacks.DragonBloodDef.PoisonTicks = value; } }
		}

		public double[] DragonBrainInterval {
			get { return MonsterSpecialAttacks.DragonBrainDef?.Interval; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.Interval = value; } }
		}

		public double DragonBrainRegeneration {
			get { return MonsterSpecialAttacks.DragonBrainDef?.Regeneration ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.Regeneration = value; } }
		}

		public double[] DragonBrainWeights {
			get { return MonsterSpecialAttacks.DragonBrainDef?.Weights; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.Weights = value; } }
		}

		public double DragonBrainStampDamage {
			get { return MonsterSpecialAttacks.DragonBrainDef?.StampDamage ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.StampDamage = value; } }
		}

		public double DragonBrainStampParalysis {
			get { return MonsterSpecialAttacks.DragonBrainDef?.StampParalysis ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.StampParalysis = value; } }
		}

		public double DragonBrainStampCritical {
			get { return MonsterSpecialAttacks.DragonBrainDef?.StampCritical ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.StampCritical = value; } }
		}

		public double DragonBrainPoisonStrength {
			get { return MonsterSpecialAttacks.DragonBrainDef?.PoisonStrength ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.PoisonStrength = value; } }
		}

		public double DragonBrainAcidDamage {
			get { return MonsterSpecialAttacks.DragonBrainDef?.AcidDamage ?? 0; }
			set { if (MonsterSpecialAttacks.DragonBrainDef != null) { MonsterSpecialAttacks.DragonBrainDef.AcidDamage = value; } }
		}

			//#1022 summoning bosses
		public int FireLordExplosionOneIn {
			get { return MonsterSpecialAttacks.FireLordDef?.ExplosionOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.FireLordDef != null) { MonsterSpecialAttacks.FireLordDef.ExplosionOneIn = value; } }
		}

		public int FireLordColumns {
			get { return MonsterSpecialAttacks.FireLordDef?.Columns ?? 0; }
			set { if (MonsterSpecialAttacks.FireLordDef != null) { MonsterSpecialAttacks.FireLordDef.Columns = value; } }
		}

		public int FireLordSummonOneIn {
			get { return MonsterSpecialAttacks.FireLordDef?.SummonOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.FireLordDef != null) { MonsterSpecialAttacks.FireLordDef.SummonOneIn = value; } }
		}

		public double FireLordSummonLifetime {
			get { return MonsterSpecialAttacks.FireLordDef?.SummonLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.FireLordDef != null) { MonsterSpecialAttacks.FireLordDef.SummonLifetime = value; } }
		}

		public double FireLordDeathDamage {
			get { return MonsterSpecialAttacks.FireLordDef?.DeathDamage ?? 0; }
			set { if (MonsterSpecialAttacks.FireLordDef != null) { MonsterSpecialAttacks.FireLordDef.DeathDamage = value; } }
		}

		public int StrachLord2TauntOneIn {
			get { return MonsterSpecialAttacks.StrachLord2Def?.TauntOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord2Def != null) { MonsterSpecialAttacks.StrachLord2Def.TauntOneIn = value; } }
		}

		public int StrachLord2DaemonOneIn {
			get { return MonsterSpecialAttacks.StrachLord2Def?.DaemonOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord2Def != null) { MonsterSpecialAttacks.StrachLord2Def.DaemonOneIn = value; } }
		}

		public int StrachLord2TarantulaOneIn {
			get { return MonsterSpecialAttacks.StrachLord2Def?.TarantulaOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord2Def != null) { MonsterSpecialAttacks.StrachLord2Def.TarantulaOneIn = value; } }
		}

		public int StrachLord2FireLordOneIn {
			get { return MonsterSpecialAttacks.StrachLord2Def?.FireLordOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord2Def != null) { MonsterSpecialAttacks.StrachLord2Def.FireLordOneIn = value; } }
		}

		public int StrachLord2ExplosionOneIn {
			get { return MonsterSpecialAttacks.StrachLord2Def?.ExplosionOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord2Def != null) { MonsterSpecialAttacks.StrachLord2Def.ExplosionOneIn = value; } }
		}

		public int SerpentDruidSummonOneIn {
			get { return MonsterSpecialAttacks.SerpentDruidDef?.SummonOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.SerpentDruidDef != null) { MonsterSpecialAttacks.SerpentDruidDef.SummonOneIn = value; } }
		}

		public double SerpentDruidSummonLifetime {
			get { return MonsterSpecialAttacks.SerpentDruidDef?.SummonLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.SerpentDruidDef != null) { MonsterSpecialAttacks.SerpentDruidDef.SummonLifetime = value; } }
		}

		public double SerpentDruidFormLifetime {
			get { return MonsterSpecialAttacks.SerpentDruidDef?.FormLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.SerpentDruidDef != null) { MonsterSpecialAttacks.SerpentDruidDef.FormLifetime = value; } }
		}

		public int CajunSummonOneIn {
			get { return MonsterSpecialAttacks.CajunDef?.SummonOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.CajunDef != null) { MonsterSpecialAttacks.CajunDef.SummonOneIn = value; } }
		}

		public double CajunSummonLifetime {
			get { return MonsterSpecialAttacks.CajunDef?.SummonLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.CajunDef != null) { MonsterSpecialAttacks.CajunDef.SummonLifetime = value; } }
		}

		public double CajunDeathDamage {
			get { return MonsterSpecialAttacks.CajunDef?.DeathDamage ?? 0; }
			set { if (MonsterSpecialAttacks.CajunDef != null) { MonsterSpecialAttacks.CajunDef.DeathDamage = value; } }
		}

		public int DroneSummonerThreeOneIn {
			get { return MonsterSpecialAttacks.DroneSummonerDef?.ThreeOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.DroneSummonerDef != null) { MonsterSpecialAttacks.DroneSummonerDef.ThreeOneIn = value; } }
		}

		public int DroneSummonerOneOneIn {
			get { return MonsterSpecialAttacks.DroneSummonerDef?.OneOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.DroneSummonerDef != null) { MonsterSpecialAttacks.DroneSummonerDef.OneOneIn = value; } }
		}

		public double DroneSummonerSummonLifetime {
			get { return MonsterSpecialAttacks.DroneSummonerDef?.SummonLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.DroneSummonerDef != null) { MonsterSpecialAttacks.DroneSummonerDef.SummonLifetime = value; } }
		}

		public int WormDropOnSight {
			get { return MonsterSpecialAttacks.WormDropDef?.OnSight ?? 0; }
			set { if (MonsterSpecialAttacks.WormDropDef != null) { MonsterSpecialAttacks.WormDropDef.OnSight = value; } }
		}

		public int WormDropOnDeath {
			get { return MonsterSpecialAttacks.WormDropDef?.OnDeath ?? 0; }
			set { if (MonsterSpecialAttacks.WormDropDef != null) { MonsterSpecialAttacks.WormDropDef.OnDeath = value; } }
		}

		public int WormDropOnSpell {
			get { return MonsterSpecialAttacks.WormDropDef?.OnSpell ?? 0; }
			set { if (MonsterSpecialAttacks.WormDropDef != null) { MonsterSpecialAttacks.WormDropDef.OnSpell = value; } }
		}

		public int SandGuardianHealPower {
			get { return MonsterSpecialAttacks.SandGuardianDef?.HealPower ?? 0; }
			set { if (MonsterSpecialAttacks.SandGuardianDef != null) { MonsterSpecialAttacks.SandGuardianDef.HealPower = value; } }
		}

		public double SandGuardianDeathDamage {
			get { return MonsterSpecialAttacks.SandGuardianDef?.DeathDamage ?? 0; }
			set { if (MonsterSpecialAttacks.SandGuardianDef != null) { MonsterSpecialAttacks.SandGuardianDef.DeathDamage = value; } }
		}

		public int FanatikTauntOneIn {
			get { return MonsterSpecialAttacks.FanatikDef?.TauntOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.FanatikDef != null) { MonsterSpecialAttacks.FanatikDef.TauntOneIn = value; } }
		}

		public double[] LavaBombChance {
			get { return MonsterSpecialAttacks.LavaBombDef?.Chance ?? null; }
			set { if (MonsterSpecialAttacks.LavaBombDef != null) { MonsterSpecialAttacks.LavaBombDef.Chance = value; } }
		}

		public double[] LavaBombRange {
			get { return MonsterSpecialAttacks.LavaBombDef?.Range ?? null; }
			set { if (MonsterSpecialAttacks.LavaBombDef != null) { MonsterSpecialAttacks.LavaBombDef.Range = value; } }
		}

		public double[] LavaBombCooldown {
			get { return MonsterSpecialAttacks.LavaBombDef?.Cooldown ?? null; }
			set { if (MonsterSpecialAttacks.LavaBombDef != null) { MonsterSpecialAttacks.LavaBombDef.Cooldown = value; } }
		}

		public double[] BombaBoyDamage {
			get { return MonsterSpecialAttacks.BombaBoyDef?.Damage ?? null; }
			set { if (MonsterSpecialAttacks.BombaBoyDef != null) { MonsterSpecialAttacks.BombaBoyDef.Damage = value; } }
		}

		public string PaladinBossGateTarget {
			get { return MonsterSpecialAttacks.PaladinBossDef?.GateTarget ?? null; }
			set { if (MonsterSpecialAttacks.PaladinBossDef != null) { MonsterSpecialAttacks.PaladinBossDef.GateTarget = value; } }
		}

		public double PaladinBossGateSeconds {
			get { return MonsterSpecialAttacks.PaladinBossDef?.GateSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.PaladinBossDef != null) { MonsterSpecialAttacks.PaladinBossDef.GateSeconds = value; } }
		}

		public string StrachLord1GateTarget {
			get { return MonsterSpecialAttacks.StrachLord1Def?.GateTarget ?? null; }
			set { if (MonsterSpecialAttacks.StrachLord1Def != null) { MonsterSpecialAttacks.StrachLord1Def.GateTarget = value; } }
		}

		public double StrachLord1GateSeconds {
			get { return MonsterSpecialAttacks.StrachLord1Def?.GateSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord1Def != null) { MonsterSpecialAttacks.StrachLord1Def.GateSeconds = value; } }
		}

		public double StrachLord1VanishSeconds {
			get { return MonsterSpecialAttacks.StrachLord1Def?.VanishSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.StrachLord1Def != null) { MonsterSpecialAttacks.StrachLord1Def.VanishSeconds = value; } }
		}

		public double OcsHelperRecordSeconds {
			get { return MonsterSpecialAttacks.OcsHelperDef?.RecordSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.OcsHelperDef != null) { MonsterSpecialAttacks.OcsHelperDef.RecordSeconds = value; } }
		}

		public double OcsResserChance {
			get { return MonsterSpecialAttacks.OcsResserDef?.Chance ?? 0; }
			set { if (MonsterSpecialAttacks.OcsResserDef != null) { MonsterSpecialAttacks.OcsResserDef.Chance = value; } }
		}

		public double OcsResserHelperLifetime {
			get { return MonsterSpecialAttacks.OcsResserDef?.HelperLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.OcsResserDef != null) { MonsterSpecialAttacks.OcsResserDef.HelperLifetime = value; } }
		}

		public int OcsResserHomeDistance {
			get { return MonsterSpecialAttacks.OcsResserDef?.HomeDistance ?? 0; }
			set { if (MonsterSpecialAttacks.OcsResserDef != null) { MonsterSpecialAttacks.OcsResserDef.HomeDistance = value; } }
		}

		public string BeridorNecroSpawnDef {
			get { return MonsterSpecialAttacks.BeridorNecroDef?.SpawnDef ?? null; }
			set { if (MonsterSpecialAttacks.BeridorNecroDef != null) { MonsterSpecialAttacks.BeridorNecroDef.SpawnDef = value; } }
		}

		public double BeridorNecroVanishSeconds {
			get { return MonsterSpecialAttacks.BeridorNecroDef?.VanishSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.BeridorNecroDef != null) { MonsterSpecialAttacks.BeridorNecroDef.VanishSeconds = value; } }
		}

		public double NecroSlavePlayerDelay {
			get { return MonsterSpecialAttacks.NecroSlaveDef?.PlayerDelay ?? 0; }
			set { if (MonsterSpecialAttacks.NecroSlaveDef != null) { MonsterSpecialAttacks.NecroSlaveDef.PlayerDelay = value; } }
		}

		public double NecroSlaveDecay {
			get { return MonsterSpecialAttacks.NecroSlaveDef?.Decay ?? 0; }
			set { if (MonsterSpecialAttacks.NecroSlaveDef != null) { MonsterSpecialAttacks.NecroSlaveDef.Decay = value; } }
		}

		public int BalrogBurnOneIn {
			get { return MonsterSpecialAttacks.BalrogDef?.BurnOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.BurnOneIn = value; } }
		}

		public double[] BalrogBurnDamage {
			get { return MonsterSpecialAttacks.BalrogDef?.BurnDamage ?? null; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.BurnDamage = value; } }
		}

		public int BalrogWeakenOneIn {
			get { return MonsterSpecialAttacks.BalrogDef?.WeakenOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.WeakenOneIn = value; } }
		}

		public int BalrogReflectOneIn {
			get { return MonsterSpecialAttacks.BalrogDef?.ReflectOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.ReflectOneIn = value; } }
		}

		public double[] BalrogReflectDamage {
			get { return MonsterSpecialAttacks.BalrogDef?.ReflectDamage ?? null; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.ReflectDamage = value; } }
		}

		public int BalrogReflectKillOneIn {
			get { return MonsterSpecialAttacks.BalrogDef?.ReflectKillOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.ReflectKillOneIn = value; } }
		}

		public int BalrogReflectKillHits {
			get { return MonsterSpecialAttacks.BalrogDef?.ReflectKillHits ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.ReflectKillHits = value; } }
		}

		public int BalrogCurseOneIn {
			get { return MonsterSpecialAttacks.BalrogDef?.CurseOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CurseOneIn = value; } }
		}

		public double BalrogCurseSeconds {
			get { return MonsterSpecialAttacks.BalrogDef?.CurseSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CurseSeconds = value; } }
		}

		public double BalrogCurseTick {
			get { return MonsterSpecialAttacks.BalrogDef?.CurseTick ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CurseTick = value; } }
		}

		public double[] BalrogCurseDamage {
			get { return MonsterSpecialAttacks.BalrogDef?.CurseDamage ?? null; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CurseDamage = value; } }
		}

		public int BalrogCurseMana {
			get { return MonsterSpecialAttacks.BalrogDef?.CurseMana ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CurseMana = value; } }
		}

		public int BalrogCastDamage {
			get { return MonsterSpecialAttacks.BalrogDef?.CastDamage ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CastDamage = value; } }
		}

		public int BalrogCastMana {
			get { return MonsterSpecialAttacks.BalrogDef?.CastMana ?? 0; }
			set { if (MonsterSpecialAttacks.BalrogDef != null) { MonsterSpecialAttacks.BalrogDef.CastMana = value; } }
		}

		public int RinaldResistStep {
			get { return MonsterSpecialAttacks.RinaldDef?.ResistStep ?? 0; }
			set { if (MonsterSpecialAttacks.RinaldDef != null) { MonsterSpecialAttacks.RinaldDef.ResistStep = value; } }
		}

		public int RinaldArmorStep {
			get { return MonsterSpecialAttacks.RinaldDef?.ArmorStep ?? 0; }
			set { if (MonsterSpecialAttacks.RinaldDef != null) { MonsterSpecialAttacks.RinaldDef.ArmorStep = value; } }
		}

		public int RinaldCraftKillOneIn {
			get { return MonsterSpecialAttacks.RinaldDef?.CraftKillOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.RinaldDef != null) { MonsterSpecialAttacks.RinaldDef.CraftKillOneIn = value; } }
		}

		public int RinaldInvulHitsStep {
			get { return MonsterSpecialAttacks.RinaldDef?.InvulHitsStep ?? 0; }
			set { if (MonsterSpecialAttacks.RinaldDef != null) { MonsterSpecialAttacks.RinaldDef.InvulHitsStep = value; } }
		}

		public int RinaldInvulHitsCap {
			get { return MonsterSpecialAttacks.RinaldDef?.InvulHitsCap ?? 0; }
			set { if (MonsterSpecialAttacks.RinaldDef != null) { MonsterSpecialAttacks.RinaldDef.InvulHitsCap = value; } }
		}

		public int OdinvisKrikCryOneIn {
			get { return MonsterSpecialAttacks.OdinvisKrikDef?.CryOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.OdinvisKrikDef != null) { MonsterSpecialAttacks.OdinvisKrikDef.CryOneIn = value; } }
		}

		public string MenivecMorphName {
			get { return MonsterSpecialAttacks.MenivecDef?.MorphName ?? null; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.MorphName = value; } }
		}

		public string MenivecMorphEmote {
			get { return MonsterSpecialAttacks.MenivecDef?.MorphEmote ?? null; }
			set { if (MonsterSpecialAttacks.MenivecDef != null) { MonsterSpecialAttacks.MenivecDef.MorphEmote = value; } }
		}

		public string PowerfulMenivecMorphName {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef?.MorphName ?? null; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.MorphName = value; } }
		}

		public string PowerfulMenivecMorphEmote {
			get { return MonsterSpecialAttacks.PowerfulMenivecDef?.MorphEmote ?? null; }
			set { if (MonsterSpecialAttacks.PowerfulMenivecDef != null) { MonsterSpecialAttacks.PowerfulMenivecDef.MorphEmote = value; } }
		}

		public string IceMenivecMorphName {
			get { return MonsterSpecialAttacks.IceMenivecDef?.MorphName ?? null; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.MorphName = value; } }
		}

		public string IceMenivecMorphEmote {
			get { return MonsterSpecialAttacks.IceMenivecDef?.MorphEmote ?? null; }
			set { if (MonsterSpecialAttacks.IceMenivecDef != null) { MonsterSpecialAttacks.IceMenivecDef.MorphEmote = value; } }
		}

		public string DruidMorphForms {
			get { return MonsterSpecialAttacks.DruidMorphDef?.Forms ?? null; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.Forms = value; } }
		}

		public int DruidMorphMorphOneIn {
			get { return MonsterSpecialAttacks.DruidMorphDef?.MorphOneIn ?? 0; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.MorphOneIn = value; } }
		}

		public double DruidMorphFirstDelay {
			get { return MonsterSpecialAttacks.DruidMorphDef?.FirstDelay ?? 0; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.FirstDelay = value; } }
		}

		public double DruidMorphFormSeconds {
			get { return MonsterSpecialAttacks.DruidMorphDef?.FormSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.FormSeconds = value; } }
		}

		public double DruidMorphRestSeconds {
			get { return MonsterSpecialAttacks.DruidMorphDef?.RestSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.RestSeconds = value; } }
		}

		public double DruidMorphNearDeathRest {
			get { return MonsterSpecialAttacks.DruidMorphDef?.NearDeathRest ?? 0; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.NearDeathRest = value; } }
		}

		public string DruidMorphMorphName {
			get { return MonsterSpecialAttacks.DruidMorphDef?.MorphName ?? null; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.MorphName = value; } }
		}

		public string DruidMorphMorphEmote {
			get { return MonsterSpecialAttacks.DruidMorphDef?.MorphEmote ?? null; }
			set { if (MonsterSpecialAttacks.DruidMorphDef != null) { MonsterSpecialAttacks.DruidMorphDef.MorphEmote = value; } }
		}

		public double DarkmoonChance {
			get { return MonsterSpecialAttacks.DarkmoonDef?.Chance ?? 0; }
			set { if (MonsterSpecialAttacks.DarkmoonDef != null) { MonsterSpecialAttacks.DarkmoonDef.Chance = value; } }
		}

		public double DarkmoonCooldown {
			get { return MonsterSpecialAttacks.DarkmoonDef?.Cooldown ?? 0; }
			set { if (MonsterSpecialAttacks.DarkmoonDef != null) { MonsterSpecialAttacks.DarkmoonDef.Cooldown = value; } }
		}

		public string DarkmoonForms {
			get { return MonsterSpecialAttacks.DarkmoonDef?.Forms ?? null; }
			set { if (MonsterSpecialAttacks.DarkmoonDef != null) { MonsterSpecialAttacks.DarkmoonDef.Forms = value; } }
		}

		public double[] RozdeleniCopyChance {
			get { return MonsterSpecialAttacks.RozdeleniDef?.CopyChance ?? null; }
			set { if (MonsterSpecialAttacks.RozdeleniDef != null) { MonsterSpecialAttacks.RozdeleniDef.CopyChance = value; } }
		}

		public int RozdeleniCopyFloor {
			get { return MonsterSpecialAttacks.RozdeleniDef?.CopyFloor ?? 0; }
			set { if (MonsterSpecialAttacks.RozdeleniDef != null) { MonsterSpecialAttacks.RozdeleniDef.CopyFloor = value; } }
		}

		public double RozdeleniCopyLifetime {
			get { return MonsterSpecialAttacks.RozdeleniDef?.CopyLifetime ?? 0; }
			set { if (MonsterSpecialAttacks.RozdeleniDef != null) { MonsterSpecialAttacks.RozdeleniDef.CopyLifetime = value; } }
		}

		public double[] FrostWaveChance {
			get { return MonsterSpecialAttacks.FrostWaveDef?.Chance ?? null; }
			set { if (MonsterSpecialAttacks.FrostWaveDef != null) { MonsterSpecialAttacks.FrostWaveDef.Chance = value; } }
		}

		public double[] FrostWaveStrength {
			get { return MonsterSpecialAttacks.FrostWaveDef?.Strength ?? null; }
			set { if (MonsterSpecialAttacks.FrostWaveDef != null) { MonsterSpecialAttacks.FrostWaveDef.Strength = value; } }
		}

		public int FrostWaveDamage {
			get { return MonsterSpecialAttacks.FrostWaveDef?.Damage ?? 0; }
			set { if (MonsterSpecialAttacks.FrostWaveDef != null) { MonsterSpecialAttacks.FrostWaveDef.Damage = value; } }
		}

		public double FrostWaveParalyzeSeconds {
			get { return MonsterSpecialAttacks.FrostWaveDef?.ParalyzeSeconds ?? 0; }
			set { if (MonsterSpecialAttacks.FrostWaveDef != null) { MonsterSpecialAttacks.FrostWaveDef.ParalyzeSeconds = value; } }
		}

		public double DropEggEggChance {
			get { return MonsterSpecialAttacks.DropEggDef?.EggChance ?? 0; }
			set { if (MonsterSpecialAttacks.DropEggDef != null) { MonsterSpecialAttacks.DropEggDef.EggChance = value; } }
		}

		public double DropEggHatchDelay {
			get { return MonsterSpecialAttacks.DropEggDef?.HatchDelay ?? 0; }
			set { if (MonsterSpecialAttacks.DropEggDef != null) { MonsterSpecialAttacks.DropEggDef.HatchDelay = value; } }
		}

		public double DropEggStompWeight {
			get { return MonsterSpecialAttacks.DropEggDef?.StompWeight ?? 0; }
			set { if (MonsterSpecialAttacks.DropEggDef != null) { MonsterSpecialAttacks.DropEggDef.StompWeight = value; } }
		}

		public double DropEggCleanupDelay {
			get { return MonsterSpecialAttacks.DropEggDef?.CleanupDelay ?? 0; }
			set { if (MonsterSpecialAttacks.DropEggDef != null) { MonsterSpecialAttacks.DropEggDef.CleanupDelay = value; } }
		}

		public double[] WarniakDropHitChance {
			get { return MonsterSpecialAttacks.WarniakDropDef?.HitChance ?? null; }
			set { if (MonsterSpecialAttacks.WarniakDropDef != null) { MonsterSpecialAttacks.WarniakDropDef.HitChance = value; } }
		}

		public double WarniakDropHitCooldown {
			get { return MonsterSpecialAttacks.WarniakDropDef?.HitCooldown ?? 0; }
			set { if (MonsterSpecialAttacks.WarniakDropDef != null) { MonsterSpecialAttacks.WarniakDropDef.HitCooldown = value; } }
		}

		public double WarniakDropSightCooldown {
			get { return MonsterSpecialAttacks.WarniakDropDef?.SightCooldown ?? 0; }
			set { if (MonsterSpecialAttacks.WarniakDropDef != null) { MonsterSpecialAttacks.WarniakDropDef.SightCooldown = value; } }
		}

		public double WarniakDropLifetime {
			get { return MonsterSpecialAttacks.WarniakDropDef?.Lifetime ?? 0; }
			set { if (MonsterSpecialAttacks.WarniakDropDef != null) { MonsterSpecialAttacks.WarniakDropDef.Lifetime = value; } }
		}

	}
}
