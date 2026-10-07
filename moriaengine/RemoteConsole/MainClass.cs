using System;
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

			Tools.ExitBinDirectory();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

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

		static void Application_ThreadException(object sender, ThreadExceptionEventArgs e) {
			Common.Logger.WriteError("Untrapped exception", e.Exception);
		}
	}
}