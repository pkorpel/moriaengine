using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1001 - the nine spells Erebor's numbers keep out of a book: real for a GM, no use to a mage, from book or scroll.</summary>
	[Collection(CoreSpellsCollection.Name)]
	public sealed class CoreSpellsTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private const int DaemonButton = 6;
		private readonly GameServerPairFixture fixture;

		public CoreSpellsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Uid(string line, string name) {
			return uint.Parse(Regex.Match(line, name + @"=#(\d+)").Groups[1].Value);
		}

		//a cast can fizzle, so a few tries
		private string CastUntil(TestGameClient client, string spell, string target, string wanted, int waitMs = 3200) {
			string report = "";
			for (int tries = 0; (tries < 6) && !report.Contains(wanted); tries++) {
				this.Ask(client, ".TestCoreCast \"" + spell + " " + target + "\"", "CORE_CAST:");
				Thread.Sleep(waitMs);
				report = this.Ask(client, ".TestCoreReport", "CORE_REPORT:");
			}
			return report;
		}

		[Fact]
		public void RealForAGmButNotForAMage() {
			using var client = CommandPermissionTests.Connect("coretest", "Core Tester");
			string ready = this.Ask(client, ".TestCoreReady", "CORE_READY:");
			string name = Regex.Match(ready, @"name=(.*?) model=").Groups[1].Value;
			string model = Regex.Match(ready, @"model=(\d+)").Groups[1].Value;
			int strength = int.Parse(Regex.Match(ready, @"str=(\d+)").Groups[1].Value);

			Assert.Contains("food=1", this.CastUntil(client, "s_create_food", "ground", "food=1", 4800));

			//the key decides whether it locks, then Unlock opens it only without one
			Assert.Contains("box=t_container ", this.CastUntil(client, "s_magic_lock", "box", "box=t_container_locked"));
			this.Ask(client, ".TestCoreLockBox", "CORE_LOCKED:");
			Assert.Contains("box=t_container ", this.CastUntil(client, "s_unlock", "box", "box=t_container "));
			this.Ask(client, ".TestCoreGiveKey", "CORE_KEY:");
			Assert.Contains("box=t_container_locked", this.CastUntil(client, "s_magic_lock", "box", "box=t_container_locked"));
			this.Ask(client, ".TestCoreDropKey", "CORE_KEY:");

			//mana goes from a mage, never from one with no Magery, and none comes to the caster
			string drained = "";
			for (int tries = 0; (tries < 6) && !Regex.IsMatch(drained, @"mageMana=\d?\d "); tries++) {
				drained = this.CastUntil(client, "s_mana_vampire", "mage", "mageMana=", 7600);
			}
			Assert.DoesNotContain("mageMana=100", drained);
			Assert.Contains("rabbitMana=100", this.CastUntil(client, "s_mana_vampire", "rabbit", "rabbitMana=9", 300));

			//the name of your race, for a while
			string incognito = this.CastUntil(client, "s_incognito", "self", "name=Man", 3000);
			Assert.DoesNotContain("name=" + name + " ", incognito);
			string ended = this.Ask(client, ".TestCoreEnd", "CORE_END:");
			Assert.Contains("name=" + name + " ", ended);

			//its menu comes first and runs on the caster, Str up to 150 more, all back at the end
			string daemon = "model=" + Regex.Match(ready, @"daemon=(\d+)").Groups[1].Value + " ";
			string polymorphed = "";
			for (int tries = 0; (tries < 6) && !polymorphed.Contains(daemon); tries++) {
				this.Ask(client, ".TestCoreCast \"s_polymorph self\"", "CORE_CAST:");
				var dialog = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Man", dialog.Strings);
				Assert.Contains("Daemon", dialog.Strings);
				Assert.Contains("Dragon", dialog.Strings);
				client.SendGumpResponse(dialog.GumpUid, DaemonButton);
				Thread.Sleep(4000);
				polymorphed = this.Ask(client, ".TestCoreReport", "CORE_REPORT:");
			}
			Assert.True(polymorphed.Contains(daemon), polymorphed + " wanted " + daemon);
			Assert.True(polymorphed.Contains("str=" + (strength + 150) + " "), polymorphed);
			ended = this.Ask(client, ".TestCoreEnd", "CORE_END:");
			Assert.Contains("model=" + model + " str=" + strength, ended);

			//a double click from nine tiles away
			string door = " door=" + Regex.Match(ready, @"doorModel=(\d+)").Groups[1].Value + " ";
			string opened = door;
			for (int tries = 0; (tries < 6) && opened.Contains(door); tries++) {
				this.Ask(client, ".TestCoreCast \"s_telekinesis door\"", "CORE_CAST:");
				Thread.Sleep(2500);
				opened = this.Ask(client, ".TestCoreReport", "CORE_REPORT:");
			}
			Assert.DoesNotContain(door, opened);

			//a mage at the cap: the book refuses
			string player = this.Ask(client, ".TestCorePlayer", "CORE_PLAYER:");
			this.Ask(client, ".TestCoreCast \"s_unlock box\"", "CORE_CAST:");
			DateTime deadline = DateTime.UtcNow + Wait;
			while (client.ReadSystemMessage(deadline - DateTime.UtcNow) != "You lack the skill to cast this spell.") {
			}
			Assert.Contains("box=t_container_locked", this.Ask(client, ".TestCoreReport", "CORE_REPORT:"));

			//a scroll only halves the difficulty: 250.0 against 100.0, so it fizzles and is gone, as in Sphere
			client.SendDoubleClick(Uid(player, "pack"));
			Thread.Sleep(300);
			client.SendDoubleClick(Uid(player, "scroll"));
			Thread.Sleep(300);
			client.SendTargetUid(Uid(player, "box"));
			Thread.Sleep(3000);
			string report = this.Ask(client, ".TestCoreReport", "CORE_REPORT:");
			Assert.Contains("box=t_container_locked", report);
			Assert.Contains("scrolls=0", report);
		}
	}

	/// <summary>#1424 - Polymorph and Incognito as their effect plugins.</summary>
	[Collection(CoreSpellsCollection.Name)]
	public sealed class MorphPluginTests {
		private readonly GameServerPairFixture fixture;

		public MorphPluginTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void PolymorphAndIncognitoAreTheirPlugins() {
			using var client = CommandPermissionTests.Connect("coretest", "Morph Tester");
			CommandPermissionTests.Ask(this.fixture, client, ".TestMorphPlugins", "MORPH_DONE");
			string output = this.fixture.GameOutputSnapshot();
			string failed = "";
			foreach (string check in new[] { "incognitoIsPlugin", "secondIncognitoRenews", "incognitoTimesOut", "polymorphIsPlugin", "deathEndsBoth" }) {
				string marker = "MORPH_CHECK: " + check + "=";
				int at = output.LastIndexOf(marker, StringComparison.Ordinal);
				if ((at < 0) || !output.Substring(at + marker.Length).StartsWith("True", StringComparison.Ordinal)) {
					failed += " " + check;
				}
			}
			Assert.True(failed.Length == 0, "Failed checks:" + failed);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CoreSpellsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CoreSpells";
	}
}
