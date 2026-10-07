using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CrunchyUtils {
	public static class Logger {
		public static void Write(object msg) {

			var dispatcher = Application.Current.Dispatcher;

			Action action = () => {

				var w = Application.Current.MainWindow;

				var tbLog = (RichTextBox) w.FindName("tbLog");




				var asException = msg as Exception;
				if (asException != null) {
					tbLog.AppendText(string.Concat("ERROR: ", asException.Message, Environment.NewLine));
				} else {
					tbLog.AppendText(string.Concat(msg, Environment.NewLine));
				}
			};

			if (dispatcher.CheckAccess()) {
				// The calling thread owns the dispatcher, and hence the UI element
				action();
			} else {
				// Invokation required
				dispatcher.Invoke(DispatcherPriority.Normal, action);
			}
		}

	}
}
