using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - the GM's venue tools: .venue marks or edits the venue where you stand, .venues lists them, finds rooms
	/// not yet venues (navrh) and writes them all to the setup file (export).</summary>
	public static class VenueCommands {
		[SteamFunction]
		public static void Venue(Character self) {
			CityVenue here = VenueRegistry.At(self);
			if (here != null) {
				D_Venue.Show(self, VenueDraft.For(here));
				return;
			}
			FoundRoom room;
			VenueDraft draft = VenueDraft.At(self, out room);
			if (room == null) {
				self.SysMessage("Tady nad tebou neni strecha: nastav plochu rohy kurzorem.");
			} else {
				draft.ShowOutline(self);
				if (room.TooBig) {
					self.SysMessage("Mistnost je prilis velka, vzata jen cast. Zkontroluj obrys.");
				}
			}
			D_Venue.Show(self, draft);
		}

		[SteamFunction]
		public static void Venues(Character self, ScriptArgs sa) {
			string what = (sa == null) ? "" : (sa.Args ?? "").Trim('"', ' ').ToLowerInvariant();
			string city = VenueRegistry.CityAt(self);
			switch (what) {
				case "":
					D_Venues.Show(self, city, 0);
					break;
				case "vse":
					D_Venues.Show(self, null, 0);
					break;
				case "navrh":
					if (city == null) {
						self.SysMessage("Nestojis v zadnem meste.");
						return;
					}
					List<FoundRoom> rooms = RoomScan.Candidates(city, self.MovementSettings);
					self.SysMessage("Budov bez mista: " + rooms.Count + ".");
					D_VenueProposals.Show(self, city, rooms, 0);
					break;
				case "export":
					int written = VenueFile.Export(VenueFile.Path);
					self.SysMessage("Zapsano " + written + " mist do " + VenueFile.Path + ". .SetupWorld je z nej obnovi.");
					break;
				default:
					self.SysMessage(".venues [vse | navrh | export]");
					break;
			}
		}
	}

	/// <summary>#1240 - venues kept in a text file in scripts/Setup, one per line, so a world rebuilt from an empty save gets
	/// them back through .SetupWorld (VenueSetup). id|city|type|opens|closes|m|zmin|zmax|rects|title, rects as CityVenue keeps them.</summary>
	public static class VenueFile {
		public static string PathOverride;

		public static string Path {
			get { return PathOverride ?? System.IO.Path.Combine(Globals.ScriptsPath, "Setup", "venues.txt"); }
		}

		public static int Export(string path) {
			StringBuilder text = new StringBuilder();
			text.Append("# #1240 venues, written by .venues export; .SetupWorld places those missing\n");
			text.Append("# id|city|type|opens|closes|m|zmin|zmax|rects|title\n");
			List<CityVenue> venues = new List<CityVenue>(VenueRegistry.All);
			venues.Sort((a, b) => string.Compare(a.VenueId, b.VenueId, StringComparison.OrdinalIgnoreCase));
			foreach (CityVenue venue in venues) {
				VenueType type = venue.VenueType;
				text.Append(venue.VenueId).Append('|').Append(venue.City ?? "").Append('|').Append((type == null) ? "" : type.Key).Append('|')
					.Append(venue.Opens.ToString(CultureInfo.InvariantCulture)).Append('|').Append(venue.Closes.ToString(CultureInfo.InvariantCulture)).Append('|')
					.Append(venue.M.ToString(CultureInfo.InvariantCulture)).Append('|').Append(venue.ZMin.ToString(CultureInfo.InvariantCulture)).Append('|')
					.Append(venue.ZMax.ToString(CultureInfo.InvariantCulture)).Append('|').Append(CityVenue.FormatRects(venue.Rectangles)).Append('|')
					.Append(venue.Title.Replace("|", "/").Replace("\r", " ").Replace("\n", " ")).Append('\n');
			}
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			File.WriteAllText(path, text.ToString());
			return venues.Count;
		}

		/// <summary>The file's venues, skipping comments and broken lines.</summary>
		public static List<VenueLine> Read(string path) {
			List<VenueLine> lines = new List<VenueLine>();
			if (!File.Exists(path)) {
				return lines;
			}
			foreach (string raw in File.ReadAllLines(path)) {
				string line = raw.Trim();
				if ((line.Length == 0) || line.StartsWith("#", StringComparison.Ordinal)) {
					continue;
				}
				string[] parts = line.Split(new[] { '|' }, 10);
				int opens, closes, m, zMin, zMax;
				if ((parts.Length == 10) && (VenueType.Get(parts[2]) != null) &&
						int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out opens) && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out closes) &&
						int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out m) && int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out zMin) &&
						int.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out zMax)) {
					List<ImmutableRectangle> rects = CityVenue.ParseRects(parts[8]);
					if (rects.Count > 0) {
						lines.Add(new VenueLine(parts[0], (parts[1].Length == 0) ? null : parts[1], VenueType.Get(parts[2]), opens, closes, (byte) m, zMin, zMax, rects, parts[9]));
					}
				}
			}
			return lines;
		}
	}

	public sealed class VenueLine {
		public readonly string Id, City, Title;
		public readonly VenueType Type;
		public readonly int Opens, Closes, ZMin, ZMax;
		public readonly byte M;
		public readonly List<ImmutableRectangle> Rects;

		public VenueLine(string id, string city, VenueType type, int opens, int closes, byte m, int zMin, int zMax, List<ImmutableRectangle> rects, string title) {
			this.Id = id;
			this.City = city;
			this.Type = type;
			this.Opens = opens;
			this.Closes = closes;
			this.M = m;
			this.ZMin = zMin;
			this.ZMax = zMax;
			this.Rects = rects;
			this.Title = title;
		}
	}
}
