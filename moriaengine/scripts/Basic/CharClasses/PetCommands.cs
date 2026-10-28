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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Pet control via normal speech, parsed like Sphere 0.99z8 (#768, see PetCommandParser):
	/// "all &lt;command&gt;" / "pets &lt;command&gt;" for every pet this character owns in range, or
	/// "&lt;name or creature type&gt; &lt;command&gt;" for one. Commands: come / follow me, guard me /
	/// defend me / guard, stay / stay here / stop, follow (target), go (target), kill / attack
	/// (target), transfer (target), release / dismiss, status. Every order replaces what the pet
	/// was doing, a fight included, and the pet answers "Yes Master" / "Sorry" (animals make
	/// their idle/anger sound instead).
	///
	/// GMs bypass the ownership check - their commands reach any NPC in range, wild monsters
	/// included (Sphere's NPC_IsOwnedBy does the same for a higher plevel). See
	/// Character.FollowTarget for how a GM can direct an unowned NPC without making it their pet.
	///
	/// Parsed on the speaker's own On_Say (not per-pet On_Hear) so that "all go"/"all kill" issue
	/// a single target request whose result applies to every matched pet.
	///
	/// Not here yet: "friend" (co-owners need a persisted friend list) and "drop" (pets carry nothing).
	/// </summary>
	public partial class Character {
		public override TriggerResult On_Say(string speech, SpeechType type, int[] keywords) {
			this.TryHandlePetCommand(speech);
			return base.On_Say(speech, type, keywords);
		}

		private void TryHandlePetCommand(string speech) {
			string addressee;
			PetCommand command;
			if (!PetCommandParser.TryParse(speech, out addressee, out command)) {
				return;
			}

			List<NPC> pets = this.FindControllableNpcs(addressee);
			Logger.WriteDebug("PetCommand: " + this.Name + " said '" + speech + "' (target='" + addressee + "', verb='" + command + "') -> matched " + pets.Count + " NPC(s)" +
				(pets.Count > 0 ? ": " + string.Join(", ", pets.ConvertAll(p => p.Name + "#" + p.Uid)) : ""));
			if (pets.Count == 0) {
				return;
			}
			string petsWord = "pet" + (pets.Count > 1 ? "s" : "");

			switch (command) {
				case PetCommand.Come:
				case PetCommand.GuardMe:
					foreach (NPC pet in pets) {
						BreakOffForNewOrder(pet);
						pet.FollowTarget = this; //explicit, not just relying on ControlMaster - lets a GM direct an unowned NPC too
						pet.ShouldFollowOwner = true;
						pet.PetGoalPoint = null;
						if (command == PetCommand.GuardMe) {
							pet.IsGuarding = true;
						}
						PetResponse(pet, this, true);
					}
					this.SysMessage(command == PetCommand.GuardMe ?
						"Your " + petsWord + " will guard you." :
						"Your " + petsWord + " will come to you.");
					break;
				case PetCommand.Stay:
					foreach (NPC pet in pets) {
						BreakOffForNewOrder(pet); //previously "stop" only touched Follow/wander state - an active fight kept running regardless
						if (pet.ControlMaster == null) {
							pet.FollowTarget = this; //mark a wild NPC as under control so it stands put instead of resuming autonomous wandering
						}
						pet.ShouldFollowOwner = false;
						pet.PetGoalPoint = null;
						pet.IsGuarding = false; //a staying pet no longer runs off to defend, then stays wherever that fight ended
						PetResponse(pet, this, true);
					}
					this.SysMessage("Your " + petsWord + " will stay put.");
					break;
				case PetCommand.Follow:
					this.TargetForPets(SingletonScript<Targ_PetFollow>.Instance, pets, "Whom should your " + petsWord + " follow?");
					break;
				case PetCommand.Go:
					this.TargetForPets(SingletonScript<Targ_PetGo>.Instance, pets, "Where should your " + petsWord + " go?");
					break;
				case PetCommand.Kill:
					this.TargetForPets(SingletonScript<Targ_PetKill>.Instance, pets, "What do you want your " + petsWord + " to attack?");
					break;
				case PetCommand.Transfer:
					this.TargetForPets(SingletonScript<Targ_PetTransfer>.Instance, pets, "Whom do you want to transfer your " + petsWord + " to?");
					break;
				case PetCommand.Release:
					foreach (NPC pet in pets) {
						BreakOffForNewOrder(pet);
						pet.ControlMaster = null;
						pet.FollowTarget = null;
						pet.IsGuarding = false;
						pet.ShouldFollowOwner = true;
						pet.PetGoalPoint = null;
						PetResponse(pet, this, true);
					}
					this.SysMessage("You release your " + petsWord + " into the wild.");
					break;
				case PetCommand.Status:
					foreach (NPC pet in pets) {
						this.SysMessage(pet.Name + ": health " + pet.Hits + "/" + pet.MaxHits + ", " + DescribeOrders(pet));
					}
					break;
			}
		}

		private void TargetForPets(CompiledTargetDef targetDef, List<NPC> pets, string prompt) {
			Player self = this as Player;
			if (self != null) {
				self.SysMessage(prompt);
				self.Target(targetDef, pets);
			}
		}

		/// <summary>Every order replaces what the pet was doing, a fight included (Sphere's Skill_Start).</summary>
		internal static void BreakOffForNewOrder(NPC pet) {
			NPCAiTimer timer = NPCAiTimer.GetFor(pet);
			if (timer != null) {
				timer.Disengage();
			}
		}

		/// <summary>Sphere's NPC_PetResponse: the pet turns to its master and says "Yes Master" or
		/// "Sorry"; a pet that can't speak (anything not human) makes its idle or anger sound instead.</summary>
		internal static void PetResponse(NPC pet, Character master, bool success) {
			if (master != null) {
				pet.Direction = Point2D.GetDirFromTo(pet, master);
			}
			if (pet.IsHuman) {
				pet.Say(success ? "Yes Master" : "Sorry");
			} else if (success) {
				SoundCalculator.PlayIdleSound(pet);
			} else {
				SoundCalculator.PlayAngerSound(pet);
			}
		}

		private static string DescribeOrders(NPC pet) {
			NPCAiTimer timer = NPCAiTimer.GetFor(pet);
			if ((timer != null) && (timer.Target != null)) {
				return "fighting " + timer.Target.Name;
			}
			if (pet.PetGoalPoint != null) {
				return "going to " + pet.PetGoalPoint;
			}
			if (pet.ShouldFollowOwner) {
				return (pet.IsGuarding ? "guarding " : "following ") + ((pet.FollowTarget ?? pet.ControlMaster)?.Name ?? "nobody");
			}
			return "staying";
		}

		/// <summary>
		/// "all"/"pets" -> every controllable NPC within speech range. Otherwise -> the first one
		/// whose own name or creature type name matches. A GM's commands match ANY NPC; a regular
		/// player's commands only match NPCs that player actually owns (ControlMaster == this).
		/// </summary>
		private List<NPC> FindControllableNpcs(string targetName) {
			List<NPC> result = new List<NPC>();
			bool wantsAll = PetCommandParser.IsAllPets(targetName);
			bool isGM = this.IsGM;
			int npcsInRange = 0;
			int npcsOwnedByMe = 0;

			foreach (AbstractCharacter ch in this.GetMap().GetCharsInRange(this.X, this.Y, Globals.SpeechDistance)) {
				NPC pet = ch as NPC;
				if (pet == null) {
					continue;
				}
				npcsInRange++;
				if (pet.ControlMaster == this) {
					npcsOwnedByMe++;
				}
				if (!isGM && (pet.ControlMaster != this)) {
					continue;
				}
				if (wantsAll) {
					result.Add(pet);
				} else if (PetCommandParser.NameMatches(targetName, pet.Name) || PetCommandParser.NameMatches(targetName, pet.Def.Name)) {
					result.Add(pet);
					break; //first match wins, same as Sphere
				}
			}
			Logger.WriteDebug("FindControllableNpcs: " + this.Name + " (GM=" + isGM + ") speech range=" + Globals.SpeechDistance +
				" -> " + npcsInRange + " NPC(s) in range, " + npcsOwnedByMe + " owned by me, " + result.Count + " matched target '" + targetName + "'");
			return result;
		}
	}

	public class Targ_PetFollow : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			foreach (NPC pet in pets) {
				if (targetted == pet) {
					Character.PetResponse(pet, self, false);
					continue;
				}
				Character.BreakOffForNewOrder(pet);
				pet.FollowTarget = targetted;
				pet.ShouldFollowOwner = true;
				pet.PetGoalPoint = null;
				Character.PetResponse(pet, self, true);
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You must target someone to follow.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You must target someone to follow.");
			return TargetResult.RestartTargetting;
		}
	}

	public class Targ_PetGo : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			Point3D goal = new Point3D(targetted.X, targetted.Y, targetted.Z);
			foreach (NPC pet in pets) {
				Character.BreakOffForNewOrder(pet);
				pet.PetGoalPoint = goal;
				Character.PetResponse(pet, self, true);
			}
			Logger.WriteDebug("PetCommand 'go': " + self.Name + " sent " + pets.Count + " pet(s) to " + goal);
			self.SysMessage("Your pet" + (pets.Count > 1 ? "s" : "") + " will go there.");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return this.On_TargonGround(self, targetted, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return this.On_TargonGround(self, targetted, parameter);
		}
	}

	public class Targ_PetKill : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			if (targetted == self) {
				self.SysMessage("Your pet won't attack you.");
				return TargetResult.Done;
			}
			NPC targettedNpc = targetted as NPC;
			if ((targettedNpc != null) && (targettedNpc.ControlMaster == self)) {
				//Sphere refuses targets the speaker owns - one pet of yours won't attack another
				foreach (NPC pet in pets) {
					Character.PetResponse(pet, self, false);
				}
				return TargetResult.Done;
			}
			foreach (NPC pet in pets) {
				if (targetted == pet) {
					Character.PetResponse(pet, self, false);
					continue;
				}
				Character.PetResponse(pet, self, true);
				pet.PetGoalPoint = null;
				pet.Flag_WarMode = true;
				NPCAiTimer timer = NPCAiTimer.GetFor(pet);
				Logger.WriteDebug("PetCommand 'kill': " + pet.Name + "#" + pet.Uid + " engaging " + targetted.Name + " (timer found=" + (timer != null) + ")");
				if (timer != null) {
					timer.EngageTarget(targetted);
				}
			}
			self.SysMessage("Your pet" + (pets.Count > 1 ? "s" : "") + " will attack " + targetted.Name + ".");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You must target a creature to attack.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You must target a creature to attack.");
			return TargetResult.RestartTargetting;
		}
	}

	public class Targ_PetTransfer : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			if (targetted == self) {
				self.SysMessage("Your pet is already yours.");
				return TargetResult.Done;
			}
			foreach (NPC pet in pets) {
				pet.ControlMaster = targetted;
				pet.FollowTarget = null; //clear any GM override so the new owner takes effect in NPCAiTimer's FollowTarget ?? ControlMaster check
				pet.ShouldFollowOwner = true;
				pet.PetGoalPoint = null;
			}
			self.SysMessage("You transfer your pet" + (pets.Count > 1 ? "s" : "") + " to " + targetted.Name + ".");
			targetted.SysMessage(self.Name + " transfers " + (pets.Count > 1 ? "pets" : "a pet") + " to you.");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You must target a person to transfer your pet to.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You must target a person to transfer your pet to.");
			return TargetResult.RestartTargetting;
		}
	}
}
