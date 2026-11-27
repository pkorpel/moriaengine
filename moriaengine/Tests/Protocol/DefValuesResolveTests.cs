using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1587 - every constant and def field with braces in its script text resolves, and every NPCDef, TemplateDef
	/// and NPC loot line can be made, except those listed in Data/DefValuesKnownFailures.txt (#1589). Values resolve lazily,
	/// so before this a value LScript couldn't parse only failed when something first read it. Other values that don't
	/// resolve are only reported (MORIA_DEFVALUES_REPORT). Server side: scripts/Tests/DefValuesResolveTests.cs.</summary>
	[Collection(DefValuesResolveCollection.Name)]
	public class DefValuesResolveTests : LivingWorldTestBase {
		public DefValuesResolveTests(GameServerPairFixture servers)
			: base(servers, "defvalues", "DEFVALUES") {
		}

		[Fact]
		public void EveryScriptedValueResolves() {
			string dump = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "defvalues-" + Guid.NewGuid().ToString("N") + ".txt");
			string output;
			int mark;
			using (TestGameClient game = this.Login("Rozresovac")) {
				mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestResolveAllDefValues \"" + dump + "\"");
				bool done = this.fixture.WaitForGameOutputSince("DEFVALUES_RESOLVED:", mark, TimeSpan.FromMinutes(10));
				output = this.fixture.GameOutputSnapshot();
				Assert.True(done, "No DEFVALUES_RESOLVED: the command threw, or the server died (a value that recursed did). Game output's end:\n"
					+ output.Substring(Math.Max(mark, output.Length - 3000)));
			}
			string failures = System.IO.File.ReadAllText(dump);
			string others = System.IO.File.ReadAllText(dump + ".other");
			System.IO.File.Delete(dump);
			System.IO.File.Delete(dump + ".other");
			int end = output.IndexOf("DEFVALUES_RESOLVED:", mark, StringComparison.Ordinal);
			string line = output.Substring(end, output.IndexOf('\n', end) - end);
			//what creating the NPCs logged without throwing (a loot or equipment line it couldn't make), for the follow-ups
			System.Text.StringBuilder logged = new System.Text.StringBuilder();
			foreach (string l in output.Substring(mark, end - mark).Split('\n')) {
				if ((l.Contains("WARNING:") || l.Contains("ERROR:")) && !l.TrimStart().StartsWith("at ", StringComparison.Ordinal)) {
					logged.Append(l.Trim()).Append('\n');
				}
			}
			string report = Environment.GetEnvironmentVariable("MORIA_DEFVALUES_REPORT");
			if (!string.IsNullOrEmpty(report)) { //the values outside this test's scope, written out for the follow-up issues
				System.IO.File.WriteAllText(report, failures + "\n\n== other values ==\n" + others + "\n\n== logged while creating ==\n" + logged);
			}
			Assert.DoesNotContain("constants=0 ", line); //it really walked them
			Assert.DoesNotContain("fields=0 ", line);
			Assert.DoesNotContain("braces=0 ", line);
			Assert.DoesNotContain("npcs=0 ", line);
			Assert.DoesNotContain("templates=0 ", line);
			Assert.DoesNotContain("lootLines=0 ", line);
			//#1589 - what can't be made for other reasons than braces is listed; anything else fails, and so does a listed one
			//that works now
			string[] known = System.IO.File.ReadAllLines(System.IO.Path.Combine(this.fixture.RepoRoot, "Tests", "Protocol", "Data", "DefValuesKnownFailures.txt"));
			System.Collections.Generic.HashSet<string> expected = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
			System.Collections.Generic.HashSet<string> maybe = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal); //"? x": only some random picks fail
			foreach (string k in known) {
				if (k.StartsWith("? ", StringComparison.Ordinal)) {
					maybe.Add(k.Substring(2));
				} else if ((k.Length > 0) && !k.StartsWith("#", StringComparison.Ordinal)) {
					expected.Add(k);
				}
			}
			System.Text.StringBuilder unexpected = new System.Text.StringBuilder();
			foreach (string f in failures.Split('\n')) {
				string key = (f.Length > 0) ? f.Substring(0, f.IndexOf(" : ", StringComparison.Ordinal)) : null;
				if ((key != null) && !expected.Remove(key) && !maybe.Contains(key)) {
					unexpected.Append(f).Append('\n');
				}
			}
			Assert.True(unexpected.Length == 0, line + "\n" + unexpected);
			Assert.True(expected.Count == 0, "Listed in DefValuesKnownFailures.txt but made fine now, take them out: " + string.Join(", ", expected));
		}
	}

	[CollectionDefinition(Name)]
	public class DefValuesResolveCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DefValuesResolve";
	}

	/// <summary>Its own boot: EveryScriptedValueResolves leaves items in limbo from the creations listed in
	/// DefValuesKnownFailures.txt, and their sync errors would land in this test's output.</summary>
	[Collection(AddBraceNpcsCollection.Name)]
	public class AddBraceNpcsTests : LivingWorldTestBase {
		public AddBraceNpcsTests(GameServerPairFixture servers)
			: base(servers, "pridavac", "PRIDAVAC") {
		}

		/// <summary>The GM's ".add" of the NPCs whose creation failed on braces (pkorpel's report on PR #1591:
		/// c_saur_hat_thief, DESCRIPTION = Thief {male}; it was its colour constant), through the real command and target.</summary>
		[Fact]
		public void AddCommandMakesTheBraceNpcs() {
			using (TestGameClient game = this.Login("Pridavac")) {
				this.Run(game, ".TestSetGmOn", "GM_MODE_TEST: SET");
				this.Run(game, ".TestReportPositionXYZ", "STATIC_POSITION:");
				string pos = this.LastLine("STATIC_POSITION:");
				int x = Number(pos, "x"), y = Number(pos, "y"), z = Number(pos, "z");
				foreach (string def in new[] { "c_saur_hat_thief", "c_ratman" }) {
					int mark = this.fixture.GameOutputSnapshot().Length;
					game.SendSpeech(".add " + def);
					System.Threading.Thread.Sleep(500);
					game.SendTargetGround(x + 2, y, z);
					System.Threading.Thread.Sleep(1000);
					this.Run(game, ".TestCountAddedNpcs \"" + def + "\"", "DEFVALUES_ADDED: def=" + def);
					string output = this.fixture.GameOutputSnapshot().Substring(mark);
					string added = this.LastLine("DEFVALUES_ADDED: def=" + def);
					Assert.True(added.Contains(" count=1 "), ".add " + def + ": " + added + ". Game output since:\n" + output);
					//a failure in @create or the equipment leaves the NPC standing and is only logged
					Assert.False(output.Contains("ERROR:"), ".add " + def + " logged an error:\n" + output);
				}
			}
		}
	}

	[CollectionDefinition(Name)]
	public class AddBraceNpcsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "AddBraceNpcs";
	}
}
