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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>Verification-harness diagnostics for the GM ".kill" and ".flip" commands
	/// (#192, #193) - see Tests/Protocol/GmCommandsTests.cs.</summary>
	public static class GmCommandsTests {
		[SteamFunction]
		public static void TestSetupKillTarget(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef rabbitDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC target = (NPC) rabbitDef.Create(self.X + 1, self.Y, self.Z, self.M);
			target.Name = "TestKillTarget";
			Console.WriteLine("KILL_SETUP: uid=" + target.Uid);
		}

		/// <summary>"Dead" covers both outcomes of Character.CauseDeath: a player becomes a
		/// Flag_Dead ghost in place, but an NPC is deleted outright (scripts/Basic/CharClasses/
		/// Character.cs) - so for an NPC, the uid simply no longer resolving to anything IS the
		/// expected "dead" result, not an error.</summary>
		[SteamFunction]
		public static void TestCheckDead(Character self, ScriptArgs sa) {
			int uid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character ch = Thing.UidGetCharacter(uid) as Character;
			bool dead = (ch == null) || ch.Flag_Dead;
			Console.WriteLine("KILL_TEST: dead=" + dead);
		}

		/// <summary>i_wall_stone_4 (scripts/converted/itemb1.def) is one of the few converted
		/// walls that still has its original Sphere DUPEITEM/flip group intact - most converted
		/// items lost theirs (the auto-converter only preserved DUPEITEM where the source had it
		/// uncommented).</summary>
		[SteamFunction]
		public static void TestSetupFlippableItem(Character self) {
			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone_4");
			AbstractItem item = (AbstractItem) wallDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("FLIP_SETUP: uid=" + item.Uid + " model=" + item.Model);
		}

		/// <summary>i_wall_stone (scripts/converted/itemb1.def, [ItemDef 0x1a]) is the plain
		/// wall piece nothing else's DUPEITEM points back at - a real content gap (#206), used
		/// here to cover .flip's "nothing to flip to" feedback message.</summary>
		[SteamFunction]
		public static void TestSetupNonFlippableItem(Character self) {
			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			AbstractItem item = (AbstractItem) wallDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("FLIP_SETUP: uid=" + item.Uid + " model=" + item.Model);
		}
	}
}
