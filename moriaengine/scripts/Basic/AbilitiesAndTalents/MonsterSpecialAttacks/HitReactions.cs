using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_npc_golddrop, e_npc_reagdrop, e_npc_diamonddrop (new_events.scp): one hit in so many, the monster
	/// spills a little of what it carries and complains about it.</summary>
	public abstract class DropOnHitAbilityDef : PassiveAbilityDef {
		private FieldValue dropOneIn;
		private FieldValue dropAmount;
		private FieldValue dropItems;
		private FieldValue dropSound;
		private FieldValue dropSpeech;

		protected DropOnHitAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.dropOneIn = this.InitTypedField("dropOneIn", 5, typeof(int));
			this.dropAmount = this.InitTypedField("dropAmount", new double[] { 1, 10 }, typeof(double[]));
			this.dropItems = this.InitTypedField("dropItems", "", typeof(string));
			this.dropSound = this.InitTypedField("dropSound", 0, typeof(int));
			this.dropSpeech = this.InitTypedField("dropSpeech", "", typeof(string));
		}

		public int DropOneIn {
			get { return (int) this.dropOneIn.CurrentValue; }
			set { this.dropOneIn.CurrentValue = value; }
		}

		public double[] DropAmount {
			get { return (double[]) this.dropAmount.CurrentValue; }
			set { this.dropAmount.CurrentValue = value; }
		}

		/// <summary>Defnames to pick from, split by commas.</summary>
		public string DropItems {
			get { return (string) this.dropItems.CurrentValue; }
			set { this.dropItems.CurrentValue = value; }
		}

		public int DropSound {
			get { return (int) this.dropSound.CurrentValue; }
			set { this.dropSound.CurrentValue = value; }
		}

		public string DropSpeech {
			get { return (string) this.dropSpeech.CurrentValue; }
			set { this.dropSpeech.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class GoldDropAbilityDef : DropOnHitAbilityDef {
		public GoldDropAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	[ViewableClass]
	public class ReagDropAbilityDef : DropOnHitAbilityDef {
		public ReagDropAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	[ViewableClass]
	public class DiamondDropAbilityDef : DropOnHitAbilityDef {
		public DiamondDropAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	/// <summary>#1022 e_scales (new_events.scp): one blow in three glances off the scales for 1 to 4.</summary>
	[ViewableClass]
	public class ScalesAbilityDef : PassiveAbilityDef {
		private FieldValue absorbOneIn;
		private FieldValue absorbedDamage;

		public ScalesAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.absorbOneIn = this.InitTypedField("absorbOneIn", 3, typeof(int));
			this.absorbedDamage = this.InitTypedField("absorbedDamage", new double[] { 1, 4 }, typeof(double[]));
		}

		public int AbsorbOneIn {
			get { return (int) this.absorbOneIn.CurrentValue; }
			set { this.absorbOneIn.CurrentValue = value; }
		}

		public double[] AbsorbedDamage {
			get { return (double[]) this.absorbedDamage.CurrentValue; }
			set { this.absorbedDamage.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_npc_manashield and e_manashieldednpc (macros.scp): one blow in 15 raises a shield for 16 s if the mana
	/// covers it. While it holds, blows cost mana instead of hits, and one the mana can't cover ends it.</summary>
	[ViewableClass]
	public class NpcManaShieldAbilityDef : PassiveAbilityDef {
		private FieldValue raiseOneIn;
		private FieldValue seconds;

		public NpcManaShieldAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.raiseOneIn = this.InitTypedField("raiseOneIn", 15, typeof(int));
			this.seconds = this.InitTypedField("shieldSeconds", (double) 16, typeof(double));
		}

		public int RaiseOneIn {
			get { return (int) this.raiseOneIn.CurrentValue; }
			set { this.raiseOneIn.CurrentValue = value; }
		}

		public double ShieldSeconds {
			get { return (double) this.seconds.CurrentValue; }
			set { this.seconds.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_npc_regeneration and i_npc_regeneration (macros.scp): under half its hits a monster starts to mend, a
	/// tenth of its hits (or tag.regespeed) every 8 s. After 12 mendings it never does again. A curse can stop it.</summary>
	[ViewableClass]
	public class NpcRegenerationAbilityDef : PassiveAbilityDef {
		private FieldValue firstDelay;
		private FieldValue period;
		private FieldValue divisor;
		private FieldValue mostHeals;

		public NpcRegenerationAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.firstDelay = this.InitTypedField("firstDelay", (double) 3, typeof(double));
			this.period = this.InitTypedField("healPeriod", (double) 8, typeof(double));
			this.divisor = this.InitTypedField("hitsDivisor", new double[] { 10, 10 }, typeof(double[]));
			this.mostHeals = this.InitTypedField("mostHeals", 12, typeof(int));
		}

		public double FirstDelay {
			get { return (double) this.firstDelay.CurrentValue; }
			set { this.firstDelay.CurrentValue = value; }
		}

		public double HealPeriod {
			get { return (double) this.period.CurrentValue; }
			set { this.period.CurrentValue = value; }
		}

		public double[] HitsDivisor {
			get { return (double[]) this.divisor.CurrentValue; }
			set { this.divisor.CurrentValue = value; }
		}

		public int MostHeals {
			get { return (int) this.mostHeals.CurrentValue; }
			set { this.mostHeals.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_resist_countdown (new_events.scp): a player's blows wear a boss's resists down, those their own kind of
	/// attack meets. Poison and bleed stay if the monster is all but immune to them.</summary>
	[ViewableClass]
	public class ResistCountdownAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue amount;
		private FieldValue immuneFrom;

		public ResistCountdownAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("countdownChance", new double[] { 500, 500 }, typeof(double[]));
			this.amount = this.InitTypedField("countdownAmount", new double[] { 25, 25 }, typeof(double[]));
			this.immuneFrom = this.InitTypedField("immuneFrom", 950, typeof(int));
		}

		public double[] CountdownChance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		public double[] CountdownAmount {
			get { return (double[]) this.amount.CurrentValue; }
			set { this.amount.CurrentValue = value; }
		}

		public int ImmuneFrom {
			get { return (int) this.immuneFrom.CurrentValue; }
			set { this.immuneFrom.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		internal static readonly TimerKey manaShieldTK = TimerKey.Acquire("_npcManaShield_");
		internal static readonly TimerKey regenerationTK = TimerKey.Acquire("_npcRegeneration_");
		internal static readonly TagKey regenerationCountTK = TagKey.Acquire("_npcRegenerationCount_");
		internal static readonly TagKey noMoreRegenerationTK = TagKey.Acquire("_npcNoMoreRegeneration_");
		private static readonly TagKey attackedByPlayerTK = TagKey.Acquire("_npcAttackedByPlayer_");

		private static T Lookup<T>(string defname) where T : class {
			return AbstractScript.GetByDefname(defname) as T;
		}

		internal static GoldDropAbilityDef GoldDropDef { get { return Lookup<GoldDropAbilityDef>("a_tevent_e_npc_golddrop"); } }
		internal static ReagDropAbilityDef ReagDropDef { get { return Lookup<ReagDropAbilityDef>("a_tevent_e_npc_reagdrop"); } }
		internal static DiamondDropAbilityDef DiamondDropDef { get { return Lookup<DiamondDropAbilityDef>("a_tevent_e_npc_diamonddrop"); } }
		internal static ScalesAbilityDef ScalesDef { get { return Lookup<ScalesAbilityDef>("a_tevent_e_scales"); } }
		internal static NpcManaShieldAbilityDef NpcManaShieldDef { get { return Lookup<NpcManaShieldAbilityDef>("a_tevent_e_npc_manashield"); } }
		internal static NpcRegenerationAbilityDef NpcRegenerationDef { get { return Lookup<NpcRegenerationAbilityDef>("a_tevent_e_npc_regeneration"); } }
		internal static ResistCountdownAbilityDef ResistCountdownDef { get { return Lookup<ResistCountdownAbilityDef>("a_tevent_e_resist_countdown"); } }

		private static AbilityDef NoPlayerAttackDef { get { return Lookup<AbilityDef>("a_tevent_e_no_player_attack"); } }

		/// <summary>Before the damage: these can take a blow's force away.</summary>
		public static void TryAbsorbSwing(Character attacker, Character defender, WeaponSwingArgs swing) {
			swing.DamageAfterAC = DamageAfterAbsorbing(attacker, defender, swing.DamageAfterAC);
		}

		internal static double DamageAfterAbsorbing(Character attacker, Character defender, double damage) {
			damage = DamageAgainstSummons(attacker, defender, damage);
			ScalesAbilityDef scales = ScalesDef;
			if (HasAbilityDef(defender, scales) && (scales.AbsorbOneIn > 0) && (Globals.dice.Next(scales.AbsorbOneIn) == 0)) {
				attacker.SysMessage("Jeho supiny absorbuji vetsinu tveho utoku!");
				damage = ScriptUtil.GetRandInRange(scales.AbsorbedDamage[0], scales.AbsorbedDamage[1]);
			}
			NpcManaShieldAbilityDef shield = NpcManaShieldDef;
			if (!HasAbilityDef(defender, shield)) {
				return damage;
			}
			if (defender.GetTimer(manaShieldTK) != null) {
				if (defender.Mana <= damage) {
					defender.DeleteTimer(manaShieldTK);
				} else {
					defender.Mana = (short) (defender.Mana - damage);
					EffectFactory.StationaryEffect(defender, 0x375A, 6, 20);
					return 0;
				}
			} else if ((shield.RaiseOneIn > 0) && (defender.Mana >= damage) && (Globals.dice.Next(shield.RaiseOneIn) == 0)) {
				defender.Say("Beleg Del Tir");
				EffectFactory.StationaryEffect(defender, 0x2EEA, 6, 20);
				defender.AddTimer(manaShieldTK, new NpcManaShieldTimer()).DueInSeconds = shield.ShieldSeconds;
			}
			return damage;
		}

		/// <summary>#1022 - a monster's own blow landed.</summary>
		public static void TryReactToOwnSwing(Character attacker, Character defender, int damage) {
			TryLeechAboveMaxHits(attacker);
			TryAcidSplash(attacker);
			TryDrinkAfterOwnSwing(attacker);
			TrySummonerOwnSwing(attacker, defender); //#1022 the summoning bosses
			if (!defender.IsDeleted) {
				TryKnockWeaponOut(attacker, defender, damage);
				TryBlowUpOnOwnSwing(attacker, defender);
			}
		}

		/// <summary>After a blow landed and the monster lived.</summary>
		public static void TryReactToHit(Character attacker, Character defender) {
			TryDrinkOnGotHit(defender);
			if (defender is NPC hit) {
				TryBerserk(hit);
				TryBattleSummon(hit, attacker);
				TryBlowUpOnHit(hit, attacker);
				TryIllusion(hit, attacker);
				TryShapeshift(hit);
				TrySummonFrenzy(hit, attacker);
				TryKillSummonHittingGuard(hit, attacker);
				TrySummonerReactions(hit, attacker); //#1022 the summoning bosses
			}
			TryDrop(defender, GoldDropDef);
			TryDrop(defender, ReagDropDef);
			TryDrop(defender, DiamondDropDef);
			TryStartRegeneration(defender);
			if (attacker is Player player) {
				TryResistCountdown(player, defender);
				if ((defender is NPC npc) && HasAbilityDef(defender, NoPlayerAttackDef) && !defender.HasTag(attackedByPlayerTK)) {
					defender.SetTag(attackedByPlayerTK, 1);
					NPCAiTimer.Attack(npc, player);
				}
			}
		}

		internal static bool IsAxeHater(NPC npc) {
			return HasAbilityDef(npc, Lookup<AbilityDef>("a_tevent_e_guard_ent"));
		}

		/// <summary>#1100 - Erebor's e_guard or e_guard_ent.</summary>
		internal static bool HasGuardEvent(NPC npc) {
			return HasAbilityDef(npc, Lookup<AbilityDef>("a_tevent_e_guard")) || IsAxeHater(npc);
		}

		/// <summary>e_guard's @aftergetswing: a summon that strikes a guard dies.</summary>
		private static void TryKillSummonHittingGuard(NPC guard, Character attacker) {
			if ((guard.AggroMode == AggroMode.GuardsRealm) && SummonSpellDef.IsSummonedCreature(attacker)) {
				attacker.Hits = 0;
			}
		}

		private static AbilityDef MonsterEventDef { get { return Lookup<AbilityDef>("a_tevent_e_monster"); } }

		/// <summary>#1070 - whether the NPC carries Erebor's e_monster.</summary>
		internal static bool HasMonsterEvent(NPC npc) {
			return HasAbilityDef(npc, MonsterEventDef);
		}

		/// <summary>e_no_player_attack: leaves players alone until one has hit it.</summary>
		internal static bool LeavesPlayersAlone(NPC npc) {
			return HasAbilityDef(npc, NoPlayerAttackDef) && !npc.HasTag(attackedByPlayerTK);
		}

		private static void TryDrop(Character defender, DropOnHitAbilityDef def) {
			if (!HasAbilityDef(defender, def) || (def.DropOneIn <= 0) || (Globals.dice.Next(def.DropOneIn) != 0)) {
				return;
			}
			string[] items = def.DropItems.Split(',');
			ThingDef item = ThingDef.GetByDefname(items[Globals.dice.Next(items.Length)].Trim());
			if (item != null) {
				Item dropped = (Item) item.Create(defender.X, defender.Y, defender.Z, defender.M);
				dropped.Amount = (int) ScriptUtil.GetRandInRange(def.DropAmount[0], def.DropAmount[1]);
				defender.Sound(def.DropSound);
				defender.Say(def.DropSpeech);
			}
		}

		/// <summary>Also from a spell, as e_npc_regeneration's @beforegeteffect.</summary>
		public static void TryStartRegeneration(Character defender) {
			NpcRegenerationAbilityDef def = NpcRegenerationDef;
			if (HasAbilityDef(defender, def) && (defender.Hits < (defender.MaxHits / 2)) && !defender.HasTag(noMoreRegenerationTK) &&
					(defender.GetTimer(regenerationTK) == null)) {
				BoundTimer timer = new NpcRegenerationTimer();
				defender.AddTimer(regenerationTK, timer);
				timer.DueInSeconds = def.FirstDelay;
				timer.PeriodInSeconds = def.HealPeriod;
			}
		}

		/// <summary>i_npc_regeneration's @timer. False when it's over.</summary>
		internal static bool RegenerateOnce(Character self) {
			NpcRegenerationAbilityDef def = NpcRegenerationDef;
			int heals = (self.GetTag(regenerationCountTK) is int count) ? count : 0;
			if ((def == null) || (heals >= def.MostHeals)) {
				self.SetTag(noMoreRegenerationTK, 1);
				return false;
			}
			if ((self.Hits >= self.MaxHits) || (IsCursed(self) && (Globals.dice.Next(2) == 0))) {
				return false;
			}
			int divisor = (int) RollTunable((self.Def as NPCDef)?.RegenerationDivisor, def.HitsDivisor);
			self.Hits = (short) System.Math.Min(self.MaxHits, self.Hits + (self.MaxHits / System.Math.Max(divisor, 1)));
			self.Emote("regenerating");
			self.SetTag(regenerationCountTK, heals + 1);
			return true;
		}

		private static void TryResistCountdown(Player attacker, Character defender) {
			ResistCountdownAbilityDef def = ResistCountdownDef;
			if (!HasAbilityDef(defender, def)) {
				return;
			}
			NPCDef npcDef = defender.Def as NPCDef;
			if (!RollChance(RollTunable(npcDef?.ResistCountdownChance, def.CountdownChance))) {
				return;
			}
			int amount = (int) RollTunable(npcDef?.ResistCountdownAmount, def.CountdownAmount);
			if (Professions.HasProfession(attacker, Professions.Necromant) || Professions.HasProfession(attacker, Professions.Mage) ||
					Professions.HasProfession(attacker, Professions.Mystic) || Professions.HasProfession(attacker, Professions.Shaman)) {
				defender.ResistSummon -= amount;
				defender.ResistFire -= amount;
				defender.ResistCold -= amount;
				defender.ResistElectric -= amount;
				attacker.SysMessage("Citis jak " + defender.Name + " slabne vuci magii.");
			} else if (Professions.HasProfession(attacker, Professions.Thief)) {
				if (defender.ResistPoison < def.ImmuneFrom) {
					defender.ResistPoison -= amount;
				}
				if (defender.ResistBleed < def.ImmuneFrom) {
					defender.ResistBleed -= amount;
				}
				defender.ResistSlashing -= amount;
				defender.ResistStabbing -= amount;
				attacker.SysMessage("Citis jak " + defender.Name + " je zranitelnejsi.");
			} else {
				defender.ResistSharp -= amount;
				defender.ResistSlashing -= amount;
				defender.ResistBlunt -= amount;
				defender.ResistStabbing -= amount;
				attacker.SysMessage("Citis jak " + defender.Name + " je zranitelnejsi.");
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class NpcManaShieldTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public NpcManaShieldTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class NpcRegenerationTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public NpcRegenerationTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (!self.IsAliveAndValid || !MonsterSpecialAttacks.RegenerateOnce(self)) {
				self.DeleteTimer(MonsterSpecialAttacks.regenerationTK);
			}
		}
	}
}
