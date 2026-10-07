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

namespace SteamEngine.CompiledScripts {
	public static class SoundCalculator {
		public const int NoSound = (int) SoundNames.None;

		[SteamFunction]
		public static void PlayAngerSound(Character self) {
			int sound = self.TypeDef.AngerSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.AngerSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		[SteamFunction]
		public static void PlayIdleSound(Character self) {
			int sound = self.TypeDef.IdleSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.IdleSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		[SteamFunction]
		public static void PlayMissSound(Character self) {
			int sound = NoSound;
			switch (self.WeaponType) {
				case WeaponType.OneHandSword:
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					sound = (int) SoundNames.swish03;
					break;
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					sound = (int) SoundNames.swish02;
					break;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
				case WeaponType.TwoHandSword:
				case WeaponType.Bow:
				case WeaponType.XBow:
					sound = (int) SoundNames.swish01;
					break;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		[SteamFunction]
		public static void PlayAttackSound(Character self) {
			CharModelInfo cmi = self.CharModelInfo;
			int sound = NoSound;
			if ((cmi.charAnimType & CharAnimType.Human) == CharAnimType.Human) {
				switch (self.WeaponType) {
					case WeaponType.OneHandAxe:
					case WeaponType.TwoHandAxe:
						sound = (int) SoundNames.axe01;
						break;
					case WeaponType.OneHandBlunt:
					case WeaponType.TwoHandBlunt:
						sound = (int) SoundNames.blunt01;
						break;
					case WeaponType.OneHandSpike:
						sound = (int) SoundNames.sword1;
						break;
					case WeaponType.TwoHandSpike:
						sound = (int) SoundNames.sword7;
						break;
					case WeaponType.OneHandSword:
					case WeaponType.TwoHandSword:
						sound = (int) SoundNames.hvyswrd4;
						break;
					case WeaponType.Bow:
					case WeaponType.XBow:
						sound = (int) SoundNames.crossbow;
						break;
				}
			} else {
				sound = self.TypeDef.AttackSound;
				if (sound == NoSound) {
					sound = cmi.charDef.AttackSound;
				}
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		[SteamFunction]
		public static void PlayHurtSound(Character self) {
			int sound = self.TypeDef.HurtSound;
			if (sound == NoSound) {
				sound = self.CharModelInfo.charDef.HurtSound;
			}
			if (sound != NoSound) {
				self.Sound(sound);
			}
		}

		[SteamFunction]
		public static void PlayDeathSound(Character self) {
			CharModelInfo cmi = self.CharModelInfo;
			int sound;
			if ((cmi.charAnimType & CharAnimType.Human) == CharAnimType.Human) {
				if (cmi.isFemale) {
					sound = Globals.dice.Next(4) + 788;
				} else {
					sound = Globals.dice.Next(5) + 1059;
				}
				self.Sound(sound);
			} else {
				sound = self.TypeDef.DeathSound;
				if (sound == NoSound) {
					sound = cmi.charDef.DeathSound;
				}
				if (sound != NoSound) {
					self.Sound(sound);
				}
			}
		}
	}
}






 

 

 

 

 

 

 

 

			//miss
			//swish01 = 568,
			//swish02 = 569,
			//swish03 = 570,