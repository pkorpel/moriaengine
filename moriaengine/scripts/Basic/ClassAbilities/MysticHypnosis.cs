using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - the mystic's hypnosis, specabilities.scp's hypnotize, hypnoswap and hypnoswap_back: the mystic's
	/// soul goes into a monster's body for a while. The monster waits on plane 250, an alter ego stands in the mystic's
	/// place, and a blow or spell on it, the mystic's death or a logout bring the mystic home. The state lives on the
	/// mystic's PossessionTimer.</summary>
	public static class MysticHypnosis {
		internal const int ParkedPlane = 250;
		internal static readonly TagKey EgoOfTK = TagKey.Acquire("_hypnoEgoOf_");
		private static readonly PluginKey egoParalysisPK = PluginKey.Acquire("_hypnoEgoParalysis_");
		private static readonly HashSet<string> throwers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"c_ogre", "c_ogre_lord", "c_ettin", "c_cyclops", "c_elem_rock", "c_elem_ice_archer", "c_elem_snow_archer", "c_moria_jeskyni_Obr" };
		private static readonly HashSet<string> shooters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"c_skeleton_archer", "c_skeleton_archer_lvl_2", "c_skeleton_archer_lvl_3", "c_skeleton_archer_lvl_4" };

		internal static PossessionTimer Possession(Character ch) {
			return ch.GetTimer(PossessionTimer.Key) as PossessionTimer;
		}

		internal static bool IsPossessing(Character ch) {
			return Possession(ch) != null;
		}

		/// <summary>The body a possessing mystic is in, or null.</summary>
		internal static NPC Body(Character ch) {
			PossessionTimer state = Possession(ch);
			return ((state != null) && (state.monster != null) && !state.monster.IsDeleted) ? state.monster : null;
		}

		/// <summary>hypnoswap's karma: an evil body (karma -800 or less) makes the mystic karma -10000, anything else
		/// leaves them below that; the guards of e_guard and its kin let alone a possessing mystic above -10000.</summary>
		internal static bool GuardsLetAlone(Character ch) {
			NPC body = Body(ch);
			return (body != null) && (body.Karma > -800);
		}

		/// <summary>hypnoswap sets kills to 0: a possessing mystic is no murderer to the guards.</summary>
		internal static bool CountsAsMurderer(Character ch) {
			return !IsPossessing(ch) && Notoriety.IsMurderer(ch);
		}

		/// <summary>e_monster's @npcseenewplayer: a monster goes for a possessing mystic on sight.</summary>
		internal static bool MonsterGoesFor(NPC npc, Character candidate) {
			return IsPossessing(candidate) && (Body(candidate) != npc) && MonsterSpecialAttacks.HasMonsterEvent(npc);
		}

		/// <summary>The menus a possessing mystic can't open: travel stones, signs, town, tax, office and shrine stones.
		/// Erebor's @userdclick just returns.</summary>
		internal static bool RefusesMenu(AbstractCharacter ch) {
			return (ch is Character c) && IsPossessing(c);
		}

		/// <summary>Character.DefForCombat: a possessing mystic fights as the monster.</summary>
		internal static CharacterDef CombatDef(Character ch) {
			PossessionTimer state = Possession(ch);
			return ((state != null) && (state.monster != null)) ? state.monster.TypeDef : null;
		}

		/// <summary>zjisti_koef_return.</summary>
		internal static int Koef(int level) {
			return (level < 25) ? 1 : (level < 50) ? 2 : (level < 75) ? 3 : (level < 100) ? 4 : 5;
		}

		/// <summary>#1461 - the retry delay is the mystic's profession plugin's; a GM who isn't a mystic has none.</summary>
		private static void Delay(Character ch, double seconds) {
			MysticProfessionPlugin mystic = MysticProfessionPlugin.Of(ch);
			if (mystic != null) {
				mystic.nextHypnosis = Globals.TimeInSeconds + seconds;
			}
		}

		internal static HypnotizerBall FindBall(Character ch) {
			return ShamanAbilities.FindInPack(ch, MysticMind.BallDef) as HypnotizerBall;
		}

		[SteamFunction]
		public static void Hypnotize(Player self) {
			HypnotizerBall ball = FindBall(self);
			if (!ClassAbilitySettings.instance.hypnosisAllowed) {
				self.RedMessage("Hypnoza docasne zakazana");
			} else if (ball == null) {
				self.SysMessage("Musis mit svou kristalovou kouli!");
			} else if (BuffStealPlugin.Of(self) != null) {
				self.RedMessage("Tva mysl je prilis vycerpana na pouziti hypnozy.");
			} else if ((CatchFieldPlugin.Of(self) != null) || (CatchPlugin.Of(self) != null)) {
				self.SysMessage("Tva mysl je jiz plne zaneprazdnena.");
			} else if (!MysticMind.IsMystic(self) && !self.IsGM) {
				self.RedMessage("Toto umi pouzit pouze mystik!");
			} else if (IsPossessing(self)) {
				SwapBack(self, 0, false);
			} else {
				self.Dismount();
				if (ConcentrationPlugin.Of(self) is ConcentrationPlugin concentrating) {
					concentrating.Delete();
					self.SysMessage((self.IsFemale ? "Prerusila" : "Prerusil") + " jsi koncentraci.");
				}
				if (self.Flag_Dead) {
					self.SysMessage("Ve svem stavu nejsi niceho " + (self.IsFemale ? "schopna." : "schopen."));
				} else if (ParalyzeEffectPluginDef.IsParalyzed(self)) {
					self.SysMessage("Nejsi ve stavu vhodnem pro hypnozu.");
				} else {
					self.SysMessage("Zamer monstrum");
					self.Target(SingletonScript<Targ_Hypnosis>.Instance);
				}
			}
		}

		/// <summary>i_hypnotarget's @TargOn_char.</summary>
		internal static void Targeted(Player self, Character target) {
			NPC monster = target as NPC;
			if (!self.CanSeeLOS(target).Allow) {
				self.RedMessage("Cil musis mit v dohledu!");
			} else if (Point2D.GetSimpleDistance(self, target) > 10) {
				self.RedMessage("Cil je prilis daleko!");
			} else if ((monster == null) || (monster.ControlMaster != null)) {
				self.RedMessage("Zameruj pouze neochocena monstra!");
			} else if ((MysticMind.Flags(monster) & MysticMind.FlagCanBe) == 0) {
				self.SysMessage("Jevi se jako hypnoze netecny.");
			} else if ((MysticProfessionPlugin.Of(self) is MysticProfessionPlugin mystic) && (mystic.nextHypnosis > Globals.TimeInSeconds)) {
				self.SysMessage("Jsi jeste prilis " + (self.IsFemale ? "unavena" : "unaven") + " z predchoziho pokusu", ClassAbilities.ClassMessageHue);
			} else {
				int cas = 10 / Koef(self.Level);
				ConcentrationPlugin concentration = (ConcentrationPlugin) ConcentrationPluginDef.instance.Create(); //#1461 - i_hypno_timer
				concentration.monster = monster;
				self.AddPlugin(ConcentrationPlugin.Key, concentration);
				concentration.Timer = Math.Max(1, Globals.dice.Next(cas - 3, cas + 5));
				self.Sound(586);
				self.Flag_Hidden = false;
			}
		}

		/// <summary>A blow on a possessing mystic's alter ego brings the mystic home. (A blow on a concentrating mystic is
		/// ConcentrationPlugin's @getSwingLanding, #1461.)</summary>
		internal static void OnSwungAt(Character defender) {
			if (defender.GetTag(EgoOfTK) is Player mystic) {
				mystic.SysMessage("Tvou mysl zalila rezava bolest");
				SwapBack(mystic, 2, false);
			}
		}

		/// <summary>A spell landing on the alter ego brings the mystic home.</summary>
		internal static bool OnSpellEffect(Character target) {
			if (target.GetTag(EgoOfTK) is Player mystic) {
				mystic.SysMessage("Tvou mysli projela vlna energie");
				SwapBack(mystic, 2, false);
				return true;
			}
			return false;
		}

		/// <summary>Top of CauseDeath: neither the possessing mystic nor the alter ego dies; the mystic comes home.
		/// A possessing mystic's killers are paid as for the monster.</summary>
		internal static bool InterceptDeath(Character dying, Character killedBy) {
			if (dying.GetTag(EgoOfTK) is Player mystic) {
				SwapBack(mystic, 0, true);
				return true;
			}
			PossessionTimer state = Possession(dying);
			if (state == null) {
				return false;
			}
			if (state.monster != null) {
				int attackers = Math.Max(1, LevelingSystem.FindFighters(dying, killedBy, false).Count);
				state.gained -= (int) ((state.monster.ExperienceReward * Math.Sqrt(attackers)) / 1000);
				LevelingSystem.ShareMonsterExperience(state.monster, killedBy);
			}
			SwapBack((Player) dying, 5, false);
			return true;
		}

		/// <summary>addexp while possessing only counts; it's paid on the way home.</summary>
		internal static bool CountsExperience(Player player, int amount) {
			PossessionTimer state = Possession(player);
			if (state == null) {
				return false;
			}
			state.gained += amount;
			return true;
		}

		/// <summary>povedla_se_hypnoza.</summary>
		internal static bool Succeeds(Player self, NPC monster) {
			int exp = monster.ExperienceReward;
			if (exp <= 0) {
				self.SysMessage("Toto monstrum je zcela netecne tvym pokusum.", ClassAbilities.ClassMessageHue);
				Delay(self, 2);
				return false;
			}
			if (exp > 4000) {
				self.SysMessage("Toto monstrum je na hypnozu prilis narocne.", ClassAbilities.ClassMessageHue);
				Delay(self, 0.5);
				return false;
			}
			int l = self.Level * Koef(self.Level);
			if (Globals.dice.Next(l - 40, l + 21) >= exp / 10) {
				return true;
			}
			self.SysMessage("Hypnoza se nezdarila.", ClassAbilities.ClassMessageHue);
			if (l + 20 < exp / 10) {
				self.SysMessage("Citis, ze toto monstrum je nad tve sily.", ClassAbilities.ClassMessageHue);
			}
			Delay(self, 0.5);
			return false;
		}

		internal static void Concentrated(Player self, NPC monster) {
			if ((monster == null) || monster.IsDeleted || monster.Flag_Dead) {
				self.SysMessage("Hypnotizovane monstrum jiz neni...");
				return;
			}
			if (Succeeds(self, monster)) {
				Swap(self, monster);
				self.Sound(580);
			}
		}

		private static bool StaysOnMystic(Item item) {
			string defname = item.Def.PrettyDefname;
			return (defname == "i_ring_power") || (defname == "i_leading_ring_power");
		}

		private static bool IsEquipmentLayer(int layer) {
			return (layer >= (int) LayerNames.Hand1) && (layer <= (int) LayerNames.Leggins) && (layer != (int) LayerNames.Light) &&
				(layer != (int) LayerNames.Hair) && (layer != (int) LayerNames.Hidden) && (layer != (int) LayerNames.Beard) && (layer != (int) LayerNames.Pack);
		}

		private static Item Copy(Item original, Thing cont, bool fixedThere) {
			Item copy = (Item) ((ItemDef) original.Def).Create(cont);
			copy.Color = original.Color;
			if (fixedThere) {
				copy.SetAttribute(ItemAttributes.attr_move_never, true);
			}
			return copy;
		}

		/// <summary>hypnoswap: the mystic into the monster.</summary>
		internal static void Swap(Player self, NPC monster) {
			PossessionTimer state = new PossessionTimer();
			HypnotizerBall ball = FindBall(self);
			self.Dismount();
			ParalyzeEffectPluginDef.Unparalyze(self);
			Poison.RemoveAll(self);

			//the alter ego, where the mystic stands
			NPC ego = (NPC) ((NPCDef) ThingDef.GetByDefname("c_hypno_ego")).Create(self.X, self.Y, self.Z, self.M);
			foreach (Item own in WornItems(ego)) {
				own.Delete();
			}
			ego.Model = self.Model;
			ego.Color = self.Color;
			ego.Name = self.Name;
			ego.Title = self.Title;
			ego.Str = self.Str;
			ego.Dex = self.Dex;
			ego.Int = self.Int;
			ego.MaxHits = self.MaxHits;
			ego.Hits = self.Hits;
			ego.Flag_WarMode = false;
			ego.SetTag(EgoOfTK, self);
			ImmobilePlugin.Set(ego, true);
			ParalyzeEffectPluginDef.Paralyze(ego, egoParalysisPK, TimeSpan.FromDays(1));
			if (self.FindLayer(LayerNames.Hair) is Item hair) {
				Copy(hair, ego, true);
			}
			state.ego = ego;
			state.monster = monster;

			//worn things to a hidden bag in the bank, copies on the alter ego; the pack's loose things to another
			state.layersBox = (Container) ThingDef.GetByDefname("i_backpack").Create(self.Bank);
			state.layersBox.Name = "Veci layer";
			state.packBox = (Container) ThingDef.GetByDefname("i_backpack").Create(self.Bank);
			state.packBox.Name = "Veci bagl";
			foreach (Item worn in WornItems(self)) {
				if (!StaysOnMystic(worn)) {
					Copy(worn, ego, true);
					worn.Cont = state.layersBox;
				}
			}
			foreach (Item loose in new List<Item>(PackItems(self))) {
				if (!loose.HasAttribute(ItemAttributes.attr_move_never) && (loose != ball)) {
					loose.Cont = state.packBox;
				}
			}

			//the mystic as was
			state.name = self.Name;
			state.title = self.Title;
			state.model = self.Model;
			state.color = self.Color;
			state.str = self.Str;
			state.dex = self.Dex;
			state.intel = self.Int;
			state.vit = self.Vit;
			state.hits = self.Hits;
			state.stam = self.Stam;
			state.mana = self.Mana;
			state.x = self.X;
			state.y = self.Y;
			state.z = self.Z;
			state.m = self.M;
			int count = AbstractSkillDef.SkillsCount;
			string[] skills = new string[count];
			for (int i = 0; i < count; i++) {
				skills[i] = self.GetSkill(i).ToString(CultureInfo.InvariantCulture);
				self.SetRealSkillValue(i, monster.GetSkill(i));
			}
			state.skills = string.Join(",", skills);
			self.SetRealSkillValue((int) SkillName.Magery, 0);

			//the mystic becomes the monster, which waits on plane 250
			self.Name = monster.Name;
			self.Title = "";
			self.Model = monster.Model;
			self.Color = monster.Color;
			self.Str = monster.Str;
			self.Dex = monster.Dex;
			self.Int = monster.Int;
			self.Vit = (short) monster.MaxHits; //max hits follow vitality here
			self.Hits = monster.Hits;
			self.Stam = (short) Math.Min(monster.Stam, self.MaxStam);
			self.Mana = (short) Math.Min(monster.Mana, self.MaxMana);
			state.monsterInvulnerable = monster.Flag_Invulnerable;
			self.P(monster.X, monster.Y, monster.Z, monster.M);
			if ((monster.Model == 0x190) || (monster.Model == 0x191)) {
				foreach (Item gear in WornItems(monster)) {
					Copy(gear, self, true);
				}
			}
			Archer(self, monster);
			monster.Flag_WarMode = false;
			monster.Flag_Invulnerable = true;
			ImmobilePlugin.Set(monster, true);
			monster.P(monster.X, monster.Y, monster.Z, (byte) ParkedPlane);

			if (ball != null) {
				ball.Color = HypnotizerBall.PossessingColor;
			}
			int koef = Koef(self.Level);
			int cas = 100 + (koef * 60);
			self.AddTimer(PossessionTimer.Key, state);
			state.DueInSeconds = Globals.dice.Next(cas - 100, cas + 61);
			self.InvalidateCombatWeaponValues();
			self.InvalidateCombatArmorValues();
			AlarmTimer alarm = new AlarmTimer();
			ego.AddTimer(AlarmTimer.Key, alarm);
			alarm.DueInSeconds = 1;
			alarm.PeriodInSeconds = 1;
		}

		/// <summary>strelec: throwers get rocks, archers a bow.</summary>
		private static void Archer(Player self, NPC monster) {
			string defname = monster.Def.PrettyDefname;
			if (throwers.Contains(defname)) {
				Give(self, "i_bow_rock", 1, true);
				Give(self, "i_rock_plain", 7, false);
			} else if (shooters.Contains(defname)) {
				Give(self, "i_bow_copper", 1, true);
				Give(self, "i_arrow_copper", 50, false);
			}
		}

		private static void Give(Player self, string defname, int amount, bool wear) {
			if (ThingDef.GetByDefname(defname) is ItemDef def) {
				Item item = (Item) def.Create(wear ? (Thing) self : self.Backpack);
				item.Amount = Math.Max(amount, 1);
				item.SetAttribute(ItemAttributes.attr_move_never, true);
			}
		}

		private static List<Item> WornItems(Character ch) {
			List<Item> worn = new List<Item>();
			foreach (AbstractItem item in ch) {
				if ((item is Item real) && IsEquipmentLayer(real.Z)) {
					worn.Add(real);
				}
			}
			return worn;
		}

		private static IEnumerable<Item> PackItems(Character ch) {
			foreach (AbstractItem item in ch.Backpack) {
				if (item is Item real) {
					yield return real;
				}
			}
		}

		/// <summary>hypnoswap_back. hitsDivisor 2 when the alter ego was hit or spelled, 5 when the mystic died in the
		/// body; egoDied when the alter ego died. Erebor brought the mystic back with 0 hits then; here with 1.</summary>
		internal static void SwapBack(Player self, int hitsDivisor, bool egoDied) {
			SwapBack(self, Possession(self), hitsDivisor, egoDied);
		}

		internal static void SwapBack(Player self, PossessionTimer state, int hitsDivisor, bool egoDied) {
			if (state == null) {
				self.SysMessage("Neznama chyba. Pokud ziskas pocit, ze je tvoje postava nejak pokazena, kontaktuj GM");
				return;
			}
			state.Delete();
			NPC monster = state.monster;
			bool paralyzed = ParalyzeEffectPluginDef.IsParalyzed(self);
			ParalyzeEffectPluginDef.Unparalyze(self);
			Poison.RemoveAll(self);
			BleedingEffectPluginDef.StopBleeding(self);
			Delay(self, 3);
			HypnotizerBall ball = FindBall(self);
			if (ball != null) {
				ball.Color = HypnotizerBall.NormalColor;
			}

			//the monster comes back where its body was, with what was left of it
			if ((monster != null) && !monster.IsDeleted) {
				monster.P(self.X, self.Y, self.Z, self.M);
				monster.Hits = (short) Math.Max(1, Math.Min((int) self.Hits, (int) monster.MaxHits));
				monster.Stam = self.Stam;
				monster.Mana = self.Mana;
				monster.Flag_Invulnerable = state.monsterInvulnerable;
				ImmobilePlugin.Set(monster, false);
				monster.Flag_WarMode = false;
				monster.ExperienceReward += Math.Max(state.gained, 0);
				if (paralyzed) { //Erebor's: forever
					ParalyzeEffectPluginDef.Paralyze(monster, egoParalysisPK, TimeSpan.FromDays(1));
				}
			}

			//the mystic as was
			if (self.FindLayer(LayerNames.Dragging) is Item held) {
				held.Cont = self.Backpack;
			}
			foreach (Item worn in WornItems(self)) {
				if (!StaysOnMystic(worn)) {
					worn.Delete();
				}
			}
			foreach (Item loot in new List<Item>(PackItems(self))) {
				if (!loot.HasAttribute(ItemAttributes.attr_move_never)) {
					if ((monster != null) && !monster.IsDeleted) {
						loot.Cont = monster.Backpack; //Erebor's vrat_mi_itemy: the loot stays with the monster
					}
				} else if (!(loot is HypnotizerBall)) {
					loot.Delete(); //arrows and rocks of the body
				}
			}
			self.Name = state.name;
			self.Title = state.title;
			self.Model = state.model;
			self.Color = state.color;
			self.Str = (short) state.str;
			self.Dex = (short) state.dex;
			self.Int = (short) state.intel;
			self.Vit = (short) state.vit;
			self.Stam = (short) Math.Min(self.MaxStam, (state.stam > 0) ? state.stam / 2 : 50);
			self.Mana = (short) Math.Min(self.MaxMana, (state.mana > 0) ? state.mana / 2 : 50);
			string[] skills = (state.skills ?? "").Split(',');
			for (int i = 0; i < skills.Length; i++) {
				if (int.TryParse(skills[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) {
					self.SetRealSkillValue(i, value);
				}
			}
			self.P(state.x, state.y, state.z, (byte) state.m);
			if (state.layersBox != null) {
				foreach (Item worn in new List<Item>(ContentsOf(state.layersBox))) {
					worn.Cont = self;
				}
				state.layersBox.Delete();
			}
			if (state.packBox != null) {
				foreach (Item loose in new List<Item>(ContentsOf(state.packBox))) {
					loose.Cont = self.Backpack;
				}
				state.packBox.Delete();
			}
			if ((state.ego != null) && !state.ego.IsDeleted) {
				state.ego.Delete();
			}
			self.Flag_WarMode = false;
			self.InvalidateCombatWeaponValues();
			self.InvalidateCombatArmorValues();
			LevelingSystem.AddExperience(self, state.gained);

			int hits;
			if (hitsDivisor > 0) {
				hits = (state.hits > 0) ? state.hits / hitsDivisor : 50;
				self.SysMessage("Citis se byt fyzicky velmi " + (self.IsFemale ? "oslabena" : "oslaben"));
			} else if (egoDied) {
				hits = 1;
			} else {
				hits = (state.hits > 0) ? state.hits : 50;
			}
			self.Hits = (short) Math.Max(1, Math.Min(hits, self.MaxHits));
		}

		private static IEnumerable<Item> ContentsOf(Container box) {
			foreach (AbstractItem item in box) {
				if (item is Item real) {
					yield return real;
				}
			}
		}

		/// <summary>The ring of alarm pads round the alter ego: a monster crossing it, 5 tiles out, goes for the body.</summary>
		internal static void Alarm(NPC ego) {
			foreach (AbstractCharacter near in ego.GetMap().GetCharsInRange(ego.X, ego.Y, 5)) {
				if ((near is NPC npc) && (npc != ego) && (npc.ControlMaster == null) && !npc.Flag_Dead &&
						(Math.Max(Math.Abs(npc.X - ego.X), Math.Abs(npc.Y - ego.Y)) == 5) && (npc.Karma < 200)) {
					NPCAiTimer.DefendAgainst(npc, ego);
				}
			}
		}

		/// <summary>e_hypnotising_mystic and e_monster: no telepads, no potions or bandages, no looting unless the body
		/// may, and spellbooks or scrolls never.</summary>
		internal static bool RefusesTelepad(Character ch) {
			if (IsPossessing(ch)) {
				ch.SysMessage("V cizim tele nelze prochazet teleporty");
				return true;
			}
			return false;
		}

		internal static bool RefusesUse(Character ch, Item item) {
			if (!IsPossessing(ch)) {
				return false;
			}
			if (item is Potion) {
				ch.SysMessage("Nemas poneti k cemu tato lahvicka slouzi");
				return true;
			}
			if ((item.Type != null) && (item.Type.Defname == "t_bandage")) {
				ch.SysMessage("O pouziti hadriku nevis nic");
				return true;
			}
			return false;
		}

		internal static string RefusesPickup(Character ch, Item item) {
			PossessionTimer state = Possession(ch);
			if ((state == null) || (item.TopObj() == ch)) {
				return null;
			}
			if ((state.monster == null) || ((MysticMind.Flags(state.monster) & MysticMind.FlagLoot) == 0)) {
				return "V tomto tele nejsi " + (ch.IsFemale ? "schopna" : "schopen") + " lootovani";
			}
			if ((item is SpellBook) || ((item.Type != null) && ((item.Type.Defname == "t_scroll") || (item.Type.Defname == "t_customspell_scroll")))) {
				return "Citis ze tuto vec opravdu nebudes potrebovat";
			}
			return null;
		}

		/// <summary>Chat, .explevel and the like refuse a possessing mystic.</summary>
		[SteamFunction]
		public static bool HypnoAllows(Character self) {
			if (IsPossessing(self)) {
				self.SysMessage("V tuto chvili tento prikaz nelze pouzit.");
				return false;
			}
			return true;
		}

		/// <summary>e_allplayers' @Logout.</summary>
		internal static void OnLogout(Player self) {
			if (IsPossessing(self)) {
				SwapBack(self, 0, false);
			}
		}
	}

	public class Targ_Hypnosis : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			MysticHypnosis.Targeted(self, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.RedMessage("Hypnotizovat veci? To nepujde ...");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.RedMessage("Tady nic k hypnotizovani neni!");
			return TargetResult.Done;
		}
	}

	/// <summary>i_hypno_delka, and everything needed to bring the mystic back.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PossessionTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_hypnoPossession_");

		[SaveableData, CopyableData]
		public NPC monster, ego;

		[SaveableData, CopyableData]
		public Container layersBox, packBox;

		[SaveableData, CopyableData]
		public string name, title, skills;

		[SaveableData, CopyableData]
		public int model, color, str, dex, intel, vit, hits, stam, mana, x, y, z, m, gained;

		[SaveableData, CopyableData]
		public bool monsterInvulnerable;

		[DeepCopyImplementation, LoadingInitializer]
		public PossessionTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Player self) {
				self.SysMessage("Spojeni s monstrem te jiz prilis vycerpalo");
				MysticHypnosis.SwapBack(self, this, 0, false);
			}
		}
	}

	/// <summary>The alarm pads' ring, looked at every second.</summary>
	[SaveableClass, DeepCopyableClass]
	public class AlarmTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_hypnoAlarm_");

		[DeepCopyImplementation, LoadingInitializer]
		public AlarmTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is NPC ego) {
				MysticHypnosis.Alarm(ego);
			}
		}
	}
}
