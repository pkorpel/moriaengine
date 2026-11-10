using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#892 - Mordor's Rings of Power. Its own accounts: an account holds 5 characters.</summary>
	[Collection(NazgulTestsCollection.Name)]
	public class NazgulTests {
		private const string Account = "nazgultests";
		private const string AccountB = "nazgultestsb";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public NazgulTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RingIsEarnedRulesACityAndLeavesTheUnworthy() {
			using (var nazgul = this.Connect("Town Ring Bearer")) {
				this.Ask(nazgul, ".TestTownBecomeGm", "TOWN_GM:");
				this.Ask(nazgul, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var enemy = this.Connect("Town Ring Enemy", AccountB);
				this.Ask(nazgul, ".SetupTowns", "SETUP_DONE:");
				string ready = this.Ask(nazgul, ".TestNazgulPrepare", "NAZGUL_READY:");
				uint shrine = (uint) Number(ready, "shrine");
				uint nazgulUid = (uint) Number(ready, "uid");
				this.Ask(enemy, ".TestNazgulPrepare G", "NAZGUL_READY:");
				this.Nazgul(nazgul);

				//the quest first: the shrine starts it, and it takes 5 of Gondor
				nazgul.SendDoubleClick(shrine);
				Thread.Sleep(300);
				Assert.Contains("nazgul=False", this.Nazgul(nazgul));
				this.Ask(enemy, ".TestNazgulDie 4", "NAZGUL_DIED:");
				nazgul.SendDoubleClick(shrine);
				Thread.Sleep(300);
				Assert.Contains("nazgul=False", this.Nazgul(nazgul));
				this.Ask(enemy, ".TestNazgulDie 1", "NAZGUL_DIED:");
				nazgul.SendDoubleClick(shrine);
				Thread.Sleep(300);
				Assert.Contains("nazgul=True first=False name=Town_Ring_Bearer ruler=Town_Ring_Bearer", this.Nazgul(nazgul));

				//the crowd sees what they are
				enemy.SendSingleClick(nazgulUid);
				AssertNameSent(enemy, "NAZGUL");

				//the same victim counts once in a while, not every time
				this.Ask(enemy, ".TestNazgulDie 2", "NAZGUL_DIED:");
				Assert.Contains("kills=1 deaths=0", this.Nazgul(nazgul));

				//the shrine is now their city's menu
				nazgul.SendDoubleClick(shrine);
				var menu = nazgul.ReadGumpDialogWithStrings(Wait);
				Assert.True(menu.Strings.Exists(s => s.Contains("Pridat hrace na blacklist")), string.Join(" | ", menu.Strings));

				//fear takes the uniform
				string before = this.Ask(enemy, ".TestNazgulReport", "NAZGUL_REPORT:");
				nazgul.SendSpeech(".fear");
				Thread.Sleep(300);
				Assert.Equal(Number(before, "str"), Number(this.Ask(enemy, ".TestNazgulReport", "NAZGUL_REPORT:"), "str"));
				this.Ask(nazgul, ".TestNazgulDress", "NAZGUL_DRESSED:");
				Thread.Sleep(400);
				Assert.Contains("uniform=True", this.Nazgul(nazgul));
				nazgul.SendSpeech(".fear");
				Thread.Sleep(300);
				string feared = this.Ask(enemy, ".TestNazgulReport", "NAZGUL_REPORT:");
				Assert.Equal(Number(before, "str") - 3, Number(feared, "str"));
				Assert.Equal(Number(before, "vit") - 3, Number(feared, "vit"));

				//judged: fine while they're online and kill; gone when they stay away
				this.Ask(nazgul, ".TestNazgulJudge 1", "NAZGUL_JUDGED:");
				Assert.Contains("nazgul=True", this.Nazgul(nazgul));
				this.Ask(nazgul, ".TestNazgulJudge 0", "NAZGUL_JUDGED:");
				string gone = this.Nazgul(nazgul);
				Assert.Contains("nazgul=False", gone);
				Assert.Contains("ruler=none", gone);

				//#910 - the ring lies at its shrine in four fire columns, fixed; its double click is the shrine's quest
				string lying = Field(gone, "lying=");
				Assert.True(lying.EndsWith("/4/fixed", StringComparison.Ordinal), gone);
				uint ring = uint.Parse(lying.Substring(0, lying.IndexOf('/')), CultureInfo.InvariantCulture);
				nazgul.SendDoubleClick(ring);
				Thread.Sleep(300);
				Assert.Contains("nazgul=False", this.Nazgul(nazgul)); //the kills first again
				this.Ask(enemy, ".TestNazgulDie 5", "NAZGUL_DIED:");
				nazgul.SendDoubleClick(ring);
				Thread.Sleep(300);
				string again = this.Nazgul(nazgul);
				Assert.Contains("nazgul=True", again);
				Assert.Contains("lying=none", again);
			}
		}

		[Fact]
		public void TheFirstNazgulComesThroughTheTrial() {
			using (var nazgul = this.Connect("Town First Bearer")) {
				this.Ask(nazgul, ".TestTownBecomeGm", "TOWN_GM:");
				this.Ask(nazgul, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var other = this.Connect("Town Trial Other", AccountB);
				this.Ask(nazgul, ".SetupTowns", "SETUP_DONE:");
				this.Ask(nazgul, ".TestNazgulPrepare", "NAZGUL_READY:");
				this.Ask(other, ".TestNazgulPrepare", "NAZGUL_READY:");

				//one without a ring can't open the gate
				this.Ask(other, ".TestNazgulStep gate", "NAZGUL_STEPPED:");
				Assert.Contains("trial=False", this.Nazgul(other));

				this.Ask(nazgul, ".TestNazgulGiveRing", "NAZGUL_GIVEN:");
				this.Ask(nazgul, ".TestNazgulStep gate", "NAZGUL_STEPPED:");
				Thread.Sleep(300);
				string inside = this.Nazgul(nazgul);
				Assert.True(inside.Contains("trial=True", StringComparison.Ordinal), inside);

				uint leading = (uint) Number(this.Ask(nazgul, ".TestNazgulLeading", "NAZGUL_LEADING:"), "shrine");
				nazgul.SendDoubleClick(leading);
				Thread.Sleep(300);
				string first = this.Nazgul(nazgul);
				Assert.Contains("nazgul=True first=True name=Prvni_Nazgul_Town_First_Bearer ruler=none office=Prvni_Nazgul_Town_First_Bearer", first);
				Assert.Contains("gate=0x689", first);

				//the gate is shut while there's a First
				this.Ask(other, ".TestNazgulStep gate", "NAZGUL_STEPPED:");
				Assert.Contains("trial=False", this.Nazgul(other));

				//#909 - .nazghulfell: Lord or Lady only, the horse becomes a fell beast and back no sooner than 5 minutes; a death undoes it
				other.SendSpeech(".nazghulfell"); //a plain player's command
				Assert.NotNull(TryReadMessageContaining(other, "Nejsi Prvni Nazhgul", Wait));
				this.Ask(nazgul, ".TestNazgulDress", "NAZGUL_DRESSED:");
				Thread.Sleep(400);
				Assert.Contains("horse=0x690/0x3e9f", this.Nazgul(nazgul));
				this.Ask(nazgul, ".TestNazgulFell \"100\"", "NAZGUL_FELL:");
				nazgul.SendSpeech(".nazghulfell");
				Assert.NotNull(TryReadMessageContaining(nazgul, "muzes privolat Fella", Wait));
				this.Ask(nazgul, ".TestNazgulFell \"10000\"", "NAZGUL_FELL:");
				nazgul.SendSpeech(".nazghulfell");
				Assert.NotNull(TryReadMessageContaining(nazgul, "Privolavam Fella", Wait));
				Assert.Contains("horse=0x692/0x3f0e", this.Nazgul(nazgul));
				nazgul.SendSpeech(".nazghulfell");
				Assert.NotNull(TryReadMessageContaining(nazgul, "jeste nemuzes pouzit", Wait));
				this.Ask(nazgul, ".TestNazgulFell \"death\"", "NAZGUL_FELL:");
				Thread.Sleep(400);
				Assert.Contains("horse=0x690/0x3e9f", this.Nazgul(nazgul));

				this.Ask(nazgul, ".TestNazgulDismiss", "NAZGUL_DISMISSED:");
				string laid = this.Nazgul(nazgul); //#910 - the leading ring lies at the First's shrine in Fela Nazgul, its fire harmless
				Assert.True(Field(laid, "leadingLying=").EndsWith("/4/fixed", StringComparison.Ordinal), laid);
				Assert.True(Field(laid, "lying=").EndsWith("/4/fixed", StringComparison.Ordinal), laid); //the city ring left at the swap
				string dismissed = this.Nazgul(nazgul);
				Assert.Contains("nazgul=False first=False name=Town_First_Bearer ruler=none office=none", dismissed);
				Assert.Contains("gate=0x692", dismissed);
			}
		}

		private string Nazgul(TestGameClient client) {
			return this.Ask(client, ".TestNazgulReport", "NAZGUL_REPORT:");
		}

		/// <summary>#912 - the Nazgul-only gate, a GM tool: a GM sets where it leads; only Nazguls and GMs pass.</summary>
		[Fact]
		public void OnlyNazgulsPassTheirGate() {
			using (var gm = this.Connect("Gate Keeper")) {
				this.Ask(gm, ".TestTownBecomeGm", "TOWN_GM:");
				this.Ask(gm, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				this.Ask(gm, ".SetupTowns", "SETUP_DONE:");
				using var walker = this.Connect("Gate Walker", AccountB);
				this.Ask(walker, ".TestNazgulPrepare", "NAZGUL_READY:");
				this.Ask(gm, ".TestNazgulPrepare", "NAZGUL_READY:");
				this.Ask(gm, ".TestTownBecomeGm", "TOWN_GM:");
				string made = this.Ask(gm, ".TestNazgulGateMake", "NAZGUL_GATE:");
				Assert.Contains("leads=none", this.Ask(walker, ".TestNazgulGateStep", "NAZGUL_GATE_STEP:"));
				Assert.NotNull(TryReadMessageContaining(walker, "Jen pro vyvolene!", Wait));

				//a way back into the gate itself is refused (it would loop); the cursor stays for another spot
				gm.SendSpeech(".nazgulgateto");
				Thread.Sleep(300);
				gm.SendTargetUid((uint) Number(made, "uid"));
				Thread.Sleep(300);
				gm.SendTargetGround((int) Number(made, "x") - 5, (int) Number(made, "y"), (int) Number(made, "z"));
				Assert.NotNull(TryReadMessageContaining(gm, "vyber jine misto", Wait));
				gm.SendTargetGround((int) Number(made, "x"), (int) Number(made, "y"), (int) Number(made, "z"));
				Thread.Sleep(300);
				string to = Field(made, "x=") + "," + Field(made, "y=");

				string refused = this.Ask(walker, ".TestNazgulGateStep", "NAZGUL_GATE_STEP:");
				Assert.Contains("leads=" + to + " fixed=True", refused);
				Assert.DoesNotContain("at=" + to + " ", refused);
				Assert.NotNull(TryReadMessageContaining(walker, "Jen pro vyvolene!", Wait));
				this.Ask(walker, ".TestNazgulGiveRing", "NAZGUL_GIVEN:");
				Assert.Contains("at=" + to + " ", this.Ask(walker, ".TestNazgulGateStep", "NAZGUL_GATE_STEP:"));
			}
		}

		private static string TryReadMessageContaining(TestGameClient client, string text, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				string message;
				try {
					message = client.ReadSystemMessage(deadline - DateTime.UtcNow);
				} catch (TimeoutException) {
					return null;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return message;
				}
			}
			return null;
		}

		/// <summary>The overhead name as a client gets it, 0xAE's text after the 30 byte name field.</summary>
		private static void AssertNameSent(TestGameClient client, string name) {
			const int TextOffset = 47;
			DateTime deadline = DateTime.UtcNow + Wait;
			string seen = "";
			while (DateTime.UtcNow < deadline) {
				byte[] payload = client.ReadPacketPayload(0xAE, deadline - DateTime.UtcNow);
				seen = System.Text.Encoding.BigEndianUnicode.GetString(payload, TextOffset, payload.Length - TextOffset).TrimEnd('\0');
				if (seen == name) {
					return;
				}
			}
			Assert.Fail("No name '" + name + "', the last was '" + seen + "'");
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static string Field(string line, string key) {
			int start = line.IndexOf(key, StringComparison.Ordinal) + key.Length;
			int end = line.IndexOf(' ', start);
			return (end < 0) ? line.Substring(start) : line.Substring(start, end - start);
		}

		private static double Number(string line, string key) {
			string marker = key + "=";
			int start = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(start >= 0, "Could not find '" + marker + "' in: " + line);
			start += marker.Length;
			int end = line.IndexOf(' ', start);
			string text = (end < 0) ? line.Substring(start) : line.Substring(start, end - start);
			return double.Parse(text.Trim(), CultureInfo.InvariantCulture);
		}

		private TestGameClient Connect(string charName, string account = Account) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}
	}
}
