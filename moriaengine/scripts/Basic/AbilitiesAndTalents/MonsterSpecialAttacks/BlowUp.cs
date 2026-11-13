using System;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_npc_blow_up (BlowUp.scp): a walking bomb after the nearest player. Standing next to one, or being hit by
	/// it, lights a fuse of two seconds. Under a tenth of its hits it goes at once. Other monsters and summons it ignores, and
	/// sends them after its target. It leaves no body. The flying limbs and blood stains are left out.</summary>
	[ViewableClass]
	public class BlowUpAbilityDef : PassiveAbilityDef {
		private FieldValue fuseSeconds;
		private FieldValue sureReach;
		private FieldValue reach;
		private FieldValue farOneIn;
		private FieldValue damageByDistance;
		private FieldValue npcMultiplier;
		private FieldValue lowHitsDivisor;

		public BlowUpAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.fuseSeconds = this.InitTypedField("fuseSeconds", (double) 1, typeof(double));
			this.sureReach = this.InitTypedField("sureReach", 6, typeof(int));
			this.reach = this.InitTypedField("reach", 8, typeof(int));
			this.farOneIn = this.InitTypedField("farOneIn", 3, typeof(int));
			this.damageByDistance = this.InitTypedField("damageByDistance", 16, typeof(int));
			this.npcMultiplier = this.InitTypedField("npcMultiplier", 10, typeof(int));
			this.lowHitsDivisor = this.InitTypedField("lowHitsDivisor", 10, typeof(int));
		}

		/// <summary>Each of the fuse's two stages.</summary>
		public double FuseSeconds {
			get { return (double) this.fuseSeconds.CurrentValue; }
			set { this.fuseSeconds.CurrentValue = value; }
		}

		/// <summary>Everyone this near is hit.</summary>
		public int SureReach {
			get { return (int) this.sureReach.CurrentValue; }
			set { this.sureReach.CurrentValue = value; }
		}

		/// <summary>Beyond SureReach and up to here, one in FarOneIn is.</summary>
		public int Reach {
			get { return (int) this.reach.CurrentValue; }
			set { this.reach.CurrentValue = value; }
		}

		public int FarOneIn {
			get { return (int) this.farOneIn.CurrentValue; }
			set { this.farOneIn.CurrentValue = value; }
		}

		/// <summary>Damage is this times 9 minus the distance.</summary>
		public int DamageByDistance {
			get { return (int) this.damageByDistance.CurrentValue; }
			set { this.damageByDistance.CurrentValue = value; }
		}

		public int NpcMultiplier {
			get { return (int) this.npcMultiplier.CurrentValue; }
			set { this.npcMultiplier.CurrentValue = value; }
		}

		public int LowHitsDivisor {
			get { return (int) this.lowHitsDivisor.CurrentValue; }
			set { this.lowHitsDivisor.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int BlowUpHue = 0x98c, BlowUpSound = 285, BlowUpNineTiles = 9, BlowUpEffect = 0x36BD;
		internal static readonly TimerKey blowUpFuseTK = TimerKey.Acquire("_npcBlowUpFuse_");
		private static readonly TagKey blowUpDoneTK = TagKey.Acquire("_npcBlowUpDone_");
		private static readonly TagKey blowUpTargetTK = TagKey.Acquire("_npcBlowUpTarget_");

		internal static BlowUpAbilityDef BlowUpDef { get { return Lookup<BlowUpAbilityDef>("a_tevent_e_npc_blow_up"); } }

		internal static bool LeavesNoCorpse(Character self) {
			return HasAbilityDef(self, BlowUpDef) || VanishesAtDeath(self);
		}

		internal static void TryBlowUpOnHit(NPC self, Character attacker) {
			BlowUpAbilityDef def = BlowUpDef;
			if (!HasAbilityDef(self, def)) {
				return;
			}
			Character target = self.GetTag(blowUpTargetTK) as Character;
			if (!(attacker is Player player)) {
				if ((attacker is NPC other) && (target != null)) {
					NPCAiTimer.Attack(other, target);
					NPCAiTimer.Attack(self, target);
				}
				return;
			}
			if (player.IsGM) {
				return;
			}
			if (self.Hits < (self.MaxHits / def.LowHitsDivisor)) {
				BlowUp(self);
			} else if (Point2D.GetSimpleDistance(self, player) < 2) {
				LightFuse(self);
			} else if ((target == null) || target.IsDeleted || (Point2D.GetSimpleDistance(self, player) < Point2D.GetSimpleDistance(self, target))) {
				self.SetTag(blowUpTargetTK, player);
				NPCAiTimer.Attack(self, player);
			}
		}

		internal static void TryBlowUpOnOwnSwing(Character attacker, Character defender) {
			if ((attacker is NPC self) && (defender is Player) && HasAbilityDef(self, BlowUpDef)) {
				LightFuse(self);
			}
		}

		internal static void LightFuse(NPC self) {
			if ((self.GetTimer(blowUpFuseTK) == null) && !self.HasTag(blowUpDoneTK)) {
				self.Color = BlowUpHue;
				self.AddTimer(blowUpFuseTK, new BlowUpFuseTimer()).DueInSeconds = BlowUpDef.FuseSeconds;
			}
		}

		internal static void BlowUp(NPC self) {
			if (self.HasTag(blowUpDoneTK)) {
				return;
			}
			self.SetTag(blowUpDoneTK, 1);
			BlowUpAbilityDef def = BlowUpDef;
			EffectFactory.StationaryEffect(self, BlowUpEffect, 6, 15);
			self.Sound(BlowUpSound);
			foreach (AbstractCharacter near in self.GetMap().GetCharsInRange(self.X, self.Y, def.Reach)) {
				if ((near is Character other) && (other != self) && !other.Flag_Dead && !other.IsGM && !other.Flag_Hidden && !other.Flag_InvisByMagic &&
						self.CanSeeLOS(other).Allow) {
					int distance = Point2D.GetSimpleDistance(self, other);
					if ((distance <= def.SureReach) || OneIn(def.FarOneIn)) {
						int damage = def.DamageByDistance * (BlowUpNineTiles - distance) * ((other is NPC) ? def.NpcMultiplier : 1);
						Character.SetHitsBy(other, (short) Math.Max(0, other.Hits - damage), self); //dam_god: no resist, no armor; #1464 - killed by the blast
						if (other is Player) {
							other.SysMessage("Zasahl te kus tela");
						}
					}
				}
			}
			Character.SetHitsBy(self, 0, null); //#1464 - its own blast, no killer (Sphere's hits=0)
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class BlowUpFuseTimer : BoundTimer {
		[SaveableData, CopyableData]
		public bool lastStage;

		[LoadingInitializer, DeepCopyImplementation]
		public BlowUpFuseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC self = (NPC) cont;
			if (!self.IsAliveAndValid) {
				return;
			}
			if (this.lastStage) {
				MonsterSpecialAttacks.BlowUp(self);
				return;
			}
			AnimCalculator.PerformAttackAnim(self);
			BlowUpFuseTimer next = new BlowUpFuseTimer();
			next.lastStage = true;
			self.AddTimer(MonsterSpecialAttacks.blowUpFuseTK, next).DueInSeconds = MonsterSpecialAttacks.BlowUpDef.FuseSeconds;
		}
	}
}
