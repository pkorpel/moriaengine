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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_hcn_teletotarget (the HCN tombs' rat and wraiths): while it fights a player, every second it dashes
	/// at its target (the species' Steps, else up to 3 tiles short of it), within 20 tiles and in sight. Every RetargetTicks
	/// seconds it goes for the player it hates most among those it remembers within MemoryRange: casters first, the nearer
	/// the more (a profession's base, less 10 a tile). It forgets them every ForgetTicks seconds and looks again. Fixed from
	/// Erebor: rangers count (Erebor's table left them out, so a ranger was never picked), GMs are left out (one as a target
	/// broke Erebor's memory), and a player is remembered once.</summary>
	[ViewableClass]
	public class HcnPursuitAbilityDef : PassiveAbilityDef {
		private FieldValue steps, memoryRange, retargetTicks, forgetTicks;

		public HcnPursuitAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.steps = this.InitTypedField("hcnPursuitSteps", new double[] { 0, 0 }, typeof(double[]));
			this.memoryRange = this.InitTypedField("hcnPursuitMemoryRange", 30, typeof(int));
			this.retargetTicks = this.InitTypedField("hcnPursuitRetargetTicks", 5, typeof(int));
			this.forgetTicks = this.InitTypedField("hcnPursuitForgetTicks", 30, typeof(int));
		}

		/// <summary>0 = up to 3 tiles short of the target.</summary>
		public double[] Steps { get { return (double[]) this.steps.CurrentValue; } set { this.steps.CurrentValue = value; } }
		public int MemoryRange { get { return (int) this.memoryRange.CurrentValue; } set { this.memoryRange.CurrentValue = value; } }
		public int RetargetTicks { get { return (int) this.retargetTicks.CurrentValue; } set { this.retargetTicks.CurrentValue = value; } }
		public int ForgetTicks { get { return (int) this.forgetTicks.CurrentValue; } set { this.forgetTicks.CurrentValue = value; } }
	}

	/// <summary>#1022 e_hcn_npcpoison (the HCN snake): each blow that does damage poisons with Strength percent of the
	/// victim's hits every Seconds seconds (Erebor's poison_c). The snake's own tag.jed_sila(14) is the default; the
	/// converter doesn't read that call form, and it's the only carrier.</summary>
	[ViewableClass]
	public class HcnPoisonAbilityDef : PassiveAbilityDef {
		private FieldValue strength, seconds;

		public HcnPoisonAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.strength = this.InitTypedField("hcnPoisonStrength", new double[] { 14, 14 }, typeof(double[]));
			this.seconds = this.InitTypedField("hcnPoisonSeconds", 2.0, typeof(double));
		}

		public double[] Strength { get { return (double[]) this.strength.CurrentValue; } set { this.strength.CurrentValue = value; } }
		public double Seconds { get { return (double) this.seconds.CurrentValue; } set { this.seconds.CurrentValue = value; } }
	}

	/// <summary>#1022 e_hcn_madnessability (the HCN mummies): each swing, dodged too, Chance permille the player it swings at
	/// goes mad for a moment (Strength permille, 4 more a level) and turns their profession on the players and pets around
	/// them: a warrior hits up to 8 within 2, a thief one with a bleed, a mage burns 5 within 8, a ranger with a bow knocks
	/// one within 8 back, a necromancer poisons 5 within 4, a priest curses 5 within 4, a shaman puts a voodoo on one within
	/// 8, a mystic strikes and paralyses 5 within 4, anyone else hits one within 2. Their own blow, burn or poison, scaled by
	/// the strength. Departures: the mage's and necromancer's power is their mind power (Erebor's tag.sm), the mystic's
	/// strike is their mind-power blow (Erebor's spell 103 isn't ported), all of it at once (Erebor's 0.1 s later).</summary>
	[ViewableClass]
	public class HcnMadnessAbilityDef : PassiveAbilityDef {
		private FieldValue chance, strength;

		public HcnMadnessAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("hcnMadnessChance", new double[] { 100, 100 }, typeof(double[]));
			this.strength = this.InitTypedField("hcnMadnessStrength", new double[] { 1000, 1000 }, typeof(double[]));
		}

		public new double[] Chance { get { return (double[]) this.chance.CurrentValue; } set { this.chance.CurrentValue = value; } }
		public double[] Strength { get { return (double[]) this.strength.CurrentValue; } set { this.strength.CurrentValue = value; } }
	}

	public static partial class MonsterSpecialAttacks {
		internal static HcnPursuitAbilityDef HcnPursuitDef { get { return Lookup<HcnPursuitAbilityDef>("a_tevent_e_hcn_teletotarget"); } }
		internal static HcnPoisonAbilityDef HcnPoisonDef { get { return Lookup<HcnPoisonAbilityDef>("a_tevent_e_hcn_npcpoison"); } }
		internal static HcnMadnessAbilityDef HcnMadnessDef { get { return Lookup<HcnMadnessAbilityDef>("a_tevent_e_hcn_madnessability"); } }

		private static readonly PluginKey madnessParalyzePK = PluginKey.Acquire("_hcnMadness_");

		#region e_hcn_teletotarget
		/// <summary>NPCAiTimer.Fight: starts the pursuit on a player target.</summary>
		internal static void TryHcnPursuit(NPC self, Character target) {
			HcnPursuitAbilityDef def = HcnPursuitDef;
			if (!HasAbilityDef(self, def) || !(target is Player) || self.HasTimer(HcnPursuitTimer.Key) || self.Flag_Hidden || self.Flag_Invulnerable || self.Flag_InvisByMagic) {
				return;
			}
			HcnPursuitTimer pursuit = new HcnPursuitTimer();
			RememberPlayers(self, pursuit, def);
			self.AddTimer(HcnPursuitTimer.Key, pursuit);
			pursuit.DueInSeconds = 1;
			pursuit.PeriodInSeconds = 1;
		}

		private static void RememberPlayers(NPC self, HcnPursuitTimer pursuit, HcnPursuitAbilityDef def) {
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, def.MemoryRange)) {
				if ((ch is Player player) && !player.IsGM && player.IsAliveAndValid && !pursuit.remembered.Contains(player)) {
					pursuit.remembered.Add(player);
				}
			}
		}

		/// <summary>TryReactToHit: a player it doesn't remember brings everyone around back to mind.</summary>
		private static void TryHcnRemember(NPC self, Character attacker) {
			HcnPursuitAbilityDef def = HcnPursuitDef;
			if ((attacker is Player player) && HasAbilityDef(self, def) && (self.GetTimer(HcnPursuitTimer.Key) is HcnPursuitTimer pursuit) && !pursuit.remembered.Contains(player)) {
				RememberPlayers(self, pursuit, def);
			}
		}

		/// <summary>f_hcn_TTT_Agro: a profession's hate, 10 less a tile, none out of sight.</summary>
		internal static int HcnAggro(NPC self, Player player) {
			if (!player.IsAliveAndValid || player.IsGM || (player.M != self.M) || !self.CanSeeLOS(player).Allow) {
				return 0;
			}
			ProfessionDef prof = player.Profession;
			int baseAggro;
			if ((prof == Professions.Mage) || (prof == Professions.Necromant)) {
				baseAggro = 1000;
			} else if ((prof == Professions.Priest) || (prof == Professions.Shaman)) {
				baseAggro = 950;
			} else if (prof == Professions.Mystic) {
				baseAggro = 930;
			} else {
				baseAggro = 900; //thief, warrior, craftsman; and the ranger, whom Erebor's table left out
			}
			return Math.Max(0, baseAggro - (10 * Point2D.GetSimpleDistance(self, player)));
		}

		internal static void HcnPursuitTick(NPC self, HcnPursuitTimer pursuit) {
			HcnPursuitAbilityDef def = HcnPursuitDef;
			NPCAiTimer ai = NPCAiTimer.GetFor(self);
			Character victim = ai?.Target;
			if ((def == null) || (victim == null) || victim.IsDeleted) {
				self.DeleteTimer(HcnPursuitTimer.Key);
				return;
			}
			pursuit.counter++;
			if ((pursuit.counter % def.ForgetTicks) == 0) {
				pursuit.remembered.Clear();
				RememberPlayers(self, pursuit, def);
			}
			if ((pursuit.counter % def.RetargetTicks) == 1) {
				Player best = null;
				int bestAggro = 0;
				foreach (Player player in pursuit.remembered) {
					int aggro = HcnAggro(self, player);
					if (aggro > bestAggro) {
						best = player;
						bestAggro = aggro;
					}
				}
				if ((best != null) && (best != victim)) {
					NPCAiTimer.Attack(self, best);
					ai.RunTowards(self, best, Math.Min(Point2D.GetSimpleDistance(self, best), 3));
					victim = best;
				}
			}
			if (!ai.isHeld && victim.IsAliveAndValid && (victim is Player)) {
				HcnDash(self, ai, victim, def);
			}
		}

		/// <summary>f_npc_portTimer: a run at the victim.</summary>
		internal static int HcnDash(NPC self, NPCAiTimer ai, Character victim, HcnPursuitAbilityDef def) {
			int distance = Point2D.GetSimpleDistance(self, victim);
			if ((victim.M != self.M) || (distance > 20) || (distance <= 1) || !self.CanSeeLOS(victim).Allow) {
				return 0;
			}
			int steps = Tuned(self, "npcPortDistance", def.Steps);
			if (steps <= 0) {
				steps = Math.Min(distance - 1, 3);
			}
			ai.RunTowards(self, victim, steps);
			return steps;
		}
		#endregion

		/// <summary>The snake's landed blow.</summary>
		private static void TryHcnPoison(Character attacker, Character defender, int damage) {
			HcnPoisonAbilityDef def = HcnPoisonDef;
			if ((damage > 0) && (attacker is NPC self) && HasAbilityDef(self, def) && !defender.IsDeleted && !defender.Flag_Dead) {
				Poison.Apply(defender, self, (int) RollTunable(null, def.Strength), def.Seconds, PoisonKind.PercentOfHits);
			}
		}

		#region e_hcn_madnessability
		/// <summary>The mummy's swing, landed or dodged.</summary>
		internal static void TryHcnMadness(Character attacker, Character target) {
			HcnMadnessAbilityDef def = HcnMadnessDef;
			if (!(attacker is NPC self) || !HasAbilityDef(self, def) || !(target is Player player) || player.IsGM || !player.IsAliveAndValid ||
					!RollChance(Tuned(self, "hcnMadnessChance", def.Chance))) {
				return;
			}
			Madden(player, Tuned(self, "hcnMadnessStrength", def.Strength) + (4 * player.Level));
		}

		/// <summary>f_hcn_madness_&lt;class&gt;: how many it turns on, within what, how; the victims hit.</summary>
		internal static int Madden(Player player, int strength) {
			ProfessionDef prof = player.Profession;
			bool armed = (player.FindLayer(LayerNames.Hand1) != null) || (player.FindLayer(LayerNames.Hand2) != null);
			double factor = strength / 1000.0;
			int count, range;
			Action<Character> effect;
			string message;
			bool costsStamina = true;
			if (prof == Professions.Warrior) {
				if (!armed) {
					return 0;
				}
				count = 8; range = 2;
				message = "V nahlem pomateni bijes bezhlave kolem sebe!";
				effect = victim => MadBlow(player, victim, factor);
			} else if (prof == Professions.Thief) {
				if (!armed) {
					return 0;
				}
				count = 1; range = 2;
				message = "V nahlem pomateni bijes bezhlave kolem sebe!";
				effect = victim => {
					double dealt = MadBlow(player, victim, factor);
					if (!victim.IsDeleted && !victim.Flag_Dead) {
						SingletonScript<BleedingEffectPluginDef>.Instance.Strike(player, victim, dealt);
					}
				};
			} else if (prof == Professions.Mage) {
				count = 5; range = 8;
				message = "V nahlem pomateni kolem sebe bezhlave sesilas kouzla!";
				effect = victim => {
					EffectFactory.StationaryEffect(victim, FireColumnEffect, 6, 16);
					victim.Sound(FlamestrikeSound);
					DamageManager.CauseDamage(player, victim, DamageType.MagicFire, (player.MindPowerVsP * 10 / 6.0) * factor);
				};
			} else if (prof == Professions.Ranger) {
				Item held = player.FindLayer(LayerNames.Hand1) as Item;
				bool bow = (held is Weapon weapon) && ((weapon.WeaponType == WeaponType.Bow) || (weapon.WeaponType == WeaponType.XBow));
				count = 1; range = bow ? 8 : 2;
				message = bow ? "V nahlem pomateni bezhlave strilis kolem sebe!" : null;
				costsStamina = bow;
				effect = victim => {
					MadBlow(player, victim, bow ? factor : 1);
					if (bow && !victim.IsDeleted && !victim.Flag_Dead) {
						Direction away = Point2D.GetDirFromTo(player, victim);
						ForceMove(victim, ClassAbilitySettings.instance.knockbackRange, (x, y) => away, (x, y) => false);
					}
				};
			} else if (prof == Professions.Necromant) {
				count = 5; range = 4;
				message = "V nahlem pomateni kolem sebe bezhlave sesilas kletby!";
				effect = victim => Poison.Apply(victim, player, (int) Math.Max(1, player.MindPowerVsP * factor), 5, PoisonKind.AbsoluteDamage);
			} else if (prof == Professions.Priest) {
				count = 5; range = 4;
				message = "V nahlem pomateni kolem sebe bezhlave sesilas kletby!";
				effect = victim => {
					SpellDef.GetById(27)?.ApplyEffectChar(player, victim, (int) (player.GetSkill(SkillName.Healing) * factor), EffectFlag.HarmfulEffect | EffectFlag.FromAbility);
					victim.SysMessage(victim.IsFemale ? "Byla jsi prokleta." : "Byl jsi proklet.");
				};
			} else if (prof == Professions.Shaman) {
				count = 1; range = 8;
				message = "V nahlem pomateni kolem sebe bezhlave sesilas kletby!";
				effect = victim => { //#1450 - a real lasting curse, as Erebor's woodooeffect; any voodoo (a blessing too) spares one
					if (VoodooEffectPlugin.HasAny(victim)) {
						return;
					}
					victim.SysMessage("Jsi pod prokletim voodoo.");
					VoodooEffectPlugin.Apply(victim, Globals.dice.Next(1, 8), player);
				};
			} else if (prof == Professions.Mystic) {
				count = 5; range = 4;
				message = "V nahlem pomateni kolem sebe bezhlave sesilas kletby!";
				effect = victim => {
					MadBlow(player, victim, factor);
					if (!victim.IsDeleted && !victim.Flag_Dead) {
						ParalyzeEffectPluginDef.Paralyze(victim, madnessParalyzePK, TimeSpan.FromSeconds(Globals.dice.Next(20, 31)));
					}
				};
			} else {
				count = 1; range = 2;
				message = "V nahlem pomateni bijes bezhlave kolem sebe!";
				effect = victim => MadBlow(player, victim, 1);
			}
			if (costsStamina) {
				player.Stam = (short) Math.Max(0, player.Stam - Globals.dice.Next(10, 21));
			}
			if (message != null) {
				player.SysMessage(message);
			}
			int hit = 0;
			foreach (Character victim in CharsAround(player.GetMap(), player.X, player.Y, range)) {
				if (hit >= count) {
					break;
				}
				if (IsMadnessVictim(player, victim)) {
					effect(victim);
					hit++;
				}
			}
			return hit;
		}

		//players and pets, not GMs, not the mad one; the invisible too; in sight beyond the next tile
		private static bool IsMadnessVictim(Player mad, Character victim) {
			if ((victim == mad) || victim.IsDeleted || !victim.IsAliveAndValid || victim.IsGM) {
				return false;
			}
			if (!(victim is Player) && !((victim is NPC npc) && (npc.ControlMaster != null))) {
				return false;
			}
			return (Point2D.GetSimpleDistance(mad, victim) <= 1) || mad.CanSeeLOS(victim).Allow;
		}

		/// <summary>The mad player's own blow on a friend, scaled.</summary>
		private static double MadBlow(Player player, Character victim, double factor) {
			double damage = DamageManager.GetWeaponSwingArgs(player, victim).DamageAfterAC * factor;
			DamageManager.CauseCustomDamage(player, victim, DamageType.Physical, damage); //#1511 - doDmg, damagecust
			return damage;
		}
		#endregion

		/// <summary>TryReactToOwnSwing's part for the HCN tombs (landed blows).</summary>
		internal static void TryHcnOwnSwing(Character attacker, Character defender, int damage) {
			TryHcnPoison(attacker, defender, damage);
			TryHcnMadness(attacker, defender);
		}

		/// <summary>TryReactToHit's part for the HCN tombs.</summary>
		internal static void TryHcnReactions(NPC self, Character attacker) {
			TryHcnRemember(self, attacker);
		}
	}

	/// <summary>i_hcn_AttackPort_npc: on an HCN monster while it fights a player, gone with the fight.</summary>
	[SaveableClass, DeepCopyableClass]
	public class HcnPursuitTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_hcnPursuit_");

		[SaveableData, CopyableData]
		public int counter;

		[SaveableData, CopyableData]
		public List<Player> remembered = new List<Player>();

		[LoadingInitializer, DeepCopyImplementation]
		public HcnPursuitTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is NPC self) && !self.IsDeleted && !self.Flag_Dead) {
				MonsterSpecialAttacks.HcnPursuitTick(self, this);
			}
		}
	}
}
