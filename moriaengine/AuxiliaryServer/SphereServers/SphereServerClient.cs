using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using SteamEngine.AuxiliaryServer.ConsoleServer;
using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SphereServers {
	public class SphereServerClient : GameServer {
		static GameUid uids = GameUid.LastSphereServer;

		private readonly SphereServerConnection conn;
		private SphereServerSetup setup;

		private bool loggedIn;

		private Timer loginTimeout;
		private Timer consoleAuthTimeout;

		private Timer exitlaterTimer;


		internal SphereServerClient(SphereServerConnection sphereServerConnection, SphereServerSetup setup) {
			this.uid = uids--;
			this.conn = sphereServerConnection;
			this.setup = setup;

			GameServersManager.AddGameServer(this);

			Console.WriteLine(this + " connected.");

			this.conn.StartLoginSequence(this.setup);

			this.loginTimeout = new Timer(this.LoginTimeout, null, 5000, Timeout.Infinite);
		}

		private void LoginTimeout(object o) {
			Console.WriteLine("LoginTimeout in");
			try {
				this.Conn.Close("Login timed out.");
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("LoginTimeout out");
		}

		internal void On_Close(string reason) {
			Console.WriteLine(this + " closed: " + reason);

			GameServersManager.RemoveGameServer(this);

			this.DisposeConsoleAuthTimeoutTimer();
			this.DisposeLoginTimeoutTimer();

			SphereServerClientFactory.Connect(this.setup, 5000); //start connecting again
		}


		public SphereServerConnection Conn {
			get {
				return this.conn;
			}
		}

		public override IGameServerSetup Setup {
			get {
				return this.setup;
			}
		}

		public override bool StartupFinished {
			get {
				return true; //we wouldn't even connect otherwise
			}
		}

		public override string ToString() {
			return "Sphere 0x" + ((int) this.ServerUid).ToString("X");
		}

		public bool IsLoggedIn {
			get {
				return this.loggedIn;
			}
		}

		internal void On_LoginSequenceFinished(LoginResult result) {
			this.DisposeLoginTimeoutTimer();
			switch (result) {
				case LoginResult.Success:
					Console.WriteLine(this + " logged in.");
					this.loggedIn = true;
					foreach (ConsoleClient console in ConsoleServer.ConsoleServer.AllConsoles) {
						console.OpenCmdWindow(this.Setup.Name, this.ServerUid);
						console.TryLoginToGameServer(this);
					}
					break;
				case LoginResult.BadPassword:
					this.conn.Close("Bad admin password set in steamaux.ini");
					break;
				case LoginResult.AccountInUse:
					this.conn.Close("Account '" + this.setup.AdminAccount + "' already in use.");
					break;
			}
		}

		private void DisposeLoginTimeoutTimer() {
			if (this.loginTimeout != null) {
				this.loginTimeout.Dispose();
				this.loginTimeout = null;
			}
		}

		#region authentising console
		public override void SendConsoleLogin(ConsoleId consoleId, string accName, string accPassword) {
			ConsoleCredentials state = new ConsoleCredentials(consoleId, accName, accPassword);

			if (accPassword.Length == 0) {
				this.DenyConsole(state, " - password must be > 0 characters");
				return;
			}

			this.conn.SendCommand("show findaccount(" + accName + ").password", 
				new CallbackCommandResponse<ConsoleCredentials>(
					this.OnAccountQueryResponse, state));

			this.consoleAuthTimeout = new Timer(this.ConsoleAuthTimeout, state, 5000, Timeout.Infinite);
		}

		private void ConsoleAuthTimeout(object o) {
			Console.WriteLine("ConsoleAuthTimeout in");
			try {
				//ConsoleCredentials state = (ConsoleCredentials) o;
				this.Conn.Close("Console user auth sequence timed out.");
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("ConsoleAuthTimeout out");
		}

		private class ConsoleCredentials {
			internal ConsoleId consoleUid;
			internal string accName, accPassword;

			internal ConsoleCredentials(ConsoleId consoleUid, string accName, string accPassword) {
				this.consoleUid = consoleUid;
				this.accName = accName;
				this.accPassword = accPassword;
			}
		}

		static Regex passwordQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).password' for '(.+?)' is '(?<password>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnAccountQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = passwordQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						if (!m.Groups["password"].Value.Equals(state.accPassword)) {
							this.DenyConsole(state, "and/or it's password.");
						} else {
							this.conn.SendCommand("show findaccount(" + state.accName + ").priv",
								new CallbackCommandResponse<ConsoleCredentials>(
									this.OnPrivQueryResponse, state));
						}
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Common.Logger.WriteDebug(e);
			}
		}

		static Regex privQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).priv' for '(.+?)' is '(?<priv>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnPrivQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = privQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						int priv = ConvertTools.ParseInt32(m.Groups["priv"].Value.Trim());
						if ((priv & 0x0200) == 0x0200) {
							this.DenyConsole(state, "- the acc is blocked.");
						} else {
							this.conn.SendCommand("show findaccount(" + state.accName + ").plevel",
								new CallbackCommandResponse<ConsoleCredentials>(
									this.OnPlevelQueryResponse, state));
						}
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Common.Logger.WriteDebug(e);
			}
		}

		static Regex plevelQueryRE = new Regex(
			@"findaccount\((?<username>.+?)\).plevel' for '(.+?)' is '(?<plevel>.*?)'",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void OnPlevelQueryResponse(IList<string> lines, ConsoleCredentials state) {
			try {
				foreach (string line in lines) {
					Match m = plevelQueryRE.Match(line);
					if (m.Groups["username"].Value.Equals(state.accName, StringComparison.OrdinalIgnoreCase)) {
						int plevel = ConvertTools.ParseInt32(m.Groups["plevel"].Value.Trim());
						if (plevel < 4) {
							this.DenyConsole(state, "- low plevel.");
						} else {
							ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
							if (console != null) {
								console.SetLoggedInTo(this);
							}
							this.SendCommand(console, "i");
						}
						this.DisposeConsoleAuthTimeoutTimer();
						return;
					}
				}
			} catch (Exception e) {
				this.DenyConsole(state, "Exception while auth sequence: " + e.Message);
				Common.Logger.WriteDebug(e);
			}
		}

		private void DenyConsole(ConsoleCredentials state, string reason) {
			ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(state.consoleUid);
			if (console != null) {
				console.CloseCmdWindow(this.ServerUid);

				ICollection<GameServer> serversLoggedIn = GameServersManager.AllServersWhereLoggedIn(console);
				if (serversLoggedIn.Count == 0) {
					Settings.ForgetUser(console.AccountName);
					console.SendLoginFailedAndClose("GameServer '" + this.Setup.Name + "' rejected username '" + state.accName + "' " + reason);
				}
			}

			this.DisposeConsoleAuthTimeoutTimer();
		}

		private void DisposeConsoleAuthTimeoutTimer() {
			if (this.consoleAuthTimeout != null) {
				this.consoleAuthTimeout.Dispose();
				this.consoleAuthTimeout = null;
			}
		}

		#endregion authentising console

		internal void On_ReceievedLine(string line) {
			line = line.Trim();
			if (!string.IsNullOrEmpty(line)) {
				if (this.loggedIn) {
					foreach (ConsoleClient console in GameServersManager.AllConsolesIn(this)) {
						if (!console.filteredGameServers.Contains(this.ServerUid)) {
							console.WriteLine(this.ServerUid, line);
						}
					}
				}
			}
		}

		public override void SendCommand(ConsoleClient console, string cmd) {
			if (cmd.StartsWith("[")) {
				SphereCommands.HandleCommand(console, this, cmd.Substring(1));
			} else {
				ConsoleId consoleId = ConsoleId.FakeConsole;
				if (console != null) {
					consoleId = console.ConsoleId;
				}
				this.conn.SendCommand(cmd, new CallbackCommandResponse<ConsoleId>(
					this.OnCommandReply, consoleId));
			}
		}

		private void OnCommandReply(IList<string> lines, ConsoleId consoleId) {
			ConsoleClient console = ConsoleServer.ConsoleServer.GetClientByUid(consoleId);
			if (console != null) {
				foreach (string line in lines) {
					string l = line.Trim();
					if (!string.IsNullOrEmpty(l)) {
						console.WriteLine(this.ServerUid, l);
					}
				}
			}
		}

		private void PrivateSendCommand(object o) {
			Console.WriteLine("PrivateSendCommand in");
			try {
				object[] arr = (object[]) o;
				this.SendCommand((ConsoleClient) arr[0], (string) arr[1]);
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("PrivateSendCommand out");
		}

		public void ExitLater(ConsoleClient console, TimeSpan timeSpan) {
			this.PrivateExitLater(new object[] { console, timeSpan } );
		}

		private void PrivateExitLater(object o) {
			Console.WriteLine("PrivateExitLater in");
			try {
				object[] arr = (object[]) o;
				ConsoleClient console = (ConsoleClient) arr[0];
				TimeSpan timeSpan = (TimeSpan) arr[1];

				if (timeSpan == TimeSpan.Zero) {
					this.SendCommand(console, "B Shutdown after save");
					this.ExitSave(console);
				} else if (timeSpan > TimeSpan.Zero) {
					this.SendCommand(console, "B Shutdown in " + timeSpan);
					TimeSpan newSpan = timeSpan.Subtract(TimeSpan.FromMinutes(1));
					if (newSpan < TimeSpan.Zero) {
						newSpan = TimeSpan.Zero;
					}
					arr[1] = newSpan;
					this.exitlaterTimer = new Timer(this.PrivateExitLater, arr, (int) timeSpan.TotalMilliseconds, Timeout.Infinite);
				} else if (this.exitlaterTimer != null) { //timespan negative and countdown on
					this.exitlaterTimer.Dispose();
					this.exitlaterTimer = null;
					this.SendCommand(console, "B Shutdown countdown aborted");
				}
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("PrivateExitLater out");
		}

		public void ExitSave(ConsoleClient console) {
			this.Save(console);
			new Timer(this.PrivateSendCommand, new object[] { console, "S"}, 1000, Timeout.Infinite);
			new Timer(this.PrivateSendCommand, new object[] { console, "X" }, 2000, Timeout.Infinite);
		}

		public void Save(ConsoleClient console) {
			this.SendCommand(console, "b Save imminent!");
			this.SendCommand(console, "var.wassave=1");
			this.SendCommand(console, "save 1");
			new Timer(this.PrivateSendCommand, new object[] { console, "var.wassave=0" }, 120 * 1000, Timeout.Infinite);
		}
	}
}
