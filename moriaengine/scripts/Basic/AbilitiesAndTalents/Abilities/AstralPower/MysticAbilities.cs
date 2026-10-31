using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#868 - Erebor's Mystic class abilities: astralpower and switchmagic
	/// (specabilities.scp, magie_custom.scp) and the strike's effects (f_effect_a_mystic_strike).</summary>
	public static class MysticAbilities {
		private static readonly TagKey lightningTK = TagKey.Acquire("_mysticLightning_");
		private static readonly PluginKey frozenKey = PluginKey.Acquire("mysticStrikeFreeze");
		private static PassiveAbilityDef crushingBlowDef;

		/// <summary>f_setmainability: the chance out of 100000 of a class ability, by statslevel.</summary>
		public static int CalculateAbility(int statsLevel) {
			int missing = 60 - statsLevel;
			return ((3600 - (missing * missing)) * 264) / 10;
		}

		private static bool IsMystic(Character self) {
			return Professions.HasProfession(self, Professions.Mystic);
		}

		internal static bool IsAstralPowerActive(Character self) {
			return self.GetTimer(AstralPowerTimer.TimerKey) != null;
		}

		/// <summary>Whether this Mystic's strikes are lightning: vs monsters only, PvP is always cold.</summary>
		internal static bool StrikesWithLightning(Character self) {
			return self.HasTag(lightningTK);
		}

		[SteamFunction]
		public static void AstralPower(Character self) {
			Player player = self as Player;
			MysticAbilitiesLoc loc = Loc<MysticAbilitiesLoc>.Get(self.Language);
			if ((player == null) || (player.Level < 1) || !IsMystic(self)) {
				self.RedMessage(loc.OnlyMystic);
				return;
			}
			if (IsAstralPowerActive(self)) {
				self.DeleteTimer(AstralPowerTimer.TimerKey);
				return;
			}
			MagerySettings settings = MagerySettings.instance;
			if (self.Stam < settings.astralPowerStaminaCost) {
				self.RedMessage(loc.NotReady);
				return;
			}
			self.Stam -= (short) settings.astralPowerStaminaCost;
			AstralPowerTimer timer = new AstralPowerTimer();
			self.AddTimer(AstralPowerTimer.TimerKey, timer);
			timer.DueInSeconds = 1;
			timer.PeriodInSeconds = 1;
			self.WriteLine(StrikesWithLightning(self) ? loc.ParalysingStrikes : loc.FreezingStrikes);
		}

		[SteamFunction]
		public static void SwitchMagic(Character self) {
			MysticAbilitiesLoc loc = Loc<MysticAbilitiesLoc>.Get(self.Language);
			if (!IsMystic(self)) {
				self.RedMessage(loc.OnlyMystic);
				return;
			}
			MagerySettings settings = MagerySettings.instance;
			if (self.Stam <= settings.mysticSwitchStamina) {
				self.RedMessage(loc.NotReady);
				return;
			}
			self.Stam -= (short) settings.mysticSwitchStamina;
			if (StrikesWithLightning(self)) {
				self.RemoveTag(lightningTK);
				self.WriteLine(loc.NowCold);
			} else {
				self.SetTag(lightningTK, true);
				self.WriteLine(loc.NowLightning);
			}
		}

		/// <summary>specability: Astral Power has to be on, then it's a roll against the level.</summary>
		internal static bool RollSpecial(Character self) {
			Player player = self as Player;
			return (player != null) && IsAstralPowerActive(self) && (Globals.dice.Next(100001) < CalculateAbility(player.EffectiveLevel));
		}

		/// <summary>mystikDoubleswingSuccess, vs monsters: a strong strike for some stamina.</summary>
		internal static bool RollCrushingBlow(Character self) {
			if (crushingBlowDef == null) {
				crushingBlowDef = (PassiveAbilityDef) AbilityDef.GetByDefname("a_mystik_doubleswing");
			}
			int points = self.GetAbility(crushingBlowDef);
			MagerySettings settings = MagerySettings.instance;
			if ((points <= 0) || (Globals.dice.Next(1000) >= (100 + (points * 5))) || (self.Stam < settings.mysticCrushingBlowStamina)) {
				return false;
			}
			self.Stam -= (short) settings.mysticCrushingBlowStamina;
			self.SysMessage(Loc<MysticAbilitiesLoc>.Get(self.Language).CrushingBlow);
			return true;
		}

		/// <summary>The rest of f_effect_a_mystic_strike after the damage of a special strike.</summary>
		internal static void ApplySpecialStrike(Character attacker, Character defender, bool lightning) {
			MagerySettings settings = MagerySettings.instance;
			MysticAbilitiesLoc loc = Loc<MysticAbilitiesLoc>.Get(attacker.Language);
			if (lightning) {
				Player player = attacker as Player;
				if ((player == null) || (Globals.dice.Next(10001) > (CalculateAbility(player.EffectiveLevel) / 23))) {
					return;
				}
				int power = (int) settings.mysticLightningPower + (attacker.MindPowerVsM * Globals.dice.Next(8, 17));
				((SpellDef) SpellDef.GetByDefname("s_lightning")).ApplyEffectChar(attacker, defender, power, EffectFlag.FromAbility | EffectFlag.HarmfulEffect);
				attacker.SysMessage(loc.TargetStruck);
				return;
			}
			bool defenderIsPlayer = defender.IsPlayerForCombat;
			if (Globals.dice.Next(1, 1001) > (defenderIsPlayer ? settings.mysticFreezeChanceVsP : settings.mysticFreezeChanceVsM)) {
				return;
			}
			if (defenderIsPlayer && (MagicReflectionPluginDef.GetMagicReflectionPlugin(defender) != null)) {
				return;
			}
			ParalyzeEffectPluginDef.Paralyze(defender, frozenKey, TimeSpan.FromSeconds(1), attacker, EffectFlag.FromAbility);
			attacker.SysMessage(loc.TargetFrozen);
		}
	}

	/// <summary>i_spec_ability_counter: takes stamina every second, ends at under 5.</summary>
	[SaveableClass]
	public class AstralPowerTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_astralPowerTimer_");

		[LoadingInitializer]
		public AstralPowerTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = cont as Character;
			if ((self == null) || (self.Stam < 5)) {
				this.Delete();
				return;
			}
			self.Stam = (short) Math.Max(0, self.Stam - MagerySettings.instance.astralPowerStaminaPerSecond);
		}
	}

	public class MysticAbilitiesLoc : CompiledLocStringCollection<MysticAbilitiesLoc> {
		internal string OnlyMystic = "Toto umi pouzit pouze Mystik.";
		internal string NotReady = "Nejsi na to pripraveny.";
		internal string FreezingStrikes = "Tve udery jsou mrazive.";
		internal string ParalysingStrikes = "Tve udery jsou ochromujici.";
		internal string NowCold = "Nyni zasazujes mrazive udery";
		internal string NowLightning = "Nyni zasazujes udery energii";
		internal string CrushingBlow = "Zdrcujici uder!";
		internal string TargetFrozen = "Cil je zmrazen!";
		internal string TargetStruck = "Cil je ochromen!";
	}
}
