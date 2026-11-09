using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - the venues of scripts/Setup/venues.txt (.venues export), placed where missing, then their residents (#1242). A venue already in the world
	/// keeps what a GM changed on it: setups only add.</summary>
	public sealed class VenueSetup : WorldSetup {
		public override string Name {
			get { return "Mista ve mestech"; }
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (VenueLine line in VenueFile.Read(VenueFile.Path)) {
				Point4D at = VenueRegistry.MarkerSpot(line.Rects, line.ZMin, line.ZMax, line.M);
				pieces.Item(line.Id, VenueRegistry.MarkerDefname, new[] { at.X, at.Y, at.Z }, at.M, item => {
					VenueRegistry.Fill((CityVenue) item, line.Id, line.Type, line.Title, line.City, line.Opens, line.Closes, line.Rects, line.ZMin, line.ZMax);
					Residents.ApplyCounts((CityVenue) item, line.Counts); //#1315
				});
			}
			//#1242 - the residents, once their venues are there; one already in the world keeps what it is
			foreach (ResidentLine line in VenueFile.ReadResidents(VenueFile.Path)) {
				CityVenue venue = VenueRegistry.ById(line.VenueId);
				if ((venue != null) && (Residents.ById(line.Id) == null) && Residents.Fits(venue, line.RoleKey, line.Id)) {
					Item gem = Residents.Create(pieces, line.Id, venue, line.RoleKey, line.Female, line.Name, line.HomeId, line.Traits);
					if ((line.Spot != null) && venue.Contains(line.Spot[0], line.Spot[1], line.Spot[2])) { //#1315 - where a GM had moved it
						gem.P(line.Spot[0], line.Spot[1], line.Spot[2], venue.M);
						NPC body = Residents.BodyOf(gem);
						if (body != null) {
							ResidentSchedule.Apply(gem, body, true);
						}
					}
				}
			}
		}
	}
}
