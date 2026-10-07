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

namespace SteamEngine {
	//Microsoft says to make enums names' singular, but to make bit-flag enums names' plural. So I've changed all
	//these plural ones to singular (I had added half of them myself anyways). -SL

	public enum LoginAttemptResult {
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Failed_NoSuchAccount,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Failed_BadPassword,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Failed_Blocked,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Failed_AlreadyOnline,
		Success
	}

	//This is actually used now, for Character's direction, and you pass this instead of a byte
	//to WalkRunOrFly and to the methods in OutPackets which take a direction as a parameter.
	//-SL
	[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
	public enum Direction : byte {
		North = 0, Default = North,
		NorthEast = 1,
		East = 2,
		SouthEast = 3,
		South = 4,
		SouthWest = 5,
		West = 6,
		NorthWest = 7, Mask = NorthWest
	}

	public enum StatLockType {
		Up = 0,
		Down = 1,
		Locked = 2
	}

	[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
	public enum SkillLockType : byte {
		Up = 0,
		Down = 1,
		Locked = 2
	}

	//One of the possible values that can be sent to the client when it requests to delete a character.
	//AcceptedRequest and RejectWithoutSendingAMessage are special codes which aren't sent to the client,
	//the others are all reasons for rejection which are sent. The name, after "Deny_", is exactly what
	//the client prints, but with spaces. (We send a number, but the client knows what it means)
	//-SL
	public enum DeleteCharacterResult {
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_InvalidCharacterPassword = 0,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_NonexistantCharacter = 1,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_CharacterIsBeingPlayedRightNow = 2,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_CharacterIsNotOldEnoughToDelete = 3,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_CharacterIsCurrentlyQueuedForBackup = 4,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_CouldntCarryOutRequest = 5,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_NoMessage = 6,
		Allow = 7
	}

	public enum PickupItemResult {
		//item manipulation denials
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_YouCannotPickThatUp = 0,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_ThatIsTooFarAway = 1,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_ThatIsOutOfSight = 2,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_ThatDoesNotBelongToYou = 3,	//you will have to steal it
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_YouAreAlreadyHoldingAnItem = 4,
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_RemoveFromView = 5,	//remove from view
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		Deny_NoMessage = 6,
		Allow = 7
		//[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		//Deny_ThatIsLocked = 8,
		//[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		//Deny_ContainerClosed = 9, //You cannot peek into the container.
	}

	public enum LoginDeniedReason {
		NoAccount = 0,
		SomeoneIsAlreadyUsingThisAccount = 1,
		Blocked = 2,
		InvalidAccountCredentials = 3,
		CommunicationsProblem = 4
	}
	//Used by AnimRequest (search on it, it was in Temporary at the time this comment was written, but might have
	//been implemented since then) - Actually, just search on RequestableAnims.
	//-SL
	public enum RequestableAnim {
		Bow,
		Salute
	}

	//Used by the core speech code.
	//-SL
	public enum SpeechType {
		Speech = 0,
		//If you send something of speechtype 1, it simply isn't displayed. Hmm. (Wild guess, GM only? Or maybe it really isn't displayed for anyone, heh.)
		Emote = 2,
		Server = 3,
		Name = 6,
		OnlyOne = 7,	//only one of these will be shown at a time. If you send another, the last one dissapears. It stays in the journal though.
		Whisper = 8,
		Yell = 9,
		Spell = 10,
		Encoded = 0xc0,
		Guild = 13,
		Alliance = 14,
		Command = 15
	}

	public enum SpeechResult {
		IgnoredOrActedUpon = 0,
		ActedUponExclusively = 1
	}

	//only 0 and 3 normally used
	public enum ClientFont {
		Server = 0, Wide = 0,
		ShadowFaceSmall = 1,
		ShadowFaceBig = 2,
		Regular = 3, Unified = 3, //ascii the same as unicode, more or less
		FancyTall = 4,
		FancyNoColor = 5,
		FancyBorderLess = 6,
		FancyShadowFace = 7,
		Runic = 8,
		BorderLess = 9
	}

	public enum TriggerResult {
		Continue = 0,
		Cancel = 1
	}

	public enum RenderModes {
		Opaque = 0,      // not_transparent (just as you see regular effects in uo)
		Black = 1,       // will be all black shape with no sighns of texture
		HardLighten = 2, // will be lightened, might lose some detail of original texture (good for flares)
		Lighten = 3,      // will be softly lightened, (great for smokes!)
		Transparent = 4  // Just regular trannparent sprite (about 50% alpha)
	}

	//Used by the core status-bar sending code.
	//-SL
	public enum StatusBarType {
		Other,
		Pet,
		Me
	}

	[Flags]
	public enum MovementType {
		None = 0,
		Walking = 1,
		Running = 2,
		RequestedStep = 4,
		Teleporting = 8
	}

	public enum Season {
		Spring = 0,
		Summer = 1,
		Fall = 2, Autumn = 2,
		Winter = 3,
		Dead = 4, Desolation = 4
	}

	public enum CursorType {
		Normal = 0,
		Gold = 1
	}

	public enum MoveRestriction {
		Normal = 0,
		Movable = 1
		//Immovable = 2	//Does not work
	}

	public enum MapTileType {
		Water = 0,
		Rock = 1,
		Grass = 2,
		Lava = 3,
		Dirt = 4,
		Other = 5
	}

	public enum HighlightColor {
		NoColor = 0,		//This draws them as color 0 (the default color). You can't change this color in the client.

		//Name = number		//default color						The description in the client (Options->Reputation System (3rd tab down on the right))
		Innocent = 1,		//Blue by default.					("Innocent highlight color")
		Allied = 2,			//Green by default.					("Friendly guilds highlight color")
		Attackable = 3,		//Grey by default.					("Someone that can be attacked color")
		Criminal = 4,		//Also grey by default.				("Criminal highlight color")
		Enemy = 5,			//Orangeish-brown by default.		("Enemy guildmembers highlight color")
		Murderer = 6,		//Red by default.					("Murderer highlight color")

		Invulnerable = 7, Yellow = 7, //You can't change this color in the client.
		Transparent = 8	//Makes their status bar look really bizarre, and makes them turn transparent-black when highlighted.
	}

	public enum ClientType {
		Iris,
		Osi2D,
		Osi3D,
		OsiGod,
		PlayUO,
		Palanthir,
		Unknown
	}
}