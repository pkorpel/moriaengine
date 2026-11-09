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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_balrog (Balrogh, Zatraceny, Vidlak Ochrance). Its swings, dodged too: 1 in BurnOneIn its fire burns the
	/// target for BurnDamage, else 1 in WeakenOneIn it takes all their stamina. A blow at it: 1 in ReflectOneIn part of it comes
	/// back (ReflectDamage), and 1 in ReflectKillOneIn it kills an attacker down to ReflectKillHits. A spell landing on it: 1 in
	/// CurseOneIn its shadow curse on its target for CurseSeconds, one at a time: every CurseTick seconds "Dusis se!", CurseMana
	/// mana and CurseDamage hits; and no spell can be cast (CastDamage hits and CastMana mana for trying). Fixed from Erebor:
	/// the curse's mana drain read the curse item's mana, so it zeroed the victim's; here it takes CurseMana.</summary>
	[ViewableClass]
	public class BalrogAbilityDef : PassiveAbilityDef {
		private FieldValue burnOneIn, burnDamage, weakenOneIn, reflectOneIn, reflectDamage, reflectKillOneIn, reflectKillHits;
		private FieldValue curseOneIn, curseSeconds, curseTick, curseDamage, curseMana, castDamage, castMana;

		public BalrogAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.burnOneIn = this.InitTypedField("balrogBurnOneIn", 6, typeof(int));
			this.burnDamage = this.InitTypedField("balrogBurnDamage", new double[] { 10, 26 }, typeof(double[]));
			this.weakenOneIn = this.InitTypedField("balrogWeakenOneIn", 10, typeof(int));
			this.reflectOneIn = this.InitTypedField("balrogReflectOneIn", 8, typeof(int));
			this.reflectDamage = this.InitTypedField("balrogReflectDamage", new double[] { 10, 40 }, typeof(double[]));
			this.reflectKillOneIn = this.InitTypedField("balrogReflectKillOneIn", 6, typeof(int));
			this.reflectKillHits = this.InitTypedField("balrogReflectKillHits", 25, typeof(int));
			this.curseOneIn = this.InitTypedField("balrogCurseOneIn", 6, typeof(int));
			this.curseSeconds = this.InitTypedField("balrogCurseSeconds", 30.0, typeof(double));
			this.curseTick = this.InitTypedField("balrogCurseTick", 4.0, typeof(double));
			this.curseDamage = this.InitTypedField("balrogCurseDamage", new double[] { 10, 20 }, typeof(double[]));
			this.curseMana = this.InitTypedField("balrogCurseMana", 10, typeof(int));
			this.castDamage = this.InitTypedField("balrogCastDamage", 10, typeof(int));
			this.castMana = this.InitTypedField("balrogCastMana", 15, typeof(int));
		}

		public int BurnOneIn { get { return (int) this.burnOneIn.CurrentValue; } set { this.burnOneIn.CurrentValue = value; } }
		public double[] BurnDamage { get { return (double[]) this.burnDamage.CurrentValue; } set { this.burnDamage.CurrentValue = value; } }
		public int WeakenOneIn { get { return (int) this.weakenOneIn.CurrentValue; } set { this.weakenOneIn.CurrentValue = value; } }
		public int ReflectOneIn { get { return (int) this.reflectOneIn.CurrentValue; } set { this.reflectOneIn.CurrentValue = value; } }
		public double[] ReflectDamage { get { return (double[]) this.reflectDamage.CurrentValue; } set { this.reflectDamage.CurrentValue = value; } }
		public int ReflectKillOneIn { get { return (int) this.reflectKillOneIn.CurrentValue; } set { this.reflectKillOneIn.CurrentValue = value; } }
		public int ReflectKillHits { get { return (int) this.reflectKillHits.CurrentValue; } set { this.reflectKillHits.CurrentValue = value; } }
		public int CurseOneIn { get { return (int) this.curseOneIn.CurrentValue; } set { this.curseOneIn.CurrentValue = value; } }
		public double CurseSeconds { get { return (double) this.curseSeconds.CurrentValue; } set { this.curseSeconds.CurrentValue = value; } }
		public double CurseTick { get { return (double) this.curseTick.CurrentValue; } set { this.curseTick.CurrentValue = value; } }
		public double[] CurseDamage { get { return (double[]) this.curseDamage.CurrentValue; } set { this.curseDamage.CurrentValue = value; } }
		public int CurseMana { get { return (int) this.curseMana.CurrentValue; } set { this.curseMana.CurrentValue = value; } }
		public int CastDamage { get { return (int) this.castDamage.CurrentValue; } set { this.castDamage.CurrentValue = value; } }
		public int CastMana { get { return (int) this.castMana.CurrentValue; } set { this.castMana.CurrentValue = value; } }
	}

	/// <summary>#1022 e_rinald (the Grandmaster quest's Rinald, and the GM event boss). Only players hurt him: anyone else's
	/// damage is lost. Each player's blow or spell weakens him by their profession: resists by ResistStep (warriors' and
	/// rangers' and priests' armor by ArmorStep), a craftsman's, a thief's and a shaman's take hits off him, a craftsman's own
	/// weapon may kill him at once (1 in CraftKillOneIn), rangers and priests may stun him for a second, a mystic takes Dex
	/// and Str, a shaman may drain his mana. While he can't be hurt yet, every attempt raises his hits by InvulHitsStep, up
	/// to InvulHitsCap. Fixed from Erebor: hits taken off directly leave him at 1 at least, as the blow itself does the
	/// killing (Erebor's could leave him at 0 and alive).</summary>
	[ViewableClass]
	public class RinaldAbilityDef : PassiveAbilityDef {
		private FieldValue resistStep, armorStep, craftKillOneIn, invulHitsStep, invulHitsCap, breathDamage;

		public RinaldAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.resistStep = this.InitTypedField("rinaldResistStep", 25, typeof(int));
			this.armorStep = this.InitTypedField("rinaldArmorStep", 50, typeof(int));
			this.craftKillOneIn = this.InitTypedField("rinaldCraftKillOneIn", 20, typeof(int));
			this.invulHitsStep = this.InitTypedField("rinaldInvulHitsStep", 200, typeof(int));
			this.invulHitsCap = this.InitTypedField("rinaldInvulHitsCap", 5000, typeof(int));
			this.breathDamage = this.InitTypedField("rinaldBreathDamage", 35, typeof(int)); //#837 f_dragon_fire(35)
		}

		public int ResistStep { get { return (int) this.resistStep.CurrentValue; } set { this.resistStep.CurrentValue = value; } }
		public int ArmorStep { get { return (int) this.armorStep.CurrentValue; } set { this.armorStep.CurrentValue = value; } }
		public int CraftKillOneIn { get { return (int) this.craftKillOneIn.CurrentValue; } set { this.craftKillOneIn.CurrentValue = value; } }
		public int InvulHitsStep { get { return (int) this.invulHitsStep.CurrentValue; } set { this.invulHitsStep.CurrentValue = value; } }
		public int InvulHitsCap { get { return (int) this.invulHitsCap.CurrentValue; } set { this.invulHitsCap.CurrentValue = value; } }
		public int BreathDamage { get { return (int) this.breathDamage.CurrentValue; } set { this.breathDamage.CurrentValue = value; } }
	}

	/// <summary>#1022 e_odinvis_krik (the living helm): when hit, 1 in CryOneIn "crying.", and a second later every player
	/// hidden or invisible in its 64x64 part of the map is found ("Byl jsi objeven!").</summary>
	[ViewableClass]
	public class OdinvisKrikAbilityDef : PassiveAbilityDef {
		private FieldValue cryOneIn;

		public OdinvisKrikAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.cryOneIn = this.InitTypedField("odinvisKrikCryOneIn", 12, typeof(int));
		}

		public int CryOneIn { get { return (int) this.cryOneIn.CurrentValue; } set { this.cryOneIn.CurrentValue = value; } }
	}

	/// <summary>#1022 e_emotedmg (the test titan Testoid, or anyone a GM gives it): says each blow's damage and its damage
	/// type, as "-37: flags 0402". The damage is what the blow did; Erebor's was before armor. A GM gives or takes it in
	/// the character's .info abilities (Erebor's .emotedmg toggle isn't ported).</summary>
	[ViewableClass]
	public class EmoteDamageAbilityDef : PassiveAbilityDef {
		public EmoteDamageAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		internal static BalrogAbilityDef BalrogDef { get { return Lookup<BalrogAbilityDef>("a_tevent_e_balrog"); } }
		internal static RinaldAbilityDef RinaldDef { get { return Lookup<RinaldAbilityDef>("a_tevent_e_rinald"); } }
		internal static OdinvisKrikAbilityDef OdinvisKrikDef { get { return Lookup<OdinvisKrikAbilityDef>("a_tevent_e_odinvis_krik"); } }
		internal static EmoteDamageAbilityDef EmoteDamageDef { get { return Lookup<EmoteDamageAbilityDef>("a_tevent_e_emotedmg"); } }

		private const int GlowSpikeEffect = 0x37C4, FlamestrikeSound = 0x208, RinaldKillSound = 0x227;
		private static readonly TagKey balrogCursingTK = TagKey.Acquire("_balrogCursing_");
		private static readonly PluginKey rinaldStunPK = PluginKey.Acquire("_rinaldStun_");

		#region Balrog
		/// <summary>The Balrog's own swing, landed or dodged.</summary>
		internal static void TryBalrogSwing(Character attacker, Character target) {
			BalrogAbilityDef def = BalrogDef;
			if (!(attacker is NPC self) || !Reacts(self, def) || (target == null) || target.IsDeleted || target.Flag_Dead) {
				return;
			}
			bool burn = OneIn(def.BurnOneIn);
			if (!burn && !OneIn(def.WeakenOneIn)) {
				return;
			}
			target.SysMessage(burn ? "Popalil te Balroghuv ohen!" : "Balroghuv ohen te oslabil!");
			EffectFactory.StationaryEffect(target, FireColumnEffect, 6, 16);
			self.Sound(FlamestrikeSound);
			if (burn) {
				DamageManager.CauseDamage(null, target, DamageType.MagicFire, RollTunable(null, def.BurnDamage));
			} else {
				target.Stam = 0;
			}
		}

		/// <summary>A blow at the Balrog: part of it may come back.</summary>
		private static void TryBalrogReflect(NPC self, Character attacker) {
			BalrogAbilityDef def = BalrogDef;
			if (!Reacts(self, def) || (attacker == null) || attacker.IsDeleted || attacker.Flag_Dead) {
				return;
			}
			if ((attacker.Hits > def.ReflectKillHits) && OneIn(def.ReflectOneIn)) {
				attacker.SysMessage("Cast tveho uderu byla odrazena zpet!");
				DamageManager.CauseDamage(null, attacker, DamageType.Magic, RollTunable(null, def.ReflectDamage));
			}
			if (!attacker.IsDeleted && !attacker.Flag_Dead && (attacker.Hits <= def.ReflectKillHits) && OneIn(def.ReflectKillOneIn)) {
				attacker.SysMessage("Cast tveho uderu byla odrazena zpet!");
				attacker.CauseDeath(self);
			}
		}

		/// <summary>A spell landed on the Balrog: maybe its shadow curse, on its own target (as Erebor's: not the caster).</summary>
		internal static BalrogCurseTimer TryBalrogSpellCurse(NPC self) {
			BalrogAbilityDef def = BalrogDef;
			if (!Reacts(self, def) || self.HasTag(balrogCursingTK) || !OneIn(def.CurseOneIn)) {
				return null;
			}
			Character victim = NPCAiTimer.GetFor(self)?.Target;
			if ((victim == null) || victim.IsDeleted || victim.Flag_Dead) {
				return null;
			}
			return CurseWithShadow(self, victim, def);
		}

		internal static BalrogCurseTimer CurseWithShadow(NPC balrog, Character victim, BalrogAbilityDef def) {
			EffectFactory.StationaryEffect(victim, GlowSpikeEffect, 6, 120);
			balrog.SetTag(balrogCursingTK, 1);
			BalrogCurseTimer curse = new BalrogCurseTimer { balrog = balrog, endsAt = Globals.TimeInSeconds + def.CurseSeconds };
			victim.AddTimer(BalrogCurseTimer.Key, curse);
			curse.DueInSeconds = 2;
			curse.PeriodInSeconds = def.CurseTick;
			victim.SysMessage("Byl jsi zasazen stinovym kouzlem Balrogha!");
			return curse;
		}

		internal static void BalrogCurseTick(Character victim, BalrogCurseTimer curse) {
			BalrogAbilityDef def = BalrogDef;
			if ((def == null) || victim.Flag_Dead || (Globals.TimeInSeconds >= curse.endsAt)) {
				EndBalrogCurse(victim, curse);
				return;
			}
			victim.SysMessage("Dusis se!");
			victim.Mana = (short) Math.Max(0, victim.Mana - def.CurseMana);
			if (victim.Hits > 5) {
				DamageManager.CauseDamage(null, victim, DamageType.Magic, RollTunable(null, def.CurseDamage));
			} else {
				victim.CauseDeath(curse.balrog);
			}
		}

		internal static void EndBalrogCurse(Character victim, BalrogCurseTimer curse) {
			victim.DeleteTimer(BalrogCurseTimer.Key);
			curse.balrog?.RemoveTag(balrogCursingTK);
			if (!victim.IsDeleted) {
				victim.SysMessage("Stin Balrogha te opustil, opet muzes volne dychat.");
			}
		}

		/// <summary>e_prokleti_balroga's @spellcast: a cursed caster can't cast.</summary>
		internal static bool BalrogCurseRefusesCast(Character caster) {
			BalrogAbilityDef def = BalrogDef;
			if ((def == null) || !caster.HasTimer(BalrogCurseTimer.Key)) {
				return false;
			}
			caster.SysMessage("Stin Balroga te pohlcuje, nejsi " + (caster.IsFemale ? "schopna" : "schopen") + " se soustredit!");
			DamageManager.CauseDamage(null, caster, DamageType.Magic, def.CastDamage);
			caster.Mana = (short) Math.Max(0, caster.Mana - def.CastMana);
			return true;
		}
		#endregion

		#region Rinald
		/// <summary>DamageManager.CauseDamage, before anything else: true loses the damage.</summary>
		internal static bool TryRinald(Character attacker, Character defender) {
			RinaldAbilityDef def = RinaldDef;
			if (!(defender is NPC rinald) || !Reacts(rinald, def) || rinald.IsDeleted) {
				return false;
			}
			if (rinald.Flag_Invulnerable) {
				rinald.Say("Pche! Pokud neznas me jmeno, tak me neporazis!");
				if (rinald.MaxHits < def.InvulHitsCap) {
					rinald.MaxHits = (short) Math.Min(def.InvulHitsCap, rinald.MaxHits + def.InvulHitsStep);
					rinald.Hits = rinald.MaxHits;
				}
				return false;
			}
			if (!(attacker is Player player)) {
				return true;
			}
			return WeakenRinald(rinald, player, def);
		}

		/// <summary>e_rinald by profession; true when his own weapon killed him, the blow's damage is lost then.</summary>
		private static bool WeakenRinald(NPC rinald, Player player, RinaldAbilityDef def) {
			int step = def.ResistStep, armor = def.ArmorStep;
			bool she = player.IsFemale;
			if (Professions.HasProfession(player, Professions.Necromant)) {
				player.SysMessage("Citis jak tva temna magie oslabila Rinalda!");
				rinald.ResistSummon -= step;
				rinald.ResistFire -= step;
				rinald.ResistCold -= step;
			} else if (Professions.HasProfession(player, Professions.Mage)) {
				player.SysMessage("Citis jak tva magie oslabuje Rinalda!");
				rinald.ResistSummon -= step;
				rinald.ResistFire -= step;
				rinald.ResistElectric -= step;
			} else if (Professions.HasProfession(player, Professions.Craftsman)) {
				TakeHits(rinald, Globals.dice.Next(75, 101));
				foreach (Weapon weapon in new[] { player.FindLayer(LayerNames.Hand1) as Weapon, player.FindLayer(LayerNames.Hand2) as Weapon }) {
					if (weapon == null) {
						continue;
					}
					if ((CraftingSkillDef.GetCrafter(weapon) == player) && OneIn(def.CraftKillOneIn)) {
						rinald.Say("Tu zbran jsi si " + (she ? "vyrobila sama" : "vyrobil sam") + "?!!");
						rinald.Sound(RinaldKillSound);
						BombPotionKind.Blast(rinald);
						rinald.CauseDeath(player);
						return true;
					}
					if (Globals.dice.Next(3) < 2) {
						player.SysMessage((she ? "Zasadila" : "Zasadil") + " jsi Rinaldovi kriticky uder");
						TakeHits(rinald, Globals.dice.Next(50, 101));
					}
				}
			} else if (Professions.HasProfession(player, Professions.Warrior)) {
				player.SysMessage("Tvuj silny uder oslabil Rinaldovou zbroj");
				rinald.ResistSharp -= armor;
				rinald.ResistSlashing -= armor;
				rinald.ResistBlunt -= armor;
			} else if (Professions.HasProfession(player, Professions.Thief)) {
				player.SysMessage("Tvuj zasah pronikl Rinaldovou kuzi");
				rinald.ResistPoison -= step;
				rinald.ResistBleed -= step;
				TakeHits(rinald, 50);
			} else if (Professions.HasProfession(player, Professions.Ranger)) {
				player.SysMessage("Tvuj sip se zabodl do Rinaldova tela.");
				rinald.ResistStabbing -= armor;
				StunRinald(rinald, player);
			} else if (Professions.HasProfession(player, Professions.Mystic)) {
				player.SysMessage("Tvuj uder oslabil a zpomalil Rinalda.");
				if (rinald.Dex >= 5) {
					rinald.Dex -= (short) Globals.dice.Next(1, 6);
				}
				if (rinald.Str >= 10) {
					rinald.Str -= (short) Globals.dice.Next(1, 11);
				}
				rinald.ResistElectric -= step;
				rinald.ResistCold -= step;
			} else if (Professions.HasProfession(player, Professions.Shaman)) {
				player.SysMessage("Citis jak magicka ochrana Rinalda slabne!");
				TakeHits(rinald, Globals.dice.Next(25, 51));
				rinald.ResistFire -= armor;
				rinald.ResistElectric -= armor;
				rinald.ResistCold -= armor;
				if (Globals.dice.Next(3) < 2) {
					player.SysMessage((she ? "Vysala" : "Vysal") + " jsi z Rinalda manu.");
					player.Mana = (short) Math.Min(player.MaxMana, player.Mana + 15);
				}
			} else if (Professions.HasProfession(player, Professions.Priest)) {
				StunRinald(rinald, player);
				rinald.ResistBlunt -= armor;
				player.SysMessage("Tvuj silny uder oslabil Rinaldovou zbroj");
			}
			return false;
		}

		private static void TakeHits(NPC rinald, int hits) {
			rinald.Hits = (short) Math.Max(1, rinald.Hits - hits);
		}

		private static void StunRinald(NPC rinald, Player player) {
			if (Globals.dice.Next(3) == 0) {
				player.SysMessage((player.IsFemale ? "Omracila" : "Omracil") + " jsi Rinalda.");
				ParalyzeEffectPluginDef.Paralyze(rinald, rinaldStunPK, TimeSpan.FromSeconds(1));
			}
		}
		#endregion

		#region the helm's cry
		private static void TryOdinvisCry(NPC self) {
			OdinvisKrikAbilityDef def = OdinvisKrikDef;
			if (!Reacts(self, def) || PeacemakingSkillDef.IsAsleep(self) || !OneIn(def.CryOneIn)) {
				return;
			}
			self.Emote("crying.");
			OdinvisCryTimer cry = new OdinvisCryTimer { x = self.X, y = self.Y, m = self.M };
			self.AddTimer(OdinvisCryTimer.Key, cry);
			cry.DueInSeconds = 1;
		}

		/// <summary>zviditelni over the 64x64 block holding the spot; how many were found.</summary>
		internal static int RevealAround(Map map, int x, int y) {
			int found = 0;
			int left = x - (x % 64), top = y - (y % 64);
			foreach (Character ch in CharsAround(map, left + 32, top + 32, 46)) {
				if (!(ch is Player player) || player.IsGM || player.Flag_Dead || (player.X < left) || (player.X >= left + 64) || (player.Y < top) || (player.Y >= top + 64)) {
					continue;
				}
				if (player.Flag_Hidden || player.Flag_InvisByMagic) {
					player.Flag_Hidden = false;
					player.Flag_InvisByMagic = false;
					player.SysMessage(player.IsFemale ? "Byla jsi objevena!" : "Byl jsi objeven!");
					found++;
				}
			}
			return found;
		}
		#endregion

		/// <summary>TryReactToHit's part for these rules.</summary>
		internal static void TryDamageRuleReactions(NPC self, Character attacker) {
			TryBalrogReflect(self, attacker);
			if (!self.IsDeleted) {
				TryOdinvisCry(self);
			}
		}

		/// <summary>e_emotedmg, a landed blow on the carrier.</summary>
		internal static void TryEmoteDamage(Character defender, double damage, DamageType type) {
			if (!defender.IsDeleted && Reacts(defender, EmoteDamageDef)) {
				defender.Say("-" + (int) Math.Round(damage) + ": flags " + ((int) type).ToString("X4"));
			}
		}
	}

	/// <summary>i_prokleti_balroga on its victim, ticking until endsAt.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BalrogCurseTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_balrogCurse_");

		[SaveableData, CopyableData]
		public NPC balrog;

		[SaveableData, CopyableData]
		public double endsAt;

		[LoadingInitializer, DeepCopyImplementation]
		public BalrogCurseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Character victim) && !victim.IsDeleted) {
				MonsterSpecialAttacks.BalrogCurseTick(victim, this);
			}
		}
	}

	/// <summary>i_src_changer_odinvis: a second after the cry.</summary>
	[SaveableClass, DeepCopyableClass]
	public class OdinvisCryTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_odinvisCry_");

		[SaveableData, CopyableData]
		public int x, y;

		[SaveableData, CopyableData]
		public byte m;

		[LoadingInitializer, DeepCopyImplementation]
		public OdinvisCryTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Map map = Map.GetMap(this.m);
			if (map != null) {
				MonsterSpecialAttacks.RevealAround(map, this.x, this.y);
			}
		}
	}
}
