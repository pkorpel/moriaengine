using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the shaman's rituals, sacrifice, weakening, concentration and voodoo shield, from
	/// specabilities.scp, magie.scp and combat.scp. Pacts and the soul are in ShamanPacts.</summary>
	public static class ShamanAbilities {
		internal const string RubyDef = "i_gem_ruby", CitrineDef = "i_gem_citrine";
		private static readonly TagKey ritualTK = TagKey.Acquire("_voodooRitual_");
		private static readonly TagKey shieldPoolTK = TagKey.Acquire("_voodooShield_");
		internal static readonly TimerKey ShieldTK = TimerKey.Acquire("_voodooShieldTimer_");
		private const int RitualAmount = 30, SpikeEffect = 0x37c4, RitualSound = 0x1a0;

		internal static bool IsShaman(Character ch) {
			return Professions.HasProfession(ch, Professions.Shaman);
		}

		internal static Item FindInPack(Character ch, string defname) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if (def == null) {
				return null;
			}
			foreach (AbstractItem item in ch.Backpack) {
				if (item.Def == def) {
					return (Item) item;
				}
			}
			return null;
		}

		internal static bool ConsumeOne(Character ch, string defname) {
			Item item = FindInPack(ch, defname);
			if (item == null) {
				return false;
			}
			if (item.Amount > 1) {
				item.Amount--;
			} else {
				item.Delete();
			}
			return true;
		}

		private static void RitualEffect(Character self) {
			self.Sound(RitualSound);
			EffectFactory.StationaryEffect(self, SpikeEffect, 0, 20);
		}

		/// <summary>The stats ritual in effect: 1 blood to mind, 2 mind to blood, 0 none.</summary>
		internal static int Ritual(Character self) {
			return Convert.ToInt32(self.GetTag(ritualTK) ?? 0);
		}

		/// <summary>i_voodoo_stats: a player's max hits and stamina follow Vitality here, max mana Intelligence,
		/// so Erebor's maxhits, maxstam -30 and int, maxmana +30 are Vitality -30 and Intelligence +30.</summary>
		private static void ApplyRitual(Player self, int ritual, int times) {
			int sign = (ritual == 1) ? 1 : -1;
			self.Vit = (short) (self.Vit - (sign * RitualAmount * times));
			self.Int = (short) (self.Int + (sign * RitualAmount * times));
			self.Hits = (short) Math.Min(self.Hits, self.MaxHits);
			self.Stam = (short) Math.Min(self.Stam, self.MaxStam);
			self.Mana = (short) Math.Min(self.Mana, self.MaxMana);
		}

		[SteamFunction]
		public static void VoodooInt(Player self) {
			StatsRitual(self, 1, "Cerpas magickou silu ze sve vlastni krve!");
		}

		[SteamFunction]
		public static void VoodooVit(Player self) {
			StatsRitual(self, 2, "Magicka sila krve posiluje tvou odolnost!");
		}

		private static void StatsRitual(Player self, int ritual, string done) {
			if (self.Flag_Dead) {
				return;
			}
			if (!IsShaman(self)) {
				self.SysMessage("Tohle neni pro tebe");
				return;
			}
			if (!ConsumeOne(self, RubyDef)) {
				self.SysMessage("Nemas vsetko potrebne, abys tohle dokazal.");
				return;
			}
			RitualEffect(self);
			if (Globals.dice.Next(1, 11) > ClassAbilities.Points(self, "woodoo")) {
				self.SysMessage("Pokazil jsi ritual.");
				return;
			}
			int now = Ritual(self);
			if (now == ritual) {
				self.SysMessage("Tenhle ritual jsi uz uspesne provedl!");
				return;
			}
			ApplyRitual(self, ritual, (now == 0) ? 1 : 2);
			self.SetTag(ritualTK, ritual);
			self.SysMessage(done);
		}

		/// <summary>.voodoorovnovaha: no class needed and nothing to pay.</summary>
		[SteamFunction]
		public static void VoodooRovnovaha(Player self) {
			int ritual = Ritual(self);
			if (ritual == 0) {
				self.SysMessage("Nic se nestalo...");
				return;
			}
			ApplyRitual(self, (ritual == 1) ? 2 : 1, 1);
			self.RemoveTag(ritualTK);
			self.SysMessage("Sila krve je nyni v rovnovaze.");
		}

		/// <summary>e_voodoo_stats' @death.</summary>
		internal static void OnDeath(Character dead) {
			Player self = dead as Player;
			int ritual = (self == null) ? 0 : Ritual(self);
			if (ritual != 0) {
				ApplyRitual(self, (ritual == 1) ? 2 : 1, 1);
				self.RemoveTag(ritualTK);
				self.SysMessage("Sila krve te se smrti opousti.");
			}
		}

		/// <summary>[function voodooobet]: 40 hits for mana. Erebor's script let hits go below 0; here it kills.</summary>
		[SteamFunction]
		public static void VoodooObet(Player self) {
			int woodoo = ClassAbilities.Points(self, "woodoo");
			if (self.Flag_Dead) {
				return;
			}
			if (!IsShaman(self)) {
				self.SysMessage("Tohle neni pro tebe");
				return;
			}
			if (woodoo <= 0) {
				self.SysMessage("Nejsi dost dobry saman");
				return;
			}
			int w = (woodoo <= 9) ? woodoo : 10 + ClassAbilities.Points(self, "woodoo_obet");
			RitualEffect(self);
			if (self.Hits <= 40) {
				self.Hits = 0;
				self.CauseDeath(self);
				return;
			}
			self.Hits = (short) (self.Hits - 40);
			if (Globals.dice.Next(1, 11) <= w) {
				self.Mana = (short) Math.Min(self.MaxMana, self.Mana + 10 + (3 * w));
			} else {
				self.SysMessage("Au! Poranil jsi se a skazil ritual");
			}
		}

		/// <summary>[function oslabenivoodoo]: a ruby, then a monster's cold resist drops a while. Erebor gates
		/// it on positive voodoo and scales it by negative; kept.</summary>
		[SteamFunction]
		public static void OslabeniVoodoo(Player self) {
			if (!IsShaman(self)) {
				self.RedMessage("Tohle neni pro tebe!");
			} else if (ClassAbilities.Points(self, "woodoo") <= 0) {
				self.SysMessage("Tohle jeste nedokazes.");
			} else if (FindInPack(self, RubyDef) == null) {
				self.SysMessage("Nemas vsetko potrebne, abys tohle dokazal.");
			} else {
				self.Target(SingletonScript<Targ_Weaken>.Instance);
				self.SysMessage("Zamer obludu, kteru chces oslabit.");
			}
		}

		internal static void Weaken(Player self, NPC monster) {
			if (monster.ResistCold == 1000) {
				self.SysMessage("Obluda je odolna na tvoje voodoo");
				return;
			}
			if (monster.HasTimer(WeakenTimer.Key)) {
				self.SysMessage("Obluda uz je oslabena");
				return;
			}
			if (!ConsumeOne(self, RubyDef)) {
				self.SysMessage("Nemas vsetko potrebne, abys tohle dokazal.");
				return;
			}
			WeakenTimer timer = new WeakenTimer();
			timer.shaman = self;
			timer.points = ClassAbilities.Points(self, "woodoo_curse");
			monster.AddTimer(WeakenTimer.Key, timer);
			timer.DueInSeconds = 2;
		}

		/// <summary>Koncentrace, magie.scp: a player's spell on a monster, not a field, (15 + points)% of the time
		/// for 25 stamina is a third stronger.</summary>
		internal static int Concentrate(Character caster, Character target, SpellDef spell, int spellPower) {
			int points = ClassAbilities.Points(caster, "woodoo_curse");
			if ((points <= 0) || !caster.IsPlayer || target.IsPlayerForCombat || (spell.Id == 28) || (spell.Id == 39) || (spell.Id == 47)) {
				return spellPower;
			}
			if ((Globals.dice.Next(1, 101) > 15 + points) || (caster.Stam <= 24)) {
				return spellPower;
			}
			caster.Stam = (short) (caster.Stam - 25);
			caster.SysMessage((caster.IsFemale ? "Zkoncentrovala" : "Zkoncentroval") + " jsi silu a " + (caster.IsFemale ? "zasadila" : "zasadil") + " silnejsi ranu!");
			return (spellPower * 13) / 10;
		}

		/// <summary>[function voodooshield]: a citrine and 29 mana for 30 s of 5 a point against monsters' spells.
		/// At exactly 30 mana Erebor neither casts nor says why; kept.</summary>
		[SteamFunction]
		public static void VoodooShield(Player self) {
			int points = ClassAbilities.Points(self, "voodooShield");
			VoodooShieldTimer timer = self.GetTimer(ShieldTK) as VoodooShieldTimer;
			if (!IsShaman(self) || (points <= 0)) {
				self.SysMessage("Tohle nemuzes pouzivat.");
			} else if ((timer != null) && (self.GetTag(shieldPoolTK) == null)) {
				self.SysMessage("Tva magicka moc je jeste prilis oslabena.");
			} else if (timer != null) {
				self.SysMessage("Jeste stale mas stit kolem sebe vytvoren.");
			} else if (FindInPack(self, CitrineDef) == null) {
				self.SysMessage("Nemas potrebne prisady.");
			} else if (self.Mana < 30) {
				self.SysMessage("Nemas dost energie!");
			} else if (self.Mana > 30) {
				ConsumeOne(self, CitrineDef);
				self.SetTag(shieldPoolTK, points * 5);
				VoodooShieldTimer shield = new VoodooShieldTimer();
				self.AddTimer(ShieldTK, shield);
				shield.DueInSeconds = 30;
				self.Say("Beleg Del Tir");
				EffectFactory.StationaryEffect(self, 0x2ed0, 6, 20);
				self.Sound(0x212);
				self.SysMessage("Vztycil jsi voodoo stit!", ClassAbilities.ClassMessageHue);
				self.Mana = (short) (self.Mana - 29);
				if (StandcastHelper.StandcastDef.IsActive(self)) {
					StandcastHelper.ConsumeIfActive(self);
					self.SysMessage("Jsi zpatky v normalnim stavu.", ClassAbilities.ClassMessageHue);
				}
			}
		}

		[SteamFunction]
		public static void CancelVoodooShield(Player self) {
			VoodooShieldTimer timer = self.GetTimer(ShieldTK) as VoodooShieldTimer;
			if ((timer != null) && (self.GetTag(shieldPoolTK) != null)) {
				self.RemoveTag(shieldPoolTK);
				timer.StartRest();
				self.SysMessage("Zrusil jsi voodoo stit.", ClassAbilities.ClassMessageHue);
			}
		}

		internal static void EndShield(Character self) {
			self.RemoveTag(shieldPoolTK);
		}

		internal static bool HasShieldUp(Character self) {
			return self.GetTag(shieldPoolTK) != null;
		}

		/// <summary>f_dmgFinal_applyVoodooShield: a monster's spell eats the pool first.</summary>
		internal static double AbsorbSpell(Character attacker, Character defender, double damage) {
			object pool = defender.GetTag(shieldPoolTK);
			if ((pool == null) || (attacker == null) || attacker.IsPlayer) {
				return damage;
			}
			int left = Convert.ToInt32(pool) - (int) damage;
			EffectFactory.StationaryEffect(defender, 0x2eeb, 6, 20);
			if (left < 0) {
				defender.RemoveTag(shieldPoolTK);
				if (defender.GetTimer(ShieldTK) is VoodooShieldTimer timer) {
					timer.StartRest();
				}
				defender.SysMessage("Kouzlo castesne pohlceno.", ClassAbilities.ClassMessageHue);
				return -left;
			}
			defender.SetTag(shieldPoolTK, left);
			defender.SysMessage("Kouzlo pohlceno stitem", ClassAbilities.ClassMessageHue);
			return 0;
		}
	}

	public class Targ_Weaken : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (targetted is NPC npc) {
				ShamanAbilities.Weaken(self, npc);
			} else {
				self.RedMessage("Zameriavaj iba obludy");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.RedMessage("Zameriavaj iba obludy");
			return TargetResult.Done;
		}
	}

	/// <summary>i_voodoo_enticement: after 2 s, min(points, 10) * 10% to take half a percent a point off a positive
	/// cold resist for 20 + 2 * points s. The monster then goes for the shaman.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WeakenTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_voodooWeaken_");

		[SaveableData, CopyableData]
		public Character shaman;

		[SaveableData, CopyableData]
		public int points, oldResist;

		[SaveableData, CopyableData]
		public bool weaker;

		[DeepCopyImplementation, LoadingInitializer]
		public WeakenTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC monster = cont as NPC;
			if (monster == null) {
				return;
			}
			if (this.weaker) {
				monster.ResistCold = this.oldResist;
				return;
			}
			if (Globals.dice.Next(1, 101) > Math.Min(this.points, 10) * 10) {
				if (this.shaman != null) {
					this.shaman.SysMessage("Oslabeni se nepovedlo");
				}
				return;
			}
			this.oldResist = monster.ResistCold;
			if (this.oldResist > 0) {
				monster.ResistCold = this.oldResist - ((this.oldResist * 5 * this.points) / 1000);
			}
			this.weaker = true;
			if (this.shaman != null) {
				this.shaman.SysMessage("Oslabeni uspesne");
				NPCAiTimer.DefendAgainst(monster, this.shaman);
			}
			WeakenTimer again = new WeakenTimer();
			again.shaman = this.shaman;
			again.points = this.points;
			again.oldResist = this.oldResist;
			again.weaker = true;
			monster.AddTimer(Key, again);
			again.DueInSeconds = 20 + (2 * this.points);
		}
	}

	/// <summary>i_voodooshield: 30 s up, then 180 s before another.</summary>
	[SaveableClass, DeepCopyableClass]
	public class VoodooShieldTimer : BoundTimer {
		[SaveableData, CopyableData]
		public bool resting;

		[DeepCopyImplementation, LoadingInitializer]
		public VoodooShieldTimer() {
		}

		internal void StartRest() {
			this.resting = true;
			this.DueInSeconds = 180;
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = cont as Character;
			if (self == null) {
				return;
			}
			if (!this.resting) {
				if (ShamanAbilities.HasShieldUp(self)) {
					self.SysMessage("Stit se vycerpal.", ClassAbilities.ClassMessageHue);
				}
				ShamanAbilities.EndShield(self);
				VoodooShieldTimer rest = new VoodooShieldTimer();
				rest.resting = true;
				self.AddTimer(ShamanAbilities.ShieldTK, rest);
				rest.DueInSeconds = 180;
				return;
			}
			self.SysMessage("Nyni muzes pouzit voodoo stit znovu.", ClassAbilities.ClassMessageHue);
		}
	}
}
