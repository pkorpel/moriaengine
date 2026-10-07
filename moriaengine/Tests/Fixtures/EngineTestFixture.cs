using System;
using System.IO;

namespace SteamEngine.Tests.Fixtures {
	/// <summary>
	/// GameCore.Globals initializes itself from steamengine.ini the first time ANY of its
	/// static members is touched (a static field initializer, not something MainClass.Init()
	/// controls), reading/writing relative to whatever Environment.CurrentDirectory happens to
	/// be at that moment - and it only does this once per process, with no way to reset it.
	/// This fixture claims that one shot: it points CurrentDirectory at a fresh temp directory
	/// before anything else gets a chance to touch Globals, so the "first boot" ini/log/save
	/// files land somewhere disposable instead of wherever `dotnet test` happened to run from.
	///
	/// This deliberately does NOT call the real MainClass.Init() - that needs real .mul files
	/// (see GameCore/Main.cs) and belongs to a separate, slower full-boot test tier.
	/// </summary>
	public sealed class EngineTestFixture : IDisposable {
		public string TempDir { get; }

		public EngineTestFixture() {
			this.TempDir = Path.Combine(Path.GetTempPath(), "steamengine-tests-" + Guid.NewGuid());
			Directory.CreateDirectory(this.TempDir);
			Environment.CurrentDirectory = this.TempDir;

			// First touch of Globals with no steamengine.ini present in TempDir: this is the
			// same "write a default ini and carry on" path a real first boot takes
			// (Globals.LoadIni catches its own ShowMessageAndExitException internally - it
			// doesn't propagate here, and Show() only writes to Console.Error, no GUI).
			//
			// CoreLogger.Init() is internal to GameCore and not called here - Logger.WriteLine
			// already falls back to Console.WriteLine when no Logger subclass has been
			// constructed yet (Common/Logging/Logger.cs), so this is safe to skip for the fast
			// tier. A full-boot test (MainClass.Init(), called from within GameCore itself)
			// initializes it for real.
			_ = Globals.ServerName;
		}

		public void Dispose() {
			try {
				Directory.Delete(this.TempDir, true);
			} catch (IOException) {
				// best-effort cleanup; not worth failing a test run over
			} catch (UnauthorizedAccessException) {
			}
		}
	}
}
