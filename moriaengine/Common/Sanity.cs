/*
	Sanity.cs
    Copyright (C) 2004 Richard Dillingham and contributors (if any)

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

namespace SteamEngine.Common {
	public static class Sanity {
		//Sanity.IfTrueThrow sounds better than Debug.Assert, methinks. This throws a SanityCheckException, which should
		//be caught by what runs scripts, and marked as a probable script error, but elsewhere it should be marked
		//as a probable SE bug.
		[Conditional("TRACE")]
		public static void IfTrueThrow(bool b, string s) {
			if (b) {
				throw new SanityCheckException(s);
			}
		}

		[Conditional("TRACE")]
		public static void IfTrueThrow(bool b, LogStr s) {
			if (b) {
				throw new SanityCheckException(s);
			}
		}

		[Conditional("TRACE")]
		public static void IfTrueSay(bool b, string s) {
			if (b) {
				Logger.WriteError(s, new StackTrace(true));
			}
		}

		[Conditional("TRACE")]
		public static void IfTrueSay(bool b, LogStr s) {
			if (b) {
				Logger.WriteError(s, new StackTrace(true));
			}
		}

		[Conditional("TRACE")]
		public static void StackTraceIf(bool condition) {
			if (condition) {
				Logger.WriteError(new StackTrace(true));
			}
		}
	}
}