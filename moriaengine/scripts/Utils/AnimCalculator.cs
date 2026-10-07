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

namespace SteamEngine.CompiledScripts {
	public static class AnimCalculator {

		//For the AnimDuration method, which returns the approximate number of seconds an anim will take.
		private static float[,] animDuration;
		private static uint[] animsAvailableTranslate;

		static AnimCalculator() {
			animDuration = new float[256, 256];
			for (int numFrames = 0; numFrames < 256; numFrames++) {
				for (int frameDelay = 0; frameDelay < 256; frameDelay++) {
					animDuration[numFrames, frameDelay] = SlowAnimDuration((ushort) numFrames, (byte) frameDelay);
				}
			}
			animsAvailableTranslate = new uint[32];

			//Fill animsAvailableTranslate with the power-of-two for each bit of animsAvailable.
			uint code = 1;
			for (int anim = 0; anim < 32; anim++) {
				animsAvailableTranslate[anim] = code;
				code += code;
			}
		}

		/**
			Returns an estimate of how long UO should take to finish the anim.
			The formula used is (.25*numFrames)+(.1*numFrames*frameDelay), which I got by timing how long it took to do
			anim 14 (which has 7 frames) with a variety of frameDelay values. (I figured out what the base delay
			per frame was first, and then figured out how much frameDelay affected it - I rounded, by the way, to nice
			even values, because if I were programming the UO client I would have used nice even values.
			
			But the people who programmed the UO client didn't always do what I would have done, hehe. In fact,
			they often seem to have done things that seem totally bizarre to me - like having numBackwardsFrames
			and numAnims (my names, since of course I don't have a clue what they called them!) work the way they do....
			)
			
			Example for anim 14:
				Anim(14);
				conn.WriteLine("That should take "+AnimDuration(7, 0)+" seconds to draw.");
			If someone makes a script using @timer to print the message when time is up, we can see if my estimates
			are accurate. I'll probably wind up doing that later, since the whole purpose of my testing the anim packet
			and making the AnimDuration method was that I would need to be able to predict when an anim would end,
			for future scripts - the use I was pondering was making the swing speed of weapons actually be synced with
			the anim speed of the attacking anims when using that anim, and it looks like I should be able to do that :).
			
			(
				This method, by the way, uses a two-dimensional lookup-table, which is why numFrames is a byte -
				we need 256 KB of RAM for the 256x256x4b table (4bytes for each element is a float), which isn't much,
				but if numFrames were a ushort and all possible values were legal, then it would need 65536*256*4b =
				64 MB of RAM, and that would not be good. :P
				
				I figure 256 KB is an acceptable tradeoff for the speed gain from not needing the multiplications
				(unless .NET does evil things with two-dimensional tables - I know they're slower than one-dimensional
				ones, but even if it's 4x slower than a one-dimensional table, it's still 15x or so (estimating!)
				faster than doing the three multiplications instead, and also still faster than it would be to have two
				one-dimensional tables (animBaseTime[numFrames] and animAdditionalTimePerFrame[frameDelay]),
				and to multiply the additional-time-per-frame by numFrames, (then adding the two together, which takes
				very little time compared to multiplication).
			)
				
			@numFrames This is a byte, although it is possible to have more than 255 frames. If you do, you could
				call SlowAnimDuration(ushort numFrames, byte frameDelay) instead, which is slower than this method.
				Or, if your number of frames is fixed, or if frameDelay is, then you could precalculate the results
				or write a faster calculation than what SlowAnimDuration does (based on
				(.25*numFrames)+(.1*numFrames*frameDelay), just precalculate whatever you can, like .25*numFrames if you
				know numFrames, or .1*frameDelay if you know frameDelay, etc). 
				
				This is the number of frames UO would be drawing. So if you're drawing it both forward and backward
				(using 'undo'), double the # of frames. If you're drawing it backwards and limiting the # of frames,
				well, it's up to you to feed this method the actual # of frames that will be drawn.
			@frameDelay The 'frameDelay' value you passed to the Anim method. If the anim method you used didn't take
				a frameDelay parameter, it defaulted to 0.
		*/
		public static float AnimDuration(byte numFrames, byte frameDelay) {
			return animDuration[numFrames, frameDelay];
		}

