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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Networking;
using SteamEngine.Persistence;

namespace SteamEngine {
	public abstract class AbstractAccount : PluginHolder {

		/// private static int firstFreeGameAccount=0; private static int
		/// lastUsedGameAccount=-1;                                      
		private static Dictionary<string, AbstractAccount> accounts = new Dictionary<string, AbstractAccount>(StringComparer.OrdinalIgnoreCase);

		//private string _password = null;	//http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpguide/html/cpconensuringdataintegritywithhashcodes.asp
		private byte[] passwordHash;
		private string password;

		/// private uid representation.
		//private int uid = 0; public int Uid { get { return uid; } }

		private byte maxPlevel;
		private byte plevel;


		//------------------------
		//Variables and Properties

		//- Constants
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const int maxCharactersPerGameAccount = 5;

		//- Public
		private AbstractCharacter[] characters = new AbstractCharacter[maxCharactersPerGameAccount];
		private ReadOnlyCollection<AbstractCharacter> charactersReadOnly;

		private bool deleted;
		private bool blocked;
		private string name;
		private GameState gameState;

		internal static void ClearAll() {
			accounts.Clear();
		}

		protected AbstractAccount(PropsSection input)
			: this(input.HeaderName) {
			//Console.WriteLine("["+input.headerType+" "+input.headerName+"]");
			this.charactersReadOnly = new ReadOnlyCollection<AbstractCharacter>(this.characters);
			this.LoadSectionLines(input);
		}

		protected AbstractAccount(string name) {
			Commands.AuthorizeCommandThrow(Globals.Src, "CreateGameAccount");

			//if (String.Equals("eof", name, StringComparison.OrdinalIgnoreCase)) {
			//    Globals.SrcWriteLine("EOF is an illegal account name");
			//    throw new OverrideNotAllowedException("EOF is an illegal account name");
			//}
			if (accounts.ContainsKey(name)) {
				throw new OverrideNotAllowedException("There is already an account named " + name + "!");
			}
			accounts[name] = this;
			this.name = name;
			this.plevel = 1;
			this.maxPlevel = 1;
		}

		public bool Blocked {
			get { 
				return this.blocked; 
			}
		}

