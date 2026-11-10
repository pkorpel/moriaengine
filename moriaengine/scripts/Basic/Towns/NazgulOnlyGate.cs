namespace SteamEngine.CompiledScripts {
	/// <summary>#912 - Erebor's i_nazgulsonly_gate, a gate only Nazguls and GMs pass. The user's call (2026-11-10): not an item
	/// of its own; any var gate (i_varGate) does it with the function f_vg_nazgulOnly. This command only says so.</summary>
	public static class NazgulOnlyGate {
		[SteamFunction]
		public static void NazgulGateTo(Character self) {
			self.SysMessage("Branu jen pro vyvolene udelej z i_varGate: v jeho menu zapni funkce, pridej funkci f_vg_nazgulOnly (jen Nazgulove a GM projdou) a nastav, kam vede.");
		}
	}
}
