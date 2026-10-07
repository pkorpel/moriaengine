using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;

namespace SteamEngine {

	public sealed class AosToolTips : Poolable {
		private int uid;
		private Thing thing;
		private List<int> ids = new List<int>(3);
		private List<string> arguments = new List<string>(3);
		private bool initDone;
		private Language language;

		//SteamEngine.Packets.FreedPacketGroup oldIdGroup;
		//SteamEngine.Packets.FreedPacketGroup newIdGroup;
		//SteamEngine.Packets.FreedPacketGroup dataGroup;

		PacketGroup oldIdNGroup;
		PacketGroup newIdNGroup;
		PacketGroup dataNGroup;

		private int nameValueClilocsUsed;

		static CacheDictionary<Thing, AosToolTips>[] cachesByLanguage = InitCachesArray();

		private static CacheDictionary<Thing, AosToolTips>[] InitCachesArray() {
			int n = Tools.GetEnumLength<Language>();
			CacheDictionary<Thing, AosToolTips>[] cachesArray = new CacheDictionary<Thing, AosToolTips>[n];
			for (int i = 0; i < n; i++) {
				cachesArray[i] = new CacheDictionary<Thing, AosToolTips>(50000, true);//znate nekdo nejaky lepsi cislo? :)
			}
			return cachesArray;
		}

		private static int uids;

		public static AosToolTips GetFromCache(Thing thing, Language language) {
			AosToolTips toolTips;
			cachesByLanguage[(int) language].TryGetValue(thing, out toolTips);
			return toolTips;
		}

		public static void RemoveFromCache(Thing thing, Language language) {
			cachesByLanguage[(int) language].Remove(thing);
		}

		public static void RemoveFromCache(Thing thing) {
			foreach (CacheDictionary<Thing, AosToolTips> cache in cachesByLanguage) {
				cache.Remove(thing);
			}
		}

		protected override void On_Reset() {
			base.On_Reset();

			this.uid = uids++;
			this.ids.Clear();
			this.arguments.Clear();
			this.oldIdNGroup = null;
			this.newIdNGroup = null;
			this.dataNGroup = null;
			this.initDone = false;
			this.thing = null;
			this.nameValueClilocsUsed = 0;
		}

		public override void Dispose() {
			try {
				if (this.initDone) {
					cachesByLanguage[(int) this.language].Remove(this.thing);
					this.initDone = false;
				}
			} finally {
				base.Dispose();
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "thing")]
		public void InitDone(Thing thing) {
			this.thing = thing;
			cachesByLanguage[(int) this.language][thing] = this;
			this.initDone = true;
		}

		public bool IsInitDone {
			get {
				return this.initDone;
			}
		}

		public Language Language {
			get {
				return this.language;
			}
			set {
				Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
				this.language = value;
			}
		}

		//public void AddLine(string plainText) {
		//    Sanity.IfTrueThrow(frozen, "You can't modify a frozen ObjectPropertiesContainer, Unfreeze first");
		//    this.ids.Add(1042971); //1042971, 1070722 
		//    this.arguments.Add(plainText);
		//}

		public void AddLine(int clilocId) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(null);
		}

		public void AddLine(int clilocId, string arg) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(arg);
		}

		public void AddLine(int clilocId, string arg0, string arg1) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Concat(arg0, "\t", arg1));
		}

		public void AddLine(int clilocId, params string[] args) {
			Sanity.IfTrueThrow(this.initDone, "Trying to modify ObjectPropertiesContainer after InitDone");
			this.ids.Add(clilocId);
			this.arguments.Add(string.Join("\t", args));
		}

		//name: value clilocs
		public void AddNameColonValue(string name, string value) {
			if (this.nameValueClilocsUsed < 6) {
				this.ids.Add(1060658 + this.nameValueClilocsUsed);
				this.arguments.Add(string.Concat(name, "\t", value));
				this.nameValueClilocsUsed++;
			} else {
				throw new SEException("Out of name:value cliloc ids");
			}
		}
		//1060658	~1_val~: ~2_val~
		//1060659	~1_val~: ~2_val~
		//1060660	~1_val~: ~2_val~
		//1060661	~1_val~: ~2_val~
		//1060662	~1_val~: ~2_val~
		//1060663	~1_val~: ~2_val~

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void SendIdPacket(GameState state, TcpConnection<GameState> conn) {
			if (state.Version.OldAosToolTips) {
				if (this.oldIdNGroup == null) {
					this.oldIdNGroup = PacketGroup.CreateFreePG();
					this.oldIdNGroup.AcquirePacket<OldPropertiesRefreshOutPacket>().Prepare(this.thing.FlaggedUid, this.uid);
				}
				conn.SendPacketGroup(this.oldIdNGroup);
			} else {
				if (this.newIdNGroup == null) {
					this.newIdNGroup = PacketGroup.CreateFreePG();
					this.newIdNGroup.AcquirePacket<PropertiesRefreshOutPacket>().Prepare(this.thing.FlaggedUid, this.uid);
				}
				conn.SendPacketGroup(this.newIdNGroup);
			}
		}

		public int FirstId {
			get {
				return this.ids[0];
			}
		}

		public string FirstArgument {
			get {
				return this.arguments[0];
			}
		}

		internal void SendDataPacket(TcpConnection<GameState> conn) {
			if (this.dataNGroup == null) {
				this.dataNGroup = PacketGroup.CreateFreePG();
				this.dataNGroup.AcquirePacket<MegaClilocOutPacket>().Prepare(this.thing.FlaggedUid, this.uid, this.ids, this.arguments);
			}
			conn.SendPacketGroup(this.dataNGroup);
		}

		public static void ClearCache() {
			foreach (CacheDictionary<Thing, AosToolTips> cache in cachesByLanguage) {
				cache.Clear();
			}
		}
	}
}

//1050044	~1_COUNT~ items, ~2_WEIGHT~ stones
//1050045	~1_PREFIX~~2_NAME~~3_SUFFIX~