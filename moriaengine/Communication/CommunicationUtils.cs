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

using System.Diagnostics;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Communication {
	public static class CommunicationUtils {

		[Conditional("DEBUG")]
		public static void OutputPacketLog(byte[] array, int len) {
			OutputPacketLog(array, 0, len);
		}

		[Conditional("DEBUG")]
		public static void OutputPacketLog(byte[] array, int start, int len) {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Packet Contents: (" + len + " bytes)");
			for (int i = start, n = start + len; i < n; i++) {
				sb.Append(array[i].ToString("X2")).Append(" ");
				if (i % 10 == 0) {
					sb.AppendLine();
				}
			}
			sb.AppendLine();

			for (int i = start, n = start + len; i < n; i++) {
				byte a = array[i];
				if (a < 32 || a > 126) {
					sb.Append((char) 128);
				} else {
					sb.Append((char) a);
				}
				sb.Append(" ");

				if (i % 10 == 0) {
					sb.AppendLine();
				}
			}
			Logger.WriteDebug(sb);
		}
	}
}