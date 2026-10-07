using System;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class CommandLineDisplay : UserControl {
		GameUid id;

		public CommandLineDisplay(string name, GameUid id) {
			this.id = id;
			this.InitializeComponent();
			this.txtDisplay.DefaultTitle = name;
			this.InitButtons();
		}

		public void EnableComandLine() {
			this.txtCommandLine.Enabled = true;
			this.pnlGameServerButtons.Enabled = true;
		}

		private void txtCommandLine_KeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar == '\r') {
				string cmd = this.txtCommandLine.Text;
				this.txtCommandLine.Items.Remove(cmd);

				//if (txtCommandLine.Items.Count>maxCmdHistory)
				//    txtCommandLine.Items.RemoveAt(txtCommandLine.Items.Count-1);
				this.txtCommandLine.Items.Insert(0, cmd);

				if (cmd.Trim() != "") {
					ConsoleClient.SendCommand(this.id, cmd);
					this.txtCommandLine.Text = "";
				}
				e.Handled = true;
			} else if (e.KeyChar == 27) {
				this.txtCommandLine.Text = "";
				e.Handled = true;
			}
		}

		private const GameUid uidSplit = (GameUid) ((GameUid.LastSphereServer - GameUid.FirstSEGameServer) / 2);

		private void InitButtons() {
			if (this.id == GameUid.AuxServer) {
				this.AddButtonsFromIni("AuxButtons");
			} else  if ((this.id >= GameUid.FirstSEGameServer) && (this.id < uidSplit)) {
				this.AddButtonsFromIni("SEButtons");
			} else if ((this.id <= GameUid.LastSphereServer) && (this.id > uidSplit)) {
				this.AddButtonsFromIni("SphereButtons");
			} else {
				this.pnlGameServerButtons.Dispose();
			}
		}

		private void AddButtonsFromIni(string iniSectionName) {
			IniFile ini = new IniFile(Settings.iniFileName);
			IniFileSection section = ini.GetNewOrParsedSection(iniSectionName);
			foreach (IniFileValueLine line in section.Lines) {
				Button button = new Button();
				button.Text = line.Name;
				button.Tag = line.GetValue<string>();
				button.Click += this.btnCommand_Click;
				this.pnlGameServerButtons.Controls.Add(button);
			}
			ini.WriteToFile();
		}


		private void btnCommand_Click(object sender, EventArgs e) {
			Button button = (Button) sender;
			string cmd = (string) button.Tag;

			ConsoleClient.SendCommand(this.id, cmd);
		}
	}
}
