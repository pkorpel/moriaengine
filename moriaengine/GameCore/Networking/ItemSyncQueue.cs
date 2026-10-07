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
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Regions;

namespace SteamEngine.Networking {
	[SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public sealed class ItemSyncQueue : SyncQueue {
		internal static ItemSyncQueue instance = new ItemSyncQueue();

		private SimpleQueue<AbstractItem> queue = new SimpleQueue<AbstractItem>();

		/// <summary>Call when a thing is about to be created/changed</summary>
		public static void Resend(AbstractItem item) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "Resend(" + item + ") called");
				instance.SetFlagsOnItem(item, ItemSyncFlags.Resend);
			}
		}

		/// <summary>Call when an item is about to be changed</summary>
		public static void AboutToChange(AbstractItem item) {
			ItemOnGroundUpdater.RemoveFromCache(item);
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "ItemAboutToChange(" + item + ") called");
				instance.SetFlagsOnItem(item, ItemSyncFlags.ItemUpdate);
			}
		}

		/// <summary>Call when an item is about to be changed</summary>
		public static void PropertiesChanged(AbstractItem item) {
			if (IsEnabled) {
				Logger.WriteInfo(Globals.NetSyncingTracingOn, "ItemPropertiesChanged(" + item + ") called");
				instance.SetFlagsOnItem(item, ItemSyncFlags.Property);
			}
		}

		private ItemSyncQueue() {
		}

		protected override void ProcessQueue() {
			while (this.queue.Count > 0) {
				AbstractItem item = this.queue.Dequeue();
				if ((item != null) && (!item.IsDeleted)) {
					ItemSyncFlags syncFlags = item.SyncFlags;
					item.SyncFlags = ItemSyncFlags.None;

					if ((syncFlags & (ItemSyncFlags.Resend | ItemSyncFlags.ItemUpdate)) != ItemSyncFlags.None) { //no difference between update and resend. Maybe one day we will discover something :)
						UpdateItemAndProperties(item);
					} else if (Globals.UseAosToolTips) {//only new properties
						SendItemPropertiesOnly(item);
					}
				}
			}
		}

		private void SetFlagsOnItem(AbstractItem item, ItemSyncFlags flags) {
			Sanity.IfTrueThrow(flags == ItemSyncFlags.None, "flags == SyncFlags.None");

			ItemSyncFlags itemSyncFlags = item.SyncFlags;
			if (itemSyncFlags == ItemSyncFlags.None) {
				this.queue.Enqueue(item);
				this.autoResetEvent.Set();
			}
			item.SyncFlags = itemSyncFlags | flags;
		}

		[Flags]
		internal enum ItemSyncFlags : byte {
			None = 0x00,
			Resend = 0x01,	//complete update - after creation, or on demand
			ItemUpdate = 0x02,
			Property = 0x04 //update properties
		}

		private static void SendItemPropertiesOnly(AbstractItem item) {
			Logger.WriteInfo(Globals.NetSyncingTracingOn, "ProcessItemProperties " + item);
			IEnumerable<AbstractCharacter> enumerator;
			AbstractItem contAsItem = item.Cont as AbstractItem;
			if (contAsItem != null) {
				enumerator = OpenedContainers.GetViewers(contAsItem);
			} else {
				Thing top = item.TopObj();
				enumerator = top.GetMap().GetPlayersInRange(top.X, top.Y, Globals.MaxUpdateRange);
			}

			AosToolTips[] toolTipsArray = null;
			foreach (AbstractCharacter player in enumerator) {
				GameState state = player.GameState;
				if (state != null) {
					TcpConnection<GameState> conn = state.Conn;
					if (state.Version.AosToolTips) {
						Language language = state.Language;
						AosToolTips toolTips = null;
						if (toolTipsArray != null) {
							toolTips = toolTipsArray[(int) language];
						}

						if (toolTips == null) {
							toolTips = item.GetAosToolTips(language);
							if (toolTips == null) {
								break;
							}
							if (toolTipsArray == null) {
								toolTipsArray = new AosToolTips[Tools.GetEnumLength<Language>()];
							}
							toolTipsArray[(int) language] = toolTips;
						}
						toolTips.SendIdPacket(state, conn);
					}
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private static void UpdateItemAndProperties(AbstractItem item) {
			Logger.WriteInfo(Globals.NetSyncingTracingOn, "ProcessItem " + item);

			bool propertiesExist = true;
			bool isOnGround = item.IsOnGround;
			bool isEquippedAndVisible = false;
			bool isInContainer = false;
			if (!isOnGround) {
				isEquippedAndVisible = item.IsEquipped;
				if (isEquippedAndVisible) {
					if (item.Z >= AbstractCharacter.sentLayers) {
						isEquippedAndVisible = false;
					}
				} else {
					isInContainer = item.IsInContainer;
				}
			}

			if (isOnGround || isEquippedAndVisible || isInContainer) {
				PacketGroup pg = null;//iteminfo or paperdollinfo or itemincontainer
				AosToolTips[] toolTipsArray = null;

				IEnumerable<AbstractCharacter> enumerator;
				AbstractItem contAsItem = item.Cont as AbstractItem;
				if (contAsItem != null) {
					enumerator = OpenedContainers.GetViewers(contAsItem);
					//checkPreviousVisibility = false;
				} else {
					Thing newMapPoint = item.TopObj();
					Map newMap = newMapPoint.GetMap();
					enumerator = newMap.GetPlayersInRange(newMapPoint.X, newMapPoint.Y, Globals.MaxUpdateRange);
				}

				foreach (AbstractCharacter viewer in enumerator) {
					GameState state = viewer.GameState;
					if (state != null) {
						TcpConnection<GameState> conn = state.Conn;

						if (viewer.CanSeeForUpdate(item).Allow) {
							if (isOnGround) {
								item.GetOnGroundUpdater().SendTo(viewer, state, conn); //0x1a + corpseitems or some such stuff, when needed
							} else if (isEquippedAndVisible) {
								if (pg == null) {
									pg = PacketGroup.AcquireMultiUsePG();
									pg.AcquirePacket<WornItemOutPacket>().PrepareItem(item.Cont.FlaggedUid, item);//0x2e
								}
								conn.SendPacketGroup(pg);
							} else { //isInContainer
								if (pg == null) {
									pg = PacketGroup.AcquireMultiUsePG();
									pg.AcquirePacket<AddItemToContainerOutPacket>().Prepare(item.Cont.FlaggedUid, item);//0x25
								}
								conn.SendPacketGroup(pg);
							}

							if (propertiesExist) {
								if (Globals.UseAosToolTips && state.Version.AosToolTips) {
									Language language = state.Language;
									AosToolTips toolTips = null;
									if (toolTipsArray != null) {
										toolTips = toolTipsArray[(int) language];
									}

									if (toolTips == null) {
										toolTips = item.GetAosToolTips(language);
										if (toolTips == null) {
											propertiesExist = false;
											continue;
										}
										if (toolTipsArray == null) {
											toolTipsArray = new AosToolTips[Tools.GetEnumLength<Language>()];
										}
										toolTipsArray[(int) language] = toolTips;
									}
									toolTips.SendIdPacket(state, conn);
								}
							}
						}
					}
				}

				if (pg != null) {
					pg.Dispose();
				}
			}
		}
	}
}