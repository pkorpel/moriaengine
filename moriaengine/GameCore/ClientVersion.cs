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
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine {
	public sealed class ClientVersion {
		private static Regex osi2dCliVerRE = new Regex(@"^(?<major>[0-9]+)\.(?<minor>[0-9]+)\.(?<revision>[0-9]+)(?<letter>[a-z])$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static Dictionary<string, ClientVersion> byVersionString = new Dictionary<string, ClientVersion>(StringComparer.OrdinalIgnoreCase);

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
		public static ClientVersion Acquire(string versionString) {
			ClientVersion cliver;
			if (!byVersionString.TryGetValue(versionString, out cliver)) {
				cliver = new ClientVersion(versionString);
				byVersionString[versionString] = cliver;
			}
			return cliver;
		}

		internal static readonly ClientVersion nullValue = new ClientVersion();

		private readonly ClientType type = ClientType.Unknown;
		private readonly string versionString;//what we got from the client


		private OSI2DVersionNumber osi2dVerNum = OSI2DVersionNumber.nullValue;
		//int palanthirVerNum = 0;

		//flags:
		private readonly bool displaySkillCaps;
		private readonly bool aosToolTips;
		private readonly bool oldAosToolTips;
		private readonly bool needsNewSpellbook;

		private ClientVersion() {
		}

		private ClientVersion(string versionString) {
			this.versionString = versionString;
			try {
				Match m = osi2dCliVerRE.Match(versionString);
				if (m.Success) {
					byte major = byte.Parse(m.Groups["major"].Value, CultureInfo.InvariantCulture);
					byte minor = byte.Parse(m.Groups["minor"].Value, CultureInfo.InvariantCulture);
					byte revision = byte.Parse(m.Groups["revision"].Value, CultureInfo.InvariantCulture);
					char letter = m.Groups["letter"].Value[0];
					this.type = ClientType.Osi2D;
					this.osi2dVerNum = new OSI2DVersionNumber(major, minor, revision, letter);

					int number = this.osi2dVerNum.ComparableNumber;
					if (number >= 3000803) {//client 3.0.8d
						this.displaySkillCaps = true;
					}
					if (number > 3000816) {//client 3.0.8o
						this.aosToolTips = true;
						if (number < 4000500) {//client 4.0.5? not sure here.
							this.oldAosToolTips = true;
						}
					}
					this.needsNewSpellbook = number >= 4000000;
				}
			} catch (Exception e) {
				Logger.WriteWarning("While evaluating '" + LogStr.Ident(versionString) + "' as client version string", e);
			}
		}

		public bool DisplaySkillCaps {
			get { 
				return this.displaySkillCaps; 
			}
		}

		public bool AosToolTips {
			get { 
				return this.aosToolTips; 
			}
		}

		public bool OldAosToolTips {
			get { 
				return this.oldAosToolTips; 
			}
		}

		public bool NeedsNewSpellbook {
			get { 
				return this.needsNewSpellbook; 
			}
		}

		public ClientType Type {
			get { 
				return this.type; 
			}
		}

		public string VersionString {
			get { 
				return this.versionString; 
			}
		} 

		public OSI2DVersionNumber OSI2DVerNum {
			get {
				return this.osi2dVerNum;
			}
		}

		//public int PalanthirVerNum { get {
		//	return palanthirVerNum;
		//} }

		public override string ToString() {
			switch (this.type) {

				case ClientType.Osi2D:
					return "OSI2D Client " + this.osi2dVerNum;

				//case ClientType.Iris:
				//case ClientType.OSI3D:
				//case ClientType.OSIGod:
				//case ClientType.PlayUO:
				//case ClientType.Palanthir:
				//case ClientType.Unknown:
			}
			return "not implemented client version: " + this.versionString;
		}
	}

	public sealed class OSI2DVersionNumber {
		private readonly int major;
		private readonly int minor;
		private readonly int revision;
		private readonly char letter;
		private readonly int comparableNumber;

		public int Major {
			get { 
				return this.major; 
			}
		}

		public int Minor {
			get { 
				return this.minor; 
			}
		}
		
		public int Revision {
			get { 
				return this.revision; 
			}
		}

		public char Letter {
			get { 
				return this.letter; 
			}
		}

		public int ComparableNumber {
			get { 
				return this.comparableNumber; 
			}
		} 

		internal static readonly OSI2DVersionNumber nullValue = new OSI2DVersionNumber(0, 0, 0, 'a');

		public OSI2DVersionNumber(int major, int minor, int revision, char letter) {
			this.major = major;
			this.minor = minor;
			this.revision = revision;
			this.letter = char.ToLower(letter, CultureInfo.InvariantCulture);

			checked {
				this.comparableNumber = major * 1000000;
				this.comparableNumber += minor * 10000;
				this.comparableNumber += revision * 100;
				this.comparableNumber += this.letter - valueOfA;
			}
		}

		const int valueOfA = 'a';

		public override string ToString() {
			return string.Concat(this.major, ".", this.minor, ".", this.revision, this.letter);
		}
	}
}