using System.Globalization;
using System.Resources;

namespace SteamEngine.Common {

	/// <summary>
	/// Backing store for all localized text (#104, Part B): two plain embedded resx
	/// resources, one per known language, looked up directly by Language rather than via
	/// ResourceManager's CultureInfo-based satellite-assembly fallback - this sandbox can't
	/// reliably exercise satellite-culture resx splitting (no ICU, needs
	/// DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 just to run dotnet at all), and a fixed
	/// 2-language server doesn't need that generic machinery anyway. Always queried with
	/// InvariantCulture so the lookup reads straight from each resource's own embedded data,
	/// with no culture-fallback probing involved.
	/// </summary>
	public static class GameTextResources {
		private const string BaseName = "SteamEngine.Common.Localisation.Resources.GameText";

		private static readonly ResourceManager English = new ResourceManager(BaseName, typeof(GameTextResources).Assembly);
		private static readonly ResourceManager Czech = new ResourceManager(BaseName + ".Czech", typeof(GameTextResources).Assembly);

		public static string GetEntry(string flatKey, Language language) {
			ResourceManager rm = (language == Language.Czech) ? Czech : English;
			return rm.GetString(flatKey, CultureInfo.InvariantCulture) ?? LocStringCollection.defaultText;
		}
	}
}
