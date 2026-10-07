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
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Regions;

namespace SteamEngine.Networking {

	//ushort size at the beginning of the packet


	public abstract class GameOutgoingPacket : OutgoingPacket {
		protected void EncodeStatVals(short curval, short maxval, bool showReal) {
			if (showReal) {
				this.EncodeShort(maxval);
				this.EncodeShort(curval);
			} else {
				this.EncodeShort(255);
				this.EncodeShort((short) ((curval << 8) / maxval));
			}
		}
	}

	public abstract class DynamicLengthOutPacket : GameOutgoingPacket {

		protected sealed override void Write() {
			this.SeekFromCurrent(2);
			this.WriteDynamicPart();
			this.SeekFromStart(1);
			this.EncodeUShort((ushort) this.CurrentSize);
		}

		protected abstract void WriteDynamicPart();

	}

	[CLSCompliant(false)]
	public abstract class GeneralInformationOutPacket : DynamicLengthOutPacket {
		public sealed override byte Id {
			get { return 0xBF; }
		}

		public abstract ushort SubCmdId {
			get;
		}

		protected sealed override void WriteDynamicPart() {
			this.EncodeUShort(this.SubCmdId);
			this.WriteSubCmd();
		}

		protected abstract void WriteSubCmd();

		public override string FullName {
			get {
				return string.Concat(this.Name,
					" ( 0x", this.Id.ToString("X", CultureInfo.InvariantCulture),
					"-0x", this.SubCmdId.ToString("X", CultureInfo.InvariantCulture), " )");
			}
		}

