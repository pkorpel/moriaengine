using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class MainForm : Form {
		private Dictionary<GameUid, CommandLineDisplay> displays = new Dictionary<GameUid, CommandLineDisplay>();

		private EndPointSetting epsBeingReconnected;

		EventHandler txtDisplay_TitleChanged;

		public MainForm() {
			this.InitializeComponent();
			this.txtDisplay_TitleChanged = this.TxtDisplay_TitleChanged;
		}

		private void menuExit_Click(object sender, EventArgs e) {
			this.Close();
		}

		public LogStrDisplay SystemDisplay {
			get {
				return this.systemTabPage;
			}
		}

		public void AddCmdLineDisplay(GameUid id, string name) {
			if (!this.displays.ContainsKey(id)) {
				TabPage tab = new TabPage(name);
				CommandLineDisplay cld = new CommandLineDisplay(name, id);
				
				cld.txtDisplay.TitleChanged += this.txtDisplay_TitleChanged;
				tab.Controls.Add(cld);
				cld.Dock = DockStyle.Fill;
				this.tabControl.Controls.Add(tab);

				this.RemoveCmdLineDisplay(id);
				this.displays.Add(id, cld);
			}
		}

		public void RemoveCmdLineDisplay(GameUid id) {
			CommandLineDisplay cld;
			if (this.displays.TryGetValue(id, out cld)) {
				cld.Parent.Dispose();

				this.displays.Remove(id);
			}
		}

		public void EnableCommandLineOnDisplay(GameUid id) {
			CommandLineDisplay cld;
			if (this.displays.TryGetValue(id, out cld)) {
				cld.EnableComandLine();
			}
		}

		public void ClearCmdLineDisplays() {
			Control[] controls = new Control[this.tabControl.Controls.Count];
			this.tabControl.Controls.CopyTo(controls, 0);
			foreach (Control ctrl in controls) {
				if ((ctrl != this.systemTab) && (ctrl is TabPage)) {
					ctrl.Dispose();
				}
			}
			this.displays.Clear();
		}

		private void TxtDisplay_TitleChanged(object sender, EventArgs ignored) {
			LogStrDisplay display = (LogStrDisplay) sender;
			TabPage page = display.Parent.Parent as TabPage;
			if (page != null) {
				page.Text = display.Title;
			}
		}

		private void menuConnect_Click(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();
			ConnectionForm cf = new ConnectionForm();
			cf.ShowDialog();
		}

		protected override void OnControlRemoved(ControlEventArgs e) {
			TabPage page = e.Control as TabPage;
			if (page != null) {
				foreach (Control pageControl in page.Controls) {
					CommandLineDisplay cld = pageControl as CommandLineDisplay;
					if (cld != null) {
						cld.txtDisplay.TitleChanged -= this.txtDisplay_TitleChanged;
					}
				}
			}

			base.OnControlRemoved(e);
		}

		//ConsoleClient cc;

		//private void displayBox_MouseDoubleClick(object sender, MouseEventArgs e) {
		//    cc = Pool<ConsoleClient>.Acquire();

		//    cc.Connect("localhost", 12345);
		//}
		//private void statusBar_MouseDoubleClick(object sender, MouseEventArgs e) {
		//    ConsolePacketGroup pg = Pool<ConsolePacketGroup>.Acquire();
		//    pg.AddPacket(Pool<ConsoleOutgoingPacket>.Acquire());

		//    cc.SendPacketGroup(pg);
		//}

		internal void SetConnectedTrue() {
			this.SetConnected(true);
		}

		internal void SetConnectedFalse() {
			this.SetConnected(false);
		}

		internal void SetConnected(bool connected) {
			this.menuConnect.Enabled = !connected;
			this.menuDisconnect.Enabled = connected;
			this.menuRemoteServer.Enabled = connected;
		}

		private void menuDisconnect_Click(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();
			ConsoleClient.Disconnect("Disconnect menu item used.");
		}

		internal void WriteLine(GameUid uid, string str) {
			if (uid >= 0) {
				CommandLineDisplay cld;
				if (this.displays.TryGetValue(uid, out cld)) {
					cld.txtDisplay.WriteLine(str);
				}
			} else {
				this.systemTabPage.WriteLine(str);
			}
		}

		internal void Write(GameUid uid, string str) {
			if (uid >= 0) {
				CommandLineDisplay cld;
				if (this.displays.TryGetValue(uid, out cld)) {
					cld.txtDisplay.Write(str);
				}
			} else {
				this.systemTabPage.Write(str);
			}
		}

		private void menuStartGameServer_Click(object sender, EventArgs e) {
			ConsoleClient cc = ConsoleClient.ConnectedInstance;
			if (cc != null) {
				cc.Conn.SendPacketGroup(RequestServersToStartPacket.group);
			}
		}

		private void menuRestartAuxServer_Click(object sender, EventArgs e) {
			ConsoleClient.SendCommand(0, "restart");
		}

		private void systemTabPage_Load(object sender, EventArgs e) {

		}

		private void MainForm_Load(object sender, EventArgs e) {

		}

		internal void DelayReconnect(EndPointSetting eps) {
			this.Invoke(new Action<EndPointSetting>(this.InvokedDelayReconnect), eps);
		}

		private void InvokedDelayReconnect(EndPointSetting ipe) {
			this.epsBeingReconnected = ipe;
			//this.reconnectingTimer.Stop();
			this.reconnectingTimer.Start();
		}

		private void reconnectingTimer_Tick(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();

			Console.WriteLine(string.Concat("Reconnecting to ", 
				this.epsBeingReconnected.UserName,  "@", this.epsBeingReconnected.Address, ":" ,
				this.epsBeingReconnected.Port.ToString(CultureInfo.InvariantCulture)));
			ConsoleClient.Connect(this.epsBeingReconnected);
		}

		private void packetHandlingTimer_Tick(object sender, EventArgs e) {
			ConsoleIncomingPacket.HandleQueuedPackets();
		}
	}
}