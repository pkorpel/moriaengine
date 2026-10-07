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

using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Networking;

namespace SteamEngine {
	public static class OpenedContainers {
		private static readonly Dictionary<AbstractItem, LinkedList<AbstractCharacter>> charsByContainer = new Dictionary<AbstractItem, LinkedList<AbstractCharacter>>();
		private static readonly Dictionary<AbstractCharacter, HashSet<AbstractItem>> containersByChar = new Dictionary<AbstractCharacter, HashSet<AbstractItem>>();

		public static DenyResult HasContainerOpen(AbstractCharacter ch, AbstractItem container) {
			HashSet<AbstractItem> conts;
			if (containersByChar.TryGetValue(ch, out conts)) {
				if (conts.Contains(container)) {
					DenyResult retVal = ch.CanReach(container);
					if (!retVal.Allow) {
						SetContainerClosed(ch, container);
					}
					return retVal;
				}
			}
			return DenyResultMessages.Deny_ContainerClosed;
		}

		internal static DenyResult HasContainerOpenFromAt(AbstractCharacter ch, IPoint4D fromPoint, IPoint4D targetPoint, AbstractItem container, bool checkTopobj) {
			HashSet<AbstractItem> conts;
			if (containersByChar.TryGetValue(ch, out conts)) {
				if (conts.Contains(container)) {
					DenyResult retVal = DenyResultMessages.Deny_ContainerClosed;
					if (ch != null) {
						retVal = ch.CanReachFromAt(fromPoint, targetPoint, container, checkTopobj);
					}
					if (!retVal.Allow) {
						SetContainerClosed(ch, container);
					}
					return retVal;
				}
			}
			return DenyResultMessages.Deny_ContainerClosed;
		}

		public static void SetContainerOpened(AbstractCharacter ch, AbstractItem container) {
			HashSet<AbstractItem> conts;
			if (!containersByChar.TryGetValue(ch, out conts)) {
				conts = new HashSet<AbstractItem>();
				containersByChar[ch] = conts;
			}
			conts.Add(container);

			LinkedList<AbstractCharacter> openedBy;
			if (!charsByContainer.TryGetValue(container, out openedBy)) {
				openedBy = new LinkedList<AbstractCharacter>();
				charsByContainer[container] = openedBy;
			}
			if (!openedBy.Contains(ch)) {
				openedBy.AddFirst(ch);
			}
		}

		public static void SetContainerClosed(AbstractCharacter ch, AbstractItem container) {
			HashSet<AbstractItem> conts;
			if (containersByChar.TryGetValue(ch, out conts)) {
				conts.Remove(container);
				if (conts.Count == 0) {
					containersByChar.Remove(ch);
				}
			}

			LinkedList<AbstractCharacter> openedBy;
			if (charsByContainer.TryGetValue(container, out openedBy)) {
				openedBy.Remove(ch);
				if (openedBy.Count == 0) {
					charsByContainer.Remove(container);//opened for no one
				}
			}
		}

		public static void SetContainerClosed(AbstractItem container) {
			LinkedList<AbstractCharacter> openedBy;
			if (charsByContainer.TryGetValue(container, out openedBy)) {
				charsByContainer.Remove(container);

				foreach (AbstractCharacter ch in openedBy) {
					HashSet<AbstractItem> conts;
					if (containersByChar.TryGetValue(ch, out conts)) {
						conts.Remove(container);
						if (conts.Count == 0) {
							containersByChar.Remove(ch);
						}
					}
				}
			}
		}

		public static IEnumerable<AbstractItem> GetOpenedContainers(AbstractCharacter ch) {
			ch.ThrowIfDeleted();

			HashSet<AbstractItem> conts;

			if (containersByChar.TryGetValue(ch, out conts)) {
				List<AbstractItem> toRemove = null;

				foreach (AbstractItem con in conts) {
					if (!ch.CanReach(con).Allow) {
						if (toRemove == null) {
							toRemove = new List<AbstractItem>();
						}
						toRemove.Add(con);
					}
				}

				if (toRemove != null) {
					foreach (AbstractItem con in toRemove) {
						SetContainerClosed(ch, con);
					}
				}
				return conts;
			}
			return EmptyReadOnlyGenericCollection<AbstractItem>.instance;
		}

		public static IEnumerable<AbstractCharacter> GetViewers(AbstractItem container) {
			container.ThrowIfDeleted();

			LinkedList<AbstractCharacter> openedBy;
			if (charsByContainer.TryGetValue(container, out openedBy)) {
				List<AbstractCharacter> toRemove = null;

				foreach (AbstractCharacter ch in openedBy) {
					if (!ch.CanReach(container).Allow) {
						if (toRemove == null) {
							toRemove = new List<AbstractCharacter>();
						}
						toRemove.Add(ch);
					}
				}

				if (toRemove != null) {
					foreach (AbstractCharacter ch in toRemove) {
						SetContainerClosed(ch, container);
					}
				}

				return openedBy;
			}
			return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
		}

		internal static void ClearAll() {
			charsByContainer.Clear();
			containersByChar.Clear();
		}

		//info about opened containers is cleared, clients should know.
		internal static void SendRemoveAllOpenedContainersFromView() {
			foreach (GameState state in GameServer.AllClients) {
				AbstractCharacter onlineChar = state.Character;
				if (onlineChar != null) {
					HashSet<AbstractItem> conts;
					if (containersByChar.TryGetValue(onlineChar, out conts)) {
						foreach (AbstractItem cont in conts) {
							PacketSequences.SendRemoveFromView(state.Conn, cont.FlaggedUid);
							if (cont.IsOnGround) {
								cont.GetOnGroundUpdater().SendTo(onlineChar, state, state.Conn);
							} else if (cont.IsEquipped) {
								WornItemOutPacket packet = Pool<WornItemOutPacket>.Acquire();
								packet.PrepareItem(onlineChar.FlaggedUid, cont);
								state.Conn.SendSinglePacket(packet);
							} //else it's in some other container, which gets closed too so we don't want it automatically visible
						}
					}
				}
			}
		}
	}
}