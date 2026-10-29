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
	}
}
