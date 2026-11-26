using System;
using System.IO;

namespace SteamEngine.Tests.Fixtures {
	internal sealed class TempDirectory : IDisposable {
		private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();
		private bool disposed;

		internal string Path => directory.FullName;

		public void Dispose() {
			if (disposed) return;
			directory.Delete(recursive: true);
			disposed = true;
		}
	}
}
