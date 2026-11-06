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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;

namespace SteamEngine.Regions {

	/// <summary>#1216 - which of a sector's lists a <see cref="Map.ThingsInArea{T}"/> walks.</summary>
	internal enum ThingWalk : byte {
		Things, //characters, then items
		Chars,
		NPCs,
		NPCsInLoadedSectors, //makes no sector
		Items,
		Disconnects
	}

	//#1216 - the range queries as structs: a foreach over one allocates nothing. Each walks the sectors (x outer, y inner) and
	//their lists by hand, reading the next element before handing out the current one, as the iterators did (the current one may
	//leave the list meanwhile). Used as an IEnumerable (LScript's foreach, LINQ, a List) a query is boxed, as costly as before.
	public partial class Map {

		private void GetSectorCoords(int minX, int minY, int maxX, int maxY,
				out int xSectorStart, out int ySectorStart, out int xSectorEnd, out int ySectorEnd) {
			this.GetSectorXY(Math.Min(minX, maxX), Math.Min(minY, maxY), out xSectorStart, out ySectorStart);
			this.GetSectorXY(Math.Max(minX, maxX), Math.Max(minY, maxY), out xSectorEnd, out ySectorEnd);
		}

		/// <summary>The things lying in a rectangle: characters, items, NPCs or disconnected things, as the query that made it says.</summary>
		public readonly struct ThingsInArea<T> : IEnumerable<T> where T : Thing {
			private readonly Map map;
			private readonly int minX, minY, maxX, maxY;
			private readonly ThingWalk walk;

			internal ThingsInArea(Map map, int minX, int minY, int maxX, int maxY, ThingWalk walk) {
				this.map = map;
				this.minX = minX;
				this.minY = minY;
				this.maxX = maxX;
				this.maxY = maxY;
				this.walk = walk;
			}

			public Enumerator GetEnumerator() {
				return new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY, this.walk);
			}

			IEnumerator<T> IEnumerable<T>.GetEnumerator() {
				return this.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			public struct Enumerator : IEnumerator<T> {
				private readonly Map map;
				private readonly int minX, minY, maxX, maxY;
				private readonly int xSectorStart, ySectorStart, xSectorEnd, ySectorEnd;
				private readonly ThingWalk walk;
				private int sx, sy;
				private Sector sector;
				private bool inItems; //ThingWalk.Things: the sector's characters are done
				private Thing next;
				private T current;

				internal Enumerator(Map map, int minX, int minY, int maxX, int maxY, ThingWalk walk) {
					this.map = map;
					this.minX = minX;
					this.minY = minY;
					this.maxX = maxX;
					this.maxY = maxY;
					this.walk = walk;
					map.GetSectorCoords(minX, minY, maxX, maxY, out this.xSectorStart, out this.ySectorStart, out this.xSectorEnd, out this.ySectorEnd);
					this.sx = this.xSectorStart;
					this.sy = this.ySectorStart - 1;
					this.sector = null;
					this.inItems = false;
					this.next = null;
					this.current = null;
				}

				public T Current {
					get {
						return this.current;
					}
				}

				object IEnumerator.Current {
					get {
						return this.current;
					}
				}

				public bool MoveNext() {
					while (true) {
						Thing thing = this.next;
						if (thing != null) {
							this.next = thing.nextInList;
							int x = thing.X;
							int y = thing.Y;
							if ((this.minX <= x) && (this.minY <= y) && (this.maxX >= x) && (this.maxY >= y)) {
								//Account == null is what IsPlayer says, read directly
								if (((this.walk != ThingWalk.NPCs) && (this.walk != ThingWalk.NPCsInLoadedSectors)) ||
										(((AbstractCharacter) thing).Account == null)) {
									this.current = (T) thing;
									return true;
								}
							}
						} else if (!this.NextList()) {
							this.current = null;
							return false;
						}
					}
				}

				private bool NextList() {
					if ((this.walk == ThingWalk.Things) && (!this.inItems) && (this.sector != null)) {
						this.inItems = true;
						//read only now, as the iterator did: an item made while the characters were walked can be met
						this.next = this.sector.Items.firstThing;
						return true;
					}
					while (true) {
						this.sy++;
						if (this.sy > this.ySectorEnd) {
							this.sy = this.ySectorStart;
							this.sx++;
						}
						if (this.sx > this.xSectorEnd) {
							this.sx = this.xSectorEnd + 1; //stays past the end, however often MoveNext is called
							this.sy = this.ySectorEnd;
							this.sector = null;
							return false;
						}
						Sector s;
						if (this.walk == ThingWalk.NPCsInLoadedSectors) {
							s = this.map.sectors[this.sx, this.sy]; //#1201 - a sector never made holds nothing
							if (s == null) {
								continue;
							}
						} else {
							s = this.map.GetSector(this.sx, this.sy);
						}
						this.sector = s;
						this.inItems = false;
						switch (this.walk) {
							case ThingWalk.Items:
								this.next = s.Items.firstThing;
								break;
							case ThingWalk.Disconnects:
								this.next = s.Disconnects.firstThing;
								break;
							default:
								this.next = s.Chars.firstThing;
								break;
						}
						return true;
					}
				}

				public void Reset() {
					this = new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY, this.walk);
				}

				public void Dispose() {
				}
			}
		}

