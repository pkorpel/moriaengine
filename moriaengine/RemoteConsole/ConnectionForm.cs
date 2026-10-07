using System;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	public partial class ConnectionForm : Form {
		public ConnectionForm() {
			this.InitializeComponent();

			this.chkSavePassword.Checked = Settings.saveEndpointPasswords;

			this.endPointSettingBindingSource.DataSource = Settings.knownEndPoints;
		}

		private void btnCancel_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.Cancel;
		}

		private void chkSavePassword_CheckedChanged(object sender, EventArgs e) {
			Settings.saveEndpointPasswords = this.chkSavePassword.Checked;
		}

		private void btnNew_Click(object sender, EventArgs e) {
			this.endPointSettingBindingSource.AddNew();
		}

		private void btnRemove_Click(object sender, EventArgs e) {
			this.endPointSettingBindingSource.RemoveCurrent();
		}

		private void btnClear_Click(object sender, EventArgs e) {
			this.endPointSettingBindingSource.Clear();
		}

		private void txtAddress_TextChanged(object sender, EventArgs e) {
			this.EnableConnectIfPossible();
		}

		private void txtUserName_TextChanged(object sender, EventArgs e) {
			this.EnableConnectIfPossible();
		}

		private void txtPort_TextChanged(object sender, EventArgs e) {
			this.EnableConnectIfPossible();
		}

		private void EnableConnectIfPossible() {
			this.btnConnect.Enabled = (this.txtAddress.Text.Length > 0) &&
				(this.txtUserName.Text.Length > 0) &&
				(this.txtPort.Text.Length > 0);
		}

		private void btnConnect_Click(object sender, EventArgs e) {
			EndPointSetting eps = (EndPointSetting) this.endPointSettingBindingSource.Current;
			if (eps != null) {
				ConsoleClient.Connect(new EndPointSetting(eps));

				this.DialogResult = DialogResult.OK;
			}
		}

		private void btnSave_Click(object sender, EventArgs e) {
			Settings.Save();
		}
	}
}