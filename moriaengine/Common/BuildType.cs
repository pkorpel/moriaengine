namespace SteamEngine.Common {
	public enum BuildType {
		Debug,
		Release
	}

	public class Build {
		public static BuildType Type {
			get {
#if DEBUG
				return BuildType.Debug;
#else
				return BuildType.Release;
#endif
			}
		}
	}
}