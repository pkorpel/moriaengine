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

using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class SpellBookDef {
	}

	[ViewableClass]
	public partial class SpellBook {

		public int FirstSpellId {
			get {
				return this.TypeDef.FirstSpellId;
			}
		}

		public override void On_DenyDClick(DenyClickArgs args) {
			Character ch = (Character) args.ClickingChar;
			Thing cont = this.Cont;
			if (ch.IsGM || (cont == ch) || (cont == ch.Backpack)) {
				base.On_DenyDClick(args);
			}

			//necessary since DisplayTo implies this is in a client-visible and known container.
			args.Result = DenyResultMessages_SpellBook.Deny_MustBeInYourBackPack;
		}

		public override void On_DClick(AbstractCharacter from) {
			//base.On_DClick(from);
			this.DisplayTo((Character) from);
		}

		public void DisplayTo(Character viewer) {
			GameState state = viewer.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;

				//this is probably not necessary. We'll see if it breaks :)
				//PacketGroup pg = PacketGroup.AcquireSingleUsePG();
				//Thing cont = this.Cont;
				//if (cont == null) {
				//    ItemOnGroundUpdater updater = this.GetOnGroundUpdater();
				//    updater.SendTo(viewer, state, conn);
				//} else if (cont is Item) {
				//    pg.AcquirePacket<AddItemToContainerOutPacket>().Prepare(cont.FlaggedUid, this);
				//} else {
				//    pg.AcquirePacket<WornItemOutPacket>().PrepareItem(cont.FlaggedUid, this);
				//}
				//conn.SendPacketGroup(pg);

				PacketGroup pg = PacketGroup.AcquireSingleUsePG();
				pg.AcquirePacket<DrawContainerOutPacket>().PrepareSpellbook(this.FlaggedUid);
				conn.SendPacketGroup(pg);

				pg = PacketGroup.AcquireSingleUsePG();//the 2 packets can't be in one group. Don't ask me why.
				if (state.Version.NeedsNewSpellbook) {
					pg.AcquirePacket<NewSpellbookOutPacket>().Prepare(this.FlaggedUid, this.Model, this.FirstSpellId, this.contents);
				} else {
					pg.AcquirePacket<ItemsInContainerOutPacket>().PrepareSpellbook(this.FlaggedUid, this.FirstSpellId, this.contents);
				}
				conn.SendPacketGroup(pg);
			}
		}

		public bool AddSpell(SpellDef spell) {
			return this.AddSpell(spell.Id);
		}

		public bool AddSpell(int spellId) {
			int firstSpellId = this.FirstSpellId;
			if ((spellId >= firstSpellId) && (spellId < (firstSpellId + 64))) {
				ulong mask = (ulong) 1 << (spellId - firstSpellId);
				if ((this.contents & mask) == 0) {
					this.contents |= mask;
					this.Update();
					return true;
				}
			}
			return false;
		}

		public bool RemoveSpell(SpellDef spell) {
			return this.RemoveSpell(spell.Id);
		}

		public bool RemoveSpell(int spellId) {
			int firstSpellId = this.FirstSpellId;
			if ((spellId >= firstSpellId) && (spellId < (firstSpellId + 64))) {
				ulong mask = (ulong) 1 << (spellId - firstSpellId);
				if ((this.contents & mask) != 0) {
					this.contents &= ~mask;
					this.Update();
					return true;
				}
			}
			return false;
		}

		public bool HasSpell(SpellDef spell) {
			return this.HasSpell(spell.Id);
		}

		public bool HasSpell(int spellId) {
			int firstSpellId = this.FirstSpellId;
			if ((spellId >= firstSpellId) && (spellId < (firstSpellId + 64))) {
				ulong mask = (ulong) 1 << (spellId - firstSpellId);
				return (this.contents & mask) != 0;
			}
			return false;
		}

		public override void On_PutItemOn(ItemOnItemArgs args) {
			SpellScroll scroll = args.ManipulatedItem as SpellScroll;
			if (scroll != null) {
				if (this.AddSpell(scroll.SpellId)) {
					scroll.Delete();
				}
			}
		}
	}

	public class EmptyBookTargetDef : CompiledTargetDef {

		[SteamFunction]
		public static void EmptyBook(Player self) {
			if (!self.CheckAliveWithMessage()) {
				self.Target(SingletonScript<EmptyBookTargetDef>.Instance);
			}
		}

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<SpellBookLoc>.Get(self.Language).TargetBookToEmpty);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			SpellBook book = targetted as SpellBook;
			if (book != null) {
				if (self.CanPickUpWithMessage(book)) {
					Container cont = targetted.Cont as Container;
					if (cont == null) {
						cont = self.Backpack;
					}
					int firstSpellId = book.FirstSpellId;
					for (int i = firstSpellId, n = firstSpellId + 64; i < n; i++) {
						if (book.HasSpell(i)) {
							SpellDef spell = SpellDef.GetById(i);
							if (spell != null) {
								SpellScrollDef scrollDef = spell.ScrollItem;
								if (scrollDef != null) {
									scrollDef.Create(cont);
									book.RemoveSpell(i);
								}
							}
						}
					}
				}
				return TargetResult.Done;
			}
			return TargetResult.RestartTargetting; //neni to spellbook, restart target
		}
	}

	public static class DenyResultMessages_SpellBook {
		public static readonly DenyResult Deny_MustBeInYourBackPack =
			new DenyResult_ClilocSysMessage(500207); //The spellbook must be in your backpack (and not in a container within) to open.
	}

	public class SpellBookLoc : CompiledLocStringCollection<SpellBookLoc> {
		internal readonly string TargetBookToEmpty = "Ze které knihy chceš vysypat svitky?";
	}
}