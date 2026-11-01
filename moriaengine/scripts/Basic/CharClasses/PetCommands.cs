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
	/// defend me, guard (target), stay / stay here / stop, follow (target), go (target), kill /
	/// attack (target), transfer (target), release / dismiss, friend (target), drop, status. Every
	/// order replaces what the pet was doing, a fight included, and the pet answers "Yes Master" /
	/// "Sorry" (animals make their idle/anger sound instead). Friends (#774) can give orders too,
	/// except transfer/release/friend, which stay with the owner.
	///
	/// GMs bypass the ownership check - their commands reach any NPC in range, wild monsters
	/// included (Sphere's NPC_IsOwnedBy does the same for a higher plevel). See
	/// Character.FollowTarget for how a GM can direct an unowned NPC without making it their pet.
	///
	/// Parsed on the speaker's own On_Say (not per-pet On_Hear) so that "all go"/"all kill" issue
	/// a single target request whose result applies to every matched pet.
	///
	/// </summary>
	public partial class Character {
		public override TriggerResult On_Say(string speech, SpeechType type, int[] keywords) {
			if (this.Flag_Hidden) {
				HidingSkillDef.Reveal(this); //#962
			}
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
			if ((command == PetCommand.Transfer) || (command == PetCommand.Release) || (command == PetCommand.Friend)) {
				//#774 - a friend may order the pet around, but only its owner can give it away
				int before = pets.Count;
				pets.RemoveAll(pet => !this.IsGM && (pet.ControlMaster != this));
				if (pets.Count < before) {
					this.SysMessage("Only the owner can do that.");
				}
				if (pets.Count == 0) {
					return;
				}
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
				case PetCommand.Guard:
					this.TargetForPets(SingletonScript<Targ_PetGuard>.Instance, pets, "What should your " + petsWord + " guard?");
					break;
				case PetCommand.Friend:
					this.TargetForPets(SingletonScript<Targ_PetFriend>.Instance, pets, "Who should your " + petsWord + " also obey?");
					break;
				case PetCommand.Drop:
					foreach (NPC pet in pets) {
						PetDrop(pet, this);
					}
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
						pet.PetFriends = null;
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

		/// <summary>Every order replaces what the pet was doing, a fight or guarding an item included (Sphere's Skill_Start).</summary>
		internal static void BreakOffForNewOrder(NPC pet) {
			NPCAiTimer timer = NPCAiTimer.GetFor(pet);
			if (timer != null) {
				timer.Disengage();
			}
			pet.GuardedItem = null;
		}

		/// <summary>#774 - may `speaker` give this pet orders: its owner, a friend added with "friend",
		/// or a GM (Sphere's NPC_IsOwnedBy: MEMORY_IPET | MEMORY_FRIEND, or a higher plevel).</summary>
		public bool AcceptsOrdersFrom(Character speaker) {
			if (speaker == null) {
				return false;
			}
			if (speaker.IsGM || (this.ControlMaster == speaker)) {
				return true;
			}
			List<Character> friends = this.PetFriends;
			return (friends != null) && friends.Contains(speaker);
		}

		/// <summary>#774 - the pet guarding `item` (or the ground item it's inside of) that's close
		/// enough to stop `picker` taking it, or null. Owners, friends and GMs are never stopped.</summary>
		public static NPC FindItemGuard(AbstractItem item, Character picker) {
			AbstractItem top = item.TopObj() as AbstractItem;
			if ((top == null) || !top.IsOnGround) {
				return null;
			}
			foreach (AbstractCharacter ch in top.GetMap().GetCharsInRange(top.X, top.Y, ItemGuardRange)) {
				NPC guard = ch as NPC;
				if ((guard != null) && (guard.GuardedItem == top) && !guard.Flag_Dead && !guard.AcceptsOrdersFrom(picker)) {
					return guard;
				}
			}
			return null;
		}

		/// <summary>#774 - how close a guarding pet has to be to its item to stop someone taking it.</summary>
		public const int ItemGuardRange = 6;

		/// <summary>#774 - Sphere's NPC_PetDrop: empties the pet's backpack at its feet. Its own
		/// starting gear (attr_newbie) stays with it.</summary>
		private static void PetDrop(NPC pet, Character master) {
			AbstractItem pack = pet.FindLayer(LayerNames.Pack);
			List<AbstractItem> toDrop = new List<AbstractItem>();
			if (pack != null) {
				foreach (AbstractItem item in pack) {
					Item asItem = item as Item;
					if ((asItem == null) || !asItem.HasAttribute(ItemAttributes.attr_newbie)) {
						toDrop.Add(item);
					}
				}
			}
			if (toDrop.Count == 0) {
				if (pet.IsHuman) {
					pet.Say("I'm carrying nothing.");
				} else {
					PetResponse(pet, master, false);
				}
				return;
			}
			foreach (AbstractItem item in toDrop) {
				item.P(pet.X, pet.Y, pet.Z, pet.M);
			}
			PetResponse(pet, master, true);
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
			if (pet.GuardedItem != null) {
				return "guarding " + pet.GuardedItem.Name;
			}
			if (pet.ShouldFollowOwner) {
				return (pet.IsGuarding ? "guarding " : "following ") + ((pet.FollowTarget ?? pet.ControlMaster)?.Name ?? "nobody");
			}
			return "staying";
		}

		/// <summary>
		/// "all"/"pets" -> every controllable NPC within speech range. Otherwise -> the first one
		/// whose own name or creature type name matches. A GM's commands match ANY NPC; a regular
		/// player's commands only match NPCs that accept their orders (owner or friend, #774).
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
				if (!isGM && !pet.AcceptsOrdersFrom(this)) {
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

	/// <summary>#774 - "guard": an item -> stay by it and stop strangers taking it; a character ->
	/// follow and protect them; yourself -> the same as "guard me".</summary>
	public class Targ_PetGuard : CompiledTargetDef {
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
				pet.IsGuarding = true;
				Character.PetResponse(pet, self, true);
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			AbstractItem top = targetted.TopObj() as AbstractItem;
			if ((top == null) || !top.IsOnGround) {
				self.SysMessage("Your pet can only guard something lying on the ground.");
				return TargetResult.RestartTargetting;
			}
			foreach (NPC pet in pets) {
				Character.BreakOffForNewOrder(pet);
				pet.GuardedItem = top;
				pet.ShouldFollowOwner = false;
				pet.PetGoalPoint = null;
				pet.IsGuarding = false;
				Character.PetResponse(pet, self, true);
			}
			Logger.WriteDebug("PetCommand 'guard': " + self.Name + " set " + pets.Count + " pet(s) to guard " + top);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You must target an item or someone to guard.");
			return TargetResult.RestartTargetting;
		}
	}

	/// <summary>#774 - "friend": the targeted player may give these pets orders too.</summary>
	public class Targ_PetFriend : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			List<NPC> pets = (List<NPC>) parameter;
			if (!targetted.IsPlayer) {
				self.SysMessage("Only a player can be your pet's friend.");
				return TargetResult.RestartTargetting;
			}
			foreach (NPC pet in pets) {
				if ((targetted == pet.ControlMaster) || pet.AcceptsOrdersFrom(targetted)) {
					Character.PetResponse(pet, self, false); //already obeys them
					continue;
				}
				List<Character> friends = pet.PetFriends ?? new List<Character>();
				friends.Add(targetted);
				pet.PetFriends = friends;
				Character.PetResponse(pet, self, true);
				targetted.SysMessage(self.Name + " has granted you the ability to give orders to " + pet.Name + ".");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("You must target a player.");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("You must target a player.");
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
				Character.BreakOffForNewOrder(pet);
				pet.ControlMaster = targetted;
				pet.PetFriends = null; //the new owner chooses their own friends
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