		private static float SlowAnimDuration(ushort numFrames, byte frameDelay) {
			return (.25f * numFrames) + (.1f * numFrames * frameDelay);
		}

		public static bool CanPerformAnim(Character self, AnimalAnim anim) {
			return CanPerformAnim(anim, self.AnimsAvailable);
		}
		public static bool CanPerformAnim(AnimalAnim anim, uint animsAvailable) {
			return (animsAvailable & (animsAvailableTranslate[(int) anim])) > 0;
		}
		public static bool CanPerformAnim(Character self, MonsterAnim anim) {
			return CanPerformAnim(anim, self.AnimsAvailable);
		}
		public static bool CanPerformAnim(MonsterAnim anim, uint animsAvailable) {
			return (animsAvailable & (animsAvailableTranslate[(int) anim])) > 0;
		}

		public static AnimalAnim GetAnimalAnim(GenericAnim anim, uint animsAvailable) {
			AnimalAnim realAnim = AnimalAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk:
					realAnim = AnimalAnim.Walk;
					break;
				case GenericAnim.Run:
					if (CanPerformAnim(AnimalAnim.Run, animsAvailable)) {
						realAnim = AnimalAnim.Run;
					} else {
						realAnim = AnimalAnim.Walk;
					}
					break;
				case GenericAnim.StandStill:
					if (CanPerformAnim(AnimalAnim.StandStill, animsAvailable)) {
						realAnim = AnimalAnim.StandStill;
					} else {
						realAnim = AnimalAnim.Walk;
					}
					break;
				case GenericAnim.RandomIdleAction:
					bool canPerformIdle1 = CanPerformAnim(AnimalAnim.IdleAction, animsAvailable);
					bool canPerformIdle2 = CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable);
					if (canPerformIdle1 && canPerformIdle2) {
						switch (Globals.dice.Next(2)) {
							case 0:
								realAnim = AnimalAnim.IdleAction;
								break;
							case 1:
								realAnim = AnimalAnim.IdleAction2;
								break;
						}
					} else if (canPerformIdle1) {
						realAnim = AnimalAnim.IdleAction;
					} else if (canPerformIdle2) {
						realAnim = AnimalAnim.IdleAction2;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.IdleAction:
					if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
						realAnim = AnimalAnim.IdleAction;
					} else if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
						realAnim = AnimalAnim.IdleAction2;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.LookAround:
					if (CanPerformAnim(AnimalAnim.IdleAction2, animsAvailable)) {
						realAnim = AnimalAnim.IdleAction2;
					} else if (CanPerformAnim(AnimalAnim.IdleAction, animsAvailable)) {
						realAnim = AnimalAnim.IdleAction;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.AttackStab:
					if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim = AnimalAnim.Attack2;
					} else if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim = AnimalAnim.Attack;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.AttackSwing:
				case GenericAnim.AttackOverhead:
				case GenericAnim.AttackShoot:
				case GenericAnim.AttackBareHands:
				case GenericAnim.Cast:
					if (CanPerformAnim(AnimalAnim.Attack, animsAvailable)) {
						realAnim = AnimalAnim.Attack;
					} else if (CanPerformAnim(AnimalAnim.Attack2, animsAvailable)) {
						realAnim = AnimalAnim.Attack2;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.GetHit:
					if (CanPerformAnim(AnimalAnim.GetHit, animsAvailable)) {
						realAnim = AnimalAnim.GetHit;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.FallBackwards:
					if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
						realAnim = AnimalAnim.Die;
					} else if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
						realAnim = AnimalAnim.Die2;
					} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
						realAnim = AnimalAnim.LieDown;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.FallForwards:
					if (CanPerformAnim(AnimalAnim.Die2, animsAvailable)) {
						realAnim = AnimalAnim.Die2;
					} else if (CanPerformAnim(AnimalAnim.Die, animsAvailable)) {
						realAnim = AnimalAnim.Die;
					} else if (CanPerformAnim(AnimalAnim.LieDown, animsAvailable)) {
						realAnim = AnimalAnim.LieDown;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.Block:		//also represents Dodge
					if (CanPerformAnim(AnimalAnim.Unknown, animsAvailable)) {
						realAnim = AnimalAnim.Unknown;
					} else {
						realAnim = AnimalAnim.StandStill;
					}
					break;
				case GenericAnim.Bow:
					realAnim = AnimalAnim.Eat;
					break;
				case GenericAnim.Salute:
					realAnim = AnimalAnim.Eat;
					break;
				case GenericAnim.Drink:	//also represents Eat
					realAnim = AnimalAnim.Eat;
					break;
				default:
					throw new SanityCheckException("Unknown generic anim " + anim);
			}
			return realAnim;
		}

