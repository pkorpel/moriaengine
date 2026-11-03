using System;
using SteamEngine.Common;

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
			ClassAbilities.SetPoints(player, "woodoo", 10);
			Give(player, ShamanAbilities.RubyDef, 10);
			Give(player, ShamanAbilities.CitrineDef, 1);

			ShamanAbilities.VoodooInt(player);
			Report("bloodToMind", (player.Int == 130) && (player.Vit == 70) && (Count(player, ShamanAbilities.RubyDef) == 9));
			ShamanAbilities.VoodooVit(player);
			Report("mindToBloodTurnsItTwice", (player.Int == 70) && (player.Vit == 130));
			ShamanAbilities.VoodooRovnovaha(player);
			Report("balanceComesBack", (player.Int == 100) && (player.Vit == 100) && (ShamanAbilities.Ritual(player) == 0));

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
			Report("aBloodPactCostsBlood", (player.Hits == shamanHits - 29) && (partner.GetTag(ShamanPacts.ByTK) == player) && partner.HasTimer(ShamanPacts.PactTK));
			partner.Hits = 50;
			shamanHits = player.Hits;
			ShamanPacts.ShareBlood(monster, partner, 100);
			Report("aMonstersBlowIsShared", (partner.Hits == 65) && (player.Hits == shamanHits - 15));
			Report("theGiverEarnsThroughTheBearer", ShamanPacts.EarnsNoKillExp(player) && !ShamanPacts.EarnsNoKillExp(partner));
			ShamanPacts.ShareBlood(player, partner, 100);
			partner.Hits = 50;
			ShamanPacts.ShareBlood(monster, partner, 100);
			Report("aPlayersBlowSwitchesItOff", partner.Hits == 50);
			ShamanPacts.CancelPact(player);
			Report("cancellingEndsBothSides", (player.GetTag(ShamanPacts.ToTK) == null) && (partner.GetTag(ShamanPacts.ByTK) == null) && !partner.HasTimer(ShamanPacts.PactTK));

			ShamanPacts.Give(player, player, "mana");
			Report("aManaPactMayBeOwn", (Count(player, ShamanAbilities.RubyDef) == 6) && player.HasTimer(ShamanPacts.PactTK));
			player.Mana = 0;
			ShamanPacts.CancelPact(player);

			Item soulCase = Give(player, "i_soul_case", 1);
			Report("theCaseIsAShamansThing", soulCase is ShamanItem);
			ClassAbilities.SetPoints(player, "woodoo_lifepact", 5);
			player.Stam = 100;
			player.Mana = 100;
			ShamanPacts.GiveLife(player, partner);
			Report("aLifePactGivesASoul", (ShamanAbilities.FindInPack(partner, "i_soul") != null) && soulCase.IsDeleted && (player.Stam == 45) && (player.Mana == 35));

			Report("resistsRead", (ShamanPacts.ResistText(-500, "ohnem", null) == "je smrtelne nachylny na utok ohnem.") && (ShamanPacts.ResistText(999, "ohnem", null) == "je prakticky nezranitelny ohnem."));
			Item bottle = Give(player, "i_empathy_bottle", 1);
			ShamanPacts.DrinkEmpathy(player, (ShamanItem) bottle);
			Report("theElixirLetsResistsBeSeen", player.HasTimer(ShamanPacts.EmpathyTK) && bottle.IsDeleted);

			monster.Delete();
			partner.Hits = 0;
			partner.CauseDeath(null);
			if (partner.GetTimer(ShamanPacts.SoulTK) is SoulTimer soul) {
				soul.createdAt -= 60;
			}
			Report("theSoulStaysThroughDeath", partner.Flag_Dead && (ShamanAbilities.FindInPack(partner, "i_soul") != null));
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
			Console.WriteLine("SHAMAN_DONE");
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
}
