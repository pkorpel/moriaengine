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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Classic UO pet control via normal speech - "all come"/"all guard"/"all kill" (every pet
	/// this character owns in range) or "&lt;petname&gt; come" etc (one specific pet). "drop" is
	/// deliberately not implemented - pets don't carry any cargo at all right now (no pack-animal
	/// concept exists), so there'd be nothing for it to act on, and the exact item-container
	/// movement API wasn't worth guessing at for a command with nothing to drop yet.
	///
	/// GMs bypass the ownership check entirely - "all come"/"<name> stop" from a GM affects any
	/// NPC in range (wild monsters included), not just their own pets. See Character.FollowTarget
	/// for how a GM can direct an unowned NPC without making it their permanent pet.
	///
	/// Parsed on the speaker's own On_Say (not per-pet On_Hear) so that "all go"/"all transfer"/
	/// "all kill" can issue a single target request whose result applies to every matched pet - if
	/// each pet independently called Player.Target, only the last one processed would actually
	/// receive the click.
	/// </summary>
	public partial class Character {
		private static readonly string[] petCommandVerbs = { "come", "go", "stop", "transfer", "guard", "kill", "attack", "release" };

		public override TriggerResult On_Say(string speech, SpeechType type, int[] keywords) {
			this.TryHandlePetCommand(speech);
			return base.On_Say(speech, type, keywords);
		}

		private void TryHandlePetCommand(string speech) {
			if (string.IsNullOrWhiteSpace(speech)) {
				return;
			}
			string[] parts = speech.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2) {
				return;
			}
			string verb = parts[parts.Length - 1].ToLowerInvariant();
			if (Array.IndexOf(petCommandVerbs, verb) < 0) {
				return;
			}
			string targetName = string.Join(" ", parts, 0, parts.Length - 1);

			List<NPC> pets = this.FindControllableNpcs(targetName);
			if (pets.Count == 0) {
				return;
			}

			switch (verb) {
				case "come":
					foreach (NPC pet in pets) {
						pet.FollowTarget = this; //explicit, not just relying on ControlMaster - lets a GM direct an unowned NPC too
						pet.ShouldFollowOwner = true;
						pet.PetGoalPoint = null;
					}
					this.SysMessage("Your pet" + (pets.Count > 1 ? "s" : "") + " will come to you.");
					break;
				case "stop":
					foreach (NPC pet in pets) {
						if (pet.ControlMaster == null) {
							pet.FollowTarget = this; //mark a wild NPC as under control so it stands put instead of resuming autonomous wandering
						}
						pet.ShouldFollowOwner = false;
						pet.PetGoalPoint = null;
					}
					this.SysMessage("Your pet" + (pets.Count > 1 ? "s" : "") + " will stay put.");
					break;
				case "go": {
						Player self = this as Player;
						if (self != null) {
							self.SysMessage("Where should your pet" + (pets.Count > 1 ? "s" : "") + " go?");
							self.Target(SingletonScript<Targ_PetGo>.Instance, pets);
						}
						break;
					}
				case "transfer": {
						Player self = this as Player;
						if (self != null) {
							self.SysMessage("Whom do you want to transfer your pet" + (pets.Count > 1 ? "s" : "") + " to?");
							self.Target(SingletonScript<Targ_PetTransfer>.Instance, pets);
						}
						break;
					}
				case "guard":
					foreach (NPC pet in pets) {
						pet.IsGuarding = true;
						pet.FollowTarget = this;
						pet.ShouldFollowOwner = true;
						pet.PetGoalPoint = null;
					}
					this.SysMessage("Your pet" + (pets.Count > 1 ? "s" : "") + " will guard you.");
					break;
				case "kill":
				case "attack": {
						Player self = this as Player;
						if (self != null) {
							self.SysMessage("What do you want your pet" + (pets.Count > 1 ? "s" : "") + " to attack?");
							self.Target(SingletonScript<Targ_PetKill>.Instance, pets);
						}
						break;
					}
				case "release":
					foreach (NPC pet in pets) {
						pet.ControlMaster = null;
						pet.FollowTarget = null;
						pet.IsGuarding = false;
						pet.ShouldFollowOwner = true;
						pet.PetGoalPoint = null;
					}
					this.SysMessage("You release your pet" + (pets.Count > 1 ? "s" : "") + " into the wild.");
					break;
			}
		}

		/// <summary>
		/// "all" -> every matching NPC within speech range. Otherwise -> the one matching NPC
		/// whose name matches (case-insensitive), if any. A GM's commands match ANY NPC; a
		/// regular player's commands only match NPCs that NPC actually owns (ControlMaster == this).
		/// </summary>
		private List<NPC> FindControllableNpcs(string targetName) {
			List<NPC> result = new List<NPC>();
			bool wantsAll = string.Equals(targetName, "all", StringComparison.OrdinalIgnoreCase);
			bool isGM = this.IsGM;

			foreach (AbstractCharacter ch in this.GetMap().GetCharsInRange(this.X, this.Y, Globals.SpeechDistance)) {
				NPC pet = ch as NPC;
				if ((pet == null) || (!isGM && (pet.ControlMaster != this))) {
					continue;
				}
				if (wantsAll) {
					result.Add(pet);
				} else if (string.Equals(pet.Name, targetName, StringComparison.OrdinalIgnoreCase)) {
					result.Add(pet);
					break; //names are expected to be unique enough for this purpose - first match wins
				}
			}
			return result;
		}
	}

	public class Targ_PetGo : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			Point3D goal = new Point3D(targetted.X, targetted.Y, targetted.Z);
			foreach (NPC pet in pets) {
				pet.PetGoalPoint = goal;
			}
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
			foreach (NPC pet in pets) {
				pet.Flag_WarMode = true;
				NPCAiTimer timer = NPCAiTimer.GetFor(pet);
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