		/// <summary>The players in a rectangle, logged in ones only (the sectors' player lists).</summary>
		public readonly struct PlayersInArea : IEnumerable<AbstractCharacter> {
			private readonly Map map;
			private readonly int minX, minY, maxX, maxY;

			internal PlayersInArea(Map map, int minX, int minY, int maxX, int maxY) {
				this.map = map;
				this.minX = minX;
				this.minY = minY;
				this.maxX = maxX;
				this.maxY = maxY;
			}

			public Enumerator GetEnumerator() {
				return new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY);
			}

			IEnumerator<AbstractCharacter> IEnumerable<AbstractCharacter>.GetEnumerator() {
				return this.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			/// <summary>Walks the players' list by its nodes. A player whose node leaves the list while it's the next one ends
			/// that sector's walk (a removed node has no Next); LinkedList's own enumerator threw there.</summary>
			public struct Enumerator : IEnumerator<AbstractCharacter> {
				private readonly Map map;
				private readonly int minX, minY, maxX, maxY;
				private readonly int xSectorStart, ySectorStart, xSectorEnd, ySectorEnd;
				private int sx, sy;
				private LinkedListNode<AbstractCharacter> next;
				private AbstractCharacter current;

				internal Enumerator(Map map, int minX, int minY, int maxX, int maxY) {
					this.map = map;
					this.minX = minX;
					this.minY = minY;
					this.maxX = maxX;
					this.maxY = maxY;
					map.GetSectorCoords(minX, minY, maxX, maxY, out this.xSectorStart, out this.ySectorStart, out this.xSectorEnd, out this.ySectorEnd);
					this.sx = this.xSectorStart;
					this.sy = this.ySectorStart - 1;
					this.next = null;
					this.current = null;
				}

				public AbstractCharacter Current {
					get {
						return this.current;
					}
				}

				object IEnumerator.Current {
					get {
						return this.current;
					}
				}

				public bool MoveNext() {
					while (true) {
						LinkedListNode<AbstractCharacter> node = this.next;
						if (node != null) {
							this.next = node.Next;
							AbstractCharacter player = node.Value;
							int x = player.X;
							int y = player.Y;
							if ((this.minX <= x) && (this.minY <= y) && (this.maxX >= x) && (this.maxY >= y)) {
								this.current = player;
								return true;
							}
						} else if (!this.NextSector()) {
							this.current = null;
							return false;
						}
					}
				}

				private bool NextSector() {
					this.sy++;
					if (this.sy > this.ySectorEnd) {
						this.sy = this.ySectorStart;
						this.sx++;
					}
					if (this.sx > this.xSectorEnd) {
						this.sx = this.xSectorEnd + 1;
						this.sy = this.ySectorEnd;
						return false;
					}
					this.next = this.map.GetSector(this.sx, this.sy).Players.First;
					return true;
				}

				public void Reset() {
					this = new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY);
				}

				public void Dispose() {
				}
			}
		}

		/// <summary>The connections of the players in a rectangle, or (made by GetConnectionsWhoCanSee) of those who can see a thing.</summary>
		public readonly struct ConnectionsInArea : IEnumerable<TcpConnection<GameState>> {
			private readonly PlayersInArea players;
			private readonly Thing seen;

			internal ConnectionsInArea(PlayersInArea players, Thing seen) {
				this.players = players;
				this.seen = seen;
			}

			public Enumerator GetEnumerator() {
				return new Enumerator(this.players.GetEnumerator(), this.seen);
			}

			IEnumerator<TcpConnection<GameState>> IEnumerable<TcpConnection<GameState>>.GetEnumerator() {
				return this.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			public struct Enumerator : IEnumerator<TcpConnection<GameState>> {
				private PlayersInArea.Enumerator players;
				private readonly PlayersInArea.Enumerator start;
				private readonly Thing seen;
				private TcpConnection<GameState> current;

				internal Enumerator(PlayersInArea.Enumerator players, Thing seen) {
					this.players = players;
					this.start = players;
					this.seen = seen;
					this.current = null;
				}

				public TcpConnection<GameState> Current {
					get {
						return this.current;
					}
				}

				object IEnumerator.Current {
					get {
						return this.current;
					}
				}

				public bool MoveNext() {
					while (this.players.MoveNext()) {
						AbstractCharacter player = this.players.Current;
						GameState state = player.GameState;
						if ((state != null) && ((this.seen == null) || player.CanSeeForUpdate(this.seen).Allow)) {
							this.current = state.Conn;
							return true;
						}
					}
					this.current = null;
					return false;
				}

				public void Reset() {
					this.players = this.start;
					this.current = null;
				}

				public void Dispose() {
				}
			}
		}

		/// <summary>The statics and multi components in a rectangle, or the multi components only.</summary>
		public readonly struct InternalItemsInArea<T> : IEnumerable<T> where T : AbstractInternalItem {
			private readonly Map map;
			private readonly int minX, minY, maxX, maxY;
			private readonly bool withStatics;

			internal InternalItemsInArea(Map map, int minX, int minY, int maxX, int maxY, bool withStatics) {
				this.map = map;
				this.minX = minX;
				this.minY = minY;
				this.maxX = maxX;
				this.maxY = maxY;
				this.withStatics = withStatics;
			}

			public Enumerator GetEnumerator() {
				return new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY, this.withStatics);
			}

			IEnumerator<T> IEnumerable<T>.GetEnumerator() {
				return this.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}

			public struct Enumerator : IEnumerator<T> {
				private readonly Map map;
				private readonly int minX, minY, maxX, maxY;
				private readonly int xSectorStart, ySectorStart, xSectorEnd, ySectorEnd;
				private readonly bool withStatics;
				private int sx, sy;
				private Sector sector;
				private StaticItem[] statics; //null once the sector's statics are done
				private int staticsIndex;
				private MultiItemComponent nextComponent;
				private T current;

				internal Enumerator(Map map, int minX, int minY, int maxX, int maxY, bool withStatics) {
					this.map = map;
					this.minX = minX;
					this.minY = minY;
					this.maxX = maxX;
					this.maxY = maxY;
					this.withStatics = withStatics;
					map.GetSectorCoords(minX, minY, maxX, maxY, out this.xSectorStart, out this.ySectorStart, out this.xSectorEnd, out this.ySectorEnd);
					this.sx = this.xSectorStart;
					this.sy = this.ySectorStart - 1;
					this.sector = null;
					this.statics = null;
					this.staticsIndex = 0;
					this.nextComponent = null;
					this.current = null;
				}

				public T Current {
					get {
						return this.current;
					}
				}

				object IEnumerator.Current {
					get {
						return this.current;
					}
				}

				public bool MoveNext() {
					while (true) {
						StaticItem[] array = this.statics;
						if (array != null) {
							while (this.staticsIndex < array.Length) {
								StaticItem s = array[this.staticsIndex++];
								if (this.Contains(s)) {
									this.current = (T) (AbstractInternalItem) s;
									return true;
								}
							}
							this.statics = null;
							this.nextComponent = FirstComponent(this.sector);
						}
						MultiItemComponent component = this.nextComponent;
						if (component != null) {
							this.nextComponent = component.nextInList;
							if (this.Contains(component)) {
								this.current = (T) (AbstractInternalItem) component;
								return true;
							}
						} else if (!this.NextSector()) {
							this.current = null;
							return false;
						}
					}
				}

				private bool Contains(AbstractInternalItem item) {
					int x = item.X;
					int y = item.Y;
					return (this.minX <= x) && (this.minY <= y) && (this.maxX >= x) && (this.maxY >= y);
				}

				private static MultiItemComponent FirstComponent(Sector sector) {
					MultiComponentLinkedList list = sector.MultiComponents;
					return (list == null) ? null : list.firstMultiComponent;
				}

				private bool NextSector() {
					this.sy++;
					if (this.sy > this.ySectorEnd) {
						this.sy = this.ySectorStart;
						this.sx++;
					}
					if (this.sx > this.xSectorEnd) {
						this.sx = this.xSectorEnd + 1;
						this.sy = this.ySectorEnd;
						this.sector = null;
						return false;
					}
					this.sector = this.map.GetSector(this.sx, this.sy);
					if (this.withStatics) {
						this.statics = this.sector.Statics;
						this.staticsIndex = 0;
					} else {
						this.nextComponent = FirstComponent(this.sector);
					}
					return true;
				}

				public void Reset() {
					this = new Enumerator(this.map, this.minX, this.minY, this.maxX, this.maxY, this.withStatics);
				}

				public void Dispose() {
				}
			}
		}
	}
}
