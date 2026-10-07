using System.ComponentModel;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	public partial class LogStrDisplay {
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
			this.chckAutoScroll = new System.Windows.Forms.CheckBox();
			this.chkCompress = new System.Windows.Forms.CheckBox();
			this.btnClear = new System.Windows.Forms.Button();
			this.txtBox = new SteamEngine.RemoteConsole.ExtendedRichTextBox();
			this.SuspendLayout();
			// 
			// chckAutoScroll
			// 
			this.chckAutoScroll.AutoSize = true;
			this.chckAutoScroll.Checked = true;
			this.chckAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chckAutoScroll.Location = new System.Drawing.Point(3, 7);
			this.chckAutoScroll.Name = "chckAutoScroll";
			this.chckAutoScroll.Size = new System.Drawing.Size(72, 17);
			this.chckAutoScroll.TabIndex = 0;
			this.chckAutoScroll.Text = "Autoscroll";
			this.chckAutoScroll.UseVisualStyleBackColor = true;
			// 
			// chkCompress
			// 
			this.chkCompress.AutoSize = true;
			this.chkCompress.Checked = true;
			this.chkCompress.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkCompress.Location = new System.Drawing.Point(81, 7);
			this.chkCompress.Name = "chkCompress";
			this.chkCompress.Size = new System.Drawing.Size(140, 17);
			this.chkCompress.TabIndex = 1;
			this.chkCompress.Text = "Compress multiline entries";
			this.chkCompress.UseVisualStyleBackColor = true;
			// 
			// btnClear
			// 
			this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClear.AutoSize = true;
			this.btnClear.Location = new System.Drawing.Point(353, 3);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(44, 23);
			this.btnClear.TabIndex = 2;
			this.btnClear.Text = "Clear";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// txtBox
			// 
			this.txtBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtBox.BackColor = System.Drawing.SystemColors.Window;
			this.txtBox.DetectUrls = false;
			this.txtBox.Location = new System.Drawing.Point(0, 30);
			this.txtBox.Name = "txtBox";
			this.txtBox.ReadOnly = true;
			this.txtBox.Size = new System.Drawing.Size(400, 370);
			this.txtBox.TabIndex = 3;
			this.txtBox.Text = "";
			// 
			// LogStrDisplay
			// 
			this.Controls.Add(this.btnClear);
			this.Controls.Add(this.chkCompress);
			this.Controls.Add(this.chckAutoScroll);
			this.Controls.Add(this.txtBox);
			this.Name = "LogStrDisplay";
			this.Size = new System.Drawing.Size(400, 400);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ExtendedRichTextBox txtBox;
		private CheckBox chckAutoScroll;
		private CheckBox chkCompress;
		private Button btnClear;

	}
}
