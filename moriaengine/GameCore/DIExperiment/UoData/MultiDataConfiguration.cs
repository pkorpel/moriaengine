using System.ComponentModel;
using System.IO;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	[IniSection("files")]
	internal sealed record MultiDataConfiguration : ICoreSettings {
		[Description("Path to the mul files")]
		public string MulPath { get; init; } = "../uo-files";

		[Description("Whether to use multi items...")]
		public bool UseMultiItems { get; init; } = true;

		[Description("If this is true/on/1, SteamEngine writes general information gathered from MUL files into the docs folder.")]
		public bool WriteMulDocsFiles { get; init; }

		[Description("Path to the docs (Used when writing out some information from MUL files, like map tile info)")]
		public string DocsPath { get; init; } = "./docs/";

		/// <summary>Checks the configured paths and requires both multi files when multi items are enabled.</summary>
		public void Validate() {
			if (string.IsNullOrWhiteSpace(MulPath)) {
				throw new SEException("The MulPath setting cannot be empty.");
			}

			// Both files are required to read multis; report their paths together for diagnosis.
			if (UseMultiItems) {
				var indexPath = Path.Combine(MulPath, "multi.idx");
				var dataPath = Path.Combine(MulPath, "multi.mul");
				if (!File.Exists(indexPath) || !File.Exists(dataPath)) {
					throw new SEException($"Multi item data requires both '{indexPath}' and '{dataPath}'.");
				}
			}

			// A docs destination is only needed when generating MUL documentation.
			if (WriteMulDocsFiles && string.IsNullOrWhiteSpace(DocsPath)) {
				throw new SEException("The DocsPath setting cannot be empty when WriteMulDocsFiles is enabled.");
			}
		}
	}
}
