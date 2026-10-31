using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#927 - server-side helpers for Tests/Protocol/PotionsTests.cs. Every potion is drunk the
	/// way a player's double click does it.</summary>
	public static class PotionsTests {
		private static int fails;

		private static void Check(string name, bool ok, string detail = "") {
			if (!ok) {
				fails++;
				Console.WriteLine("POTIONS_FAIL: " + name + " " + detail);
			}
		}

		private static void Ready(Character ch) {
			ch.DeletePlugin(PotionEffectPlugin.MainSlot);
			ch.DeletePlugin(PotionEffectPlugin.NightSightSlot);
			ch.DeletePlugin(PotionEffectPlugin.SkinSlot);
			PotionDrinking.Wait(ch, -1);
		}

		private static Potion Give(Character ch, string defname, int amount = 1) {
			Potion potion = (Potion) ThingDef.GetByDefname(defname).Create(ch.Backpack);
			if (amount > 1) {
				potion.Amount = amount;
			}
			return potion;
		}

		private static void Drink(Character ch, string defname) {
			PotionDrinking.ForgetAttempts(ch);
			PotionDrinking.Wait(ch, -1);
			Give(ch, defname).On_DClick(ch);
		}

		private static string Names(Character ch) {
			return string.Join(",", ActiveEffects.Of(ch).ConvertAll(effect => effect.Name));
		}

		private static ActiveEffect Effect(Character ch, string name) {
			return ActiveEffects.Of(ch).Find(effect => effect.Name == name);
		}

		private static void StatPotion(Character ch, string defname, Func<int> stat, int min, int max, string name) {
			int before = stat();
			Drink(ch, defname);
			int raised = stat() - before;
			Check(defname + " raises", (raised >= min) && (raised <= max), raised.ToString());
			ActiveEffect shown = Effect(ch, name);
			Check(defname + " shows on .boosts", (shown != null) && (shown.Section == EffectSection.Positive) && shown.Source.Contains("lektvar"), (shown != null) ? shown.Source : "none");
			ch.DeletePlugin(PotionEffectPlugin.MainSlot);
			Check(defname + " ends", stat() == before, stat() + " vs " + before);
		}

		[SteamFunction]
		public static void TestPotionsRules(Character self) {
			Player player = (Player) self;
			fails = 0;
			self.Account.PLevel = 1;
			PotionSettings settings = PotionSettings.instance;
			settings.bottleBreakChance = 0;
			Ready(self);

			//every potion in the game does something
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				if ((script is PotionDef def) && !(script is PoisonPotionDef) && def.PrettyDefname.StartsWith("i_potion_", StringComparison.OrdinalIgnoreCase)) {
					Potion potion = null;
					try {
						potion = (Potion) def.Create(self.Backpack);
						Check("no kind for " + def.PrettyDefname, PotionKinds.Find(potion) != null);
					} catch (Exception e) {
						Check("can't create " + def.PrettyDefname, false, e.Message);
					}
					potion?.Delete();
				}
			}

			StatPotion(self, "i_potion_AgilityLess", () => self.Dex, 12, 20, "Agility");
			StatPotion(self, "i_potion_AgilityElixir", () => self.Dex, 30, 30, "Agility");
			StatPotion(self, "i_potion_Strength", () => self.Str, 14, 20, "Strength");
			StatPotion(self, "i_potion_CleverGreat", () => self.Int, 16, 20, "Cleverness");
			StatPotion(self, "i_potion_EnergyElixir", () => player.Vit, 30, 30, "Energy");
			StatPotion(self, "i_potion_Shield", () => self.GetSkill(SkillName.MagicResist), 280, 280, "Spell Shield");
			StatPotion(self, "i_potion_mobility", () => self.ResistParalyse, 1000, 1000, "Mobility");

			//one potion slot
			int str = self.Str;
			Drink(self, "i_potion_StrengthElixir");
			Drink(self, "i_potion_AgilityElixir");
			Check("the second potion took the slot", (self.Str == str) && (Effect(self, "Agility") != null) && (Effect(self, "Strength") == null), ActiveEffects.Of(self).Count.ToString());
			Drink(self, "i_potion_AgilityElixir");
			Check("the same one doesn't add up", ActiveEffects.Of(self).FindAll(effect => effect.Name == "Agility").Count == 1);

			//night sight and skin have their own
			int armor = self.ArmorClassModifier;
			Drink(self, "i_potion_Nightsight");
			Drink(self, "i_potion_Stoneskin");
			Check("nightsight beside it", NightsightPotions.HasNightSight(self) && (Effect(self, "Agility") != null));
			Check("nightsight is bright", LightAndWeather.GetLightAt(self) == 0, LightAndWeather.GetLightAt(self).ToString());
			Check("stoneskin armor", self.ArmorClassModifier == (armor + 5), self.ArmorClassModifier.ToString());
			Drink(self, "i_potion_Stoneskinelixir");
			Check("steelskin replaces it", (self.ArmorClassModifier >= (armor + 7)) && (self.ArmorClassModifier <= (armor + 8)) && (Effect(self, "Steelskin") != null), self.ArmorClassModifier.ToString());

			//invisibility takes the slot too
			Drink(self, "i_potion_Invisibility");
			Check("invisible", self.Flag_InvisByMagic && (InvisibilitySpellDef.SecondsLeft(self) > 200), InvisibilitySpellDef.SecondsLeft(self).ToString());
			Check("the slot's empty", Effect(self, "Agility") == null);
			self.Flag_InvisByMagic = false;
			Ready(self);

			//the spell potions
			Drink(self, "i_potion_reflection");
			Check("reflection", Effect(self, SpellDef.GetById(36).Name) != null, Names(self));
			Drink(self, "i_potion_reactive");
			Check("reactive", Effect(self, SpellDef.GetById(7).Name) != null);

			//heal and refresh
			self.Hits = 1;
			Drink(self, "i_potion_HealLess");
			Check("lesser heal", self.Hits == 31, self.Hits.ToString());
			self.Hits = 1;
			Drink(self, "i_potion_HealElixir");
			Check("elixir", self.Hits == self.MaxHits, self.Hits.ToString());
			self.Stam = 0;
			Drink(self, "i_potion_RefreshLess");
			Check("refresh", self.Stam == ((self.MaxStam * 200) / 1000), self.Stam + " of " + self.MaxStam);

			//dragon resist counts, and stops counting
			Ready(self);
			Drink(self, "i_potion_dragon_resist");
			Check("dragon resist", PotionEffectPlugin.DragonResist(self) == 10, PotionEffectPlugin.DragonResist(self).ToString());
			Ready(self);
			Check("dragon resist ended", PotionEffectPlugin.DragonResist(self) == 0);

			//perception adds up
			HpSight.Add(self, 100);
			double left = HpSight.SecondsLeft(self);
			Drink(self, "i_potion_HPsightLess");
			Check("perception adds up", (HpSight.SecondsLeft(self) > (left + 290)) && (Effect(self, "Vnimani") != null), HpSight.SecondsLeft(self).ToString());

			//the wait, the bottle
			Ready(self);
			PotionDrinking.ForgetAttempts(self);
			Potion heals = Give(self, "i_potion_Heal", 3);
			heals.On_DClick(self);
			Check("one's gone", heals.Amount == 2, heals.Amount.ToString());
			double wait = PotionDrinking.NextPotion(self) - Globals.TimeInSeconds;
			Check("a long wait", (wait > 18) && (wait <= 20), wait.ToString());
			Check("the wait shows", Effect(self, "Dalsi lektvar") != null);
			heals.On_DClick(self);
			Check("too soon: nothing", heals.Amount == 2, heals.Amount.ToString());
			heals.On_DClick(self);
			double penalised = PotionDrinking.NextPotion(self) - Globals.TimeInSeconds;
			Check("hammering costs", penalised > 22, penalised.ToString());
			heals.Delete();
			PotionDrinking.Wait(self, -1);
			Potion agility = Give(self, "i_potion_AgilityLess");
			int bottles = VarGate.CountOf(self, "i_bottle_empty", false);
			agility.On_DClick(self);
			wait = PotionDrinking.NextPotion(self) - Globals.TimeInSeconds;
			Check("a short wait", (wait > 3) && (wait <= 5), wait.ToString());
			Check("an empty bottle", VarGate.CountOf(self, "i_bottle_empty", false) == (bottles + 1));
			settings.bottleBreakChance = 100;
			Drink(self, "i_potion_AgilityLess");
			Check("a broken bottle", VarGate.CountOf(self, "i_bottle_empty", false) == (bottles + 1));
			settings.bottleBreakChance = 0;
			Ready(self);

			//the macro takes the best
			Potion less = Give(self, "i_potion_HealLess");
			Potion great = Give(self, "i_potion_HealGreat");
			PotionCommands.PotionHeal(self);
			Check("the macro drinks the best", great.IsDeleted && !less.IsDeleted);
			less.Delete();

			//bombs: armor off, divided further out
			Check("bomb next to one", BombPotionKind.DamageAt(80, 20, 1) == 60);
			Check("bomb further out", BombPotionKind.DamageAt(80, 20, 3) == 20);
			Check("armor stops it", BombPotionKind.DamageAt(40, 50, 1) == 0);

			Console.WriteLine("POTIONS_RULES: fails=" + fails + " end");
		}

		private static Character dummy;

		/// <summary>A bomb on the ground beside a dummy, going off now. Nobody's a criminal for it.</summary>
		[SteamFunction]
		public static void TestPotionsBomb(Character self) {
			fails = 0;
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			self.Realm = Realm.Gondor;
			dummy = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.Hits = 200;
			Potion bomb = (Potion) ThingDef.GetByDefname("i_potion_Lava").Create(self.X + 2, self.Y, self.Z, self.M);
			((BombPotionKind) PotionKinds.Find(bomb)).Explode(bomb);
			Check("the bomb's gone", bomb.IsDeleted);
			Check("it hurt", dummy.IsDeleted || (dummy.Hits < 200), dummy.IsDeleted ? "" : dummy.Hits.ToString());
			Check("no crime", !Notoriety.IsCriminal(self));
			if (!dummy.IsDeleted) {
				dummy.Delete();
			}
			Console.WriteLine("POTIONS_BOMB: fails=" + fails + " end");
		}

		/// <summary>A dispel bomb takes what spells put on those around.</summary>
		[SteamFunction]
		public static void TestPotionsDispelBomb(Character self) {
			fails = 0;
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			SpellDef.GetById(17).EffectChar(self, self, EffectFlag.FromSpellBook);
			Check("blessed", Effect(self, SpellDef.GetById(17).Name) != null);
			PotionDrinking.Wait(self, -1);
			Drink(self, "i_potion_StrengthLess");
			Potion bomb = (Potion) ThingDef.GetByDefname("i_potion_ExplosionDispell").Create(self.X, self.Y, self.Z, self.M);
			((BombPotionKind) PotionKinds.Find(bomb)).Explode(bomb);
			Check("the spell's gone", Effect(self, SpellDef.GetById(17).Name) == null, ActiveEffects.Of(self).Count.ToString());
			Check("a potion isn't dispelled", Effect(self, "Strength") != null);
			Console.WriteLine("POTIONS_DISPEL: fails=" + fails + " end");
		}

		[SteamFunction]
		public static void TestPotionsGiveBomb(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			self.Account.PLevel = 1;
			self.Hits = 100;
			PotionDrinking.Wait(self, -1);
			AbilityDef throwing = (AbilityDef) AbilityDef.GetByDefname("a_throwing");
			self.SetRealAbilityPoints(throwing, 10);
			Potion bomb = Give(self, "i_potion_ExplosionLess", 2);
			Console.WriteLine("POTIONS_GIVEN: bomb=" + (uint) bomb.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " x=" + (self.X + 3) + " y=" + self.Y + " z=" + self.Z + " end");
		}

		[SteamFunction]
		public static void TestPotionsThrown(Character self) {
			int fuses = 0, bombs = 0;
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 8)) {
				if (item is Potion potion) {
					bombs++;
					fuses += potion.HasTimer(BombFuseTimer.Key) ? 1 : 0;
				}
			}
			Console.WriteLine("POTIONS_THROWN: left=" + VarGate.CountOf(self, "i_potion_ExplosionLess", false) + " onGround=" + bombs + " armed=" + fuses + " hits=" + self.Hits + " end");
		}

		/// <summary>Perception: hitting something shows its hitpoints to who drank it.</summary>
		[SteamFunction]
		public static void TestPotionsSight(Character self) {
			self.Account.PLevel = 1;
			Drink(self, "i_potion_HPsight");
			Character target = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			target.Hits = 200;
			DamageManager.CauseDamage(null, target, DamageType.Irresistable, 20);
			target.Delete();
			Console.WriteLine("POTIONS_SIGHT: done");
		}

		private static NPC pet;

		[SteamFunction]
		public static void TestPotionsShrinkStart(Character self) {
			self.Account.PLevel = 1;
			pet = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.P());
			pet.ControlMaster = self;
			pet.Name = "Micinka";
			Potion shrink = Give(self, "i_potion_Shrink");
			Console.WriteLine("POTIONS_SHRINK: potion=" + (uint) shrink.FlaggedUid + " pet=" + (uint) pet.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestPotionsFigurine(Character self) {
			PetFigurine figurine = (PetFigurine) VarGate.Carried(self).Find(item => item is PetFigurine);
			Console.WriteLine("POTIONS_FIGURINE: petGone=" + pet.IsDeleted + " figurine=" + ((figurine != null) ? (uint) figurine.FlaggedUid : 0) + " named=" + ((figurine != null) ? figurine.Name : "") + " end");
		}

		[SteamFunction]
		public static void TestPotionsPetBack(Character self) {
			Character found = null;
			foreach (AbstractCharacter around in self.GetMap().GetCharsInRange(self.X, self.Y, 2)) {
				if (around is NPC npc && npc.IsPetOf(self)) {
					found = npc;
				}
			}
			Console.WriteLine("POTIONS_PETBACK: found=" + (found != null) + " name=" + ((found != null) ? found.Name : "") + " end");
			found?.Delete();
		}

		private static PotionKeg keg;

		[SteamFunction]
		public static void TestPotionsKeg(Character self) {
			self.Account.PLevel = 1;
			keg = (PotionKeg) ThingDef.GetByDefname("i_keg_potion").Create(self.X, self.Y, self.Z, self.M);
			Potion heals = Give(self, "i_potion_Heal", 5);
			Item bottles = (Item) ThingDef.GetByDefname("i_bottle_empty").Create(self.Backpack);
			bottles.Amount = 3;
			Potion other = Give(self, "i_potion_Refresh", 1);
			Console.WriteLine("POTIONS_KEG: keg=" + (uint) keg.FlaggedUid + " heals=" + (uint) heals.FlaggedUid + " other=" + (uint) other.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestPotionsKegBottles(Character self) {
			Item bottles = VarGate.Carried(self).Find(item => VarGate.Matches(item, "i_bottle_empty", false));
			Console.WriteLine("POTIONS_BOTTLES: uid=" + (uint) bottles.FlaggedUid + " count=" + bottles.Amount + " end");
		}

		[SteamFunction]
		public static void TestPotionsKegReport(Character self) {
			Console.WriteLine("POTIONS_KEGSTATE: count=" + keg.potionsCount + " heals=" + VarGate.CountOf(self, "i_potion_Heal", false) + " bottles=" + VarGate.CountOf(self, "i_bottle_empty", false) + " end");
		}

		/// <summary>Everything a save has to hold, running.</summary>
		[SteamFunction]
		public static void TestPotionsForSave(Character self) {
			self.Account.PLevel = 1;
			Ready(self);
			Drink(self, "i_potion_Stoneskin");
			Drink(self, "i_potion_StrengthElixir");
			Drink(self, "i_potion_Nightsight");
			Drink(self, "i_potion_HPsight");
			Potion bomb = (Potion) ThingDef.GetByDefname("i_potion_Explosion").Create(self.X + 3, self.Y, self.Z, self.M);
			bomb.SetAttribute(ItemAttributes.attr_move_never, true);
			bomb.AddTimer(BombFuseTimer.Key, new BombFuseTimer { count = 3 }).DueInSeconds = 1000;
			PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(self.Backpack);
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.P());
			figurine.Keep(rabbit);
			rabbit.Delete();
			PotionKeg saved = (PotionKeg) ThingDef.GetByDefname("i_keg_potion").Create(self.X, self.Y, self.Z, self.M);
			saved.potionDef = (PotionDef) ThingDef.GetByDefname("i_potion_Heal");
			saved.potionsCount = 7;
			Console.WriteLine("POTIONS_SAVED: done");
		}

		[SteamFunction]
		public static void TestPotionsAfterRestart(Character self) {
			int fuses = 0, kegs = 0, figurines = 0, effects = 0;
			foreach (Thing thing in Thing.AllThings) {
				if (thing is Potion potion) {
					fuses += potion.HasTimer(BombFuseTimer.Key) ? 1 : 0;
				} else if ((thing is PotionKeg k) && (k.potionsCount == 7) && (k.potionDef != null)) {
					kegs++;
				} else if ((thing is PetFigurine f) && (f.petDef != null)) {
					figurines++;
				} else if (thing is Character ch) {
					foreach (Plugin plugin in ch.GetAllPlugins()) {
						effects += (plugin is PotionEffectPlugin) ? 1 : 0;
					}
				}
			}
			Console.WriteLine("POTIONS_LOADED: fuses=" + fuses + " kegs=" + kegs + " figurines=" + figurines + " effects=" + effects + " end");
		}
	}
}
