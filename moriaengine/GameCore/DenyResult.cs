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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;

namespace SteamEngine {

	public abstract class DenyResult {
		private bool allow;

		public DenyResult() {
			//this.allow = false;
		}

		public DenyResult(bool allow) {
			this.allow = allow;
		}

		public bool Allow {
			get { return this.allow; }
		}

		public void SendDenyMessage(AbstractCharacter ch) {
			GameState state = ch.GameState;
			if (state != null) {
				this.SendDenyMessage(ch, state, state.Conn);
			}
		}

		public abstract void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn);

		#region convenience methods
		public void SendDenyMessage(AbstractCharacter ch, TcpConnection<GameState> conn, GameState state) {
			this.SendDenyMessage(ch, state, conn);
		}

		public void SendDenyMessage(AbstractCharacter ch, GameState state) {
			if (state != null) {
				this.SendDenyMessage(ch, state, state.Conn);
			}
		}

		public void SendDenyMessage(AbstractCharacter ch, TcpConnection<GameState> conn) {
			if (conn != null) {
				this.SendDenyMessage(ch, conn.State, conn);
			}
		}

		public void SendDenyMessage(TcpConnection<GameState> conn, GameState state) {
			if (state != null) {
				this.SendDenyMessage(state.Character, state, conn);
			}
		}

		public void SendDenyMessage(GameState state, TcpConnection<GameState> conn) {
			if (state != null) {
				this.SendDenyMessage(state.Character, state, conn);
			}
		}
		#endregion convenience methods
	}

	public class DenyResult_StringSysMessage : DenyResult {
		private readonly string message;

		public DenyResult_StringSysMessage(string message) {
			this.message = message;
		}

		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
			if (!string.IsNullOrEmpty(this.message)) {
				PacketSequences.SendSystemMessage(conn, this.message, -1);
			}
		}
	}

	public class DenyResult_ClilocSysMessage : DenyResult {
		private readonly int message;
		private readonly int color;

		public DenyResult_ClilocSysMessage(int message) {
			this.message = message;
			this.color = -1;
		}

		public DenyResult_ClilocSysMessage(int message, int color) {
			this.message = message;
			this.color = color;
		}

		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
			PacketSequences.SendClilocSysMessage(conn, this.message, this.color);
		}
	}

	public class DenyResult_Allow : DenyResult {

		public DenyResult_Allow()
			: base(true) {
		}

		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
			Logger.WriteWarning("Can't send DenyMessage when the result is Allow.", 
				new StackTrace());
		}
	}
	
	public class DenyResult_NoMessage : DenyResult {
		public override void SendDenyMessage(AbstractCharacter ch, GameState state, TcpConnection<GameState> conn) {
		}
	}

	public static class DenyResultMessages {
	    //item manipulation denials
	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_YouCannotPickThatUp = 
	        new DenyResult_ClilocSysMessage(3000267); //You cannot pick that up.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatIsTooFarAway = //do we want to tell players what's invisible and what's far away?
	        new DenyResult_ClilocSysMessage(3000268);	//That is too far away.

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatDoesntExist = //do we want to tell players what's invisible and what's far away?
			new DenyResult_ClilocSysMessage(3000268);	//That is too far away.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatIsInvisible = //do we want to tell players what's invisible and what's far away?
			new DenyResult_ClilocSysMessage(3000268);	//That is too far away.

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatIsOutOfLOS = //do we want to tell players what's invisible and what's far away?
			new DenyResult_ClilocSysMessage(3000269);	//That is out of sight.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatDoesNotBelongToYou = 
	        new DenyResult_ClilocSysMessage(500364);		//You can't use that, it belongs to someone else.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_YouAreAlreadyHoldingAnItem = 
	        new DenyResult_ClilocSysMessage(3000271);	//You are already holding an item.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_NoMessage = new DenyResult_NoMessage();

		public static readonly DenyResult Allow = new DenyResult_Allow();

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ThatIsLocked = 
	        new DenyResult_ClilocSysMessage(501283);		//That is locked.

	    [SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public static readonly DenyResult Deny_ContainerClosed =
			new DenyResult_ClilocSysMessage(500209);		//You cannot peek into the container.
		//SendSystemMessage(c, "Tento kontejner není otevøený.", 0);
	}
}
