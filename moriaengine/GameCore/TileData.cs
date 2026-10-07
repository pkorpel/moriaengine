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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	[Flags]
	public enum TileFlag : long {
		None = 0x00000000,
		Background = 0x00000001,	//No idea. None whatsoever. Maybe it's the blackness.
		Weapon = 0x00000002,		//I smack thee with this here ... club?
		Transparent = 0x00000004,	//Yeah. So we can see through it?
		Translucent = 0x00000008,	//Okay...
		Wall = 0x00000010,			//Hey look, we can't walk through it!
		Damaging = 0x00000020,		//Lava, perhaps? Fires, hmm!
		Impassable = 0x00000040,	//Mountains and stuff, I'll wager.
		Wet = 0x00000080,			//Water? Or mud? Or a slick road in a rainstorm? Probably the first.
		Unknown = 0x00000100,		//Uh...
		Surface = 0x00000200,		//Tables or something?
		Bridge = 0x00000400,		//I wonder why they'd have a flag for that.
		Stackable = 0x00000800,
		Generic = 0x00000800,
		Window = 0x00001000,		//So we can see/shoot out?
		Noshoot = 0x00002000,		//? We can't shoot out or something? So, like a glass window maybe?
		PrefixA = 0x00004000,		//_A_ card
		PrefixAn = 0x00008000,		//_An_ apple
		Internal = 0x00010000,		//hair, beards, etc
		Foliage = 0x00020000,		//Probably bushes and tree leaves and stuff.
		PartialHue = 0x00040000,	//semi-glowy? (Or maybe it can accept the 08000 color flag)
		Unknown1 = 0x00080000,		//Well, gee. I should see if it's used on anything...
		Map = 0x00100000,			//Sounds good to me.
		Container = 0x00200000,		//They flag these!?
		Wearable = 0x00400000,		//Omigod!
		LightSource = 0x00800000,	//I'm getting tired of typing repetitive shiznit now.
		Animated = 0x01000000,		//Like fire again. And stuff. Those spinny propeller thingies!
		NoDiagonal = 0x02000000,	//!?!???!!?
		Unknown2 = 0x04000000,		//I really hope some of these unknowns are n/w/s/e facing flags.
		Armor = 0x08000000,			//Armor, okay, so does that count shields? Hmmm?
		Roof = 0x10000000,			//"Don't fall through me!" Or why isn't it just flagged surface or something?
		Door = 0x20000000,			//Okay...
		StairBack = 0x40000000,		//Don't we have stairs that go forward or left too? This could cover both...
		StairRight = 0x80000000,		//Well, whatever, you can climb them, so, hey... Good use for a flag.

		ImpassableSurface = Impassable | Surface
	}

	public static class TileData {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const int numLandTiles = 16384;
		private static TileFlag[] landFlags;

		//public static readonly string[] flagNames = new string[] {"background","weapon","transparent","translucent","wall",
		//    "damaging","impassable","wet","unknown","surface","bridge","stackable","window",
		//    "noshoot","prefixA","prefixAn","internal","foliage","partialHue","unknown_1","map",
		//    "container","wearable", "lightSource", "animated", "noDiagonal", "unknown_2", "armor", "roof",
		//    "door", "stairBack", "stairRight"};

		public static bool HasFlag(TileFlag whatsit, TileFlag flag) {
			return ((whatsit & flag) == flag);
		}

		public static TileFlag GetFlagFromFlagNum(int bitNum) {
			return (TileFlag) (1 << bitNum);
		}

		public static TileFlag GetTileFlags(int id) {
			return landFlags[id];
		}

		public static bool HasFlagNum(TileFlag whatsit, int bitNum) {
			TileFlag flag = (TileFlag) (1 << bitNum);
			return ((whatsit & flag) == flag);
		}

		public static bool IsIgnoredId(int id) {
			return (id == 2 || id == 0x1DB || (id >= 0x1AE && id <= 0x1B5));
		}

		[SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.ItemDispidInfo")]
		public static void Init() {
			string mulFileP = Path.Combine(Globals.MulPath, "tiledata.mul");
			Logger.WriteDebug("Loading " + LogStr.File("tiledata.mul") + " - terrain tile info.");
			if (File.Exists(mulFileP)) {
				landFlags = new TileFlag[numLandTiles];

				using (BinaryReader mulbr = new BinaryReader(new FileStream(mulFileP, FileMode.Open, FileAccess.Read))) {
					StreamWriter mtfi = null;
					string tileNameS;

					try {
						if (Globals.WriteMulDocsFiles) {
							mtfi = File.CreateText(Globals.GetMulDocPathFor("TileData - map tiles.txt"));
						}
						ushort texId;
						int tileId = 0;

						for (int block = 0; block < 512; block++) {
							mulbr.BaseStream.Seek(4, SeekOrigin.Current);	//header
							for (int tileNum = 0; tileNum < 32; tileNum++) {
								landFlags[tileId] = (TileFlag) mulbr.ReadUInt32();
								texId = mulbr.ReadUInt16();
								tileNameS = Utility.GetCAsciiString(mulbr, 20);
								if (Globals.WriteMulDocsFiles) {
									if (tileNameS.Length > 0 || texId != 0) {
										mtfi.WriteLine("TileID: 0x" + tileId.ToString("x", CultureInfo.InvariantCulture) + " (" +
											tileId + ")\tName: " + tileNameS + "\tFlags: " + landFlags[tileId] + "\ttexId: " + texId);
									}
								}
								tileId++;
							}
						}
					} finally {
						if (mtfi != null) {
							mtfi.Close();
						}
					}
					Logger.WriteDebug("Loading " + LogStr.File("tiledata.mul") + " - item dispid info.");
					long bytes = 0;
					//bool multipleModels=false;	// Kemoc - I didnt find reason for this
					//uint lastAmount=0;
					//int dispIDNum=0;
					//bool firstModel=false;
					//ItemDispidInfo cur=null;
					//ItemDispidInfo idi = null;
					TileFlag flags;
					byte weight;
					byte quality;
					ushort unknown;
					byte minItemsToDisplayThisArt;
					byte quantity;
					ushort animID;
					byte unknown2;
					byte hue;
					ushort unknown3;
					byte height;

					try {
						while (true) {
							mulbr.BaseStream.Seek(4, SeekOrigin.Current);	//header
							for (int tileNum = 0; tileNum < 32; tileNum++) {
								flags = (TileFlag) mulbr.ReadUInt32();
								weight = mulbr.ReadByte();
								quality = mulbr.ReadByte();
								unknown = mulbr.ReadUInt16();
								minItemsToDisplayThisArt = mulbr.ReadByte();
								quantity = mulbr.ReadByte();
								animID = mulbr.ReadUInt16();
								unknown2 = mulbr.ReadByte();
								hue = mulbr.ReadByte();
								unknown3 = mulbr.ReadUInt16();
								height = mulbr.ReadByte();
								tileNameS = Utility.GetCAsciiString(mulbr, 20);
								bytes += 20 + tileNameS.Length * 2;
								/*idi = */
								new ItemDispidInfo(flags, weight, quality, unknown, minItemsToDisplayThisArt, quantity, animID, unknown2, hue, unknown3, height, tileNameS);
								/* Kemoc - not used for anything
								if (idi==null || multipleModels==false || idi.unknown<=lastAmount || (!(HasFlag(idi.flags,TileFlag.stackable)))) {
									if (HasFlag(idi.flags,TileFlag.stackable)) {
										multipleModels=true;
										firstModel=true;
										cur=idi;
									} else {
										multipleModels=false;
									}
								}
								if (HasFlag(idi.flags,TileFlag.stackable)) {
									lastAmount=idi.unknown;
									//string aname = "Unnamed";
									//if (idi.name.Length>0) {
									//	aname=idi.name;
									//}
									//int amt=idi.unknown;
									//if (firstModel) {
									//	amt=1;
									//}
									//scr.WriteLine("stackmodel="+amt+":0x"+a.ToString("x")+":"+name);
									//firstModel=false;
									//dunno what does the firstModel and aname variable mean, but they wasn't used... maybe the values should be set back to the info instance somehow...?
								}
								*/
								//dispIDNum++;	// Kemoc - not used
							}
						}
					} catch (EndOfStreamException) {
					}
					long kilobytes = bytes / 1024;
					long megabytes = bytes / (1024 * 1024);
					Console.WriteLine("Finished loading tiledata.mul, item dispid info takes about " + LogStr.Number(bytes) + " bytes or " + LogStr.Number(kilobytes) + " KB or " + LogStr.Number(megabytes) + " MB (of RAM).");
				}
			} else {
				Logger.WriteCritical("Unable to locate tiledata.mul. We're gonna crash soon ;)");
			}
		}

		public static void GenerateMissingDefs() {
			if (Globals.GenerateMissingDefs) {
				string itemdefsPath = Tools.CombineMultiplePaths(Globals.ScriptsPath, "defaults", "itemdefs");
				Tools.EnsureDirectory(itemdefsPath, true);

				StreamWriter scr = File.AppendText(Path.Combine(itemdefsPath, "newItemDefsFromMuls.def"));
				StreamWriter nonexistant = null;
				string filepath = null;
				if (Globals.WriteMulDocsFiles) {
					filepath = Globals.GetMulDocPathFor("Free (unused) itemID numbers.txt");
					nonexistant = File.CreateText(filepath);
				}
				int numWritten = 0;
				int numNonexistant = 0;
				int lastItemNum = 0;
				ItemDispidInfo lastItem = null;
				for (int a = 0; a < ItemDispidInfo.Count; a++) {
					AbstractItemDef def = ThingDef.FindItemDef(a);
					if (def == null) {
						ItemDispidInfo idi = ItemDispidInfo.GetByModel(a);
						if (idi.IsEmpty) {
							numNonexistant++;
							if (Globals.WriteMulDocsFiles) {
								nonexistant.WriteLine("0x" + a.ToString("x", CultureInfo.InvariantCulture));
							}
						} else {
							if (numWritten == 0) {
								scr.WriteLine("\n\n\n//-------------- Written " + DateTime.Now + " --------------//\n");
							}
							numWritten++;
							if (lastItem == null || !lastItem.Equals(idi)) {
								lastItem = idi;
								lastItemNum = a;
								string name = "Unnamed";
								if (idi.SingularName.Length > 0) {
									name = idi.SingularName;
								}
								//defname, category, subsection, description
								scr.WriteLine("");
								string type = "ItemDef";
								if (HasFlag(idi.Flags, TileFlag.Wearable)) {
									type = "EquippableDef";
								}
								if (HasFlag(idi.Flags, TileFlag.Container)) {
									type = "ContainerDef";
								}
								scr.WriteLine("[" + type + " 0x" + a.ToString("x", CultureInfo.InvariantCulture) + "]");
								scr.WriteLine("Model=0x" + a.ToString("x", CultureInfo.InvariantCulture));
								scr.WriteLine("Name=\"" + name + "\"");
								if (HasFlag(idi.Flags, TileFlag.Wearable)) {
									scr.WriteLine("Layer=" + idi.Quality);
								} else {
									scr.WriteLine("//Quality=" + idi.Quality);
								}
								if (HasFlag(idi.Flags, TileFlag.Container)) {
									scr.WriteLine("//MaxContents=" + idi.Height);
								} else {
									scr.WriteLine("//Height=" + idi.Height);
								}
								scr.WriteLine("//Weight=" + idi.Weight);
								scr.WriteLine("//Unknown=" + idi.Unknown1);
								scr.WriteLine("//Min Items to display this art=" + idi.MinItemsToDisplayThisArt);
								scr.WriteLine("//AnimID=0x" + idi.AnimId.ToString("x", CultureInfo.InvariantCulture));
								scr.WriteLine("//Quantity (Wpn/armor type)=" + idi.Quantity);
								scr.WriteLine("//Unknown2=" + idi.Unknown2);
								scr.WriteLine("//Hue=" + idi.Hue);
								scr.WriteLine("//Unknown3=" + idi.Unknown3);
								//if (idi.weight==255) {
								//    scr.WriteLine("Flag=attr_move_never");
								//}
								//if (HasFlag(idi.flags,TileFlag.noshoot)) {
								//    scr.WriteLine("Flag=attr_block");
								//}
								//if (HasFlag(idi.flags,TileFlag.window) || (HasFlag(idi.flags,TileFlag.impassable) && !HasFlag(idi.flags,TileFlag.noshoot))) {
								//    scr.WriteLine("Flag=attr_cover");
								//}
								if (HasFlag(idi.Flags, TileFlag.Stackable)) {
									scr.WriteLine("Stackable=1");
								}
								scr.WriteLine("//Tiledata flags = " + idi.Flags);
							} else {
								string type = "ItemDef";
								if (HasFlag(idi.Flags, TileFlag.Wearable)) {
									type = "EquippableDef";
								}
								if (HasFlag(idi.Flags, TileFlag.Container)) {
									type = "ContainerDef";
								}
								scr.WriteLine("[" + type + " 0x" + a.ToString("x", CultureInfo.InvariantCulture) + "]");
								scr.WriteLine("Model=0x" + a.ToString("x", CultureInfo.InvariantCulture));
								scr.WriteLine("DupeItem=0x" + lastItemNum.ToString("x", CultureInfo.InvariantCulture));
							}

						}
					}
				}
				scr.Close();
				if (Globals.WriteMulDocsFiles) {
					nonexistant.Close();
				}
				if (numWritten > 0) {
					Console.WriteLine("Wrote " + numWritten + " basic itemdefs (for which there were no itemdefs in the scripts), to " + itemdefsPath + "/fromTileData.def.");
				}
				if (Globals.WriteMulDocsFiles && (numNonexistant > 0)) {
					Console.WriteLine("Tiledata.mul appears to contain " + numNonexistant + " free slots. These have been recorded in " + filepath + ".");
				}
				if (Globals.WriteMulDocsFiles) {
					DumpInfoFromTileData();
				}
			}
		}

		public static void DumpInfoFromTileData() {
			string tiledataDocPath = Globals.GetMulDocPathFor("tiledata");
			Tools.EnsureDirectory(tiledataDocPath, true);

			StreamWriter scr = new StreamWriter(Path.Combine(tiledataDocPath, "item dispids.txt"));

			StreamWriter[] sw = new StreamWriter[32];
			for (int flagNum = 0; flagNum < 32; flagNum++) {
				string flagName = Enum.GetName(typeof(TileFlag), flagNum);
				sw[flagNum] = File.CreateText(Path.Combine(tiledataDocPath, flagName + ".txt"));
				sw[flagNum].WriteLine("//This lists all items with the " + flagName + " flag (0x" + GetFlagFromFlagNum(flagNum).ToString("x") + ")");
			}
			scr.WriteLine("//This is not a script.");
			for (int a = 0; a < ItemDispidInfo.Count; a++) {
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(a);
				scr.WriteLine("");
				scr.WriteLine("[Dispid 0x" + a.ToString("x", CultureInfo.InvariantCulture) + "]");
				string name = "Unnamed";
				if (idi.SingularName.Length > 0) {
					name = idi.SingularName;
				}
				scr.WriteLine("Name=" + name);
				if (HasFlag(idi.Flags, TileFlag.Wearable)) {
					scr.WriteLine("Layer=" + idi.Quality);
				} else {
					scr.WriteLine("Quality=" + idi.Quality);
				}
				if (HasFlag(idi.Flags, TileFlag.Container)) {
					scr.WriteLine("Height, or Size (Determines the max # of items that can be held in it?)=" + idi.Height);
				} else {
					scr.WriteLine("Height=" + idi.Height);
				}
				scr.WriteLine("Weight=" + idi.Weight);
				if (idi.Weight == 255) {
					scr.WriteLine("\t(Too heavy to move)");
				}
				scr.WriteLine("Unknown=" + idi.Unknown1);
				scr.WriteLine("Min Items to display this art (Probably used by the client with stackables)=" + idi.MinItemsToDisplayThisArt);
				scr.WriteLine("AnimID=0x" + idi.AnimId.ToString("x", CultureInfo.InvariantCulture));
				scr.WriteLine("Quantity (Wpn/armor type)=" + idi.Quantity);
				scr.WriteLine("Unknown2=" + idi.Unknown2);
				scr.WriteLine("Hue=" + idi.Hue);
				scr.WriteLine("Unknown3=" + idi.Unknown3);
				scr.WriteLine("Tiledata flags = " + idi.Flags);

				for (int flagNum = 0; flagNum < 32; flagNum++) {
					if (HasFlagNum(idi.Flags, flagNum)) {
						sw[flagNum].WriteLine("0x" + a.ToString("x", CultureInfo.InvariantCulture) + ") " + name);

					}
				}


			}
			for (int flagNum = 0; flagNum < 32; flagNum++) {
				sw[flagNum].Close();
			}
			scr.Close();
		}
	}
}
