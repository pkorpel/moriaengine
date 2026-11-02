using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 what e_healdrinker (verbatim.scp) and e_potiondrinker (macros.scp) share: the first blow doesn't count, then
	/// under half its hits a monster may heal a third, or drink one potion at a time.</summary>
	public abstract class NpcDrinkerAbilityDef : PassiveAbilityDef {
		private FieldValue healOneIn;
		private FieldValue stoneskinOneIn;
		private FieldValue spellshieldOneIn;
		private FieldValue healDivisor;

		protected NpcDrinkerAbilityDef(string defname, string filename, int headerLine, int heal, int stoneskin)
			: base(defname, filename, headerLine) {
			this.healOneIn = this.InitTypedField("healOneIn", heal, typeof(int));
			this.stoneskinOneIn = this.InitTypedField("stoneskinOneIn", stoneskin, typeof(int));
			this.spellshieldOneIn = this.InitTypedField("spellshieldOneIn", 12, typeof(int));
			this.healDivisor = this.InitTypedField("healDivisor", 3, typeof(int));
		}

		public int HealOneIn {
			get { return (int) this.healOneIn.CurrentValue; }
			set { this.healOneIn.CurrentValue = value; }
		}

		public int StoneskinOneIn {
			get { return (int) this.stoneskinOneIn.CurrentValue; }
			set { this.stoneskinOneIn.CurrentValue = value; }
		}

		public int SpellshieldOneIn {
			get { return (int) this.spellshieldOneIn.CurrentValue; }
			set { this.spellshieldOneIn.CurrentValue = value; }
		}

		public int HealDivisor {
			get { return (int) this.healDivisor.CurrentValue; }
			set { this.healDivisor.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class HealDrinkerAbilityDef : NpcDrinkerAbilityDef {
		public HealDrinkerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, 30, 20) {
		}
	}

	/// <summary>e_potiondrinker also drinks after its own swing while above half its hits. Its Strength potion does nothing:
	/// Erebor's e_strboost isn't in the archive.</summary>
	[ViewableClass]
	public class PotionDrinkerAbilityDef : NpcDrinkerAbilityDef {
		private FieldValue boostOneIn;

		public PotionDrinkerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, 20, 12) {
			this.boostOneIn = this.InitTypedField("boostOneIn", 8, typeof(int));
		}

		public int BoostOneIn {
			get { return (int) this.boostOneIn.CurrentValue; }
			set { this.boostOneIn.CurrentValue = value; }
		}
	}

	public enum NpcPotion {
		Stoneskin, Spellshield, Strength, Speed, Intellect
	}

	public static partial class MonsterSpecialAttacks {
		private const int ResistFromPotion = 800, IntFromPotion = 1000, ManaBelowWhichPotionFills = 2000, SpeedFromPotion = 5000;
		private const double DrinkAgainAfter = 20;
		private static readonly TagKey firstBlowTK = TagKey.Acquire("_npcDrinkerFirstBlow_");
		internal static readonly TagKey drunkTK = TagKey.Acquire("_npcDrunk_");
		private static readonly TagKey speedBeforeTK = TagKey.Acquire("_npcSpeedBeforePotion_");

		internal static HealDrinkerAbilityDef HealDrinkerDef { get { return Lookup<HealDrinkerAbilityDef>("a_tevent_e_healdrinker"); } }
		internal static PotionDrinkerAbilityDef PotionDrinkerDef { get { return Lookup<PotionDrinkerAbilityDef>("a_tevent_e_potiondrinker"); } }

		private static bool OneIn(int chance) {
			return (chance > 0) && (Globals.dice.Next(chance) == 0);
		}

		/// <summary>tag.neknockuj: the very first blow only sets it.</summary>
		private static bool PastFirstBlow(Character self) {
			if (self.HasTag(firstBlowTK)) {
				return true;
			}
			self.SetTag(firstBlowTK, 1);
			return false;
		}

		internal static void TryDrinkOnGotHit(Character self) {
			NpcDrinkerAbilityDef def = HasAbilityDef(self, HealDrinkerDef) ? (NpcDrinkerAbilityDef) HealDrinkerDef : PotionDrinkerDef;
			if (!HasAbilityDef(self, def) || !PastFirstBlow(self) || self.HasTag(drunkTK) || (self.Hits >= (self.MaxHits / 2))) {
				return;
			}
			if (OneIn(def.HealOneIn)) {
				self.Hits = (short) System.Math.Min(self.MaxHits, self.Hits + (self.MaxHits / def.HealDivisor));
				self.Emote("drinking potion");
				self.Emote("regenerating!");
			} else if (OneIn(def.StoneskinOneIn)) {
				Drink(self, NpcPotion.Stoneskin);
			} else if (OneIn(def.SpellshieldOneIn)) {
				Drink(self, NpcPotion.Spellshield);
			}
		}

		internal static void TryDrinkAfterOwnSwing(Character self) {
			PotionDrinkerAbilityDef def = PotionDrinkerDef;
			if (!HasAbilityDef(self, def) || !PastFirstBlow(self) || self.HasTag(drunkTK) || (self.Hits >= self.MaxHits) || (self.Hits <= (self.MaxHits / 2))) {
				return;
			}
			if (OneIn(def.BoostOneIn)) {
				Drink(self, NpcPotion.Strength);
			} else if (OneIn(def.BoostOneIn)) {
				Drink(self, NpcPotion.Speed);
			} else if (OneIn(def.BoostOneIn) && (self.Mana > 0)) {
				Drink(self, NpcPotion.Intellect);
			}
		}

		internal static TimerKey PotionTK(NpcPotion potion) {
			return TimerKey.Acquire("_npcPotion" + potion + "_");
		}

		internal static void Drink(Character self, NpcPotion potion) {
			self.SetTag(drunkTK, 1);
			if (self.GetTimer(PotionTK(potion)) != null) {
				return;
			}
			self.Emote("drinking potion.");
			switch (potion) {
				case NpcPotion.Stoneskin:
					self.ResistPhysical += ResistFromPotion;
					self.Emote("s skin becomes harder.");
					break;
				case NpcPotion.Spellshield:
					self.ResistFire += ResistFromPotion;
					self.ResistElectric += ResistFromPotion;
					self.Emote("becomes resistent to spells!");
					break;
				case NpcPotion.Strength:
					self.Emote("becomes stronger.");
					break;
				case NpcPotion.Speed:
					if (self is NPC npc) {
						self.SetTag(speedBeforeTK, npc.SwingStamina);
						npc.SwingStamina = SpeedFromPotion;
						npc.InvalidateCombatWeaponValues();
					}
					self.Emote("becomes much faster.");
					break;
				case NpcPotion.Intellect:
					self.Int = (short) (self.Int + IntFromPotion);
					if (self.Mana < ManaBelowWhichPotionFills) {
						self.Mana = (short) (self.Mana + IntFromPotion);
					}
					self.Emote("becomes more clever.");
					break;
			}
			NpcPotionTimer timer = new NpcPotionTimer();
			timer.potion = potion;
			self.AddTimer(PotionTK(potion), timer).DueInSeconds = DrinkAgainAfter;
		}

		/// <summary>The rest of each potion's time after a new one can be drunk, its second @timer in macros.scp.</summary>
		internal static double PotionLingers(NpcPotion potion) {
			switch (potion) {
				case NpcPotion.Strength:
					return 20;
				case NpcPotion.Speed:
					return 15;
				case NpcPotion.Intellect:
					return 25;
				default:
					return 10;
			}
		}

		internal static void PotionWoreOff(Character self, NpcPotion potion) {
			switch (potion) {
				case NpcPotion.Stoneskin:
					self.ResistPhysical -= ResistFromPotion;
					break;
				case NpcPotion.Spellshield:
					self.ResistFire -= ResistFromPotion;
					self.ResistElectric -= ResistFromPotion;
					break;
				case NpcPotion.Speed:
					if ((self is NPC npc) && (self.GetTag(speedBeforeTK) is int before)) {
						npc.SwingStamina = before;
						npc.InvalidateCombatWeaponValues();
						self.RemoveTag(speedBeforeTK);
					}
					break;
				case NpcPotion.Intellect:
					self.Int = (short) (self.Int - IntFromPotion);
					break;
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class NpcPotionTimer : BoundTimer {
		[SaveableData, CopyableData]
		public NpcPotion potion;

		[SaveableData, CopyableData]
		public bool lingering;

		[LoadingInitializer, DeepCopyImplementation]
		public NpcPotionTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (this.lingering) {
				MonsterSpecialAttacks.PotionWoreOff(self, this.potion);
				return;
			}
			self.RemoveTag(MonsterSpecialAttacks.drunkTK);
			NpcPotionTimer rest = new NpcPotionTimer();
			rest.potion = this.potion;
			rest.lingering = true;
			self.AddTimer(MonsterSpecialAttacks.PotionTK(this.potion), rest).DueInSeconds = MonsterSpecialAttacks.PotionLingers(this.potion);
		}
	}
}
