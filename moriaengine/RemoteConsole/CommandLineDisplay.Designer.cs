using System.ComponentModel;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	partial class CommandLineDisplay {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.txtCommandLine = new System.Windows.Forms.ComboBox();
			this.pnlGameServerButtons = new System.Windows.Forms.FlowLayoutPanel();
			this.txtDisplay = new SteamEngine.RemoteConsole.LogStrDisplay();
			this.SuspendLayout();
			// 
			// txtCommandLine
			// 
			this.txtCommandLine.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.txtCommandLine.Enabled = false;
			this.txtCommandLine.Location = new System.Drawing.Point(0, 379);
			this.txtCommandLine.Name = "txtCommandLine";
			this.txtCommandLine.Size = new System.Drawing.Size(400, 21);
			this.txtCommandLine.TabIndex = 2;
			this.txtCommandLine.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCommandLine_KeyPress);
			// 
			// pnlGameServerButtons
			// 
			this.pnlGameServerButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlGameServerButtons.Enabled = false;
			this.pnlGameServerButtons.Location = new System.Drawing.Point(0, 350);
			this.pnlGameServerButtons.Name = "pnlGameServerButtons";
			this.pnlGameServerButtons.Size = new System.Drawing.Size(400, 29);
			this.pnlGameServerButtons.TabIndex = 1;
			// 
			// txtDisplay
			// 
			this.txtDisplay.DefaultTitle = null;
			this.txtDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtDisplay.Location = new System.Drawing.Point(0, 0);
			this.txtDisplay.Name = "txtDisplay";
			this.txtDisplay.Size = new System.Drawing.Size(400, 350);
			this.txtDisplay.TabIndex = 0;
			// 
			// CommandLineDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.txtDisplay);
			this.Controls.Add(this.pnlGameServerButtons);
			this.Controls.Add(this.txtCommandLine);
			this.Name = "CommandLineDisplay";
			this.Size = new System.Drawing.Size(400, 400);
			this.ResumeLayout(false);

		}

		#endregion

		private ComboBox txtCommandLine;
		public LogStrDisplay txtDisplay;
		private FlowLayoutPanel pnlGameServerButtons;
	}
}
