using System;
using System.Collections.Generic;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>t_potion_bomba. On the ground it counts down from 3 and goes off; lifted it goes off at once;
	/// in a pack it burns whoever uses it.</summary>
	public sealed class BombPotionKind : PotionKind {
		internal const int DispelSpell = 41, Range = 5, ExplosionEffect = 0x36B0, ExplosionSound = 0x207;
		private readonly Func<int> strength;
		internal readonly bool dispels;

		public BombPotionKind(Func<int> strength, bool dispels) {
			this.strength = strength;
			this.dispels = dispels;
		}

		public override void Drink(Character drinker, Potion potion) {
		}

		public override void Use(Character user, Potion potion) {
			if (potion.TopObj() is Character) {
				user.AddTimer(BombInHandTimer.Key, new BombInHandTimer { strength = this.strength() }).DueInSeconds = 3;
				potion.Consume(1);
			} else if (PotionDrinking.CanDrinkNow(user)) {
				PotionDrinking.Wait(user, PotionSettings.instance.delay);
				Arm(potion);
			}
		}

		public override void On_PutOnGround(Character by, Potion potion) {
			if (by != null) {
				this.Use(by, potion);
			}
		}

		internal static void Arm(Potion potion) {
			if (!potion.HasTimer(BombFuseTimer.Key)) {
				BombFuseTimer fuse = new BombFuseTimer { count = 3 };
				potion.AddTimer(BombFuseTimer.Key, fuse);
				fuse.DueInSeconds = 1;
				fuse.PeriodInSeconds = 1;
			}
		}

		internal static void Blast(Thing where) {
			EffectFactory.StationaryEffect(where, ExplosionEffect, 6, 15);
			PacketSequences.SendSound(where, ExplosionSound, Globals.MaxUpdateRange);
		}

		internal void Explode(Potion potion) {
			Blast(potion);
			List<Character> chars = new List<Character>();
			foreach (AbstractCharacter around in potion.GetMap().GetCharsInRange(potion.X, potion.Y, Range)) {
				if (around is Character ch) {
					chars.Add(ch);
				}
			}
			List<Item> items = new List<Item>();
			foreach (AbstractItem around in potion.GetMap().GetItemsInRange(potion.X, potion.Y, Range)) {
				if ((around is Item item) && (item != potion)) {
					items.Add(item);
				}
			}
			foreach (Character ch in chars) {
				if (this.dispels) {
					this.DispelOn(ch);
				} else {
					this.Hurt(ch, potion);
				}
			}
			foreach (Item item in items) {
				if (this.dispels) {
					this.Dispel(item);
				} else {
					this.Destroy(item, potion);
				}
			}
			potion.Delete();
		}

		/// <summary>exploze: armor comes off, and further out it's divided by the distance. Nobody answers it,
		/// it isn't an attack: damage_noresponse.</summary>
		internal static int DamageAt(int strength, int armor, int distance) {
			int damage = strength - armor;
			if (damage <= 0) {
				return 0;
			}
			return (distance > 1) ? (damage / distance) : damage;
		}

		private void Hurt(Character ch, Potion potion) {
			int armor = ch.ArmorClassVsP;
			if (ch.IsPlayer && !ch.IsGM) {
				armor += DamageManager.GetPvPArmorChange(ch);
			}
			int damage = DamageAt(this.strength(), armor, Point2D.GetSimpleDistance(ch.X, ch.Y, potion.X, potion.Y));
			if (damage > 0) {
				DamageManager.CauseDamage(null, ch, DamageType.MagicFire, damage); //no attacker: nobody answers it
				Blast(ch);
			}
		}

		private void DispelOn(Character ch) {
			Blast(ch);
			SpellDef.GetById(DispelSpell).ApplyEffectChar(ch, ch, 1000, EffectFlag.FromPotion);
		}

		/// <summary>znicsit: webs, and a rock, the closer the likelier.</summary>
		private void Destroy(Item item, Potion potion) {
			if (VarGate.Matches(item, "t_web", true)) {
				Blast(item);
				item.Delete();
			} else if (VarGate.Matches(item, "i_rock_plain", false)) {
				int distance = Math.Max(Point2D.GetSimpleDistance(item.X, item.Y, potion.X, potion.Y), 1);
				if ((distance < Range) && ((this.strength() / distance) > Globals.dice.Next(151))) {
					Blast(item);
					item.Delete();
				}
			}
		}

		/// <summary>dispellitem: what spells left around.</summary>
		private void Dispel(Item item) {
			if (item is SpellEffectItem effect) {
				Blast(item);
				effect.Delete();
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class BombFuseTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_bomb_fuse_");

		[SaveableData, CopyableData]
		public int count;

		[LoadingInitializer, DeepCopyImplementation]
		public BombFuseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Potion potion = cont as Potion;
			if (potion == null) {
				this.Delete();
				return;
			}
			potion.Message(this.count.ToString(System.Globalization.CultureInfo.InvariantCulture));
			this.count--;
			if (this.count <= 0) {
				this.Delete();
				(PotionKinds.Find(potion) as BombPotionKind)?.Explode(potion);
			}
		}
	}

	/// <summary>i_mem_bomba: what a bomb used in a pack does, unresisted.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BombInHandTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_bomb_in_hand_");

		[SaveableData, CopyableData]
		public int strength;

		[LoadingInitializer, DeepCopyImplementation]
		public BombInHandTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character ch) {
				DamageManager.CauseDamage(null, ch, DamageType.Irresistable, this.strength);
				BombPotionKind.Blast(ch);
			}
		}
	}

	public static class BombPotions {
		internal static void AddTo(Action<string, PotionKind> add) {
			PotionSettings s = PotionSettings.instance;
			add("i_potion_ExplosionLess", new BombPotionKind(() => s.explosionLess, false));
			add("i_potion_Explosion", new BombPotionKind(() => s.explosion, false));
			add("i_potion_ExplosionGreat", new BombPotionKind(() => s.explosionGreat, false));
			add("i_potion_Lava", new BombPotionKind(() => s.lava, false));
			add("i_potion_ExplosionDispell", new BombPotionKind(() => 0, true));
		}
	}
}
