using SteamEngine;

namespace SteamEngine.DIExperiment {
	internal interface IMultiDataCatalog {
		MultiData GetByModel(int model);
	}

	internal sealed class MultiDataService(MultiDataConfiguration settings, ILogger log) : ICoreService, IMultiDataCatalog {
		public void Init() =>
			MultiData.Init(settings.MulPath, settings.UseMultiItems, settings.WriteMulDocsFiles, settings.DocsPath, log);

		public MultiData GetByModel(int model) => MultiData.GetByModel(model);

		public void Dispose() => MultiData.Clear();
	}
}
