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
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.Regions {

	[Flags]
	public enum RegionFlags {
		Zero = 0, None = 0,
		NoMagicIn = 0x0001,
		NoMagicOut = 0x0002,
		NoHarmfulMagicIn = 0x0004,
		NoHarmfulMagicOut = 0x0008,
		NoBeneficialMagicIn = 0x0010,
		NoBeneficialMagicOut = 0x0020,
		NoTeleportingIn = 0x0040,
		NoTeleportingOut = 0x0080,
		NoEnemyTeleportingIn = 0x0100,
		NoEnemyTeleportingOut = 0x0200,

		Underground = 0x0800, Dungeon = Underground,

		NoDecay = 0x1000,

		Safe = 0x02000,
		Guarded = 0x04000,
		NoPvp = 0x08000,
		Arena = 0x10000
	}

	//todo: make some members virtual?
	[SaveableClass]
	[ViewableClass]
	public class FlaggedRegion : StaticRegion {
		private RegionFlags flags;

		//#813 - from the archive map (spheremap.scp): Sphere region events and RESOURCES region
		//types by name, weather chances and the area group. Kept as data here; behavior for them
		//lives elsewhere (region resources, ported region events).
		private List<string> regionEvents;
		private List<string> regionResources;
		private int rainChance, coldChance;
		private string group;
		private int realmTag;
		private string city;
		private int houseNumber;
		private int houseRealmTag;
		private bool noMask;
		private int customFlags;
		private int elvenGroveX = -1, elvenGroveY = -1;
		private int battlefield;
		private Point4D portMeTo;

		[LoadSection]
		public FlaggedRegion(PropsSection input)
			: base(input) {
		}

		/// <summary>#813 - [FlaggedRegion x] sections in scripts: the converted archive map.</summary>
		public static void Bootstrap() {
			ScriptLoader.RegisterScriptType("FlaggedRegion", LoadFromScripts, false);
		}

		private static IUnloadable LoadFromScripts(PropsSection input) {
			return StaticRegion.LoadFromScripts(input, section => new FlaggedRegion(section));
		}

		/// <summary>Sphere region events/region types (EVENTS=), e.g. r_nosumon, r_playerhouse.</summary>
		public IList<string> RegionEvents {
			get { return (IList<string>) this.regionEvents ?? Array.Empty<string>(); }
		}

		/// <summary>Region types (RESOURCES=) deciding what rock/trees/water yield here, e.g. r_default_rock, r_doly.</summary>
		public IList<string> RegionResources {
			get { return (IList<string>) this.regionResources ?? Array.Empty<string>(); }
		}

		public int RainChance {
			get { return this.rainChance; }
		}

		public int ColdChance {
			get { return this.coldChance; }
		}

		/// <summary>Erebor's tag.realm of the area: 1 Gondor, 2 Mordor, 3 wilderness, 4 where a thief can't
		/// change masks. Not inherited: every area that needs it has its own.</summary>
		public int RealmTag {
			get { return this.realmTag; }
			set {
				this.realmTag = value;
				this.MarkGmModified();
			}
		}

		public Realm Realm {
			get {
				switch (this.realmTag) {
					case 1:
						return Realm.Gondor;
					case 2:
						return Realm.Mordor;
					default:
						return Realm.None;
				}
			}
		}

		/// <summary>#941 - Erebor's tag.housenumber: a house, stall or field, each kind numbered on its own. 0 if none.</summary>
		public int HouseNumber {
			get { return this.houseNumber; }
		}

		/// <summary>#941 - a dwelling, the only kind that gets a sign and keeps people out. Stalls, fields and three temples
		/// have house numbers too.</summary>
		public bool IsHouse {
			get { return (this.houseNumber != 0) && this.Defname.StartsWith("a_dum_", System.StringComparison.OrdinalIgnoreCase); }
		}

		/// <summary>#941 - Erebor's tag.houserealm: whose this house is. Realm.None for the few that have no such tag.</summary>
		public Realm HouseRealm {
			get {
				switch (this.houseRealmTag) {
					case 1:
						return Realm.Gondor;
					case 2:
						return Realm.Mordor;
					default:
						return Realm.None;
				}
			}
		}

		/// <summary>#1070 - Erebor's tag.nomask: a thief can't change masks here.</summary>
		public bool NoMask {
			get { return this.noMask; }
		}

		/// <summary>#1070 - Erebor's tag.cFlags, region_cf_*: recall and gate denied in (0x10) or out (0x20), resurrection
		/// at once (0x80).</summary>
		public int CustomFlags {
			get { return this.customFlags; }
		}

		/// <summary>#427 - Erebor's tag.elven: the point this area's elven probe wanders round (ElvenGrove).</summary>
		public bool HasElvenGrove {
			get { return this.elvenGroveX >= 0; }
		}

		public int ElvenGroveX {
			get { return this.elvenGroveX; }
		}

		public int ElvenGroveY {
			get { return this.elvenGroveY; }
		}

		/// <summary>#1116 - Erebor's tag.bf: this area's place on the front of the keep conquest. 1 and 2 are Gondor's keeps
		/// (Gondor_1, Gondor_2), -1 and -2 Mordor's; 0 is no battlefield.</summary>
		public int Battlefield {
			get { return this.battlefield; }
		}

		public const int DenyRecallIn = 0x10, DenyRecallOut = 0x20, InstantRes = 0x80;

		public bool HasCustomFlag(int flag) {
			return (this.customFlags & flag) != 0;
		}

		/// <summary>#892 - Erebor's tag.jmeno_mesta: the city this area answers to, and several areas can
		/// share one (Pelargir and Linhir are Dol_Amroth's). Multi-word names have underscores.</summary>
		public string City {
			get { return this.city; }
			set {
				this.city = value;
				this.MarkGmModified();
			}
		}

		/// <summary>Sphere's area group (GROUP=), e.g. "Doly", "Mesta Gondor", "Arena".</summary>
		public string Group {
			get { return this.group; }
		}

		protected override void ResetForSavedOverride() {
			this.flags = RegionFlags.Zero;
			this.regionEvents = null;
			this.regionResources = null;
			this.rainChance = 0;
			this.coldChance = 0;
			this.group = null;
			this.realmTag = 0;
			this.city = null;
			this.houseNumber = 0;
			this.houseRealmTag = 0;
			this.noMask = false;
			this.customFlags = 0;
			this.portMeTo = null;
		}

		/// <summary>#1116 - Erebor's region tag PortMeTo, set by a GM and never in the archive: where a PortZone
		/// arena sends its dead (PortZone). Null is the realm's default point.</summary>
		public Point4D PortMeTo {
			get { return this.portMeTo; }
			set {
				this.portMeTo = value;
				this.MarkGmModified();
			}
		}

		private static List<string> AddNames(List<string> list, string valueString) {
			foreach (string part in valueString.Split(',')) {
				string name = part.Trim();
				if (name.Length > 0) {
					if (list == null) {
						list = new List<string>();
					}
					if (!list.Contains(name)) {
						list.Add(name);
					}
				}
			}
			return list;
		}

		public FlaggedRegion()
		{
		}

		public FlaggedRegion(string defname, Region parent)
		{
			this.Defname = defname;
			this.Parent = parent;
		}

		public RegionFlags Flags {
			get {
				return this.flags;
			}
			set {
				this.flags = value;
				this.MarkGmModified(); //#813 - loading sets the field directly, so this is an in-game edit
			}
		}

		public bool Flag_Underground {
			get {//positive value isinherited from parent - underground stays underground
				if ((this.flags & RegionFlags.Underground) == RegionFlags.Underground) {
					return true;
				}
				FlaggedRegion parentAsFlagged = this.Parent as FlaggedRegion;
				if (parentAsFlagged != null) {
					return parentAsFlagged.Flag_Underground;
				}
				return false;
			}
			set {
				if (value) {
					this.flags |= RegionFlags.Underground;
				} else {
					this.flags &= ~RegionFlags.Underground;
				}
				//todo? refresh light and stuff of present players?
			}
		}

		private bool HasInheritedFlag(RegionFlags flag) {
			if ((this.flags & flag) == flag) {
				return true;
			}
			FlaggedRegion parentAsFlagged = this.Parent as FlaggedRegion;
			if (parentAsFlagged != null) {
				return parentAsFlagged.HasInheritedFlag(flag);
			}
			return false;
		}

		/// <summary>No PvP damage allowed at all, regardless of notoriety (e.g. a newbie starting area).</summary>
		public bool Flag_Safe {
			get { return this.HasInheritedFlag(RegionFlags.Safe); }
		}

		/// <summary>Town/city guards patrol here - attacking another player (unless already criminal/murderer) is forbidden.</summary>
		public bool Flag_Guarded {
			get { return this.HasInheritedFlag(RegionFlags.Guarded); }
		}

		/// <summary>Player-vs-player combat disallowed, independent of guards (e.g. a roleplay-safe zone).</summary>
		public bool Flag_NoPvp {
			get { return this.HasInheritedFlag(RegionFlags.NoPvp); }
		}

		/// <summary>Explicitly overrides Safe/Guarded/NoPvp to allow consensual PvP (e.g. a dueling pit).</summary>
		public bool Flag_Arena {
			get { return this.HasInheritedFlag(RegionFlags.Arena); }
		}

		/// <summary>Items inside this region never decay, regardless of their own attr_decay flag (#219).</summary>
		public bool Flag_NoDecay {
			get { return this.HasInheritedFlag(RegionFlags.NoDecay); }
		}

		public override TriggerResult On_Enter(AbstractCharacter ch, bool forced) {
			Player asPlayer = ch as Player;
			if (asPlayer != null) {
				asPlayer.SendGlobalLightLevel(LightAndWeather.GetLightIn(this));
			}

			return TriggerResult.Continue;//if forced is true, the return value is irrelevant
		}

		public override TriggerResult On_Exit(AbstractCharacter ch, bool forced) {
			Player asPlayer = ch as Player;
			if (asPlayer != null) {
				//we're exiting this one, hence "re-entering" the parent
				FlaggedRegion parentAsFlagged = this.Parent as FlaggedRegion;
				if (parentAsFlagged != null) {
					asPlayer.SendGlobalLightLevel(LightAndWeather.GetLightIn(parentAsFlagged));
				}
			}

			return TriggerResult.Continue;//if forced is true, the return value is irrelevant
		}

		#region Persistence
		public override void Save(SaveStream output) {
			this.ThrowIfDeleted();
			base.Save(output);//Region save

			if (this.flags != RegionFlags.Zero) {
				output.WriteValue("flags", this.flags);
			}
			if (this.regionEvents != null) {
				output.WriteValue("events", string.Join(", ", this.regionEvents));
			}
			if (this.regionResources != null) {
				output.WriteValue("resources", string.Join(", ", this.regionResources));
			}
			if (this.rainChance != 0) {
				output.WriteValue("rainchance", this.rainChance);
			}
			if (this.coldChance != 0) {
				output.WriteValue("coldchance", this.coldChance);
			}
			if (this.group != null) {
				output.WriteValue("group", this.group);
			}
			if (this.realmTag != 0) {
				output.WriteValue("realm", this.realmTag);
			}
			if (this.city != null) {
				output.WriteValue("city", this.city);
			}
			if (this.houseNumber != 0) {
				output.WriteValue("housenumber", this.houseNumber);
			}
			if (this.houseRealmTag != 0) {
				output.WriteValue("houserealm", this.houseRealmTag);
			}
			if (this.noMask) {
				output.WriteValue("nomask", 1);
			}
			if (this.customFlags != 0) {
				output.WriteValue("customflags", this.customFlags);
			}
			if (this.HasElvenGrove) {
				output.WriteLine("elvengrove=" + this.elvenGroveX + "," + this.elvenGroveY);
			}
			if (this.battlefield != 0) {
				output.WriteValue("battlefield", this.battlefield);
			}
			if (this.portMeTo != null) {
				output.WriteLine("portmeto=" + this.portMeTo.ToNormalString());
			}
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			this.ThrowIfDeleted();
			switch (valueName) {
				case "flag_announce":
					this.LoadSpecificFlag(filename, line, 0x00200, valueString);
					break;
				case "flag_antimagic_all":
					this.LoadSpecificFlag(filename, line, 0x00001, valueString);
					break;
				case "flag_antimagic_damage":
					this.LoadSpecificFlag(filename, line, 0x00020, valueString);
					break;
				case "flag_antimagic_gate":
					this.LoadSpecificFlag(filename, line, 0x00008, valueString);
					break;
				case "flag_antimagic_recallin":
					this.LoadSpecificFlag(filename, line, 0x00002, valueString);
					break;
				case "flag_antimagic_recallout":
					this.LoadSpecificFlag(filename, line, 0x00004, valueString);
					break;
				case "flag_antimagic_teleport":
					this.LoadSpecificFlag(filename, line, 0x00010, valueString);
					break;
				case "flag_arena":
					this.LoadSpecificFlag(filename, line, 0x10000, valueString);
					break;
				case "flag_guarded":
					this.LoadSpecificFlag(filename, line, 0x04000, valueString);
					break;
				case "flag_instalogout":
					this.LoadSpecificFlag(filename, line, 0x00400, valueString);
					break;
				case "flag_nobuilding":
					this.LoadSpecificFlag(filename, line, 0x00080, valueString);
					break;
				case "flag_nodecay":
					this.LoadSpecificFlag(filename, line, 0x01000, valueString);
					break;
				case "flag_nopvp":
					this.LoadSpecificFlag(filename, line, 0x08000, valueString);
					break;
				//case "flag_roof"://"this region has a roof" - wtf does that mean? -tar
				//	LoadSpecificFlag(filename, line, 0x20000, args);
				//	break;
				case "flag_safe":
					this.LoadSpecificFlag(filename, line, 0x02000, valueString);
					break;
				case "flag_ship":
					this.LoadSpecificFlag(filename, line, 0x00040, valueString);
					break;
				case "flag_underground":
				case "flag_dungeon":
					this.LoadSpecificFlag(RegionFlags.Underground, valueString);
					break;
				//case "flag_unused":
				//	LoadSpecificFlag(0x00100, args); 
				//	break;

				case "flag":
				case "flags":
					//#438 - same regression as AbstractCharacter's "directionandflags" and
					//AbstractItem's "flags": #407 made Save() embed this as "(Enum:RegionFlags)N",
					//but this naive ConvertTools.ParseInt32(valueString) still expected a bare
					//number - confirmed as a real, already-occurring FormatException in logs
					//("The input string '(Enum:RegionFlags)4498' was not in a correct format").
					//#813 - the converted archive map has Sphere's own hex form (FLAGS = 00812)
					if (valueString.TrimStart().StartsWith("(", StringComparison.Ordinal)) {
						this.flags = (RegionFlags) ObjectSaver.Load(valueString);
					} else {
						this.flags = (RegionFlags) ConvertTools.ParseInt32(valueString.Trim());
					}
					break;
				case "event":
				case "events":
					this.regionEvents = AddNames(this.regionEvents, ConvertTools.LoadSimpleQuotedString(valueString));
					break;
				case "resources":
					this.regionResources = AddNames(this.regionResources, ConvertTools.LoadSimpleQuotedString(valueString));
					break;
				case "realm":
					this.realmTag = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "city":
					this.city = ConvertTools.LoadSimpleQuotedString(valueString.Trim());
					break;
				case "housenumber":
					this.houseNumber = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "houserealm":
					this.houseRealmTag = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "nomask":
					this.noMask = ConvertTools.ParseInt32(valueString.Trim()) != 0;
					break;
				case "customflags":
					this.customFlags = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "battlefield":
					this.battlefield = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "portmeto":
					this.portMeTo = Point4D.Parse(valueString);
					break;
				case "elvengrove": {
						string[] xy = valueString.Split(',');
						this.elvenGroveX = ConvertTools.ParseInt32(xy[0].Trim());
						this.elvenGroveY = ConvertTools.ParseInt32(xy[1].Trim());
						break;
					}
				case "rainchance":
					this.rainChance = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "coldchance":
					this.coldChance = ConvertTools.ParseInt32(valueString.Trim());
					break;
				case "group":
					this.group = ConvertTools.LoadSimpleQuotedString(valueString.Trim());
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);//the Region Loadline
					break;
			}
		}

		/// <summary>#818 - a Sphere FLAGS bit (the flag_* names are Sphere's), in the engine's terms.</summary>
		private void LoadSpecificFlag(string filename, int line, int sphereMask, string args) {
			if (ConvertTools.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= FromSphereFlags(sphereMask);
			}
		}

		/// <summary>#818 - Sphere 0.99z8's region FLAGS bits in the engine's RegionFlags. They agree
		/// only from 0x800 up (underground, nodecay, safe, guarded, nopvp, arena); below that Sphere's
		/// bits are anti-magic rules that mean other things here (Sphere's 0x02 "no recall in" is our
		/// NoMagicOut). Recall, gate and the Teleport spell have no separate rule in the engine, so
		/// they become NoTeleportingIn/Out; ship, nobuilding, announce and instalogout have no
		/// equivalent and are dropped. The converter uses this for the archive map's FLAGS.</summary>
		public static RegionFlags FromSphereFlags(int sphere) {
			RegionFlags result = (RegionFlags) (sphere & 0x1f800); //underground, nodecay, safe, guarded, nopvp, arena: same bits
			if ((sphere & 0x01) != 0) { //ANTIMAGIC_ALL
				result |= RegionFlags.NoMagicIn | RegionFlags.NoMagicOut;
			}
			if ((sphere & 0x02) != 0) { //ANTIMAGIC_RECALL_IN
				result |= RegionFlags.NoTeleportingIn;
			}
			if ((sphere & 0x04) != 0) { //ANTIMAGIC_RECALL_OUT
				result |= RegionFlags.NoTeleportingOut;
			}
			if ((sphere & 0x18) != 0) { //ANTIMAGIC_GATE, ANTIMAGIC_TELEPORT
				result |= RegionFlags.NoTeleportingIn | RegionFlags.NoTeleportingOut;
			}
			if ((sphere & 0x20) != 0) { //ANTIMAGIC_DAMAGE
				result |= RegionFlags.NoHarmfulMagicIn | RegionFlags.NoHarmfulMagicOut;
			}
			return result;
		}

		private void LoadSpecificFlag(RegionFlags mask, string args) {
			if (ConvertTools.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= mask;
			}
		}
		#endregion Persistence
	}
}
