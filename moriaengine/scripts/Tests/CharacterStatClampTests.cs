/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#731 - real-user-reported: an NPC guard self-cast Greater Heal in combat and
	/// died a moment later. Root cause: Character.AddHits/AddMana/AddStamina's clamp was
	/// inverted since the initial commit (Math.Min(0, Math.Max(Max, newCount)) always
	/// evaluates to 0, since Math.Max(Max, newCount) is always positive) - every single call,
	/// heal or damage, zeroed the stat outright. Natural regen and real combat damage both
	/// bypass these methods via direct Hits assignment, which is why this went unnoticed until
	/// a live Heal/Greater Heal cast (the only real caller besides crafting-recipe resource
	/// costs) actually exercised it.</summary>
	public static class CharacterStatClampTests {
		[SteamFunction]
		public static void TestAddHitsClampsCorrectly(Character self) {
			self.MaxHits = 100;
			self.Hits = 60;
			self.AddHits(20); //should heal to 80, not zero
			short afterHeal = self.Hits;

			self.AddHits(1000); //should cap at MaxHits, not overflow past it
			short afterOverheal = self.Hits;

			self.AddHits(-9999); //should floor at 0, not go negative
			short afterLethal = self.Hits;

			Console.WriteLine("ADD_HITS_CLAMP_TEST: afterHeal=" + afterHeal +
				" afterOverheal=" + afterOverheal + " afterLethal=" + afterLethal);
		}

		[SteamFunction]
		public static void TestAddManaAndStaminaClampCorrectly(Character self) {
			self.MaxMana = 50;
			self.Mana = 20;
			self.AddMana(10); //should go to 30, not zero
			short afterManaGain = self.Mana;

			self.MaxStam = 50;
			self.Stam = 20;
			self.AddStamina(10); //should go to 30, not zero
			short afterStamGain = self.Stam;

			Console.WriteLine("ADD_MANA_STAMINA_CLAMP_TEST: afterManaGain=" + afterManaGain +
				" afterStamGain=" + afterStamGain);
		}
	}
}
