using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - when someone of a role is at their venue: the game day's blocks (VenueClock).</summary>
	[Flags]
	public enum VenueShift {
		Night = 1, Morning = 2, Day = 4, Evening = 8,
		Work = Morning | Day,
		Always = Night | Morning | Day | Evening
	}

	/// <summary>#1240 - one kind of person a venue wants: the innkeeper, the patrons. Residents (#1237 step 2) fill them.</summary>
	public sealed class VenueRole {
		public readonly string Key, Name;
		public readonly int Count;
		public readonly VenueShift Shift;

		public VenueRole(string key, string name, int count, VenueShift shift) {
			this.Key = key;
			this.Name = name;
			this.Count = count;
			this.Shift = shift;
		}
	}

	/// <summary>#1240 - a kind of venue (the design's section 4.1): its Czech name, opening hours on the game clock and its roles.
	/// Mordor's own kinds come after the common ones.</summary>
	public sealed class VenueType {
		public readonly string Key, Name;
		public readonly int Opens, Closes;
		public readonly bool Mordor;
		public readonly VenueRole[] Roles;

		private VenueType(string key, string name, int opens, int closes, bool mordor, params VenueRole[] roles) {
			this.Key = key;
			this.Name = name;
			this.Opens = opens;
			this.Closes = closes;
			this.Mordor = mordor;
			this.Roles = roles;
		}

		private static VenueRole R(string key, string name, int count, VenueShift shift = VenueShift.Work) {
			return new VenueRole(key, name, count, shift);
		}

		public static readonly VenueType[] All = {
			new VenueType("tavern", "Hospoda", 8, 2, false, R("innkeeper", "Hostinsky", 1, VenueShift.Morning | VenueShift.Day | VenueShift.Evening),
				R("server", "Servirka", 1, VenueShift.Day | VenueShift.Evening), R("cook", "Kuchar", 1), R("bard", "Bard", 1, VenueShift.Evening), R("patron", "Host", 3, VenueShift.Evening)),
			new VenueType("bathhouse", "Lazne", 6, 22, false, R("attendant", "Lazebnik", 1, VenueShift.Morning | VenueShift.Day | VenueShift.Evening), R("stoker", "Topic", 1), R("bather", "Host", 2, VenueShift.Morning | VenueShift.Evening)),
			new VenueType("theatre", "Divadlo", 10, 23, false, R("director", "Reziser", 1, VenueShift.Day | VenueShift.Evening), R("actor", "Herec", 3, VenueShift.Day | VenueShift.Evening),
				R("prompter", "Napoveda", 1, VenueShift.Evening), R("spectator", "Divak", 4, VenueShift.Evening)),
			new VenueType("armoury", "Zbrojnice", 0, 24, false, R("quartermaster", "Zbrojmistr", 1), R("armourer", "Zbrojir", 1)),
			new VenueType("smithy", "Kovarna", 6, 20, false, R("smith", "Kovar", 1), R("apprentice", "Ucen", 1)),
			new VenueType("temple", "Chram", 0, 24, false, R("priest", "Knez", 1, VenueShift.Always), R("acolyte", "Akolyta", 1), R("pilgrim", "Poutnik", 2, VenueShift.Morning | VenueShift.Evening)),
			new VenueType("healing", "Lecebna", 0, 24, false, R("healer", "Lecitel", 1, VenueShift.Always), R("herbalist", "Bylinkar", 1), R("patient", "Pacient", 2, VenueShift.Always)),
			new VenueType("library", "Knihovna", 8, 20, false, R("librarian", "Knihovnik", 1), R("scholar", "Ucenec", 1), R("student", "Student", 2, VenueShift.Day)),
			new VenueType("harbour", "Pristav", 5, 21, false, R("harbourmaster", "Pristavni mistr", 1), R("docker", "Pristavni delnik", 2), R("fisher", "Rybar", 2, VenueShift.Morning), R("sailor", "Namornik", 2)),
			new VenueType("stable", "Staje", 5, 21, false, R("stablemaster", "Konusak", 1), R("groom", "Podkoni", 1)),
			new VenueType("market", "Trh", 7, 19, false, R("merchant", "Obchodnik", 3), R("crier", "Vyvolavac", 1)),
			new VenueType("bakery", "Pekarna", 3, 15, false, R("baker", "Pekar", 1, VenueShift.Night | VenueShift.Morning | VenueShift.Day)),
			new VenueType("brewery", "Pivovar", 6, 20, false, R("brewer", "Sladek", 1), R("labourer", "Delnik", 2)),
			new VenueType("barracks", "Kasarna", 0, 24, false, R("captain", "Kapitan", 1), R("guard", "Strazny", 2, VenueShift.Always), R("recruit", "Rekrut", 2)),
			new VenueType("prison", "Vezeni", 0, 24, false, R("jailer", "Zalarnik", 1, VenueShift.Always), R("prisoner", "Vezen", 2, VenueShift.Always)),
			new VenueType("bank", "Banka", 8, 18, false, R("clerk", "Bankovni urednik", 1)),
			new VenueType("townhall", "Radnice", 8, 18, false, R("scribe", "Pisar", 1), R("messenger", "Posel", 1)),
			new VenueType("tailor", "Krejcovstvi", 8, 18, false, R("tailor", "Krejci", 1)),
			new VenueType("farm", "Statek", 5, 21, false, R("farmer", "Sedlak", 2)),
			new VenueType("mine", "Dul", 0, 24, false, R("foreman", "Predak", 1), R("miner", "Hornik", 3)),
			new VenueType("house", "Obydli", 0, 24, false),
			new VenueType("messhall", "Jidelna", 0, 24, true, R("cook", "Kuchar", 1), R("grunt", "Skret", 4, VenueShift.Evening | VenueShift.Night)),
			new VenueType("pits", "Zapasiste", 12, 2, true, R("pitmaster", "Mistr jam", 1, VenueShift.Day | VenueShift.Evening), R("fighter", "Zapasnik", 2, VenueShift.Evening), R("spectator", "Divak", 4, VenueShift.Evening)),
			new VenueType("forge", "Vyhne", 0, 24, true, R("forgemaster", "Mistr vyhni", 1), R("slave", "Otrok", 3, VenueShift.Always)),
			new VenueType("kennels", "Psince vargu", 0, 24, true, R("kennelmaster", "Chovatel vargu", 1)),
			new VenueType("overseer", "Stanoviste dozorce", 0, 24, true, R("overseer", "Dozorce", 1, VenueShift.Always), R("guard", "Strazny", 2, VenueShift.Always)),
			new VenueType("slavepen", "Otrokarna", 0, 24, true, R("slaver", "Otrokar", 1), R("slave", "Otrok", 4, VenueShift.Always)),
		};

		private static readonly Dictionary<string, VenueType> byKey = new Dictionary<string, VenueType>(StringComparer.OrdinalIgnoreCase);

		static VenueType() {
			foreach (VenueType type in All) {
				byKey[type.Key] = type;
			}
		}

		public static VenueType Get(string key) {
			VenueType type;
			return ((key != null) && byKey.TryGetValue(key, out type)) ? type : null;
		}

		public int IndexOf() {
			return Array.IndexOf(All, this);
		}

		public int RoleCount {
			get {
				int count = 0;
				foreach (VenueRole role in this.Roles) {
					count += role.Count;
				}
				return count;
			}
		}
	}

	/// <summary>#1240 - the game clock in hours: day is the first half of LightAndWeather's cycle, 6:00 to 18:00, night the other
	/// half. A real minute is a game hour at the default 24 minute day. Tests fix the hour with Override.</summary>
	public static class VenueClock {
		public static double? Override;

		public static double Hour {
			get {
				return Override ?? ((6 + (LightAndWeather.DayPhase * 24)) % 24);
			}
		}

		public static VenueShift Shift {
			get {
				return ShiftAt(Hour);
			}
		}

		public static VenueShift ShiftAt(double hour) {
			if ((hour >= 6) && (hour < 10)) {
				return VenueShift.Morning;
			}
			if ((hour >= 10) && (hour < 18)) {
				return VenueShift.Day;
			}
			if ((hour >= 18) && (hour < 22)) {
				return VenueShift.Evening;
			}
			return VenueShift.Night;
		}

		/// <summary>Open between opens and closes, which can run past midnight (8 to 2); 0 to 24 is always.</summary>
		public static bool IsOpen(int opens, int closes, double hour) {
			if ((closes - opens) >= 24 || (opens == closes)) {
				return true;
			}
			return (opens < closes) ? ((hour >= opens) && (hour < closes)) : ((hour >= opens) || (hour < closes));
		}
	}
}
