using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#931 - Erebor's chflag_*: what tag.chaton held.</summary>
	[Flags]
	public enum ChatChannel {
		None = 0,
		Gondor = 0x01,
		Mordor = 0x02,
		All = 0x04,
		Gm = 0x08,
		Guild = 0x10
	}
}
