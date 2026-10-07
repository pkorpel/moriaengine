using System;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class StartGameForm : Form {
		private SendServersToStartPacket.GameServerEntry[] entries;

		public StartGameForm(SendServersToStartPacket.GameServerEntry[] entries) {
			this.entries = entries;

			this.InitializeComponent();
		}

		private void StartGameForm_Load(object sender, EventArgs e) {
			this.ddBuild.DataSource = EnumItem<BuildType>.GetAllItemsAsList();

			this.gameServerEntryBindingSource.DataSource = this.entries;
		}

		private void btnStart_Click(object sender, EventArgs e) {
			SendServersToStartPacket.GameServerEntry entry = (SendServersToStartPacket.GameServerEntry)
				this.gameServerEntryBindingSource.Current;
			BuildType build = (BuildType) this.ddBuild.SelectedValue;

			RequestStartGameServer packet = Pool<RequestStartGameServer>.Acquire();
			packet.Prepare(entry.Number, build);

			ConsoleClient.ConnectedInstance.Conn.SendSinglePacket(packet);

			this.DialogResult = DialogResult.OK;
		}

		private void gameServerEntryBindingSource_CurrentChanged(object sender, EventArgs e) {
			SendServersToStartPacket.GameServerEntry entry = (SendServersToStartPacket.GameServerEntry)
				this.gameServerEntryBindingSource.Current;

			this.btnStart.Enabled = !entry.Running;
		}
	}
}