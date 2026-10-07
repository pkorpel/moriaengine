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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public abstract class CraftingSkillDef : SkillDef {

		public CraftingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		/// <summary>
		/// This trigger is used only when clicked the skill-list blue radio button for some crafting skill.
		/// opens the craftmenu for the given skill
		/// </summary>
		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return TriggerResult.Cancel; //something wrong, finish now
			}

			if (skillSeqArgs.Param1 != null) {
				return TriggerResult.Continue; //do not stop, we have some item here, we can start crafting now
			} //no item pre-selected, open the craftmenu
			self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(CraftmenuContents.MainCategories[(SkillName) this.Id]));
			//do not continue, the rest will be solved from the craftmenu
			return TriggerResult.Cancel; //F = continue to @start, T = stop
		}

		/// <summary>
		/// This trigger is called when OK button is clicked from the craftmenu. One item from the queue is picked and its making process 
		/// begins here (i.e. the making success and the number of strokes is pre-computed here)
		/// </summary>
		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return TriggerResult.Cancel; //something wrong, finish now
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;//the on_start trigger runs only if there is something here...
															   //get the strokes count (i.e. number of animations and skillmaking sounds before the item is made)
			double[] itemStrokes = iDefToMake.Strokes; //always 2-item array
													   //compute the actual strokes count to make the selected item for "self" char
			int strokes = (int) Math.Round(ScriptUtil.EvalRangePermille(this.SkillValueOfChar(self), itemStrokes));
			//check the Success now / TODO> item difficulty
			skillSeqArgs.Success = this.CheckSuccess(self, iDefToMake.Difficulty);
			if (!skillSeqArgs.Success) {//the item making will fail
				skillSeqArgs.Param2 = (int) Math.Round(ScriptUtil.GetRandInRange(1, strokes));//fail will occur after a few strokes (not necessary immediatelly)
			} else {//item will be created, with pre-computed number of strokes
				skillSeqArgs.Param2 = strokes;
			}
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		/// <summary>
		/// This trigger is run a pre-computed number of times before the item is either created or failed.
		/// The item-making animations and sounds are run from here
		/// </summary>
		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(skillSeqArgs.Self);
				return TriggerResult.Cancel; //something wrong, finish now
			}

			int strokesCnt = Convert.ToInt32(skillSeqArgs.Param2);
			if (strokesCnt > 1) {
				//do the animation, sound and repeat the stroke
				this.DoStroke(skillSeqArgs);
				skillSeqArgs.Param2 = strokesCnt - 1;
				skillSeqArgs.DelayStroke();
				return TriggerResult.Cancel; //stop here for now (we are waiting for the next stroke round...)
			}
			this.DoStroke(skillSeqArgs);//do the animation, sound and finish
			return TriggerResult.Continue; //continue to @success or @fail (the result is already prepared from the "@start" phase
		}

		/// <summary>
		/// This trigger is run in case the item making succeeds. It consumes the desired number of resources, 
		/// creates the item instance and checks if there are any other items to be created in the queue. If so, it 
		/// re-runs the @start trigger
		/// </summary>
		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Player self = (Player) skillSeqArgs.Self;
			//todo: paralyzed state etc.
			//some special requirements will be also checked (if present)
			if (!this.CheckPrerequisities(skillSeqArgs) || !this.DoCheckSpecials(skillSeqArgs)) {
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return;//something wrong, finish now
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			IResourceListEntry missingResource;
			bool canMake = true;
			//first check the necessary resources to have (GM needn't have anything) (if any resources are needed)
			if (!self.IsGM && iDefToMake.SkillMake != null && !iDefToMake.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingResource)) {
				self.SysMessage(missingResource.GetResourceMissingMessage(self.Language));
				canMake = false;
			}

			if (canMake) {//if still OK try to consume the consumable resources (if any) otherwise step over this block
				if (!self.IsGM && iDefToMake.Resources != null && !iDefToMake.Resources.ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missingResource)) {
					self.SysMessage(missingResource.GetResourceMissingMessage(self.Language));
					canMake = false;
				} else {//resources consumed or we are GM or no resources needed, create the item and place it to the pre-defined (or default) location
					Item newItem = (Item) iDefToMake.Create(self.ReceivingContainer);
					//self.ClilocSysMessage(500638);//You create the item and put it in your backpack.
					self.SysMessage(string.Format(
								Loc<CraftSkillsLoc>.Get(self.Language).ItemMadeAndPutInRecCont,
								iDefToMake.Name, self.ReceivingContainer.Name));
					this.DoSuccess(skillSeqArgs, newItem);

					CraftingProcessPlugin.MakeFinished(skillSeqArgs, true);
				}
			}

			//deal with failure - not enough resources 
			if (!canMake) {
				CraftingProcessPlugin.MakeImpossible(skillSeqArgs);
				//the rest will be decided by the CraftingProcessPlugin
			}
		}

		/// <summary>
		/// This trigger is run in case the item making fails. It consumes some of the desired number of resources (i.e. these are wasted), 
		/// and checks if there are any other items to be created in the queue. If so, it 
		/// re-runs the @start trigger
		/// </summary>
		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) { //checking alive will be enough
				CraftingProcessPlugin.UnInstallCraftingPlugin(self);
				return;//no message needed, it's been already sent in the called method
			}

			ItemDef iDefToMake = (ItemDef) skillSeqArgs.Param1;

			//check if we can even make it (i.e. we have necessary resources) otherwise we can attempt to make something without resources and in case 
			//we failed the next attempt will begin afterwards (which makes no sense since we do not even have the resources...)
			IResourceListEntry missingResource;
			bool canMake = true;
			//first check the necessary resources to have (GM needn't have anything) (if any resources are needed)
			if (!self.IsGM && iDefToMake.SkillMake != null && !iDefToMake.SkillMake.HasResourcesPresent(self, ResourcesLocality.BackpackAndLayers, out missingResource)) {
				canMake = false;
			}

			bool hasMaterial = true;
			//then check if the necessary material is present
			if (!self.IsGM && iDefToMake.Resources != null && !iDefToMake.Resources.HasResourcesPresent(self, ResourcesLocality.Backpack, out missingResource)) {
				hasMaterial = false;
			}

			if (canMake && hasMaterial) {
				//lost some resources. if some of resources/material was missing then nothing happened...
				iDefToMake.Resources.ConsumeSomeResources(self, ResourcesLocality.Backpack);
				self.SysMessage(string.Format(
						Loc<CraftSkillsLoc>.Get(self.Language).ItemMakingFailed,
						iDefToMake.Name));
			}
			CraftingProcessPlugin.MakeFinished(skillSeqArgs, canMake && hasMaterial);
			//the rest will be decided by the CraftingProcessPlugin
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<CraftSkillsLoc>.Get(self.Language).CraftingAborted);
			CraftingProcessPlugin.UnInstallCraftingPlugin(self);
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to make something related to the stroke of the particular skill (such as playing some sound...)
		/// </summary>
		protected virtual void DoStroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(skillSeqArgs.SkillDef.Defname + " stroke");
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to make something related to the success of the particular skill (such as playing sound,
		/// computing some special item characteristics etc.)
		/// </summary>
		protected virtual void DoSuccess(SkillSequenceArgs skillSeqArgs, Item newItem) {
			//TODO pocitat nejake ty "exceptional" vlastnosti, podepisovani predmetu atp.
			Character self = skillSeqArgs.Self;
			self.SysMessage(skillSeqArgs.SkillDef.Defname + " success");
		}

		/// <summary>
		/// This method is intended to be overriden by particular CraftingSkillDef classes. 
		/// Its purpose is to check any additional pre-requisities for succesfull skill usage 
		/// (e.g. for BS check if the anvil or forge is near etc...).
		/// Return false if the trigger above should be cancelled or true if we can continue
		/// </summary>
		protected virtual bool DoCheckSpecials(SkillSequenceArgs skillSeqArgs) {
			return true;
		}

		/// <summary>
		/// Check if we are alive, have enough stats etc. Return false if the trigger above
		/// should be cancelled or true if we can continue
		/// </summary>
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Stam <= self.MaxStam / 10) {
				self.ClilocSysMessage(501991);//You are too fatigued to even lift a finger.
				return false; //stop
			}
			return true;
		}

		/// <summary>Check the resources and skillmake if the given character can craft this item</summary>
		public static bool CanBeMade(ItemDef iDef, Character chr) {
			if (chr.IsGM) {//GM can everything
				return true;
			}
			//skillmake (skills, tools etc.)
			ResourcesList requir = iDef.SkillMake;
			if (requir != null) {
				IResourceListEntry missingItem;
				if (!requir.HasResourcesPresent(chr, ResourcesLocality.BackpackAndLayers, out missingItem)) {
					return false;
				}
			}

			//resources (necessary items)
			ResourcesList reslist = iDef.Resources;
			if (reslist != null) {
				IResourceListEntry missingItem;
				if (!reslist.HasResourcesPresent(chr, ResourcesLocality.Backpack, out missingItem)) {
					return false;
				}
			}
			return true;
		}
	}

	internal class CraftSkillsLoc : CompiledLocStringCollection<CraftSkillsLoc> {
		public string ResourcesLackingButContinue = "Not enough resources to make {0}, continuing with other items.";
		public string ResourcesLackingAndFinish = "Not enough resources to make {0}, crafting is finished.";
		public string ItemMakingFailed = "You have failed to make {0} and lost some of the resources";
		public string FinishCrafting = "Crafting is finished";
		public string CraftingAborted = "Crafting aborted";
		//500638 - You create the item and put it in your backpack.
		public string ItemMadeAndPutInRecCont = "You create the {0} and put it in your {1}";
	}
}