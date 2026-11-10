using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	static class MainClass {
		public static readonly object globalLock = new object();

		public static readonly CancellationTokenSource exitTokenSource = new CancellationTokenSource();

		public static MainForm mainForm;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			//name the console window for better recognizability
			try {
				Console.Title = "SE Remote Console - " + Assembly.GetExecutingAssembly().Location;
			} catch { }

			EnterHomeDirectory();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			// #29: .NET 8 made Segoe UI 9 the default; the forms were laid out for the .NET Framework font.
			Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));

			mainForm = new MainForm();

			Logger.Init(mainForm.SystemDisplay);

			Application.ThreadException += Application_ThreadException;

			Application.Run(mainForm);


			//while (!mainForm.IsDisposed) {
			//    lock (globalLock) {
			//        Application.DoEvents();
			//    }

			//    Thread.Sleep(5);
			//}
		}

		// #29: Tools.ExitBinDirectory() only knew the old ..\build\Debug output. The console works from the folder holding
		// steamrc.example.ini (moriaengine/): steamrc.ini is read there and the log's relative file links resolve against it.
		private static void EnterHomeDirectory() {
			if (File.Exists("steamrc.example.ini")) {
				return;
			}
			for (DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
				if (File.Exists(Path.Combine(dir.FullName, "steamrc.example.ini"))) {
					Directory.SetCurrentDirectory(dir.FullName);
					Console.WriteLine($"Changed current directory to '{dir.FullName}'.");
					return;
				}
			}
		}

		static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			Common.Logger.WriteError("Untrapped exception", e.Exception);
		}
	}
}