		[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Byte.ToString(System.String)"), SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.UInt16.ToString(System.String)")]
		public override string ToString() {
			return string.Concat("0x", this.Id.ToString("X"), "-0x", this.SubCmdId.ToString("X"));
		}
	}

	public sealed class LoginDeniedOutPacket : GameOutgoingPacket {
		byte why;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "why")]
		public void Prepare(LoginDeniedReason why) {
			this.why = (byte) why;
		}

		public override byte Id {
			get { return 0x82; }
		}

		protected override void Write() {
			this.EncodeByte(this.why);
		}
	}

	public sealed class EnableLockedClientFeaturesOutPacket : GameOutgoingPacket {
		ushort features;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "features")]
		public void Prepare(int features) {
			this.features = (ushort) features;
		}

		public override byte Id {
			get { return 0xB9; }
		}

		protected override void Write() {
			this.EncodeUShort(this.features);
		}
	}

	public sealed class CharactersListOutPacket : DynamicLengthOutPacket {
		string[] charNames = new string[AbstractAccount.maxCharactersPerGameAccount];
		int loginFlags;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "loginFlags")]
		public void Prepare(AbstractAccount charsSource, int loginFlags) {
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				AbstractCharacter ch = charsSource.GetCharacterInSlot(i);
				if (ch != null) {
					this.charNames[i] = ch.Name;
				} else {
					this.charNames[i] = null;
				}
			}
			this.loginFlags = loginFlags;
		}

		public override byte Id {
			get { return 0xA9; }
		}

		protected override void WriteDynamicPart() {
			this.SeekFromCurrent(1); //skip numOfCharacters byte

			int numOfCharacters = 0;
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				string name = this.charNames[i];
				if (name != null) {
					this.EncodeASCIIString(name, 30);
					this.EncodeZeros(30);
					numOfCharacters++;
				} else {
					this.EncodeZeros(60);
				}
			}

			this.EncodeByte(1);//just 1 location, no meaning anyway
			this.EncodeByte(0);//index 0
			this.EncodeASCIIString("London", 31);
			this.EncodeASCIIString("London", 31);

			//TODO: Login Flags as bools in globals.
			//Login Flags:
			//0x14 = One character only
			//0x08 = Right-click menus
			//0x20 = AOS features
			//0x40 = Six characters instead of five
			this.EncodeInt(this.loginFlags);

			this.SeekFromStart(3);
			this.EncodeByte((byte) numOfCharacters);
		}
	}

	public sealed class LoginConfirmationOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, x, y, mapSizeX, mapSizeY;
		sbyte z;
		byte direction;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "mapSizeX-8"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "mapSizeY"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "mapSizeX")]
		public void Prepare(AbstractCharacter chr, int mapSizeX, int mapSizeY) {
			this.flaggedUid = chr.FlaggedUid;
			this.model = chr.ShortModel;
			this.x = (ushort) chr.X;
			this.y = (ushort) chr.Y;
			this.z = (sbyte) chr.Z;
			this.direction = chr.DirectionByte;

			this.mapSizeX = (ushort) (mapSizeX - 8);
			this.mapSizeY = (ushort) mapSizeY;
		}

		public override byte Id {
			get { return 0x1B; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeZeros(4);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(0);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.direction);
			this.EncodeZeros(9);
			//EncodeByte(0xff, 19); //old SE code was sending 0xff, dunno why
			//EncodeByte(0xff, 20);
			//EncodeByte(0xff, 21);
			//EncodeByte(0xff, 22);
			this.EncodeUShort(this.mapSizeX);
			this.EncodeUShort(this.mapSizeY);
			this.EncodeZeros(6);
		}
	}

	[CLSCompliant(false)]
	public sealed class SetFacetOutPacket : GeneralInformationOutPacket {
		byte facet;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "facet")]
		public void Prepare(int facet) {
			this.facet = (byte) facet;
		}

		public override ushort SubCmdId {
			get { return 0x08; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(this.facet);
		}
	}

	public sealed class SeasonalInformationOutPacket : GameOutgoingPacket {
		byte season, cursor;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "cursor"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "season")]
		public void Prepare(Season season, CursorType cursor) {
			this.season = (byte) season;
			this.cursor = (byte) cursor;
		}

		public override byte Id {
			get { return 0xBC; }
		}

		protected override void Write() {
			this.EncodeByte(this.season);
			this.EncodeByte(this.cursor);
		}
	}

	public sealed class DrawGamePlayerOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, color, x, y;
		sbyte z;
		byte flagsToSend, direction;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void Prepare(GameState state, AbstractCharacter ch) {
			this.flaggedUid = ch.FlaggedUid;
			this.model = ch.ShortModel;
			this.color = ch.ShortColor;
			MutablePoint4D p = ch.point4d;
			this.x = p.x;
			this.y = p.y;
			this.z = p.z;
			this.flagsToSend = ch.FlagsToSend;
			this.direction = ch.DirectionByte;

			state.movementState.ResetMovementSequence();
		}

		public override byte Id {
			get { return 0x20; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeZeros(2);
			this.EncodeByte(this.direction);
			this.EncodeSByte(this.z);
		}
	}

	public sealed class SetWarModeOutPacket : GameOutgoingPacket {
		bool warModeEnabled;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "warModeEnabled")]
		public void Prepare(bool warModeEnabled) {
			this.warModeEnabled = warModeEnabled;
		}

		public override byte Id {
			get { return 0x72; }
		}

		protected override void Write() {
			this.EncodeBool(this.warModeEnabled);
			this.EncodeByte(0);
			this.EncodeByte(32);
			this.EncodeByte(0);
		}
	}

	public sealed class ClientViewRangeOutPacket : GameOutgoingPacket {
		byte range;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "range")]
		public void Prepare(int range) {
			this.range = (byte) range;
		}

		public override byte Id {
			get { return 0xC8; }
		}

		protected override void Write() {
			this.EncodeByte(this.range);
		}
	}

	public sealed class LoginCompleteOutPacket : GameOutgoingPacket {
		public override byte Id {
			get { return 0x55; }
		}

		protected override void Write() {
		}
	}

	public sealed class DrawObjectOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, x, y, color;
		sbyte z;
		byte dir, flagsToSend, highlight;
		List<ItemInfo> items = new List<ItemInfo>();


		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly ushort color, model;
			internal readonly byte layer;

			internal ItemInfo(uint flaggedUid, int color, int model, int layer) {
				this.flaggedUid = flaggedUid;
				this.color = (ushort) color;
				this.model = (ushort) model;
				this.layer = (byte) layer;
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "highlight")]
		public void Prepare(AbstractCharacter ch, HighlightColor highlight) {
			this.flaggedUid = ch.FlaggedUid;
			this.model = ch.ShortModel;
			MutablePoint4D point = ch.point4d;
			this.x = point.x;
			this.y = point.y;
			this.z = point.z;
			this.dir = ch.DirectionByte;
			this.color = ch.ShortColor;
			this.flagsToSend = ch.FlagsToSend;
			this.highlight = (byte) highlight;

			this.items.Clear();
			foreach (AbstractItem i in ch.VisibleEquip) {
				this.items.Add(new ItemInfo(i.FlaggedUid, i.ShortColor, i.ShortModel, i.Layer));
			}

			AbstractCharacter mount = ch.Mount;
			if (mount != null) {
				this.items.Add(new ItemInfo(mount.FlaggedUid | 0x40000000, mount.ShortColor, mount.MountItem, (int) LayerNames.Mount));
			}
		}

		public override byte Id {
			get { return 0x78; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.dir);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeByte(this.highlight);

			foreach (ItemInfo i in this.items) {
				this.EncodeUInt(i.flaggedUid);
				if (i.color == 0) {
					this.EncodeUShort(i.model);
					this.EncodeByte(i.layer);
				} else {
					this.EncodeUShort((ushort) (i.model | 0x8000));
					this.EncodeByte(i.layer);
					this.EncodeUShort(i.color);
				}
			}

			this.EncodeZeros(4);
		}
	}

	public sealed class UpdatePlayerPacket : GameOutgoingPacket {
		uint flaggedUid;
		ushort model, x, y, color;
		sbyte z;
		byte dir, flagsToSend, highlight;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "highlight")]
		public void Prepare(AbstractCharacter chr, bool running, HighlightColor highlight) {
			this.flaggedUid = chr.FlaggedUid;
			this.model = chr.ShortModel;
			this.x = (ushort) chr.X;
			this.y = (ushort) chr.Y;
			this.z = (sbyte) chr.Z;
			this.color = chr.ShortColor;
			this.flagsToSend = chr.FlagsToSend;
			this.highlight = (byte) highlight;

			this.dir = chr.DirectionByte;
			if (running) {
				this.dir |= 0x80;
			}
		}

		public override byte Id {
			get { return 0x77; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeSByte(this.z);
			this.EncodeByte(this.dir);
			this.EncodeUShort(this.color);
			this.EncodeByte(this.flagsToSend);
			this.EncodeByte(this.highlight);
		}
	}

	public sealed class DrawContainerOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		short gump;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "gump"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), CLSCompliant(false)]
		public void PrepareContainer(uint flaggedUid, short gump) {
			this.flaggedUid = flaggedUid;
			this.gump = gump;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), CLSCompliant(false)]
		public void PrepareSpellbook(uint flaggedUid) {
			this.flaggedUid = flaggedUid;
			this.gump = -1;
		}

		public override byte Id {
			get { return 0x24; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeShort(this.gump);
		}
	}

	public sealed class AddItemToContainerOutPacket : GameOutgoingPacket {
		uint flaggedUid, contFlaggedUid;
		ushort model, x, y, color, amount;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "contFlaggedUid"), CLSCompliant(false)]
		public void Prepare(uint contFlaggedUid, AbstractItem i) {
			this.flaggedUid = i.FlaggedUid;
			this.contFlaggedUid = contFlaggedUid;
			this.model = i.ShortModel;
			this.x = (ushort) i.X;
			this.y = (ushort) i.Y;
			this.color = i.ShortColor;
			this.amount = i.ShortAmount;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), CLSCompliant(false)]
		public void PrepareItemInCorpse(uint corpseUid, ICorpseEquipInfo i) {
			this.flaggedUid = i.FlaggedUid;
			this.contFlaggedUid = corpseUid;
			this.model = (ushort) i.Model;
			this.x = 0;
			this.y = 0;
			this.color = (ushort) i.Color;
			this.amount = 1;
		}

		public override byte Id {
			get { return 0x25; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0); //unknown
			this.EncodeUShort(this.amount);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeUInt(this.contFlaggedUid);
			this.EncodeUShort(this.color);
		}
	}

	public sealed class CorpseClothingOutPacket : DynamicLengthOutPacket {
		List<ItemInfo> items = new List<ItemInfo>();
		uint corpseUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "corpseUid"), CLSCompliant(false)]
		public void Prepare(uint corpseUid, IEnumerable<ICorpseEquipInfo> equippedItems) {
			this.corpseUid = corpseUid;

			this.items.Clear();
			foreach (ICorpseEquipInfo iulp in equippedItems) {
				this.items.Add(new ItemInfo(iulp.FlaggedUid, iulp.Layer));
			}
		}

		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly byte layer;

			internal ItemInfo(uint flaggedUid, int layer) {
				this.flaggedUid = flaggedUid;
				this.layer = (byte) layer;
			}
		}

		public override byte Id {
			get { return 0x89; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.corpseUid);
			foreach (ItemInfo i in this.items) {
				this.EncodeByte(i.layer);
				this.EncodeUInt(i.flaggedUid);
			}
			this.EncodeByte(0); //terminator
		}
	}

	public sealed class ItemsInContainerOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		private List<ItemInfo> items = new List<ItemInfo>();

		private struct ItemInfo {
			internal readonly uint flaggedUid;
			internal readonly ushort color, model, amount, x, y;

			internal ItemInfo(AbstractItem i) {
				this.flaggedUid = i.FlaggedUid;
				this.color = i.ShortColor;
				this.model = i.ShortModel;
				this.amount = i.ShortAmount;
				this.x = (ushort) i.X;
				this.y = (ushort) i.Y;
			}

			internal ItemInfo(ICorpseEquipInfo i) {
				this.flaggedUid = i.FlaggedUid;
				this.color = (ushort) i.Color;
				this.model = (ushort) i.Model;
				this.amount = 1;
				this.x = 0;
				this.y = 0;
			}

			internal ItemInfo(uint flaggedUid, ushort amount) {
				this.flaggedUid = flaggedUid;
				this.color = 0;
				this.model = 0;
				this.amount = amount;
				this.x = 0;
				this.y = 0;
			}
		}

		public bool PrepareContainer(AbstractItem cont, AbstractCharacter viewer, IList<AbstractItem> visibleItems) {
			this.flaggedUid = cont.FlaggedUid;

			this.items.Clear();

			foreach (AbstractItem i in cont) {
				if (viewer.CanSeeVisibility(i)) {
					this.items.Add(new ItemInfo(i));
					visibleItems.Add(i);
				}
			}

			return this.items.Count > 0;
		}

		[CLSCompliant(false)]
		public bool PrepareCorpse(uint corpseUid, IEnumerable<ICorpseEquipInfo> equippedItems) {
			this.flaggedUid = corpseUid;

			this.items.Clear();
			foreach (ICorpseEquipInfo i in equippedItems) {
				this.items.Add(new ItemInfo(i));
			}

			return this.items.Count > 0;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), CLSCompliant(false)]
		public void PrepareSpellbook(uint flaggedUid, int offset, ulong content) {
			this.flaggedUid = flaggedUid;

			this.items.Clear();

			ulong mask = 1;
			for (int i = 0; i < 64; i++, mask <<= 1) {
				if ((content & mask) != 0) {
					this.items.Add(new ItemInfo((uint) (0x7FFFFFFF - i), (ushort) (i + offset)));
				}
			}
		}

		public override byte Id {
			get { return 0x3c; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUShort((ushort) this.items.Count);

			foreach (ItemInfo i in this.items) {
				this.EncodeUInt(i.flaggedUid);
				this.EncodeUShort(i.model);
				this.EncodeByte(0);
				this.EncodeUShort(i.amount);
				this.EncodeUShort(i.x);
				this.EncodeUShort(i.y);
				//TODO? post 6.0.1.7: byte gridindex
				this.EncodeUInt(this.flaggedUid);
				this.EncodeUShort(i.color);
			}
		}
	}

	[CLSCompliant(false)]
	public sealed class NewSpellbookOutPacket : GeneralInformationOutPacket {
		uint flaggedUid;
		ushort bookModel;
		short firstSpellId;
		ulong content;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "firstSpellId"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "content"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "bookModel")]
		public void Prepare(uint bookUid, int bookModel, int firstSpellId, ulong content) {
			this.flaggedUid = bookUid;
			this.bookModel = (ushort) bookModel;
			this.firstSpellId = (short) firstSpellId;
			this.content = content;
		}

		public override ushort SubCmdId {
			get { return 0x1b; }
		}

		protected override void WriteSubCmd() {
			this.EncodeShort(1);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.bookModel);
			this.EncodeShort(this.firstSpellId);

			for (int i = 0; i < 8; i++) {
				this.EncodeByte((byte) (this.content >> (i * 8)));
			}
		}
	}

	[CLSCompliant(false)]
	public sealed class OldPropertiesRefreshOutPacket : GeneralInformationOutPacket {
		uint flaggedUid;
		int propertiesUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "propertiesUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid")]
		public void Prepare(uint flaggedUid, int propertiesUid) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
		}

		public override ushort SubCmdId {
			get { return 0x10; }
		}

		protected override void WriteSubCmd() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeInt(this.propertiesUid);
		}
	}

	public sealed class PropertiesRefreshOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		int propertiesUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "propertiesUid"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid, int propertiesUid) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
		}

		public override byte Id {
			get { return 0xdc; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeInt(this.propertiesUid);
		}
	}

	public sealed class ObjectInfoOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort amount, model, x, y, color;
		sbyte z;
		byte dir, flagsToSend;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void Prepare(AbstractItem item, MoveRestriction restrict) {
			this.flaggedUid = item.FlaggedUid;
			this.amount = item.ShortAmount;
			this.model = item.ShortModel;
			MutablePoint4D point = item.point4d;
			this.x = point.x;
			this.y = point.y;
			this.z = point.z;
			this.dir = item.DirectionByte;
			this.flagsToSend = item.FlagsToSend;
			if (restrict == MoveRestriction.Movable) {
				this.flagsToSend |= 0x20;
			}
			this.color = (ushort) item.Color;
		}

		/// <summary>
		/// Prepare method for creating the 'fake item' packets
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "3#"), SuppressMessage("Microsoft.Naming", "CA1718:AvoidLanguageSpecificTypeNamesInParameters", MessageId = "3#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "model"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "dir"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "color")]
		[CLSCompliant(false)]
		public void PrepareFakeItem(uint itemUid, int model, IPoint3D point3D, ushort shortAmount, Direction dir, int color) {
			//this must be the item UID (containing 0x40000000)
			this.flaggedUid = itemUid | 0x40000000;
			this.amount = shortAmount;
			this.model = (ushort) model;
			this.x = (ushort) point3D.X;
			this.y = (ushort) point3D.Y;
			this.z = (sbyte) point3D.Z;
			this.dir = (byte) dir;
			this.flagsToSend = 0x00;
			this.color = (ushort) color;
		}

		public override byte Id {
			get { return 0x1A; }
		}

		protected override void WriteDynamicPart() {
			if (this.amount != 1) {
				this.EncodeUInt(this.flaggedUid | 0x80000000);
				this.EncodeUShort(this.model);
				this.EncodeUShort(this.amount);
			} else {
				this.EncodeUInt(this.flaggedUid);
				this.EncodeUShort(this.model);
			}

			if (this.dir != 0) {
				this.x |= 0x8000;
			}
			this.EncodeUShort(this.x);
			if (this.color != 0) {
				this.y |= 0x8000;
			}
			if (this.flagsToSend != 0) {
				this.y |= 0x4000;
			}
			this.EncodeUShort(this.y);
			if (this.dir != 0) {
				this.EncodeByte(this.dir);
			}
			this.EncodeSByte(this.z);

			if (this.color != 0) {
				this.EncodeUShort(this.color);
			}
			if (this.flagsToSend != 0) {
				this.EncodeByte(this.flagsToSend);
			}
		}
	}

	public sealed class MobAttributesOutPacket : GameOutgoingPacket {
		bool showReal;
		short mana, maxMana, hits, maxHits, stam, maxStam;
		uint flaggedUid;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "showReal")]
		public void Prepare(AbstractCharacter cre, bool showReal) {
			this.flaggedUid = cre.FlaggedUid;
			this.mana = cre.Mana;
			this.maxMana = cre.MaxMana;
			this.hits = cre.Hits;
			this.maxHits = cre.MaxHits;
			this.stam = cre.Stam;
			this.maxStam = cre.MaxStam;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0x2d; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.hits, this.maxHits, this.showReal);
			this.EncodeStatVals(this.mana, this.maxMana, this.showReal);
			this.EncodeStatVals(this.stam, this.maxStam, this.showReal);
		}
	}

	public sealed class UpdateCurrentHealthOutPacket : GameOutgoingPacket {
		bool showReal;
		short hits, maxHits;
		uint flaggedUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "hits"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "maxHits"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "showReal"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid, short hits, short maxHits, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.hits = hits;
			this.maxHits = maxHits;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa1; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.hits, this.maxHits, this.showReal);
		}
	}

	public sealed class UpdateCurrentManaOutPacket : GameOutgoingPacket {
		bool showReal;
		short mana, maxMana;
		uint flaggedUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "mana"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "maxMana"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "showReal"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid, short mana, short maxMana, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.mana = mana;
			this.maxMana = maxMana;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa2; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.mana, this.maxMana, this.showReal);
		}
	}

	public sealed class UpdateCurrentStaminaOutPacket : GameOutgoingPacket {
		bool showReal;
		short stam, maxStam;
		uint flaggedUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "maxStam"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "showReal"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "stam"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid, short stam, short maxStam, bool showReal) {
			this.flaggedUid = flaggedUid;
			this.stam = stam;
			this.maxStam = maxStam;
			this.showReal = showReal;
		}

		public override byte Id {
			get { return 0xa3; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeStatVals(this.stam, this.maxStam, this.showReal);
		}
	}

	public sealed class StatusBarInfoOutPacket : DynamicLengthOutPacket {
		StatusBarType type;
		uint flaggedUid, gold;
		string charName;
		short mana, maxMana, hits, maxHits, stam, maxStam, strength, dexterity, intelligence, armor,
			fireResist, coldResist, poisonResist, energyResist, luck, minDamage, maxDamage, tithingPoints, statCap;
		byte currentPets, maxPets;
		ushort weight;
		bool isFemale;
		bool canRenameSelf;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type")]
		public void Prepare(AbstractCharacter ch, StatusBarType type) {
			Sanity.IfTrueThrow(ch == null, "PrepareStatusBar called with a null character.");
			//Sanity.IfTrueThrow(!Enum.IsDefined(typeof(StatusBarType), type), "Invalid value " + type + " for StatusBarType in PrepareStatusBar.");

			this.type = type;
			this.charName = ch.Name;

			this.hits = ch.Hits;
			this.maxHits = ch.MaxHits;
			this.flaggedUid = ch.FlaggedUid;

			if (type == StatusBarType.Me) {
				this.mana = ch.Mana;
				this.maxMana = ch.MaxMana;
				this.stam = ch.Stam;
				this.maxStam = ch.MaxStam;

				this.strength = ch.Str;
				this.dexterity = ch.Dex;
				this.intelligence = ch.Int;

				long lgold = ch.Gold;
				this.gold = (uint) (lgold > 0xffffffff ? 0xffffffff : lgold);
				this.armor = ch.StatusArmorClass;
				this.weight = (ushort) ch.Weight;

				this.currentPets = ch.ExtendedStatusNum07;
				this.maxPets = ch.ExtendedStatusNum08;
				this.statCap = ch.ExtendedStatusNum09;

				this.fireResist = ch.ExtendedStatusNum01;
				this.coldResist = ch.ExtendedStatusNum02;
				this.poisonResist = ch.ExtendedStatusNum03;
				this.energyResist = ch.StatusMindDefense;
				this.luck = ch.ExtendedStatusNum04;
				this.minDamage = ch.ExtendedStatusNum05;
				this.maxDamage = ch.ExtendedStatusNum06;
				this.tithingPoints = ch.TithingPoints;

				this.isFemale = ch.IsFemale;

				this.canRenameSelf = ch.CanRename(ch);
			}
		}

		public override byte Id {
			get { return 0x11; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeASCIIString(this.charName, 30);

			if (this.type == StatusBarType.Me) {
				this.EncodeShort(this.hits);
				this.EncodeShort(this.maxHits);
				this.EncodeBool(this.canRenameSelf);
				this.EncodeByte(4); //more data following
			} else {
				this.EncodeShort((short) ((this.hits << 8) / this.maxHits));
				this.EncodeShort(256);
				if (this.type == StatusBarType.Pet) {
					this.EncodeByte(1);
				} else {
					this.EncodeByte(0);
				}
				this.EncodeByte(0); //no data following
				return;
			}

			this.EncodeBool(this.isFemale);

			this.EncodeShort(this.strength);
			this.EncodeShort(this.dexterity);
			this.EncodeShort(this.intelligence);
			this.EncodeShort(this.stam);
			this.EncodeShort(this.maxStam);
			this.EncodeShort(this.mana);
			this.EncodeShort(this.maxMana);
			this.EncodeUInt(this.gold);
			this.EncodeShort(this.armor);
			this.EncodeUShort(this.weight);

			//If (flag 5 or higher)
			//* BYTE[2] Max Weight
			//* BYTE[1] Race

			this.EncodeShort(this.statCap);
			this.EncodeByte(this.currentPets);
			this.EncodeByte(this.maxPets);

			this.EncodeShort(this.fireResist);
			this.EncodeShort(this.coldResist);
			this.EncodeShort(this.poisonResist);
			this.EncodeShort(this.energyResist);
			this.EncodeShort(this.luck);
			this.EncodeShort(this.minDamage);
			this.EncodeShort(this.maxDamage);
			this.EncodeInt(this.tithingPoints);

			//If (flag 6 or higher)
			//* BYTE[2] Hit Chance Increase
			//* BYTE[2] Swing Speed Increase
			//* BYTE[2] Damage Chance Increase
			//* BYTE[2] Lower Reagent Cost
			//* BYTE[2] Hit Points Regeneration
			//* BYTE[2] Stamina Regeneration
			//* BYTE[2] Mana Regeneration
			//* BYTE[2] Reflect Physical Damage
			//* BYTE[2] Enhance Potions
			//* BYTE[2] Defense Chance Increase
			//* BYTE[2] Spell Damage Increase
			//* BYTE[2] Faster Cast Recovery
			//* BYTE[2] Faster Casting
			//* BYTE[2] Lower Mana Cost
			//* BYTE[2] Strength Increase
			//* BYTE[2] Dexterity Increase
			//* BYTE[2] Intelligence Increase
			//* BYTE[2] Hit Points Increase
			//* BYTE[2] Stamina Increase
			//* BYTE[2] Mana Increase
			//* BYTE[2] Maximum Hit Points Increase
			//* BYTE[2] Maximum Stamina Increase
			//* BYTE[2] Maximum Mana Increase
		}
	}


	public sealed class SendSkillsOutPacket : DynamicLengthOutPacket {
		List<SkillInfo> skillList = new List<SkillInfo>();
		bool displaySkillCaps, singleSkill;
		byte type;
		//0x00= full list, 0xFF = single skill update, 
		//0x02 full list with skillcap, 0xDF single skill update with cap

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "displaySkillCaps")]
		public void PrepareAllSkillsUpdate(IEnumerable<ISkill> skills, bool displaySkillCaps) {
			this.singleSkill = false;
			this.displaySkillCaps = displaySkillCaps;
			if (displaySkillCaps) {
				this.type = 0x02;//full list with skillcaps
			} else {
				this.type = 0x00;//full list without skillcaps
			}

			this.skillList.Clear();
			foreach (ISkill s in skills) {
				this.skillList.Add(new SkillInfo(s.Id, s.RealValue, s.ModifiedValue, s.Cap, s.Lock));
			}
		}

		public void PrepareSingleSkillUpdate(int skillId, ISkill skill, bool displaySkillCaps) {
			int realValue, modifiedValue, cap;
			SkillLockType skillLock;
			if (skill == null) {
				realValue = 0;
				modifiedValue = 0;
				cap = 1000;
				skillLock = SkillLockType.Up;
			} else {
				realValue = skill.RealValue;
				modifiedValue = skill.ModifiedValue;
				cap = skill.Cap;
				skillLock = skill.Lock;
			}

			if (displaySkillCaps) {
				this.PrepareSingleSkillUpdate(skillId, realValue, modifiedValue, skillLock, cap);
			} else {
				this.PrepareSingleSkillUpdate(skillId, realValue, modifiedValue, skillLock);
			}
		}

		public void PrepareSingleSkillUpdate(int skillId, int realValue, int modifiedValue, SkillLockType skillLock) {
			this.displaySkillCaps = false;
			this.singleSkill = true;
			this.type = 0xFF; //partial list without caps
			this.skillList.Clear();
			this.skillList.Add(new SkillInfo(skillId, realValue, modifiedValue, 0, skillLock));
		}

		public void PrepareSingleSkillUpdate(int skillId, int realValue, int modifiedValue, SkillLockType skillLock, int cap) {
			this.displaySkillCaps = true;
			this.singleSkill = true;
			this.type = 0xDF; //partial list with caps
			this.skillList.Clear();
			this.skillList.Add(new SkillInfo(skillId, realValue, modifiedValue, cap, skillLock));
		}

		private struct SkillInfo {
			public readonly ushort realValue, modifiedValue, cap, id;
			public readonly SkillLockType skillLock;

			public SkillInfo(int id, int realValue, int modifiedValue, int cap, SkillLockType skillLock) {
				this.realValue = (ushort) realValue;
				this.modifiedValue = (ushort) modifiedValue;
				this.cap = (ushort) cap;
				this.id = (ushort) id;
				this.skillLock = skillLock;
			}
		}

		public override byte Id {
			get { return 0x3A; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeByte(this.type);

			foreach (SkillInfo s in this.skillList) {
				if (this.singleSkill) {
					this.EncodeUShort(s.id);
				} else {
					this.EncodeUShort((ushort) (s.id + 1));
				}
				this.EncodeUShort(s.modifiedValue);
				this.EncodeUShort(s.realValue);
				this.EncodeByte((byte) s.skillLock);
				if (this.displaySkillCaps) {
					this.EncodeUShort(s.cap);
				}
			}

			//if (this.type == 0x00) {
			//    this.EncodeUShort(0);
			//}
		}
	}


	public sealed class DeleteObjectOutPacket : GameOutgoingPacket {
		uint flaggedUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid) {
			this.flaggedUid = flaggedUid;
		}

		public void Prepare(int uid) {
			this.flaggedUid = (uint) uid;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void Prepare(Thing thing) {
			this.flaggedUid = thing.FlaggedUid;
		}

		public override byte Id {
			get { return 0x1d; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
		}
	}

	public sealed class MegaClilocOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		int propertiesUid;
		IList<int> ids;
		IList<string> strings;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "strings"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "propertiesUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "ids"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid"), CLSCompliant(false)]
		public void Prepare(uint flaggedUid, int propertiesUid, IList<int> ids, IList<string> strings) {
			this.flaggedUid = flaggedUid;
			this.propertiesUid = propertiesUid;
			this.ids = ids;
			this.strings = strings;
		}

		public override byte Id {
			get { return 0xd6; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeShort(0x0001);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeZeros(2);
			this.EncodeInt(this.propertiesUid);

			for (int i = 0, n = this.ids.Count; i < n; i++) {
				this.EncodeInt(this.ids[i]);
				string msg = this.strings[i];
				if (msg == null) {
					msg = "";
				}
				this.EncodeLittleEndianUnicodeStringWithLen(msg);
			}

			this.EncodeZeros(4);
		}
	}

	public sealed class SendSpeechOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, message;
		byte type;

		//from can be null
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "font"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "color"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type")]
		public void Prepare(Thing from, string message, string sourceName, SpeechType type, ClientFont font, int color) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.ShortModel;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.type = (byte) type;
			this.color = (ushort) Utility.NormalizeDyedColor(color, Globals.DefaultAsciiMessageColor);
			this.font = (ushort) Utility.NormalizeClientFont(font);
		}

		public override byte Id {
			get { return 0x1c; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeASCIIStringTerminated(this.message);
		}
	}

	public sealed class UnicodeSpeechOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, message, language;
		byte type;

		//from can be null
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "color"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "font"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "language"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type")]
		public void Prepare(Thing from, string message, string sourceName, SpeechType type, ClientFont font, int color, string language) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.ShortModel;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.language = language;
			this.type = (byte) type;
			this.color = (ushort) Utility.NormalizeDyedColor(color, Globals.DefaultUnicodeMessageColor);
			this.font = (ushort) Utility.NormalizeClientFont(font);
		}

		public override byte Id {
			get { return 0xAE; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeASCIIString(this.language, 4);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeBigEndianUnicodeString(this.message);
			this.EncodeZeros(2);
		}
	}

	public sealed class ClilocMessageOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, args;
		int message;
		byte type;

		//from can be null
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "font"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "color"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "args")]
		public void Prepare(Thing from, int message, string sourceName, SpeechType type, ClientFont font, int color, string args) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.ShortModel;
			}

			this.sourceName = sourceName;
			this.message = message;
			this.args = args;
			this.type = (byte) type;
			this.color = (ushort) Utility.NormalizeDyedColor(color, Globals.DefaultUnicodeMessageColor); ;
			this.font = (ushort) Utility.NormalizeClientFont(font);
		}

		public override byte Id {
			get { return 0xC1; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeInt(this.message);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeLittleEndianUnicodeString(this.args);
			this.EncodeZeros(2);//msg terminator
		}
	}

	public enum AffixType {
		Append = 0x00,
		Prepend = 0x01
		//System = 0x02
	}

	public sealed class ClilocMessageAffixOutPacket : DynamicLengthOutPacket {
		uint flaggedUid;
		ushort model, color, font;
		string sourceName, args, affix;
		int message;
		byte type, flags;

		//from can be null
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceName"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "color"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "font"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flags"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "args"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "affix"), CLSCompliant(false)]
		public void Prepare(Thing from, int message, string sourceName, SpeechType type, ClientFont font, int color, AffixType flags, string affix, string args) {
			if (from == null) {
				this.flaggedUid = 0xffffffff;
				this.model = 0xffff;
				this.flags = 0x02;
			} else {
				this.flaggedUid = from.FlaggedUid;
				this.model = from.ShortModel;
				this.flags = 0x00;
			}

			this.flags |= (byte) flags;
			this.sourceName = sourceName;
			this.message = message;
			this.args = args;
			this.type = (byte) type;
			this.color = (ushort) Utility.NormalizeDyedColor(color, Globals.DefaultUnicodeMessageColor); ;
			this.font = (ushort) Utility.NormalizeClientFont(font);
			this.affix = affix;
		}

		public override byte Id {
			get { return 0xCC; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(this.type);
			this.EncodeUShort(this.color);
			this.EncodeUShort(this.font);
			this.EncodeInt(this.message);
			this.EncodeByte(this.flags);
			this.EncodeASCIIString(this.sourceName, 30);
			this.EncodeASCIIStringTerminated(this.affix);
			this.EncodeBigEndianUnicodeString(this.args);
			this.EncodeZeros(2);//msg terminator
		}
	}

	public sealed class TargetCursorCommandsOutPacket : GameOutgoingPacket {
		byte type;
		byte cursorType;//1 = Harmful, 2 = Beneficial 3 = cancel

		public void Prepare(bool ground) {
			this.type = ground ? (byte) 1 : (byte) 0;
			this.cursorType = 0;
		}

		public void PrepareAsCancel() {
			this.type = 0;
			this.cursorType = 3;
		}

		public override byte Id {
			get { return 0x6c; }
		}

		protected override void Write() {
			this.EncodeByte(this.type);
			this.EncodeZeros(4);
			this.EncodeByte(this.cursorType);
			this.EncodeZeros(12);
		}
	}

	public sealed class GiveBoatOrHousePlacementViewOutPacket : GameOutgoingPacket {
		ushort model;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "model")]
		public void Prepare(int model) {
			this.model = (ushort) model;
		}

		public override byte Id {
			get { return 0x99; }
		}

		protected override void Write() {
			this.EncodeByte(1);
			this.EncodeZeros(16);
			this.EncodeShort((short) (this.model & 0x8fff));
			this.EncodeZeros(6);
		}
	}

	public sealed class OpenPaperdollOutPacket : GameOutgoingPacket {
		uint flaggedUid;
		string text;
		byte flagsToSend;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void Prepare(AbstractCharacter character, bool canEquip) {
			this.flaggedUid = character.FlaggedUid;
			this.text = character.PaperdollName;
			this.flagsToSend = character.FlagsToSend;

			Sanity.IfTrueThrow((this.flagsToSend & 0x02) > 0, character + "'s FlagsToSend included 0x02, which is the 'can equip items on' flag for paperdoll packets - FlagsToSend should never include it.");
			if (canEquip) {	//include 0x02 if we can equip stuff on them.
				this.flagsToSend |= 0x02;
			}
		}

		public override byte Id {
			get { return 0x88; }
		}

		protected override void Write() {
			this.EncodeUInt(this.flaggedUid);
			this.EncodeASCIIString(this.text, 60);
			this.EncodeByte(this.flagsToSend);
		}
	}

	public sealed class WornItemOutPacket : GameOutgoingPacket {
		uint itemFlaggedUid, charUid;
		sbyte layer;
		ushort model, color;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "charUid"), CLSCompliant(false)]
		public void PrepareItem(uint charUid, AbstractItem item) {
			this.charUid = charUid;
			this.itemFlaggedUid = item.FlaggedUid;
			this.layer = (sbyte) item.Z;
			this.model = item.ShortModel;
			this.color = item.ShortColor;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "charUid"), CLSCompliant(false)]
		public void PrepareMount(uint charUid, AbstractCharacter mount) {
			this.charUid = charUid;
			this.itemFlaggedUid = (uint) (mount.Uid | 0x40000000);
			this.layer = (sbyte) LayerNames.Mount;
			this.model = (ushort) mount.MountItem;
			this.color = mount.ShortColor;
		}

		public override byte Id {
			get { return 0x2e; }
		}

		protected override void Write() {
			this.EncodeUInt(this.itemFlaggedUid);
			this.EncodeUShort(this.model);
			this.EncodeByte(0);
			this.EncodeSByte(this.layer);
			this.EncodeUInt(this.charUid);
			this.EncodeUShort(this.color);
		}
	}

	public sealed class CharacterMoveAcknowledgeOutPacket : GameOutgoingPacket {
		byte sequence, highlight;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sequence"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "highlight")]
		public void Prepare(byte sequence, HighlightColor highlight) {
			this.sequence = sequence;
			this.highlight = (byte) highlight;
		}

		public override byte Id {
			get { return 0x22; }
		}

		protected override void Write() {
			this.EncodeByte(this.sequence);
			this.EncodeByte(this.highlight);
		}
	}

	public sealed class CharMoveRejectionOutPacket : GameOutgoingPacket {
		byte sequence, direction;
		ushort x, y;
		sbyte z;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sequence")]
		public void Prepare(byte sequence, AbstractCharacter ch) {
			this.sequence = sequence;
			this.direction = ch.DirectionByte;
			this.x = (ushort) ch.X;
			this.y = (ushort) ch.Y;
			this.z = (sbyte) ch.Z;
		}

		public override byte Id {
			get { return 0x21; }
		}

		protected override void Write() {
			this.EncodeByte(this.sequence);
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(this.direction);
			this.EncodeSByte(this.z);
		}
	}

	public sealed class RejectMoveItemRequestOutPacket : GameOutgoingPacket {
		byte denyResult;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "denyResult")]
		public void Prepare(PickupItemResult denyResult) {
			this.denyResult = (byte) denyResult;
		}

		public override byte Id {
			get { return 0x27; }
		}

		protected override void Write() {
			this.EncodeByte(this.denyResult);
		}
	}

	public sealed class RejectDeleteCharacterOutPacket : GameOutgoingPacket {
		byte reason;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "reason")]
		public void Prepare(DeleteCharacterResult reasonEnum) {
			switch (reasonEnum) {
				case DeleteCharacterResult.Deny_NoMessage:
					this.reason = 254;
					break;
				case DeleteCharacterResult.Allow:
					this.reason = 255;
					break;
				default:
					this.reason = (byte) reasonEnum;
					break;
			}
		}

		public override byte Id {
			get { return 0x85; }
		}

		protected override void Write() {
			this.EncodeByte(this.reason);
		}
	}

	public sealed class ResendCharactersAfterDeleteOutPacket : DynamicLengthOutPacket {
		string[] names = new string[AbstractAccount.maxCharactersPerGameAccount];

		public void Prepare(IList<AbstractCharacter> chars) {
			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				AbstractCharacter ch = chars[i];
				if (ch != null) {
					this.names[i] = ch.Name;
				} else {
					this.names[i] = string.Empty;
				}
			}
		}

		public override byte Id {
			get { return 0x86; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeByte(AbstractAccount.maxCharactersPerGameAccount);

			for (int i = 0; i < AbstractAccount.maxCharactersPerGameAccount; i++) {
				this.EncodeASCIIString(this.names[i], 30);
				this.EncodeZeros(30);
			}
		}
	}

	public sealed class AllNamesOutPacket : DynamicLengthOutPacket {
		int flaggedUid;
		string name;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "name"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid")]
		public void Prepare(int flaggedUid, string name) {
			this.flaggedUid = flaggedUid;
			this.name = name;
		}

		public override byte Id {
			get { return 0x98; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeInt(this.flaggedUid);
			this.EncodeASCIIStringTerminated(this.name);
		}
	}

	public sealed class SendGumpMenuDialogPacket : DynamicLengthOutPacket {
		uint focusFlaggedUid;
		int x, y, gumpUid;
		string layoutText;
		List<string> strings = new List<string>();

		public void Prepare(Gump gump) {
			this.focusFlaggedUid = gump.Focus.FlaggedUid;
			this.gumpUid = gump.Uid;
			this.x = gump.X;
			this.y = gump.Y;
			this.layoutText = gump.layout.ToString();

			this.strings.Clear();
			if (gump.textsList != null) {
				this.strings.AddRange(gump.textsList);
			}
		}

		public override byte Id {
			get { return 0xb0; }
		}

		protected override void WriteDynamicPart() {
			this.EncodeUInt(this.focusFlaggedUid);
			this.EncodeInt(this.gumpUid);
			this.EncodeInt(this.x);
			this.EncodeInt(this.y);

			this.EncodeUShort((ushort) (this.layoutText.Length + 1)); //+1 cos of null terminator
			this.EncodeASCIIStringTerminated(this.layoutText);

			this.EncodeUShort((ushort) this.strings.Count);
			foreach (string s in this.strings) {
				this.EncodeUShort((ushort) s.Length);
				this.EncodeBigEndianUnicodeString(s);
			}
		}
	}

	[CLSCompliant(false)]
	public sealed class CloseGenericGumpOutPacket : GeneralInformationOutPacket {
		int gumpUid;
		int buttonId;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "gumpUid"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "buttonId")]
		public void Prepare(int gumpUid, int buttonId) {
			this.gumpUid = gumpUid;
			this.buttonId = buttonId;
		}

		public override ushort SubCmdId {
			get { return 0x04; }
		}

		protected override void WriteSubCmd() {
			this.EncodeInt(this.gumpUid);
			this.EncodeInt(this.buttonId);
		}
	}

	public sealed class PlaySoundEffectOutPacket : GameOutgoingPacket {
		ushort sound;
		ushort x, y;
		sbyte z;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sound")]
		public void Prepare(IPoint3D source, int sound) {
			source = source.TopPoint;
			this.sound = (ushort) sound;
			this.x = (ushort) source.X;
			this.y = (ushort) source.Y;
			this.z = (sbyte) source.Z;
		}

		public override byte Id {
			get { return 0x54; }
		}

		protected override void Write() {
			this.EncodeByte(0x01); //mode (0x00=quiet, repeating, 0x01=single normally played sound effect) . TODO? enable the 0 mode
			this.EncodeUShort(this.sound);
			this.EncodeZeros(2); //unknown3 (speed/volume modifier? Line of sight stuff?) 
			this.EncodeUShort(this.x);
			this.EncodeUShort(this.y);
			this.EncodeByte(0); //z as short, again?
			this.EncodeSByte(this.z);
		}
	}


	/**
		Shows the character doing a particular animation.
			
		[ I did quite a lot of testing of this packet, since none of the packet guides seemed to have correct information about it.
			All testing on this packet was done with the AOS 2D client.
			Things that Jerrith's has wrong about this packet:
				Anim is a byte, not a ushort, and it starts at pos 6. If anim is greater than the # of valid anims for that character's model (It's out-of-range), then anim 0 is drawn. The byte at pos 5 doesn't seem to do anything (If it were a ushort, the client would display out-of-range-anim# behavior if you set byte 5 to something; It doesn't, it only looks at byte 6 for the anim#.). However, there are also anims which may not exist - for human (and equippables) models, these draw as some other anim instead. There are anims in the 3d client which don't exist in the 2d, too.
				Byte 5 is ignored by the client (But I said that already).
				Jerrith's "direction" variable isn't direction at all; The client knows the direction the character is facing already. It isn't a byte either. Read the next line:
				Bytes 7 and 8, which Jerrith's calls "unknown" and "direction," are actually a ushort which I call numBackwardsFrames. What it does is really weird - it's the number of frames to draw when the anim is drawn backwards, IF 'backwards' is true. If you send 0, it draws a blank frame (i.e. the character vanishes, but reappears after the frame is over). If you send something greater than the number of frames in the anim, then it draws a blank frame for both forwards AND backwards! It's beyond me how that behavior could have been useful for anything, but that's what it does...
				What Jerrith's calls "repeat" isn't the number of times to repeat the anim, it is the number of anims to draw, starting with 'anim'. If you specify 0 for this, though, it will continue drawing anims until the cows come home (and it apparently goes back to 0 after it draws the last one!).
				Jerrith's has "forward/backward" correct, although it doesn't mention what happens if you have both this and 'repeat' (undo) set to true (In that case it runs in reverse, and then runs forward).
				What Jerrith's calls "repeat flag" doesn't make the anim repeat. What it REALLY does is make the anim run once, and then run in reverse immediately after (so I call it "undo" :P). If 'backwards' is true, then it's going to run backwards and then forwards, but it still looks like it's undo'ing the anim it just drew, so :).
				Jerrith's has 'frame delay' correct.
		]
			
			
			
		@param anim The animation the character should perform.
		@param numBackwardsFrames If backwards is true, this is the number of frames to do when going backwards. 0 makes the character blink invisible for the first frame of the backwards anim. Anything above the number of frames in that particular anim makes the character blink invisible for both forwards and backwards, but ONLY if backwards is true. This is rather bizarre, considering how numAnims works, and how various anims have different frame amounts.
		@param numAnims The number of anims to do, starting with 'anim' and counting up from there. 0 means 'keep going forever'. If this exceeds the number of valid anims, it wraps back around to anim 0 and keeps going. So if you specify 0, it really WILL run forever, or at least until another anim is drawn for that character, including if it isn't through an anim packet (like if it moves or turns or something).
		@param backwards If true, perform the animation in reverse.
		@param undo If true, the animation is run, and then the opposite of the animation is run, and this is done for each anim which would be drawn (according to numAnims). If backwards is also true, then you will see the animation performed backwards and then forwards. Conversely, if backwards is false, then with undo true you will see the animation performed forwards and then backwards.
		@param frameDelay The delay time between animations: 0 makes the animation fastest, higher values make it proportionally slower (0xff is like watching glaciers drift).
			I timed some different values with a normal anim 14, which has 7 frames. (Using only my eyes and the windows clock, mind you, so it isn't super-accurate or anything.
				(~ means approximately)
				0: ~2s
				1: ~3s
				5: ~5s (4.5s?)
				10: ~8s
				50: ~36s
					
				What I gathered from those results:
				.25-.3 second delay between frames by default.
				.65-.7 seconds * frameDelay extra delay (for all 7 frames, so if it were .7 then it would be .1*frameDelay per frame)
					
				I did some more math and decided .25 and .7->.1 were probably pretty accurate estimates, and so:
					
				(.25*7)+(.7*frameDelay)=how many seconds UO should spend showing the anim
				(.25*7)+(.7*50)=36.75
				(.25*7)+(.7*1)=2.45
				(.25*7)+(.7*0)=1.75
					
				Or for anims without 7 frames:
				(.25*numFrames)+(.1*numFrames*frameDelay)=how many seconds UO should spend showing the anim
	*/
	public sealed class CharacterAnimationOutPacket : GameOutgoingPacket {
		uint charUid;
		byte dir, frameDelay;
		ushort animId, numAnims;
		bool backwards, undo;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "undo"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "numAnims"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "frameDelay"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "backwards"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "animId")]
		public void Prepare(AbstractCharacter cre, int animId, int numAnims, bool backwards, bool undo, byte frameDelay) {
			this.charUid = cre.FlaggedUid;
			this.dir = cre.DirectionByte;
			this.animId = (ushort) animId;
			this.numAnims = (ushort) numAnims;
			this.backwards = backwards;
			this.undo = undo;
			this.frameDelay = frameDelay;
		}

		public override byte Id {
			get { return 0x6E; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeUShort(this.animId);
			this.EncodeByte(1);
			this.EncodeByte((byte) ((this.dir - 4) & 0x7)); //-4? huh ?
			this.EncodeUShort(this.numAnims);
			this.EncodeBool(this.backwards);
			this.EncodeBool(this.undo);
			this.EncodeByte(this.frameDelay);
		}
	}

	public sealed class GraphicalEffectOutPacket : GameOutgoingPacket {
		uint sourceUid, targetUid;
		int renderMode, hue;
		byte type, speed, duration;
		bool fixedDirection, explodes;
		ushort effect, unk, sourceX, sourceY, targetX, targetY;
		sbyte sourceZ, targetZ;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "unk"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "type"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "speed"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "renderMode"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "hue"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "fixedDirection"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "explodes"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "effect"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "duration")]
		public void Prepare(IPoint4D source, IPoint4D target, byte type, int effect, byte speed, byte duration, int unk, bool fixedDirection, bool explodes, int hue, RenderModes renderMode) {
			source = source.TopPoint;
			target = target.TopPoint;
			Thing sourceAsThing = source as Thing;
			if (sourceAsThing != null) {
				this.sourceUid = sourceAsThing.FlaggedUid;
				MutablePoint4D p = sourceAsThing.point4d;
				this.sourceX = p.x;
				this.sourceY = p.y;
				this.sourceZ = p.z;
			} else {
				this.sourceUid = 0xffffffff;
				this.sourceX = (ushort) source.X;
				this.sourceY = (ushort) source.Y;
				this.sourceZ = (sbyte) source.Z;
			}
			Thing targetAsThing = target as Thing;
			if (targetAsThing != null) {
				this.targetUid = targetAsThing.FlaggedUid;
				MutablePoint4D p = targetAsThing.point4d;
				this.targetX = p.x;
				this.targetY = p.y;
				this.targetZ = p.z;
			} else {
				this.targetUid = 0xffffffff;
				this.targetX = (ushort) target.X;
				this.targetY = (ushort) target.Y;
				this.targetZ = (sbyte) target.Z;
			}
			this.type = type;
			this.effect = (ushort) effect;
			this.speed = speed;
			this.duration = duration;
			this.unk = (ushort) unk;
			this.fixedDirection = fixedDirection;
			this.explodes = explodes;
			this.hue = hue;
			this.renderMode = (int) renderMode;
		}

		public override byte Id {
			get { return 0xC0; }
		}

		protected override void Write() {
			this.EncodeByte(this.type);
			this.EncodeUInt(this.sourceUid);
			this.EncodeUInt(this.targetUid);
			this.EncodeUShort(this.effect);
			this.EncodeUShort(this.sourceX);
			this.EncodeUShort(this.sourceY);
			this.EncodeSByte(this.sourceZ);
			this.EncodeUShort(this.targetX);
			this.EncodeUShort(this.targetY);
			this.EncodeSByte(this.targetZ);
			this.EncodeByte(this.speed);
			this.EncodeByte(this.duration);
			this.EncodeUShort(this.unk);
			this.EncodeBool(this.fixedDirection);
			this.EncodeBool(this.explodes);
			this.EncodeInt(this.hue);
			this.EncodeInt(this.renderMode);
		}
	}

	public sealed class DraggingOfItemOutPacket : GameOutgoingPacket {
		uint sourceUid, targetUid;
		ushort model, amount, sourceX, sourceY, targetX, targetY;
		sbyte sourceZ, targetZ;

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void Prepare(IPoint4D source, IPoint4D target, AbstractItem i) {
			source = source.TopPoint;
			target = target.TopPoint;
			Thing sourceAsThing = source as Thing;
			if (sourceAsThing != null) {
				this.sourceUid = sourceAsThing.FlaggedUid;
				MutablePoint4D p = sourceAsThing.point4d;
				this.sourceX = p.x;
				this.sourceY = p.y;
				this.sourceZ = p.z;
			} else {
				this.sourceUid = 0xffffffff;
				this.sourceX = (ushort) source.X;
				this.sourceY = (ushort) source.Y;
				this.sourceZ = (sbyte) source.Z;
			}
			Thing targetAsThing = target as Thing;
			if (targetAsThing != null) {
				this.targetUid = targetAsThing.FlaggedUid;
				MutablePoint4D p = targetAsThing.point4d;
				this.targetX = p.x;
				this.targetY = p.y;
				this.targetZ = p.z;
			} else {
				this.targetUid = 0xffffffff;
				this.targetX = (ushort) target.X;
				this.targetY = (ushort) target.Y;
				this.targetZ = (sbyte) target.Z;
			}
			this.model = i.ShortModel;
			this.amount = i.ShortAmount;
		}

		public override byte Id {
			get { return 0x23; }
		}

		protected override void Write() {
			this.EncodeUShort(this.model);
			this.EncodeZeros(3);
			this.EncodeUShort(this.amount);
			this.EncodeUInt(this.sourceUid);
			this.EncodeUShort(this.sourceX);
			this.EncodeUShort(this.sourceY);
			this.EncodeSByte(this.sourceZ);
			this.EncodeUInt(this.targetUid);
			this.EncodeUShort(this.targetX);
			this.EncodeUShort(this.targetY);
			this.EncodeSByte(this.targetZ);
		}
	}

	[CLSCompliant(false)]
	public sealed class ExtendedStatsOutPacket : GeneralInformationOutPacket {
		byte statLockByte;
		uint flaggedUid;

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "statLockByte"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "flaggedUid")]
		public void Prepare(uint flaggedUid, byte statLockByte) {
			this.flaggedUid = flaggedUid;
			this.statLockByte = statLockByte;
		}

		public override ushort SubCmdId {
			get { return 0x19; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x02);
			this.EncodeUInt(this.flaggedUid);
			this.EncodeByte(0);
			this.EncodeByte(this.statLockByte);
		}
	}

	public sealed class ResurrectionMenuOutPacket : GameOutgoingPacket {
		byte action;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "action")]
		public void Prepare(byte action) {
			this.action = action;
		}

		public override byte Id {
			get { return 0x2c; }
		}

		protected override void Write() {
			this.EncodeByte(this.action);
		}
	}

	public sealed class DisplayDeathActionOutPacket : GameOutgoingPacket {
		uint charUid, corpseUid;

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "charUid"), CLSCompliant(false)]
		public void Prepare(uint charUid, AbstractItem corpse) {
			this.charUid = charUid;
			if (corpse == null) {
				this.corpseUid = 0xffffffff;
			} else {
				this.corpseUid = corpse.FlaggedUid;
			}
		}

		public override byte Id {
			get { return 0xAF; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeUInt(this.corpseUid);
			this.EncodeZeros(4);
		}
	}

	[CLSCompliant(false)]
	public sealed class AddPartyMembersOutPacket : GeneralInformationOutPacket {
		List<uint> members = new List<uint>();

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "members")]
		public void Prepare(IEnumerable<AbstractCharacter> members) {
			this.members.Clear();
			foreach (AbstractCharacter ch in members) {
				this.members.Add(ch.FlaggedUid);
			}
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x01);
			int n = this.members.Count;
			this.EncodeByte((byte) n);
			for (int i = 0; i < n; i++) {
				this.EncodeUInt(this.members[i]);
			}
		}
	}

	[CLSCompliant(false)]
	public sealed class RemoveAPartyMemberOutPacket : GeneralInformationOutPacket {
		List<uint> members = new List<uint>();

		public void PrepareEmpty(AbstractCharacter self) {
			this.members.Clear();
			this.members.Add(self.FlaggedUid);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "members")]
		public void Prepare(AbstractCharacter removedMember, IEnumerable<AbstractCharacter> members) {
			this.members.Clear();
			this.members.Add(removedMember.FlaggedUid);
			if (members != null) {
				foreach (AbstractCharacter ch in members) {
					this.members.Add(ch.FlaggedUid);
				}
			}
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x02);
			int n = this.members.Count;
			this.EncodeByte((byte) (n - 1));
			for (int i = 0; i < n; i++) {
				this.EncodeUInt(this.members[i]);
			}
		}
	}

	[CLSCompliant(false)]
	public sealed class TellPartyMemberAMessageOutPacket : GeneralInformationOutPacket {
		uint sourceUid;
		string message;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceUid")]
		public void Prepare(uint sourceUid, string message) {
			this.sourceUid = sourceUid;
			this.message = message;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x03);
			this.EncodeUInt(this.sourceUid);
			this.EncodeBigEndianUnicodeString(this.message);
		}
	}

	[CLSCompliant(false)]
	public sealed class TellFullPartyAMessageOutPacket : GeneralInformationOutPacket {
		uint sourceUid;
		string message;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "message"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceUid")]
		public void Prepare(uint sourceUid, string message) {
			this.sourceUid = sourceUid;
			this.message = message;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x04);
			this.EncodeUInt(this.sourceUid);
			this.EncodeBigEndianUnicodeString(this.message);
		}
	}

	[CLSCompliant(false)]
	public sealed class PartyInvitationOutPacket : GeneralInformationOutPacket {
		uint leaderUid;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "leaderUid")]
		public void Prepare(uint leaderUid) {
			this.leaderUid = leaderUid;
		}

		public override ushort SubCmdId {
			get { return 0x6; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(0x07);
			this.EncodeUInt(this.leaderUid);
		}
	}

	[CLSCompliant(false)]
	public sealed class EnableMapDiffFilesOutPacket : GeneralInformationOutPacket {
		byte facetsCount;
		List<int> mapPatches = new List<int>();
		List<int> staticsPatches = new List<int>();

		public void Prepare() {
			this.facetsCount = (byte) Map.FacetCount;
			this.mapPatches.Clear();
			this.staticsPatches.Clear();
			for (int i = 0; i < this.facetsCount; i++) {
				this.mapPatches.Add(Map.GetFacetPatchesMapCount(i));
				this.staticsPatches.Add(Map.GetFacetPatchesStaticsCount(i));
			}
		}

		public override ushort SubCmdId {
			get { return 0x18; }
		}

		protected override void WriteSubCmd() {
			this.EncodeByte(this.facetsCount);
			for (int i = 0; i < this.facetsCount; i++) {
				this.EncodeInt(this.mapPatches[i]);
				this.EncodeInt(this.staticsPatches[i]);
			}
		}
	}

	public sealed class ClientVersionOutPacket : DynamicLengthOutPacket {
		public override byte Id {
			get { return 0xBD; }
		}

		protected override void WriteDynamicPart() {
		}
	}

	public sealed class PersonalLightLevelOutPacket : GameOutgoingPacket {
		uint charUid;
		byte lightLevel;

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "lightLevel"), SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "charUid"), CLSCompliant(false)]
		public void Prepare(uint charUid, int lightLevel) {
			this.charUid = charUid;
			this.lightLevel = (byte) lightLevel;
		}

		public override byte Id {
			get { return 0x4E; }
		}

		protected override void Write() {
			this.EncodeUInt(this.charUid);
			this.EncodeByte(this.lightLevel);
		}
	}

	public sealed class OverallLightLevelOutPacket : GameOutgoingPacket {
		byte lightLevel;

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "lightLevel")]
		public void Prepare(int lightLevel) {
			this.lightLevel = (byte) lightLevel;
		}

		public override byte Id {
			get { return 0x4f; }
		}

		protected override void Write() {
			this.EncodeByte(this.lightLevel);
		}
	}

	public sealed class QuestArrowOutPacket : GameOutgoingPacket {
		bool active;
		ushort xPos, yPos;

		public void Prepare(bool active, int xPos, int yPos) {
			this.active = active;
			this.xPos = (ushort) xPos;
			this.yPos = (ushort) yPos;
		}

		public void Prepare(bool active, IPoint2D position) {
			this.active = active;
			this.xPos = (ushort) position.X;
			this.yPos = (ushort) position.Y;
		}

		public override byte Id {
			get {
				return 0xba;
			}
		}

		protected override void Write() {
			this.EncodeBool(this.active);
			this.EncodeUShort(this.xPos);
			this.EncodeUShort(this.yPos);
		}
	}

	public sealed class OpenDialogBoxPacket : DynamicLengthOutPacket {
		int uid;
		string header;
		List<Entry> entries = new List<Entry>();

		private struct Entry {
			internal ushort color, model;
			internal string text;
		}

		public override byte Id {
			get { return 0x7C; }
		}


		public void Prepare(int uid, IEnumerable<string> allTexts) {
			this.uid = uid;

			this.entries.Clear();
			bool headerDone = false;
			foreach (string str in allTexts) {
				if (headerDone) {
					Sanity.IfTrueThrow(str.Length > byte.MaxValue, "Choice text length 256 exceeded");
					this.entries.Add(new Entry { text = str });
				} else {
					Sanity.IfTrueThrow(str.Length > byte.MaxValue, "Header text length 256 exceeded");
					this.header = str;
					headerDone = true;
				}
			}

			Sanity.IfTrueThrow(this.entries.Count > byte.MaxValue, "Choices count 256 exceeded");
		}

		public void Prepare(int uid, string header, IEnumerable<string> choices) {
			Sanity.IfTrueThrow(header.Length > byte.MaxValue, "Header text length 256 exceeded");
			this.uid = uid;
			this.header = header;

			this.entries.Clear();
			foreach (string str in choices) {
				Sanity.IfTrueThrow(str.Length > byte.MaxValue, "Choice text length 256 exceeded");
				this.entries.Add(new Entry { text = str });
			}

			Sanity.IfTrueThrow(this.entries.Count > byte.MaxValue, "Choices count 256 exceeded");
		}

		public interface IItemMenuEntry {
			int Model { get; }
			int Color { get; }
			string Text { get; }
		}

		//untested
		public void Prepare(int uid, string header, IEnumerable<IItemMenuEntry> choices) {
			Sanity.IfTrueThrow(header.Length > byte.MaxValue, "Header text length 256 exceeded");
			this.uid = uid;
			this.header = header;

			this.entries.Clear();
			foreach (IItemMenuEntry entry in choices) {
				string str = entry.Text;
				Sanity.IfTrueThrow(str.Length > byte.MaxValue, "Choice text length 256 exceeded");
				this.entries.Add(new Entry
				{
					text = str,
					model = (ushort) entry.Model,
					color = (ushort) entry.Color
				});
			}

			Sanity.IfTrueThrow(this.entries.Count > byte.MaxValue, "Choices count 256 exceeded");
		}

		//TODO prepare as itemlist?

		protected override void WriteDynamicPart() {
			this.EncodeInt(this.uid);
			this.EncodeShort((short) Globals.dice.Next(short.MaxValue));

			this.EncodeASCIIStringWithLenByte(this.header);
			int n = this.entries.Count;
			this.EncodeByte((byte) n);

			for (int i = 0; i < n; i++) {
				Entry entry = this.entries[i];
				this.EncodeUShort(entry.model);
				this.EncodeUShort(entry.color);
				this.EncodeASCIIStringWithLenByte(entry.text);
			}
		}
	}

	public sealed class OpenWebBrowserOutPacket : DynamicLengthOutPacket {
		string url;

		public void Prepare(string url) {
			this.url = url;
		}

		public override byte Id {
			get {
				return 0xA5;
			}
		}

		protected override void WriteDynamicPart() {
			this.EncodeASCIIStringTerminated(this.url);
		}
	}
}