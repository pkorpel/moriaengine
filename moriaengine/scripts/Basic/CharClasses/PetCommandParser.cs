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

namespace SteamEngine.CompiledScripts {
	public enum PetCommand {
		None,
		Come,     //"come", "follow me"
		GuardMe,  //"guard me", "defend me"
		Guard,    //"guard" + target an item to guard, a character to protect, or yourself (= guard me)
		Stay,     //"stay", "stay here", "stop"
		Follow,   //"follow" + target a character
		Go,       //"go" + target a spot
		Kill,     //"kill", "attack" + target a character
		Transfer, //"transfer" + target the new owner
		Release,  //"release", "dismiss"
		Status,   //"status"
		Friend,   //"friend" + target a player who may give the pet orders too
		Drop,     //"drop" - empty the pet's backpack onto the ground
	}

	/// <summary>
	/// #768 - parses pet-command speech the way Sphere 0.99z8 does (EreborNext CChar.PetCommands.cs):
	/// "&lt;addressee&gt; &lt;command phrase&gt;", where the addressee is "all"/"pets" or a pet's own
	/// name or creature type name, and the phrase can be several words ("follow me", "guard me",
	/// "stay here"). The old parser only looked at the last word, so every multi-word command
	/// was silently ignored. Pure - no engine state - so it's unit-tested directly.
	/// "guard" asks for a target like Sphere's does (an item to guard); targeting a character
	/// protects that character instead, and targeting yourself is the same as "guard me".
	/// </summary>
	public static class PetCommandParser {
		//longest phrases first, so "follow me" wins over "follow" and "guard me" over "guard"
		private static readonly string[] phrases = {
			"follow me", "guard me", "defend me", "stay here",
			"come", "guard", "stay", "stop", "follow", "go", "kill", "attack", "transfer", "release", "dismiss", "status",
			"friend", "drop",
		};
		private static readonly PetCommand[] commands = {
			PetCommand.Come, PetCommand.GuardMe, PetCommand.GuardMe, PetCommand.Stay,
			PetCommand.Come, PetCommand.Guard, PetCommand.Stay, PetCommand.Stay, PetCommand.Follow, PetCommand.Go, PetCommand.Kill, PetCommand.Kill, PetCommand.Transfer, PetCommand.Release, PetCommand.Release, PetCommand.Status,
			PetCommand.Friend, PetCommand.Drop,
		};

		/// <summary>Splits speech into addressee and command. False when the speech doesn't end with
		/// a known command phrase or has nothing before it - i.e. it's just normal speech.</summary>
		public static bool TryParse(string speech, out string addressee, out PetCommand command) {
			addressee = null;
			command = PetCommand.None;
			if (string.IsNullOrWhiteSpace(speech)) {
				return false;
			}
			string text = NormalizeSpaces(speech);
			for (int i = 0; i < phrases.Length; i++) {
				string phrase = phrases[i];
				if ((text.Length > phrase.Length) && text.EndsWith(phrase, StringComparison.OrdinalIgnoreCase) &&
						(text[text.Length - phrase.Length - 1] == ' ')) {
					addressee = text.Substring(0, text.Length - phrase.Length - 1);
					command = commands[i];
					return true;
				}
			}
			return false;
		}

		public static bool IsAllPets(string addressee) {
			return string.Equals(addressee, "all", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(addressee, "pets", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Does the addressee name this pet? Compared against its own name and its creature
		/// type name ("horse come" for any owned horse), ignoring case and a leading "a"/"an"/"the".</summary>
		public static bool NameMatches(string addressee, string name) {
			if (string.IsNullOrWhiteSpace(addressee) || string.IsNullOrWhiteSpace(name)) {
				return false;
			}
			string a = StripArticle(NormalizeSpaces(addressee));
			string n = StripArticle(NormalizeSpaces(name));
			return string.Equals(a, n, StringComparison.OrdinalIgnoreCase);
		}

		private static string StripArticle(string text) {
			foreach (string article in new[] { "a ", "an ", "the " }) {
				if ((text.Length > article.Length) && text.StartsWith(article, StringComparison.OrdinalIgnoreCase)) {
					return text.Substring(article.Length);
				}
			}
			return text;
		}

		private static string NormalizeSpaces(string text) {
			return string.Join(" ", text.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries));
		}
	}
}
