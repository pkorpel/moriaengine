/*
	This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;

namespace SteamEngine.Common {
	public enum LogStyles
	{
		Default = 0,
		Warning, Error, Fatal, Critical, Debug,
		Highlight, Ident, FileLine, FilePos, File, Number
	}

	public interface ILogStrDisplay {
		void Write(string data, LogStyleInfo style);
		void SetTitle(string data);
		void SetTitleToDefault();
	}

	public class LogStyleInfo : IDisposable {
		private readonly Color textColor;
		private readonly FontStyle fontStyle;
		private readonly FontFamily fontFamily;
		private readonly float fontSize;
		private readonly Font font;
		private readonly bool isLink;

		public LogStyleInfo(Color color, FontStyle fontStyle, FontFamily fontFamily, float fontSize, bool isLink) {
			this.textColor = color;
			this.fontStyle = fontStyle;
			this.fontFamily = fontFamily;
			this.fontSize = fontSize;
			this.isLink = isLink;
			this.font = new Font(this.fontFamily, this.fontSize, this.fontStyle);
		}

		public LogStyleInfo(Color color, FontStyle fontStyle)
			: this(color, fontStyle, LogStrBase.defaultFamily, LogStrBase.defaultSize, false) {

		}

		public bool IsLink {
			get { return this.isLink; }
		}

		public Color TextColor {
			get { return this.textColor; }
		}

		public FontStyle FontStyle {
			get { return this.fontStyle; }
		}

		public FontFamily FontFamily {
			get { return this.fontFamily; }
		}

		public float FontSize {
			get { return this.fontSize; }
		}

		public Font Font {
			get { return this.font; }
		}


		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		~LogStyleInfo() {
			this.Dispose(false);
		}

		protected virtual void Dispose(bool disposing) {
			this.font.Dispose();
		}
	}

	public static class LogStrBase {
		internal const char separatorChar = '\u001B';
		internal const char eosChar = 'e';
		internal const char titleChar = 't';
		internal const char styleChar = 's';
		internal const string separatorString = "\u001B";
		internal const string titleString = "t";
		internal const string styleString = "s";
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		internal const string eosString = "e";

		internal const float defaultSize = 8.25f;
		internal const FontStyle defaultFontStyle = FontStyle.Regular;
		internal static readonly FontFamily defaultFamily = new FontFamily(GenericFontFamilies.SansSerif);
		internal static readonly Color defaultColor = Color.Black;

		private static readonly LogStyleInfo[] logStyles = {
			new LogStyleInfo(defaultColor, defaultFontStyle, defaultFamily, defaultSize, false), //LogStyles.Default
			new LogStyleInfo(Color.Red, defaultFontStyle), //LogStyles.Warning
			new LogStyleInfo(Color.Red, defaultFontStyle), //LogStyles.Error
			new LogStyleInfo(Color.Red, FontStyle.Bold), //LogStyles.Fatal
			new LogStyleInfo(Color.Red, FontStyle.Bold), //LogStyles.Critical
			new LogStyleInfo(Color.Gray, defaultFontStyle), //LogStyles.Debug
			new LogStyleInfo(Color.Orange, defaultFontStyle), //LogStyles.Highlight
			new LogStyleInfo(Color.Blue, FontStyle.Bold), //LogStyles.Ident
			new LogStyleInfo(Color.Blue, defaultFontStyle, defaultFamily, defaultSize, true), //LogStyles.FileLine			
			new LogStyleInfo(defaultColor, FontStyle.Italic, defaultFamily, defaultSize, true), //LogStyles.FilePos
			new LogStyleInfo(Color.Purple, defaultFontStyle, defaultFamily, defaultSize, true), //LogStyles.File
			new LogStyleInfo(Color.Blue, defaultFontStyle) //LogStyles.Number			
		};

		public static LogStyleInfo DefaultLogStyleInfo {
			get { return logStyles[(int) LogStyles.Default]; }
		}

		public static LogStyleInfo GetLogStyleInfo(LogStyles style) {
			return logStyles[(int) style];
		}
	}
}