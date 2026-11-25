using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Piglet.Lexer;
using Piglet.Parser;
using Piglet.Parser.Configuration.Fluent;


namespace Client {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			new Task(this.Crunch).Start();

			//new Task(this.MathExample).Start();
		}

		private void Crunch() {
			var configurator = ParserFactory.Configure<dynamic>();


			var hoursMinutes = configurator.CreateTerminal(@"\d\d:\d\d:", f => DateTime.ParseExact(f, "hh:mm:", CultureInfo.InvariantCulture));
			var hoursMinutesClientId = configurator.CreateTerminal(@"\d\d:\d\d:[0-9a-eA-E]+:", f => {
				var parts = f.Split(':');
				var date = DateTime.Today.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
				var clientId = int.Parse(parts[2], NumberStyles.HexNumber);
				return Tuple.Create(date, clientId);
			});
			var completeDate = configurator.CreateTerminal(@"\d\d\d\d/\d\d/\d\d \d\d:\d\d:\d\d", f => DateTime.ParseExact(f, "yyyy/MM/dd hh:mm:ss", CultureInfo.InvariantCulture));
			var hexNumber = configurator.CreateTerminal(@"[0-9a-eA-E]+", f => f);
			var ipAddress = configurator.CreateTerminal(@"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+", f => IPAddress.Parse(f));
			var scriptFilePosition = configurator.CreateTerminal(@"([a-zA-Z0-9_]+\.scp,\d+)");
			var logLine = configurator.CreateNonTerminal();

			logLine.AddProduction(hoursMinutesClientId, "Client disconnected [Total:", hexNumber, "]").SetReduceFunction(s =>
				new ClientDisconnectedLine { time = s[0].Item1, clientId = s[0].Item2, totalClients = int.Parse(s[2]) });
			logLine.AddProduction(hoursMinutesClientId, "Client connected [Total:", hexNumber, "] from '", ipAddress, "'.").SetReduceFunction(s =>
				new ClientConnectedLine { time = s[0].Item1, clientId = s[0].Item2, totalClients = int.Parse(s[2]), ipAddress = s[4] });

			var log = "00:00:620:Client connected [Total:41] from '213.211.43.149'.";

			var parser = configurator.CreateParser();
			var result = parser.Parse(log);

			log = "00:00:620:Client disconnected [Total:40]";
			result = parser.Parse(log);

		}
	}

	public abstract class LogLine {
		public DateTime time;
		public bool isTimeExact;
	}

	public class ClientActionLine : LogLine {
		public int clientId;
	}

	public class ClientLoginLine : ClientActionLine {

		public string accountName;
	}

	public class ClientConnectedLine : ClientActionLine {
		public IPAddress ipAddress;
		public int totalClients;
	}

	public class ClientDisconnectedLine : ClientActionLine {
		public int totalClients;
	}

	//	public static class ParserExtensions {

	//		public static IMaybeNewRuleConfigurator ClientActionLineConfig(this IOptionalAsConfigurator configurator) {
	//			return configurator.As("LogLineObject")
	//				.WhenFound(f => {
	//					var l = (ClientActionLine) f.LogLineObject;
	//					l.clientId = f.ClientId;
	//					return l;
	//				});
	//		}
	//	}
	//}
}
