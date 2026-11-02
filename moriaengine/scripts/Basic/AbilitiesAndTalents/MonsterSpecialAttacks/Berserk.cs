using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_berserk_potvora (new_events.scp): as its hits fall a monster swings faster and gives up its defence, in
	/// three stages. Back above 81% of its hits it calms down and everything returns.</summary>
	[ViewableClass]
	public class BerserkAbilityDef : PassiveAbilityDef {
		private FieldValue dex;
		private FieldValue speeds;
		private FieldValue resistLoss;
		private FieldValue thresholds;
		private FieldValue calmAbove;

		public BerserkAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.dex = this.InitTypedField("berserkDex", 350, typeof(int));
			this.speeds = this.InitTypedField("berserkSpeeds", new double[] { 900, 1800, 6000 }, typeof(double[]));
			this.resistLoss = this.InitTypedField("berserkResistLoss", new double[] { 100, 200, 400 }, typeof(double[]));
			this.thresholds = this.InitTypedField("berserkThresholds", new double[] { 800, 500, 250 }, typeof(double[]));
			this.calmAbove = this.InitTypedField("berserkCalmAbove", (double) 810, typeof(double));
		}

		public int Dex {
			get { return (int) this.dex.CurrentValue; }
			set { this.dex.CurrentValue = value; }
		}

		/// <summary>Tag.npc_speed in each stage.</summary>
		public double[] Speeds {
			get { return (double[]) this.speeds.CurrentValue; }
			set { this.speeds.CurrentValue = value; }
		}

		/// <summary>Taken off seven resists on entering each stage, which adds up.</summary>
		public double[] ResistLoss {
			get { return (double[]) this.resistLoss.CurrentValue; }
			set { this.resistLoss.CurrentValue = value; }
		}

		/// <summary>Permille of maxhits under which each stage starts.</summary>
		public double[] Thresholds {
			get { return (double[]) this.thresholds.CurrentValue; }
			set { this.thresholds.CurrentValue = value; }
		}

		public double CalmAbove {
			get { return (double) this.calmAbove.CurrentValue; }
			set { this.calmAbove.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static readonly TagKey berserkStageTK = TagKey.Acquire("_npcBerserkStage_");
		private static readonly TagKey berserkDexTK = TagKey.Acquire("_npcBerserkDex_");
		private static readonly TagKey berserkSpeedTK = TagKey.Acquire("_npcBerserkSpeed_");
		private static readonly TagKey berserkLostTK = TagKey.Acquire("_npcBerserkLost_");
		private static readonly string[] berserkEmotes = { "zacina zurive utocit a prestava se branit", "se prestava ovladat", "propadl silenstvi" };

		internal static BerserkAbilityDef BerserkDef { get { return Lookup<BerserkAbilityDef>("a_tevent_e_berserk_potvora"); } }

		internal static int BerserkStage(Character self) {
			return (self.GetTag(berserkStageTK) is int stage) ? stage : 0;
		}

		internal static void TryBerserk(NPC self) {
			BerserkAbilityDef def = BerserkDef;
			if (!HasAbilityDef(self, def)) {
				return;
			}
			int stage = BerserkStage(self);
			double permille = self.Hits * 1000.0 / self.MaxHits;
			if (stage == 0) {
				if (self.Hits < self.MaxHits) {
					self.SetTag(berserkStageTK, 1);
					self.SetTag(berserkDexTK, (int) self.Dex);
					self.SetTag(berserkSpeedTK, self.SwingStamina);
				}
			} else if ((stage <= def.Thresholds.Length) && (permille < def.Thresholds[stage - 1])) {
				self.SetTag(berserkStageTK, stage + 1);
				self.Dex = (short) def.Dex;
				self.SwingStamina = (int) def.Speeds[stage - 1];
				int lost = (int) def.ResistLoss[stage - 1];
				ChangeBaseDamageResists(self, -lost);
				self.SetTag(berserkLostTK, ((self.GetTag(berserkLostTK) is int soFar) ? soFar : 0) + lost);
				self.InvalidateCombatWeaponValues();
				self.Emote(berserkEmotes[stage - 1]);
			} else if ((permille > def.CalmAbove) && (stage > 1)) {
				self.SetTag(berserkStageTK, 1);
				self.Dex = (short) (int) self.GetTag(berserkDexTK);
				self.SwingStamina = (int) self.GetTag(berserkSpeedTK);
				ChangeBaseDamageResists(self, (int) self.GetTag(berserkLostTK));
				self.RemoveTag(berserkLostTK);
				self.InvalidateCombatWeaponValues();
				self.Emote("pusobi vyrovnane a odhodlane");
			}
		}

		/// <summary>reduceBaseDmgResists: sharp, slash, pierce, blunt, fire, elect, cold.</summary>
		private static void ChangeBaseDamageResists(Character self, int by) {
			self.ResistSharp += by;
			self.ResistSlashing += by;
			self.ResistStabbing += by;
			self.ResistBlunt += by;
			self.ResistFire += by;
			self.ResistElectric += by;
			self.ResistCold += by;
		}
	}
}
