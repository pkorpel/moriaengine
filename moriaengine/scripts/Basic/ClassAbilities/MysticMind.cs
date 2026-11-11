using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class HypnotizerBallDef {
	}

	/// <summary>#1017 - i_hypnotizer, the mystic's crystal ball: a double click opens d_hypnotizerBall, or brings a
	/// possessing mystic home.</summary>
	[ViewableClass]
	public partial class HypnotizerBall {
		internal const int NormalColor = 0x480, PossessingColor = 0x445;

		public override void On_DClick(AbstractCharacter from) {
			if (from is Player player) {
				if (MysticHypnosis.IsPossessing(player)) {
					MysticHypnosis.Hypnotize(player);
				} else {
					player.Dialog(player, SingletonScript<D_HypnotizerBall>.Instance, new DialogArgs());
				}
			}
		}
	}

	/// <summary>The mystic's mind abilities from specabilities.scp: controlnpc, catch, catchField, buffSteal.
	/// Hypnosis is in MysticHypnosis.</summary>
	public static class MysticMind {
		internal const string BallDef = "i_hypnotizer";
		internal const int FlagCanBe = 0x1, FlagLoot = 0x2;
		private static readonly int[] buffStats = { 2, 4, 7, 11, 16, 22, 29, 37, 46, 56 };
		private static readonly int[] buffDmg = { 2, 5, 8, 11, 14, 19, 24, 30, 37, 45 };
		private static readonly int[] buffDmgNpc = { 20, 22, 24, 26, 28, 30, 32, 34, 36, 38 };
		private static readonly string[] talentNames = { "Inteligence", "Sila", "Obratnost", "Utok" };

		internal static readonly TagKey CatchedOnceTK = TagKey.Acquire("_wasCatched_");
		internal static readonly TagKey FieldReleasedAtTK = TagKey.Acquire("_catchFieldReleasedAt_");
		internal static readonly TagKey BuffDmgBonusTK = TagKey.Acquire("_buffDmgBonus_");
		internal static readonly TagKey NpcDamageCutTK = TagKey.Acquire("_changeDmgPercent_");
		private static readonly PluginKey fieldParalysisPK = PluginKey.Acquire("_catchFieldParalysis_");

		internal static bool IsMystic(Character ch) {
			return Professions.HasProfession(ch, Professions.Mystic);
		}

		internal static bool HasBall(Character ch) {
			return ShamanAbilities.FindInPack(ch, BallDef) != null;
		}

		internal static int Flags(Character ch) {
			return (ch is NPC npc) ? ((NPCDef) npc.Def).Hypnotizable : 0;
		}

		/// <summary>Sphere's flag_immobile, as catch and catchField set it: no steps, a blow is still possible (#1409: ImmobilePlugin).</summary>
		internal static bool CheckCanMove(Character self) {
			return !ImmobilePlugin.IsImmobile(self);
		}

		private static void SetImmobile(Character ch, bool on) {
			ImmobilePlugin.Set(ch, on);
		}

		private static bool IsIdle(Character ch) {
			return (ch.CurrentSkill == null) && !ch.Flag_WarMode;
		}

		#region controlnpc
		[SteamFunction]
		public static void ControlNpc(Player self) {
			if (!IsMystic(self)) {
				self.SysMessage("Toto muze pouzit jen mystik.");
			} else if (self.Level < 30) {
				self.SysMessage("Nejsi dost dobry mystik");
			} else if (!HasBall(self)) {
				self.RedMessage("Musis mit svou kristalovou kouli!");
			} else if (self.Flag_Dead) {
				self.SysMessage("You are dead!");
			} else if (!CheckCanMove(self)) {
				self.SysMessage("You are frozen and cannot move.");
			} else {
				self.SysMessage("Zamer npc.");
				self.Target(SingletonScript<Targ_ControlNpc>.Instance);
			}
		}

		internal static void ControlTarget(Player self, Character target) {
			if (target.Flag_Invulnerable || (target.Def.PrettyDefname == "c_dog") || AnimateDeadSpellDef.IsAnimated(target)) {
				self.RedMessage("Cil nelze ovladnout");
			} else if (target.GetSkill(SkillName.AnimalLore) > self.GetSkill(SkillName.AnimalLore)) {
				self.RedMessage("V mysli cile se nedokazes orientovat");
			} else if ((target.ControlMaster == null) && !SummonSpellDef.IsSummonedCreature(target)) {
				self.RedMessage("Nepovedlo se.");
			} else if (!self.CanSeeLOS(target).Allow) {
				self.RedMessage("Nevidis na cil.");
			} else if (Point2D.GetSimpleDistance(self, target) > 5) {
				self.RedMessage("Cil je prilis daleko.");
			} else {
				ControlNpcPlugin old = ControlNpcPlugin.Of(self);
				if (old != null) {
					old.Delete();
				}
				ControlNpcPlugin control = (ControlNpcPlugin) ControlNpcPluginDef.instance.Create(); //#1455 - i_controlnpc
				control.target = target;
				self.AddPlugin(ControlNpcPlugin.Key, control);
				control.Timer = 5;
			}
		}

		/// <summary>i_controlnpc's @timer: ra * 900 out of 10000, and the creature is the mystic's. Erebor left the
		/// old owner's memory beside the new one; here a creature has one master, so it simply changes hands.</summary>
		internal static void FinishControl(Character self, Character target) {
			if ((target == null) || target.IsDeleted) {
				return;
			}
			int points = ClassAbilities.Points(self, "controlnpc");
			if ((Globals.dice.Next(0, 10001) > points * ClassAbilitySettings.instance.controlNpcChance) && !self.IsGM) {
				self.RedMessage("Nepovedlo se.");
				return;
			}
			self.RedMessage((self.IsFemale ? "Ovladla" : "Ovladl") + " jsi ho.");
			target.ControlMaster = self;
		}
		#endregion

		#region catch
		[SteamFunction]
		public static void Catch(Player self) {
			CatchPlugin held = CatchPlugin.Of(self);
			if (ClassAbilities.Points(self, "catch") <= 0) {
				self.RedMessage("Tuto schopnost neumis pouzit.");
			} else if (MysticHypnosis.IsPossessing(self)) {
				self.RedMessage("Nelze pouzit v hypnoze!");
			} else if (held != null) {
				self.SysMessage((self.IsFemale ? "Uvolnila" : "Uvolnil") + " jsi svou mysl i mysl nepritele.");
				ReleaseCatch(self, held, false);
			} else if (CatchFieldPlugin.Of(self) != null) {
				self.SysMessage("Tva mysl je jiz plne zaneprazdnena.");
			} else if (!IsMystic(self)) {
				self.RedMessage("Tuto abilitu nemuzes pouzit.");
			} else if (self.Flag_Dead) {
				self.RedMessage((self.IsFemale ? "Mrtva" : "Mrtvy") + " tuto abilitu nemuzes pouzit.");
			} else if (!HasBall(self)) {
				self.SysMessage("Musis mit u sebe kristalovou kouli.");
			} else if (self.Level < 20) {
				self.SysMessage("Nejsi jeste dost " + (self.IsFemale ? "zkusena." : "zkuseny."));
			} else if (self.Mana < 20) {
				self.SysMessage("Nemas dost energie.");
			} else if (ClassAbilities.ReduceStamina(self, 10)) {
				self.Target(SingletonScript<Targ_Catch>.Instance);
			}
		}

		internal static void CatchTarget(Player self, Character target) {
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage("Nevidis na cil.");
			} else if (target.IsPlayer) {
				self.SysMessage("Toto nelze pouzit na hrace.");
			} else if (Globals.dice.Next(0, ClassAbilities.Points(self, "catch") + 1) == 1) { //Erebor's roll fails only on exactly 1
				self.SysMessage("Nepovedlo se.");
			} else if (target.GetTag(CatchedOnceTK) != null) {
				self.SysMessage("Mysl monstra se vzeprela!");
				DamageManager.CauseDamage(null, self, DamageType.MagicMystical, 30);
			} else {
				target.SetTag(CatchedOnceTK, 1);
				CatchPlugin hold = (CatchPlugin) CatchPluginDef.instance.Create(); //#1455 - i_catchitem
				hold.monster = target;
				self.AddPlugin(CatchPlugin.Key, hold);
				hold.Timer = 0;
			}
		}

		/// <summary>i_catchitem's tick: the hold starts, then mana and half as much stamina drain a second, more each
		/// second, for 25 ticks or until either is under 20.</summary>
		internal static void CatchTick(Character self, CatchPlugin hold) {
			if (self.Flag_Dead || MysticHypnosis.IsPossessing(self) || (hold.monster == null) || hold.monster.IsDeleted) {
				ReleaseCatch(self, hold, true);
				return;
			}
			hold.Timer = 1; //the next tick, unless this one lets go
			if (hold.ticks == 0) {
				hold.ticks = 1;
				hold.monsterExp = MonsterExp(hold.monster);
				SetImmobile(hold.monster, true);
				SetImmobile(self, true);
				self.SysMessage("Drzis monstrum svou mysli na miste...");
				return;
			}
			if ((self.Mana < 20) || (self.Stam < 20) || (hold.ticks > 25)) {
				ReleaseCatch(self, hold, true);
				return;
			}
			int drain = CatchDrain(hold.monsterExp, self is Player player ? player.Level : 1, hold.ticks);
			self.Mana = (short) Math.Max(0, self.Mana - drain);
			self.Stam = (short) Math.Max(0, self.Stam - (drain / 2));
			hold.ticks++;
		}

		/// <summary>propocet: the monster's exp over five times the level, and 3 more each second.</summary>
		internal static int CatchDrain(int monsterExp, int level, int tick) {
			return (monsterExp / (Math.Max(level, 1) * 5)) + (tick * 3);
		}

		/// <summary>Erebor's message on running out was lost for everyone after the first manual release (a global
		/// var); here only a manual release skips it.</summary>
		internal static void ReleaseCatch(Character self, CatchPlugin hold, bool tired) {
			if (tired && !MysticHypnosis.IsPossessing(self)) {
				self.SysMessage("Uz nemas silu monstrum drzet!");
			}
			if (hold.monster != null) {
				SetImmobile(hold.monster, false);
			}
			SetImmobile(self, false);
			hold.Delete();
		}

		internal static int MonsterExp(Character ch) {
			return (ch is NPC npc) ? npc.ExperienceReward : 0;
		}
		#endregion

		#region catchField
		[SteamFunction]
		public static void CatchField(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			CatchFieldPlugin field = CatchFieldPlugin.Of(self);
			MysticProfessionPlugin mystic = MysticProfessionPlugin.Of(self);
			if (ClassAbilities.Points(self, "catch") <= 0) {
				self.RedMessage("Tuto schopnost neumis pouzit.");
			} else if (field != null) {
				EndCatchField(self, field);
			} else if ((CatchPlugin.Of(self) != null) || MysticHypnosis.IsPossessing(self)) {
				self.SysMessage("Tva mysl je jiz plne zaneprazdnena.");
			} else if (!IsMystic(self)) {
				self.RedMessage("Tuto abilitu nemuzes pouzit.");
			} else if (self.Flag_Dead) {
				self.RedMessage((self.IsFemale ? "Mrtva" : "Mrtvy") + " tuto abilitu nemuzes pouzit.");
			} else if (!HasBall(self)) {
				self.RedMessage("Musis mit svou kristalovou kouli!");
			} else if ((mystic != null) && (mystic.nextCatchField > Globals.TimeInSeconds)) { //#1455 - the cooldown is the plugin's
				self.SysMessage("Abilitu jeste nemuzes pouzit.", ClassAbilities.ClassMessageHue);
			} else if (self.Level < 20) {
				self.SysMessage("Nejsi jeste dost " + (self.IsFemale ? "zkusena." : "zkuseny."));
			} else if ((self.Mana < 120) || (self.Stam < 80)) {
				self.SysMessage("Nemas dost energie.");
			} else if (ClassAbilities.ReduceStamina(self, 10)) {
				HidingSkillDef.Reveal(self);
				self.SysMessage("Poutas na sebe monstra ve svem blizkem okoli.", ClassAbilities.ClassMessageHue);
				StartCatchField(self, mystic, settings);
			}
		}

		/// <summary>i_memory_catchField's @Equip. Erebor's 10 mana was charged to the item, so nothing; kept.</summary>
		private static void StartCatchField(Player self, MysticProfessionPlugin mystic, ClassAbilitySettings settings) {
			if (2 * ClassAbilities.Points(self, "catch") < Globals.dice.Next(0, 41)) {
				self.SysMessage("Nepovedlo se.");
				if (mystic != null) {
					mystic.nextCatchField = Globals.TimeInSeconds + 10;
				}
				self.SysMessage((self.IsFemale ? "Uvolnila" : "Uvolnil") + " jsi svou mysl i mysl nepritele.");
				return;
			}
			CatchFieldPlugin field = (CatchFieldPlugin) CatchFieldPluginDef.instance.Create(); //#1455 - i_memory_catchField
			field.effort = 3;
			field.held = new List<NPC>();
			self.AddPlugin(CatchFieldPlugin.Key, field);
			field.Timer = 1;
			CaptureAround(self, field);
			self.Sound(586);
			EffectFactory.StationaryEffect(self, 0x373a, 9, 40);
			if (mystic != null) {
				mystic.nextCatchField = Globals.TimeInSeconds + settings.catchFieldCooldownSeconds;
			}
			SetImmobile(self, true);
		}

		/// <summary>f_catchField_captureNPC over NPCs within 3 tiles. Erebor scanned only its own 64x64 sector, so
		/// at a sector edge some were missed; here all within reach.</summary>
		internal static void CaptureAround(Character self, CatchFieldPlugin field) {
			int level = (self is Player player) ? player.Level : 1;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 3)) {
				if (!(ch is NPC npc) || (CaughtPlugin.Of(npc) != null) || npc.Flag_Dead ||
						((npc.GetTag(FieldReleasedAtTK) != null) && (ClassAbilities.SecondsTag(npc, FieldReleasedAtTK) + 3600 > Globals.TimeInSeconds))) {
					continue;
				}
				int power = MonsterExp(npc) / (400 + (level * 3));
				CaughtPlugin caught = (CaughtPlugin) CaughtPluginDef.instance.Create(); //#1455 - e_catchedInField
				caught.mystic = self;
				caught.power = power;
				npc.AddPlugin(CaughtPlugin.Key, caught);
				field.effort += power + 2;
				field.held.Add(npc);
				npc.Flag_WarMode = false;
				ParalyzeEffectPluginDef.Paralyze(npc, fieldParalysisPK, TimeSpan.FromHours(10));
				SetImmobile(npc, true);
				self.SysMessage(npc.Name + " je spoutan tvou mysli.");
			}
		}

		internal static void CatchFieldTick(Character self, CatchFieldPlugin field) {
			if (self.Flag_Dead) {
				EndCatchField(self, field);
				return;
			}
			field.Timer = 1; //the next tick, unless this one ends it
			CaptureAround(self, field);
			self.Flag_WarMode = false;
			self.AbortSkill();
			int level = (self is Player player) ? player.Level : 1;
			int effort = (Globals.dice.Next(0, 450) < level) ? field.effort / 2 : field.effort;
			self.Mana = (short) Math.Max(0, self.Mana - effort);
			self.Stam = (short) Math.Max(0, self.Stam - (effort / 4));
			if ((self.Mana == 0) || (self.Stam == 0)) {
				EndCatchField(self, field);
			}
		}

		internal static void EndCatchField(Character self, CatchFieldPlugin field) {
			if (field.held != null) {
				foreach (NPC npc in field.held.ToArray()) {
					ReleaseFromField(npc, field);
				}
			}
			self.SysMessage((self.IsFemale ? "Uvolnila" : "Uvolnil") + " jsi svou mysl i mysl nepritele.");
			SetImmobile(self, false);
			field.Delete();
		}

		/// <summary>f_catchField_releaseNPC: free for catch again, not for a field for an hour.</summary>
		internal static void ReleaseFromField(NPC npc, CatchFieldPlugin field) {
			CaughtPlugin caught = CaughtPlugin.Of(npc);
			if ((field != null) && (field.held != null)) {
				field.effort -= (caught == null) ? 0 : caught.power;
				field.held.Remove(npc);
			}
			npc.SetTag(FieldReleasedAtTK, Globals.TimeInSeconds);
			npc.RemoveTag(CatchedOnceTK);
			if (caught != null) {
				caught.Delete();
			}
			SetImmobile(npc, false);
			ParalyzeEffectPluginDef.Unparalyze(npc);
		}
		#endregion

		#region buffSteal
		[SteamFunction]
		public static void BuffSteal(Player self) {
			if (ClassAbilities.Points(self, "buffSteal") <= 0) {
				self.RedMessage("Tuto schopnost neumis pouzit.");
			} else if (self.Flag_Dead) {
				self.RedMessage((self.IsFemale ? "Mrtva" : "Mrtvy") + " tuto abilitu nemuzes pouzit!"); //Erebor's typo said nothing
			} else if (!HasBall(self)) {
				self.RedMessage("Musis mit svou kristalovou kouli!");
			} else if (MysticHypnosis.IsPossessing(self) || (CatchFieldPlugin.Of(self) != null) || (CatchPlugin.Of(self) != null)) {
				self.SysMessage("Tva mysl je jiz plne zaneprazdnena.");
			} else if (self.Mana < 100) {
				self.SysMessage("Nemas dost many.");
			} else if (!IsIdle(self)) {
				self.RedMessage("Pro ukradnuti talentu musis byt v naprostem klidu.");
			} else if (self.HasTimer(BuffStealTimer.Key)) {
				self.RedMessage("Tuto schopnost jeste nelze pouzit.");
			} else {
				self.SysMessage("Zamer monstrum, ktere chces zbavit talentu.");
				self.Target(SingletonScript<Targ_BuffSteal>.Instance);
			}
		}

		internal static int TalentOf(Character target) {
			int flags = Flags(target);
			for (int n = 1; n <= 4; n++) {
				if ((flags & (0x4 << (n - 1))) != 0) {
					return n;
				}
			}
			return 0;
		}

		internal static int PowerOf(Character target) {
			int flags = Flags(target);
			for (int p = 1; p <= 10; p++) {
				if ((flags & (0x400000 << (p - 1))) != 0) {
					return p;
				}
			}
			return 0;
		}

		internal static void BuffStealTarget(Player self, Character target) {
			int talent = TalentOf(target);
			if (!self.CanSeeLOS(target).Allow) {
				self.RedMessage("Nemas s cilem primy kontakt.");
			} else if (target.HasTimer(BuffStolenTimer.Key)) {
				self.RedMessage("Cili uz nekdo talent vzal.");
			} else if (self.Stam < 80) {
				self.SysMessage("Nemas dost staminy.");
			} else if (talent == 0) {
				self.SysMessage("Cil nema zadny talent.");
			} else {
				BuffStealTimer timer = new BuffStealTimer();
				timer.target = target;
				timer.type = talent;
				self.AddTimer(BuffStealTimer.Key, timer);
				timer.DueInSeconds = 3;
				self.SysMessage("Pokusis se cili ukrast talent.");
				self.Sound(586);
			}
		}

		/// <summary>f_buffStaffValue, by the staff in hand.</summary>
		internal static int StaffValue(Character self) {
			if (self.IsGM) {
				return 2200;
			}
			if (!(self.FindLayer(LayerNames.Hand2) is ColoredStaff staff) || !(staff.Def is IObjectWithMaterial made)) {
				return 0;
			}
			switch (made.Material) {
				case Material.Elven:
					return 2200;
				case Material.Eben:
					return 1500;
				case Material.Mahagon:
					return 1200;
				case Material.Teak:
					return 950;
				case Material.Oak:
					return 700;
				case Material.Chestnut:
					return 400;
				case Material.Spruce:
					return 200;
				default:
					return 0;
			}
		}

		/// <summary>i_buffSteal's first tick: the checks, then f_buffSteal. Returns how long the next state lasts.</summary>
		internal static void StealTick(Player self, BuffStealTimer timer) {
			Character target = timer.target;
			if ((target == null) || target.IsDeleted || !self.CanSeeLOS(target).Allow) {
				self.RedMessage("Ztratil" + (self.IsFemale ? "a" : "") + " jsi cil z dohledu.");
				timer.Delete();
				return;
			}
			if (!IsIdle(self)) {
				self.RedMessage((self.IsFemale ? "Prerusila" : "Prerusil") + " jsi soustredeni.");
				timer.Delete();
				return;
			}
			if (SummonSpellDef.IsSummonedCreature(target)) {
				self.SysMessage("Cil nema vlastni talent.", ClassAbilities.ClassMessageHue);
				timer.Delete();
				return;
			}
			if (target.HasTimer(BuffStolenTimer.Key)) {
				self.RedMessage("Cili uz nekdo talent vzal.");
				timer.Delete();
				return;
			}
			int a = (10 * self.MindPowerVsM) + StaffValue(self) + (self.Level * 5);
			int m = Globals.dice.Next(0, (10 * target.GetSkill(SkillName.SpiritSpeak)) + 1);
			bool rolled = ClassAbilities.Points(self, "buffSteal") >= Globals.dice.Next(0, 22);
			int power = Math.Max(PowerOf(target), 1);
			timer.power = power;
			if (rolled && (a > m)) {
				if (PowerOf(target) == 0) { //Erebor applied it at power 1 and could never take it back
					self.RedMessage("Cil nema definovanu silu boostu!");
					timer.state = 2;
					timer.DueInSeconds = 20;
					return;
				}
				ClassAbilities.ReduceStaminaToZero(self, power * 6);
				Steal(self, target, timer);
				timer.state = 1;
				timer.success = true;
				timer.DueInSeconds = 60;
			} else if (rolled ? (Globals.dice.Next(0, (3 * a) + 1) < Globals.dice.Next(0, Math.Max(m - a, 0) + 1)) : (Globals.dice.Next(0, (2 * a) + 1) < (m - a))) {
				self.RedMessage("Pri pokusu ovladnout talent cile jsi fatalne " + (self.IsFemale ? "selhala!" : "selhal!"));
				ClassAbilities.ReduceStaminaToZero(self, 100);
				Cripple(self, timer);
				timer.state = 1;
				timer.success = false;
				timer.DueInSeconds = 60;
			} else {
				ClassAbilities.ReduceStaminaToZero(self, power * (rolled ? 6 : 10));
				self.SysMessage("Nepodarilo se ti ovladnout talent cile.", ClassAbilities.ClassMessageHue);
				timer.state = 2;
				timer.DueInSeconds = 20;
			}
		}

		private static void Steal(Player self, Character target, BuffStealTimer timer) {
			int v = buffStats[timer.power - 1], d = buffDmgNpc[timer.power - 1];
			BuffStolenTimer npcTimer = new BuffStolenTimer();
			npcTimer.type = timer.type;
			switch (timer.type) {
				case 1:
					self.Int = (short) (self.Int + v);
					npcTimer.changed = (d * target.GetSkill(SkillName.EvalInt)) / 100;
					target.SetRealSkillValue((int) SkillName.EvalInt, target.GetSkill(SkillName.EvalInt) - npcTimer.changed);
					break;
				case 2:
					self.Str = (short) (self.Str + v);
					target.SetTag(NpcDamageCutTK, d);
					break;
				case 3:
					self.Dex = (short) (self.Dex + v);
					npcTimer.changed = (d * target.Dex) / 100;
					target.Dex = (short) (target.Dex - npcTimer.changed);
					break;
				case 4:
					self.SetTag(BuffDmgBonusTK, buffDmg[timer.power - 1]);
					target.SetTag(NpcDamageCutTK, d);
					break;
			}
			timer.changed = (timer.type == 4) ? 0 : v;
			target.AddTimer(BuffStolenTimer.Key, npcTimer);
			npcTimer.DueInSeconds = 60;
			self.SysMessage((self.IsFemale ? "Ukradla" : "Ukradl") + " jsi talent protivnika. " + ((timer.type == 4) ? "Zvysen Utok." : "Zvysena " + talentNames[timer.type - 1] + "."), ClassAbilities.ClassMessageHue);
		}

		private static void Cripple(Player self, BuffStealTimer timer) {
			int v = buffStats[timer.power - 1];
			timer.changed = (timer.type == 4) ? 0 : v;
			switch (timer.type) {
				case 1:
					self.Int = (short) (self.Int - v);
					break;
				case 2:
					self.Str = (short) (self.Str - v);
					break;
				case 3:
					self.Dex = (short) (self.Dex - v);
					break;
				case 4:
					self.SetTag(BuffDmgBonusTK, -buffDmg[timer.power - 1]);
					break;
			}
			self.SysMessage("Docasne " + ((timer.type == 4) ? "ochromen Utok." : "ochromena " + talentNames[timer.type - 1] + "."), ClassAbilities.ClassMessageHue);
		}

		/// <summary>f_buffreturnDown after a steal, f_buffreturnUp after a fatal failure.</summary>
		internal static void Reverse(Character self, BuffStealTimer timer) {
			int sign = timer.success ? -1 : 1;
			self.SysMessage(timer.success ? "Ztracis ziskany talent." : "Ochromeni po neuspesnem kradeni talentu pominulo.", ClassAbilities.ClassMessageHue);
			switch (timer.type) {
				case 1:
					self.Int = (short) (self.Int + (sign * timer.changed));
					self.Mana = (short) Math.Min(self.Mana, self.MaxMana);
					break;
				case 2:
					self.Str = (short) (self.Str + (sign * timer.changed));
					break;
				case 3:
					self.Dex = (short) (self.Dex + (sign * timer.changed));
					break;
				case 4:
					self.RemoveTag(BuffDmgBonusTK);
					break;
			}
		}

		/// <summary>e_mystik's @death: a steal under way is undone and its cooldown goes with it.</summary>
		internal static void OnDeath(Character self) {
			if (self.GetTimer(BuffStealTimer.Key) is BuffStealTimer timer) {
				if (timer.state == 1) {
					Reverse(self, timer);
				}
				timer.Delete();
			}
		}

		/// <summary>magie.scp:176 and e_npcChangeDmg, on blows: a stolen attack talent, and a robbed monster's cut.</summary>
		internal static void ModifyBlow(Character attacker, Character defender, WeaponSwingArgs swing) {
			if (attacker is Player && !defender.IsPlayerForCombat && (attacker.GetTag(BuffDmgBonusTK) is int bonus)) {
				swing.DamageAfterAC += (swing.DamageAfterAC / 100) * bonus;
			}
			if ((attacker is NPC) && (attacker.GetTag(NpcDamageCutTK) is int cut)) {
				swing.DamageAfterAC -= (swing.DamageAfterAC * cut) / 100;
			}
		}

		internal static int ModifySpellPower(Character caster, Character target, int spellPower) {
			if (!target.IsPlayerForCombat && (caster.GetTag(BuffDmgBonusTK) is int bonus)) {
				return spellPower + ((spellPower / 100) * bonus);
			}
			return spellPower;
		}
		#endregion
	}

	public class Targ_ControlNpc : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			MysticMind.ControlTarget(self, targetted);
			return TargetResult.Done;
		}
	}

	public class Targ_Catch : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamer monstrum, ktere chces zadrzet.");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			MysticMind.CatchTarget(self, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("Neni mozne pouzit na item!");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Tady nic neni.");
			return TargetResult.Done;
		}
	}

	public class Targ_BuffSteal : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			MysticMind.BuffStealTarget(self, targetted);
			return TargetResult.Done;
		}
	}

	/// <summary>i_buffSteal: state 0 the 3 s wind-up, 1 the 60 s effect, 2 the cooldown.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BuffStealTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_buffSteal_");

		[SaveableData, CopyableData]
		public Character target;

		[SaveableData, CopyableData]
		public int type, power, changed, state;

		[SaveableData, CopyableData]
		public bool success;

		[DeepCopyImplementation, LoadingInitializer]
		public BuffStealTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Player self = cont as Player;
			if (self == null) {
				return;
			}
			if (this.state == 0) {
				this.Again(self);
				MysticMind.StealTick(self, this);
			} else if (this.state == 1) {
				MysticMind.Reverse(self, this);
				BuffStealTimer cooldown = this.Again(self);
				cooldown.state = 2;
				cooldown.DueInSeconds = 50 + (7 * this.power);
			}
		}

		/// <summary>A one-shot timer is gone by its timeout, so the next state is a fresh one.</summary>
		private BuffStealTimer Again(Player self) {
			BuffStealTimer next = new BuffStealTimer();
			next.target = this.target;
			next.type = this.type;
			next.power = this.power;
			next.changed = this.changed;
			next.state = this.state;
			next.success = this.success;
			self.AddTimer(Key, next);
			next.DueInSeconds = 1;
			return next;
		}
	}

	/// <summary>i_buffStealNPC: 60 s, then the monster has its talent back.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BuffStolenTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_buffStealNpc_");

		[SaveableData, CopyableData]
		public int type, changed;

		[DeepCopyImplementation, LoadingInitializer]
		public BuffStolenTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character npc) {
				switch (this.type) {
					case 1:
						npc.SetRealSkillValue((int) SkillName.EvalInt, npc.GetSkill(SkillName.EvalInt) + this.changed);
						break;
					case 3:
						npc.Dex = (short) (npc.Dex + this.changed);
						break;
					default:
						npc.RemoveTag(MysticMind.NpcDamageCutTK);
						break;
				}
			}
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_hypnotizerBall.</summary>
	public class D_HypnotizerBall : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			List<string[]> rows = new List<string[]>();
			if (MysticMind.IsMystic(player)) {
				rows.Add(new[] { "1", "Hypnoza", ".hypnotize" });
			}
			if (ClassAbilities.Points(player, "buffSteal") > 0) {
				rows.Add(new[] { "2", "Ovladnuti Talentu", ".buffsteal" });
			}
			if (ClassAbilities.Points(player, "controlnpc") > 0) {
				rows.Add(new[] { "3", "Ovladani NPC", ".controlnpc" });
			}
			if (ClassAbilities.Points(player, "catch") > 0) {
				rows.Add(new[] { "4", "Drzeni NPC", ".catch" });
				rows.Add(new[] { "5", "Drzeni NPC v okoli", ".catchfield" });
			}
			this.SetLocation(250, 300);
			this.AddPage(0);
			this.AddBackground(0, 0, 314, 60 + (Math.Max(rows.Count, 1) * 22), 2620);
			this.AddLabel(90, 12, 42, "Hypnotizer's ball");
			this.AddButton(276, 9, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (rows.Count == 0) {
				this.AddLabel(20, 40, 2301, "Nevis, jak kouli pouzit.");
			}
			for (int i = 0; i < rows.Count; i++) {
				int y = 40 + (i * 22);
				this.AddButton(20, y, 4020, 4022, int.Parse(rows[i][0], System.Globalization.CultureInfo.InvariantCulture), GumpButtonType.Reply, 0);
				this.AddLabel(55, y, 2301, rows[i][1]);
				this.AddLabel(210, y, 945, rows[i][2]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			switch (gr.PressedButton) {
				case 1:
					MysticHypnosis.Hypnotize(player);
					break;
				case 2:
					MysticMind.BuffSteal(player);
					break;
				case 3:
					MysticMind.ControlNpc(player);
					break;
				case 4:
					MysticMind.Catch(player);
					break;
				case 5:
					MysticMind.CatchField(player);
					break;
			}
		}
	}
}
