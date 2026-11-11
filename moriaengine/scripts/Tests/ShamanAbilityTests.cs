using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - the shaman: rituals, sacrifice, weakening, concentration, voodoo shield, pacts, the life pact.
	/// The pacts' partner is a second client, "Seer".</summary>
	public static class ShamanAbilityTests {
		[SteamFunction]
		public static void TestShamanPartner(Character self) {
			Player partner = (Player) self;
			partner.Account.PLevel = 1;
			partner.Realm = Realm.Gondor;
			partner.Hits = partner.MaxHits;
			Console.WriteLine("SHAMAN_PARTNER_READY");
		}

		/// <summary>#1445 - a frozen orc's real blow on the blood pact's bearer: a probe makes it 40 at @getHit, and the
		/// bearer's @afterGetHit moves a share of the damage to the shaman.</summary>
		private static void TestBloodInASwing(Player shaman, Player bearer) {
			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(bearer.X + 1, bearer.Y, bearer.Z, bearer.M);
			NPCAiTimer.Freeze(orc, 300);
			bearer.AddTriggerGroup(SingletonScript<E_BlowOf40>.Instance);
			try {
				bool shared = false;
				int shamanLoss = 0, bearerLoss = 0;
				for (int i = 0; (i < 50) && !shared; i++) {
					bearer.Hits = bearer.MaxHits;
					shaman.Hits = shaman.MaxHits;
					DamageManager.ProcessSwing(orc, bearer);
					shamanLoss = shaman.MaxHits - shaman.Hits;
					bearerLoss = bearer.MaxHits - bearer.Hits;
					shared = (shamanLoss > 0) && (bearerLoss > 0);
				}
				//the blow dealt bearerLoss + shamanLoss in all, and the shaman took 15% of it once
				Report("aMonstersRealBlowIsSharedThroughTheTrigger", shared &&
					(shamanLoss == Math.Min(((bearerLoss + shamanLoss) * ClassAbilitySettings.instance.bloodPactPercent) / 100, 20)));
			} finally {
				bearer.RemoveTriggerGroup(SingletonScript<E_BlowOf40>.Instance);
				orc.Delete();
				bearer.Hits = bearer.MaxHits;
				shaman.Hits = shaman.MaxHits;
			}
		}

		[SteamFunction]
		public static void TestShamanAbilities(Character self) {
			Player player = (Player) self;
			Player partner = FindPlayer("Seer");
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Shaman;
			player.Realm = Realm.Gondor;
			player.Level = 100;
			player.Vit = 100;
			player.Int = 100;
			ClassAbilities.Reset(player);
			ClassAbilities.OnDeath(player);
			ShamanPacts.OnGone(player); //#1445 - a pact left by an aborted run
			ShamanPacts.OnGone(partner);
			ClassAbilities.SetPoints(player, "woodoo", 10);
			Give(player, ShamanAbilities.RubyDef, 10);
			Give(player, ShamanAbilities.CitrineDef, 1);

			ShamanAbilities.VoodooInt(player);
			Report("bloodToMind", (player.Int == 130) && (player.Vit == 70) && (Count(player, ShamanAbilities.RubyDef) == 9));
			ShamanAbilities.VoodooVit(player);
			Report("mindToBloodTurnsItTwice", (player.Int == 70) && (player.Vit == 130));
			ShamanAbilities.VoodooRovnovaha(player);
			Report("balanceComesBack", (player.Int == 100) && (player.Vit == 100) && (ShamanAbilities.Ritual(player) == 0));
			Give(player, ShamanAbilities.RubyDef, 1); //#1442 - one more ritual; the pack is newest first, so this ruby goes and the counts below stay
			ShamanAbilities.VoodooInt(player);
			Report("theRitualIsAPluginOnTheCharacter", (VoodooStatsPlugin.Of(player) != null) && (VoodooStatsPlugin.Of(player).ritual == 1));
			player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //@death alone, nobody dies
			Report("deathEndsTheRitualThroughItsPlugin", (player.Int == 100) && (player.Vit == 100) && (VoodooStatsPlugin.Of(player) == null));

			player.Hits = 90;
			player.Mana = 0;
			ShamanAbilities.VoodooObet(player);
			Report("sacrificeTradesHitsForMana", (player.Hits == 50) && (player.Mana == 40));

			ClassAbilities.SetPoints(player, "voodooShield", 4);
			player.Mana = 100;
			ShamanAbilities.VoodooShield(player);
			NPC monster = ClassAbilityTests.Dummy(self, 2);
			Report("theShieldCostsACitrine", ShamanAbilities.HasShieldUp(player) && (player.Mana == 71) && (Count(player, ShamanAbilities.CitrineDef) == 0));
			Report("theShieldIgnoresPlayers", ShamanAbilities.AbsorbSpell(partner, player, 15) == 15);
			Report("theShieldTakesAMonstersSpell", ShamanAbilities.AbsorbSpell(monster, player, 15) == 0);
			Report("theRestGetsThrough", (ShamanAbilities.AbsorbSpell(monster, player, 15) == 10) && !ShamanAbilities.HasShieldUp(player));
			VoodooShieldPlugin shield = VoodooShieldPlugin.Of(player); //#1442
			Report("theShieldRestsAsAPlugin", (shield != null) && !shield.IsUp && (shield.Timer > 170));
			if (shield != null) {
				shield.On_Timer(); //the rest's end
			}
			Report("theShieldComesBackAfterItsRest", VoodooShieldPlugin.Of(player) == null);
			short manaBefore = player.Mana;
			Give(player, ShamanAbilities.CitrineDef, 1);
			player.Mana = 100;
			ShamanAbilities.VoodooShield(player); //a new one, whose 30 s run out
			VoodooShieldPlugin second = VoodooShieldPlugin.Of(player);
			if (second != null) {
				second.On_Timer();
			}
			Report("theShieldExpiresIntoItsRest", (second != null) && (VoodooShieldPlugin.Of(player) == second) && !second.IsUp && (second.Timer > 170));
			player.DeletePlugin(VoodooShieldPlugin.Key);
			player.Mana = manaBefore;

			ClassAbilities.SetPoints(player, "woodoo_curse", 10);
			int procs = 0;
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");
			for (int i = 0; i < 400; i++) {
				player.Stam = 100;
				if (ShamanAbilities.Concentrate(player, monster, fireball, 100) == 130) {
					procs++;
				}
			}
			player.Stam = 100;
			Report("concentrationIsSometimesAThirdStronger", (procs > 40) && (procs < 170));
			Report("noConcentrationOnPlayers", ShamanAbilities.Concentrate(player, partner, fireball, 100) == 100);
			Report("noConcentrationForFields", ShamanAbilities.Concentrate(player, monster, SpellDef.GetById(28), 100) == 100);

			ShamanAbilities.Weaken(player, monster);
			Report("weakeningTakesARuby", monster.HasTimer(WeakenTimer.Key) && (Count(player, ShamanAbilities.RubyDef) == 7));

			ClassAbilities.SetPoints(player, "woodoo_pacts", 5);
			player.Hits = player.MaxHits;
			int shamanHits = player.Hits;
			ShamanPacts.Give(player, partner, "blood");
			Report("aBloodPactCostsBlood", (player.Hits == shamanHits - 29) && (PactPlugin.Of(partner) != null) && (PactPlugin.Of(partner).giver == player) &&
				(PactPlugin.Of(partner).TimerObject != null) && (PactPlugin.Of(player) != null) && (PactPlugin.Of(player).receiver == partner)); //#1445
			Report("thePactCooldownIsThePlugins", (ShamanProfessionPlugin.Of(player) != null) && (ShamanProfessionPlugin.Of(player).nextPact > Globals.TimeInSeconds)); //#1442
			TestBloodInASwing(player, partner); //#1445
			partner.Hits = 50;
			shamanHits = player.Hits;
			DamageManager.Trigger_AfterHit(DamageManager.GetWeaponSwingArgs(monster, partner), 100); //#1445 - as ProcessSwing raises it
			Report("aMonstersBlowIsShared", (partner.Hits == 65) && (player.Hits == shamanHits - 15));
			Report("theGiverEarnsThroughTheBearer", ShamanPacts.EarnsNoKillExp(player) && !ShamanPacts.EarnsNoKillExp(partner));
			DamageManager.Trigger_AfterHit(DamageManager.GetWeaponSwingArgs(player, partner), 100);
			partner.Hits = 50;
			DamageManager.Trigger_AfterHit(DamageManager.GetWeaponSwingArgs(monster, partner), 100);
			Report("aPlayersBlowSwitchesItOff", partner.Hits == 50);
			ShamanPacts.CancelPact(player);
			Report("cancellingEndsBothSides", (PactPlugin.Of(player) == null) && (PactPlugin.Of(partner) == null));

			ShamanPacts.Give(player, player, "mana");
			PactPlugin own = PactPlugin.Of(player);
			Report("aManaPactMayBeOwn", (Count(player, ShamanAbilities.RubyDef) == 6) && (own != null) && (own.givenType == "mana") && (own.borneType == "mana") &&
				(own.TimerObject != null));
			player.Mana = 0;
			player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //#1445 - @death alone, nobody dies
			Report("deathEndsThePactThroughItsPlugin", PactPlugin.Of(player) == null);

			Item soulCase = Give(player, "i_soul_case", 1);
			Report("theCaseIsAShamansThing", soulCase is ShamanItem);
			ClassAbilities.SetPoints(player, "woodoo_lifepact", 5);
			player.Stam = 100;
			player.Mana = 100;
			ShamanPacts.GiveLife(player, partner);
			Report("aLifePactGivesASoul", (ShamanAbilities.FindInPack(partner, "i_soul") != null) && soulCase.IsDeleted && (player.Stam == 45) && (player.Mana == 35));
			LifePactPlugin lifePact = LifePactPlugin.Of(partner); //#1447 - i_soul_remover, 1200 s + 60 s a point
			Report("theLifePactIsAPluginOnTheReceiver", (lifePact != null) && (lifePact.giver == player) && (lifePact.Timer > 1490) && (lifePact.Timer <= 1500));

			Report("resistsRead", (ShamanPacts.ResistText(-500, "ohnem", null) == "je smrtelne nachylny na utok ohnem.") && (ShamanPacts.ResistText(999, "ohnem", null) == "je prakticky nezranitelny ohnem."));
			Item bottle = Give(player, "i_empathy_bottle", 1);
			ShamanPacts.DrinkEmpathy(player, (ShamanItem) bottle);
			EmpathyPlugin empathy = EmpathyPlugin.Of(player); //#1447 - i_getphysresists
			Report("theElixirLetsResistsBeSeen", (empathy != null) && (empathy.Timer > 7) && (empathy.Timer <= 8) && bottle.IsDeleted);

			monster.Delete();
			partner.Hits = 0;
			partner.CauseDeath(null);
			if (LifePactPlugin.Of(partner) is LifePactPlugin soul) {
				soul.createdAt -= 60;
			}
			Report("theSoulStaysThroughDeath", partner.Flag_Dead && (ShamanAbilities.FindInPack(partner, "i_soul") != null));
			Report("theLifePactStaysThroughDeath", LifePactPlugin.Of(partner) != null);
			ShamanPacts.LifePact(partner);
			Report("tooRestlessRightAfterDying", partner.Flag_Dead);
			Console.WriteLine("SHAMAN_PARTNER_DIED");
		}

		[SteamFunction]
		public static void TestShamanRise(Character self) {
			Player partner = FindPlayer("Seer");
			ShamanPacts.LifePact(partner);
			Report("theSoulBringsItBack", !partner.Flag_Dead && (ShamanAbilities.FindInPack(partner, "i_soul") == null));
			Report("backWithATenth", partner.Hits == Math.Max(1, partner.MaxHits / 10));
			Report("risingEndsTheLifePact", LifePactPlugin.Of(partner) == null);

			//#1070 - carving and the voodoo
			Player player = (Player) self;
			Corpse body = null;
			foreach (Thing thing in partner.GetMap().GetThingsInRange(partner.X, partner.Y, 3)) {
				if ((thing is Corpse found) && (found.Owner == partner)) {
					body = found;
				}
			}
			Item head = null;
			if (body != null) {
				CorpseCarving.Carve(player, body);
				foreach (Thing thing in body.GetMap().GetThingsInRange(body.X, body.Y, 1)) {
					if ((thing is Item part) && CorpseCarving.IsHead(part)) {
						head = part;
					}
				}
			}
			Report("aPlayersBodyFallsApart", (body != null) && body.HasTag(CorpseCarving.CarvedTK) && (head != null) &&
				(CorpseCarving.HeadOf(head) == partner) && (head.Name == partner.Name));
			Item dagger = (Item) ((ItemDef) ThingDef.GetByDefname("i_dagger_copper")).Create(player.Backpack);
			Report("daggersCurseByMetal", (ShamanVoodoo.DaggerTier(dagger) == 1) && (ShamanVoodoo.DaggerTier(((ItemDef) ThingDef.GetByDefname("i_dagger")).Create(player.Backpack) as Item) == 0));
			ClassAbilities.SetPoints(player, "woodoo_curse", 40);
			ShamanProfessionPlugin shaman = ShamanProfessionPlugin.Of(player); //#1442
			Report("theShamansPluginIsItsProfessionPlugin", shaman != null);
			if (shaman != null) {
				shaman.nextVoodoo = 0;
			}
			if (head != null) {
				ShamanVoodoo.Curse(player, head, ShamanVoodoo.Damage);
			}
			partner.Hits = 100;
			ShamanVoodoo.Arrive(partner, player, ShamanVoodoo.Damage);
			Report("aCurseHurtsAFifth", ShamanVoodoo.Has(partner, ShamanVoodoo.Damage) && (partner.Hits == 80));
			Report("theCurseCooldownIsThePlugins", (shaman != null) && (shaman.nextVoodoo > Globals.TimeInSeconds)); //#1442
			ShamanVoodoo.Arrive(partner, player, ShamanVoodoo.Shock);
			Report("aSecondCurseFallsBack", ShamanVoodoo.Has(player, ShamanVoodoo.Shock) && ShamanVoodoo.RefusesCast(player, SpellDef.GetByDefname("s_heal") as SpellDef));
			VoodooEffectTimer.Apply(player, ShamanVoodoo.NoPort, null);
			Report("noPortRefusesRecall", ShamanVoodoo.RefusesCast(player, SpellDef.GetByDefname("s_recall") as SpellDef) && ShamanVoodoo.RefusesRide(player));
			player.DeleteTimer(VoodooEffectTimer.Key);
			partner.DeleteTimer(VoodooEffectTimer.Key);
			dagger.Delete();

			NPC crow = (NPC) ((NPCDef) ThingDef.GetByDefname("C_BIRD_CROW")).Create(player.X + 1, player.Y, player.Z, player.M);
			crow.CauseDeath(null);
			Corpse crowBody = null;
			foreach (Thing thing in player.GetMap().GetThingsInRange(player.X + 1, player.Y, 1)) {
				if ((thing is Corpse found) && (found.CharDef == ThingDef.GetByDefname("C_BIRD_CROW"))) {
					crowBody = found;
				}
			}
			int feathersBefore = CountAround(player, "i_feather");
			if (crowBody != null) {
				CorpseCarving.Carve(player, crowBody);
			}
			Report("aMonstersBodyGivesItsResources", (crowBody != null) && (CountAround(player, "i_feather") == feathersBefore + 10));
			if (crowBody != null) {
				int model = player.Model;
				AstralMorph.Morph(player, crowBody);
				bool morphed = AstralMorph.IsMorphed(player) && (player.Model != model);
				AstralMorph.Dismorph(player);
				Report("theAstralMorphTakesTheBody", morphed && !AstralMorph.IsMorphed(player) && (player.Model == model));
			}
			Console.WriteLine("SHAMAN_DONE");
		}

		/// <summary>#1447 - the elixir reads a monster through the shaman's own @swingLanding (the client hears the lines and
		/// then the elixir's end); the partner gets a life pact whose soul ends in a second, and a ghost in an arena (instant
		/// resurrection) rises from it at once.</summary>
		[SteamFunction]
		public static void TestShamanEmpathy(Character self) {
			Player player = (Player) self;
			Player partner = FindPlayer("Seer");
			player.SetRealSkillValue((int) SkillName.ArmsLore, 1000);
			EmpathyPlugin old = EmpathyPlugin.Of(player);
			if (old != null) {
				old.Delete();
			}
			ShamanPacts.DrinkEmpathy(player, (ShamanItem) Give(player, "i_empathy_bottle", 1));
			EmpathyPlugin drunk = EmpathyPlugin.Of(player);
			Item second = Give(player, "i_empathy_bottle", 1);
			second.On_DClick(player);
			Report("oneElixirAtATime", (drunk != null) && (EmpathyPlugin.Of(player) == drunk) && !second.IsDeleted);
			second.Delete();
			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(orc, 300);
			try {
				for (int i = 0; i < 10; i++) { //Arms Lore 100.0 against 1.0 to 120.0
					DamageManager.Trigger_SwingLanding(DamageManager.GetWeaponSwingArgs(player, orc));
				}
				player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //@death alone, nobody dies
				Report("theElixirStaysThroughDeath", EmpathyPlugin.Of(player) != null);
			} finally {
				orc.Delete();
			}

			ClassAbilities.SetPoints(player, "woodoo_lifepact", 0);
			player.Stam = 100;
			player.Mana = 100;
			Give(player, "i_soul_case", 1);
			ShamanPacts.GiveLife(player, partner);
			LifePactPlugin ending = LifePactPlugin.Of(partner);
			if (ending != null) {
				ending.Timer = 1;
			}
			Console.WriteLine("SHAMAN_EMPATHY_READ");
		}

		[SteamFunction]
		public static void TestShamanSoulGone(Character self) {
			Player player = (Player) self;
			Player partner = FindPlayer("Seer");
			Report("theSoulGoesWhenThePactEnds", (LifePactPlugin.Of(partner) == null) && (ShamanAbilities.FindInPack(partner, "i_soul") == null));
			Report("theElixirEndsItself", EmpathyPlugin.Of(player) == null);

			player.Stam = 100;
			player.Mana = 100;
			Give(player, "i_soul_case", 1);
			ShamanPacts.GiveLife(player, partner);
			LifePactPlugin pact = LifePactPlugin.Of(partner);
			if (pact != null) {
				pact.createdAt -= 60;
			}
			partner.Hits = 0;
			partner.CauseDeath(null);
			Point4D before = new Point4D(partner.X, partner.Y, partner.Z, partner.M);
			partner.Go(((FlaggedRegion) StaticRegion.GetByDefname("a_Arena_Gondor")).P);
			ShamanPacts.LifePact(partner);
			Report("anArenaRisesAtOnce", (pact != null) && !partner.Flag_Dead && (LifePactPlugin.Of(partner) == null));
			partner.Go(before);
			partner.Hits = partner.MaxHits;
			Console.WriteLine("SHAMAN_SOUL_DONE");
		}

		private static int CountAround(Character self, string defname) {
			int count = 0;
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 4)) {
				if ((thing is Item item) && (item.Def.PrettyDefname == defname) && (item.Cont == null)) {
					count += item.Amount;
				}
			}
			return count;
		}

		private static Player FindPlayer(string name) {
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is Player found) && (found.Name == name) && !found.IsDeleted) {
					return found;
				}
			}
			return null;
		}

		private static Item Give(Character self, string defname, int amount) {
			Item item = (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(self.Backpack);
			item.Amount = amount;
			return item;
		}

		private static int Count(Character self, string defname) {
			Item item = ShamanAbilities.FindInPack(self, defname);
			return (item == null) ? 0 : item.Amount;
		}

		private static void Report(string name, bool ok) {
			ClassAbilityTests.Report(name, ok);
		}
	}

	/// <summary>#1445 - a blow of 40 at @getHit, so a real swing always hurts and kills no one.</summary>
	public class E_BlowOf40 : CompiledTriggerGroup {
		public void On_GetHit(Character self, WeaponSwingArgs swingArgs) {
			swingArgs.DamageAfterAC = 40;
		}
	}
}