		#region persistence
		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		internal sealed class AccountSaveCoordinator : IBaseClassSaveCoordinator {
			private static readonly Regex accountNameRE = new Regex(@"^\$(?<value>\w*)\s*$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

			public string FileNameToSave {
				get { return "accounts"; }
			}

			public void StartingLoading() {
			}

			public void SaveAll(SaveStream writer) {
				Logger.WriteDebug("Saving GameAccounts.");
				writer.WriteComment("GameAccounts");
				writer.WriteLine();
				int numGameAccounts = accounts.Count;
				foreach (AbstractAccount acc in accounts.Values) {
					acc.SaveWithHeader(writer);
					ObjectSaver.FlushCache(writer);
				}
				Logger.WriteDebug("Saved " + numGameAccounts + " accounts.");
			}

			public void LoadingFinished() {
				Logger.WriteDebug("Loaded " + accounts.Count + " accounts.");
			}

			public Type BaseType {
				get { return typeof(AbstractAccount); }
			}

			public string GetReferenceLine(object value) {
				return "$" + ((AbstractAccount) value).name;
			}

			public Regex ReferenceLineRecognizer {
				get { return accountNameRE; }
			}

			[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
			public object Load(Match m) {
				string name = m.Groups["value"].Value;
				return GetByName(name);
			}
		}

		//static Regex charuidRE= new Regex(@"charuid\[(?<index>\d+)\]\s*$",
		//	RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			//gets a name/value pair for each line in the file
			//Match m=charuidRE.Match(name);
			//if (m.Success) {
			//    int index=int.Parse(m.Groups["index"].Value, NumberStyles.Integer);
			//    if (index>=maxCharactersPerGameAccount) {
			//        throw new Exception("More than allowed characters per account");	
			//    }
			//    ObjectSaver.Load(value, new LoadObjectParam(CharLoad_Delayed), filename, line, index);
			//    return;
			//}
			switch (valueName) {
				case "password":
					string str = ConvertTools.LoadSimpleQuotedString(valueString);

					if (Globals.HashPasswords) {
						this.passwordHash = Tools.HashPassword(str);
						this.password = null;
					} else {
						this.password = str;
						this.passwordHash = null;
					}
					break;
				case "passwordHash":
					Match m = ConvertTools.stringRE.Match(valueString);
					if (m.Success) {
						if (Globals.HashPasswords) {
							if (this.passwordHash == null) {	//Allows admins to set password=xxx without erasing passwordHash, and the password=xxx will override the passwordHash.
								this.passwordHash = DecodeEncodedHash(m.Groups["value"].Value);
							}
							this.password = null;
						} else {
							if (this.password == null) {		//Allows admins to set password=xxx without erasing passwordHash, etc.
								this.passwordHash = null;
								this.password = m.Groups["value"].Value;
							}
						}
					} else {
						Logger.WriteError(filename, line, "Saved passwordHash for acc " + this + " has invalid format");
					}
					break;
				case "blocked":
					this.blocked = ConvertTools.ParseBoolean(valueString);
					break;
				case "plevel":
					this.plevel = ConvertTools.ParseByte(valueString);
					break;
				case "maxplevel":
					this.maxPlevel = ConvertTools.ParseByte(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public void SaveWithHeader(SaveStream output) {
			output.WriteLine("[" + Tools.TypeToString(this.GetType()) + " " + this.name + "]");
			this.Save(output);
			output.WriteLine();
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			//output.WriteValue("uid",uid);
			if (this.passwordHash != null) {
				string hash = EncodeHashToString(this.passwordHash);
				output.WriteValue("passwordHash", hash);
			} else {
				output.WriteValue("password", this.password);
			}
			if (this.maxPlevel != 1) {
				output.WriteValue("plevel", this.plevel);
				output.WriteValue("maxplevel", this.maxPlevel);
			}
			if (this.blocked) {
				output.WriteValue("blocked", this.blocked);
			}
			this.CheckReferences();
			for (int i = 0; i < maxCharactersPerGameAccount; i++) {
				if (this.characters[i] != null) {
					//output.WriteValue("CharUID["+i+"]", this.characters[i]);
					output.WriteComment("CharUID[" + i + "]=#" + this.characters[i].Uid);
				}
			}
			base.Save(output);//base save - TagHolder
		}
		#endregion persistence


		public static IEnumerable<AbstractAccount> AllAccounts {
			get {
				return accounts.Values;
			}
		}

		public bool IsOnline {
			get {
				return (this.gameState != null);
			}
		}

		public GameState GameState {
			get {
				return this.gameState;
			}
		}

		public void Password(string pass) {
			Commands.AuthorizeCommandThrow(Globals.Src, "SetAccountPassword");
			if (Globals.HashPasswords) {
				this.passwordHash = Tools.HashPassword(pass);
				this.password = null;
			} else {
				this.password = pass;
				this.passwordHash = null;
			}
		}

		public static AbstractAccount GetByName(string acctname) {
			AbstractAccount acc;
			if (accounts.TryGetValue(acctname, out acc)) {
				return acc;
			}
			return null;
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public AbstractCharacter GetLingeringCharacter() {
			foreach (AbstractCharacter ch in this.characters) {
				if ((ch != null) && (ch.IsLingering)) {
					return ch;
				}
			}
			return null;
		}

		private static ScriptHolder createGameAccountFunction;
		public static ScriptHolder CreateGameAccountFunction {
			get {
				if (createGameAccountFunction == null) {
					createGameAccountFunction = ScriptHolder.GetFunction("CreateGameAccount");
					if (createGameAccountFunction == null) {
						throw new SEException("CreateGameAccount function not declared! It needs to have 1 string parameter - the acc name, and return a GameAccount instance");
					}
				}
				return createGameAccountFunction;
			}
		}

		private static AbstractAccount CreateAccount(string username, string password) {
			AbstractAccount acc = CreateGameAccountFunction.TryRun(null, username) as AbstractAccount;
			Console.WriteLine("Creating new account {0}", username);
			if (acc == null) {
				throw new SEException("CreateGameAccount function failed to create a new account!");
			}
			acc.Password(password);
			return acc;
		}

		[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
		public static LoginAttemptResult HandleLoginAttempt(string username, string password, GameState gs, out AbstractAccount acc) {
			acc = GetByName(username);
			if (acc == null)
			{
				if ((Globals.AutoAccountCreation) || (accounts.Count == 0)) {
					acc = CreateAccount(username, password);
					if (accounts.Count == 1) {
						acc.plevel = Globals.MaximalPlevel;
						acc.maxPlevel = Globals.MaximalPlevel;
					}
					gs.SetLoggedIn(acc);
					acc.SetLoggedIn(gs);
					return LoginAttemptResult.Success;
				}
				return LoginAttemptResult.Failed_NoSuchAccount;
			}
			if (!acc.TestPassword(password)) {
				return LoginAttemptResult.Failed_BadPassword;
			}
			if (acc.blocked) {
				return LoginAttemptResult.Failed_Blocked;
			}
			if (acc.IsOnline) {
				return LoginAttemptResult.Failed_AlreadyOnline;
			}
			gs.SetLoggedIn(acc);
			acc.SetLoggedIn(gs);
			return LoginAttemptResult.Success;
		}

		internal void SetLoggedIn(GameState gs) {
			this.gameState = gs;
		}

		internal void SetLoggedOut() {
			this.gameState = null;
		}

		public static AbstractAccount HandleConsoleLoginAttempt(string username, string password) {
			AbstractAccount acc = null;
			if (accounts.Count == 0) {
				acc = CreateAccount(username, password);
				acc.maxPlevel = Globals.MaximalPlevel;
				acc.plevel = Globals.MaximalPlevel;
			} else {
				acc = GetByName(username);
			}
			if (acc == null) {
				return null;
			}
			if (acc.TestPassword(password) == false || (acc.maxPlevel < 4) || (acc.blocked)) {
				return null;
			}
			return acc;
		}

		public override string Name {
			get {
				return this.name;
			}
		}

		public byte PLevel {
			get { return this.plevel; }
			set {
				if (value < this.maxPlevel && value <= Globals.MaximalPlevel) {
					this.plevel = value;
				} else {
					this.plevel = this.maxPlevel;
				}
			}
		}
		public byte MaxPLevel {
			get { return this.maxPlevel; }
		}

		//readonly property for info dialogs...
		public ReadOnlyCollection<AbstractCharacter> Characters {
			get {
				return this.charactersReadOnly;
			}
		}

		/*
			Method: Promote
			Promote this account to <plevel> maxPlevel. Both their maxPlevel and plevel are set to this, but note
			that they can change their own plevel (but can't set it above their maxPlevel). Yes, you can set
			your plevel to 1, and then later set it back to however high you want it, so long as that is not
			above your maxPlevel. The "GM" command toggles between player plevel and your maxPlevel.
			
			Parameters:
				name - Their account name
				plevel - The maxPlevel you want them to be.
		*/
		public void Promote(int newMaxPlevel) {
			PromoteOrDemote(Globals.Src.MaxPlevel, GetByName(this.name), newMaxPlevel);
		}

		public override bool IsDeleted { get { return this.deleted; } }

		public override void Delete() {
			Commands.AuthorizeCommandThrow(Globals.Src, "DeleteAccount");

			if (this.gameState != null) {
				PreparedPacketGroups.SendLoginDenied(this.gameState.Conn, LoginDeniedReason.NoAccount);
				this.gameState.Conn.Close("Account is being deleted.");
			}

			//delete characters
			for (int a = 0; a < maxCharactersPerGameAccount; a++) {
				if (this.characters[a] != null) {
					this.characters[a].InternalDelete();
					this.characters[a] = null;
				}
			}

			base.Delete();

			accounts.Remove(this.name);
			this.deleted = true;

		}

		public void Block() {
			if (this.blocked) {
				Globals.SrcWriteLine("GameAccount " + this.name + " is already blocked.");
			} else {
				if (this.MaxPLevel < Globals.MaximalPlevel) {
					Commands.AuthorizeCommandThrow(Globals.Src, "BlockAccount");
					this.blocked = true;
					Globals.SrcWriteLine("GameAccount " + this.name + " blocked successfully.");
				} else {
					Globals.SrcWriteLine("GameAccount " + this.name + " cannot be blocked; It's owner..");
				}
			}
		}

		public void Unblock() {
			if (!this.blocked) {
				Globals.SrcWriteLine("GameAccount " + this.name + " is not blocked.");
			} else {
				this.blocked = false;
				Commands.AuthorizeCommandThrow(Globals.Src, "UnblockAccount");
				Globals.SrcWriteLine("GameAccount " + this.name + " unblocked successfully.");
			}
		}

		public void Block(bool yesorno) {
			if (yesorno) {
				this.Block();
			} else {
				this.Unblock();
			}
		}

		public override string ToString() {
			return this.name + " - Plevel " + this.PLevel + "/" + this.MaxPLevel;
		}

		public override int GetHashCode() {
			return this.name.GetHashCode();
		}

		public override bool Equals(object obj) {
			return ReferenceEquals(this, obj);
		}

		//------------------------
		//Private & internal stuff

		/**
			You should call MakeBeNonPlayer on a character, and it will call this.
			Detaches a character from this account. This does NOT clear their account.
			If the character is not on this account, calling this method is considered
			an error and results in a thrown SanityCheckException.

			@param cre The character to detach from the account.
				
		*/
		internal void DetachCharacter(AbstractCharacter cre) {
			for (int i = 0; i < maxCharactersPerGameAccount; i++) {
				if (this.characters[i] == cre) {
					this.characters[i] = null;
					return;
				}
			}
			throw new SanityCheckException("Call was made to DetachCharacterFromAccount, but the character (" + cre + ") was not attached to this account (" + this + ")!");
		}

		/**
			You should call MakeBePlayer on a character, and it will call this.
			Attaches a character to this account, if any slots are free. This does NOT set their account.
			If they're already in this account, this returns true (So that the account-fixing code works).
			
			@param cre The character to attach to the account.
				
			@return	True if the character was successfully attached, false otherwise.
		*/
		internal bool AttachCharacter(AbstractCharacter cre, out int slot) {
			for (int i = 0; i < maxCharactersPerGameAccount; i++) {
				if (this.characters[i] == cre) {//we have that char already
					slot = i;
					return true;
				}
			}
			for (int i = 0; i < maxCharactersPerGameAccount; i++) {
				if (this.characters[i] == null) {
					this.characters[i] = cre;
					slot = i;
					return true;
				}
			}
			slot = -1;
			return false;
		}

		public AbstractCharacter GetCharacterInSlot(int index) {
			Sanity.IfTrueThrow(index < 0 || index >= maxCharactersPerGameAccount, "Call was made to GetCharacterInSlot with an invalid character index " + index + ", valid values being from 0 to " + (maxCharactersPerGameAccount - 1) + ".");
			//Sanity.IfTrueThrow(conn==null, "Call was made to LoginCharacter when account was null!"); //wtf?? why could it not be null? -tar
			if (this.characters[index] == null) {
				return null;
			}
			return this.characters[index];
		}

		internal DeleteCharacterResult RequestDeleteCharacter(int index) {
			Sanity.IfTrueThrow(index < 0 || index >= maxCharactersPerGameAccount, "Call was made to RequestDeleteCharacter with an invalid character index " + index + ", valid values being from 0 to " + (maxCharactersPerGameAccount - 1) + ".");
			AbstractCharacter cre = this.characters[index];
			if (cre == null) {
				return DeleteCharacterResult.Deny_NonexistantCharacter;
			}
			if (!cre.Flag_Disconnected && !cre.IsLingering) {
				return DeleteCharacterResult.Deny_CharacterIsBeingPlayedRightNow;
			}
			//TODO: Trigger on=@deleteCharacter or something (someone else can decide what to put it on)
			// with return 1 to cancel it.

			cre.InternalDelete();
			return DeleteCharacterResult.Allow;
		}

		internal static void PromoteOrDemote(int promotingPlevel, AbstractAccount whoToPromote, int promoteOrDemoteTo) {
			if (promoteOrDemoteTo >= 0 && promoteOrDemoteTo <= Globals.MaximalPlevel && promotingPlevel >= promoteOrDemoteTo) {
				whoToPromote.maxPlevel = (byte) promoteOrDemoteTo;
				whoToPromote.plevel = (byte) promoteOrDemoteTo;
			}
		}

		private void CheckReferences() {
			for (int i = 0; i < maxCharactersPerGameAccount; i++) {
				if (this.characters[i] != null) {
					if (this.characters[i].IsDeleted || this.characters[i].Account != this) {
						//deleted or removed from account
						this.characters[i] = null;
					}
				}
			}
		}

		internal bool HasFreeSlot {
			get {
				for (int a = 0; a < maxCharactersPerGameAccount; a++) {
					if (this.characters[a] == null) {
						return true;
					}
				}
				return false;
			}
		}

		//If both password and passwordHash become set, then passwordHash takes
		//precedence. This should not happen, but this is here as an additional
		//security measure, in case someone manages to somehow set the string
		//password despite there being a hashed password already.
		internal bool TestPassword(string pass) {
			//Preconditions
			Sanity.IfTrueThrow((this.passwordHash != null && this.password != null), "GameAccount [" + this.name + "]: Has both a password and hashed password.");
			Sanity.IfTrueThrow((this.passwordHash == null && this.password == null), "GameAccount [" + this.name + "]: Has neither a password nor hashed password.");

			if (this.passwordHash != null)
			{
				if (TestHash(this.passwordHash, Tools.HashPassword(pass))) {
					if (!Globals.HashPasswords) {
						//record the password string and get rid of the hash now that we know what the password is again
						this.password = pass;
						this.passwordHash = null;
					}
					return true;
				}
				return false;
			}
			//We should only get here if we're not using hashed passwords, but let's check anyways.
			if (!Globals.HashPasswords) {
				if (this.password != null)
				{
					if (this.password == pass) {
						return true;
					}
					return false;
				}
			} else {	//Eh, convert the account's password to a hash and THEN compare.
				this.passwordHash = Tools.HashPassword(this.password);
				this.password = null;

				if (TestHash(this.passwordHash, Tools.HashPassword(pass))) {
					return true;
				}
				return false;
			}
			return false;
		}

		private static byte[] DecodeEncodedHash(string encodedHash) {
			//decode it back to the hash
			return Convert.FromBase64String(encodedHash);
		}

		private static string EncodeHashToString(byte[] decodedHash) {
			//encode it into a string which we can write to a text save if necessary
			return Convert.ToBase64String(decodedHash);
		}

		private static bool TestHash(byte[] original, byte[] test) {
			if (original.Length == test.Length) {
				for (int a = 0; a < original.Length; a++) {
					if (original[a] != test[a]) return false;
				}
			}
			return true;
		}

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();
		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override void Trigger(TriggerKey tk, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				registeredTGs[i].Run(this, tk, sa);
			}
			base.TryTrigger(tk, sa);
		}

		public override void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				registeredTGs[i].TryRun(this, tk, sa);
			}
			base.TryTrigger(tk, sa);
		}

		public override TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				if (TagMath.Is1(registeredTGs[i].Run(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return base.TryCancellableTrigger(tk, sa);
		}

		public override TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				if (TagMath.Is1(registeredTGs[i].TryRun(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}
			return base.TryCancellableTrigger(tk, sa);
		}
	}
}