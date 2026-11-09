using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1333 - walks the roads out of every town before any errand needs them: each city's centre (an errand without a
	/// giver's town) and every area of every city from its own centre (a giver working there), one walk a step so no step holds
	/// the server long. Roads already known (saved with the world) aren't walked again. Places no pieces.</summary>
	public sealed class RoadsOutSetup : WorldSetup {
		public override string Name {
			get { return "Roads out of towns"; }
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (string step in this.PlaceInSteps(pieces, gm)) {
			}
		}

		protected internal override IEnumerable<string> PlaceInSteps(WorldPieces pieces, Character gm) {
			int walked = WorldEvents.roadsWalked;
			foreach (string city in WorldEvents.Cities) {
				Point4D centre = WorldEvents.CentreOf(city);
				FlaggedRegion town = (centre == null) ? null : Errands.TownAt(city, centre.X, centre.Y, centre.M);
				int before = WorldEvents.roadsWalked;
				if (town != null) {
					WorldEvents.RoutesOutOf(centre, town, Errands.OutsideBeyond, Errands.OutsideMaxSteps);
				}
				if (WorldEvents.roadsWalked > before) {
					yield return city; //a pause only after a walk: a run after the first costs nothing
				}
				foreach (FlaggedRegion area in VenueRegistry.AreasOf(city)) {
					Point4D areaCentre = WorldEvents.CentreOfArea(area);
					before = WorldEvents.roadsWalked;
					if (areaCentre.M == area.Mapplane) {
						WorldEvents.RoutesOutOf(areaCentre, area, Errands.OutsideBeyond, Errands.OutsideMaxSteps);
					}
					if (WorldEvents.roadsWalked > before) {
						yield return area.Defname;
					}
				}
			}
			yield return "walked " + (WorldEvents.roadsWalked - walked) + ", known " + WorldEvents.routesOut.Count;
		}
	}
}
