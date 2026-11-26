using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using SharpConfig;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests.DIExperiment {
	public class SharpConfigStoreTests {
		[IniSection("files")]
		internal sealed record MulSettings : ICoreSettings {
			[Description("Path to the mul files")]
			public string mulPath { get; init; } = "../uo-files";

			public void Validate() { }
		}

		[IniSection("scripts")]
		internal sealed record ModelSettings : ICoreSettings {
			[Description("Default item model")]
			public ushort defaultItemModel { get; init; } = 0xeed;

			public void Validate() { }
		}

		[IniSection("text")]
		internal sealed record LanguageSettings : ICoreSettings {
			[Description("Default connection language")]
			public Language defaultLanguage { get; init; } = Language.English;

			public void Validate() { }
		}

		private static IConfigStore Loaded(string path) {
			IConfigStore store = new SharpConfigStore(path);
			store.Load();
			return store;
		}

		/// <summary>Reading a partial section preserves existing values and supplies missing defaults immediately.</summary>
		[Fact]
		public void ReadCompletesPartialSettingsWithoutASecondPass() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, "[files]\nlogPath=custom/logs\n");
			var store = Loaded(path);

			// A partial section must produce usable settings immediately, not a null retry signal.
			Assert.Equal(new LoggerConfiguration { LogPath = "custom/logs" }, store.Read<LoggerConfiguration>());
		}

		/// <summary>An existing INI can gain a whole section without preventing its consumer from starting.</summary>
		[Theory]
		[InlineData("")]
		[InlineData("[unrelated]\nkey=value\n")]
		public void ExistingIniIsCompletedAndStartupContinues(string content) {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, content);
			var events = new List<string>();
			using var host = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>)], events);

			Assert.Equal(StartupResult.Running, host.Start(new SharpConfigStore(path)));
			Assert.Equal(new[] { "init" }, events);
			Assert.Equal(new MulSettings(), host.Get<ConfigProbe<MulSettings>>().Settings);
		}

		/// <summary>A missing INI is generated for review without starting services; its next boot can run.</summary>
		[Fact]
		public void MissingIniIsGeneratedAndStopsOnlyItsFirstStartup() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			var events = new List<string>();

			// First boot generates the file but starts no consumer.
			using (var firstHost = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>)], events)) {
				Assert.Equal(StartupResult.DefaultsWritten, firstHost.Start(new SharpConfigStore(path)));
				Assert.Empty(events);
			}

			// The generated file is an existing configuration on the next boot.
			using var secondHost = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>)], events);
			Assert.Equal(StartupResult.Running, secondHost.Start(new SharpConfigStore(path)));
			Assert.Equal(new[] { "init" }, events);
		}

		/// <summary>A failed final write prevents services from starting and cleans up its temporary file.</summary>
		[Fact]
		public void FailedSaveStopsStartup() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			Directory.CreateDirectory(path);
			var events = new List<string>();
			using var host = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>)], events);

			Assert.Equal(StartupResult.Failed, host.Start(new SharpConfigStore(path)));
			Assert.Empty(events);
			Assert.Single(Directory.EnumerateFileSystemEntries(folder.Path));
		}

		[IniSection("invalid")]
		internal sealed class InvalidSettings : ICoreSettings {
			[Description("A setting that fails validation")]
			public int value { get; init; }

			public void Validate() => throw new InvalidOperationException("Invalid settings.");
		}

		/// <summary>Syntax, conversion, and validation failures stop startup without creating or changing the INI.</summary>
		[Theory]
		[InlineData(null, typeof(ConfigProbe<InvalidSettings>))]
		[InlineData("[unrelated]\nkey=value\n", typeof(ConfigProbe<InvalidSettings>))]
		[InlineData("[broken", typeof(ConfigProbe<ModelSettings>))]
		[InlineData("[scripts]\ndefaultItemModel=not-a-number\n", typeof(ConfigProbe<ModelSettings>))]
		public void FailedConfigurationStopsStartupWithoutWriting(string content, Type failing) {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			if (content != null) File.WriteAllText(path, content);
			var events = new List<string>();
			// MulSettings has defaults to stage, so a premature write would be visible.
			using (var host = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>), failing], events)) {
				Assert.Equal(StartupResult.Failed, host.Start(new SharpConfigStore(path)));
				Assert.Empty(events);
			}
			if (content == null) Assert.False(File.Exists(path));
			else Assert.Equal(content, File.ReadAllText(path));

			// A retry after the failure must still stop for review when no INI existed before.
			if (failing == typeof(ConfigProbe<InvalidSettings>)) {
				using var retry = new CoreServiceOrchestrator([typeof(ConfigProbe<MulSettings>)], events);
				var expected = content == null ? StartupResult.DefaultsWritten : StartupResult.Running;
				Assert.Equal(expected, retry.Start(new SharpConfigStore(path)));
			}
		}

		internal sealed class ConfigProbe<T>(T settings, List<string> events) : ICoreService where T : ICoreSettings {
			internal T Settings => settings;
			public void Init() => events.Add("init");
			public void Dispose() => events.Add("dispose");
		}

		/// <summary>Syntax errors name the file and line; conversion errors name the file, section, and value.</summary>
		[Theory]
		[InlineData("[files]\nmulPath=x\n[broken\n", new[] { "steamengine.ini, 3" })]
		[InlineData("[text]\ndefaultLanguage=unknown\n", new[] { "[text]", "unknown" })]
		public void ErrorsReportWhereTheyAre(string content, string[] expected) {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, content);
			IConfigStore store = new SharpConfigStore(path);
			var error = Assert.Throws<SEException>(() => {
				store.Load();
				store.Read<LanguageSettings>();
			});
			string message = error.NiceMessage.RawString + " " + error.InnerException?.Message;
			Assert.Contains(path, message);
			foreach (string fragment in expected) Assert.Contains(fragment, message);
		}

		/// <summary>A new file gains described defaults from several settings types only when saved.</summary>
		[Fact]
		public void MissingFileWritesDescribedDefaultsFromSeveralTypesIntoOneSection() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			IConfigStore store = new SharpConfigStore(path);
			Assert.False(store.Load());

			Assert.Equal(new LoggerConfiguration(), store.Read<LoggerConfiguration>());
			Assert.Equal(new MulSettings(), store.Read<MulSettings>());
			Assert.False(File.Exists(path));
			store.Save();

			var ini = Configuration.LoadFromFile(path);
			Assert.Single(ini);
			Assert.Equal("Path to the log files", ini["files"]["LogPath"].PreComment);
			Assert.Equal("Whether to log console output to a file", ini["files"]["LogToFiles"].PreComment);
			Assert.Equal("Path to the mul files", ini["files"]["mulPath"].PreComment);
			var reloaded = Loaded(path);
			Assert.Equal(new LoggerConfiguration(), reloaded.Read<LoggerConfiguration>());
			Assert.Equal(new MulSettings(), reloaded.Read<MulSettings>());
		}

		/// <summary>Automatic additions preserve operator values, comments, and unrelated settings.</summary>
		[Fact]
		public void PartialSectionAddsOnlyMissingKeysAndPreservesOperatorValuesAndComments() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			const string original = "[files]\n# operator comment\nlogPath=custom/logs\nunknown=keep\n[unrelated]\nkey=value\n";
			File.WriteAllText(path, original);
			var store = Loaded(path);
			Assert.Equal(new LoggerConfiguration { LogPath = "custom/logs" }, store.Read<LoggerConfiguration>());
			Assert.Equal(new MulSettings(), store.Read<MulSettings>());
			Assert.Equal(original, File.ReadAllText(path));
			store.Save();

			var ini = Configuration.LoadFromFile(path);
			Assert.Equal("operator comment", ini["files"]["logPath"].PreComment);
			Assert.Equal("keep", ini["files"]["unknown"].StringValue);
			Assert.Equal("value", ini["unrelated"]["key"].StringValue);
			Assert.Equal(new LoggerConfiguration { LogPath = "custom/logs" }, store.Read<LoggerConfiguration>());
			string written = File.ReadAllText(path);
			store.Read<LoggerConfiguration>();
			store.Save();
			Assert.Equal(written, File.ReadAllText(path));
		}

		/// <summary>Saving unchanged settings leaves a complete existing file untouched.</summary>
		[Fact]
		public void CompleteConfigurationIsNotRewritten() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			const string original = "[files]\nmulPath = custom/path\n";
			File.WriteAllText(path, original);
			var store = Loaded(path);
			Assert.Equal("custom/path", store.Read<MulSettings>().mulPath);
			store.Save();
			Assert.Equal(original, File.ReadAllText(path));
		}

		/// <summary>Replacing an existing INI preserves its Unix file permissions.</summary>
		[Fact]
		public void SavingPreservesUnixPermissions() {
			if (OperatingSystem.IsWindows()) return;
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, "[unrelated]\nkey=value\n");
			const UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite
				| UnixFileMode.GroupRead | UnixFileMode.GroupWrite;
			File.SetUnixFileMode(path, mode);
			var store = Loaded(path);
			store.Read<MulSettings>();
			store.Save();
			Assert.Equal(mode, File.GetUnixFileMode(path));
		}

		[IniSection("missing-description")]
		internal sealed record UndocumentedSettings {
			public string value { get; init; } = "default";
		}

		/// <summary>Programmer-authored settings require documentation before their defaults are written.</summary>
		[Fact]
		public void EveryValueRequiresADescriptionBeforeAnythingIsWritten() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			var store = Loaded(path);
			Assert.Throws<SEException>(() => store.Read<UndocumentedSettings>());
			Assert.False(File.Exists(path));
		}

		/// <summary>A failed save leaves staged defaults available for a retry without leaving a partial file.</summary>
		[Fact]
		public void FailedWriteCanBeRetried() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			var store = Loaded(path);
			Assert.Equal(new MulSettings(), store.Read<MulSettings>());

			// Block the save with a directory; retrying while blocked must still attempt the write.
			Directory.CreateDirectory(path);
			Assert.Contains(path, Assert.Throws<SEException>(() => store.Save()).Message);
			Assert.Throws<SEException>(() => store.Save());
			Assert.Single(Directory.EnumerateFileSystemEntries(folder.Path));

			// Once the obstruction is removed, defaults can be persisted by the original store.
			Directory.Delete(path);
			store.Save();
			Assert.Equal(new MulSettings(), Loaded(path).Read<MulSettings>());
			Assert.Single(Directory.EnumerateFileSystemEntries(folder.Path));
		}

		/// <summary>The reference section models use the same native mapping and described defaults.</summary>
		[Fact]
		public void ReferenceSectionsCanStillProduceTheDocumentedIni() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			var store = Loaded(path);
			Assert.Equal("Unnamed SteamEngine Shard", store.Read<SetupConfiguration>().name);
			Assert.Equal(60, store.Read<FilesConfiguration>().autosaveMinutes);
		}
	}
}
