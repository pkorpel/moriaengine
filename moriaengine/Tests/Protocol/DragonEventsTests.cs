using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - the dragons' events. Server side: scripts/Tests/DragonEventsTests.cs.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class DragonEventsTests {
		private readonly GameServerPairFixture fixture;

		public DragonEventsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"" + what + "\"", "DRAGON_TEST: " + what + " ");
		}

		[Fact]
		public void BreathBombSpinAurasCageBloodAndBrain() {
			using var client = CommandPermissionTests.Connect("dragontests", "Dragon Fighter");
			this.Do(client, "ready");
			//the breath: the brown dragon's own 120 from its .def, a strong tile, the black fire's stamina drain after
			string cone = this.Do(client, "cone");
			Assert.Contains("tuned=120 ", cone);
			Assert.True(!cone.Contains("lost=0 "), cone);
			Assert.Contains("lingering=4 breathAbility=True sees=True", cone);
			//a dragon resist above the damage: nothing
			Assert.Contains("lost=0 lingering=none", this.Do(client, "resist"));
			//the bomb on the player: cold fire
			string bomb = this.Do(client, "bomb");
			Assert.DoesNotContain("lost=0 ", bomb);
			Assert.Contains("lingering=2", bomb);
			//the spin: a turn clockwise to the east, the player burnt with no lingering effect, the dragon held still
			string spin = this.Do(client, "spin");
			Assert.Contains("facing=East ", spin);
			Assert.DoesNotContain("lost=0 ", spin);
			Assert.Contains("lingering=none frozen=True", spin);
			//the aura, then its first tick a second later
			Assert.Contains("reached=1 aura=True", this.Do(client, "aura"));
			Thread.Sleep(1500);
			string tick = this.Do(client, "auratick");
			Assert.DoesNotContain("lost=0 ", tick);
			Assert.DoesNotContain("stamLost=0 ", tick);
			//the cage: a mage can't slip out; fire east, the step refused; a click weakens it, the next one a second later puts it out
			Assert.Contains("caged=1 fire=True refused=True clicked=True standing=True", this.Do(client, "cage"));
			Thread.Sleep(1200);
			Assert.Contains("clicked=True gone=True open=True", this.Do(client, "cageclick"));
			//the blood: 3 puddles below 85%, 6 more past 65% and 45% at once
			Assert.Contains("first=3 later=9", this.Do(client, "blood"));
			//the brain's stamp: it rears up, a second later the ground shakes
			Assert.Contains("brain=True frozen=True", this.Do(client, "stamp"));
			Thread.Sleep(1500);
			string stamped = this.Do(client, "stamped");
			Assert.DoesNotContain("lost=0 ", stamped);
			Assert.Contains("paralysed=True", stamped);
		}
	}

	/// <summary>#1022 PR 2 - the summoning bosses.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class SummoningBossTests {
		private readonly GameServerPairFixture fixture;

		public SummoningBossTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestBoss \"" + what + "\"", "BOSS_TEST: " + what + " ");
		}

		[Fact]
		public void SummonsSightDeathsAndForms() {
			using var client = CommandPermissionTests.Connect("dragontests", "Boss Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//the fire lord: columns, two phoenixes and an eye of its own; dying, it blasts whoever fought it
			string lord = this.Do(client, "firelord");
			Assert.Contains("phoenixes=2 eyes=1 columns=True", lord);
			Assert.True(!lord.Contains("deathBlast=0 "), lord);
			//the cajun: its three children and then a third one more around the attacker
			Assert.Contains("children=4 third=2", this.Do(client, "cajun"));
			//the elder drone: three drones, then home
			Assert.Contains("drones=3 home=True", this.Do(client, "drone"));
			//the serpent druid: snakes of its own; below half the serpent's look, and no more snakes
			Assert.Contains("snakes=3 serpent=True model=True snakesAfter=3", this.Do(client, "druid"));
			//SwARPUS: three worms on first sight, none the next look; poison shrugged off, a heal brings two
			Assert.Contains("sightThreeEach=True noneAgain=True poisonRefused=True healRefused=False spellWorms=2", this.Do(client, "worms"));
			//the guardian: a golem for a mage; dying, a blast, no body and no experience
			string guardian = this.Do(client, "guardian");
			Assert.Contains("golems=1 vanishes=True", guardian);
			Assert.Contains("exp=0", guardian);
		}
	}

	/// <summary>#1022 PR 3 - the dying bosses.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class DyingBossTests {
		private readonly GameServerPairFixture fixture;

		public DyingBossTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestDeathBoss \"" + what + "\"", "DEATHBOSS_TEST: " + what + " ");
		}

		[Fact]
		public void LavaKamikazeGatesRisingHelpersAndUndead() {
			using var client = CommandPermissionTests.Connect("dragontests", "Dying Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//Rascal: no bomb before his cooldown since the first blow, then one lit bomb nobody can pick up, Rascal's own 10 s
			Assert.Contains("first=0 bombs=1 armed=True cooldown=10", this.Do(client, "lava"));
			//the ghost: its fuse, a second later the blast and it's gone
			Assert.Contains("fuse=True tuned=60", this.Do(client, "bomba"));
			Thread.Sleep(1500);
			string blast = this.Do(client, "bombaafter");
			Assert.DoesNotContain("lost=0 ", blast);
			Assert.Contains("gone=True", blast);
			//the paladin's gate to 807,633 on plane 2, gone after its time
			Assert.Contains("gates=807,633,2;", this.Do(client, "paladin"));
			//Lord Strachu: nothing above a sixth; below, one gate to level 2 on plane 10 however many blows, he stands and goes
			Assert.Contains("early=none gates=5348,4063,10; frozen=True vanishing=True", this.Do(client, "lord1"));
			//OCS: two helpers' bodies remembered, Rascal raises both, worth nothing, gone in time; nothing left to raise
			Assert.Contains("bodies=2 raised=2 risenNoExp=2 bodiesGone=True again=0", this.Do(client, "ocs"));
			//Arghelior: a blow above half does nothing; below, one dragon, he stands and goes
			Assert.Contains("early=0 dragons=1 frozen=True vanishing=True", this.Do(client, "beridor"));
			//e_necroslave: bare hands a mage, a bow an archer, a player outside Beridor nothing; a body rises, a carved one doesn't
			string necro = this.Do(client, "necro");
			Assert.True(necro.Contains("kinds=mage,archer,none rising=2"), necro);
			Thread.Sleep(1500);
			string risen = this.Do(client, "necroafter");
			Assert.True(risen.Contains("risen=1 noExp=1 firstBodyGone=True carvedStays=True"), risen);
		}
	}

	/// <summary>#1022 PR 4 - bosses with damage rules of their own.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class DamageRuleBossTests {
		private readonly GameServerPairFixture fixture;

		public DamageRuleBossTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			string line = CommandPermissionTests.Ask(this.fixture, client, ".TestDamageRule \"" + what + "\"", "DAMAGERULE_TEST: " + what + " ");
			return line;
		}

		private void Expect(TestGameClient client, string what, string expected) {
			string line = this.Do(client, what);
			Assert.True(line.Contains(expected), line);
		}

		[Fact]
		public void BalrogRinaldHelmDruidAndTitan() {
			using var client = CommandPermissionTests.Connect("dragontests", "Rule Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//the Balrog: its fire burns, else weakens; a blow comes back; its shadow curse stops casting, one at a time
			this.Expect(client, "balrog", "burnt=True stam=0 reflected=True cursed=True noCast=True oneAtATime=True castsAgain=True");
			//Rinald: hitting him early makes him tougher; nobody but players hurts him; a warrior takes his armor, a mage his fire
			this.Expect(client, "rinald", "invulGrows=200 npcHurts=False warriorArmor=50 mageFire=25");
			//the helm's cry finds the hidden a second later
			this.Expect(client, "helm", "crying=True");
			Thread.Sleep(1500);
			this.Expect(client, "helmafter", "hidden=False");
			//the druid: a bear or a wolf, as Druid, and back
			this.Expect(client, "druid", "name=Druid form=True back=True");
			this.Expect(client, "titan", "emotes=True");
		}

		[Fact]
		public void DarkmoonTakesAFormWithItsAbilitiesAndNumbers() {
			using var client = CommandPermissionTests.Connect("dragontests", "Moon Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			string line = CommandPermissionTests.Ask(this.fixture, client, ".TestDarkmoon", "DARKMOON_TEST: ");
			//a worker: its look, its bleed and numbers, not her own dispel bomb; the queen: no bleed, the queen's lightnings and magery
			Assert.True(line.Contains("worker=True bleed=True dispelGone=True keepsMorph=True blunt=600 magery=0 bleedChance=300 hitsFit=True"), line);
			Assert.True(line.Contains("queen: bleed=False lightnings=True magery=4500"), line);
			//back to herself: her look, her abilities and numbers
			Assert.True(line.Contains("own: model=True dispel=True lightnings=False tuning=True blunt=850"), line);
			//a blow changes her, then she waits out the cooldown
			Assert.True(line.Contains("hit: changed=True waits=True"), line);
			//a spell's hit makes her change too
			Assert.True(line.Contains("spell: changed=True"), line);
		}
	}

	/// <summary>#1022 PR 5a - Halda Molle's fights.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class HaldaMolleTests {
		private readonly GameServerPairFixture fixture;

		public HaldaMolleTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private void Expect(TestGameClient client, string what, string expected) {
			string line = CommandPermissionTests.Ask(this.fixture, client, ".TestHaldaMolle \"" + what + "\"", "HALDA_TEST: " + what + " ");
			Assert.True(line.Contains(expected), line);
		}

		[Fact]
		public void SplitFrostEggsAndWarniaks() {
			using var client = CommandPermissionTests.Connect("hmtests", "Halda Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//Bezejmenny splits: 90% for it, 81% for the copy worth a third; nothing hurts it until the copy is gone
			this.Expect(client, "split", "copies=2 bossHits90=True copyHits81=True copyExp=True copyFloorChance=35 guarded=True released=True");
			//its frost wave: 59 hits whatever the armor and frozen
			this.Expect(client, "frost", "lost=59 frozen=True wave=True");
			//a warniak's eggs: too light to stomp; a stomped one doesn't hatch, the other does, its own kind
			this.Expect(client, "egg", "tooLight=True stomped=True kind=c_hm_warniak_2");
			Thread.Sleep(1500);
			this.Expect(client, "eggafter", "hatched=1 eggsGone=True");
			//warniak 4 dies into a 3, the 3 into a 2
			this.Expect(client, "degrade", "threes=1 twos=1");
			//Marali koren: one warniak for blows within the cooldown, one on sight within its cooldown, four at death
			this.Expect(client, "drop", "onHits=1 onSight=1 atDeath=4");
		}
	}

	/// <summary>#1022 PR 5b - the HCN tombs' monsters.</summary>
	[Collection(DragonEventsCollection.Name)]
	public class HcnTombTests {
		private readonly GameServerPairFixture fixture;

		public HcnTombTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private void Expect(TestGameClient client, string what, string expected) {
			string line = CommandPermissionTests.Ask(this.fixture, client, ".TestHcn \"" + what + "\"", "HCN_TEST: " + what + " ");
			Assert.True(line.Contains(expected), line);
		}

		[Fact]
		public void PursuitPoisonMadnessAndKarma() {
			using var client = CommandPermissionTests.Connect("hcntests", "Tomb Fighter");
			CommandPermissionTests.Ask(this.fixture, client, ".TestDragon \"ready\"", "DRAGON_TEST: ready ");
			//a wraith remembers the players, dashes its own 4 tiles, hates a mage over a ranger, and rangers count at all
			this.Expect(client, "pursuit", "remembers=True steps=4 closer=True mageOverRanger=100 rangerCounts=True");
			//the snake poisons on a blow that did damage only
			this.Expect(client, "poison", "missNoPoison=True poisoned=True");
			//a mad warrior needs a weapon and turns on the pet beside; a mad mage burns it
			this.Expect(client, "madness", "unarmedWarrior=0 warriorHit=True mageHit=True petBurnt=True mummyChance=666");
			//the converter's KARMA=-{...} is evil now
			this.Expect(client, "karma", "evil=True port=8");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class DragonEventsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DragonEvents";
	}
}
