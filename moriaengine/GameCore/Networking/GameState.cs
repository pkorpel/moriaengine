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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Regions;

namespace SteamEngine.Networking {

	public delegate void OnTargon(GameState state, IPoint3D getback, object parameter);
	public delegate void OnTargonCancel(GameState state, object parameter);

	public delegate void MenuRespose(GameState state, int index, object parameter);
	public delegate void MenuCancel(GameState state, object parameter);

	public class GameState : TagHolder, //IDisposable, 
		IConnectionState<TcpConnection<GameState>, GameState, IPEndPoint> {

		internal readonly MovementState movementState;

		public GameState() {
			this.movementState = new MovementState(this);

			this.encryption = new GameEncryption();
			this.uid = uids++;
		}

		#region Uid
		private static int uids;
		private int uid;

		public int Uid {
			get {
				return this.uid;
			}
		}
		#endregion Uid

		#region AllShow
		private bool allShow;

		public bool AllShow {
			get {
				return this.allShow;
			}
			set {
				if (value != this.allShow) {
					this.allShow = value;
					if (this.character != null) {
						this.character.Resync();
					}
				}
			}
		}
		#endregion AllShow

		#region Character, Account, Login, IConnection

		private IEncryption encryption;

		private AbstractAccount account;
		private AbstractCharacter character;
		private IPEndPoint ip;
		private TcpConnection<GameState> conn;


		internal void SetLoggedIn(AbstractAccount acc) {
			this.account = acc;
		}

		public AbstractAccount Account {
			get {
				return this.account;
			}
		}

		public IEncryption Encryption {
			get {
				return this.encryption;
			}
		}

		public ICompression Compression {
			get {
				return GameCompression.instance; //GameCompression is stateless
			}
		}

		public TcpConnection<GameState> Conn {
			get {
				return this.conn;
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "conn")]
		public void On_Init(TcpConnection<GameState> conn) {
			GameServer.On_ClientInit(this);

			this.conn = conn;
			this.ip = conn.EndPoint;

			Console.WriteLine(LogStr.Ident(this.ToString()) + (" connected from " + conn.EndPoint));

			Globals.Instance.TryTrigger(TriggerKey.clientAttach, new ScriptArgs(this, this.conn)); // 
		}

		public void On_Close(string reason) {
			Console.WriteLine(LogStr.Ident(this.ToString()) + LogStr.Warning(" closed: ") + reason);

			GameServer.On_ClientClose(this);


			if (this.character != null) {
				if (!this.character.IsDeleted) {
					this.character.Trigger_LogOut();
				}
				this.character = null;
			}
			if (this.account != null) {
				this.account.SetLoggedOut();
				this.account = null;
			}
		}

		public bool IsLoggedIn {
			get {
				return this.account != null;
			}
		}

		//this is called by InPackets.LoginChar
		internal void LoginCharacter(int charSlot) {
			Sanity.IfTrueThrow(charSlot < 0 || charSlot >= AbstractAccount.maxCharactersPerGameAccount, "charSlot < 0 || charSlot >= AbstractAccount.maxCharactersPerGameAccount, charSlot = " + charSlot);
			Sanity.IfTrueThrow(this.account == null, "this.account == null");

			if (this.character != null) {
				this.conn.Close("Character login with a character already logged, wtf?!");
				return;
			}

			AbstractCharacter ch = this.account.GetLingeringCharacter();
			if (ch == null) {//if we've already a lingering char on this acc, we ignore the selection and login the lingering one
				//otherwise, find the one in slot
				ch = this.account.GetCharacterInSlot(charSlot);
			}
			if (ch == null) {
				PreparedPacketGroups.SendLoginDenied(this.conn, LoginDeniedReason.CommunicationsProblem);
				this.conn.Close("Client tried to choose nonexisting character.");
				return;
			}

			this.character = ch;
			if (ch.TryLogIn()) {	//returns true for success
				if (ch.Account == null || ch.Account != this.account) {
					Logger.WriteWarning("Invalid account - Fixing.");
					ch.MakeBePlayer(this.account);
				}

				//PreparedPacketGroups.SendClientVersionQuery(this.conn);

				PacketSequences.DelayedLoginTimer timer = new PacketSequences.DelayedLoginTimer(ch);
				timer.DueInSeconds = 0;
				timer.PeriodInSeconds = 0.5;
			} else {
				this.character = null;//we don't want @logout called when @login was cancelled

				PreparedPacketGroups.SendLoginDenied(this.conn, LoginDeniedReason.CommunicationsProblem);
				this.conn.Close("Login denied by scripts or no character in that slot.");
			}
		}

		public AbstractCharacter Character {
			get {
				return this.character;
			}
		}

		public AbstractCharacter CharacterNotNull {
			get {
				if (this.character == null) {
					throw new SEException("There is no character set for this " + this);
				}
				return this.character;
			}
		}

		public bool PacketGroupsJoiningAllowed {
			get {
				return false;
			}
		}
		#endregion Character, Account, Login, IConnection

		#region ClientVersion
		private ClientVersion clientVersion = ClientVersion.nullValue;

		public ClientVersion Version {
			get {
				return this.clientVersion;
			}
		}

		internal void InternalSetClientVersion(ClientVersion value) {
			this.clientVersion = value;
		}
		#endregion ClientVersion

		#region Update/Vision Range
		private int lastSentUpdateRange = Globals.MaxUpdateRange;
		private byte requestedUpdateRange = Globals.MaxUpdateRange;

		internal byte RequestedUpdateRange {
			get {
				return this.requestedUpdateRange;
			}
			set {
				this.requestedUpdateRange = value;
				this.SyncUpdateRange();
			}
		}

		public int UpdateRange {
			get {
				return this.lastSentUpdateRange;
			}
		}

		public void SyncUpdateRange() {
			if (this.character != null) {
				int oldUpdateRange = this.lastSentUpdateRange;
				this.RecalcUpdateRange();
				if (oldUpdateRange != this.lastSentUpdateRange) {
					ClientViewRangeOutPacket packet = Pool<ClientViewRangeOutPacket>.Acquire();
					packet.Prepare(this.lastSentUpdateRange);
					this.conn.SendSinglePacket(packet);

					//if new > old, we send the stuff previously invisible
					if (this.lastSentUpdateRange > oldUpdateRange) {
						this.character.SendNearbyStuff();
						//could be optimalized but... how often do you change vision range anyway ;)
					}
				}
			}
		}

		private void RecalcUpdateRange() {
			int visionRange = this.character.VisionRange;
			if (visionRange <= this.requestedUpdateRange) {
				if (visionRange < 0) {
					this.lastSentUpdateRange = 0;
				} else if (visionRange > Globals.MaxUpdateRange) {
					this.lastSentUpdateRange = Globals.MaxUpdateRange;
				} else if (visionRange < Globals.MinUpdateRange) {
					this.lastSentUpdateRange = Globals.MinUpdateRange;
				} else {
					this.lastSentUpdateRange = visionRange;
				}
			} else {
				this.lastSentUpdateRange = this.requestedUpdateRange;
			}
		}
		#endregion Update/Vision Range

		#region Target
		private OnTargon targonDeleg;
		private OnTargonCancel targonCancelDeleg;
		private object targonParameters;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonParameters"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonDeleg"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonCancelDeleg")]
		public void Target(bool ground, OnTargon targonDeleg, OnTargonCancel targonCancelDeleg, object targonParameters) {
			this.targonDeleg = targonDeleg;
			this.targonCancelDeleg = targonCancelDeleg;
			this.targonParameters = targonParameters;
			PreparedPacketGroups.SendTargettingCursor(this.conn, ground);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonParameters"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonDeleg"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targonCancelDeleg")]
		public void TargetForMultis(int model, OnTargon targonDeleg, OnTargonCancel targonCancelDeleg, object targonParameters) {
			this.targonDeleg = targonDeleg;
			this.targonCancelDeleg = targonCancelDeleg;
			this.targonParameters = targonParameters;
			GiveBoatOrHousePlacementViewOutPacket packet = Pool<GiveBoatOrHousePlacementViewOutPacket>.Acquire();
			packet.Prepare(model);
			this.conn.SendSinglePacket(packet);
		}

		internal void HandleTarget(bool targGround, uint targetUid, ushort x, ushort y, sbyte z, ushort model) {
			Logger.WriteDebug("HandleTarget: TG=" + targGround + " uid=" + targetUid + " x=" + x + " y=" + y + " z=" + z + " dispId=" + model);
			//figure out what it is
			object parameter = this.targonParameters;
			this.targonParameters = null;
			OnTargonCancel targonCancel = this.targonCancelDeleg;
			this.targonCancelDeleg = null;
			OnTargon targ = this.targonDeleg;
			this.targonDeleg = null;
			AbstractCharacter self = this.CharacterNotNull;

			if (x == 0xffff && y == 0xffff && targetUid == 0 && z == 0 && model == 0) {
				//cancel
				if (targonCancel != null) {
					targonCancel(this, parameter);
				}
				return;
			}
			if (targ != null) {
				if (!targGround) {
					Thing thing = Thing.UidGetThing(targetUid);
					if (thing != null) {
						if (self.CanSeeForUpdate(thing).Allow) {
							targ(this, thing, parameter);
							return;
						}
					}
				} else {
					if (model == 0) {
						Point4D point = new Point4D(x, y, z, self.M);
						if (self.CanSeeCoordinates(point)) {
							targ(this, point, parameter);
							return;
						}
					} else {
						if (self.CanSeeCoordinates(x, y, self.M)) {
							Map map = self.GetMap();
							StaticItem sta = map.GetStatic(x, y, z, model);
							if (sta != null) {
								targ(this, sta, parameter);
								return;
							}
							MultiItemComponent mic = map.GetMultiComponent(x, y, z, model);
							if (mic != null) {
								targ(this, mic, parameter);
								return;
							}
						}
					}
				}
			}
			PacketSequences.SendClilocSysMessage(this.conn, 1046439, 0);//That is not a valid target.
		}
		#endregion Target

		#region Menu
		private Dictionary<int, MenuResponseEntry> menuEntries = new Dictionary<int, MenuResponseEntry>();

		private struct MenuResponseEntry {
			internal MenuRespose response;
			internal MenuCancel cancel;
			internal object parameter;
		}

		public void Menu(IEnumerable<string> allTexts, MenuRespose response, MenuCancel cancel, object parameter) {
			int menuUid = this.PrepareMenu(response, cancel, parameter);

			OpenDialogBoxPacket packet = Pool<OpenDialogBoxPacket>.Acquire();
			packet.Prepare(menuUid, allTexts);
			this.conn.SendSinglePacket(packet);
		}

		public void Menu(string header, IEnumerable<string> choices, MenuRespose response, MenuCancel cancel, object parameter) {
			int menuUid = this.PrepareMenu(response, cancel, parameter);

			OpenDialogBoxPacket packet = Pool<OpenDialogBoxPacket>.Acquire();
			packet.Prepare(menuUid, header, choices);
			this.conn.SendSinglePacket(packet);
		}

		private int PrepareMenu(MenuRespose response, MenuCancel cancel, object parameter) {
			int menuUid;
			do {
				menuUid = Globals.dice.Next();
			} while (this.menuEntries.ContainsKey(menuUid));

			this.menuEntries[menuUid] = new MenuResponseEntry
			{
				response = response,
				cancel = cancel,
				parameter = parameter
			};
			return menuUid;
		}

		internal void HandleMenu(int menuUid, int oneBasedIndex) {
			MenuResponseEntry entry;
			if (this.menuEntries.TryGetValue(menuUid, out entry)) {
				if (oneBasedIndex == 0) {
					entry.cancel(this, entry.parameter);
				} else {
					entry.response(this, oneBasedIndex - 1, entry.parameter);
				}
			}
		}
		#endregion Menu

		#region Language
		private string languageString = "enu";
		private Language language = Language.English;

		public string ClientLanguage {
			get {
				return this.languageString;
			}
			set {
				if (!StringComparer.OrdinalIgnoreCase.Equals(value, this.languageString)) {
					this.languageString = value;
					this.language = LocManager.TranslateLanguageCode(value);
				}
			}
		}

		public Language Language {
			get {
				return this.language;
			}
		}
		#endregion Language

		#region Gump
		private Dictionary<int, Gump> gumpInstancesByUid = new Dictionary<int, Gump>();
		private Dictionary<GumpDef, LinkedList<Gump>> gumpInstancesByGump = new Dictionary<GumpDef, LinkedList<Gump>>();

		internal void SentGump(Gump gi) {
			this.gumpInstancesByUid[gi.Uid] = gi;
			GumpDef thisGump = gi.Def;
			LinkedList<Gump> instancesOfThisGump;
			if (!this.gumpInstancesByGump.TryGetValue(thisGump, out instancesOfThisGump)) {
				instancesOfThisGump = new LinkedList<Gump>();
				this.gumpInstancesByGump[thisGump] = instancesOfThisGump;
			}
			instancesOfThisGump.AddFirst(gi);
		}

		internal ICollection<Gump> FindGumpInstances(GumpDef gd) {
			LinkedList<Gump> retVal;
			if (this.gumpInstancesByGump.TryGetValue(gd, out retVal)) {
				return retVal;
			}
			return EmptyReadOnlyGenericCollection<Gump>.instance;
		}

		internal Gump PopGump(int gumpInstanceUid) {
			Gump gi;
			if (this.gumpInstancesByUid.TryGetValue(gumpInstanceUid, out gi)) {
				this.gumpInstancesByUid.Remove(gumpInstanceUid);

				GumpDef gd = gi.Def;
				LinkedList<Gump> list;
				if (this.gumpInstancesByGump.TryGetValue(gd, out list)) {
					list.Remove(gi);
					if (list.Count == 0) {
						this.gumpInstancesByGump.Remove(gd);
					}
				}
				return gi;
			}
			return null;
		}
		#endregion Gump

		#region Recompiling
		internal int charBackupUid;

		internal void BackupLinksToCharacters() {
			if (this.character != null) {
				this.charBackupUid = this.character.Uid;
			} else {
				this.charBackupUid = -1;
			}
		}

		internal void RelinkCharacter() {
			if (this.charBackupUid != -1) {
				AbstractCharacter newChar = Thing.UidGetCharacter(this.charBackupUid);
				if (newChar == null) {
					this.conn.Close("Character lost while recompiling...?");
				} else {
					newChar.Account.SetLoggedIn(this);
					this.account = newChar.Account;
					this.character = newChar;
					newChar.ReLinkToGameState();
				}
			}

			this.gumpInstancesByUid.Clear();
			this.gumpInstancesByGump.Clear();
		}

		internal void RemoveBackupLinks() {
			this.charBackupUid = -1;
		}
		#endregion Recompiling

		#region LightLevel
		private int lastSentGlobalLightLevel;
		private int lastSentPersonalLightLevel;

		public void SendPersonalLightLevel(int personalLight) {
			if (personalLight != this.lastSentPersonalLightLevel) {
				PersonalLightLevelOutPacket packet = Pool<PersonalLightLevelOutPacket>.Acquire();
				packet.Prepare(this.CharacterNotNull.FlaggedUid, personalLight);
				this.conn.SendSinglePacket(packet);
				this.lastSentPersonalLightLevel = personalLight;
			}
		}

		public void SendGlobalLightLevel(int globalLight) {
			if (globalLight != this.lastSentGlobalLightLevel) {
				PreparedPacketGroups.SendOverallLightLevel(this.conn, globalLight);
				this.lastSentGlobalLightLevel = globalLight;
			}
		}
		#endregion LightLevel

		#region Last Macro Spell/Skill
		public int LastSkillMacroId {
			get;
			internal set;
		}

		public int LastSpellMacroId {
			get;
			internal set;
		}
		#endregion Last Macro Spell/Skill

		#region Last exclusive conversation partner

		private AbstractCharacter lastExclusiveConversationpartner;
		public AbstractCharacter LastExclusiveConversationPartner {
			get {
				if (this.lastExclusiveConversationpartner != null) {
					if (this.lastExclusiveConversationpartner.IsDeleted) {
						this.lastExclusiveConversationpartner = null;
					}
				}
				return this.lastExclusiveConversationpartner;
			}
			internal set { this.lastExclusiveConversationpartner = value; }
		}

		#endregion Last exclusive conversation partner

		#region IDeletable
		private bool isDeleted;

		public sealed override void Delete() {
			this.isDeleted = true;
			base.Delete();
		}

		public sealed override bool IsDeleted {
			get {
				return this.isDeleted;
			}
		}
		#endregion IDeletable


		public override string ToString() {
			StringBuilder sb = new StringBuilder("Client (uid=");
			sb.Append(this.uid);
			if (this.account != null) {
				sb.Append(", acc='").Append(this.account.Name).Append("'");
			}
			if (this.ip != null) {
				sb.Append(", IP=").Append(this.ip);
			}
			return sb.Append(")").ToString();
		}

		public override string Name {
			get {
				return this.ToString();
			}
			set {
			}
		}

		public override int GetHashCode() {
			return this.uid;
		}

		public override bool Equals(object obj) {
			return ReferenceEquals(this, obj);
		}

		public void WriteLine(string msg) {
			PacketSequences.SendSystemMessage(this.conn, msg, -1);
		}


		public void On_PacketBeingHandled(IncomingPacket<TcpConnection<GameState>, GameState, IPEndPoint> packet) {
			ISrc src = this.character;
			Globals.SetSrc(src);
		}
	}
}