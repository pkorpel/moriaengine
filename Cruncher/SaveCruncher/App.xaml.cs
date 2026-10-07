using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CrunchyUtils;

namespace SaveCruncher {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

		protected override void OnStartup(StartupEventArgs e) {

			this.DispatcherUnhandledException += App_DispatcherUnhandledException;

			base.OnStartup(e);
		}

		void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
			Logger.Write(e.Exception);

			e.Handled = true;
		}
	}
}
