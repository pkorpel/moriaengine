using System.IO;
using System.Linq;
using System.Text;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#825 - a real LScript [Speech] replaces a placeholder speech (scripts' SpeechStub, converted from an
	/// unported archive block) of the same name even when the placeholder was loaded first; a real speech is never
	/// replaced that way.</summary>
	[Collection(EngineCollection.Name)]
	public class SpeechPlaceholderTests {
		private sealed class Placeholder : AbstractSpeech {
			private readonly bool isPlaceholder;

			internal Placeholder(string name, bool isPlaceholder)
				: base(name) {
				this.isPlaceholder = isPlaceholder;
			}

			public override bool IsPlaceholder {
				get { return this.isPlaceholder; }
			}

			protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
				return SpeechResult.IgnoredOrActedUpon;
			}

			protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
				return SpeechResult.IgnoredOrActedUpon;
			}
		}

		private static PropsSection SpeechSection(string name) {
			string text = "[Speech " + name + "]\nON=*hello*\nreturn 1\n";
			using (StreamReader reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)))) {
				return PropsFileParser.Load("test.scp", reader, type => false, false).Single();
			}
		}

		[Fact]
		public void ScriptedSpeechReplacesAPlaceholderLoadedBeforeIt() {
			const string name = "spk_test_825_placeholder";
			new Placeholder(name, true).Register();

			ScriptedSpeech.LoadFromScripts(SpeechSection(name));

			Assert.IsType<ScriptedSpeech>(AbstractScript.GetByDefname(name));
		}

		[Fact]
		public void ScriptedSpeechDoesNotReplaceARealSpeech() {
			const string name = "spk_test_825_real";
			Placeholder real = new Placeholder(name, false);
			real.Register();

			Assert.Throws<SEException>(() => ScriptedSpeech.LoadFromScripts(SpeechSection(name)));
			Assert.Same(real, AbstractScript.GetByDefname(name));
		}
	}
}
