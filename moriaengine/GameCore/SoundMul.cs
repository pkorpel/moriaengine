/*	This program is free software; you can redistribute it and/or modify
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
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	static class SoundMul {
		public static void Init() {
			if (Globals.WriteMulDocsFiles) {
				StreamWriter scr = File.CreateText(Globals.GetMulDocPathFor("Sounds.txt"));
				string mulFileP = Path.Combine(Globals.MulPath, "sound.mul");
				string mulFilePI = Path.Combine(Globals.MulPath, "soundidx.mul");

				Console.WriteLine("Loading " + LogStr.File("sound.mul") + " and " + LogStr.File("soundidx.mul") + " - sounds info.");
				int id = 0;
				string longest = "";
				if (File.Exists(mulFileP) && File.Exists(mulFilePI)) {
					FileStream mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read);
					FileStream mulfsi = new FileStream(mulFilePI, FileMode.Open, FileAccess.Read);
					BinaryReader mulbr = new BinaryReader(mulfs);
					BinaryReader mulbri = new BinaryReader(mulfsi);

					int start;
					string filenameS;
					try {
						while (true) {
							start = mulbri.ReadInt32();

							if (start == -1) {
								scr.WriteLine("//Sound ID " + id + " doesn't exist.");
							} else {
								mulbr.BaseStream.Seek(start, SeekOrigin.Begin);
								filenameS = Utility.GetCAsciiString(mulbr, 30);
								if (filenameS.Length > longest.Length) {
									longest = filenameS;
								}
								scr.WriteLine(filenameS.Replace(".wav", "") + " = " + id + ",");
							}
							mulbri.BaseStream.Seek(8, SeekOrigin.Current);
							id++;
						}
					} catch (EndOfStreamException) {
					}
					mulbr.Close();
					mulbri.Close();
					Console.WriteLine("Longest filename in sound.mul: '" + longest + "', " + longest.Length + " characters long.");
				} else {
					Logger.WriteWarning("Unable to locate sound.mul or soundidx.mul.");
				}
				scr.Close();
			} else {
				Logger.WriteWarning("Ignoring sound.mul.");
			}
		}

		/*
		public static void GenerateScriptsFromAnimData() {
			if (!Directory.Exists(Globals.scriptsPath)) {
				Directory.CreateDirectory(Globals.scriptsPath);
				Console.WriteLine("Creating folder "+Globals.scriptsPath);
			}
			string defaultsPath = Path.Combine(Globals.scriptsPath,"defaults");
			if (!Directory.Exists(defaultsPath)) {
				Directory.CreateDirectory(defaultsPath);
				Console.WriteLine("Creating folder "+defaultsPath);
			}
			string chardefsPath = Path.Combine(defaultsPath,"chardefs");
			if (!Directory.Exists(chardefsPath)) {
				Directory.CreateDirectory(chardefsPath);
				Console.WriteLine("Creating folder "+chardefsPath);
			}
			StreamWriter scr = File.CreateText(Path.Combine(chardefsPath,"generated.def"));
			for (int a=0; a<CharacterDispidInfo.Num(); a++) {
				CharacterDispidInfo cdi = CharacterDispidInfo.Get(a);
				if (cdi!=null) {
					scr.WriteLine("[Character c_]");
					scr.WriteLine("defname=0x"+a.ToString("x"));
					scr.WriteLine("body=0x"+a.ToString("x"));
					scr.WriteLine("color=0");
					scr.WriteLine("height="+cdi.height);
					scr.WriteLine("width="+cdi.width);
					scr.WriteLine("unknown="+cdi.unknown);
					scr.WriteLine("CATEGORY=");
					scr.WriteLine("SUBSECTION=");
					scr.WriteLine("DESCRIPTION=");
					scr.WriteLine("");

				}
			}
			scr.Close();
		}*/
	}
}
