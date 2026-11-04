namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's keep captains (systems/velitele_pevnosti.scp): the player tag captain + keep, the captain
	/// stone of the keep. Who may be appointed, the stone and the captain's commands come with the keep commanders.</summary>
	public static class KeepCaptains {
		private static readonly TagKey keepTK = TagKey.Acquire("_keepCaptainOf_");

		/// <summary>Erebor's tag captain: a captain of some keep.</summary>
		public static bool IsCaptain(Character ch) {
			return ch.GetTag(keepTK) is Thing stone && !stone.IsDeleted;
		}
	}
}
