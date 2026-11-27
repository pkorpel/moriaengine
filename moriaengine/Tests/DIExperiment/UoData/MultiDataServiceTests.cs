using System;
using System.IO;
using System.Linq;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SteamEngine.Tests.DIExperiment.UoData {
	public sealed class MultiDataServiceTests {
		private readonly ITestOutputHelper output;

		public MultiDataServiceTests(ITestOutputHelper output) {
			this.output = output;
		}

		/// <summary>Validation identifies both files needed to load enabled multi data.</summary>
		[Fact]
		public void MissingMultiFilesFailValidationWithTheirPaths() {
			using var folder = new TempDirectory();
			var settings = new MultiDataConfiguration { MulPath = folder.Path };

			var error = Assert.Throws<SEException>(settings.Validate);
			Assert.Contains(Path.Combine(folder.Path, "multi.idx"), error.Message);
			Assert.Contains(Path.Combine(folder.Path, "multi.mul"), error.Message);
		}

		/// <summary>The service reads real multi data and releases the shared catalog when disposed.</summary>
		[Fact]
		public void LoadsRealMultiDataAndClearsItOnDispose() {
			string root = FindRepositoryRoot();
			string mulPath = Path.Combine(root, "uo-files");
			var settings = new MultiDataConfiguration { MulPath = mulPath };
			var logger = new RecordingLogger();
			using var service = new MultiDataService(settings, logger);

			// Test the real reader directly, without starting an orchestrator.
			settings.Validate();
			service.Init();
			Print(logger);

			// Find any nonempty entry rather than depending on a particular client-data model.
			IMultiDataCatalog catalog = service;
			long slots = new FileInfo(Path.Combine(mulPath, "multi.idx")).Length / 12;
			var loaded = Enumerable.Range(0, checked((int) slots))
				.Select(slot => (Model: 0x4000 + slot, Data: catalog.GetByModel(0x4000 + slot)))
				.FirstOrDefault(entry => entry.Data is { Count: > 0 });
			Assert.NotNull(loaded.Data);
			Assert.True(loaded.Data.Count > 0);
			int model = loaded.Model;
			Assert.Same(catalog.GetByModel(model), MultiData.GetByModel(model));

			Assert.Contains(logger.Entries, entry =>
				entry.Message?.ToString().StartsWith("Num of multiItem slots: ", StringComparison.Ordinal) == true);

			// Both the DI facade and the legacy entry point share the cleared catalog.
			service.Dispose();
			Assert.Null(MultiData.GetByModel(model));
		}

		private static string FindRepositoryRoot() {
			for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent) {
				if (File.Exists(Path.Combine(directory.FullName, "uo-files", "multi.idx"))) {
					return directory.FullName;
				}
			}
			throw new DirectoryNotFoundException("Could not locate the repository's uo-files directory.");
		}

		private void Print(RecordingLogger logger) {
			foreach (var entry in logger.Entries) {
				output.WriteLine($"{entry.Kind}: {entry.Display}");
			}
		}
	}
}
