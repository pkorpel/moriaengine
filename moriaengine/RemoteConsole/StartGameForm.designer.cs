using System.ComponentModel;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	partial class StartGameForm {
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			this.btnStart = new System.Windows.Forms.Button();
			this.ddServer = new System.Windows.Forms.ComboBox();
			this.gameServerEntryBindingSource = new System.Windows.Forms.BindingSource(this.components);
			this.ddBuild = new System.Windows.Forms.ComboBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize) (this.gameServerEntryBindingSource)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(12, 15);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(96, 13);
			label1.TabIndex = 0;
			label1.Text = "Gameserver name:";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point(12, 94);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(33, 13);
			label2.TabIndex = 6;
			label2.Text = "Build:";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(36, 42);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(71, 13);
			label3.TabIndex = 2;
			label3.Text = "Remote path:";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(36, 68);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(29, 13);
			label4.TabIndex = 4;
			label4.Text = "Port:";
			// 
			// btnStart
			// 
			this.btnStart.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.btnStart.Location = new System.Drawing.Point(148, 125);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(82, 23);
			this.btnStart.TabIndex = 8;
			this.btnStart.Text = "Start";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// ddServer
			// 
			this.ddServer.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ddServer.DataSource = this.gameServerEntryBindingSource;
			this.ddServer.DisplayMember = "DisplayText";
			this.ddServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ddServer.FormattingEnabled = true;
			this.ddServer.Location = new System.Drawing.Point(125, 12);
			this.ddServer.Name = "ddServer";
			this.ddServer.Size = new System.Drawing.Size(244, 21);
			this.ddServer.TabIndex = 1;
			this.ddServer.ValueMember = "Number";
			// 
			// gameServerEntryBindingSource
			// 
			this.gameServerEntryBindingSource.DataSource = typeof(SteamEngine.RemoteConsole.SendServersToStartPacket.GameServerEntry);
			this.gameServerEntryBindingSource.CurrentChanged += new System.EventHandler(this.gameServerEntryBindingSource_CurrentChanged);
			// 
			// ddBuild
			// 
			this.ddBuild.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ddBuild.DisplayMember = "Description";
			this.ddBuild.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ddBuild.FormattingEnabled = true;
			this.ddBuild.Location = new System.Drawing.Point(125, 91);
			this.ddBuild.Name = "ddBuild";
			this.ddBuild.Size = new System.Drawing.Size(244, 21);
			this.ddBuild.TabIndex = 7;
			this.ddBuild.ValueMember = "TValue";
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.gameServerEntryBindingSource, "IniPath", true));
			this.textBox1.Location = new System.Drawing.Point(125, 39);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(244, 20);
			this.textBox1.TabIndex = 3;
			// 
			// textBox2
			// 
			this.textBox2.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.gameServerEntryBindingSource, "Port", true));
			this.textBox2.Location = new System.Drawing.Point(125, 65);
			this.textBox2.Name = "textBox2";
			this.textBox2.ReadOnly = true;
			this.textBox2.Size = new System.Drawing.Size(105, 20);
			this.textBox2.TabIndex = 5;
			// 
			// StartGameForm
			// 
			this.AcceptButton = this.btnStart;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(381, 160);
			this.Controls.Add(label4);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(label3);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.btnStart);
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.Controls.Add(this.ddBuild);
			this.Controls.Add(this.ddServer);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(277, 137);
			this.Name = "StartGameForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Remote Gameserver starting";
			this.Load += new System.EventHandler(this.StartGameForm_Load);
			((System.ComponentModel.ISupportInitialize) (this.gameServerEntryBindingSource)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ComboBox ddServer;
		private ComboBox ddBuild;
		private TextBox textBox1;
		private TextBox textBox2;
		private Button btnStart;
		private BindingSource gameServerEntryBindingSource;
	}
}