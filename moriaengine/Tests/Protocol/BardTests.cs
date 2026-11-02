using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's Discordance, Peacemaking, Provocation and Sphere's Musicianship.</summary>
	[Collection(BardCollection.Name)]
	public class BardTests {
		private const int PeacemakingSkill = 9, DiscordanceSkill = 15, HidingSkill = 21;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public BardTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, params string[] patterns) {
			DateTime deadline = DateTime.UtcNow + Wait;
			foreach (string pattern in patterns) {
				string seen = "";
				try {
					string message;
					while (!Regex.IsMatch(message = client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
						seen += message + " | ";
					}
				} catch (TimeoutException) {
					throw new TimeoutException("No '" + pattern + "', only: " + seen);
				}
			}
		}

		private uint Animal(TestGameClient client, string how) {
			return uint.Parse(Regex.Match(this.Ask(client, ".TestBardAnimal \"" + how + "\"", "BARD_ANIMAL:"), @"npc=#(\d+)").Groups[1].Value);
		}

		private static void Use(TestGameClient client, int skill, uint target) {
			client.SendSkillUse(skill);
			Thread.Sleep(300);
			client.SendTargetUid(target);
		}

		private string Report(TestGameClient client, uint npc) {
			return this.Ask(client, ".TestBardReport " + npc, "BARD_REPORT:");
		}

		[Fact]
		public void DiscordanceWeakensAndWearsOff() {
			using var client = CommandPermissionTests.Connect("bardtest", "Bard Weaken");
			this.Ask(client, ".TestBardPrepare \"1100 1000\"", "BARD_PREPARED:");
			uint rabbit = this.Animal(client, "0");
			client.SendSkillUse(DiscordanceSkill);
			Expect(client, "You have no musical instrument available");

			string instrument = this.Ask(client, ".TestBardInstrument", "BARD_INSTRUMENT:");
			Assert.Contains("good=56 bad=57", instrument); //TDATA1 038, TDATA2 039

			//too strong for this skill, and one that knows the song
			Use(client, DiscordanceSkill, this.Animal(client, "3001"));
			Expect(client, "Oslabeni teto prisery presahuje tve moznosti");
			Use(client, DiscordanceSkill, this.Animal(client, "0 knows"));
			Expect(client, "Tohle nemuzes oslabit");

			Use(client, DiscordanceSkill, rabbit);
			Expect(client, "Oslabeni bylo uspesne");
			string weak = this.Report(client, rabbit);
			Assert.Contains("str=75 maxhits=75 hits=37", weak); //1000 / 4 permille, half hurt stays half
			Assert.Contains("wrestling=750", weak);
			Assert.Contains("weak=True", weak);

			this.Ask(client, ".TestBardHurry " + rabbit, "BARD_HURRIED:");
			string back = this.Report(client, rabbit);
			Assert.Contains("str=100 maxhits=100", back);
			Assert.Contains("wrestling=1000", back);

			//playing badly fails it
			this.Ask(client, ".TestBardPrepare \"0 1000\"", "BARD_PREPARED:");
			Use(client, DiscordanceSkill, this.Animal(client, "0"));
			Expect(client, "You play poorly", "Oslabeni se nepovedlo");
		}

		[Fact]
		public void PeacemakingPutsToSleepAndItWakesAngry() {
			using var client = CommandPermissionTests.Connect("bardtest", "Bard Calmer");
			this.Ask(client, ".TestBardPrepare \"1100 1000\"", "BARD_PREPARED:");
			this.Ask(client, ".TestBardInstrument", "BARD_INSTRUMENT:");

			Use(client, PeacemakingSkill, this.Animal(client, "2001"));
			Expect(client, "Uklidneni teto prisery presahuje tve moznosti");
			Use(client, PeacemakingSkill, this.Animal(client, "0 knows"));
			Expect(client, "Uklidneni teto prisery presahuje tve moznosti");

			uint rabbit = this.Animal(client, "0");
			Use(client, PeacemakingSkill, rabbit);
			Expect(client, "Uklidneni se povedlo");
			Assert.Contains("asleep=True", this.Report(client, rabbit));

			//it wakes and goes for the bard
			this.Ask(client, ".TestBardHurry " + rabbit, "BARD_HURRIED:");
			Thread.Sleep(500);
			string awake = this.Report(client, rabbit);
			Assert.Contains("asleep=False", awake);
			Assert.Contains("target=Bard Calmer", awake);

			//an unanswered cursor holds the skill
			client.SendSkillUse(PeacemakingSkill);
			Expect(client, "Koho chces zkusit uklidnit");
			client.SendSkillUse(PeacemakingSkill);
			Expect(client, "Nespechej s pouzivanim skillu");
		}

		[Fact]
		public void ProvocationSetsThemOnEachOther() {
			using var client = CommandPermissionTests.Connect("bardtest", "Bard Provoke");
			this.Ask(client, ".TestBardPrepare \"1100 1000\"", "BARD_PREPARED:");
			client.SendSkillUse(22);
			Expect(client, "Provokace se vyvolava prikazem .provo");
			client.SendSpeech(".provo");
			Expect(client, "Potrebujes nejaky hudebni nastroj");
			this.Ask(client, ".TestBardInstrument", "BARD_INSTRUMENT:");

			uint first = this.Animal(client, "0"), second = this.Animal(client, "0");
			client.SendSpeech(".provo");
			Expect(client, "Koho chces vyprovokovat");
			client.SendTargetUid(first);
			Expect(client, "Proti komu");
			client.SendTargetUid(second);
			Expect(client, "Provokace se povedla");
			Assert.DoesNotContain("target=none", this.Report(client, first));
			Assert.DoesNotContain("target=none", this.Report(client, second));

			//not one that knows the art, and another skill drops it
			client.SendSpeech(".provo");
			Thread.Sleep(300);
			client.SendTargetUid(this.Animal(client, "0 knows"));
			Expect(client, "Tohle nemuzes vyprovokovat");

			client.SendSpeech(".provo");
			Thread.Sleep(300);
			client.SendTargetUid(first);
			Expect(client, "Proti komu");
			client.SendTargetUid(second);
			client.SendSkillUse(HidingSkill);
			Expect(client, "Provokace se nepovedla");
		}

		[Fact]
		public void PlayingAnInstrument() {
			using var client = CommandPermissionTests.Connect("bardtest", "Bard Player");
			this.Ask(client, ".TestBardPrepare \"0 0\"", "BARD_PREPARED:");
			client.SendSpeech(".TestBardPlayMany");
			Expect(client, "You play poorly", "Hrajes falesne");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class BardCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Bard";
	}
}
