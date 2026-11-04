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

using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_explosionlava (Rascal of the OCS dungeon): when hit, Chance permille, at most once in Cooldown seconds and
	/// first only Cooldown seconds after the first blow, "throws lava bomb": a lit Lava Bomb at a random tile within Range, which
	/// can't be picked up, and goes off as the potion does. Fixed from Erebor: the throw no longer sets the attacker's potion
	/// delay, and the spent bomb doesn't lie about.</summary>
	[ViewableClass]
	public class LavaBombAbilityDef : PassiveAbilityDef {
		private FieldValue chance, range, cooldown;

		public LavaBombAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("lavaBombChance", new double[] { 50, 50 }, typeof(double[]));
			this.range = this.InitTypedField("lavaBombRange", new double[] { 5, 5 }, typeof(double[]));
			this.cooldown = this.InitTypedField("lavaBombCooldown", new double[] { 30, 30 }, typeof(double[]));
		}

		public new double[] Chance { get { return (double[]) this.chance.CurrentValue; } set { this.chance.CurrentValue = value; } }
		public double[] Range { get { return (double[]) this.range.CurrentValue; } set { this.range.CurrentValue = value; } }
		public new double[] Cooldown { get { return (double[]) this.cooldown.CurrentValue; } set { this.cooldown.CurrentValue = value; } }
	}

	/// <summary>#1022 e_bomba_boy (Duse Nakazeneho): after its first swing, "Umrete!" and an explosion; a second later every
	/// player within 4 tiles takes Damage, 5 less a tile beyond the first, and it is gone, with no body, loot or experience.</summary>
	[ViewableClass]
	public class BombaBoyAbilityDef : PassiveAbilityDef {
		private FieldValue damage;

		public BombaBoyAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.damage = this.InitTypedField("bombaBoyDamage", new double[] { 60, 60 }, typeof(double[]));
		}

		public double[] Damage { get { return (double[]) this.damage.CurrentValue; } set { this.damage.CurrentValue = value; } }
	}

	/// <summary>#1022 e_ztracene_mesto_paladin_boss (the Paladin of Ztracene mesto): dying, a ring of black fire columns for
	/// a while and "Zarici brana", a gate to GateTarget for GateSeconds.</summary>
	[ViewableClass]
	public class PaladinBossAbilityDef : PassiveAbilityDef {
		private FieldValue gateTarget, gateSeconds;

		public PaladinBossAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.gateTarget = this.InitTypedField("paladinBossGateTarget", "807,633,0,2", typeof(string));
			this.gateSeconds = this.InitTypedField("paladinBossGateSeconds", 300.0, typeof(double));
		}

		/// <summary>x,y,z,plane.</summary>
		public string GateTarget { get { return (string) this.gateTarget.CurrentValue; } set { this.gateTarget.CurrentValue = value; } }
		public double GateSeconds { get { return (double) this.gateSeconds.CurrentValue; } set { this.gateSeconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_strach_lord1 (Lord Strachu of Pevnost Strachu level 1): below a sixth of his hits, "Beleg Faroth Kel",
	/// "Pokud mate odvahu se mi postavit tak mne nasledujte!", he stands still in fire and opens "Zarici brana" to GateTarget
	/// for GateSeconds, and is gone VanishSeconds later, with no body, loot or experience. Fixed from Erebor: once (each blow
	/// in those seconds opened another gate), and the gate leads to plane 10, level 2's (Erebor's target had no plane).</summary>
	[ViewableClass]
	public class StrachLord1AbilityDef : PassiveAbilityDef {
		private FieldValue gateTarget, gateSeconds, vanishSeconds;

		public StrachLord1AbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.gateTarget = this.InitTypedField("strachLord1GateTarget", "5348,4063,1,10", typeof(string));
			this.gateSeconds = this.InitTypedField("strachLord1GateSeconds", 1000.0, typeof(double));
			this.vanishSeconds = this.InitTypedField("strachLord1VanishSeconds", 3.0, typeof(double));
		}

		public string GateTarget { get { return (string) this.gateTarget.CurrentValue; } set { this.gateTarget.CurrentValue = value; } }
		public double GateSeconds { get { return (double) this.gateSeconds.CurrentValue; } set { this.gateSeconds.CurrentValue = value; } }
		public double VanishSeconds { get { return (double) this.vanishSeconds.CurrentValue; } set { this.vanishSeconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_ocs_boss_resurrection_able (the OCS boss's helpers): a dead helper's body is remembered for RecordSeconds
	/// seconds, then it is gone with what's left in it.</summary>
	[ViewableClass]
	public class OcsHelperAbilityDef : PassiveAbilityDef {
		private FieldValue recordSeconds;

		public OcsHelperAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.recordSeconds = this.InitTypedField("ocsHelperRecordSeconds", 200.0, typeof(double));
		}

		public double RecordSeconds { get { return (double) this.recordSeconds.CurrentValue; } set { this.recordSeconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_ocs_boss_resser (Rascal): when hit, Chance permille "Pomocnici moji, povstante!", and each remembered
	/// helper body rises as a fresh helper worth no experience, kept within HomeDistance of the spot, gone after
	/// HelperLifetime seconds. Erebor kept the bodies in a container whose uid was set by hand in the world; here the server
	/// remembers them.</summary>
	[ViewableClass]
	public class OcsResserAbilityDef : PassiveAbilityDef {
		private FieldValue chance, helperLifetime, homeDistance;

		public OcsResserAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("ocsResserChance", 100.0, typeof(double));
			this.helperLifetime = this.InitTypedField("ocsResserHelperLifetime", 7200.0, typeof(double));
			this.homeDistance = this.InitTypedField("ocsResserHomeDistance", 3, typeof(int));
		}

		public new double Chance { get { return (double) this.chance.CurrentValue; } set { this.chance.CurrentValue = value; } }
		public double HelperLifetime { get { return (double) this.helperLifetime.CurrentValue; } set { this.helperLifetime.CurrentValue = value; } }
		public int HomeDistance { get { return (int) this.homeDistance.CurrentValue; } set { this.homeDistance.CurrentValue = value; } }
	}

	/// <summary>#1022 e_beridornecro (Arghelior of Beridor): his first three blows taken get a line each, then one of three
	/// cries every blow; below half his hits, "Konec legrace !", "Heru Val Foalooke !", "Necht plamen pohlti vase duse!", a
	/// ring of fire, and SpawnDef appears where he stood as he is gone VanishSeconds later (no body, loot or experience): he
	/// becomes the Beridor dragon. Erebor fired a spawn placed in the world by hand; here the dragon comes at his spot.</summary>
	[ViewableClass]
	public class BeridorNecroAbilityDef : PassiveAbilityDef {
		private FieldValue spawnDef, vanishSeconds;

		public BeridorNecroAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.spawnDef = this.InitTypedField("beridorNecroSpawnDef", "c_dragon_beridor", typeof(string));
			this.vanishSeconds = this.InitTypedField("beridorNecroVanishSeconds", 4.0, typeof(double));
		}

		public string SpawnDef { get { return (string) this.spawnDef.CurrentValue; } set { this.spawnDef.CurrentValue = value; } }
		public double VanishSeconds { get { return (double) this.vanishSeconds.CurrentValue; } set { this.vanishSeconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_necroslave (Beridor's necromancers and dragon, and players dying in Beridor's levels): a while after
	/// death (the monster's SelfresDelay, PlayerDelay for a player) the body rises as an undead by what the dead held - a bow
	/// an archer, a two-handed sword or axe an axeman, a shield or a blade a knight, else a mage, a dragon a bone dragon - of a level by
	/// the player's level (a monster's: 2 or 3), with the dead one's name and everything left in the body, worth no experience,
	/// gone after Decay seconds. A carved body doesn't rise ("Mas stesti, zes telo rozrezal!"). Fixed from Erebor: a death with
	/// no body leaves nothing behind, and the empty body goes.</summary>
	[ViewableClass]
	public class NecroSlaveAbilityDef : PassiveAbilityDef {
		private FieldValue playerDelay, decay;

		public NecroSlaveAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.playerDelay = this.InitTypedField("necroSlavePlayerDelay", 10.0, typeof(double));
			this.decay = this.InitTypedField("necroSlaveDecay", 3600.0, typeof(double));
		}

		public double PlayerDelay { get { return (double) this.playerDelay.CurrentValue; } set { this.playerDelay.CurrentValue = value; } }
		public double Decay { get { return (double) this.decay.CurrentValue; } set { this.decay.CurrentValue = value; } }
	}

	public static partial class MonsterSpecialAttacks {
		internal static LavaBombAbilityDef LavaBombDef { get { return Lookup<LavaBombAbilityDef>("a_tevent_e_explosionlava"); } }
		internal static BombaBoyAbilityDef BombaBoyDef { get { return Lookup<BombaBoyAbilityDef>("a_tevent_e_bomba_boy"); } }
		internal static PaladinBossAbilityDef PaladinBossDef { get { return Lookup<PaladinBossAbilityDef>("a_tevent_e_ztracene_mesto_paladin_boss"); } }
		internal static StrachLord1AbilityDef StrachLord1Def { get { return Lookup<StrachLord1AbilityDef>("a_tevent_e_strach_lord1"); } }
		internal static OcsHelperAbilityDef OcsHelperDef { get { return Lookup<OcsHelperAbilityDef>("a_tevent_e_ocs_boss_resurrection_able"); } }
		internal static OcsResserAbilityDef OcsResserDef { get { return Lookup<OcsResserAbilityDef>("a_tevent_e_ocs_boss_resser"); } }
		internal static BeridorNecroAbilityDef BeridorNecroDef { get { return Lookup<BeridorNecroAbilityDef>("a_tevent_e_beridornecro"); } }
		internal static NecroSlaveAbilityDef NecroSlaveDef { get { return Lookup<NecroSlaveAbilityDef>("a_tevent_e_necroslave"); } }

		internal static readonly TagKey lavaBombNextTK = TagKey.Acquire("_lavaBombNext_");
		private static readonly TagKey bombaBoyDoneTK = TagKey.Acquire("_bombaBoyDone_");
		private static readonly TagKey strachLord1DoneTK = TagKey.Acquire("_strachLord1Done_");
		private static readonly TagKey beridorCountTK = TagKey.Acquire("_beridorNecroCount_");
		private static readonly TagKey beridorDoneTK = TagKey.Acquire("_beridorNecroDone_");

		/// <summary>Gone after so many seconds: no death, no body, no loot.</summary>
		internal static void VanishIn(Thing thing, double seconds) {
			BossVanishTimer timer = new BossVanishTimer();
			thing.AddTimer(bossVanishTK, timer);
			timer.DueInSeconds = seconds;
		}

		/// <summary>A ring of fire columns on the 4 or 16 tiles round a spot, for show.</summary>
		private static void FireRing(Character at, int radius, int hue) {
			for (int dx = -radius; dx <= radius; dx++) {
				for (int dy = -radius; dy <= radius; dy++) {
					if ((radius == 2) ? (Math.Abs(dx) + Math.Abs(dy) == 2) && ((dx == 0) || (dy == 0)) : (Math.Max(Math.Abs(dx), Math.Abs(dy)) == radius)) {
						EffectFactory.StationaryEffectAt(new Point4D(at.X + dx, at.Y + dy, at.Z, at.M), FireColumnEffect, 10, 255, true, false, hue, RenderModes.Opaque);
					}
				}
			}
		}

		/// <summary>"Zarici brana": a variable gate to the target, gone after its seconds.</summary>
		internal static VarGate OpenGate(Character at, string target, int hue, double seconds) {
			if (!(ThingDef.GetByDefname("i_varGate") is ThingDef def)) {
				return null;
			}
			VarGate gate = (VarGate) def.Create(at.X, at.Y, at.Z, at.M);
			gate.Name = "Zarici brana";
			gate.Color = hue;
			gate.MoreP = Point4D.Parse(target);
			VanishIn(gate, seconds);
			return gate;
		}

		/// <summary>TryReactToHit: the PR 3 bosses when hit.</summary>
		internal static void TryDeathBossReactions(NPC self, Character attacker) {
			LavaBombAbilityDef lava = LavaBombDef;
			if (HasAbilityDef(self, lava)) {
				int cooldown = Tuned(self, "lavaBombCooldown", lava.Cooldown);
				if (!(self.GetTag(lavaBombNextTK) is double next)) {
					self.SetTag(lavaBombNextTK, Globals.TimeInSeconds + cooldown); //the first bomb comes a cooldown after the first blow
				} else if ((next <= Globals.TimeInSeconds) && RollChance(Tuned(self, "lavaBombChance", lava.Chance))) {
					self.SetTag(lavaBombNextTK, Globals.TimeInSeconds + cooldown);
					ThrowLavaBomb(self, Tuned(self, "lavaBombRange", lava.Range));
				}
			}
			StrachLord1AbilityDef lord1 = StrachLord1Def;
			if (HasAbilityDef(self, lord1) && !self.HasTag(strachLord1DoneTK) && (self.Hits < (self.MaxHits / 6))) {
				self.SetTag(strachLord1DoneTK, 1);
				self.Say("Beleg Faroth Kel");
				self.Say("Pokud mate odvahu se mi postavit tak mne nasledujte!");
				NPCAiTimer.Freeze(self, lord1.VanishSeconds + 1);
				FireRing(self, 2, 0);
				OpenGate(self, lord1.GateTarget, 0x98E, lord1.GateSeconds);
				VanishIn(self, lord1.VanishSeconds);
			}
			OcsResserAbilityDef resser = OcsResserDef;
			if (HasAbilityDef(self, resser) && RollChance(resser.Chance)) {
				RaiseOcsHelpers(self, resser);
			}
			BeridorNecroAbilityDef beridor = BeridorNecroDef;
			if (HasAbilityDef(self, beridor)) {
				int count = ((self.GetTag(beridorCountTK) is int c) ? c : 0) + 1;
				self.SetTag(beridorCountTK, count);
				if (count == 1) {
					self.Say("Myslite, ze mate vyhrano ?");
				} else if (count == 2) {
					self.Say("Doufate, ze mne zastavite ?");
				} else if (count == 3) {
					self.Say("Teprve poznate silu temne magie !");
				} else {
					string[] cries = { "Aaaargh !", "Zhynte !", "Neeee!" };
					self.Say(cries[Globals.dice.Next(cries.Length)]);
				}
				if (!self.HasTag(beridorDoneTK) && (self.Hits < (self.MaxHits / 2))) {
					BeridorTransform(self, beridor);
				}
			}
		}

		/// <summary>Arghelior's end: his words, a ring of fire, the dragon, and he is gone.</summary>
		internal static NPC BeridorTransform(NPC self, BeridorNecroAbilityDef def) {
			self.SetTag(beridorDoneTK, 1);
			self.Say("Konec legrace !");
			self.Say("Heru Val Foalooke !");
			self.Say("Necht plamen pohlti vase duse!");
			NPCAiTimer.Freeze(self, def.VanishSeconds + 1);
			FireRing(self, 2, 0);
			VanishIn(self, def.VanishSeconds);
			if (ThingDef.GetByDefname(def.SpawnDef) is NPCDef dragonDef) {
				return (NPC) dragonDef.Create(self.X, self.Y, self.Z, self.M);
			}
			return null;
		}

		/// <summary>e_explosionlava's throw: a lit Lava Bomb within the range, fixed to the ground.</summary>
		internal static Potion ThrowLavaBomb(NPC self, int range) {
			if (!(ThingDef.GetByDefname("i_potion_Lava") is ThingDef def)) {
				return null;
			}
			self.Emote("throws lava bomb");
			Potion bomb = (Potion) def.Create(self.X + Globals.dice.Next(-range, range + 1), self.Y + Globals.dice.Next(-range, range + 1), self.Z, self.M);
			bomb.SetAttribute(ItemAttributes.attr_move_never, true);
			BombPotionKind.Arm(bomb);
			return bomb;
		}

		/// <summary>TryReactToOwnSwing: the kamikaze ghost.</summary>
		internal static void TryDeathBossOwnSwing(Character attacker) {
			BombaBoyAbilityDef def = BombaBoyDef;
			if (!(attacker is NPC self) || !HasAbilityDef(self, def) || self.HasTag(bombaBoyDoneTK)) {
				return;
			}
			self.SetTag(bombaBoyDoneTK, 1);
			self.Say("Umrete!");
			BombPotionKind.Blast(self);
			BombaBoyTimer timer = new BombaBoyTimer { damage = Tuned(self, "bombaBoyDamage", def.Damage) };
			self.AddTimer(BombaBoyTimer.Key, timer);
			timer.DueInSeconds = 1;
		}

		/// <summary>bomba_boy's blast, then the ghost is gone.</summary>
		internal static void BombaBoyBlast(NPC self, int damage) {
			foreach (Character near in CharsAround(self.GetMap(), self.X, self.Y, 4)) {
				if (!IsDragonFireVictim(near)) {
					continue;
				}
				int distance = Point2D.GetSimpleDistance(self, near);
				DamageManager.CauseDamage(null, near, DamageType.MagicFire, (distance > 1) ? (damage - (distance * 5)) : damage);
				BombPotionKind.Blast(near);
			}
			self.Delete();
		}

		/// <summary>OnMonsterDeath: the paladin's gate.</summary>
		internal static void OnDeathBossDeath(NPC self) {
			PaladinBossAbilityDef paladin = PaladinBossDef;
			if (HasAbilityDef(self, paladin)) {
				FireRing(self, 2, 1);
				OpenGate(self, paladin.GateTarget, 0x987, paladin.GateSeconds);
			}
		}

		/// <summary>e_necroslave's undeadhandler, called before the body takes the gear: null when this death doesn't rise.</summary>
		internal static string UndeadFor(Character dying) {
			if (dying is Player player) {
				if (!RegionEventSystem.IsIn(player, "e_beridor")) {
					return null;
				}
			} else if (!HasAbilityDef(dying, NecroSlaveDef) || SummonSpellDef.IsSummonedCreature(dying)) {
				return null;
			}
			if ((dying.Def as NPCDef)?.DragonBrain == true) {
				return "c_dragon_bone";
			}
			string level;
			if (dying is Player p) {
				int lvl = p.Level;
				level = (lvl <= 11) ? "" : (lvl <= 30) ? "_lvl_2" : (lvl <= 50) ? ((Globals.dice.Next(3) == 0) ? "_lvl_2" : "_lvl_3") : ((Globals.dice.Next(3) == 0) ? "_lvl_4" : "_lvl_3");
			} else {
				level = (Globals.dice.Next(2) == 0) ? "_lvl_2" : "_lvl_3";
			}
			//Sphere's second hand holds the two-handed weapons; here they're on the first, so it goes by the weapon's kind
			Weapon weapon = (dying.FindLayer(LayerNames.Hand1) as Weapon) ?? (dying.FindLayer(LayerNames.Hand2) as Weapon);
			Item second = dying.FindLayer(LayerNames.Hand2) as Item;
			WeaponType held = (weapon == null) ? WeaponType.BareHands : weapon.WeaponType;
			string kind;
			if ((held == WeaponType.Bow) || (held == WeaponType.XBow)) {
				kind = "c_skeleton_archer";
			} else if ((held == WeaponType.TwoHandSword) || (held == WeaponType.TwoHandAxe)) {
				kind = "c_skeleton_axe";
			} else if ((second != null) && (VarGate.TypeOf(second) == "t_shield")) {
				kind = "c_skeleton_knight";
			} else if (((held == WeaponType.OneHandSword) || (held == WeaponType.OneHandAxe) || (held == WeaponType.OneHandSpike)) &&
					(weapon.Def != ThingDef.GetByDefname("i_dagger")) && (weapon.Def != ThingDef.GetByDefname("i_knife_skinning"))) {
				kind = "c_skeleton_knight";
			} else {
				kind = "c_skeleton_mage";
			}
			string defname = kind + level;
			return (ThingDef.GetByDefname(defname) is NPCDef) ? defname : kind;
		}

		/// <summary>Character.CauseDeath, once the body exists: a helper's body remembered, a necroslave's rise armed.</summary>
		internal static void OnBodyLaid(Character dying, Corpse corpse, Character killedBy, string undead) {
			OcsHelperAbilityDef helper = OcsHelperDef;
			if (HasAbilityDef(dying, helper)) {
				OcsHelperRecord record = new OcsHelperRecord { def = (NPCDef) dying.Def, x = dying.X, y = dying.Y, z = dying.Z, m = dying.M };
				corpse.AddTimer(OcsHelperRecord.Key, record);
				record.DueInSeconds = helper.RecordSeconds;
				OcsHelperRecord.All.Add(corpse);
			}
			if (undead != null) {
				NecroSlaveAbilityDef def = NecroSlaveDef;
				double delay = (dying is Player) ? ((def == null) ? 10 : def.PlayerDelay) : RollTunable(((NPCDef) dying.Def).SelfresDelay, (SelfReserDef == null) ? new double[] { 18, 18 } : SelfReserDef.Delay);
				NecroRiseTimer rise = new NecroRiseTimer {
					undead = undead, name = dying.Name, killer = (dying is Player) ? dying : killedBy, x = dying.X, y = dying.Y, z = dying.Z, m = dying.M,
					decay = (def == null) ? 3600 : def.Decay
				};
				corpse.AddTimer(NecroRiseTimer.Key, rise);
				rise.DueInSeconds = delay;
			}
		}

		/// <summary>f_ocs_create_monsters over every remembered helper body.</summary>
		internal static int RaiseOcsHelpers(NPC boss, OcsResserAbilityDef def) {
			boss.Say("Pomocnici moji, povstante!");
			int raised = 0;
			foreach (Corpse corpse in new List<Corpse>(OcsHelperRecord.All)) {
				OcsHelperRecord.All.Remove(corpse);
				if (corpse.IsDeleted || !(corpse.GetTimer(OcsHelperRecord.Key) is OcsHelperRecord record) || (record.def == null)) {
					continue;
				}
				NPC helper;
				MonsterLoot.CreatingResurrected = true;
				try {
					helper = (NPC) record.def.Create(record.x, record.y, record.z, record.m);
				} finally {
					MonsterLoot.CreatingResurrected = false;
				}
				helper.ExperienceReward = 0;
				NPCAiTimer.EnsureInstalled(helper);
				NPCAiTimer ai = NPCAiTimer.GetFor(helper);
				if (ai != null) {
					ai.hasHome = true;
					ai.homeX = record.x;
					ai.homeY = record.y;
					ai.homeZ = (sbyte) record.z;
					ai.homeM = record.m;
				}
				VanishIn(helper, def.HelperLifetime);
				corpse.Delete();
				raised++;
			}
			return raised;
		}
	}

	/// <summary>bomba_boy's 1 s fuse.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BombaBoyTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_bombaBoy_");

		[SaveableData, CopyableData]
		public int damage;

		[LoadingInitializer, DeepCopyImplementation]
		public BombaBoyTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is NPC self) && !self.IsDeleted) {
				MonsterSpecialAttacks.BombaBoyBlast(self, this.damage);
			}
		}
	}

	/// <summary>i_ocs_monster_zaznam + i_ocs_monster_remover: on a dead OCS helper's body; when it runs out, the body goes.</summary>
	[SaveableClass, DeepCopyableClass]
	public class OcsHelperRecord : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_ocsHelperRecord_");
		internal static readonly List<Corpse> All = new List<Corpse>();

		[SaveableData, CopyableData]
		public NPCDef def;

		[SaveableData, CopyableData]
		public int x, y, z;

		[SaveableData, CopyableData]
		public byte m;

		[LoadingInitializer, DeepCopyImplementation]
		public OcsHelperRecord() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Corpse corpse) {
				All.Remove(corpse);
				if (!corpse.IsDeleted) {
					corpse.Delete();
				}
			}
		}
	}

	/// <summary>i_selfress_timer as e_necroslave sets it: the undead, the name, the body's things; the body goes.</summary>
	[SaveableClass, DeepCopyableClass]
	public class NecroRiseTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_necroRise_");

		[SaveableData, CopyableData]
		public string undead, name;

		[SaveableData, CopyableData]
		public Character killer;

		[SaveableData, CopyableData]
		public int x, y, z;

		[SaveableData, CopyableData]
		public byte m;

		[SaveableData, CopyableData]
		public double decay;

		[LoadingInitializer, DeepCopyImplementation]
		public NecroRiseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Corpse corpse) || corpse.IsDeleted || !(ThingDef.GetByDefname(this.undead) is NPCDef def)) {
				return;
			}
			if (corpse.HasTag(CorpseCarving.CarvedTK)) {
				if ((this.killer is Player cutter) && !cutter.IsDeleted) {
					cutter.SysMessage("Mas stesti, zes telo " + (cutter.IsFemale ? "rozrezala" : "rozrezal") + "!");
				}
				return;
			}
			NPC risen;
			MonsterLoot.CreatingResurrected = true;
			try {
				risen = (NPC) def.Create(this.x, this.y, this.z, this.m);
			} finally {
				MonsterLoot.CreatingResurrected = false;
			}
			risen.Name = this.name;
			risen.ExperienceReward = 0;
			foreach (AbstractItem item in new List<AbstractItem>(risen.Backpack)) {
				item.Delete();
			}
			foreach (AbstractItem item in new List<AbstractItem>(corpse)) {
				item.Cont = risen.Backpack;
			}
			MonsterSpecialAttacks.VanishIn(risen, this.decay);
			EffectFactory.StationaryEffect(risen, MonsterSpecialAttacks.FxId("i_fx_bless_effect", 0x373A), 6, 15);
			risen.Sound(0x20A); //snd_spell_polymorph
			if ((this.killer is Player player) && !player.IsDeleted) {
				player.SysMessage("Monstrum vstalo z mrtvych!");
			}
			corpse.Delete();
		}
	}
}
