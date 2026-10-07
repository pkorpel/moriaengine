using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Networking {
	public class ItemOnGroundUpdater : Disposable {
		private readonly AbstractItem contItem;
		private bool isInCache;

		private PacketGroup packetGroupNormal;
		private PacketGroup packetGroupMovable;

		private static CacheDictionary<AbstractItem, ItemOnGroundUpdater> cache =
			new CacheDictionary<AbstractItem, ItemOnGroundUpdater>(50000, true);//znate nekdo nejaky lepsi cislo? :)


		public ItemOnGroundUpdater(AbstractItem item) {
			this.contItem = item;
			cache[item] = this;
			this.isInCache = true;
		}

		protected AbstractItem ContItem {
			get {
				return this.contItem;
			}
		} 

		public static ItemOnGroundUpdater GetFromCache(AbstractItem item) {
			ItemOnGroundUpdater iogu;
			cache.TryGetValue(item, out iogu);
			return iogu;
		}

		public static void RemoveFromCache(AbstractItem item) {
			cache.Remove(item);
		}

		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public sealed override void Dispose() {
			base.Dispose();
		}

		protected override void On_DisposeManagedResources() {
			try {
				if (this.packetGroupNormal != null) {
					this.packetGroupNormal.Dispose();
				}
				if (this.packetGroupMovable != null) {
					this.packetGroupMovable.Dispose();
				}
				if (this.isInCache) {
					cache.Remove(this.contItem);
					this.isInCache = false;
				}
			} finally {
				base.On_DisposeManagedResources();
			}
		}

		public void SendTo(AbstractCharacter viewer) {
			GameState state = viewer.GameState;
			if (state != null) {
				this.SendTo(viewer, state, state.Conn);
			}
		}

		public virtual void SendTo(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			if (viewer.IsPlevelAtLeast(Globals.PlevelOfGM)) {
				if (this.packetGroupMovable == null) {
					this.packetGroupMovable = PacketGroup.CreateFreePG();
					this.packetGroupMovable.AcquirePacket<ObjectInfoOutPacket>().Prepare(this.contItem, MoveRestriction.Movable);
				}
				viewerConn.SendPacketGroup(this.packetGroupMovable);
			} else {
				if (this.packetGroupNormal == null) {
					this.packetGroupNormal = PacketGroup.CreateFreePG();
					this.packetGroupNormal.AcquirePacket<ObjectInfoOutPacket>().Prepare(this.contItem, MoveRestriction.Normal);
				}
				viewerConn.SendPacketGroup(this.packetGroupNormal);
			}
		}

		public static void ClearCache() {
			cache.Clear();
		}
	}
}
