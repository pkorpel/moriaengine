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

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #509 - shared range check for "open my bank box via a banker" (double-click, BankerDClick.cs,
	/// and speech, BankerSpeech.cs). Deliberately its own fixed distance rather than reusing
	/// Globals.SpeechDistance - the user asked for this specific feature to work within range 6
	/// regardless of whatever SpeechDistance happens to be configured to for ordinary speech.
	/// </summary>
	internal static class BankerRange {
		internal const int MaxDistance = 6;

		internal static bool IsWithinRange(AbstractCharacter a, AbstractCharacter b) {
			return Point2D.GetSimpleDistance(a, b) <= MaxDistance;
		}
	}
}
