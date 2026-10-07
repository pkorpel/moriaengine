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

using System.Diagnostics.CodeAnalysis;

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

	// Platform-agnostic replacement for the color/weight info this used to carry via
	// System.Drawing.Color/Font/FontFamily - those pulled in a GDI+ dependency that
	// isn't available cross-platform, for information no in-scope console consumer
	// ever rendered as anything but plain text anyway.
	public readonly struct LogColor {
		public readonly byte R;
		public readonly byte G;
		public readonly byte B;

		public LogColor(byte r, byte g, byte b) {
			this.R = r;
			this.G = g;
			this.B = b;
		}

		public static readonly LogColor Black = new LogColor(0, 0, 0);
		public static readonly LogColor Red = new LogColor(255, 0, 0);
		public static readonly LogColor Gray = new LogColor(128, 128, 128);
		public static readonly LogColor Orange = new LogColor(255, 165, 0);
		public static readonly LogColor Blue = new LogColor(0, 0, 255);
		public static readonly LogColor Purple = new LogColor(128, 0, 128);
	}

	public class LogStyleInfo {
		private readonly LogColor textColor;
		private readonly bool bold;
		private readonly bool italic;
		private readonly bool isLink;

		public LogStyleInfo(LogColor color, bool bold, bool italic, bool isLink) {
			this.textColor = color;
			this.bold = bold;
			this.italic = italic;
			this.isLink = isLink;
		}

		public LogStyleInfo(LogColor color, bool bold)
			: this(color, bold, false, false) {
		}

		public bool IsLink {
			get { return this.isLink; }
		}

		public LogColor TextColor {
			get { return this.textColor; }
		}

		public bool Bold {
			get { return this.bold; }
		}

		public bool Italic {
			get { return this.italic; }
		}
	}

	public static class LogStrBase {
		internal const char separatorChar = '';
		internal const char eosChar = 'e';
		internal const char titleChar = 't';
		internal const char styleChar = 's';
		internal const string separatorString = "";
		internal const string titleString = "t";
		internal const string styleString = "s";
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		internal const string eosString = "e";

		internal static readonly LogColor defaultColor = LogColor.Black;

		private static readonly LogStyleInfo[] logStyles = {
			new LogStyleInfo(defaultColor, false), //LogStyles.Default
			new LogStyleInfo(LogColor.Red, false), //LogStyles.Warning
			new LogStyleInfo(LogColor.Red, false), //LogStyles.Error
			new LogStyleInfo(LogColor.Red, true), //LogStyles.Fatal
			new LogStyleInfo(LogColor.Red, true), //LogStyles.Critical
			new LogStyleInfo(LogColor.Gray, false), //LogStyles.Debug
			new LogStyleInfo(LogColor.Orange, false), //LogStyles.Highlight
			new LogStyleInfo(LogColor.Blue, true), //LogStyles.Ident
			new LogStyleInfo(LogColor.Blue, false, false, true), //LogStyles.FileLine
			new LogStyleInfo(defaultColor, false, true, true), //LogStyles.FilePos
			new LogStyleInfo(LogColor.Purple, false, false, true), //LogStyles.File
			new LogStyleInfo(LogColor.Blue, false) //LogStyles.Number
		};

		public static LogStyleInfo DefaultLogStyleInfo {
			get { return logStyles[(int) LogStyles.Default]; }
		}

		public static LogStyleInfo GetLogStyleInfo(LogStyles style) {
			return logStyles[(int) style];
		}
	}
}