		public static MonsterAnim GetMonsterAnim(GenericAnim anim, uint animsAvailable) {
			MonsterAnim realAnim = MonsterAnim.Walk;
			switch (anim) {
				case GenericAnim.Walk: {
						realAnim = MonsterAnim.Walk;
						break;
					}
				case GenericAnim.Run: {
						if (CanPerformAnim(MonsterAnim.Fly, animsAvailable)) {
							realAnim = MonsterAnim.Fly;
						} else {
							realAnim = MonsterAnim.Walk;
						}
						break;
					}
				case GenericAnim.StandStill: {
						if (CanPerformAnim(MonsterAnim.StandStill, animsAvailable)) {
							realAnim = MonsterAnim.StandStill;
						} else {
							realAnim = MonsterAnim.Walk;
						}
						break;
					}
				case GenericAnim.RandomIdleAction: {
						bool canPerformIdle1 = CanPerformAnim(MonsterAnim.IdleAction, animsAvailable);
						bool canPerformIdle2 = CanPerformAnim(MonsterAnim.LookAround, animsAvailable);
						if (canPerformIdle1 && canPerformIdle2) {
							switch (Globals.dice.Next(2)) {
								case 0:
									realAnim = MonsterAnim.IdleAction;
									break;
								case 1:
									realAnim = MonsterAnim.LookAround;
									break;
							}
						} else if (canPerformIdle1) {
							realAnim = MonsterAnim.IdleAction;
						} else if (canPerformIdle2) {
							realAnim = MonsterAnim.LookAround;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.IdleAction: {
						if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
							realAnim = MonsterAnim.IdleAction;
						} else if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
							realAnim = MonsterAnim.LookAround;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.LookAround: {
						if (CanPerformAnim(MonsterAnim.LookAround, animsAvailable)) {
							realAnim = MonsterAnim.LookAround;
						} else if (CanPerformAnim(MonsterAnim.IdleAction, animsAvailable)) {
							realAnim = MonsterAnim.IdleAction;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackSwing: {
						if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim = MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim = MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim = MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim = MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim = MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim = MonsterAnim.Attack6;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackStab: {
						if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim = MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim = MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim = MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim = MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim = MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim = MonsterAnim.Attack6;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackOverhead: {
						if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim = MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim = MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim = MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim = MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim = MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim = MonsterAnim.Attack6;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.AttackShoot: {
						if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim = MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim = MonsterAnim.Attack5;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim = MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim = MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim = MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim = MonsterAnim.Attack6;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.GetHit:
					int count = 0;

					if (CanPerformAnim(MonsterAnim.GetHit, animsAvailable)) {
						tempMonsterAnimArray[0] = MonsterAnim.GetHit;
						count++;
					}
					if (CanPerformAnim(MonsterAnim.BlockLeft, animsAvailable)) {
						tempMonsterAnimArray[count] = MonsterAnim.BlockLeft;
						count++;
					}
					if (CanPerformAnim(MonsterAnim.BlockRight, animsAvailable)) {
						tempMonsterAnimArray[count] = MonsterAnim.BlockRight;
						count++;
					}

					if (count > 0) {
						realAnim = tempMonsterAnimArray[Globals.dice.Next(count)];
					}
					break;
				case GenericAnim.FallBackwards: {
						if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
							realAnim = MonsterAnim.FallBackwards;
						} else if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
							realAnim = MonsterAnim.FallForwards;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.FallForwards: {
						if (CanPerformAnim(MonsterAnim.FallForwards, animsAvailable)) {
							realAnim = MonsterAnim.FallForwards;
						} else if (CanPerformAnim(MonsterAnim.FallBackwards, animsAvailable)) {
							realAnim = MonsterAnim.FallBackwards;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Block: {		//also represents Dodge
						double dbl = Globals.dice.NextDouble();
						if (dbl < 0.5 && CanPerformAnim(MonsterAnim.BlockLeft, animsAvailable)) {
							realAnim = MonsterAnim.BlockLeft;
						} else if (CanPerformAnim(MonsterAnim.BlockRight, animsAvailable)) {
							realAnim = MonsterAnim.BlockRight;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Cast: //TODO? check if this makes sense
				case GenericAnim.AttackBareHands: {
						if (CanPerformAnim(MonsterAnim.Attack6, animsAvailable)) {
							realAnim = MonsterAnim.Attack6;
						} else if (CanPerformAnim(MonsterAnim.Attack, animsAvailable)) {
							realAnim = MonsterAnim.Attack;
						} else if (CanPerformAnim(MonsterAnim.Attack2, animsAvailable)) {
							realAnim = MonsterAnim.Attack2;
						} else if (CanPerformAnim(MonsterAnim.Attack3, animsAvailable)) {
							realAnim = MonsterAnim.Attack3;
						} else if (CanPerformAnim(MonsterAnim.Attack4, animsAvailable)) {
							realAnim = MonsterAnim.Attack4;
						} else if (CanPerformAnim(MonsterAnim.Attack5, animsAvailable)) {
							realAnim = MonsterAnim.Attack5;
						} else {
							realAnim = MonsterAnim.StandStill;
						}
						break;
					}
				case GenericAnim.Bow: {
						realAnim = MonsterAnim.StandStill;
						break;
					}
				case GenericAnim.Salute: {
						realAnim = MonsterAnim.StandStill;
						break;
					}
				case GenericAnim.Drink: {	//also represents Eat
						realAnim = MonsterAnim.StandStill;
						break;
					}
				default: {
						throw new SanityCheckException("Unknown generic anim " + anim);
					}
			}
			return realAnim;
		}

		public static HumanAnim GetHumanAnim(Character self, GenericAnim anim) {
			HumanAnim realAnim = HumanAnim.WalkUnarmed;
			switch (anim) {
				case GenericAnim.Walk:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedWalk;
					} else if (self.Flag_WarMode) {
						realAnim = HumanAnim.WalkWarMode;
					} else if (self.WeaponAnimType != WeaponAnimType.BareHands) {
						realAnim = HumanAnim.WalkArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
					} else {
						realAnim = HumanAnim.WalkUnarmed;
					}
					break;
				case GenericAnim.Run:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedRun;
					} else if (self.WeaponAnimType != WeaponAnimType.BareHands) {
						realAnim = HumanAnim.RunArmed;	//TODO: Check this, see if it looks right with two-handed weapons, only a shield, etc.
					} else {
						realAnim = HumanAnim.RunUnarmed;
					}
					break;
				case GenericAnim.StandStill:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedStandStill;
					} else if (self.Flag_WarMode) {
						realAnim = HumanAnim.WarMode;
					} else {
						realAnim = HumanAnim.StandStill;
					}
					break;
				case GenericAnim.RandomIdleAction:
					if (self.Flag_Riding) {
						double dbl = Globals.dice.NextDouble();
						if (dbl < .25) {
							realAnim = HumanAnim.MountedSalute;
						} else if (dbl < .5) {
							realAnim = HumanAnim.MountedBlock;
						} else if (dbl < .75) {
							realAnim = HumanAnim.MountedGetHit;
						} else {
							if (self.WeaponAnimType != WeaponAnimType.HeldInLeftHand) {
								//This looks like slapping the horse, and that's what wolfpack calls it,
								//but if you ask me, you shouldn't use it for an idle action unless you don't
								//have a two-handed weapon equipped (or it will look like you're attacking).
								realAnim = HumanAnim.MountedLeftHandAttack;
							} else {
								//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
								//with only one hand, so this should look like slapping the horse too.
								realAnim = HumanAnim.MountedRightHandAttack;
							}
						}
					} else if (self.Flag_WarMode) {
						realAnim = HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						double dbl = Globals.dice.NextDouble();
						if (dbl < .5) {
							realAnim = HumanAnim.LookAround;
						} else {
							realAnim = HumanAnim.LookDown;
						}
					}
					break;
				case GenericAnim.IdleAction:
					if (self.Flag_Riding) {
						double dbl = Globals.dice.NextDouble();
						if (dbl < .5) {
							realAnim = HumanAnim.MountedSalute;
						} else {
							if (self.WeaponAnimType != WeaponAnimType.HeldInLeftHand) {
								//This looks like slapping the horse, and that's what wolfpack calls it,
								//but if you ask me, you shouldn't use it for an idle action unless you don't
								//have a two-handed weapon equipped (or it will look like you're attacking).
								//Heh.
								realAnim = HumanAnim.MountedLeftHandAttack;
							} else {
								//Let's see how this looks. IIRC, if you're mounted you hold two-handed weapons
								//with only one hand, so this should look like slapping the horse too.
								realAnim = HumanAnim.MountedRightHandAttack;
							}
						}
					} else if (self.Flag_WarMode) {
						realAnim = HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						realAnim = HumanAnim.LookDown;
					}
					break;
				case GenericAnim.LookAround:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedSalute;
					} else if (self.Flag_WarMode) {
						realAnim = HumanAnim.WarMode;	//no idle actions when in war mode.
					} else {
						realAnim = HumanAnim.LookAround;
					}
					break;
				case GenericAnim.AttackSwing:
					if (self.Flag_Riding) {
						if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim = HumanAnim.MountedLeftHandAttack;
						} else {
							realAnim = HumanAnim.MountedRightHandAttack;
						}
					} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
						realAnim = HumanAnim.LeftHandSwing;
					} else {
						realAnim = HumanAnim.RightHandSwing;
					}
					break;
				case GenericAnim.AttackStab:
					if (self.Flag_Riding) {
						if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim = HumanAnim.MountedLeftHandAttack;
						} else {
							realAnim = HumanAnim.MountedRightHandAttack;
						}
					} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
						realAnim = HumanAnim.LeftHandStab;
					} else {
						realAnim = HumanAnim.RightHandStab;
					}
					break;
				case GenericAnim.AttackOverhead:
					if (self.Flag_Riding) {
						if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
							realAnim = HumanAnim.MountedLeftHandAttack;
						} else {
							realAnim = HumanAnim.MountedRightHandAttack;
						}
					} else if (self.WeaponAnimType == WeaponAnimType.HeldInLeftHand) {
						realAnim = HumanAnim.LeftHandOverhead;
					} else {
						realAnim = HumanAnim.RightHandOverhead;
					}
					break;
				case GenericAnim.AttackShoot:
					if (self.WeaponAnimType == WeaponAnimType.XBow) {
						if (self.Flag_Riding) {
							realAnim = HumanAnim.MountedFireCrossbow;
						} else {
							realAnim = HumanAnim.FireCrossbow;
						}
					} else {//what isnt xbow, is bow.
						if (self.Flag_Riding) {
							realAnim = HumanAnim.MountedFireBow;
						} else {
							realAnim = HumanAnim.FireBow;
						}
					}
					break;
				case GenericAnim.GetHit:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedGetHit;
					} else {
						realAnim = HumanAnim.GetHit;
					}
					break;
				case GenericAnim.FallBackwards:
					realAnim = HumanAnim.FallBackwards;
					break;
				case GenericAnim.FallForwards:
					realAnim = HumanAnim.FallForwards;
					break;
				case GenericAnim.Block:		//also represents Dodge
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedBlock;
					} else {
						realAnim = HumanAnim.Block;
					}
					break;
				case GenericAnim.AttackBareHands:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedRightHandAttack;
					} else {
						realAnim = HumanAnim.AttackBareHands;
					}
					break;
				case GenericAnim.Bow:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedBlock;
					} else {
						realAnim = HumanAnim.Bow;
					}
					break;
				case GenericAnim.Salute:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedSalute;
					} else {
						realAnim = HumanAnim.Salute;
					}
					break;
				case GenericAnim.Drink:	//also represents Eat
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedSalute;
					} else {
						realAnim = HumanAnim.Drink;
					}
					break;
				case GenericAnim.Cast:
					if (self.Flag_Riding) {
						realAnim = HumanAnim.MountedCast;
					} else {
						double dbl = Globals.dice.NextDouble();
						if (dbl < .5) {
							realAnim = HumanAnim.Cast;
						} else {
							realAnim = HumanAnim.CastForward;
						}
					}
					break;
				default:
					throw new SanityCheckException("Unknown generic anim " + anim);
			}
			return realAnim;
		}

		public static byte TranslateAnim(Character self, GenericAnim anim) {
			byte realAnim = 0;
			if ((byte) anim >= (byte) HumanAnim.NumAnims) {
				Sanity.IfTrueThrow((byte) anim > 0xff, "Cannot perform anim '" + anim + "', that number is too high.");
				realAnim = (byte) anim;
			} else {
				CharModelInfo cmi = self.CharModelInfo;
				uint animsAvailable = cmi.AnimsAvailable;
				CharAnimType bat = cmi.charAnimType;

				if ((bat & CharAnimType.Human) == CharAnimType.Human) {
					realAnim = (byte) GetHumanAnim(self, anim);
				} else if ((bat & CharAnimType.Monster) == CharAnimType.Monster) {
					realAnim = (byte) GetMonsterAnim(anim, animsAvailable);
				} else if ((bat & CharAnimType.Animal) == CharAnimType.Animal) {
					realAnim = (byte) GetAnimalAnim(anim, animsAvailable);
				}
			}
			Logger.WriteDebug("Translated " + anim + " to " + realAnim);
			return realAnim;
		}

		public static HumanAnim GetHumanAttackAnim(Character self) {
			HumanAnim anim = HumanAnim.AttackBareHands;
			if (self.Flag_Riding) {
				switch (self.WeaponAnimType) {
					case WeaponAnimType.XBow:
						anim = HumanAnim.MountedFireCrossbow;
						break;
					case WeaponAnimType.Bow:
						anim = HumanAnim.MountedFireBow;
						break;
					case WeaponAnimType.HeldInLeftHand:
						anim = HumanAnim.MountedLeftHandAttack;
						break;
					case WeaponAnimType.HeldInRightHand:
						anim = HumanAnim.MountedRightHandAttack;
						break;
					case WeaponAnimType.BareHands:
						anim = HumanAnim.MountedRightHandAttack;
						break;
				}
			} else {

				switch (self.WeaponType) {
					case WeaponType.OneHandSpike:
						anim = HumanAnim.OneHandStab;
						break;
					case WeaponType.TwoHandSpike:
						anim = HumanAnim.TwoHandStab;
						break;
					case WeaponType.OneHandSword:
						switch (Globals.dice.Next(3)) {
							case 0:
								anim = HumanAnim.OneHandStab;
								break;
							case 1:
								anim = HumanAnim.OneHandSwing;
								break;
							case 2:
								anim = HumanAnim.OneHandOverhead;
								break;
						}
						break;
					case WeaponType.TwoHandSword:
						switch (Globals.dice.Next(3)) {
							case 0:
								anim = HumanAnim.TwoHandStab;
								break;
							case 1:
								anim = HumanAnim.TwoHandSwing;
								break;
							case 2:
								anim = HumanAnim.TwoHandOverhead;
								break;
						}
						break;

					case WeaponType.OneHandBlunt:
					case WeaponType.OneHandAxe:
						switch (Globals.dice.Next(2)) {
							case 0:
								anim = HumanAnim.OneHandOverhead;
								break;
							case 1:
								anim = HumanAnim.OneHandSwing;
								break;
						}
						break;
					case WeaponType.TwoHandBlunt://or should blunt weapons only do overhead?

					case WeaponType.TwoHandAxe:
						switch (Globals.dice.Next(2)) {
							case 0:
								anim = HumanAnim.TwoHandOverhead;
								break;
							case 1:
								anim = HumanAnim.TwoHandSwing;
								break;
						}
						break;
					case WeaponType.Bow:
						anim = HumanAnim.FireBow;
						break;
					case WeaponType.XBow:
						anim = HumanAnim.FireCrossbow;
						break;
				}
			}
			return anim;
		}


		static MonsterAnim[] tempMonsterAnimArray = new MonsterAnim[(int) MonsterAnim.NumAnims];
		//we have 6 different attacks. I hope it's correct.
		public static MonsterAnim GetMonsterRandomAttackAnim(uint animsAvailable) {
			int count = 0;

			for (MonsterAnim i = MonsterAnim.Attack1, n = MonsterAnim.Attack6; i <= n; i++) {
				if (CanPerformAnim(i, animsAvailable)) {
					tempMonsterAnimArray[count] = i;
					count++;
				}
			}

			if (count > 0) {
				int random = Globals.dice.Next(count);

				return tempMonsterAnimArray[random];
			}
			return MonsterAnim.Walk;
		}


		public static AnimalAnim GetAnimalRandomAttackAnim(uint animsAvailable) {
			bool canPerformAttack1 = CanPerformAnim(AnimalAnim.Attack1, animsAvailable);
			bool canPerformAttack2 = CanPerformAnim(AnimalAnim.Attack2, animsAvailable);

			if (canPerformAttack1 && canPerformAttack2) {
				switch (Globals.dice.Next(2)) {
					case 0:
						return AnimalAnim.Attack1;
					case 1:
						return AnimalAnim.Attack2;
				}
			} else if (canPerformAttack1) {
				return AnimalAnim.Attack1;
			} else if (canPerformAttack2) {
				return AnimalAnim.Attack2;
			}
			return AnimalAnim.Walk;
		}

		public static void PerformAttackAnim(Character self) {
			int anim = 0;

			CharModelInfo cmi = self.CharModelInfo;
			CharAnimType bat = cmi.charAnimType;

			if ((bat & CharAnimType.Human) == CharAnimType.Human) {
				anim = (int) GetHumanAttackAnim(self);
			} else if ((bat & CharAnimType.Monster) == CharAnimType.Monster) {
				anim = (int) GetMonsterRandomAttackAnim(cmi.AnimsAvailable);
			} else if ((bat & CharAnimType.Animal) == CharAnimType.Animal) {
				anim = (int) GetAnimalRandomAttackAnim(cmi.AnimsAvailable);
			}

			double seconds = self.WeaponDelay.TotalSeconds;
			byte frameDelay = 0;
			seconds = seconds - (0.25 * attackAnimFrames);
			if (seconds > 0) {
				frameDelay = (byte) (seconds / (.1 * attackAnimFrames));
			}

			self.Anim(anim, frameDelay);
		}
		//(.25*numFrames)+(.1*numFrames*frameDelay)
		const double attackAnimFrames = 4;

		public static void PerformAnim(Character self, GenericAnim anim) {
			self.Anim(TranslateAnim(self, anim));
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards) {
			self.Anim(TranslateAnim(self, anim), backwards);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, bool undo) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, undo, 0x01);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, byte frameDelay) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, false, frameDelay);
		}

		public static void PerformAnim(Character self, GenericAnim anim, bool backwards, bool undo, byte frameDelay) {
			self.Anim(TranslateAnim(self, anim), 1, backwards, undo, frameDelay);
		}
	}
}