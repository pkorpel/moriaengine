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
using SteamEngine.Common;

namespace SteamEngine {
	//This class has methods which are called when various packets are recieved from clients.
	//These methods exist to note things that should be implemented later, but aren't yet.
	//If you want to implement something that is in here, go right ahead, but do a search on the method name
	//you're replacing to find out what calls it, and change the calls to point to your new method
	//(And then delete or comment out the method here).

	//(I'm basically going through and adding a bunch of packets without worrying about
	// making systems to hook them up to, etc.)

	//Also, at the bottom of this file, outside the class itself, there is a list of new methods in OutPackets.cs
	//which can be used to send information to the client. The listed methods aren't actually used by anything presently,
	//but that's what the list is for - so nobody accidentally reimplements a packet-sending method which already exists
	//and is only waiting for someone to come along and use it.

	//-SL

	//Most of these methods will print debug information when they are called, and all of them have extensive
	//documentation to aid implementation. I recommend keeping the documentation, and existing parameters and
	//return values, to make things as easy for you as possible.

	//Sanity checks exist to verify assumptions. One good guideline is not to assume something that isn't tested
	//by a sanity check. If you're going to assume something which isn't checked, add a sanity check for it.
	//Sanity checks only exist in the debug build, so they won't slow down the release builds.

	internal static class Temporary {
		/*
			Method: UseSpellNumberRequest
				This is called when a client requests to cast a particular spell.
				You need to check for invalid skillNumber values. The code in InPackets
				doesn't know how many skills there are, and so does not validate the skill number.
			Parameters:
				cre - The character making the request.
				spellNumber - The ID# of the spell, with 1 being clumsy, etc.
					You need to check for invalid spellNumber values. The code in InPackets
					doesn't know how many spells there are, and so does not validate the spell number.
		*/
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UseSpellNumberRequest(AbstractCharacter cre, int spellNumber) {
			Sanity.IfTrueThrow(cre == null, "Character is null in UseSpellNumberRequest.");
			Logger.WriteWarning("Unimplemented: UseSpellNumberRequest called on " + cre + " for spell " + spellNumber);
		}

		/*
			Method: UseLastSpellRequest
				This is called when a client requests to use their last used spell again.
				
			Parameters:
				cre - The character making the request.
		*/
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UseLastSpellRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre == null, "Character is null in UseLastSpellRequest.");
			Logger.WriteWarning("Unimplemented: UseLastSpellRequest called on " + cre + ".");
		}

		/*
			Method: UseLastSpellRequest
				This is called when a client uses the macro command 'OpenDoor.'
				Presumably, they want to open any nearby door, though the client doesn't
				check for any. This does not check to make sure they are alive.
				
			Parameters:
				cre - The character making the request.
		*/
		internal static void OpenDoorMacroRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre == null, "Character is null in UseLastSpellRequest.");
			Logger.WriteWarning("Unimplemented: OpenDoorMacroRequest called on " + cre + ".");
		}

		/*
			Method: AnimRequest
				This is called when the client requests to bow, salute, etc. The client sends
				a whole string, but this works on the assumption that the only valid anim names are
				"bow" and "salute", so it only checks the first letter. (UO3D (if it isn't blocked!)
				sends its anims in a different packet) (The debug build looks at the string
				and treats anything other than "bow" or "salute" as a suspicious error - which is likely to
				get someone's attention if they do it and get kicked, which should get them to report
				it as a bug, hopefully, which would be what it is (assuming they hadn't hacked the client
				to send "flurble" or something))
			
			Parameters:
				cre - The character making the request.
				anim - The anim requested, that is RequestableAnim.Bow or RequestableAnim.Salute. 
		*/
		internal static void AnimRequest(AbstractCharacter cre, RequestableAnim anim) {
			Sanity.IfTrueThrow(cre == null, "Character is null in AnimRequest.");
			Logger.WriteWarning("Unimplemented: AnimRequest called on " + cre + ". Requested '" + anim + "'.");
		}

		/*
			Method: UsePrimaryAbilityRequest
				This is called when a player uses the "PrimaryAbility" macro (or presumably
				double-clicks the primary ability button, though I haven't shown one yet so
				I can't try it yet).
			
			Parameters:
				cre - The character making the request.
		*/
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UsePrimaryAbilityRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre == null, "Character is null in UsePrimaryAbilityRequest.");
			Logger.WriteWarning("Unimplemented: UsePrimaryAbilityRequset for " + cre + ".");
		}

		/*
			Method: UseSecondaryAbilityRequest
				This is called when a player uses the "SecondaryAbility" macro (or presumably
				double-clicks the secondary ability button, though I haven't shown one yet so
				I can't try it yet).
			
			Parameters:
				cre - The character making the request.
		*/
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UseSecondaryAbilityRequest(AbstractCharacter cre) {
			Sanity.IfTrueThrow(cre == null, "Character is null in UseSecondaryAbilityRequest.");
			Logger.WriteWarning("Unimplemented: UseSecondaryAbilityRequest for " + cre + ".");
		}
	}


	/*
(SL)	See docs/packets.txt for information on the methods you can call in PacketSender to send
		or prepare to send various things. Comments do not yet exist for most of those methods, however.
		
		Additionally, look at methods on Thing, Character, Item, etc, such as Sound, Yell, SendUpdateStats,
		SendUpdateHitpoints, etc.
		
		
		Other methods which would be useful for something or other:
			AbstractCharacter's MakeBePlayer and MakeBeNonPlayer would be useful for the 'control' (possess) command,
				and anything else which needs that sort of effect.
	*/

}
