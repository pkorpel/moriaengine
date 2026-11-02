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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	public class t_pitcher_empty : CompiledTriggerGroup {

	}

	/// <summary>#962 - Sphere's Use_Kindling: on the ground a roll against 0-29 of Camping makes a fire that burns 4
	/// minutes and one more per stick. No gain by use.</summary>
	public class t_kindling : CompiledTriggerGroup {
		private const int MostDifficulty = 30, DifficultyScale = 10, FireSound = 0x226, BaseMinutes = 4;

		public TriggerResult On_DClick(Item self, Character clicker) {
			SmallSkillsLoc texts = Loc<SmallSkillsLoc>.Get(clicker.Language);
			if (!(self.Cont is null)) {
				clicker.SysMessage(texts.CantLightHere);
			} else if (!SkillDef.GetBySkillName(SkillName.Camping).CheckSuccess(clicker, Globals.dice.Next(MostDifficulty) * DifficultyScale)) {
				clicker.SysMessage(texts.FireFailed);
			} else {
				Item fire = (Item) ThingDef.GetByDefname("i_campfire").Create(self.X, self.Y, self.Z, self.M);
				fire.Flag_NonMovable = true;
				fire.Sound(FireSound);
				fire.AddTimer(CampfireTimer.Key, new CampfireTimer()).DueInSeconds = (BaseMinutes + self.Amount) * 60;
				self.Delete();
			}
			return TriggerResult.Cancel;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class CampfireTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_campfire_");

		[LoadingInitializer, DeepCopyImplementation]
		public CampfireTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			((Thing) cont).Delete();
		}
	}

	public class t_campfire : CompiledTriggerGroup {

	}

	public class t_garbage : CompiledTriggerGroup {

	}

	public class t_key : CompiledTriggerGroup {

	}

	public class t_keyring : CompiledTriggerGroup {

	}

	public class t_comm_crystal : CompiledTriggerGroup {

	}
}