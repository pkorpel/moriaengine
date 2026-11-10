using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class LogStrDisplay : UserControl, ILogStrDisplay {
		string title;
		string defaultTitle;

		LogStrParser parser;

		delegate void InternalWriteDelegate(string str);
		InternalWriteDelegate internalWriteDeleg;

		private const string contractedSign = "[+]";

		List<string> contractedTexts = new List<string>();

		public LogStrDisplay() {
			this.InitializeComponent();
			this.internalWriteDeleg = this.InternalWriteInUIThread;
			this.parser = new LogStrParser(this);
			this.txtBox.LinkClicked += this.txtBox_LinkClicked;
		}

		void txtBox_LinkClicked(object sender, LinkClickedEventArgs e) {
			string filename;
			int line;
			if (this.TryUncontract(e.LinkText)) {
				return;
			}

			if (!LogStrParser.TryParseFileLine(e.LinkText, out filename, out line)) {
				filename = e.LinkText;
			}
			string ext = Path.GetExtension(filename);

			filename = LogStrParser.TranslateToLocalPath(filename);

			string exe, args;
			if (Settings.GetCommandLineForExt(ext, out exe, out args)) {
				args = string.Format(args, filename, line);
				// #29: .NET 8 starts processes without the shell by default; the shell also finds programs registered without a path (App Paths).
				Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true });
			} else {
				// and only the shell opens a file with its program.
				Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
			}
		}

		public void WriteThreadSafe(string logStrEncoded) {
			this.Invoke(this.internalWriteDeleg, logStrEncoded);
		}

		public void WriteLineThreadSafe(string logStrEncoded) {
			this.Invoke(this.internalWriteDeleg, logStrEncoded + Environment.NewLine);
		}

		public void WriteThreadSafe(LogStr data) {
			this.Invoke(this.internalWriteDeleg, data.RawString);
		}

		public void Write(string logStrEncoded) {
			this.InternalWriteInUIThread(logStrEncoded);
		}

		public void WriteLine(string logStrEncoded) {
			this.InternalWriteInUIThread(logStrEncoded + Environment.NewLine);
		}

		public void Write(LogStr data) {
			this.InternalWriteInUIThread(data.RawString);
		}

		private void InternalWriteInUIThread(string str) {
			//this.txtBox.BeginUpdate();
			int prevLen = this.txtBox.TextLength;
			this.parser.ProcessLogStr(str);

			if (this.chckAutoScroll.Checked) {
				this.txtBox.ScrollToBottom();
			}

			if (this.chkCompress.Checked) {				
				this.TryContract(prevLen);				
			}
			//this.txtBox.EndUpdate();
		}

		private void TryContract(int prevLen) {
#if MSWIN
			int textLength = this.txtBox.TextLength;
			int currentLine = this.txtBox.GetLineFromCharIndex(prevLen);
			int firstNextLine = this.txtBox.GetFirstCharIndexFromLine(currentLine + 1);
			if (firstNextLine > -1) {
				int len = textLength - firstNextLine;
				this.txtBox.Select(firstNextLine - 1, len + 1);
				if (this.txtBox.SelectedText.Trim().Length > 0) {
					string rtf = this.txtBox.SelectedRtf;
					this.contractedTexts.Add(rtf);
					this.txtBox.SelectedText = " ";
					this.txtBox.InsertLink(contractedSign, string.Concat(this.contractedTexts.Count - 1));
					this.txtBox.AppendText(Environment.NewLine);
				}
			}
#endif
		}

		private bool TryUncontract(string linkText) {
#if MSWIN
			if (linkText.StartsWith(contractedSign+"#")) {
				string indexStr = linkText.Substring(4);
				int i = int.Parse(indexStr);
				string rtf = this.contractedTexts[i];
				this.contractedTexts[i] = null;

				string contractedSignDecorated = " " + contractedSign;
				int signLen = contractedSignDecorated.Length;
				int selectionStart = this.txtBox.Text.IndexOf(contractedSignDecorated, this.txtBox.CurrentMouseCharIndex - signLen);
				this.txtBox.Select(selectionStart, signLen + 1 + Environment.NewLine.Length);
				this.txtBox.SelectedText = " "; //erases the [+]

				if (!string.IsNullOrEmpty(rtf)) {
					this.txtBox.SelectedRtf = rtf;
				}
				return true;
			}
#endif
			return false;
		}

		public string Title {
			get {
				if (string.IsNullOrEmpty(this.title)) {
					return this.defaultTitle;
				}
				return this.title;
			}
		}

		public string DefaultTitle {
			get {
				return this.defaultTitle;
			}
			set {
				this.defaultTitle = value;
				if (string.IsNullOrEmpty(this.title)) {
					this.OnTitleChanged();
				}
			}
		}

		// #29: the .NET 8 port took System.Drawing out of Common, so LogStyleInfo only says bold/italic; the fonts are made here, as Common made them before.
		private static readonly Font[] styleFonts = new Font[4];

		private static Font FontOf(LogStyleInfo style) {
			int index = (style.Bold ? 1 : 0) | (style.Italic ? 2 : 0);
			Font font = styleFonts[index];
			if (font == null) {
				FontStyle fontStyle = (style.Bold ? FontStyle.Bold : FontStyle.Regular) | (style.Italic ? FontStyle.Italic : FontStyle.Regular);
				font = new Font(FontFamily.GenericSansSerif, 8.25f, fontStyle);
				styleFonts[index] = font;
			}
			return font;
		}

		public void Write(string data, LogStyleInfo style) {
			int dataLen = data.Length;
			if (dataLen > 0) {
				//while (this.txtBox.TextLength + dataLen > this.txtBox.MaxLength) {
				//    this.txtBox.Text = this.txtBox.Text.Substring(this.txtBox.Text.IndexOf(Environment.NewLine) + Environment.NewLine.Length);
				//}

				this.txtBox.SelectionFont = FontOf(style);
				this.txtBox.SelectionColor = Color.FromArgb(style.TextColor.R, style.TextColor.G, style.TextColor.B);

				if (style.IsLink) {
#if MSWIN
					this.txtBox.InsertLink(data);
#else
					this.txtBox.AppendText(data);
#endif
				} else {
					this.txtBox.AppendText(data);
				}
			}
		}

		public void SetTitle(string data) {
			this.title = data;
			this.OnTitleChanged();
		}

		public void SetTitleToDefault() {
			if (!string.IsNullOrEmpty(this.title)) {
				this.title = null;
				this.OnTitleChanged();
			}
		}

		public event EventHandler TitleChanged;

		protected virtual void OnTitleChanged() {
			EventHandler handler = this.TitleChanged;
			if (handler != null) {
				handler(this, EventArgs.Empty);
			}
		}

		private void btnClear_Click(object sender, EventArgs e) {
			this.ClearText();
		}

		public void ClearText() {
			this.txtBox.Clear();
			this.contractedTexts.Clear();
		}
	}
}




