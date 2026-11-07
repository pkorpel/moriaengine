using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - the venues of scripts/Setup/venues.txt (.venues export), placed where missing. A venue already in the world
	/// keeps what a GM changed on it: setups only add.</summary>
	public sealed class VenueSetup : WorldSetup {
		public override string Name {
			get { return "Mista ve mestech"; }
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			foreach (VenueLine line in VenueFile.Read(VenueFile.Path)) {
				Point4D at = VenueRegistry.MarkerSpot(line.Rects, line.ZMin, line.ZMax, line.M);
				pieces.Item(line.Id, VenueRegistry.MarkerDefname, new[] { at.X, at.Y, at.Z }, at.M, item =>
					VenueRegistry.Fill((CityVenue) item, line.Id, line.Type, line.Title, line.City, line.Opens, line.Closes, line.Rects, line.ZMin, line.ZMax));
			}
		}
	}
}
