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

		private void LoadSpecificFlag(string filename, int line, int mask, string args) {
			if (ConvertTools.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= (RegionFlags) mask;
			}
		}

		private void LoadSpecificFlag(RegionFlags mask, string args) {
			if (ConvertTools.ParseBoolean(args)) {//args is 1 or true or something like that
				this.flags |= mask;
			}
		}
		#endregion Persistence
	}
}
